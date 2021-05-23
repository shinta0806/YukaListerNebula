// ============================================================================
// 
// ゆかり用の Web サーバー機能
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 【アクセス仕様】
// ・サムネイル画像取得
//   <アドレス>:<ポート>/thumb?uid=<ファイル番号>[&width=<横幅>][&easypass=<簡易認証キーワード>]
//   横幅として指定可能な値は YlCommon.THUMB_WIDTH_LIST 参照
//   http://localhost:13582/thumb?uid=7&width=80
// ・動画プレビュー
//   <アドレス>:<ポート>/preview?uid=<ファイル番号>[&easypass=<簡易認証キーワード>]
//   http://localhost:13582/preview?uid=123
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// コアでは無いし、待機用イベントも使わないので YukaListerCore の派生にはしない
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.WebServer
{
	public class WebServer : IDisposable
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public WebServer()
		{
			_tokenSource = new CancellationTokenSource();
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// IDisposable.Dispose()
		// --------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// --------------------------------------------------------------------
		// 非同期に稼働終了
		// --------------------------------------------------------------------
		public async Task QuitAsync()
		{
			await Task.Run(() =>
			{
				try
				{
					_tokenSource.Cancel();

					// 終了コマンドを送信してサーバーの待機を終了させる
					WebRequest request = WebRequest.Create(URL_LOCAL_HOST + YukaListerModel.Instance.EnvModel.YlSettings.WebServerPort.ToString() + '/' + SERVER_COMMAND_QUIT);
					using WebResponse response = request.GetResponse();
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "プレビューサーバー終了");
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "プレビューサーバー終了時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			});
		}

		// --------------------------------------------------------------------
		// 稼働開始
		// --------------------------------------------------------------------
		public void Start()
		{
			// async を待機しない
			_ = YlCommon.LaunchTaskAsync<Object?>(_semaphoreSlim, WebServerByWorker, null);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected virtual void Dispose(Boolean isDisposing)
		{
			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
				_semaphoreSlim.Dispose();
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// 直ちに起動できるタスクの数（アプリケーション全体）
		private const Int32 APP_WORKER_THREADS = 16;

		// Web サーバー以外用に残しておくタスクの数
		private const Int32 GENERAL_WORKER_THREADS = 1;

		// サムネイルのアスペクト比
		private const Double THUMB_ASPECT_RATIO = 16.0 / 9;

		// ローカルホスト
		private const String URL_LOCAL_HOST = "http://localhost:";

		// コマンド
		private const String SERVER_COMMAND_PREVIEW = "preview";
		private const String SERVER_COMMAND_QUIT = "quit";
		private const String SERVER_COMMAND_THUMB = "thumb";

		// サムネイル生成時のタイムアウト [ms]
		private const Int32 THUMB_TIMEOUT = 10 * 1000;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// タスク上限
		private Int32 _webServerTasksLimit;

		// 終了用（サーバー再起動があるためアプリケーションのトークンとは異なるものを使用する）
		private readonly CancellationTokenSource _tokenSource;

		// 多重起動抑止
		private readonly SemaphoreSlim _semaphoreSlim = new(1);

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// URL 引数を解析
		// --------------------------------------------------------------------
		private static Dictionary<String, String> AnalyzeCommandOptions(String command)
		{
			Dictionary<String, String> options = new();

			Int32 quesPos = command.IndexOf('?');
			if (0 <= quesPos && quesPos < command.Length - 1)
			{
				String[] optionStrings = command[(quesPos + 1)..].Split('&');
				for (Int32 i = 0; i < optionStrings.Length; i++)
				{
					Int32 eqPos = optionStrings[i].IndexOf('=');
					if (0 < eqPos && eqPos < optionStrings[i].Length - 1)
					{
						options[optionStrings[i].Substring(0, eqPos)] = optionStrings[i][(eqPos + 1)..];
					}
				}
			}

			return options;
		}

		// --------------------------------------------------------------------
		// 簡易認証を満たしているかどうか
		// ＜返値＞ 満たしている、または、認証不要の場合は true
		// --------------------------------------------------------------------
		private static Boolean CheckEasyAuth(Dictionary<String, String> options, HttpListenerRequest request, HttpListenerResponse response)
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.YukariUseEasyAuth)
			{
				// 認証不要
				return true;
			}

			if (options.ContainsKey(YlConstants.SERVER_OPTION_NAME_EASY_PASS) && options[YlConstants.SERVER_OPTION_NAME_EASY_PASS] == YukaListerModel.Instance.EnvModel.YlSettings.YukariEasyAuthKeyword)
			{
				// URL 認証成功
				Cookie newCookie = new(YlConstants.SERVER_OPTION_NAME_EASY_PASS, options[YlConstants.SERVER_OPTION_NAME_EASY_PASS]);
				newCookie.Path = "/";
				newCookie.Expires = DateTime.Now.AddDays(1.0);
				response.Cookies.Add(newCookie);
				return true;
			}

			Cookie? existCookie = request.Cookies[YlConstants.SERVER_OPTION_NAME_EASY_PASS];
			if (existCookie != null && existCookie.Value == YukaListerModel.Instance.EnvModel.YlSettings.YukariEasyAuthKeyword)
			{
				// クッキー認証成功
				return true;
			}

			return false;
		}

		// --------------------------------------------------------------------
		// サムネイルを JPEG 形式で作成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static TCacheThumb CreateThumb(String path, Int32 width)
		{
			// MediaPlayer がいつまで生きていればサムネイルが確定されるか不明のため、最後に Close() できるよう、最初に生成しておく
			MediaPlayer player = new()
			{
				IsMuted = true,
				ScrubbingEnabled = true,
			};

			try
			{
				// 動画を開いてすぐに一時停止する
				player.Open(new Uri("file://" + path, UriKind.Absolute));
				player.Play();
				player.Pause();

				// 指定位置へシーク
				player.Position = TimeSpan.FromSeconds(YukaListerModel.Instance.EnvModel.YlSettings.ThumbSeekPos);

				// 読み込みが完了するまで待機
				Int32 tick = Environment.TickCount;
				while (player.DownloadProgress < 1.0 || player.NaturalVideoWidth == 0)
				{
					Thread.Sleep(Common.GENERAL_SLEEP_TIME);
					if (Environment.TickCount - tick > THUMB_TIMEOUT)
					{
						throw new Exception("Movie read timeout.");
					}
				}

				// 描画用の Visual に動画を描画
				// 縮小して描画するとニアレストネイバー法で縮小されて画質が悪くなる
				// RenderOptions.SetBitmapScalingMode() も効かないようなので、元のサイズで描画する
				DrawingVisual origVisual = new();
				using (DrawingContext context = origVisual.RenderOpen())
				{
					context.DrawVideo(player, new Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight));
				}

				// ビットマップに Visual を描画
				Boolean isSave = true;
				tick = Environment.TickCount;
				RenderTargetBitmap origBitmap = new(player.NaturalVideoWidth, player.NaturalVideoHeight, 96, 96, PixelFormats.Pbgra32);
				for (; ; )
				{
					origBitmap.Render(origVisual);
					if (IsRenderDone(origBitmap))
					{
						Debug.WriteLine("CreateThumb() render done time: " + (Environment.TickCount - tick));
						break;
					}
					Thread.Sleep(Common.GENERAL_SLEEP_TIME);
					if (Environment.TickCount - tick > THUMB_TIMEOUT)
					{
						// サムネイルが黒い場合もタイムアウトとなるので、キャッシュに保存はしないが送信はする
						isSave = false;
						Debug.WriteLine("CreateThumb() time out: ビットマップに Visual を描画時");
						break;
					}
				}

				// 生成するサムネイルのサイズを計算
				Int32 thumbWidth = width;
				Int32 aThumbHeight = (Int32)(thumbWidth / THUMB_ASPECT_RATIO);
				Debug.WriteLine("[" + Thread.CurrentThread.ManagedThreadId + "] CreateThumb() Thumb size: " + thumbWidth + " x " + aThumbHeight);

				// 動画のリサイズサイズを計算
				Double playerAspectRatio = (Double)player.NaturalVideoWidth / player.NaturalVideoHeight;
				Int32 resizeWidth;
				Int32 resizeHeight;
				if (playerAspectRatio > THUMB_ASPECT_RATIO)
				{
					resizeWidth = thumbWidth;
					resizeHeight = (Int32)(resizeWidth / THUMB_ASPECT_RATIO);
				}
				else
				{
					resizeHeight = aThumbHeight;
					resizeWidth = (Int32)(resizeHeight * THUMB_ASPECT_RATIO);
				}
				Double scale = (Double)resizeWidth / player.NaturalVideoWidth;
				Debug.WriteLine("[" + Thread.CurrentThread.ManagedThreadId + "] CreateThumb() Resize size: " + resizeWidth + " x " + resizeHeight);

				// 縮小
				TransformedBitmap scaledBitmap = new(origBitmap, new ScaleTransform(scale, scale));

				// サムネイルサイズにはめる
				DrawingVisual thumbVisual = new();
				using (DrawingContext context = thumbVisual.RenderOpen())
				{
					context.DrawImage(scaledBitmap, new Rect((thumbWidth - resizeWidth) / 2, (aThumbHeight - resizeHeight) / 2, resizeWidth, resizeHeight));
				}

				// ビットマップに Visual を描画
				tick = Environment.TickCount;
				RenderTargetBitmap thumbBitmap = new(thumbWidth, aThumbHeight, 96, 96, PixelFormats.Pbgra32);
				thumbBitmap.Render(thumbVisual);

				// JPEG にエンコード
				JpegBitmapEncoder jpegEncoder = new();
				jpegEncoder.Frames.Add(BitmapFrame.Create(thumbBitmap));

				// キャッシュに保存
				TCacheThumb cacheThumb = SaveCache(isSave, path, jpegEncoder);

				return cacheThumb;
			}
			finally
			{
				player.Close();
			}
		}

		// --------------------------------------------------------------------
		// サムネイルキャッシュデータベースを検索
		// --------------------------------------------------------------------
		private static TCacheThumb? FindCache(String path, Int32 width)
		{
			if (!String.IsNullOrEmpty(path))
			{
				String fileName = Path.GetFileName(path);
				using ThumbContext thumbContext = ThumbContext.CreateContext(out DbSet<TCacheThumb> cacheThumbs);
				IQueryable<TCacheThumb> queryResult = cacheThumbs.Where(x => x.FileName == fileName && x.Width == width);
				foreach (TCacheThumb record in queryResult)
				{
					// ファイルのタイムスタンプを比較
					FileInfo fileInfo = new(path);
					if (record.FileLastWriteTime == JulianDay.DateTimeToModifiedJulianDate(fileInfo.LastWriteTime))
					{
						Debug.WriteLine("FindCache() Hit: " + path);
						return record;
					}

					// 不一致のキャッシュは削除し、後のキャッシュ保存が可能となるようにする
					try
					{
						cacheThumbs.Remove(record);
						thumbContext.SaveChanges();
					}
					catch (Exception)
					{
					}
				}
			}

			Debug.WriteLine("FindCache() Miss: " + path);
			return null;
		}

		// --------------------------------------------------------------------
		// URL 引数から動画ファイルのパスを解析
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static String GetPathOption(Dictionary<String, String> options)
		{
			if (!options.ContainsKey(YlConstants.SERVER_OPTION_NAME_UID))
			{
				throw new Exception("Parameter " + YlConstants.SERVER_OPTION_NAME_UID + " is not specified.");
			}
			Int32 uid = Int32.Parse(options[YlConstants.SERVER_OPTION_NAME_UID]);

			// ゆかり用データベースから UID を検索
			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds);
			TFound? target = founds.SingleOrDefault(x => x.Uid == uid);
			if (target == null)
			{
				throw new Exception("Bad " + YlConstants.SERVER_OPTION_NAME_UID + ".");
			}
			return target.Path;
		}

		// --------------------------------------------------------------------
		// URL 引数からサムネイル用オプションを解析
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static (String path, Int32 width) GetThumbOptions(Dictionary<String, String> options)
		{
			String path = GetPathOption(options);
			Int32 width;

			// 横幅を解析
			if (!options.ContainsKey(YlConstants.SERVER_OPTION_NAME_WIDTH))
			{
				width = YukaListerModel.Instance.EnvModel.YlSettings.ThumbDefaultWidth;
			}
			else
			{
				width = Int32.Parse(options[YlConstants.SERVER_OPTION_NAME_WIDTH]);
				if (width < YlConstants.THUMB_WIDTH_LIST[0] || width > YlConstants.THUMB_WIDTH_LIST[^1])
				{
					throw new Exception("Bad width.");
				}
			}

			// 既定の横幅に調整
			Int32 index = 0;
			for (Int32 i = YlConstants.THUMB_WIDTH_LIST.Length - 1; i >= 0; i--)
			{
				if (width >= YlConstants.THUMB_WIDTH_LIST[i])
				{
					index = i;
					break;
				}
			}
			width = YlConstants.THUMB_WIDTH_LIST[index];
			return (path, width);
		}

		// --------------------------------------------------------------------
		// Visual に描画された動画がいつビットマップに転写されるか分からないため
		// ビットマップ中央が黒以外になったら転写完了と判断する
		// 動画そのものが黒い場合もあるため、無限ループにならないよう呼び出し元で注意が必要
		// --------------------------------------------------------------------
		private static Boolean IsRenderDone(RenderTargetBitmap bitmap)
		{
			Int32 width = bitmap.PixelWidth;
			Int32 height = bitmap.PixelHeight;
			Byte[] pixels = new Byte[width * height * bitmap.Format.BitsPerPixel / 8];
			Int32 stride = (width * bitmap.Format.BitsPerPixel + 7) / 8;

			// ピクセルデータを配列にコピー
			bitmap.CopyPixels(pixels, stride, 0);

			// 中央の位置
			Int32 offset = (height / 2) * stride + (width / 2) * bitmap.Format.BitsPerPixel / 8;

			// RGB いずれかが 0 以外なら転写完了
			return pixels[offset] != 0 || pixels[offset + 1] != 0 || pixels[offset + 2] != 0;
		}

		// --------------------------------------------------------------------
		// キャッシュデータベースにレコードを保存する
		// --------------------------------------------------------------------
		private static TCacheThumb SaveCache(Boolean isSave, String path, JpegBitmapEncoder jpegEncoder)
		{
			TCacheThumb cacheThumb = new();
			cacheThumb.FileName = Path.GetFileName(path);
			cacheThumb.Width = (Int32)jpegEncoder.Frames[0].Width;
			Debug.WriteLine("SaveCache() width: " + cacheThumb.Width);

			// サムネイル画像データを取得
			using (MemoryStream memStream = new())
			{
				jpegEncoder.Save(memStream);
				cacheThumb.Image = new Byte[memStream.Length];
				memStream.Seek(0, SeekOrigin.Begin);
				memStream.Read(cacheThumb.Image, 0, (Int32)memStream.Length);
			}

			FileInfo fileInfo = new(path);
			cacheThumb.FileLastWriteTime = JulianDay.DateTimeToModifiedJulianDate(fileInfo.LastWriteTime);
			cacheThumb.ThumbLastWriteTime = YlCommon.UtcNowModifiedJulianDate();

			if (isSave)
			{
				using ThumbContext thumbContext = ThumbContext.CreateContext(out DbSet<TCacheThumb> cacheThumbs);

				// 保存
				cacheThumbs.Add(cacheThumb);

				try
				{
					thumbContext.SaveChanges();
				}
				catch (Exception)
				{
					// 他のスレッドが、同一 Uid や同一ファイル名・横幅のレコードを先に書き込んだ場合は例外となるが、
					// キャッシュを保存できなくても致命的ではないため、速やかにクライアントに画像を返すためにリトライはしない
					Debug.WriteLine("SaveCache() save err");
				}
			}

			return cacheThumb;
		}

		// --------------------------------------------------------------------
		// クライアントにエラーメッセージを返す
		// エラーメッセージは ASCII のみを推奨
		// --------------------------------------------------------------------
		private static void SendErrorResponse(HttpListenerResponse response, String message)
		{
			try
			{
				Byte[] data = Encoding.UTF8.GetBytes(message);

				// ヘッダー
				response.StatusCode = (Int32)HttpStatusCode.NotFound;
				response.ContentType = "text/plain";
				response.ContentEncoding = Encoding.UTF8;
				response.ContentLength64 = data.Length;

				// メッセージ本体
				response.OutputStream.Write(data, 0, data.Length);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "エラー応答送信時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// クライアントにサムネイルを返す
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SendResponseThumb(HttpListenerResponse response, Dictionary<String, String> options)
		{
			// サムネイル対象の確定
			(String path, Int32 width) = GetThumbOptions(options);

			// キャッシュから探す
			TCacheThumb? cacheThumb = FindCache(path, width);

			if (cacheThumb == null)
			{
				// キャッシュに無い場合は新規作成
				cacheThumb = CreateThumb(path, width);
			}

			// 更新日
			DateTime lastModified = JulianDay.ModifiedJulianDateToDateTime(cacheThumb.ThumbLastWriteTime);
			String lastModifiedStr = lastModified.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-US")) + " GMT";
			Debug.WriteLine("SendResponseThumb() aLastModifiedStr: " + lastModifiedStr);

			// ヘッダー
			response.StatusCode = (Int32)HttpStatusCode.OK;
			response.ContentType = "image/jpeg";
			response.ContentLength64 = cacheThumb.Image.Length;
			response.Headers.Add(HttpResponseHeader.LastModified, lastModifiedStr);

			// サムネイルデータ
			response.OutputStream.Write(cacheThumb.Image, 0, cacheThumb.Image.Length);
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// クライアントにファイルの内容を返す
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		private void SendFile(HttpListenerResponse response, String path)
		{
			FileInfo fileInfo = new(path);

			// タイプ
			String contentType = Path.GetExtension(path).ToLower() switch
			{
				Common.FILE_EXT_AVI => "video/x-msvideo",
				Common.FILE_EXT_MOV => "video/quicktime",
				Common.FILE_EXT_MP4 => "video/mp4",
				Common.FILE_EXT_MPEG or Common.FILE_EXT_MPG => "video/mpeg",
				_ => "application/octet-stream",
			};

			// ヘッダー
			response.StatusCode = (Int32)HttpStatusCode.OK;
			response.ContentType = contentType;
			response.ContentLength64 = fileInfo.Length;

			// 本体
#if DEBUG
			Int32 readSizes = 0;
#endif
			Byte[] buf = new Byte[1024 * 1024];
			using (FileStream fileStream = new(path, FileMode.Open, FileAccess.Read))
			{
				for (; ; )
				{
					Int32 readSize = fileStream.Read(buf, 0, buf.Length);
					if (readSize == 0)
					{
						break;
					}

					Int32 numRetries = 0;
					while (numRetries < YlConstants.TCP_NUM_RETRIES)
					{
						try
						{
							response.OutputStream.Write(buf, 0, readSize);
							break;
						}
						catch (Exception excep)
						{
							YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "プレビュー内容送信エラー：\n" + excep.Message + "\nリトライ回数：" + numRetries);
							YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
						}
						numRetries++;
						_tokenSource.Token.ThrowIfCancellationRequested();

						// 少し待ってみる
						Thread.Sleep(5 * 1000);
					}
					if (numRetries >= YlConstants.TCP_NUM_RETRIES)
					{
						throw new OperationCanceledException();
					}

#if DEBUG
					readSizes += readSize;
#endif
				}
			}

#if DEBUG
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Verbose, "SendFile() sent: " + readSizes.ToString("#,0") + " / " + fileInfo.Length.ToString("#,0"));
#endif
		}

		// --------------------------------------------------------------------
		// クライアントに応答を返す
		// TcpLister を使用していた頃とは異なりポートノックでは応答に入らない
		// --------------------------------------------------------------------
		private void SendResponse(HttpListenerContext context)
		{
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			try
			{
				if (String.IsNullOrEmpty(request.RawUrl))
				{
					// "http://localhost:13582" が指定されても RawUrl は "/" となるので、空は基本的にありえない
					throw new Exception("Bad URL.");
				}

				// 終了コマンドの場合は何もしない
				if (request.RawUrl.IndexOf(SERVER_COMMAND_QUIT) == 1)
				{
					return;
				}

				// 簡易認証チェック
				Dictionary<String, String> options = AnalyzeCommandOptions(request.RawUrl);
				if (!CheckEasyAuth(options, request, response))
				{
					throw new Exception("Bad auth.");
				}

				// コマンド解析（先頭が '/' であることに注意）
				if (request.RawUrl.IndexOf(SERVER_COMMAND_PREVIEW) == 1)
				{
					SendResponsePreview(response, options);
				}
				else if (request.RawUrl.IndexOf(SERVER_COMMAND_THUMB) == 1)
				{
					SendResponseThumb(response, options);
				}
				else
				{
					// ToDo: obsolete
					// ゆかり側が新 URL に対応次第削除する
					// パス解析（先頭の '/' を除く）
					if (request.RawUrl.Length == 1)
					{
						throw new Exception("File is not specified.");
					}

					String path = HttpUtility.UrlDecode(request.RawUrl, Encoding.UTF8)[1..].Replace('/', '\\');
					if (String.IsNullOrEmpty(path))
					{
						throw new Exception("Path is empty.");
					}
					if (!File.Exists(path))
					{
						throw new Exception("File not found.");
					}

					SendFile(response, path);
				}
			}
			catch (OperationCanceledException)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "クライアントへの応答を中止しました。");
			}
			catch (Exception excep)
			{
				SendErrorResponse(response, excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "クライアントへの応答時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				try
				{
					// 閉じる
					response.Close();
				}
				catch (Exception)
				{
				}
			}
		}

		// --------------------------------------------------------------------
		// クライアントにプレビュー用動画データを返す
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void SendResponsePreview(HttpListenerResponse response, Dictionary<String, String> options)
		{
			// 動画の確定
			String path = GetPathOption(options);
			if (String.IsNullOrEmpty(path))
			{
				throw new Exception("Path is empty.");
			}

			// 送信
			if (!File.Exists(path))
			{
				throw new Exception("File not found.");
			}
			SendFile(response, path!);
		}

		// --------------------------------------------------------------------
		// 外部リクエストにより Web サーバーのタスクが起動されすぎると、他のタスクの起動に悪影響が出るため、Web サーバーのタスク上限を決める
		// --------------------------------------------------------------------
		private void SetWebServerTasksLimit()
		{
			// アプリケーション全体での、直ちに起動できるタスク数を APP_WORKER_THREADS 以上に引き上げる
			ThreadPool.GetMinThreads(out Int32 workerThreads, out Int32 completionPortThreads);
			if (workerThreads < APP_WORKER_THREADS)
			{
				ThreadPool.SetMinThreads(APP_WORKER_THREADS, completionPortThreads);
			}

			// Web サーバーのタスク上限を設定
			_webServerTasksLimit = APP_WORKER_THREADS - GENERAL_WORKER_THREADS;
		}

		// --------------------------------------------------------------------
		// ゆかり用のプレビュー機能を提供する（サーバーとして動作）
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		private Task WebServerByWorker(Object? _)
		{
			HttpListener? listener = null;
			try
			{
				SetWebServerTasksLimit();
				listener = new HttpListener();

				// localhost URL を受け付ける
				listener.Prefixes.Add(URL_LOCAL_HOST + YukaListerModel.Instance.EnvModel.YlSettings.WebServerPort.ToString() + "/");
				listener.Start();

				Int32 numWebServerTasks = 0;
				for (; ; )
				{
					try
					{
						// リクエストが来たら受け入れる
						HttpListenerContext context = listener.GetContext();

						// タスク上限を超えないように調整
						while (numWebServerTasks >= _webServerTasksLimit)
						{
							_tokenSource.Token.ThrowIfCancellationRequested();
							Thread.Sleep(Common.GENERAL_SLEEP_TIME);
						}

						// タスク数として数えた上でタスク実行
						Interlocked.Increment(ref numWebServerTasks);
						Task.Run(() =>
						{
							SendResponse(context);
							Interlocked.Decrement(ref numWebServerTasks);
						});
					}
					catch (Exception excep)
					{
						YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "プレビュー接続ループエラー（リトライします）：\n" + excep.Message);
						YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
					}

					_tokenSource.Token.ThrowIfCancellationRequested();
				}
			}
			catch (OperationCanceledException)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "プレビュー処理を終了しました。");
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "プレビュー処理エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				if (listener != null)
				{
					listener.Stop();
				}
			}
			return Task.CompletedTask;
		}
	}
}
