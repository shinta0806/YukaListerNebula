// ============================================================================
// 
// ネビュラコア：統計データ作成担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ゆかり request.db に現在存在している予約からのみ統計データを作成・更新する
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Yurelin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public Yurelin()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// メインウィンドウ
		public MainWindowViewModel? MainWindowViewModel { get; set; }

		// 過去の統計データ（request.db にない予約）を更新するか
		public UpdatePastYukariStatisticsKind UpdatePastYukariStatisticsKind { get; set; }

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（統計データ作成）のメインルーチン
		// --------------------------------------------------------------------
		protected override async Task CoreMainAsync()
		{
			// 急ぎではないのでプライオリティーを下げる
			Thread.CurrentThread.Priority = ThreadPriority.Lowest;

			while (true)
			{
				MainEvent.WaitOne();
				Int32 startTick = Environment.TickCount;

				try
				{
					YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
					if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
					{
						continue;
					}
					if (!YukaListerModel.Instance.EnvModel.YlSettings.IsYukariRequestDatabaseValid())
					{
						continue;
					}

					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

					// データベースアクセス準備
					using YukariStatisticsContext yukariStatisticsContext = new();
					using YukariRequestContext yukariRequestContext = new();
					yukariRequestContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
					using ListContextInDisk listContextInDisk = new();
					listContextInDisk.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
					UpdateLastYukariRequestClearTimeIfNeeded(yukariRequestContext.YukariRequests);

					// request.db にある予約の統計更新
					await AnalyzeYukariRequests(yukariStatisticsContext.YukariStatistics, yukariRequestContext.YukariRequests, listContextInDisk.Founds);
#if DEBUG
					Boolean hasChangesBak = yukariStatisticsContext.ChangeTracker.HasChanges();
					Double lastWriteTimeBak = yukariStatisticsContext.LastWriteMjd();
#endif
					if (yukariStatisticsContext.ChangeTracker.HasChanges())
					{
						yukariStatisticsContext.BackupDatabase();
					}
					yukariStatisticsContext.SaveChanges();
#if DEBUG
					Debug.WriteLine("Yurelin.CoreMain() 更新フラグ " + hasChangesBak.ToString());
					Debug.WriteLine("Yurelin.CoreMain() 実際のファイル更新 " + (lastWriteTimeBak == yukariStatisticsContext.LastWriteMjd() ? "なし" : "有り"));
					Debug.Assert(hasChangesBak == (lastWriteTimeBak != yukariStatisticsContext.LastWriteMjd()), "Yurelin.CoreMain() フラグが実際と異なった Bak: "
							+ JulianDay.ModifiedJulianDateToDateTime(lastWriteTimeBak).ToString(YlConstants.TIME_FORMAT)
							+ ", 現在: " + JulianDay.ModifiedJulianDateToDateTime(yukariStatisticsContext.LastWriteMjd()).ToString(YlConstants.TIME_FORMAT));
#endif

					if (UpdatePastYukariStatisticsKind != UpdatePastYukariStatisticsKind.None)
					{
						// 過去の統計（request.db にないものを含む）更新
						UpdatePast();
					}
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, GetType().Name + " ループ稼働時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}

				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
		}

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// request.db の 1 レコードを統計に追加
		// --------------------------------------------------------------------
		private async Task AddYukariRequest(DbSet<TYukariStatistics> yukariStatistics, TYukariRequest yukariRequest, DbSet<TFound> founds)
		{
			try
			{
				Debug.Assert(MainWindowViewModel != null, "AddYukariRequest() MainWindowViewModel is null");
				await YlCommon.InputIdPrefixIfNeededWithInvoke(MainWindowViewModel);
			}
			catch (Exception)
			{
				// OperationCanceledException を通常の例外に変換
				throw new Exception("ID 接頭辞が設定されていません。");
			}
			Debug.Assert(YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix != null, "AddYukariRequest() IdPrefix is null");
			TYukariStatistics yukariStatisticsRecord = new()
			{
				Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareYukariStatisticsLastId(yukariStatistics),
				Dirty = true,
				RequestDatabasePath = YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath(),
				RequestTime = YukariRequestContext.LastWriteMjd(),
				RoomName = YukaListerModel.Instance.EnvModel.YlSettings.YukariRoomName,
				//IdPrefix = YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix,
			};

			CopyYukariRequestToYukariStatistics(yukariRequest, yukariStatisticsRecord);
			CopyFoundToYukariStatisticsIfNeeded(founds, yukariStatisticsRecord);
			yukariStatistics.Add(yukariStatisticsRecord);
			Debug.WriteLine("AddYukariRequest() 追加: " + yukariStatisticsRecord.RequestMoviePath);
		}

		// --------------------------------------------------------------------
		// request.db を解析してゆかり統計に反映
		// --------------------------------------------------------------------
		private async Task AnalyzeYukariRequests(DbSet<TYukariStatistics> yukariStatistics, DbSet<TYukariRequest> yukariRequests, DbSet<TFound> founds)
		{
			foreach (TYukariRequest yukariRequest in yukariRequests)
			{
				TYukariStatistics? existStatisticsRecord = ExistStatisticsRecord(yukariStatistics, yukariRequest);
				if (existStatisticsRecord == null)
				{
					await AddYukariRequest(yukariStatistics, yukariRequest, founds);
				}
				else
				{
					UpdateExistStatisticsIfNeeded(existStatisticsRecord, yukariRequest, founds);
				}
			}
		}

		// --------------------------------------------------------------------
		// TFound → TYukariStatistics へコピー（ゆかり統計が属性確認済ではない場合のみ）
		// --------------------------------------------------------------------
		private static void CopyFoundToYukariStatisticsIfNeeded(DbSet<TFound> founds, TYukariStatistics yukariStatistics)
		{
			if (yukariStatistics.AttributesDone)
			{
				return;
			}

			TFound? found = founds.FirstOrDefault(x => x.Path == yukariStatistics.RequestMoviePath);
			if (found == null)
			{
				Debug.WriteLine("CopyFoundToYukariStatisticsIfNeeded() 属性確認しようとしたが見つからない " + yukariStatistics.RequestMoviePath);
				return;
			}
			DbCommon.CopyFoundToYukariStatisticsIfAttributesPrepared(found, yukariStatistics);
		}

		// --------------------------------------------------------------------
		// TYukariRequest → TYukariStatistics へコピー
		// --------------------------------------------------------------------
		private static void CopyYukariRequestToYukariStatistics(TYukariRequest yukariRequest, TYukariStatistics yukariStatistics)
		{
			// EF Core では、代入しても実際の値が更新されていなければ更新と判定されない（無駄な保存が発生しない）模様なので、プログラムでは更新チェックはせずに常に代入する
			// 途中で変わるものについては、変わったら Dirty フラグを立てる必要がある
			yukariStatistics.RequestId = yukariRequest.Id;
			yukariStatistics.RequestMoviePath = yukariRequest.Path;
			yukariStatistics.RequestSinger = yukariRequest.Singer;

			yukariStatistics.Dirty |= yukariStatistics.RequestComment != yukariRequest.Comment;
			yukariStatistics.RequestComment = yukariRequest.Comment;

			yukariStatistics.Dirty |= yukariStatistics.RequestOrder != yukariRequest.Order;
			yukariStatistics.RequestOrder = yukariRequest.Order;

			yukariStatistics.Dirty |= yukariStatistics.RequestKeyChange != yukariRequest.KeyChange;
			yukariStatistics.RequestKeyChange = yukariRequest.KeyChange;
		}

		// --------------------------------------------------------------------
		// request.db の 1 レコードが既に統計に追加されていればその統計レコードを返す
		// --------------------------------------------------------------------
		private static TYukariStatistics? ExistStatisticsRecord(DbSet<TYukariStatistics> yukariStatistics, TYukariRequest yukariRequest)
		{
			// request.db ファイル名、ゆかり予約 Id、Path、Singer のすべてが一致したものを既存レコードとする
			// かつ、この PC で追加したレコード（ID 接頭辞の先頭が一致するレコード）を既存レコードとする
			// かつ、推定予約日時が全消去検知日時以降のものを既存レコードとする
			// ルーム名は途中で変更されることがあるので判定に使用しない
			return yukariStatistics.Where(x => x.RequestTime >= YukaListerModel.Instance.EnvModel.YlSettings.LastYukariRequestClearTime
					&& x.RequestId == yukariRequest.Id && x.RequestDatabasePath == YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath()
					&& x.RequestMoviePath == yukariRequest.Path && x.RequestSinger == yukariRequest.Singer
					&& EF.Functions.Like(x.Id, $"{YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix}%")).OrderByDescending(x => x.RequestTime).FirstOrDefault();
		}

		// --------------------------------------------------------------------
		// request.db 全消去日時を必要に応じて更新
		// --------------------------------------------------------------------
		private static void UpdateLastYukariRequestClearTimeIfNeeded(DbSet<TYukariRequest> yukariRequests)
		{
			if (yukariRequests.Any())
			{
				return;
			}

			// タイミングによっては、request.db 更新検知後 UPDATE_YUKARI_STATISTICS_DELAY_TIME ミリ秒経ってから Yurelin がアクティブ化されるため、
			// 安全マージンを取って UPDATE_YUKARI_STATISTICS_DELAY_TIME ミリ秒前に全消去を検知したことにする
			DateTime utc = DateTime.UtcNow;
			utc = utc.AddMilliseconds(-YlConstants.UPDATE_YUKARI_STATISTICS_DELAY_TIME);

			Debug.WriteLine("UpdateLastYukariRequestClearTimeIfNeeded() 全消去を検知 " + utc.ToString(YlConstants.DATE_FORMAT + " " + YlConstants.TIME_FORMAT));
			YukaListerModel.Instance.EnvModel.YlSettings.LastYukariRequestClearTime = JulianDay.DateTimeToModifiedJulianDate(utc);
			YukaListerModel.Instance.EnvModel.YlSettings.Save();
		}

		// --------------------------------------------------------------------
		// 統計を必要に応じて更新
		// --------------------------------------------------------------------
		private static void UpdateExistStatisticsIfNeeded(TYukariStatistics existStatistics, TYukariRequest yukariRequest, DbSet<TFound> founds)
		{
			CopyYukariRequestToYukariStatistics(yukariRequest, existStatistics);
			CopyFoundToYukariStatisticsIfNeeded(founds, existStatistics);
		}

		// --------------------------------------------------------------------
		// 過去の統計を更新
		// --------------------------------------------------------------------
		private void UpdatePast()
		{
			Debug.WriteLine("UpdatePast()");
			using YukariStatisticsContext yukariStatisticsContext = new();
			IQueryable<TYukariStatistics> targetYukariStatistics = yukariStatisticsContext.YukariStatistics.Where(x => (UpdatePastYukariStatisticsKind == UpdatePastYukariStatisticsKind.All || !x.AttributesDone) && !x.Invalid);
			foreach (TYukariStatistics oneStatistics in targetYukariStatistics)
			{
#if DEBUGz
				if (oneStatistics.RequestMoviePath == @"D:\TempD\TestNkl\TST9_楽曲情報編集\テストタイアップ1_テスト楽曲101_ファイル歌手1,初音ミク.mp4")
				{
				}
#endif
				if (!File.Exists(oneStatistics.RequestMoviePath))
				{
					continue;
				}
				String requestMovieFolder = Path.GetDirectoryName(oneStatistics.RequestMoviePath) ?? String.Empty;
				if (YlCommon.FindSettingsFolder(requestMovieFolder) == null)
				{
					continue;
				}
				Debug.WriteLine("UpdatePast() 設定：" + oneStatistics.RequestMoviePath);

				// TFound 下準備
				FileInfo fileInfo = new(oneStatistics.RequestMoviePath);
				TFound found = new()
				{
					Path = oneStatistics.RequestMoviePath,
					Folder = requestMovieFolder,
					ParentFolder = requestMovieFolder,
					LastWriteTime = JulianDay.DateTimeToModifiedJulianDate(fileInfo.LastWriteTime),
					FileSize = fileInfo.Length,
				};

				// フォルダー設定を読み込む
				FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(found.Folder);
				FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);

				using ListContextInMemory listContextInMemory = new();
				using TFoundSetter foundSetter = new(listContextInMemory);

				// TFound 設定
				foundSetter.SetTFoundValues(found, folderSettingsInMemory);

				// 統計設定
				DbCommon.CopyFoundToYukariStatisticsIfAttributesPrepared(found, oneStatistics);
			}

			yukariStatisticsContext.SaveChanges();
			UpdatePastYukariStatisticsKind = UpdatePastYukariStatisticsKind.None;
		}
	}
}
