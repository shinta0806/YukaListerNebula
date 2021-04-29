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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（同期）のメインルーチン
		// --------------------------------------------------------------------
		protected override void CoreMain()
		{
			while (true)
			{
				MainEvent.WaitOne();
				if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
				{
					continue;
				}

				Int32 startTick = Environment.TickCount;
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

				try
				{
					YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();

					// ログイン
					LoginToSyncServer();

					// データベースをバックアップ
					MusicInfoContext.BackupDatabase();

					// 再取得の場合は楽曲情報データベース初期化
					//CreateMusicInfoDbIfNeeded();

#if false
					// ダウンロード
					(Int32 numTotalDownloads, Int32 numTotalImports) = DownloadSyncData();

					// アップロード
					Int32 aNumTotalUploads;
					UploadSyncData(out aNumTotalUploads);
					if (aNumTotalUploads > 0)
					{
						// アップロードを行った場合は、自身がアップロードしたデータの更新日・Dirty を更新するために再ダウンロードが必要
						DownloadSyncData(out numTotalDownloads, out numTotalImports);
					}

					// 完了表示
					mMainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベース同期完了（ダウンロード"
							+ (numTotalDownloads == 0 ? "無" : " " + numTotalDownloads.ToString("#,0") + " 件、うち " + numTotalImports.ToString("#,0") + " 件インポート")
							+ "、アップロード" + (aNumTotalUploads == 0 ? "無" : " " + aNumTotalUploads.ToString("#,0") + " 件") + "）");
#endif
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception excep)
				{
					// メッセージボックスではなくステータスバーにエラー表示
					MainWindowViewModel?.SetStatusBarMessageWithInvoke(TraceEventType.Error, excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}

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
		//private const String FILE_NAME_CP_LOGIN = "CPLogin" + Common.FILE_EXT_PHP;
		private const String FILE_NAME_CP_MAIN = "CPMain" + Common.FILE_EXT_PHP;
		//private const String FILE_NAME_SYNC_DETAIL_LOG = YlConstants.APP_ID + "SyncDetail" + Common.FILE_EXT_LOG;
		//private const String FILE_NAME_SYNC_INFO = "SyncInfo" + Common.FILE_EXT_TXT;
		//private const String FILE_NAME_SYNC_LOG = YlConstants.APP_ID + "Sync" + Common.FILE_EXT_LOG;

		// 同期モード
		private const String SYNC_MODE_NAME_DOWNLOAD_POST_ERROR = "DownloadPostError";
		//private const String SYNC_MODE_NAME_DOWNLOAD_REJECT_DATE = "DownloadRejectDate";
		private const String SYNC_MODE_NAME_DOWNLOAD_SYNC_DATA = "DownloadSyncData";
		private const String SYNC_MODE_NAME_LOGIN = "Login";
		//private const String SYNC_MODE_NAME_UPLOAD_SYNC_DATA = "UploadSyncData";

		// FILE_NAME_SYNC_INFO の中のパラメーター
		//private const String SYNC_INFO_PARAM_DATE = "Date";

		// その他
		//private const Int32 IMPORT_PROGRESS_BLOCK = 1000;
		private const Int32 SYNC_INTERVAL = 200;
		private const String SYNC_NO_DATA = "NoData";
		//private const Int32 SYNC_UPLOAD_BLOCK = 100;
		private const String SYNC_URL_DATE_FORMAT = "yyyyMMdd";

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// ダウンローダー
		private Downloader _downloader = new();

		// サーバーデータ再取得
		private Boolean _isReget;

		// ログ（同期専用）
		//private LogWriter _logWriterSync = new(YlConstants.APP_ID + "Sync");

		// 詳細ログ（同期専用）
		//private LogWriter _logWriterSyncDetail = new(YlConstants.APP_ID + "SyncDetail");

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

#if false
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
				_downloader.Download(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_SYNC_DATA) + "&Date=" + targetDate.ToString(SYNC_URL_DATE_FORMAT), downloadPath);

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
				ZipFile.ExtractToDirectory(downloadPath, extractFolder);

				// インポート
				using SyncDataImporter syncDataImporter = new(MainWindowViewModel, extractFolder);
				syncDataImporter.Import(ref numTotalDownloads, ref numTotalImports);

				// 日付更新
				targetDate = DateTime.ParseExact(syncInfos[SYNC_INFO_PARAM_DATE], SYNC_URL_DATE_FORMAT, null).AddDays(1);
				if (targetDate > DateTime.UtcNow.Date)
				{
					// 今日を超えたら抜ける（mEnvironment.YukaListerSettings.LastSyncDownloadDate は今日の日付のままにしておく）
					break;
				}

				Thread.Sleep(SYNC_INTERVAL);
				mEnvironment.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			mEnvironment.YukaListerSettings.LastSyncDownloadDate = JulianDay.DateTimeToModifiedJulianDate(taskBeginDateTime.Date);
		}
#endif

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
			if (SyncPostErrorExists(out _))
			{
				throw new Exception("楽曲情報データベース同期サーバーにログインできませんでした。");
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


	}
}
