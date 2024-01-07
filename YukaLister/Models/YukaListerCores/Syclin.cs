// ============================================================================
// 
// ネビュラコア：同期担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using Shinta.Wpf;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.SyncClient;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.YukaListerCores
{
	internal class Syclin : YlCore
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
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
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（同期）のメインルーチン
		// --------------------------------------------------------------------
		protected override async Task CoreMainAsync()
		{
#if DEBUGz
			Debug.WriteLine("priority before: " + Thread.CurrentThread.Priority.ToString());
			Thread.CurrentThread.Priority = ThreadPriority.Lowest;
			Debug.WriteLine("priority after: " + Thread.CurrentThread.Priority.ToString());
#endif
			YlCommon.SetLogWriterSyncDetail(_logWriterSyncDetail);

			while (true)
			{
				MainEvent.WaitOne();
				Debug.Assert(MainWindowViewModel != null, "Syclin.CoreMain() MainWindowViewModel is null");
				Int32 startTick = Environment.TickCount;

				try
				{
					YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
					if (YlModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
					{
						continue;
					}
					if (!YlModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb)
					{
						continue;
					}

					IsActive = true;
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

					// ログイン
					MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期準備中...");
					await LoginToSyncServerAsync();

					// データベースをバックアップ
					using MusicInfoContextDefault musicInfoContextDefault = new();
					musicInfoContextDefault.BackupDatabase();
					using YukariStatisticsContext yukariStatisticsContext = new();
					yukariStatisticsContext.BackupDatabase();

					// 再取得の場合は楽曲情報データベース初期化
					CreateDatabaseIfNeeded(musicInfoContextDefault, yukariStatisticsContext);

					// ダウンロード
					(Int32 numTotalDownloads, Int32 numTotalImports) = await DownloadSyncDataAsync();

					// アップロード
					Int32 numTotalUploads = await UploadSyncDataAsync();
					if (numTotalUploads > 0)
					{
						// アップロードを行った場合は、自身がアップロードしたデータの更新日・Dirty を更新するために再ダウンロードが必要
						(numTotalDownloads, numTotalImports) = await DownloadSyncDataAsync();
					}

					// 完了表示
					MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "データベース同期完了（ダウンロード"
							+ (numTotalDownloads == 0 ? "無" : " " + numTotalDownloads.ToString("#,0") + " 件、うち " + numTotalImports.ToString("#,0") + " 件インポート")
							+ "、アップロード" + (numTotalUploads == 0 ? "無" : " " + numTotalUploads.ToString("#,0") + " 件") + "）");

					// 起動直後は Syclin と Yurelin が両方起動され、Syclin による属性未確認データのアップロード＆ダウンロードと、Yurelin による属性確認が競合し、属性未確認の状態に戻ることがある
					// Syclin スリープ化直前に Yurelin を起動し、再度属性確認が行われるようにする
					YlCommon.ActivateYurelinIfNeeded();
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception excep)
				{
					DbCommon.LogDatabaseExceptionIfCan(excep);

					// メッセージボックスではなくステータスバーにエラー表示
					MainWindowViewModel.SetStatusBarMessageWithInvoke(TraceEventType.Error, excep.Message);
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}

				IsActive = false;
				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
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
		// private 変数
		// ====================================================================

		// ダウンローダー
		private readonly Downloader _downloader = new();

		// 詳細ログ（同期専用）
		private readonly LogWriter _logWriterSyncDetail = new(YlConstants.APP_ID + YlConstants.SYNC_DETAIL_ID);

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 再取得の場合は楽曲情報データベース・ゆかり統計データベースを初期化
		// --------------------------------------------------------------------
		private void CreateDatabaseIfNeeded(MusicInfoContextDefault musicInfoContextDefault, YukariStatisticsContext yukariStatisticsContext)
		{
			if (!IsReget)
			{
				return;
			}

			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "サーバーデータ再取得のため楽曲情報データベースを初期化。");
			musicInfoContextDefault.CreateDatabase();

			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "サーバーデータ再取得のためゆかり統計データベースを初期化。");
			yukariStatisticsContext.CreateDatabase();

			IsReget = false;
		}

		// --------------------------------------------------------------------
		// アップロードを拒否されたレコードの更新日をサーバーからダウンロード
		// YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate を更新し、次回ダウンロード時に拒否レコードが上書きされるようにする
		// --------------------------------------------------------------------
		private async Task DownloadRejectDateAsync()
		{
			try
			{
				(_, String rejectDateString) = await _downloader.DownloadAsStringAsync(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_REJECT_DATE), Encoding.UTF8);
				if (String.IsNullOrEmpty(rejectDateString))
				{
					throw new Exception("サーバーからの確認結果が空です。");
				}

				DateTime rejectDate = DateTime.ParseExact(rejectDateString, YlConstants.SYNC_URL_DATE_FORMAT, null);
				Double rejectMjd = JulianDay.DateTimeToModifiedJulianDate(rejectDate);
				if (rejectMjd < YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate)
				{
					YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = rejectMjd;
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
		private async Task<(Int32 numTotalDownloads, Int32 numTotalImports)> DownloadSyncDataAsync()
		{
			Debug.Assert(MainWindowViewModel != null, "DownloadSyncData() no main window");

			// ダウンロード開始時刻の記録
			DateTime taskBeginDateTime = DateTime.UtcNow;
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ダウンロード開始");

			if (YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate < YlConstants.INVALID_MJD)
			{
				YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = YlConstants.INVALID_MJD;
			}
			DateTime targetDate = JulianDay.ModifiedJulianDateToDateTime(YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate);
			Int32 numTotalDownloads = 0;
			Int32 numTotalImports = 0;
			for (; ; )
			{
				MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データダウンロード中... ");
				YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = JulianDay.DateTimeToModifiedJulianDate(targetDate);

				// ダウンロード
				String downloadPath = Common.TempPath();
				await _downloader.DownloadAsFileAsync(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_SYNC_DATA) + "&Date=" + targetDate.ToString(YlConstants.SYNC_URL_DATE_FORMAT), downloadPath);

				FileInfo fileInfo = new(downloadPath);
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
				YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = JulianDay.DateTimeToModifiedJulianDate(taskBeginDateTime.Date);
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ダウンロード完了");
			return (numTotalDownloads, numTotalImports);
		}

		// --------------------------------------------------------------------
		// ネットワークが利用可能かどうか（簡易判定）
		// --------------------------------------------------------------------
		private async Task<Boolean> IsNetworkAvailableAsync()
		{
			try
			{
				await _downloader.DownloadAsStringAsync("https://www.google.com/", Encoding.UTF8);
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
		private async Task LoginToSyncServerAsync()
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
			Dictionary<String, String?> postParams = new()
			{
				// HTML Name 属性
				{ "Name", YlModel.Instance.EnvModel.YlSettings.SyncAccount },
				{ "PW", YlCommon.Decrypt(YlModel.Instance.EnvModel.YlSettings.SyncPassword) },
				{ "Mode", SYNC_MODE_NAME_LOGIN },
				{ "IdPrefix", YlModel.Instance.EnvModel.YlSettings.IdPrefix },
				{ "Sid", sid },
				{ "AppGeneration", YlConstants.APP_GENERATION },
				{ "AppVer", YlConstants.APP_VER },
			};
			MainWindowViewModel?.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "データベース同期サーバーにログインします...");
			await PostAsync(postParams);

			// ログイン結果確認
			(Boolean errorExists, String? errorMessage) = await SyncPostErrorExistsAsync();
			if (errorExists)
			{
				throw new Exception("データベース同期サーバーにログインできませんでした。" + errorMessage);
			}

			MainWindowViewModel?.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "データベース同期サーバーにログインしました。同期処理中です...");
			Thread.Sleep(SYNC_INTERVAL);
			YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
		}

		// --------------------------------------------------------------------
		// POST データ送信
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		private async Task PostAsync(Dictionary<String, String?> postParams, Dictionary<String, String>? files = null)
		{
			try
			{
				await _downloader.PostAndDownloadAsStringAsync(YlModel.Instance.EnvModel.YlSettings.SyncServer + FILE_NAME_CP_MAIN, Encoding.UTF8, postParams, files);
				Thread.Sleep(SYNC_INTERVAL);
				YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}
			catch (Exception)
			{
				if (await IsNetworkAvailableAsync())
				{
					// ネットワークが利用可能なのに例外になった場合は、サーバーアドレスが間違っているか、サーバーが混んでいる可能性が高い
					throw new Exception("データベース同期サーバーに接続できませんでした。サーバーアドレスが間違っているか、サーバーが混んでいます。");
				}
				else
				{
					throw new Exception("データベース同期サーバーに接続できませんでした。インターネットが使えません。");
				}
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベース同期サーバーへの POST でエラーが発生したかどうか
		// 本来は POST 時の返値を見れば良いが、過去との互換性のために、エラーを問い合わせる仕様となっている
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task<(Boolean errorExists, String? errorMessage)> SyncPostErrorExistsAsync()
		{
			try
			{
				(_, String status) = await _downloader.DownloadAsStringAsync(SyncUrl(SYNC_MODE_NAME_DOWNLOAD_POST_ERROR), Encoding.UTF8);
				if (String.IsNullOrEmpty(status))
				{
					throw new Exception("サーバーからの確認結果が空です。");
				}
				if (status[0] == '0')
				{
					if (!status.Contains(YlConstants.APP_GENERATION))
					{
						return (true, "サーバーの互換性がありません。");
					}
					return (false, null);
				}

				return (true, status[1..]);
			}
			catch (Exception excep)
			{
				throw new Exception("データベースへの送信結果を確認できませんでした。\n" + excep.Message);
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベース同期コマンド URL
		// --------------------------------------------------------------------
		private static String SyncUrl(String mode)
		{
			return YlModel.Instance.EnvModel.YlSettings.SyncServer + FILE_NAME_CP_MAIN + "?Mode=" + mode;
		}

		// --------------------------------------------------------------------
		// 同期データをサーバーへアップロード
		// ＜返値＞ アップロード件数合計
		// --------------------------------------------------------------------
		private async Task<Int32> UploadSyncDataAsync()
		{
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード開始");
			using SyncDataExporter syncDataExporter = new();
			Int32 numTotalUploads = 0;

			// 楽曲情報データベース
			for (MusicInfoTables i = 0; i < MusicInfoTables.__End__; i++)
			{
				// アップロードデータ準備
				(List<String> musicInfoCsvHead, List<List<String>> musicInfoCsvContents) = syncDataExporter.ExportMusicInfoDatabase(i);
				if (musicInfoCsvContents.Count == 0)
				{
					continue;
				}

				// アップロード
				_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード中... " + YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[(Int32)i]);
				numTotalUploads += await UploadSyncDataCoreAsync(YlConstants.MUSIC_INFO_DB_TABLE_NAMES[(Int32)i], musicInfoCsvHead, musicInfoCsvContents);
			}

			// ゆかり統計データベース
			(List<String> yukariStatisticsCsvHead, List<List<String>> yukariStatisticsCsvContents) = syncDataExporter.ExportYukariStatisticsDatabase();
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード中... ゆかり統計");
			numTotalUploads += await UploadSyncDataCoreAsync(TYukariStatistics.TABLE_NAME_YUKARI_STATISTICS, yukariStatisticsCsvHead, yukariStatisticsCsvContents);

			await DownloadRejectDateAsync();
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "アップロード完了");
			return numTotalUploads;
		}

		// --------------------------------------------------------------------
		// 同期データをサーバーへアップロード
		// ＜返値＞ アップロード件数合計
		// --------------------------------------------------------------------
		private async Task<Int32> UploadSyncDataCoreAsync(String tableName, List<String> csvHead, List<List<String>> csvContents)
		{
			Debug.Assert(MainWindowViewModel != null, "UploadSyncDataCore() no main window");

			Int32 numTotalUploads = 0;

			// 一定数ずつアップロード
			for (Int32 j = 0; j < (csvContents.Count + SYNC_UPLOAD_BLOCK - 1) / SYNC_UPLOAD_BLOCK; j++)
			{
				List<List<String>> uploadContents = new();
				uploadContents.Add(csvHead);
				uploadContents.AddRange(csvContents.GetRange(j * SYNC_UPLOAD_BLOCK, Math.Min(SYNC_UPLOAD_BLOCK, csvContents.Count - j * SYNC_UPLOAD_BLOCK)));
				String uploadFolder = Common.TempPath();
				Directory.CreateDirectory(uploadFolder);
				String uploadPath = uploadFolder + "\\" + tableName;
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
				await PostAsync(postParams, uploadFiles);

				// アップロード結果確認
				(Boolean errorExists, String? errorMessage) = await SyncPostErrorExistsAsync();
				if (errorExists)
				{
					throw new Exception("同期データをアップロードできませんでした：" + errorMessage);
				}

				// 状況
				numTotalUploads += uploadContents.Count - 1;
				MainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データをアップロード中... 合計 " + numTotalUploads.ToString("#,0") + " 件");
				Thread.Sleep(SYNC_INTERVAL);
				YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			return numTotalUploads;
		}
	}
}
