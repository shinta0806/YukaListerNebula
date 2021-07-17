// ============================================================================
// 
// ゆかりすたー NEBULA 共通で使用する関数
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet;
using Livet.Messaging;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.Models.SharedMisc
{
	public class YlCommon
	{
		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンテキストメニューにアイテムを追加
		// ToDo: MVVM 的なやり方でのコンテキストメニューへのコマンド登録方法が分からなかったのでこの方法としている
		// List<ViewModelCommand> をバインドするとコマンドの制御はできるが、表示文字列の制御ができない
		// --------------------------------------------------------------------
		public static void AddContextMenuItem(List<MenuItem> items, String label, RoutedEventHandler click)
		{
			MenuItem menuItem = new();
			menuItem.Header = label;
			menuItem.Click += click;
			items.Add(menuItem);
		}

		// --------------------------------------------------------------------
		// ID 接頭辞の正当性を確認
		// ＜返値＞ 正規化後の ID 接頭辞
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static String? CheckIdPrefix(String? idPrefix, Boolean isNullable)
		{
			idPrefix = NormalizeDbString(idPrefix);

			if (isNullable && String.IsNullOrEmpty(idPrefix))
			{
				return null;
			}

			if (String.IsNullOrEmpty(idPrefix))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞を入力して下さい。");
			}
			if (idPrefix.Length > ID_PREFIX_MAX_LENGTH)
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞は " + ID_PREFIX_MAX_LENGTH + "文字以下にして下さい。");
			}
			if (idPrefix.Contains('_'))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞に \"_\"（アンダースコア）は使えません。");
			}
			if (idPrefix.Contains(','))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞に \",\"（カンマ）は使えません。");
			}
			if (idPrefix.Contains(' '))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞に \" \"（スペース）は使えません。");
			}
			if (idPrefix.Contains('"'))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞に \"\"\"（ダブルクオート）は使えません。");
			}
			if (idPrefix.Contains('\\'))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞に \"\\\"（円マーク）は使えません。");
			}
			if (idPrefix.Contains('!'))
			{
				throw new Exception("各種 ID の先頭に付与する接頭辞に \"!\"（エクスクラメーション）は使えません。");
			}

			return idPrefix;
		}

		// --------------------------------------------------------------------
		// 設定ファイルのルールを動作時用に変換
		// --------------------------------------------------------------------
		public static FolderSettingsInMemory CreateFolderSettingsInMemory(FolderSettingsInDisk folderSettingsInDisk)
		{
			FolderSettingsInMemory folderSettingsInMemory = new();

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

				folderSettingsInMemory.FolderNameRules[inDisk.Substring(1, equalPos - 2).ToLower()] = inDisk[(equalPos + 1)..];
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
				MakeRegexPattern(fileNameRule!, out String rule, out List<String> groups);
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
			Dictionary<String, String?> varMap = new();

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
			Dictionary<String, String> varMap = new();

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
			varMap[YlConstants.RULE_VAR_WORKER] = "カラオケ動画制作者";
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
		// ちょちょいと自動更新起動を作成
		// --------------------------------------------------------------------
		public static UpdaterLauncher CreateUpdaterLauncher(Boolean checkLatest, Boolean forceShow, Boolean clearUpdateCache, Boolean forceInstall)
		{
			// 固定部分
			UpdaterLauncher updaterLauncher = new();
			updaterLauncher.ID = YlConstants.APP_ID;
			updaterLauncher.Name = YlConstants.APP_NAME_J;
			updaterLauncher.Wait = 3;
			updaterLauncher.UpdateRss = "http://shinta.coresv.com/soft/YukaListerNebula_AutoUpdate.xml";
			updaterLauncher.CurrentVer = YlConstants.APP_VER;
			updaterLauncher.Relaunch = YukaListerModel.Instance.EnvModel.ExeFullPath;

			// 変動部分
			if (checkLatest)
			{
				updaterLauncher.LatestRss = "http://shinta.coresv.com/soft/YukaListerNebula_JPN.xml";
			}
			updaterLauncher.LogWriter = YukaListerModel.Instance.EnvModel.LogWriter;
			updaterLauncher.ForceShow = forceShow;
			updaterLauncher.NotifyHWnd = IntPtr.Zero;
			updaterLauncher.ClearUpdateCache = clearUpdateCache;
			updaterLauncher.ForceInstall = forceInstall;

			// 起動用
			return updaterLauncher;
		}

		// --------------------------------------------------------------------
		// 暗号化して Base64 になっている文字列を復号化する
		// --------------------------------------------------------------------
		public static String? Decrypt(String? base64Text)
		{
			if (String.IsNullOrEmpty(base64Text))
			{
				return null;
			}

			Byte[] cipherBytes = Convert.FromBase64String(base64Text);

			// 復号化
			using AesManaged aes = new();
			using ICryptoTransform decryptor = aes.CreateDecryptor(ENCRYPT_KEY, ENCRYPT_IV);
			using MemoryStream writeStream = new();
			using (CryptoStream cryptoStream = new(writeStream, decryptor, CryptoStreamMode.Write))
			{
				cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
			}

			// 文字列化
			Byte[] plainBytes = writeStream.ToArray();
			return Encoding.Unicode.GetString(plainBytes);
		}

		// --------------------------------------------------------------------
		// ファイルが存在していれば削除
		// ＜返値＞ ファイルが存在していた場合の属性
		// --------------------------------------------------------------------
		public static FileAttributes DeleteFileIfExists(String path)
		{

			if (!File.Exists(path))
			{
				return 0;
			}

			FileAttributes attrs = 0;
			try
			{
				attrs = File.GetAttributes(path);
				File.Delete(path);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル削除時エラー：\n" + path + "\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			return attrs;
		}

		// --------------------------------------------------------------------
		// 指定されたフォルダーの除外設定有無
		// 当該フォルダーまたはその親フォルダーに除外設定があるか
		// --------------------------------------------------------------------
		public static FolderExcludeSettingsStatus DetectFolderExcludeSettingsStatus(String folder)
		{
			String? folderExcludeSettingsFolder = FindExcludeSettingsFolder(folder);
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
			String? folderSettingsFolder = FindSettingsFolder(folder);
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
		// ドライブレターを取得
		// DeviceChangeInfo と仕様を合わせ、"D:" のようにコロンまで
		// --------------------------------------------------------------------
		public static String DriveLetter(String path)
		{
			return path[0..2];
		}

		// --------------------------------------------------------------------
		// 文字列を AES 256 bit 暗号化して Base64 で返す
		// --------------------------------------------------------------------
		public static String? Encrypt(String? plainText)
		{
			if (String.IsNullOrEmpty(plainText))
			{
				return null;
			}

			Byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);

			// 暗号化
			using AesManaged aes = new();
			using ICryptoTransform encryptor = aes.CreateEncryptor(ENCRYPT_KEY, ENCRYPT_IV);
			using MemoryStream writeStream = new();
			using (CryptoStream cryptoStream = new(writeStream, encryptor, CryptoStreamMode.Write))
			{
				cryptoStream.Write(plainBytes, 0, plainBytes.Length);
			}

			// Base64
			Byte[] cipherBytes = writeStream.ToArray();
			return Convert.ToBase64String(cipherBytes);
		}

		// --------------------------------------------------------------------
		// 指定されたフォルダーのフォルダー除外設定ファイルがあるフォルダーを返す
		// --------------------------------------------------------------------
		public static String? FindExcludeSettingsFolder(String? folder)
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
		// 互換性維持のため、ニコカラりすたーの設定ファイルも扱う
		// --------------------------------------------------------------------
		public static String? FindSettingsFolder(String? folder)
		{
			while (!String.IsNullOrEmpty(folder))
			{
				if (File.Exists(folder + '\\' + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG))
				{
					return folder;
				}
				if (File.Exists(folder + '\\' + YlConstants.FILE_NAME_NICO_KARA_LISTER_CONFIG))
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
		// ID 接頭辞が未設定ならばユーザーに入力してもらう
		// ＜例外＞ OperationCanceledException
		// --------------------------------------------------------------------
		public static async Task InputIdPrefixIfNeededWithInvoke(ViewModel viewModel)
		{
			// 設定済なら直ちに返る
			if (!String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix))
			{
				return;
			}

			await DispatcherHelper.UIDispatcher.Invoke(new Func<Task>(async () =>
			{
				// 現在設定中なら待機
				await WaitInputIdPrefix();

				// 設定開始
				_isInputtingIdPrefix = true;

				// 待機中に設定済となる場合もあるので再度確認
				if (String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix))
				{
					// 設定済でない場合に限り、ユーザーに入力してもらう
					using InputIdPrefixWindowViewModel inputIdPrefixWindowViewModel = new();
					viewModel.Messenger.Raise(new TransitionMessage(inputIdPrefixWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_INPUT_ID_PREFIX_WINDOW));
				}

				// 設定終了
				_isInputtingIdPrefix = false;
			}));

			if (String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix))
			{
				throw new OperationCanceledException();
			}
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象外のフォルダーかどうか
		// --------------------------------------------------------------------
		public static Boolean IsIgnoreFolder(String folderPath)
		{
			String withoutDriveLetter = WithoutDriveLetter(folderPath);

			if (withoutDriveLetter.StartsWith(@"\$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (withoutDriveLetter.StartsWith(@"\System Volume Information", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return false;
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
		// 検索ワードフリガナ用の文字列を作成
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみをカンマ区切りで連結
		// --------------------------------------------------------------------
		public static String? KeywordRubyForSearch(String? keyword)
		{
			if (String.IsNullOrEmpty(keyword))
			{
				return null;
			}

			String[] elements = keyword.Split(YlConstants.VAR_VALUE_DELIMITER[0], StringSplitOptions.RemoveEmptyEntries);
			List<String> forSearchElements = new();
			foreach (String element in elements)
			{
				(String? ruby, Boolean allRuby, _) = NormalizeDbRubyForSearch(element);
				if (!String.IsNullOrEmpty(ruby) && allRuby && ruby != element)
				{
					forSearchElements.Add(ruby);
				}
			}

			String keywordRubyForSearch = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], forSearchElements);
			if (String.IsNullOrEmpty(keywordRubyForSearch))
			{
				return null;
			}

			return keywordRubyForSearch;
		}

		// --------------------------------------------------------------------
		// 関数を非同期駆動
		// --------------------------------------------------------------------
		public static async Task LaunchTaskAsync<T>(SemaphoreSlim semaphoreSlim, TaskAsyncDelegate<T> deleg, T vari, String taskName) where T : class?
		{
			await Task.Run(async () =>
			{
				semaphoreSlim.Wait();
				try
				{
					// 終了時に強制終了されないように設定
					Thread.CurrentThread.IsBackground = false;

					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "バックグラウンド処理開始：" + taskName);
					await deleg(vari);
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "バックグラウンド処理終了：" + taskName);
				}
				catch (OperationCanceledException)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "バックグラウンド処理中止：" + taskName);
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, taskName + " 実行時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
				finally
				{
					semaphoreSlim.Release();
				}
			});
		}

		// --------------------------------------------------------------------
		// フォルダー設定を読み込む
		// FILE_NAME_YUKA_LISTER_CONFIG 優先、無い場合は FILE_NAME_NICO_KARA_LISTER_CONFIG
		// 見つからない場合は親フォルダーの設定を読み込む
		// それでも見つからない場合は null ではなく空のインスタンスを返す
		// --------------------------------------------------------------------
		public static FolderSettingsInDisk LoadFolderSettings(String? folder)
		{
			FolderSettingsInDisk folderSettings = new();
			try
			{
				String? folderSettingsFolder = FindSettingsFolder(folder);
				if (!String.IsNullOrEmpty(folderSettingsFolder))
				{
					if (File.Exists(folderSettingsFolder + '\\' + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG))
					{
						// エントリーが欠損しているファイルから Deserialize() しても FileNameRules 等は null にはならない
						folderSettings = Common.Deserialize(folderSettingsFolder + '\\' + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG, folderSettings);
					}
					else
					{
						folderSettings = Common.Deserialize(folderSettingsFolder + '\\' + YlConstants.FILE_NAME_NICO_KARA_LISTER_CONFIG, folderSettings);
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
		// 検索結果ソート用関数（大文字小文字を区別しない名前順）
		// --------------------------------------------------------------------
		public static Int32 MasterComparisonByName<T>(T lhs, T rhs) where T : class, IRcMaster
		{
			if (String.IsNullOrEmpty(lhs.Name))
			{
				// 左側が空
				if (String.IsNullOrEmpty(rhs.Name))
				{
					return 0;
				}
				else
				{
					return 1;
				}
			}
			else
			{
				// 左側が空ではない
				if (String.IsNullOrEmpty(rhs.Name))
				{
					return -1;
				}
				else
				{
					// 大文字小文字は区別するが、"A～" の後ろにすべて "a～" が来るわけではなく、きちんと "AA", "Aa", "AB", "Ab" のように並べてくれる模様
					return String.Compare(lhs.Name, rhs.Name);
				}
			}
		}

		// --------------------------------------------------------------------
		// ファイル名とファイル命名規則・フォルダー固定値がマッチするか確認し、マッチしたマップを返す（ルビは検索用に正規化）
		// エイリアスを解決したい場合は TFoundSetter.MatchFileNameRulesAndFolderRuleForSearch() を使用すること
		// ＜引数＞ path: ファイル名フルパス
		// --------------------------------------------------------------------
		public static Dictionary<String, String?> MatchFileNameRulesAndFolderRuleForSearch(String path)
		{
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(Path.GetDirectoryName(path));
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);
			return YlCommon.MatchFileNameRulesAndFolderRuleForSearch(Path.GetFileNameWithoutExtension(path), folderSettingsInMemory);
		}

		// --------------------------------------------------------------------
		// ファイル名とファイル命名規則・フォルダー固定値がマッチするか確認し、マッチしたマップを返す（ルビは検索用に正規化）
		// エイリアスを解決したい場合は TFoundSetter.MatchFileNameRulesAndFolderRuleForSearch() を使用すること
		// ＜引数＞ fileNameBody: 拡張子無し
		// --------------------------------------------------------------------
		public static Dictionary<String, String?> MatchFileNameRulesAndFolderRuleForSearch(String fileNameBody, FolderSettingsInMemory folderSettingsInMemory)
		{
			// ファイル名命名規則
			Dictionary<String, String?> dic = MatchFileNameRulesForSearch(fileNameBody, folderSettingsInMemory);

			// フォルダー固定値をマージ
			MargeFolderRules(folderSettingsInMemory, dic);

			return dic;
		}

		// --------------------------------------------------------------------
		// ミリ秒を 100 ナノ秒に変換
		// --------------------------------------------------------------------
		public static Int64 MiliToHNano(Int32 milli)
		{
			return milli * 10000L;
		}

		// --------------------------------------------------------------------
		// 日付に合わせて年月日文字列を設定
		// --------------------------------------------------------------------
		public static (String? year, String? month, String? day) MjdToStrings(Double mjd)
		{
			if (mjd <= YlConstants.INVALID_MJD)
			{
				return (null, null, null);
			}
			else
			{
				DateTime aReleaseDate = JulianDay.ModifiedJulianDateToDateTime(mjd);
				return (aReleaseDate.Year.ToString(), aReleaseDate.Month.ToString(), aReleaseDate.Day.ToString());
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースに登録するフリガナの表記揺れを減らす
		// 最低限の表記揺れのみ減らす
		// ＜返値＞ フリガナ表記 or null（空になる場合）, 元の文字はすべてフリガナ対応文字だったか（null の場合は false）, 先頭文字がフリガナ対応文字だったか（null の場合は false）
		// --------------------------------------------------------------------
		public static (String? normalizedRuby, Boolean fromAllRuby, Boolean headRuby) NormalizeDbRubyForMusicInfo(String? str)
		{
			Debug.Assert(NORMALIZE_DB_RUBY_FOR_MUSIC_INFO_FROM.Length == NORMALIZE_DB_RUBY_FOR_MUSIC_INFO_TO.Length,
					"NormalizeDbRubyForMusicInfo() different from/to length");
			return NormalizeDbRubyCore(str, NORMALIZE_DB_RUBY_FOR_MUSIC_INFO_FROM, NORMALIZE_DB_RUBY_FOR_MUSIC_INFO_TO, Array.Empty<String>(), Array.Empty<String>());
		}

		// --------------------------------------------------------------------
		// 検索用（検索で使用されるリストデータベース登録用を含む）にフリガナの表記揺れを減らす
		// NormalizeDbRubyForMusicInfo() よりも強力に揺れを減らす
		// ＜返値＞ フリガナ表記 or null（空になる場合）, 元の文字はすべてフリガナ対応文字だったか（null の場合は false）, 先頭文字がフリガナ対応文字だったか（null の場合は false）
		// --------------------------------------------------------------------
		public static (String? normalizedRuby, Boolean fromAllRuby, Boolean headRuby) NormalizeDbRubyForSearch(String? str)
		{
			Debug.Assert(NORMALIZE_DB_RUBY_FOR_SEARCH_FROM.Length == NORMALIZE_DB_RUBY_FOR_SEARCH_TO.Length,
					"NormalizeDbRubyForListContext() different one from/to length");
			Debug.Assert(NORMALIZE_DB_RUBY_FOR_SEARCH_MULTI_FROM.Length == NORMALIZE_DB_RUBY_FOR_SEARCH_MULTI_TO.Length,
					"NormalizeDbRubyForListContext() different multi from/to length");

			(String? normalizedRuby, Boolean fromAllRuby, Boolean headRuby) = NormalizeDbRubyCore(str, NORMALIZE_DB_RUBY_FOR_SEARCH_FROM, NORMALIZE_DB_RUBY_FOR_SEARCH_TO,
					NORMALIZE_DB_RUBY_FOR_SEARCH_MULTI_FROM, NORMALIZE_DB_RUBY_FOR_SEARCH_MULTI_TO);

			// 長音はすべて削除
			if (!String.IsNullOrEmpty(normalizedRuby))
			{
				normalizedRuby = normalizedRuby.Replace("ー", null, StringComparison.Ordinal);
				if (String.IsNullOrEmpty(normalizedRuby))
				{
					normalizedRuby = null;
					fromAllRuby = false;
					headRuby = false;
				}
			}
			return (normalizedRuby, fromAllRuby, headRuby);
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

		// --------------------------------------------------------------------
		// 指定されたファイルを選択してエクスプローラーを開く
		// --------------------------------------------------------------------
		public static void OpenExplorer(String path)
		{
			Process.Start("EXPLORER.EXE", @"/select,""" + path + @"""");
		}

		// --------------------------------------------------------------------
		// カテゴリーメニューに値を設定
		// --------------------------------------------------------------------
		public static void SetContextMenuItemCategories(List<MenuItem> menuItems, RoutedEventHandler click)
		{
			using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TCategory> categories);
			musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			List<String> categoryNames = DbCommon.SelectCategoryNames(categories);
			foreach (String categoryName in categoryNames)
			{
				AddContextMenuItem(menuItems, categoryName, click);
			}
		}

		// --------------------------------------------------------------------
		// 同期詳細ログの設定
		// --------------------------------------------------------------------
		public static void SetLogWriterSyncDetail(LogWriter logWriterSyncDetail)
		{
			// 同期詳細ログ初期化
			// 大量のログが発生するため、世代・サイズともに拡大
			logWriterSyncDetail.ApplicationQuitToken = YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token;
			logWriterSyncDetail.SimpleTraceListener.MaxSize = 5 * 1024 * 1024;
			logWriterSyncDetail.SimpleTraceListener.MaxOldGenerations = 5;
			logWriterSyncDetail.SimpleTraceListener.LogFileName = Path.GetDirectoryName(logWriterSyncDetail.SimpleTraceListener.LogFileName) + "\\" + FILE_NAME_SYNC_DETAIL_LOG;
		}

		// --------------------------------------------------------------------
		// 関連付けられたファイルを開く
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void ShellExecute(String path)
		{
			ProcessStartInfo psi = new()
			{
				FileName = path,
				UseShellExecute = true,
			};
			Process.Start(psi);
		}

		// --------------------------------------------------------------------
		// カンマ区切り ID をリストに分割
		// 引数が空の場合は null ではなく空リストを返す
		// --------------------------------------------------------------------
		public static List<String> SplitIds(String? ids)
		{
			return String.IsNullOrEmpty(ids) ? new() : ids.Split(YlConstants.VAR_VALUE_DELIMITER[0], StringSplitOptions.RemoveEmptyEntries).ToList();
		}

		// --------------------------------------------------------------------
		// 年月日の文字列から日付を生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static Double StringsToMjd(String caption, String? yearString, String? monthString, String? dayString)
		{
			// 正規化
			yearString = YlCommon.NormalizeDbString(yearString);
			monthString = YlCommon.NormalizeDbString(monthString);
			dayString = YlCommon.NormalizeDbString(dayString);

			if (String.IsNullOrEmpty(yearString))
			{
				// 年が入力されていない場合は、月日も空欄でなければならない
				if (!String.IsNullOrEmpty(monthString) || !String.IsNullOrEmpty(dayString))
				{
					throw new Exception(caption + "の年が入力されていません。");
				}

				return YlConstants.INVALID_MJD;
			}

			// 年の確認
			Int32 year = Common.StringToInt32(yearString);
			Int32 nowYear = DateTime.Now.Year;
			if (year < 0)
			{
				throw new Exception(caption + "の年にマイナスの値を入力することはできません。");
			}
			if (year < 100)
			{
				// 2 桁の西暦を 4 桁に変換する
				Int32 nowYear2 = nowYear % 100;
				if (year <= nowYear2)
				{
					// 現在年の 2 桁以下の場合は、現在年と同世紀とする
					year += (nowYear / 100) * 100;
				}
				else if (year <= nowYear2 + YEAR_DISAMBIGUATION_DELTA)
				{
					// 現在年の 2 桁に近い場合は、曖昧さ回避のために受け付けないこととする
					throw new Exception(caption + "の年が今年に近いため、世紀を自動判定できません。年を西暦 4 桁で入力してください。");
				}
				else
				{
					// 現在年より 1 つ古い世紀とする
					year += (nowYear / 100 - 1) * 100;
				}
			}
			if (year < 1000)
			{
				throw new Exception(caption + "の年に 3 桁の値を入力することはできません。");
			}
			if (year < YlConstants.INVALID_YEAR)
			{
				throw new Exception(caption + "の年は " + YlConstants.INVALID_YEAR + " 以上を入力して下さい。");
			}
			if (year > nowYear)
			{
				throw new Exception(caption + "の年は " + nowYear + " 以下を入力して下さい。");
			}

			// 月の確認
			if (String.IsNullOrEmpty(monthString) && !String.IsNullOrEmpty(dayString))
			{
				// 年と日が入力されている場合は、月も入力されていなければならない
				throw new Exception(caption + "の月が入力されていません。");
			}
			Int32 month;
			if (String.IsNullOrEmpty(monthString))
			{
				// 月が空欄の場合は 1 とする
				month = 1;
			}
			else
			{
				month = Common.StringToInt32(monthString);
				if (month < 1 || month > 12)
				{
					throw new Exception(caption + "の月は 1～12 を入力して下さい。");
				}
			}

			// 日の確認
			Int32 day;
			if (String.IsNullOrEmpty(dayString))
			{
				// 日が空欄の場合は 1 とする
				day = 1;
			}
			else
			{
				day = Common.StringToInt32(dayString);
				if (day < 1 || day > 31)
				{
					throw new Exception(caption + "の日は 1～31 を入力して下さい。");
				}
			}

			return JulianDay.DateTimeToModifiedJulianDate(new DateTime(year, month, day));
		}

		// --------------------------------------------------------------------
		// テンポラリフォルダーのパス（末尾 '\\'）
		// --------------------------------------------------------------------
		public static String TempFolderPath()
		{
			return Path.GetTempPath() + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]) + '\\' + Environment.ProcessId.ToString() + '\\';
		}

		// --------------------------------------------------------------------
		// テンポラリフォルダー配下のファイル・フォルダー名として使えるパス（呼びだす度に異なるファイル、拡張子なし）
		// --------------------------------------------------------------------
		public static String TempPath()
		{
			// マルチスレッドでも安全にインクリメント
			Int32 counter = Interlocked.Increment(ref _tempPathCounter);
			return TempFolderPath() + counter.ToString() + "_" + Thread.CurrentThread.ManagedThreadId.ToString();
		}

		// --------------------------------------------------------------------
		// 現在時刻（UTC）の修正ユリウス日
		// --------------------------------------------------------------------
		public static Double UtcNowMjd()
		{
			return JulianDay.DateTimeToModifiedJulianDate(DateTime.UtcNow);
		}

		// --------------------------------------------------------------------
		// ドライブレターを除いたパス
		// "D:" 等を除いたパス
		// --------------------------------------------------------------------
		public static String WithoutDriveLetter(String path)
		{
			// "D:" のような 2 文字に対して [2..] を適用しても例外は発生しない
			return path[2..];
		}

		// --------------------------------------------------------------------
		// ゆかりすたー用ファイル群を保存するフォルダーのフルパス（末尾 '\\'）
		// --------------------------------------------------------------------
		public static String YukaListerStatusFolderPath(String driveLetter, Boolean create = false)
		{
			Debug.Assert(driveLetter.Length == 2, "YukaListerStatusFolderPath() bad driveLetter");
			String path = driveLetter + '\\' + YlConstants.FOLDER_NAME_YUKALISTER_STATUS;
			if (!Directory.Exists(path) && create)
			{
				Directory.CreateDirectory(path);
				FileAttributes attr = File.GetAttributes(path);
				File.SetAttributes(path, attr | FileAttributes.Hidden);
			}
			return path;
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// --------------------------------------------------------------------
		// DB 変換
		// --------------------------------------------------------------------

		// NormalizeDbRubyForSearch() 用：フリガナ正規化対象文字（複数文字）
		private static readonly String[] NORMALIZE_DB_RUBY_FOR_SEARCH_MULTI_FROM = { "ヴァ", "ヴィ", "ヴェ", "ヴォ" };
		private static readonly String[] NORMALIZE_DB_RUBY_FOR_SEARCH_MULTI_TO = { "ハ", "ヒ", "ヘ", "ホ" };

		// NormalizeDbRubyForSearch() 用：フリガナ正規化対象文字（小文字・濁点→大文字・清音）
		private const String NORMALIZE_DB_RUBY_FOR_SEARCH_FROM = "ァィゥェォッャュョヮヵヶヲガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポヰヱヴヷヸヹヺｧｨｩｪｫｯｬｭｮｦ"
				+ "ぁぃぅぇぉっゃゅょゎゕゖをがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゐゑゔ" + NORMALIZE_DB_RUBY_COMMON_FROM + NORMALIZE_DB_FORBIDDEN_FROM;
		private const String NORMALIZE_DB_RUBY_FOR_SEARCH_TO = "アイウエオツヤユヨワカケオカキクケコサシスセソタチツテトハヒフヘホハヒフヘホイエフワイエオアイウエオツヤユヨオ"
				+ "アイウエオツヤユヨワカケオカキクケコサシスセソタチツテトハヒフヘホハヒフヘホイエフ" + NORMALIZE_DB_RUBY_COMMON_TO + NORMALIZE_DB_FORBIDDEN_TO;

		// NormalizeDbRubyForMusicInfo() 用：フリガナ正規化対象文字（半角カタカナ→全角カタカナ等）
		private const String NORMALIZE_DB_RUBY_FOR_MUSIC_INFO_FROM = "ゕゖ" + NORMALIZE_DB_RUBY_COMMON_FROM + NORMALIZE_DB_FORBIDDEN_FROM;
		private const String NORMALIZE_DB_RUBY_FOR_MUSIC_INFO_TO = "ヵヶ" + NORMALIZE_DB_RUBY_COMMON_TO + NORMALIZE_DB_FORBIDDEN_TO;

		// NormalizeDbRubyXXX() 用
		private const String NORMALIZE_DB_RUBY_COMMON_FROM = "~～\u301C";
		private const String NORMALIZE_DB_RUBY_COMMON_TO = "ーーー";

		// NormalizeDbString() 用：禁則文字（全角スペース、一部の半角文字等）
		private const String NORMALIZE_DB_STRING_FROM = "　\u2019ﾞﾟ｡｢｣､･~\u301C♯" + NORMALIZE_DB_FORBIDDEN_FROM;
		private const String NORMALIZE_DB_STRING_TO = " '゛゜。「」、・～～#" + NORMALIZE_DB_FORBIDDEN_TO;

		// NormalizeDbXXX() 用：変換後がフリガナ対象の禁則文字（半角カタカナ大文字）
		private const String NORMALIZE_DB_FORBIDDEN_FROM = "ｦｰｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝｧｨｩｪｫｯｬｭｮ";
		private const String NORMALIZE_DB_FORBIDDEN_TO = "ヲーアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワンァィゥェォッャュョ";

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// ID 接頭辞の最大長（同期サーバーデータベースの都合上、ID のトータル長が UTF-8 で 255 バイト以下になるようにする）
		private const Int32 ID_PREFIX_MAX_LENGTH = 20;

		// 暗号化キー（256 bit = 32 byte）
		private static readonly Byte[] ENCRYPT_KEY =
		{
			0x07, 0xC1, 0x19, 0x4A, 0x99, 0x9A, 0xF0, 0x2D, 0x0C, 0x52, 0xB0, 0x65, 0x48, 0xE6, 0x1F, 0x61,
			0x9C, 0x37, 0x9C, 0xA1, 0xC2, 0x31, 0xBA, 0xD1, 0x64, 0x1D, 0x85, 0x46, 0xCA, 0xF4, 0xE6, 0x5F,
		};

		// 暗号化 IV（128 bit = 16 byte）
		private static readonly Byte[] ENCRYPT_IV =
		{
			0x80, 0xB5, 0x40, 0x56, 0x9A, 0xE0, 0x3A, 0x9F, 0xd0, 0x90, 0xC6, 0x7C, 0xAA, 0xCD, 0xE7, 0x53,
		};

		// 頭文字変換用
		private const String HEAD_CONVERT_FROM = "ぁぃぅぇぉゕゖゃゅょゎゔがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゐゑ";
		private const String HEAD_CONVERT_TO = "あいうえおかけやゆよわうかきくけこさしすせそたちつてとはひふへほはひふへほいえ";

		// 同期詳細ログ
		private const String FILE_NAME_SYNC_DETAIL_LOG = YlConstants.APP_ID + YlConstants.SYNC_DETAIL_ID + Common.FILE_EXT_LOG;

		// 西暦 2 桁時の世紀の曖昧さ回避用
		private const Int32 YEAR_DISAMBIGUATION_DELTA = 10;

		// ====================================================================
		// private static メンバー変数
		// ====================================================================

		// InputIdPrefix
		private static Boolean _isInputtingIdPrefix;

		// TempPath() 用カウンター（同じスレッドでもファイル名が分かれるようにするため）
		private static Int32 _tempPathCounter;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ルビ正規化において複数対複数変換対象であれば追加
		// ＜返値＞ 追加したら true
		// --------------------------------------------------------------------
		private static Boolean AppendMulti(String str, ref Int32 pos, String[] multiFrom, String[] multiTo, StringBuilder katakana)
		{
			for (Int32 i = 0; i < multiFrom.Length; i++)
			{
				if (str[pos..].StartsWith(multiFrom[i], StringComparison.Ordinal))
				{
					katakana.Append(multiTo[i]);
					pos += multiFrom[i].Length - 1;
					return true;
				}
			}
			return false;
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
			sb.Append('^');
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
				else if (@".$^{[(|)*+?\".Contains(ruleInDisk[beginPos]))
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
			sb.Append('$');
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

		// --------------------------------------------------------------------
		// フォルダー固定値をマージ
		// --------------------------------------------------------------------
		private static void MargeFolderRules(FolderSettingsInMemory folderSettingsInMemory, Dictionary<String, String?> dic)
		{
			foreach (KeyValuePair<String, String?> folderRule in folderSettingsInMemory.FolderNameRules)
			{
				if (dic.ContainsKey(folderRule.Key) && String.IsNullOrEmpty(dic[folderRule.Key]))
				{
					dic[folderRule.Key] = folderRule.Value;
				}
			}
		}

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
		// ファイル名とファイル命名規則がマッチするか確認し、マッチしたマップを返す（ルビは検索用に正規化）
		// ＜引数＞ fileNameBody: 拡張子無し
		// --------------------------------------------------------------------
		private static Dictionary<String, String?> MatchFileNameRulesForSearch(String fileNameBody, FolderSettingsInMemory folderSettingsInMemory)
		{
			Dictionary<String, String?> dic = MatchFileNameRulesCore(fileNameBody, folderSettingsInMemory);

			// 正規化（差異部分のみ）
			(dic[YlConstants.RULE_VAR_TITLE_RUBY], _, _) = NormalizeDbRubyForSearch(dic[YlConstants.RULE_VAR_TITLE_RUBY]);

			return dic;
		}

		// --------------------------------------------------------------------
		// フリガナの表記揺れを減らす
		// ＜返値＞ フリガナ表記 or null（空になる場合）, 元の文字はすべてフリガナ対応文字だったか（null の場合は false）, 先頭文字がフリガナ対応文字だったか（null の場合は false）
		// --------------------------------------------------------------------
		private static (String? normalizedRuby, Boolean fromAllRuby, Boolean headRuby) NormalizeDbRubyCore(String? str, String oneFrom, String oneTo, String[] multiFrom, String[] multiTo)
		{
			if (String.IsNullOrEmpty(str))
			{
				return (null, false, false);
			}

			StringBuilder katakana = new();
			Boolean fromAllRuby = true;
			Boolean headRuby = true;

			for (Int32 i = 0; i < str.Length; i++)
			{
				// 複数対複数変換テーブルを用いた変換
				if (AppendMulti(str, ref i, multiFrom, multiTo, katakana))
				{
					continue;
				}

				Char chara = str[i];

				// 1 対 1 変換テーブルを用いた変換
				Int32 pos = oneFrom.IndexOf(chara);
				if (pos >= 0)
				{
					katakana.Append(oneTo[pos]);
					continue;
				}

				// 上記以外の全角カタカナ・音引きはそのまま
				if ('ァ' <= chara && chara <= 'ヺ' || chara == 'ー')
				{
					katakana.Append(chara);
					continue;
				}

				// 上記以外のひらがなをカタカナに変換
				if ('ぁ' <= chara && chara <= 'ゔ')
				{
					katakana.Append((Char)(chara + 0x60));
					continue;
				}

				// その他の文字は無視する
				fromAllRuby = false;
				if (i == 0)
				{
					headRuby = false;
				}
			}

			String katakanaString = katakana.ToString();
			if (String.IsNullOrEmpty(katakanaString))
			{
				return (null, false, false);
			}

			return (katakanaString, fromAllRuby, headRuby);
		}

		// --------------------------------------------------------------------
		// ID 接頭辞がユーザー入力中なら待つ
		// --------------------------------------------------------------------
		private static async Task WaitInputIdPrefix()
		{
			await Task.Run(() =>
			{
				while (_isInputtingIdPrefix)
				{
					Thread.Sleep(Common.GENERAL_SLEEP_TIME);
				}
			});
		}
	}
}
