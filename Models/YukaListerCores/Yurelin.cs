// ============================================================================
// 
// ネビュラコア：統計データ作成担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Yurelin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Yurelin()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// メインウィンドウ
		public MainWindowViewModel? MainWindowViewModel { get; set; }

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（負荷監視）のメインルーチン
		// --------------------------------------------------------------------
		protected override void CoreMain()
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

					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

					// データベースアクセス準備
					using YukariStatisticsContext yukariStatisticsContext = YukariStatisticsContext.CreateContext(out DbSet<TYukariStatistics> yukariStatistics);
					using YukariRequestContext yukariRequestContext = YukariRequestContext.CreateContext(out DbSet<TYukariRequest> yukariRequests);
					yukariRequestContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
					using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> founds);
					listContextInDisk.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
					UpdateLastYukariRequestClearTimeIfNeeded(yukariRequests);

					// 統計更新
					AnalyzeYukariRequests(yukariStatistics, yukariRequests, founds);
#if DEBUG
					Double lastWriteTimeBak = YukariStatisticsContext.LastWriteTime();
#endif
					yukariStatisticsContext.SaveChanges();
#if DEBUG
					Debug.WriteLine("Yurelin.CoreMain() ファイル更新 " + (lastWriteTimeBak == YukariStatisticsContext.LastWriteTime() ? "なし" : "有り"));
