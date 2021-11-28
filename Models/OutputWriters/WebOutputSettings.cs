// ============================================================================
// 
// HTML / PHP リスト出力設定用基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;

using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.OutputWriters
{
	public class WebOutputSettings : OutputSettings
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public WebOutputSettings()
		{
			// 初期化
			EnableNew = true;
			NewDays = NEW_DAYS_DEFAULT;

			// リストはデシリアライズ時に重複するため初期化しない
		}

		// ====================================================================
		// public プロパティ
		// ====================================================================

		// NEW を出力する
		public Boolean EnableNew { get; set; }

		// NEW と見なす日数
		public Int32 NewDays { get; set; }

		// グループナビの項目の順番（[0] は必ず GroupNaviItems.New に設定される）
		public List<GroupNaviItems> GroupNaviSequence { get; set; } = new();

		// 頭文字「その他」を出力する
		public Boolean OutputHeadMisc { get; set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み後の調整
		// --------------------------------------------------------------------
		public override void AdjustAfterGenerateOrLoad()
		{
			// 自クラス調整
			if (NewDays < YlConstants.NEW_DAYS_MIN)
			{
				NewDays = NEW_DAYS_DEFAULT;
			}
			SetGroupNaviSequenceIfNeeded();

			// 基底クラス調整
			base.AdjustAfterGenerateOrLoad();
		}

		// --------------------------------------------------------------------
		// 読み込み
		// 派生クラスでオーバーライドする際は、派生クラス読み込み後にここを呼ぶ
		// --------------------------------------------------------------------
		public override void Load()
		{
			// 自クラス読み込み
			try
			{
				WebOutputSettings tmp = new();
				tmp = Common.Deserialize(SettingsPath(), tmp);
				Common.ShallowCopyProperties(tmp, this);
			}
			catch (Exception)
			{
			}

			// 基底クラス読み込み
			base.Load();
		}

		// --------------------------------------------------------------------
		// 保存
		// 派生クラスでオーバーライドする際は、ここを呼んでから派生クラスで保存する
		// --------------------------------------------------------------------
		public override void Save()
		{
			// 基底クラス保存
			base.Save();

			// 自クラス保存
			try
			{
				WebOutputSettings tmp = new();
				Common.ShallowCopyProperties(this, tmp);
				Common.Serialize(SettingsPath(), tmp);
			}
			catch (Exception)
			{
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// ====================================================================
		// private 定数
		// ====================================================================

		// NEW と見なす日数のデフォルト
		private const Int32 NEW_DAYS_DEFAULT = 31;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// GroupNaviSequence が正しく設定されていない場合はデフォルト値を設定
		// --------------------------------------------------------------------
		private void SetGroupNaviSequenceIfNeeded()
		{
			if (GroupNaviSequence.Count == (Int32)GroupNaviItems.__End__ && GroupNaviSequence[0] == GroupNaviItems.New)
			{
				return;
			}

			GroupNaviSequence.Clear();
			for (GroupNaviItems i = 0; i < GroupNaviItems.__End__; i++)
			{
				GroupNaviSequence.Add(i);
			}
		}

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		private static String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(WebOutputSettings) + Common.FILE_EXT_CONFIG;
		}
	}
}
