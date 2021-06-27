// ============================================================================
// 
// ネビュラコア：負荷監視担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 実測値からの推測
// ・"Processor"-"% Processor Time"-"_Total" は全コアに対する CPU 使用率を返す
//   例）8C16T のマシンで 1 スレッドが 100% 負荷の場合、6.25 が返る
// ・"Process"-"% Processor Time" シリーズは 1 CPU に対する CPU 使用率を返す
//   例）8C16T のマシンで 1 スレッドが 100% 負荷の場合、100.00 が返る
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

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（負荷監視）のメインルーチン
		// --------------------------------------------------------------------
		protected override void CoreMain()
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
					_logWriterMonitor.LogMessage(TraceEventType.Information, "CPU-All," + _cpuAllCounter?.NextValue().ToString());

					// CPU（プロセス別）
					AddRemoveCpuCounters();
					LogCpuCounters();

					// メモリー
					AddRemoveMemoryCounters();
					LogMemoryCounters();

					// ディスク
					AddRemoveDiskCounters();
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
					return;
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
		private const String INSTANCE_NAME_IDLE = "Idle";
		private const String INSTANCE_NAME_TOTAL = "_Total";

		// 負荷監視ログファイル名
		private const String FILE_NAME_MONITOR_LOG = YlConstants.APP_ID + YlConstants.MONITOR_ID + Common.FILE_EXT_LOG;

		// 記録必須
		private readonly String[] MUST_LOG_INSTANCES = { YlConstants.APP_ID, "mpc", "Everything" };

		// CPU 除外
		private readonly String[] CPU_EXCEPT_INSTANCES = { INSTANCE_NAME_TOTAL, INSTANCE_NAME_IDLE };

		// ディスク除外
		private readonly String[] DISK_EXCEPT_INSTANCES = { INSTANCE_NAME_TOTAL };

		// メモリー除外
		private readonly String[] MEMORY_EXCEPT_INSTANCES = { INSTANCE_NAME_TOTAL, INSTANCE_NAME_IDLE };

		// 測定間隔 [ms]
		private const Int32 INTERVAL = 10 * 1000;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// PC 全体の CPU 負荷
		private PerformanceCounter? _cpuAllCounter;

		// プロセスごとの CPU 負荷
		private Dictionary<String, PerformanceCounter> _cpuCounters = new();

		// プロセスごとのメモリー使用量
		private Dictionary<String, PerformanceCounter> _memoryCounters = new();

		// ドライブごとのディスク負荷
		private Dictionary<String, PerformanceCounter> _diskCounters = new();

		// Dispose フラグ
		private Boolean _isDisposed;

		// 詳細ログ（負荷監視専用）
		private readonly LogWriter _logWriterMonitor = new(YlConstants.APP_ID + YlConstants.MONITOR_ID);

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// 存在しないカウンターを追加
		// --------------------------------------------------------------------
		private void AddCounters(Dictionary<String, PerformanceCounter> counters, String categoryName, String counterName, String[] instanceNames, String[] exceptInstanceNames)
		{
			IEnumerable<String> addes = instanceNames.Where(x => !counters.ContainsKey(x));
			foreach (String add in addes)
			{
				if (exceptInstanceNames.Contains(add))
				{
					// 除外
					continue;
				}

				PerformanceCounter? counter = CreateCounter(categoryName, counterName, add);
				if (counter != null)
				{
					counters[add] = counter;
					_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "カウンター追加：" + add);
				}
			}
		}

		// --------------------------------------------------------------------
		// カウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveCountersCore(Dictionary<String, PerformanceCounter> counters, String categoryName, String counterName, String[] exceptInstanceNames)
		{
			PerformanceCounterCategory category = PerformanceCounterCategory.GetCategories().Single(x => x.CategoryName == categoryName);
			String[] instanceNames = category.GetInstanceNames();
			RemoveCounters(counters, instanceNames);
			AddCounters(counters, categoryName, counterName, instanceNames, exceptInstanceNames);
		}

		// --------------------------------------------------------------------
		// CPU 負荷カウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveCpuCounters()
		{
			AddRemoveCountersCore(_cpuCounters, CATEGORY_NAME_PROCESS, COUNTER_NAME_PERCENT_PROCESSOR_TIME, CPU_EXCEPT_INSTANCES);
		}

		// --------------------------------------------------------------------
		// ディスク負荷カウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveDiskCounters()
		{
			// PhysicalDisk ? LogicalDisk ?
			AddRemoveCountersCore(_diskCounters, CATEGORY_NAME_LOGICAL_DISK, COUNTER_NAME_PERCENT_DISK_TIME, DISK_EXCEPT_INSTANCES);
		}

		// --------------------------------------------------------------------
		// メモリーカウンターの追加と削除
		// --------------------------------------------------------------------
		private void AddRemoveMemoryCounters()
		{
			AddRemoveCountersCore(_memoryCounters, CATEGORY_NAME_PROCESS, COUNTER_NAME_WORKING_SET_PRIVATE, MEMORY_EXCEPT_INSTANCES);
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
		private void LogCountersCore(Dictionary<String, PerformanceCounter> counters, Boolean isPercent, String labelPrefix, Int32 rank, String[] mustLogInstances)
		{
			// カウンター値取得
			List<KeyValuePair<String, Single>> values = new(counters.Count);
			foreach (KeyValuePair<String, PerformanceCounter> kvp in counters)
			{
				try
				{
					values.Add(new KeyValuePair<String, Single>(kvp.Key, kvp.Value.NextValue()));
				}
				catch (Exception excep)
				{
					// カウンターが消滅した場合は例外となる模様
					_logWriterMonitor.LogMessage(TraceEventType.Error, "カウンター値取得時エラー：" + excep.Message);
				}
			}

			// 記録必須インスタンスを記録
			foreach (String instance in mustLogInstances)
			{
				IEnumerable<KeyValuePair<String, Single>> matches = values.Where(x => x.Key.Contains(instance, StringComparison.OrdinalIgnoreCase));
				foreach (KeyValuePair<String, Single> match in matches)
				{
					LogOne(match, isPercent, labelPrefix);
				}
				values.RemoveAll(x => x.Key.Contains(instance, StringComparison.OrdinalIgnoreCase));
			}

			// 降順に rank 個記録
			values.Sort((x, y) => y.Value.CompareTo(x.Value));
			for (Int32 i = 0; i < Math.Min(rank, values.Count); i++)
			{
				LogOne(values[i], isPercent, labelPrefix);
			}
		}

		// --------------------------------------------------------------------
		// CPU 負荷を記録
		// --------------------------------------------------------------------
		private void LogCpuCounters()
		{
			LogCountersCore(_cpuCounters, true, "CPU,", 5, MUST_LOG_INSTANCES);
		}

		// --------------------------------------------------------------------
		// ディスク負荷を記録
		// --------------------------------------------------------------------
		private void LogDiskCounters()
		{
			LogCountersCore(_diskCounters, true, "Dsk,", 3, Array.Empty<String>());
		}

		// --------------------------------------------------------------------
		// メモリー負荷を記録
		// --------------------------------------------------------------------
		private void LogMemoryCounters()
		{
			LogCountersCore(_memoryCounters, false, "Mem,", 5, MUST_LOG_INSTANCES);
		}

		// --------------------------------------------------------------------
		// 1 行分記録
		// --------------------------------------------------------------------
		private void LogOne(KeyValuePair<String, Single> value, Boolean isPercent, String labelPrefix)
		{
			if (isPercent)
			{
				_logWriterMonitor.LogMessage(TraceEventType.Information, labelPrefix + value.Key + "," + ((Int32)value.Value).ToString());
			}
			else
			{
				_logWriterMonitor.LogMessage(TraceEventType.Information, labelPrefix + value.Key + "," + (((Int64)value.Value) / 1024 / 1024).ToString());
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

			_logWriterMonitor.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "稼働開始 ====================");
		}

	}
}
