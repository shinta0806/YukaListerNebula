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
using System.Threading.Tasks;
using System.Windows.Media;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;

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
	// 楽曲情報データベースのテーブル
	// 値を増やした場合は、MainWindow.Initialize() でチェックしている定数も併せて増やす
	// --------------------------------------------------------------------
	public enum MusicInfoTables
	{
		TSong,
		TPerson,
		TTieUp,
		TCategory,
		TTieUpGroup,
		TMaker,
		TTag,
		TSongAlias,
		TPersonAlias,
		TTieUpAlias,
		TCategoryAlias,
		TTieUpGroupAlias,
		TMakerAlias,
		TArtistSequence,
		TLyristSequence,
		TComposerSequence,
		TArrangerSequence,
		TTieUpGroupSequence,
		TTagSequence,
		__End__,
	}

	// --------------------------------------------------------------------
	// リスト出力する項目（ほぼ TFound 準拠）
	// --------------------------------------------------------------------
	public enum OutputItems
	{
		Path,                   // フルパス
		FileName,               // ファイル名
		Head,                   // 頭文字
		Worker,                 // ニコカラ制作者
		Track,                  // トラック
		SmartTrack,             // スマートトラック
		Comment,                // 備考
		LastWriteTime,          // 最終更新日時
		FileSize,               // ファイルサイズ
		SongName,               // 楽曲名
		SongRuby,               // 楽曲フリガナ
		SongOpEd,               // 摘要
		SongReleaseDate,        // リリース日
		ArtistName,             // 歌手名
		ArtistRuby,             // 歌手フリガナ
		LyristName,             // 作詞者名
		LyristRuby,             // 作詞者フリガナ
		ComposerName,           // 作曲者名
		ComposerRuby,           // 作曲者フリガナ
		ArrangerName,           // 編曲者名
		ArrangerRuby,           // 編曲者フリガナ
		TieUpName,              // タイアップ名
		TieUpRuby,              // タイアップフリガナ
		TieUpAgeLimit,          // 年齢制限
		Category,               // カテゴリー
		TieUpGroupName,         // タイアップグループ名
		TieUpGroupRuby,         // タイアップグループフリガナ
		MakerName,              // 制作会社名
		MakerRuby,              // 制作会社フリガナ
		__End__,
	}

	// --------------------------------------------------------------------
	// ゆかりすたー NEBULA のどのパーツの動作状況を示すか
	// --------------------------------------------------------------------
	public enum YukaListerPartsStatusIndex
	{
		Environment,    // 環境系
		Startup,        // 起動時処理
		Sifolin,        // Sifolin
		Kamlin,         // Kamlin
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
	public delegate Task TaskAsyncDelegate<T>(T var) where T : class?;

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
		public const String APP_VER = "Ver 1.18 α";
		public const String COPYRIGHT_J = "Copyright (C) 2021 by SHINTA";

		// --------------------------------------------------------------------
		// フォルダー名
		// --------------------------------------------------------------------

		// ゆかりすたー用データベースを保存するフォルダー名
		public const String FOLDER_NAME_DATABASE = "Database\\";

		// リストテンプレートが保存されているフォルダー名
		public const String FOLDER_NAME_TEMPLATES = "Templates\\";

		// ゆかり用データベースを保存するフォルダー名
		public const String FOLDER_NAME_LIST = "list\\";

		// 各ドライブのゆかりすたー用ファイル群を保存するフォルダー名
		public const String FOLDER_NAME_YUKALISTER_STATUS = APP_ID + "Status\\";

		// --------------------------------------------------------------------
		// ファイル名
		// --------------------------------------------------------------------

		public const String FILE_NAME_NICO_KARA_LISTER_CONFIG = "NicoKaraLister" + Common.FILE_EXT_CONFIG;
		public const String FILE_NAME_YUKA_LISTER_CONFIG = APP_ID + Common.FILE_EXT_CONFIG;
		public const String FILE_NAME_YUKA_LISTER_EXCLUDE_CONFIG = APP_ID + "Exclude" + Common.FILE_EXT_CONFIG;
		public const String FILE_NAME_YUKARI_CONFIG = "config" + Common.FILE_EXT_INI;

		// --------------------------------------------------------------------
		// 拡張子
		// --------------------------------------------------------------------

		public const String FILE_EXT_SETTINGS_ARCHIVE = ".sta";

		// --------------------------------------------------------------------
		// ダイアログのフィルター
		// --------------------------------------------------------------------

		// 設定ファイル
		public const String DIALOG_FILTER_SETTINGS_ARCHIVE = "設定ファイル|*" + FILE_EXT_SETTINGS_ARCHIVE;

		// --------------------------------------------------------------------
		// 楽曲情報データベース
		// --------------------------------------------------------------------

		// 楽曲情報データベースの各テーブルの ID 第二接頭辞
		// この他、報告データベースで "R" を使用する
		public static readonly String[] MUSIC_INFO_ID_SECOND_PREFIXES =
		{
			"_S_", "_P_", "_T_","_C_", "_G_", "_M_", "_Z_",
			"_SA_", "_PA_", "_TA_","_CA_", "_GA_", "_MA_",
			String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty,
		};

		// 楽曲情報データベースのテーブル名
		public static readonly String[] MUSIC_INFO_DB_TABLE_NAMES =
		{
			TSong.TABLE_NAME_SONG, TPerson.TABLE_NAME_PERSON, TTieUp.TABLE_NAME_TIE_UP,
			TCategory.TABLE_NAME_CATEGORY, TTieUpGroup.TABLE_NAME_TIE_UP_GROUP, TMaker.TABLE_NAME_MAKER, TTag.TABLE_NAME_TAG,
			TSongAlias.TABLE_NAME_SONG_ALIAS, TPersonAlias.TABLE_NAME_PERSON_ALIAS, TTieUpAlias.TABLE_NAME_TIE_UP_ALIAS,
			TCategoryAlias.TABLE_NAME_CATEGORY_ALIAS, TTieUpGroupAlias.TABLE_NAME_TIE_UP_GROUP_ALIAS, TMakerAlias.TABLE_NAME_MAKER_ALIAS,
			TArtistSequence.TABLE_NAME_ARTIST_SEQUENCE, TLyristSequence.TABLE_NAME_LYRIST_SEQUENCE, TComposerSequence.TABLE_NAME_COMPOSER_SEQUENCE,
			TArrangerSequence.TABLE_NAME_ARRANGER_SEQUENCE, TTieUpGroupSequence.TABLE_NAME_TIE_UP_GROUP_SEQUENCE, TTagSequence.TABLE_NAME_TAG_SEQUENCE,
		};

		// 楽曲情報データベースのテーブル名（表示用）
		public static readonly String[] MUSIC_INFO_TABLE_NAME_LABELS =
		{
			"楽曲", "人物", "タイアップ", "カテゴリー", "シリーズ", "制作会社", "タグ",
			"楽曲別名", "人物別名", "タイアップ別名", "カテゴリー別名", "シリーズ別名", "制作会社別名",
			"歌手紐付", "作詞者紐付", "作曲者紐付", "編曲者紐付", "シリーズ紐付", "タグ紐付",
		};

		// 楽曲情報データベースのシステム ID 接頭辞（ユーザーは指定できない文字 '_' を含める）
		public const String MUSIC_INFO_SYSTEM_ID_PREFIX = "_SYS";

		// --------------------------------------------------------------------
		// 楽曲情報データベースカラム名
		// --------------------------------------------------------------------

		// IRcBase
		public const String FIELD_SUFFIX_ID = "id";
		public const String FIELD_SUFFIX_IMPORT = "import";
		public const String FIELD_SUFFIX_INVALID = "invalid";
		public const String FIELD_SUFFIX_UPDATE_TIME = "update_time";
		public const String FIELD_SUFFIX_DIRTY = "dirty";

		// IRcMaster
		public const String FIELD_SUFFIX_NAME = "name";
		public const String FIELD_SUFFIX_RUBY = "ruby";
		public const String FIELD_SUFFIX_RUBY_FOR_SEARCH = "ruby_for_search";
		public const String FIELD_SUFFIX_KEYWORD = "keyword";
		public const String FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH = "keyword_ruby_for_search";

		// IRcCategorizable
		public const String FIELD_SUFFIX_CATEGORY_ID = "category_id";
		public const String FIELD_SUFFIX_RELEASE_DATE = "release_date";

		// IRcAlias
		public const String FIELD_SUFFIX_ALIAS = "alias";
		public const String FIELD_SUFFIX_ORIGINAL_ID = "original_id";

		// IRcSequence
		public const String FIELD_SUFFIX_SEQUENCE = "sequence";
		public const String FIELD_SUFFIX_LINK_ID = "link_id";

		// TSong 独自項目
		public const String FIELD_SUFFIX_TIE_UP_ID = "tie_up_id";
		public const String FIELD_SUFFIX_OP_ED = "op_ed";

		// TTieUp 独自項目
		public const String FIELD_SUFFIX_MAKER_ID = "maker_id";
		public const String FIELD_SUFFIX_AGE_LIMIT = "age_limit";

		// --------------------------------------------------------------------
		// MessageKey
		// --------------------------------------------------------------------

		// 検索依頼
		public const String MESSAGE_KEY_FIND_KEYWORD = "FindKeyword";

		// メインウィンドウを開く
		public const String MESSAGE_KEY_OPEN_MAIN_WINDOW = "OpenMainWindow";

		// バージョン情報ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_ABOUT_WINDOW = "OpenAboutWindow";

		// マスター詳細編集ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_EDIT_MASTER_WINDOW = "OpenEditMasterWindow";

		// 楽曲情報等編集ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_EDIT_MUSIC_INFO_WINDOW = "OpenEditMusicInfoWindow";

		// 紐付編集ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_EDIT_SEQUENCE_WINDOW = "OpenEditSequenceWindow";

		// 楽曲詳細情報編集ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_EDIT_SONG_WINDOW = "OpenEditSongWindow";

		// タイアップ詳細情報編集ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_EDIT_TIE_UP_WINDOW = "OpenEditTieUpWindow";

		// 検索ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_FIND_KEYWORD_WINDOW = "OpenFindKeywordWindow";

		// フォルダー設定ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_FOLDER_SETTINGS_WINDOW = "OpenFolderSettingsWindow";

		// ID 接頭辞入力ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_INPUT_ID_PREFIX_WINDOW = "OpenInputIdPrefixWindow";

		// 開くダイアログを開く
		public const String MESSAGE_KEY_OPEN_OPEN_FILE_DIALOG = "OpenOpenFileDialog";

		// 保存ダイアログを開く
		public const String MESSAGE_KEY_OPEN_SAVE_FILE_DIALOG = "OpenSaveFileDialog";

		// 楽曲情報データベースマスター検索ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW = "OpenSearchMasterWindow";

		// ファイル一覧ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_VIEW_TFOUNDS_WINDOW = "OpenViewTFoundsWindow";

		// 環境設定ウィンドウを開く
		public const String MESSAGE_KEY_OPEN_YL_SETTINGS_WINDOW = "OpenYlSettingsWindow";

		// ウィンドウをアクティブ化する
		public const String MESSAGE_KEY_WINDOW_ACTIVATE = "Activate";

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
		// 出力設定
		// --------------------------------------------------------------------

		// 新着日数の最小値
		public const Int32 NEW_DAYS_MIN = 1;

		// enum.OutputItems の表示名
		public static readonly String[] OUTPUT_ITEM_NAMES = new String[] { "フルパス", "ファイル名", "頭文字", "ニコカラ制作者", "トラック", "スマートトラック",
				"備考", "最終更新日時", "ファイルサイズ", "楽曲名", "楽曲フリガナ", "摘要", "リリース日",
				"歌手名", "歌手フリガナ", "作詞者名", "作詞者フリガナ", "作曲者名", "作曲者フリガナ", "編曲者名", "編曲者フリガナ",
				"タイアップ名", "タイアップフリガナ", "年齢制限", "カテゴリー", "タイアップグループ名", "タイアップグループフリガナ", "制作会社名", "制作会社フリガナ" };

		// --------------------------------------------------------------------
		// 年齢制限
		// --------------------------------------------------------------------

		public const Int32 AGE_LIMIT_CERO_B = 12;
		public const Int32 AGE_LIMIT_CERO_C = 15;
		public const Int32 AGE_LIMIT_CERO_D = 17;
		public const Int32 AGE_LIMIT_CERO_Z = 18;

		// --------------------------------------------------------------------
		// Web サーバーコマンドオプション
		// --------------------------------------------------------------------

		public const String SERVER_OPTION_NAME_EASY_PASS = "easypass";
		public const String SERVER_OPTION_NAME_UID = "uid";
		public const String SERVER_OPTION_NAME_WIDTH = "width";

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
		public static readonly String[] FOLDER_SETTINGS_STATUS_LABELS = { "無", "有", "親に有", "未確認" };

		// グループの「その他」
		public const String GROUP_MISC = "その他";

		// 頭文字の「その他」
		public const String HEAD_MISC = GROUP_MISC;

		// タイアップグループ名を表示する際に末尾に付与する文字列
		public const String TIE_UP_GROUP_SUFFIX = "シリーズ";

		// SYNC_INFO_PARAM_DATE の日付フォーマット
		public const String SYNC_URL_DATE_FORMAT = "yyyyMMdd";

		// 日付の書式指定子
		public const String DATE_FORMAT = "yyyy/MM/dd";

		// 時刻の書式指定子
		public const String TIME_FORMAT = "HH:mm:ss";

		// 変数の値を区切る文字
		public const String VAR_VALUE_DELIMITER = ",";

		// RULE_VAR_ON_VOCAL / RULE_VAR_OFF_VOCAL のデフォルト値
		public const Int32 RULE_VALUE_VOCAL_DEFAULT = 1;

		// スマートトラックでトラック有りの場合の印
		public const String SMART_TRACK_VALID_MARK = "○";

		// スマートトラックでトラック無しの場合の印
		public const String SMART_TRACK_INVALID_MARK = "×";

		// これ以降の備考は Web リストに出力しない
		public const String WEB_LIST_IGNORE_COMMENT_DELIMITER = VAR_VALUE_DELIMITER + "//";

		// 一時的に付与する ID の接頭辞
		public const String TEMP_ID_PREFIX = "!";

		// DPI
		public const Single DPI = 96.0f;

		// 同期詳細ログ ID
		public const String SYNC_DETAIL_ID = "SyncDetail";

		// 検索の方向
		public const String FIND_DIRECTION_BACKWARD = "Backward";

		// Fantia
		public const String URL_FANTIA = "https://fantia.jp/fanclubs/65509";
	}
}
