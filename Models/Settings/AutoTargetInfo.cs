// ============================================================================
// 
// ドライブ接続時にゆかり検索対象フォルダーに自動的に追加するための情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.Settings
{
	public class AutoTargetInfo : SerializableSettings
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// driveLetter: "D:" のようにコロンまで
		// --------------------------------------------------------------------
		public AutoTargetInfo(String driveLetter)
				: base(YukaListerModel.Instance.EnvModel.LogWriter, null)
		{
			Debug.Assert(driveLetter.Length == 2, "AutoTargetInfo() bad driveLetter");
			_driveLetter = driveLetter;
		}

		// --------------------------------------------------------------------
		// シリアライズ用コンストラクター
		// --------------------------------------------------------------------
		public AutoTargetInfo()
				: base(YukaListerModel.Instance.EnvModel.LogWriter, null)
		{
			_driveLetter = String.Empty;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 前回接続時に追加されていたフォルダー群（ドライブレターを除き '\\' から始まる）
		public List<String> Folders { get; set; } = new();

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存前の調整
		// --------------------------------------------------------------------
		protected override void AdjustBeforeSave()
		{
			YlCommon.YukaListerStatusFolderPath(_driveLetter, true);
		}

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected override String SettingsPath()
		{
			if (String.IsNullOrEmpty(_driveLetter))
			{
				throw new Exception("ドライブレターが設定されていません。");
			}
			return YlCommon.YukaListerStatusFolderPath(_driveLetter) + FILE_NAME_AUTO_TARGET_INFO;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// 自動追加情報記録ファイル名
		private const String FILE_NAME_AUTO_TARGET_INFO = YlConstants.APP_ID + "AutoTarget" + Common.FILE_EXT_CONFIG;

		// ====================================================================
		// private 変数
		// ====================================================================

		// ドライブレター
		private readonly String _driveLetter;
	}
}