#endif
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
		// private メンバー変数
		// ====================================================================

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// request.db の 1 レコードを統計に追加
		// --------------------------------------------------------------------
		private void AddYukariRequest(DbSet<TYukariStatistics> yukariStatistics, TYukariRequest yukariRequest, DbSet<TFound> founds)
		{
			try
			{
				Debug.Assert(MainWindowViewModel != null, "Yurelin.AddYukariRequest() MainWindowViewModel is null");
				YlCommon.InputIdPrefixIfNeededWithInvoke(MainWindowViewModel);
			}
			catch (Exception)
			{
				// OperationCanceledException を通常の例外に変換
				throw new Exception("ID 接頭辞が設定されていません。");
			}
			TYukariStatistics yukariStatisticsRecord = new()
			{
				Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareYukariStatisticsLastId(yukariStatistics),
				Dirty = true,
				RequestDatabasePath = YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath(),
				RequestTime = YukariRequestContext.LastWriteTime(),
				RoomName = YukaListerModel.Instance.EnvModel.YlSettings.YukariRoomName,
			};

			CopyYukariRequestToYukariStatistics(yukariRequest, yukariStatisticsRecord);
			CopyFoundToYukariStatisticsIfNeeded(founds, yukariStatisticsRecord);
			yukariStatistics.Add(yukariStatisticsRecord);
			Debug.WriteLine("AddYukariRequest() 追加: " + yukariStatisticsRecord.RequestMoviePath);
		}

		// --------------------------------------------------------------------
		// request.db を解析してゆかり統計に反映
		// --------------------------------------------------------------------
		private void AnalyzeYukariRequests(DbSet<TYukariStatistics> yukariStatistics, DbSet<TYukariRequest> yukariRequests, DbSet<TFound> founds)
		{
			foreach (TYukariRequest yukariRequest in yukariRequests)
			{
				TYukariStatistics? existStatisticsRecord = ExistStatisticsRecord(yukariStatistics, yukariRequest);
				if (existStatisticsRecord == null)
				{
					AddYukariRequest(yukariStatistics, yukariRequest, founds);
				}
				else
				{
					UpdateExistStatisticsIfNeeded(existStatisticsRecord, yukariRequest, founds);
				}
			}
		}

		// --------------------------------------------------------------------
		// TFound → TYukariStatistics へコピー（属性確認済ではない場合のみ）
		// --------------------------------------------------------------------
		private void CopyFoundToYukariStatisticsIfNeeded(DbSet<TFound> founds, TYukariStatistics yukariStatistics)
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
			if (found.FileSize < 0)
			{
				Debug.WriteLine("CopyFoundToYukariStatisticsIfNeeded() 属性確認しようとしたがまだ整理されていない " + yukariStatistics.RequestMoviePath);
				return;
			}

			yukariStatistics.AttributesDone = true;
			yukariStatistics.Worker = found.Worker;
			yukariStatistics.SongReleaseDate = found.SongReleaseDate;
			yukariStatistics.Category = found.Category;
			yukariStatistics.TieUpName = found.TieUpName;
			yukariStatistics.TieUpAgeLimit = found.TieUpAgeLimit;
			yukariStatistics.MakerName = found.MakerName;
			yukariStatistics.TieUpGroupName = found.TieUpGroupName;
			yukariStatistics.SongName = found.SongName;
			yukariStatistics.SongOpEd = found.SongOpEd;
			yukariStatistics.ArtistName = found.ArtistName;
			yukariStatistics.LyristName = found.LyristName;
			yukariStatistics.ComposerName = found.ComposerName;
			yukariStatistics.ArrangerName = found.ArrangerName;
			Debug.WriteLine("CopyFoundToYukariStatisticsIfNeeded() 属性確認実施 " + yukariStatistics.RequestMoviePath);
		}

		// --------------------------------------------------------------------
		// TYukariRequest → TYukariStatistics へコピー
		// --------------------------------------------------------------------
		private void CopyYukariRequestToYukariStatistics(TYukariRequest yukariRequest, TYukariStatistics yukariStatistics)
		{
			//if (yukariStatistics.RequestId != yukariRequest.Id)
			{
				yukariStatistics.RequestId = yukariRequest.Id;
			}
			//if (yukariStatistics.RequestMoviePath != yukariRequest.Path)
			{
				yukariStatistics.RequestMoviePath = yukariRequest.Path;
			}
			//if (yukariStatistics.RequestSinger != yukariRequest.Singer)
			{
				yukariStatistics.RequestSinger = yukariRequest.Singer;
			}
			//if (yukariStatistics.RequestComment != yukariRequest.Comment)
			{
				yukariStatistics.RequestComment = yukariRequest.Comment;
			}
			//if (yukariStatistics.RequestOrder != yukariRequest.Order)
			{
				yukariStatistics.RequestOrder = yukariRequest.Order;
			}
			//if (yukariStatistics.RequestKeyChange != yukariRequest.KeyChange)
			{
				yukariStatistics.RequestKeyChange = yukariRequest.KeyChange;
			}
		}

		// --------------------------------------------------------------------
		// request.db の 1 レコードが既に統計に追加されていればその統計レコードを返す
		// --------------------------------------------------------------------
		private TYukariStatistics? ExistStatisticsRecord(DbSet<TYukariStatistics> yukariStatistics, TYukariRequest yukariRequest)
		{
			// request.db ファイル名、Id、Path、Singer のすべてが一致したものを既存レコードとする
			// かつ、推定予約日時が全消去検知日時以降のものを既存レコードとする
			return yukariStatistics.Where(x => x.RequestTime >= YukaListerModel.Instance.EnvModel.YlSettings.LastYukariRequestClearTime
					&& x.RequestId == yukariRequest.Id && x.RequestDatabasePath == YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath()
					&& x.RequestMoviePath == yukariRequest.Path && x.RequestSinger == yukariRequest.Singer).OrderByDescending(x => x.RequestTime).FirstOrDefault();
		}

		// --------------------------------------------------------------------
		// request.db 全消去日時を必要に応じて更新
		// --------------------------------------------------------------------
		private void UpdateLastYukariRequestClearTimeIfNeeded(DbSet<TYukariRequest> yukariRequests)
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
		private void UpdateExistStatisticsIfNeeded(TYukariStatistics existStatistics, TYukariRequest yukariRequest, DbSet<TFound> founds)
		{
			CopyYukariRequestToYukariStatistics(yukariRequest, existStatistics);
			CopyFoundToYukariStatisticsIfNeeded(founds, existStatistics);
		}

	}
}
