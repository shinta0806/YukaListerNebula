// ============================================================================
// 
// ネビュラコア：負荷監視担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 全プロセスの CPU 負荷を記録するのが理想だが、測定に 10 秒以上かかるため、特定プロセスのみを記録する
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// PerformanceCounter について、実測値からの推測
// ・"Processor"-"% Processor Time"-"_Total" は全コアに対する CPU 使用率を返す
//   例）8C16T のマシンで 1 スレッドが 100% 負荷の場合、6.25 が返る
// ・"Process"-"% Processor Time" シリーズは 1 CPU に対する CPU 使用率を返す
//   例）8C16T のマシンで 1 スレッドが 100% 負荷の場合、100.00 が返る
//       スレッド数で割るとタスクマネージャーの値より少し少なめに出る印象
// ・"Process"-"% Processor Time"-"_Total" には "Idle" も含まれている
//   例）8C16T のマシンは常に 1600.00 に近い数字が返る
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Lomolin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Lomolin()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 測定対象ディスクのドライブレター群（カンマ区切り）
		public String TargetDrives { get; set; } = "C";

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（負荷監視）のメインルーチン
		// --------------------------------------------------------------------
		protected override Task CoreMain()
		{
			// 急ぎではないのでプライオリティーを下げるが、Lowest だとビジー状態で進まないかもしれないので、一段だけ下げる
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

			SetLogWriterMonitor();
			PrepareCounters();

			while (true)
			{
				try
				{
					Int32 startTime = Environment.TickCount;

					// CPU（全体）
					_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "-----");
					if (_cpuAllCounter != null)
					{
						LogOne(PerformanceCounterKind.CpuAll, "CPU,", "All", _cpuAllCounter);
					}

					// CPU（プロセス別）
					AddRemoveCpuCounters();
					LogCpuCounters();

					// ディスク
					if (TargetDrives != _prevTargetDrives)
					{
						AddRemoveDiskCounters();
					}
					LogDiskCounters();

					_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "負荷計測所要時間 [ms]：" + (Environment.TickCount - startTime).ToString());
					while (startTime + INTERVAL > Environment.TickCount)
					{
						YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
						Thread.Sleep(1000);
					}
				}
				catch (OperationCanceledException)
				{
					return Task.CompletedTask;
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, GetType().Name + " ループ稼働時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
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
				_cpuAllCounter?.Dispose();
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// カテゴリー名
		private const String CATEGORY_NAME_LOGICAL_DISK = "LogicalDisk";
		private const String CATEGORY_NAME_PROCESS = "Process";
		private const String CATEGORY_NAME_PROCESSOR = "Processor";

		// カウンター名
		private const String COUNTER_NAME_PERCENT_DISK_TIME = "% Disk Time";
		private const String COUNTER_NAME_PERCENT_PROCESSOR_TIME = "% Processor Time";
		private const String COUNTER_NAME_WORKING_SET_PRIVATE = "Working Set - Private";

		// インスタンス名
		private const String INSTANCE_NAME_EVERYTHING = "Everything";
		private const String INSTANCE_NAME_OWNCLOUD = "owncloud";
		private const String INSTANCE_NAME_TOTAL = "_Total";
		private const String INSTANCE_NAME_WINDOWS_DEFENDER = "MsMpEng"; /* Antimalware Service Executable */

		// 負荷監視ログファイル名
		private const String FILE_NAME_MONITOR_LOG = YlConstants.APP_ID + YlConstants.MONITOR_ID + Common.FILE_EXT_LOG;

		// 記録対象
		private readonly String[] TARGET_INSTANCES = { YlConstants.APP_ID, "mpc", "httpd" /* Apache */, INSTANCE_NAME_EVERYTHING, INSTANCE_NAME_OWNCLOUD,
				INSTANCE_NAME_WINDOWS_DEFENDER, "avp" /* Kaspersky */, "coreServiceShell" /* ウイルスバスター */, "ccSvcHst" /* ノートン */ };

		// 測定間隔 [ms]
		private const Int32 INTERVAL = 10 * 1000;

		// 高負荷の閾値 [%]
		private const Single HIGH_LOAD_THRESHOLD = 70.0f;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// 警告関連
		// --------------------------------------------------------------------

		// Everything の警告を行った
		private Boolean _everythingWarned;

		// ownCloud の警告を行った
		private Boolean _ownCloudWarned;

		// Windows Defender の警告を行った
		private Boolean _defenderWarned;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// ドライブ設定時のドライブ群
		private String _prevTargetDrives = String.Empty;

		// PC 全体の CPU 負荷
		private PerformanceCounter? _cpuAllCounter;

		// プロセスごとの CPU 負荷
		private Dictionary<String, PerformanceCounter> _cpuCounters = new();

		// ドライブごとのディスク負荷
		private Dictionary<String, PerformanceCounter> _diskCounters = new();

		// 詳細ログ（負荷監視専用）
		private readonly LogWriter _logWriterMonitor = new(YlConstants.APP_ID + YlConstants.MONITOR_ID);

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// 存在しないカウンターを追加
		// --------------------------------------------------------------------
		private void AddCounters(Dictionary<String, PerformanceCounter> counters, String categoryName, String counterName, String[] instanceNames, String[] targetInstanceNames)
		{
			IEnumerable<String> noExists = instanceNames.Where(x => !counters.ContainsKey(x));
			foreach (String noExist in noExists)
			{
				if (targetInstanceNames.FirstOrDefault(x => noExist.Contains(x, StringComparison.OrdinalIgnoreCase)) == null)
				{
					// 対象外
					continue;
				}

				PerformanceCounter? counter = CreateCounter(categoryName, counterName, noExist);
				if (counter != null)
				{
					counters[noExist] = counter;
					_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "カウンター追加：" + noExist);
				}
			}
		}

		// --------------------------------------------------------------------
		// カウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveCountersCore(Dictionary<String, PerformanceCounter> counters, String categoryName, String counterName, String[] targetInstanceNames)
		{
			PerformanceCounterCategory category = PerformanceCounterCategory.GetCategories().Single(x => x.CategoryName == categoryName);
			String[] instanceNames = category.GetInstanceNames();
			RemoveCounters(counters, instanceNames);
			AddCounters(counters, categoryName, counterName, instanceNames, targetInstanceNames);
		}

		// --------------------------------------------------------------------
		// CPU 負荷カウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveCpuCounters()
		{
			AddRemoveCountersCore(_cpuCounters, CATEGORY_NAME_PROCESS, COUNTER_NAME_PERCENT_PROCESSOR_TIME, TARGET_INSTANCES);
		}

		// --------------------------------------------------------------------
		// ディスク負荷カウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveDiskCounters()
		{
			AddRemoveCountersCore(_diskCounters, CATEGORY_NAME_LOGICAL_DISK, COUNTER_NAME_PERCENT_DISK_TIME, TargetDrives.Split(','));
			_prevTargetDrives = TargetDrives;
		}

		// --------------------------------------------------------------------
		// カウンター作成
		// --------------------------------------------------------------------
		private PerformanceCounter? CreateCounter(String categoryName, String counterName, String instanceName)
		{
			try
			{
				if (!PerformanceCounterCategory.Exists(categoryName))
				{
					throw new Exception("カウンターカテゴリーが存在しません：" + categoryName);
				}
				if (!PerformanceCounterCategory.CounterExists(counterName, categoryName))
				{
					throw new Exception("カウンターが存在しません：" + counterName + "、" + categoryName);
				}

				return new PerformanceCounter(categoryName, counterName, instanceName);
			}
			catch (Exception excep)
			{
				_logWriterMonitor.LogMessage(TraceEventType.Error, "カウンター作成時エラー：" + excep.Message);
				return null;
			}
		}

		// --------------------------------------------------------------------
		// 負荷を記録
		// --------------------------------------------------------------------
		private void LogCountersCore(PerformanceCounterKind kind, String kindLabel, Dictionary<String, PerformanceCounter> counters)
		{
			foreach (KeyValuePair<String, PerformanceCounter> kvp in counters)
			{
				LogOne(kind, kindLabel, kvp.Key, kvp.Value);
			}
		}

		// --------------------------------------------------------------------
		// CPU 負荷を記録
		// --------------------------------------------------------------------
		private void LogCpuCounters()
		{
			LogCountersCore(PerformanceCounterKind.CpuProcess, "CPU,", _cpuCounters);
		}

		// --------------------------------------------------------------------
		// ディスク負荷を記録
		// --------------------------------------------------------------------
		private void LogDiskCounters()
		{
			LogCountersCore(PerformanceCounterKind.Disk, "Dsk,", _diskCounters);
		}

		// --------------------------------------------------------------------
		// 1 行分記録
		// --------------------------------------------------------------------
		private void LogOne(PerformanceCounterKind kind, String kindLabel, String instanceName, PerformanceCounter counter)
		{
			try
			{
				switch (kind)
				{
					case PerformanceCounterKind.CpuAll:
					case PerformanceCounterKind.Disk:
						_logWriterMonitor.LogMessage(TraceEventType.Information, kindLabel + instanceName + "," + ((Int32)counter.NextValue()).ToString());
						break;
					case PerformanceCounterKind.CpuProcess:
						Single loadAsOneCpu = counter.NextValue();
						_logWriterMonitor.LogMessage(TraceEventType.Information, kindLabel + instanceName + "," + ((Int32)(loadAsOneCpu / Environment.ProcessorCount)).ToString());
						WarnCpuIfNeeded(instanceName, loadAsOneCpu);
						break;
					default:
						Debug.Assert(false, "LogOne() bad kind");
						break;
				}
			}
			catch (Exception excep)
			{
				// カウンターが消滅した場合は例外となる模様
				_logWriterMonitor.LogMessage(TraceEventType.Error, "カウンター値記録時エラー：" + excep.Message);
			}
		}

		// --------------------------------------------------------------------
		// カウンター群の準備
		// --------------------------------------------------------------------
		private void PrepareCounters()
		{
			_cpuAllCounter = CreateCounter(CATEGORY_NAME_PROCESSOR, COUNTER_NAME_PERCENT_PROCESSOR_TIME, INSTANCE_NAME_TOTAL);
		}

		// --------------------------------------------------------------------
		// 存在しなくなったカウンターを削除
		// --------------------------------------------------------------------
		private void RemoveCounters(Dictionary<String, PerformanceCounter> counters, String[] instanceNames)
		{
			IEnumerable<KeyValuePair<String, PerformanceCounter>> removes = counters.Where(x => !instanceNames.Contains(x.Key));
			foreach (KeyValuePair<String, PerformanceCounter> kvp in removes)
			{
				_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "カウンター削除：" + kvp.Key);
				kvp.Value.Dispose();
				counters.Remove(kvp.Key);
			}
		}

		// --------------------------------------------------------------------
		// ログ設定
		// --------------------------------------------------------------------
		private void SetLogWriterMonitor()
		{
			// 大量のログが発生するため、サイズを拡大
			_logWriterMonitor.ApplicationQuitToken = YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token;
			_logWriterMonitor.SimpleTraceListener.MaxSize = 5 * 1024 * 1024;
			_logWriterMonitor.SimpleTraceListener.LogFileName = Path.GetDirectoryName(_logWriterMonitor.SimpleTraceListener.LogFileName) + "\\" + FILE_NAME_MONITOR_LOG;
			_logWriterMonitor.SimpleTraceListener.Quote = false;

			_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "稼働開始 " + Environment.ProcessorCount + " スレッド ====================");
		}

		// --------------------------------------------------------------------
		// CPU 負荷に関する警告を作成する
		// 表示は UI スレッドが行う（メッセージボックス表示中も Lomolin は引き続き動作できるように）
		// --------------------------------------------------------------------
		private void WarnCpuIfNeeded(String instanceName, Single loadAsOneCpu)
		{
			// 存在を検知したら出す警告
			if (!_ownCloudWarned && instanceName.Contains(INSTANCE_NAME_OWNCLOUD, StringComparison.OrdinalIgnoreCase))
			{
				_ownCloudWarned = YukaListerModel.Instance.EnvModel.NebulaCoreErrors.TryAdd("ownCloud が動作しています。\n\n"
						+ "ゆかり・" + YlConstants.APP_NAME_J + "動作中は ownCloud を終了することを推奨します。");
			}
			if (!_everythingWarned && instanceName.Contains(INSTANCE_NAME_EVERYTHING, StringComparison.OrdinalIgnoreCase))
			{
				_everythingWarned = YukaListerModel.Instance.EnvModel.NebulaCoreErrors.TryAdd("Everything が動作しています。\n\n"
						+ "ゆかりの動作に Everything は不要となりましたので、Everything をアンインストールすることを推奨します。");
			}

			// 高負荷を検知したら出す警告
			if (!_defenderWarned && loadAsOneCpu >= HIGH_LOAD_THRESHOLD && instanceName.Contains(INSTANCE_NAME_WINDOWS_DEFENDER, StringComparison.OrdinalIgnoreCase))
			{
				_defenderWarned = YukaListerModel.Instance.EnvModel.NebulaCoreErrors.TryAdd("Windows Defender が高負荷になっています。\n\n"
						+ "ゆかり・" + YlConstants.APP_NAME_J + "動作中は Windows Defender を無効化することを推奨します。\n"
						+ "（Windows Defender 以外のセキュリティーソフトを使用することを推奨します）");
			}
		}
	}
}
