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
using System.Text;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SharedMisc
{
	public class YlCommon
	{
		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定ファイルのルールを動作時用に変換
		// --------------------------------------------------------------------
		public static FolderSettingsInMemory CreateFolderSettingsInMemory(FolderSettingsInDisk folderSettingsInDisk)
		{
			FolderSettingsInMemory folderSettingsInMemory = new();
			String rule;
			List<String> groups;

			// フォルダー命名規則を辞書に格納
			foreach (String inDisk in folderSettingsInDisk.FolderNameRules)
			{
				Int32 equalPos = inDisk.IndexOf('=');
				if (equalPos < 2)
				{
					continue;
				}
				if (inDisk[0] != YlConstants.RULE_VAR_BEGIN[0])
				{
					continue;
				}
				if (inDisk[equalPos - 1] != YlConstants.RULE_VAR_END[0])
				{
					continue;
				}

				folderSettingsInMemory.FolderNameRules[inDisk.Substring(1, equalPos - 2).ToLower()] = inDisk.Substring(equalPos + 1);
			}

			// ファイル命名規則を正規表現に変換
			for (Int32 i = 0; i < folderSettingsInDisk.FileNameRules.Count; i++)
			{
				// ワイルドカードのみ <> で囲まれていないので、処理をやりやすくするために <> で囲む
				String? fileNameRule = folderSettingsInDisk.FileNameRules[i]?.Replace(YlConstants.RULE_VAR_ANY,
						YlConstants.RULE_VAR_BEGIN + YlConstants.RULE_VAR_ANY + YlConstants.RULE_VAR_END);

				if (String.IsNullOrEmpty(fileNameRule))
				{
					continue;
				}
				MakeRegexPattern(fileNameRule!, out rule, out groups);
				folderSettingsInMemory.FileNameRules.Add(rule);
				folderSettingsInMemory.FileRegexGroups.Add(groups);
			}

			return folderSettingsInMemory;
		}

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
		// フォルダー設定を読み込む
		// 見つからない場合は null ではなく空のインスタンスを返す
		// ニコカラりすたーのフォルダー設定ファイルには対応しない
		// --------------------------------------------------------------------
		public static FolderSettingsInDisk LoadFolderSettings2Ex(String? folder)
		{
			FolderSettingsInDisk folderSettings = new();
			try
			{
				String? folderSettingsFolder = FindSettingsFolder2Ex(folder);
				if (!String.IsNullOrEmpty(folderSettingsFolder))
				{
					if (File.Exists(folderSettingsFolder + "\\" + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG))
					{
						// エントリーが欠損しているファイルから Deserialize() しても FileNameRules 等は null にはならない
						folderSettings = Common.Deserialize(folderSettingsFolder + "\\" + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG, folderSettings);
					}
				}
			}
			catch (Exception)
			{
			}

			return folderSettings;
		}

		// --------------------------------------------------------------------
		// 環境情報をログする
		// --------------------------------------------------------------------
		public static void LogEnvironmentInfo()
		{
			SystemEnvironment se = new();
			se.LogEnvironment(YukaListerModel.Instance.EnvModel.LogWriter);
		}

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定ファイルのルール表記を正規表現に変換
		// --------------------------------------------------------------------
		private static void MakeRegexPattern(String ruleInDisk, out String ruleInMemory, out List<String> groups)
		{
			groups = new();

			// 元が空なら空で返す
			if (String.IsNullOrEmpty(ruleInDisk))
			{
				ruleInMemory = String.Empty;
				return;
			}

			StringBuilder sb = new();
			sb.Append("^");
			Int32 beginPos = 0;
			Int32 endPos;
			Boolean longestExists = false;
			while (beginPos < ruleInDisk.Length)
			{
				if (ruleInDisk[beginPos] == YlConstants.RULE_VAR_BEGIN[0])
				{
					// 変数を解析
					endPos = MakeRegexPatternFindVarEnd(ruleInDisk, beginPos + 1);
					if (endPos < 0)
					{
						throw new Exception("命名規則の " + (beginPos + 1) + " 文字目の < に対応する > がありません。\n" + ruleInDisk);
					}

					// 変数の <> は取り除く
					String varName = ruleInDisk.Substring(beginPos + 1, endPos - beginPos - 1).ToLower();
					groups.Add(varName);

					// 番組名・楽曲名は区切り文字を含むこともあるため最長一致で検索する
					// また、最低 1 つは最長一致が無いとマッチしない
					if (varName == YlConstants.RULE_VAR_PROGRAM || varName == YlConstants.RULE_VAR_TITLE || !longestExists && endPos == ruleInDisk.Length - 1)
					{
						sb.Append("(.*)");
						longestExists = true;
					}
					else
					{
						sb.Append("(.*?)");
					}

					beginPos = endPos + 1;
				}
				else if (@".$^{[(|)*+?\".IndexOf(ruleInDisk[beginPos]) >= 0)
				{
					// エスケープが必要な文字
					sb.Append('\\');
					sb.Append(ruleInDisk[beginPos]);
					beginPos++;
				}
				else
				{
					// そのまま追加
					sb.Append(ruleInDisk[beginPos]);
					beginPos++;
				}
			}
			sb.Append("$");
			ruleInMemory = sb.ToString();
		}

		// --------------------------------------------------------------------
		// <Title> 等の開始 < に対する終了 > の位置を返す
		// ＜引数＞ beginPos：開始 < の次の位置
		// --------------------------------------------------------------------
		private static Int32 MakeRegexPatternFindVarEnd(String str, Int32 beginPos)
		{
			while (beginPos < str.Length)
			{
				if (str[beginPos] == YlConstants.RULE_VAR_END[0])
				{
					return beginPos;
				}
				beginPos++;
			}
			return -1;
		}

	}
}
