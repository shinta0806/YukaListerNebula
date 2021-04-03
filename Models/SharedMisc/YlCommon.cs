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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
		// 頭文字を返す
		// ひらがな（濁点なし）、その他、のいずれか
		// --------------------------------------------------------------------
		public static String Head(String? str)
		{
			if (String.IsNullOrEmpty(str))
			{
				return YlConstants.HEAD_MISC;
			}

			Char chara = str[0];

			// カタカナをひらがなに変換
			if ('ァ' <= chara && chara <= 'ヶ')
			{
				chara = (Char)(chara - 0x0060);
			}

			// 濁点・小文字をノーマルに変換
			Int32 headConvertPos = HEAD_CONVERT_FROM.IndexOf(chara);
			if (headConvertPos >= 0)
			{
				chara = HEAD_CONVERT_TO[headConvertPos];
			}

			// ひらがなを返す
			if ('あ' <= chara && chara <= 'ん')
			{
				return new string(chara, 1);
			}

			return YlConstants.HEAD_MISC;
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

		// --------------------------------------------------------------------
		// ファイル名とファイル命名規則がマッチするか確認し、マッチしたマップを返す
		// ＜引数＞ fileNameBody: 拡張子無し
		// --------------------------------------------------------------------
		public static Dictionary<String, String?> MatchFileNameRulesForListContext(String fileNameBody, FolderSettingsInMemory folderSettingsInMemory)
		{
			Dictionary<String, String?> dic = MatchFileNameRulesCore(fileNameBody, folderSettingsInMemory);

			// 正規化（差異部分のみ）
			dic[YlConstants.RULE_VAR_TITLE_RUBY] = NormalizeDbRubyForListContext(dic[YlConstants.RULE_VAR_TITLE_RUBY]);

			return dic;
		}

		// --------------------------------------------------------------------
		// ファイル名とファイル命名規則・フォルダー固定値がマッチするか確認し、マッチしたマップを返す
		// ＜引数＞ fileNameBody: 拡張子無し
		// --------------------------------------------------------------------
		public static Dictionary<String, String?> MatchFileNameRulesAndFolderRuleForListContext(String fileNameBody, FolderSettingsInMemory folderSettingsInMemory)
		{
			// ファイル名命名規則
			Dictionary<String, String?> dic = YlCommon.MatchFileNameRulesForListContext(fileNameBody, folderSettingsInMemory);

			// フォルダー命名規則をマージ
			foreach (KeyValuePair<String, String?> folderRule in folderSettingsInMemory.FolderNameRules)
			{
				if (dic.ContainsKey(folderRule.Key) && String.IsNullOrEmpty(dic[folderRule.Key]))
				{
					dic[folderRule.Key] = folderRule.Value;
				}
			}

			return dic;
		}

		// --------------------------------------------------------------------
		// リストデータベースに登録するフリガナの表記揺れを減らす
		// ＜返値＞ フリガナ表記 or null（空になる場合）
		// ※楽曲情報データベースに登録する際は
		// --------------------------------------------------------------------
		public static String? NormalizeDbRubyForListContext(String? str)
		{
			Debug.Assert(NORMALIZE_DB_RUBY_FROM.Length == NORMALIZE_DB_RUBY_TO.Length, "NormalizeDbRubyForListContext() different NORMALIZE_DB_FURIGANA_FROM NORMALIZE_DB_FURIGANA_TO length");

			if (String.IsNullOrEmpty(str))
			{
				return null;
			}

			StringBuilder katakana = new ();

			for (Int32 i = 0; i < str.Length; i++)
			{
				Char chara = str[i];

				// 小文字・半角カタカナ等を全角カタカナに変換
				Int32 pos = NORMALIZE_DB_RUBY_FROM.IndexOf(chara);
				if (pos >= 0)
				{
					katakana.Append(NORMALIZE_DB_RUBY_TO[pos]);
					continue;
				}

				// 上記以外の全角カタカナ・音引きはそのまま
				if ('ア' <= chara && chara <= 'ン' || chara == 'ー')
				{
					katakana.Append(chara);
					continue;
				}

				// 上記以外のひらがなをカタカナに変換
				if ('あ' <= chara && chara <= 'ん')
				{
					katakana.Append((Char)(chara + 0x60));
					continue;
				}

				// その他の文字は無視する
			}

			String katakanaString = katakana.ToString();
			if (String.IsNullOrEmpty(katakanaString))
			{
				return null;
			}

			return katakanaString;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースに登録する文字列の表記揺れを減らす
		// 半角チルダ・波ダッシュは全角チルダに変換する（波ダッシュとして全角チルダが用いられているため）
		// ＜返値＞ 正規化後表記 or null（空になる場合）
		// --------------------------------------------------------------------
		public static String? NormalizeDbString(String? str)
		{
			Debug.Assert(NORMALIZE_DB_STRING_FROM.Length == NORMALIZE_DB_STRING_TO.Length, "NormalizeDbString() different NORMALIZE_DB_STRING_FROM NORMALIZE_DB_STRING_TO length");

			if (String.IsNullOrEmpty(str))
			{
				return null;
			}

			StringBuilder normalized = new();

			for (Int32 i = 0; i < str.Length; i++)
			{
				Char chara = str[i];

				// 一部記号・全角英数を半角に変換
				if ('！' <= chara && chara <= '｝')
				{
					normalized.Append((Char)(chara - 0xFEE0));
					continue;
				}

				// テーブルによる変換
				Int32 pos = NORMALIZE_DB_STRING_FROM.IndexOf(chara);
				if (pos >= 0)
				{
					normalized.Append(NORMALIZE_DB_STRING_TO[pos]);
					continue;
				}

				// 変換なし
				normalized.Append(chara);
			}

			String normalizedString = normalized.ToString().Trim();
			if (String.IsNullOrEmpty(normalizedString))
			{
				return null;
			}

			return normalizedString;
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// --------------------------------------------------------------------
		// DB 変換
		// --------------------------------------------------------------------

		// NormalizeDbRuby() 用：フリガナ正規化対象文字（小文字・濁点のカナ等）
		private const String NORMALIZE_DB_RUBY_FROM = "ァィゥェォッャュョヮヵヶガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポヰヱヴヷヸヹヺｧｨｩｪｫｯｬｭｮ"
				+ "ぁぃぅぇぉっゃゅょゎゕゖがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゐゑゔ" + NORMALIZE_DB_FORBIDDEN_FROM;
		private const String NORMALIZE_DB_RUBY_TO = "アイウエオツヤユヨワカケカキクケコサシスセソタチツテトハヒフヘホハヒフヘホイエウワイエヲアイウエオツヤユヨ"
				+ "アイウエオツヤユヨワカケカキクケコサシスセソタチツテトハヒフヘホハヒフヘホイエウ" + NORMALIZE_DB_FORBIDDEN_TO;

		// NormalizeDbString() 用：禁則文字（全角スペース、一部の半角文字等）
		private const String NORMALIZE_DB_STRING_FROM = "　\u2019ｧｨｩｪｫｯｬｭｮﾞﾟ｡｢｣､･~\u301C" + NORMALIZE_DB_FORBIDDEN_FROM;
		private const String NORMALIZE_DB_STRING_TO = " 'ァィゥェォッャュョ゛゜。「」、・～～" + NORMALIZE_DB_FORBIDDEN_TO;

		// NormalizeDbXXX() 用：変換後がフリガナ対象の禁則文字（半角カタカナ）
		private const String NORMALIZE_DB_FORBIDDEN_FROM = "ｦｰｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝ";
		private const String NORMALIZE_DB_FORBIDDEN_TO = "ヲーアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワン";

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// 頭文字変換用
		private const String HEAD_CONVERT_FROM = "ぁぃぅぇぉゕゖゃゅょゎゔがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゐゑ";
		private const String HEAD_CONVERT_TO = "あいうえおかけやゆよわうかきくけこさしすせそたちつてとはひふへほはひふへほいえ";

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ファイル名とファイル命名規則がマッチするか確認し、マッチしたマップを返す
		// ＜引数＞ fileNameBody: 拡張子無し
		// --------------------------------------------------------------------
		private static Dictionary<String, String?> MatchFileNameRulesCore(String fileNameBody, FolderSettingsInMemory folderSettingsInMemory)
		{
			Dictionary<String, String?> dic = CreateRuleDictionary();
			Match? match = null;
			Int32 matchIndex = -1;

			// ファイル名と合致する命名規則を探す
			for (Int32 i = 0; i < folderSettingsInMemory.FileNameRules.Count; i++)
			{
				match = Regex.Match(fileNameBody, folderSettingsInMemory.FileNameRules[i], RegexOptions.None);
				if (match.Success)
				{
					matchIndex = i;
					break;
				}
			}
			if (matchIndex < 0)
			{
				return dic;
			}

			for (Int32 i = 0; i < folderSettingsInMemory.FileRegexGroups[matchIndex].Count; i++)
			{
				// 定義されているキーのみ格納する
				if (dic.ContainsKey(folderSettingsInMemory.FileRegexGroups[matchIndex][i]))
				{
					// match.Groups[0] にはマッチした全体の値が入っているので無視し、[1] から実際の値が入っている
					if (String.IsNullOrEmpty(dic[folderSettingsInMemory.FileRegexGroups[matchIndex][i]]))
					{
						dic[folderSettingsInMemory.FileRegexGroups[matchIndex][i]] = match?.Groups[i + 1].Value.Trim();
					}
					else
					{
						dic[folderSettingsInMemory.FileRegexGroups[matchIndex][i]] += YlConstants.VAR_VALUE_DELIMITER + match?.Groups[i + 1].Value.Trim();
					}
				}
			}

			// 正規化（共通部分のみ）
			dic[YlConstants.RULE_VAR_CATEGORY] = NormalizeDbString(dic[YlConstants.RULE_VAR_CATEGORY]);
			dic[YlConstants.RULE_VAR_PROGRAM] = NormalizeDbString(dic[YlConstants.RULE_VAR_PROGRAM]);
			dic[YlConstants.RULE_VAR_AGE_LIMIT] = NormalizeDbString(dic[YlConstants.RULE_VAR_AGE_LIMIT]);
			dic[YlConstants.RULE_VAR_OP_ED] = NormalizeDbString(dic[YlConstants.RULE_VAR_OP_ED]);
			dic[YlConstants.RULE_VAR_TITLE] = NormalizeDbString(dic[YlConstants.RULE_VAR_TITLE]);
			dic[YlConstants.RULE_VAR_ARTIST] = NormalizeDbString(dic[YlConstants.RULE_VAR_ARTIST]);
			dic[YlConstants.RULE_VAR_WORKER] = NormalizeDbString(dic[YlConstants.RULE_VAR_WORKER]);
			dic[YlConstants.RULE_VAR_TRACK] = NormalizeDbString(dic[YlConstants.RULE_VAR_TRACK]);
			dic[YlConstants.RULE_VAR_COMMENT] = NormalizeDbString(dic[YlConstants.RULE_VAR_COMMENT]);

			return dic;
		}

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
