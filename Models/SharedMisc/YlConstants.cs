// ============================================================================
// 
// ゆかりすたー NEBULA 共通で使用する定数
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Windows.Media;

namespace YukaLister.Models.SharedMisc
{
	// ====================================================================
	// public 列挙子
	// ====================================================================

	// --------------------------------------------------------------------
	// フォルダー除外設定の状態
	// --------------------------------------------------------------------
	public enum FolderExcludeSettingsStatus
	{
		False,      // 除外しない
		True,       // 除外する
		Unchecked,  // 未確認
					//__End__
	}

	// --------------------------------------------------------------------
	// フォルダー設定の状態
	// --------------------------------------------------------------------
	public enum FolderSettingsStatus
	{
		None,       // 設定ファイルが存在しない
		Set,        // 当該フォルダーに設定ファイルが存在する
		Inherit,    // 親フォルダーの設定を引き継ぐ
		Unchecked,  // 未確認
		__End__,
	}

	// --------------------------------------------------------------------
	// フォルダーに対する操作の詳細
	// --------------------------------------------------------------------
	public enum FolderTaskDetail
	{
		// 追加詳細
		CacheToDisk,    // キャッシュ DB からディスク DB へコピー　※親の場合のみなり得る
		FindSubFolders, // サブフォルダーの検索
		AddFileNames,    // 追加（ファイル名のみ）
		AddInfos,        // 追加（ファイルが追加されたレコードに対してその他の情報を付与）

		// 削除詳細
		Remove,         // 削除

		// 完了
		Done,           // 完了（ゆかり用データベースに反映されているとは限らない）
	}

	// --------------------------------------------------------------------
	// フォルダーに対する操作の種類
	// --------------------------------------------------------------------
	public enum FolderTaskKind
	{
		Add,            // 追加
		Remove,         // 削除
	}

	// --------------------------------------------------------------------
	// フォルダーに対する操作の動作状況
	// --------------------------------------------------------------------
	public enum FolderTaskStatus
	{
		Queued,         // 待機
		Running,        // 実行中
		Error,          // エラー
		DoneInMemory,   // 完了（インメモリデータベースへの反映）
		DoneInDisk,     // 完了（ゆかり用データベースへの反映）
	}

	// --------------------------------------------------------------------
	// ゆかりすたー NEBULA のどのパーツの動作状況を示すか
	// --------------------------------------------------------------------
	public enum YukaListerPartsStatusIndex
	{
		Environment,    // 環境系
		Startup,		// 起動時処理
		Sifolin,        // Sifolin
		__End__,
	}

	// --------------------------------------------------------------------
	// ゆかりすたー NEBULA の動作状況
	// --------------------------------------------------------------------
	public enum YukaListerStatus
	{
		Ready,      // 待機
		Running,    // 実行中
		Error,      // エラー
		__End__,
	}

	// ====================================================================
	// public デリゲート
	// ====================================================================

	// TargetFolderInfo.IsOpen が変更された
	public delegate void IsOpenChanged(TargetFolderInfo targetFolderInfo);

	// タスク非同期実行
	public delegate void TaskAsyncDelegate<T>(T var) where T : class?;

	public class YlConstants
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// --------------------------------------------------------------------
		// アプリの基本情報
		// --------------------------------------------------------------------
		public const String APP_ID = "YukaLister";
		public const String APP_GENERATION = "NEBULA";
		public const String APP_NAME_J = "ゆかりすたー " + APP_GENERATION + " ";
		public const String APP_VER = "Ver 0.01 α";
		public const String COPYRIGHT_J = "Copyright (C) 2021 by SHINTA";

		// --------------------------------------------------------------------
		// フォルダー名
		// --------------------------------------------------------------------

		// ゆかりすたー用データベースを保存するフォルダー名
		public const String FOLDER_NAME_DATABASE = "Database\\";

		// ゆかり用データベースを保存するフォルダー名
		public const String FOLDER_NAME_LIST = "list\\";

		// --------------------------------------------------------------------
		// ファイル名
		// --------------------------------------------------------------------
		public const String FILE_NAME_YUKA_LISTER_CONFIG = APP_ID + Common.FILE_EXT_CONFIG;
		public const String FILE_NAME_YUKA_LISTER_EXCLUDE_CONFIG = APP_ID + "Exclude" + Common.FILE_EXT_CONFIG;
		public const String FILE_NAME_YUKARI_CONFIG = "config" + Common.FILE_EXT_INI;

		// --------------------------------------------------------------------
		// MessageKey
		// --------------------------------------------------------------------

		// ウィンドウを閉じる
		public const String MESSAGE_KEY_WINDOW_CLOSE = "Close";

		// --------------------------------------------------------------------
		// アプリ独自ルールでの変数名（小文字で表記）
		// --------------------------------------------------------------------

