// ============================================================================
// 
// ゆかりすたー NEBULA 共通で使用する関数
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;
using System.IO;

using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SharedMisc
{
	public class YlCommon
	{
		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// アプリ独自の変数を格納する変数を生成し、定義済みキーをすべて初期化（キーには <> は含まない）
		// ・キーが無いと LINQ で例外が発生することがあるため
		// ・キーの有無と値の null の 2 度チェックは面倒くさいため
		// --------------------------------------------------------------------
		public static Dictionary<String, String?> CreateRuleDictionary()
		{
			Dictionary<String, String> varMapWith = CreateRuleDictionaryWithDescription();
			Dictionary<String, String?> varMap = new Dictionary<String, String?>();

			foreach (String key in varMapWith.Keys)
			{
				varMap[key] = null;
			}

			return varMap;
		}

		// --------------------------------------------------------------------
		// アプリ独自の変数とその説明
		// --------------------------------------------------------------------
		public static Dictionary<String, String> CreateRuleDictionaryWithDescription()
		{
			Dictionary<String, String> varMap = new Dictionary<String, String>();

			// タイアップマスターにも同様の項目があるもの
			varMap[YlConstants.RULE_VAR_CATEGORY] = "カテゴリー";
			varMap[YlConstants.RULE_VAR_PROGRAM] = "タイアップ名";
			varMap[YlConstants.RULE_VAR_AGE_LIMIT] = "年齢制限";

			// 楽曲マスターにも同様の項目があるもの
			varMap[YlConstants.RULE_VAR_OP_ED] = "摘要（OP/ED 別）";
			varMap[YlConstants.RULE_VAR_TITLE] = "楽曲名";
			varMap[YlConstants.RULE_VAR_TITLE_RUBY] = "楽曲名フリガナ";
			varMap[YlConstants.RULE_VAR_ARTIST] = "歌手名";

			// ファイル名からのみ取得可能なもの
			varMap[YlConstants.RULE_VAR_WORKER] = "ニコカラ制作者";
			varMap[YlConstants.RULE_VAR_TRACK] = "トラック情報";
			varMap[YlConstants.RULE_VAR_ON_VOCAL] = "オンボーカルトラック";
			varMap[YlConstants.RULE_VAR_OFF_VOCAL] = "オフボーカルトラック";
			varMap[YlConstants.RULE_VAR_COMMENT] = "備考";

			// 楽曲マスターにも同様の項目があるもの
			varMap[YlConstants.RULE_VAR_TAG] = "タグ";

			// その他
			varMap[YlConstants.RULE_VAR_ANY] = "無視する部分";

			return varMap;
		}

		// --------------------------------------------------------------------
		// 指定されたフォルダーの除外設定有無
		// 当該フォルダーまたはその親フォルダーに除外設定があるか
		// --------------------------------------------------------------------
		public static FolderExcludeSettingsStatus DetectFolderExcludeSettingsStatus(String folder)
		{
			String? folderExcludeSettingsFolder = FindExcludeSettingsFolder2Ex(folder);
			if (String.IsNullOrEmpty(folderExcludeSettingsFolder))
			{
				return FolderExcludeSettingsStatus.False;
			}
			else
			{
				return FolderExcludeSettingsStatus.True;
			}
		}

		// --------------------------------------------------------------------
		// 指定されたフォルダーの設定有無
		// --------------------------------------------------------------------
		public static FolderSettingsStatus DetectFolderSettingsStatus2Ex(String folder)
		{
			String? folderSettingsFolder = FindSettingsFolder2Ex(folder);
			if (String.IsNullOrEmpty(folderSettingsFolder))
			{
				return FolderSettingsStatus.None;
			}
			else if (IsSamePath(folder, folderSettingsFolder))
			{
				return FolderSettingsStatus.Set;
			}
			else
			{
				return FolderSettingsStatus.Inherit;
			}
		}

		// --------------------------------------------------------------------
		// 指定されたフォルダーのフォルダー除外設定ファイルがあるフォルダーを返す
		// --------------------------------------------------------------------
		public static String? FindExcludeSettingsFolder2Ex(String? folder)
		{
			while (!String.IsNullOrEmpty(folder))
			{
				if (File.Exists(folder + "\\" + YlConstants.FILE_NAME_YUKA_LISTER_EXCLUDE_CONFIG))
				{
					return folder;
				}
				folder = Path.GetDirectoryName(folder);
			}
			return null;
		}

		// --------------------------------------------------------------------
		// 指定されたフォルダーのフォルダー設定ファイルがあるフォルダーを返す
		// --------------------------------------------------------------------
		public static String? FindSettingsFolder2Ex(String? folder)
		{
			while (!String.IsNullOrEmpty(folder))
			{
				if (File.Exists(folder + "\\" + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG))
				{
					return folder;
				}
				folder = Path.GetDirectoryName(folder);
			}
			return null;
		}

		// --------------------------------------------------------------------
		// 同一のファイル・フォルダーかどうか
		// 末尾の '\\' 有無や大文字小文字にかかわらず比較する
		// いずれかが null の場合は false とする
		// --------------------------------------------------------------------
		public static Boolean IsSamePath(String? path1, String? path2)
		{
			if (String.IsNullOrEmpty(path1) || String.IsNullOrEmpty(path2))
			{
				return false;
			}

			// 末尾の '\\' を除去
			if (path1[^1] == '\\')
			{
				path1 = path1[0..^1];
			}
			if (path2[^1] == '\\')
			{
				path2 = path2[0..^1];
			}
			return String.Compare(path1, path2, true) == 0;
		}

		// --------------------------------------------------------------------
		// 環境情報をログする
		// --------------------------------------------------------------------
		public static void LogEnvironmentInfo()
		{
			SystemEnvironment se = new();
			se.LogEnvironment(YukaListerModel.Instance.EnvModel.LogWriter);
		}

	}
}
