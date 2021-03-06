// ============================================================================
// 
// ネビュラコア：動画リスト作成担当
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
using System.Threading.Tasks;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.OutputWriters;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Kamlin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Kamlin()
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
		// ネビュラコア（動画リスト作成）のメインルーチン
		// --------------------------------------------------------------------
		protected override async Task CoreMain()
		{
			// リスト出力時の処理が重いことによるトラブルの可能性がゼロではないため、プライオリティーを下げる（チケット #97）
			Thread.CurrentThread.Priority = ThreadPriority.Lowest;

			while (true)
			{
				MainEvent.WaitOne();
				Debug.Assert(MainWindowViewModel != null, "Kamlin.CoreMain() MainWindowViewModel is null");
				Int32 startTick = Environment.TickCount;

				try
				{
					//Debug.WriteLine("Kamlin.CoreMain() priority: " + Thread.CurrentThread.Priority.ToString());
					YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
					if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
					{
						continue;
					}

					// リストデータベースのレコード数が 0 ならリスト作成不要
					using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> founds);
					if (!founds.Any())
					{
						continue;
					}

					YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Kamlin] = YukaListerStatus.Running;
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

					// 問題報告用に ID 接頭辞が必要
					try
					{
						await YlCommon.InputIdPrefixIfNeededWithInvoke(MainWindowViewModel);
					}
					catch (Exception)
					{
						// OperationCanceledException を通常の例外に変換
						throw new Exception("ID 接頭辞が設定されていません。");
					}

					// リスト出力
					YukariOutputWriter yukariOutputWriter = new();
					yukariOutputWriter.Output();

					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "リスト出力が完了しました。");
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

				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Kamlin] = YukaListerStatus.Ready;

				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
		}
	}
}