		// 番組マスターにも同様の項目があるもの
		public const String RULE_VAR_CATEGORY = "category";
		public const String RULE_VAR_GAME_CATEGORY = "gamecategory";
		public const String RULE_VAR_PROGRAM = "program";
		//public const String RULE_VAR_PROGRAM_SUB = "programsub";
		//public const String RULE_VAR_NUM_STORIES = "numstories";
		public const String RULE_VAR_AGE_LIMIT = "agelimit";
		//public const String RULE_VAR_BEGINDATE = "begindate";

		// 楽曲マスターにも同様の項目があるもの
		public const String RULE_VAR_OP_ED = "oped";
		//public const String RULE_VAR_CAST_SEQ = "castseq";
		public const String RULE_VAR_TITLE = "title";
		public const String RULE_VAR_ARTIST = "artist";
		public const String RULE_VAR_TAG = "tag";

		// ファイル名からのみ取得可能なもの
		public const String RULE_VAR_TITLE_RUBY = "titleruby";
		public const String RULE_VAR_WORKER = "worker";
		public const String RULE_VAR_TRACK = "track";
		public const String RULE_VAR_ON_VOCAL = "onvocal";
		public const String RULE_VAR_OFF_VOCAL = "offvocal";
		//public const String RULE_VAR_COMPOSER = "composer";
		//public const String RULE_VAR_LYRIST = "lyrist";
		public const String RULE_VAR_COMMENT = "comment";

		// その他
		public const String RULE_VAR_ANY = "*";

		// 開始終了
		public const String RULE_VAR_BEGIN = "<";
		public const String RULE_VAR_END = ">";

		// --------------------------------------------------------------------
		// 状態色
		// --------------------------------------------------------------------

		// 待機中
		public static readonly Color COLOR_STATUS_QUEUED = Color.FromRgb(0xFA, 0xFA, 0xFA);

		// 動作中
		public static readonly Color COLOR_STATUS_RUNNING = Color.FromRgb(0xE1, 0xFF, 0xE1);

		// 完了
		public static readonly Color COLOR_STATUS_DONE = Color.FromRgb(0xE1, 0xE1, 0xFF);

		// エラー
		public static readonly Color COLOR_STATUS_ERROR = Color.FromRgb(0xFF, 0xE1, 0xE1);

		// 対象外
		public static readonly Color COLOR_EXCLUDE = Color.FromRgb(0xCC, 0xCC, 0xCC);

		// --------------------------------------------------------------------
		// 状態ブラシ
		// --------------------------------------------------------------------

		// 待機中
		public static readonly SolidColorBrush BRUSH_STATUS_QUEUED = new(COLOR_STATUS_QUEUED);

		// 動作中
		public static readonly SolidColorBrush BRUSH_STATUS_RUNNING = new(COLOR_STATUS_RUNNING);

		// 完了
		public static readonly SolidColorBrush BRUSH_STATUS_DONE = new(COLOR_STATUS_DONE);

		// エラー
		public static readonly SolidColorBrush BRUSH_STATUS_ERROR = new(COLOR_STATUS_ERROR);

		// 対象外
		public static readonly SolidColorBrush BRUSH_EXCLUDE = new(COLOR_EXCLUDE);

		// --------------------------------------------------------------------
		// 文字ブラシ
		// --------------------------------------------------------------------

		// 通常文字
		public static readonly SolidColorBrush BRUSH_NORMAL_STRING = new(Colors.Black);

		// エラー文字
		public static readonly SolidColorBrush BRUSH_ERROR_STRING = new(Colors.Red);

		// --------------------------------------------------------------------
		// 実行中
		// --------------------------------------------------------------------

		public const String RUNNING_CACHE_TO_DISK = "キャッシュ有効化中";
		public const String RUNNING_FIND_SUB_FOLDERS = "サブフォルダー検索中";
		public const String RUNNING_ADD_FILE_NAMES = "ファイル名確認中";
		public const String RUNNING_ADD_INFOS = "属性確認中";
		public const String RUNNING_REMOVE = "削除中";

		// --------------------------------------------------------------------
		// 日付関連
		// --------------------------------------------------------------------

		// 日付が指定されていない場合はこの年にする
		public const Int32 INVALID_YEAR = 1900;

		// 日付が指定されていない場合の修正ユリウス日
		public static readonly Double INVALID_MJD = JulianDay.DateTimeToModifiedJulianDate(new DateTime(INVALID_YEAR, 1, 1));

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// FolderSettingsStatus に対応する文字列
		public static readonly String[] FOLDER_SETTINGS_STATUS_TEXTS = { "無", "有", "親に有", "未確認" };

		// グループの「その他」
		public const String GROUP_MISC = "その他";

		// 頭文字の「その他」
		public const String HEAD_MISC = GROUP_MISC;

		// 変数の値を区切る文字
		public const String VAR_VALUE_DELIMITER = ",";

		// 一時的に付与する ID の接頭辞
		public const String TEMP_ID_PREFIX = "!";

		// DPI
		public const Single DPI = 96.0f;
	}
}
