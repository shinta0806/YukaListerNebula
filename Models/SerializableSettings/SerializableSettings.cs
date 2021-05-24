// ============================================================================
// 
// 所定のフォルダーに保存する設定の基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;
using System.IO;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SerializableSettings
{
	public abstract class SerializableSettings
	{
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------
		public void Load()
		{
			try
			{
				AdjustBeforeLoad();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + "読み込み前設定調整時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				if (!File.Exists(SettingsPath()))
				{
					throw new Exception("設定が保存されていません：" + SettingsPath());
				}
				SerializableSettings loaded = Common.Deserialize(SettingsPath(), this);
				Common.ShallowCopyProperties(loaded, this);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + " 設定読み込み時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				AdjustAfterLoad();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + "読み込み後設定調整時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------
		public void Save()
		{
			try
			{
				AdjustBeforeSave();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + "保存前設定調整時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				Common.Serialize(SettingsPath(), this);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + "設定保存時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				AdjustAfterSave();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + "保存後設定調整時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み後の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustAfterLoad()
		{
		}

		// --------------------------------------------------------------------
		// 保存後の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustAfterSave()
		{
		}

		// --------------------------------------------------------------------
		// 読み込み前の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustBeforeLoad()
		{
		}

		// --------------------------------------------------------------------
		// 保存前の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustBeforeSave()
		{
		}

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected abstract String SettingsPath();
	}
}
