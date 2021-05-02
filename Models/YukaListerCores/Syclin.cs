// ============================================================================
// 
// ネビュラコア：同期担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Syclin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Syclin()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// メインウィンドウ
		public MainWindowViewModel? MainWindowViewModel { get; set; }

		// 現在アクティブに動作しているか
		public Boolean IsActive { get; set; }

		// サーバーデータ再取得
		public Boolean IsReget { get; set; }

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（同期）のメインルーチン
		// --------------------------------------------------------------------
		protected override void CoreMain()
		{
			YlCommon.SetLogWriterSyncDetail(_logWriterSyncDetail);

			while (true)
			{
				MainEvent.WaitOne();
				Debug.Assert(MainWindowViewModel != null, "Syclin.CoreMain() MainWindowViewModel is null");
				Int32 startTick = Environment.TickCount;

				try
				{
					YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
					if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
					{
						continue;
					}
					if (!YukaListerModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb)
					{
						continue;
					}

					IsActive = true;
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

					// ログイン
					MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期準備中...");
					LoginToSyncServer();

					// データベースをバックアップ
					MusicInfoContext.BackupDatabase();

					// 再取得の場合は楽曲情報データベース初期化
					CreateMusicInfoDbIfNeeded();

					// ダウンロード
					(Int32 numTotalDownloads, Int32 numTotalImports) = DownloadSyncData();

					// アップロード
					Int32 numTotalUploads = UploadSyncData();
					if (numTotalUploads > 0)
					{
						// アップロードを行った場合は、自身がアップロードしたデータの更新日・Dirty を更新するために再ダウンロードが必要
						(numTotalDownloads, numTotalImports) = DownloadSyncData();
					}

					// 完了表示
					MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベース同期完了（ダウンロード"
							+ (numTotalDownloads == 0 ? "無" : " " + numTotalDownloads.ToString("#,0") + " 件、うち " + numTotalImports.ToString("#,0") + " 件インポート")
							+ "、アップロード" + (numTotalUploads == 0 ? "無" : " " + numTotalUploads.ToString("#,0") + " 件") + "）");
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception excep)
				{
					// メッセージボックスではなくステータスバーにエラー表示
					MainWindowViewModel.SetStatusBarMessageWithInvoke(TraceEventType.Error, excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}

				IsActive = false;
				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
		}

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
				_downloader.Dispose();
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

		// ファイル名
		private const String FILE_NAME_CP_MAIN = "CPMain" + Common.FILE_EXT_PHP;

		// 同期モード
		private const String SYNC_MODE_NAME_DOWNLOAD_POST_ERROR = "DownloadPostError";
		private const String SYNC_MODE_NAME_DOWNLOAD_REJECT_DATE = "DownloadRejectDate";
		private const String SYNC_MODE_NAME_DOWNLOAD_SYNC_DATA = "DownloadSyncData";
		private const String SYNC_MODE_NAME_LOGIN = "Login";
		private const String SYNC_MODE_NAME_UPLOAD_SYNC_DATA = "UploadSyncData";

		// その他
		private const Int32 SYNC_INTERVAL = 200;
		private const String SYNC_NO_DATA = "NoData";
		private const Int32 SYNC_UPLOAD_BLOCK = 100;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// ダウンローダー
		private Downloader _downloader = new();

		// 詳細ログ（同期専用）
		private LogWriter _logWriterSyncDetail = new(YlConstants.APP_ID + YlConstants.SYNC_DETAIL_ID);

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 再取得の場合は楽曲情報データベースを初期化
		// --------------------------------------------------------------------
		private void CreateMusicInfoDbIfNeeded()
		{
			if (!IsReget)
			{
				return;
			}

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "サーバーデータ再取得のため楽曲情報データベースを初期化。");
			MusicInfoContext.CreateDatabase();
			IsReget = false;
		}

		// --------------------------------------------------------------------
		// アップロードを拒否されたレコードの更新日をサーバーからダウンロード
		// YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate を更新し、次回ダウンロード時に拒否レコードが上書きされるようにする
		// --------------------------------------------------------------------
		private void DownloadRejectDate()
		{
			try
			{
				String? rejectDateString = _downloader.Download(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_REJECT_DATE), Encoding.UTF8);
				if (String.IsNullOrEmpty(rejectDateString))
				{
					throw new Exception("サーバーからの確認結果が空です。");
				}

				DateTime rejectDate = DateTime.ParseExact(rejectDateString, YlConstants.SYNC_URL_DATE_FORMAT, null);
				Double rejectMjd = JulianDay.DateTimeToModifiedJulianDate(rejectDate);
				if (rejectMjd < YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate)
				{
					YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = rejectMjd;
				}
			}
			catch (Exception excep)
			{
				throw new Exception("アップロード拒否日付を確認できませんでした。\n" + excep.Message);
			}
		}

		// --------------------------------------------------------------------
		// 同期データをサーバーからダウンロード
		// LastSyncDownloadDate も再度ダウンロードする（同日にデータが追加されている可能性があるため）
		// ＜例外＞
		// --------------------------------------------------------------------
		private (Int32 numTotalDownloads, Int32 numTotalImports) DownloadSyncData()
		{
			Debug.Assert(MainWindowViewModel != null, "DownloadSyncData() no main window");

			// ダウンロード開始時刻の記録
			DateTime taskBeginDateTime = DateTime.UtcNow;
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ダウンロード開始");

			if (YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate < YlConstants.INVALID_MJD)
			{
				YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = YlConstants.INVALID_MJD;
			}
			DateTime targetDate = JulianDay.ModifiedJulianDateToDateTime(YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate);
			Int32 numTotalDownloads = 0;
			Int32 numTotalImports = 0;
			for (; ; )
			{
				MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データダウンロード中... ");
				YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = JulianDay.DateTimeToModifiedJulianDate(targetDate);

				// ダウンロード
				String downloadPath = YlCommon.TempPath();
				_downloader.Download(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_SYNC_DATA) + "&Date=" + targetDate.ToString(YlConstants.SYNC_URL_DATE_FORMAT), downloadPath);

				FileInfo fileInfo = new FileInfo(downloadPath);
				if (fileInfo.Length == 0)
				{
					throw new Exception("サーバーからダウンロードしたファイルが空でした。");
				}
				if (fileInfo.Length == SYNC_NO_DATA.Length)
				{
					// targetDate 以降の同期データがなかった
					break;
				}

				// 解凍
				String extractFolder = downloadPath + "_Extract\\";
				Debug.WriteLine("DownloadSyncData() extractFolder: " + extractFolder);
				ZipFile.ExtractToDirectory(downloadPath, extractFolder);

				// インポート
				using SyncDataImporter syncDataImporter = new(MainWindowViewModel, extractFolder);
				syncDataImporter.Import(ref numTotalDownloads, ref numTotalImports);

				// 日付更新
				targetDate = syncDataImporter.Date().AddDays(1);
				if (targetDate > DateTime.UtcNow.Date)
				{
					// 今日を超えたら抜ける
					break;
				}

				Thread.Sleep(SYNC_INTERVAL);
				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = JulianDay.DateTimeToModifiedJulianDate(taskBeginDateTime.Date);
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ダウンロード完了");
			return (numTotalDownloads, numTotalImports);
		}

		// --------------------------------------------------------------------
		// ネットワークが利用可能かどうか（簡易判定）
		// --------------------------------------------------------------------
		private Boolean IsNetworkAvailable()
		{
			try
			{
				_downloader.Download("https://www.google.com/", Encoding.UTF8);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベース同期サーバーにログインする
		// --------------------------------------------------------------------
		private void LoginToSyncServer()
		{
			// SID 取得
			String? sid = null;
			try
			{
				sid = UserPrincipal.Current.Sid.ToString();
			}
			catch (Exception)
			{
			}

			// ログイン情報送信
			Dictionary<String, String?> postParams = new Dictionary<String, String?>
			{
				// HTML Name 属性
				{ "Name", YukaListerModel.Instance.EnvModel.YlSettings.SyncAccount },
				{ "PW", YlCommon.Decrypt(YukaListerModel.Instance.EnvModel.YlSettings.SyncPassword) },
				{ "Mode", SYNC_MODE_NAME_LOGIN },
				{ "IdPrefix", YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix },
				{ "Sid", sid },
				{ "AppGeneration", YlConstants.APP_GENERATION },
				{ "AppVer", YlConstants.APP_VER },
			};
			MainWindowViewModel?.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベース同期サーバーにログインします...");
			Post(postParams);

			// ログイン結果確認
			if (SyncPostErrorExists(out String? errMessage))
			{
				throw new Exception("楽曲情報データベース同期サーバーにログインできませんでした。" + errMessage);
			}

			MainWindowViewModel?.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベース同期サーバーにログインしました。同期処理中です...");
			Thread.Sleep(SYNC_INTERVAL);
			YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
		}

		// --------------------------------------------------------------------
		// POST データ送信
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		private void Post(Dictionary<String, String?> postParams, Dictionary<String, String>? files = null)
		{
			try
			{
				_downloader.Post(YukaListerModel.Instance.EnvModel.YlSettings.SyncServer + FILE_NAME_CP_MAIN, postParams, files);
				Thread.Sleep(SYNC_INTERVAL);
				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}
			catch (Exception)
			{
				if (IsNetworkAvailable())
				{
					// ネットワークが利用可能なのに例外になった場合は、サーバーアドレスが間違っているか、サーバーが混んでいる可能性が高い
					throw new Exception("楽曲情報データベース同期サーバーに接続できませんでした。サーバーアドレスが間違っているか、サーバーが混んでいます。");
				}
				else
				{
					throw new Exception("楽曲情報データベース同期サーバーに接続できませんでした。インターネットが使えません。");
				}
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベース同期サーバーへの POST でエラーが発生したかどうか
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private Boolean SyncPostErrorExists(out String? errMessage)
		{
			try
			{
				String? status = _downloader.Download(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_POST_ERROR), Encoding.UTF8);
				if (String.IsNullOrEmpty(status))
				{
					throw new Exception("サーバーからの確認結果が空です。");
				}
				if (status[0] == '0')
				{
					if (!status.Contains(YlConstants.APP_GENERATION))
					{
						errMessage = "サーバーの互換性がありません。";
						return true;
					}
					errMessage = null;
					return false;
				}

				errMessage = status.Substring(1);
				return true;
			}
			catch (Exception excep)
			{
				throw new Exception("楽曲情報データベースへの送信結果を確認できませんでした。\n" + excep.Message);
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベース同期コマンド URL
		// --------------------------------------------------------------------
		private String SyncUrl(String mode)
		{
			return YukaListerModel.Instance.EnvModel.YlSettings.SyncServer + FILE_NAME_CP_MAIN + "?Mode=" + mode;
		}

		// --------------------------------------------------------------------
		// 同期データをサーバーへアップロード
		// ＜返値＞ アップロード件数合計
		// --------------------------------------------------------------------
		private Int32 UploadSyncData()
		{
			Debug.Assert(MainWindowViewModel != null, "UploadSyncData() no main window");
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード開始");
			using SyncDataExporter syncDataExporter = new();
			Int32 numTotalUploads = 0;

			for (MusicInfoTables i = 0; i < MusicInfoTables.__End__; i++)
			{
				// アップロードデータ準備
				(List<String> csvHead, List<List<String>> csvContents) = syncDataExporter.Export(i);
				if (csvContents.Count == 0)
				{
					continue;
				}

				// 一定数ずつアップロード
				_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード中... " + YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[(Int32)i]);
				for (Int32 j = 0; j < (csvContents.Count + SYNC_UPLOAD_BLOCK - 1) / SYNC_UPLOAD_BLOCK; j++)
				{
					List<List<String>> uploadContents = new();
					uploadContents.Add(csvHead);
					uploadContents.AddRange(csvContents.GetRange(j * SYNC_UPLOAD_BLOCK, Math.Min(SYNC_UPLOAD_BLOCK, csvContents.Count - j * SYNC_UPLOAD_BLOCK)));
					String uploadFolder = YlCommon.TempPath();
					Directory.CreateDirectory(uploadFolder);
					String uploadPath = uploadFolder + "\\" + YlConstants.MUSIC_INFO_DB_TABLE_NAMES[(Int32)i];
					CsvManager.SaveCsv(uploadPath, uploadContents, "\n", Encoding.UTF8);
					Dictionary<String, String> uploadFiles = new()
					{
						{ "File", uploadPath },
					};
					Dictionary<String, String?> postParams = new()
					{
						// HTML Name 属性
						{ "Mode", SYNC_MODE_NAME_UPLOAD_SYNC_DATA },
					};
					for (Int32 k = 1; k < uploadContents.Count; k++)
					{
						_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "データ：" + uploadContents[k][0]);
					}
					Post(postParams, uploadFiles);

					// アップロード結果確認
					String? errMessage;
					if (SyncPostErrorExists(out errMessage))
					{
						throw new Exception("同期データをアップロードできませんでした：" + errMessage);
					}

					// 状況
					numTotalUploads += uploadContents.Count - 1;
					MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データをアップロード中... 合計 " + numTotalUploads.ToString("#,0") + " 件");
					Thread.Sleep(SYNC_INTERVAL);
					YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
				}
			}
			DownloadRejectDate();
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード完了");
			return numTotalUploads;
		}
	}
}
