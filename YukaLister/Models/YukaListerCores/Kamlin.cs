﻿// ============================================================================
// 
// ネビュラコア：動画リスト作成担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.OutputWriters;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.YukaListerCores
{
	internal class Kamlin : YlCore
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
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
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（動画リスト作成）のメインルーチン
		// --------------------------------------------------------------------
		protected override async Task CoreMainAsync()
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
					YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();

					// 全体の動作状況がエラーの場合はリスト作成しない
					if (YlModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
					{
						continue;
					}

					// リストデータベースのレコード数が 0 ならリスト作成不要
					using ListContextInDisk listContextInDisk = new();
					if (!listContextInDisk.Founds.Any())
					{
						continue;
					}

					YlModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Kamlin] = YukaListerStatus.Running;
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

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

					YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "リスト出力が完了しました。");
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception ex)
				{
					YlModel.Instance.EnvModel.NebulaCoreErrors.Enqueue(GetType().Name + " ループ稼働時エラー：\n" + ex.Message);
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
				}

				YlModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Kamlin] = YukaListerStatus.Ready;

				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
		}
	}
}
