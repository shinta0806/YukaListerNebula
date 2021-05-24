// ============================================================================
// 
// リスト出力設定用基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 基底部分の設定内容を派生クラス間で共有できるようにするために、
// ・派生クラスで設定を保存する際は、基底部分を別ファイルとして保存する
// ・派生クラスで設定を読み込む際は、別ファイルの基底部分を追加で読み込む
// 基底部分を追加での保存・読み込みがあるため、SerializableSettings の派生にはできない
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.OutputWriters
{
	public class OutputSettings
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public OutputSettings()
		{
			// リストはデシリアライズ時に重複するため初期化しない
		}

		// ====================================================================
		// public プロパティ
		// ====================================================================

		// 全ての項目を出力する
		public Boolean OutputAllItems { get; set; }

		// 出力項目の選択
		public List<OutputItems> SelectedOutputItems { get; set; } = new();

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み
		// 派生クラスでオーバーライドする際は、派生クラス読み込み後にここを呼ぶ
		// --------------------------------------------------------------------
		public virtual void Load()
		{
			try
			{
				OutputSettings tmp = new();
				tmp = Common.Deserialize(SettingsPath(), tmp);
				Common.ShallowCopyProperties(tmp, this);
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
		// OutputAllItems / SelectedOutputItems を考慮した出力アイテム
		// --------------------------------------------------------------------
		public List<OutputItems> RuntimeOutputItems()
		{
			List<OutputItems> runtimeOutputItems;

			if (OutputAllItems)
			{
				runtimeOutputItems = new();
				OutputItems[] outputItems = (OutputItems[])Enum.GetValues(typeof(OutputItems));
				for (Int32 i = 0; i < outputItems.Length - 1; i++)
				{
					runtimeOutputItems.Add(outputItems[i]);
				}
			}
			else
			{
				runtimeOutputItems = new(SelectedOutputItems);
			}

			return runtimeOutputItems;
		}

		// --------------------------------------------------------------------
		// 保存
		// 派生クラスでオーバーライドする際は、ここを呼んでから派生クラスで保存する
		// --------------------------------------------------------------------
		public virtual void Save()
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
				OutputSettings tmp = new();
				Common.ShallowCopyProperties(this, tmp);
				Common.Serialize(SettingsPath(), tmp);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, GetType().Name + "設定保存時エラー：\n" + excep.Message);
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
			if (SelectedOutputItems.Count == 0)
			{
				SelectedOutputItems.Add(OutputItems.TieUpName);
				SelectedOutputItems.Add(OutputItems.SongOpEd);
				SelectedOutputItems.Add(OutputItems.SongName);
				SelectedOutputItems.Add(OutputItems.ArtistName);
				SelectedOutputItems.Add(OutputItems.SmartTrack);
				SelectedOutputItems.Add(OutputItems.Worker);
				SelectedOutputItems.Add(OutputItems.Comment);
				SelectedOutputItems.Add(OutputItems.FileName);
				SelectedOutputItems.Add(OutputItems.FileSize);
			}
		}

		// --------------------------------------------------------------------
		// 保存前の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustBeforeSave()
		{
		}

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		private static String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(OutputSettings) + Common.FILE_EXT_CONFIG;
		}
	}
}
