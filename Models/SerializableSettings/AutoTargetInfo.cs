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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.SerializableSettings
{
	public class AutoTargetInfo : SerializableSettings
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// driveLetter: "D:" のようにコロンまで
		// --------------------------------------------------------------------
		public AutoTargetInfo(String driveLetter)
		{
			Debug.Assert(driveLetter.Length == 2, "AutoTargetInfo() bad driveLetter");
			_driveLetter = driveLetter;
		}

		// --------------------------------------------------------------------
		// コンストラクター
		// シリアライズ用
		// --------------------------------------------------------------------
		public AutoTargetInfo()
		{
			_driveLetter = String.Empty;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 前回接続時に追加されていたフォルダー群（ドライブレターを除き '\\' から始まる）
		public List<String> Folders { get; set; } = new();

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存後の調整
		// --------------------------------------------------------------------
		protected override void AdjustAfterSave()
		{
			FileAttributes attr = File.GetAttributes(SettingsPath());
			File.SetAttributes(SettingsPath(), attr | FileAttributes.Hidden);
		}

		// --------------------------------------------------------------------
		// 保存前の調整
		// --------------------------------------------------------------------
		protected override void AdjustBeforeSave()
		{
			// 隠しファイルを直接上書きできないので一旦削除する
			if (File.Exists(SettingsPath()))
			{
				File.Delete(SettingsPath());
			}
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
			return _driveLetter + "\\" + FILE_NAME_AUTO_TARGET_INFO;
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// 自動追加情報記録ファイル名
		private const String FILE_NAME_AUTO_TARGET_INFO = YlConstants.APP_ID + "AutoTarget" + Common.FILE_EXT_CONFIG;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// ドライブレター
		private String _driveLetter;
	}
}
