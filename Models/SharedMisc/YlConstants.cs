﻿// ============================================================================
// 
// ゆかりすたー NEBULA 共通で使用する定数
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;

using System;

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
		__End__
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
		Done,           // 完了
	}

	// --------------------------------------------------------------------
	// ゆかりすたー NEBULA の動作状況
	// --------------------------------------------------------------------
	public enum YukaListerStatus
	{
		Ready,      // 待機
		Running,    // 実行中
		Error,      // エラー
					//__End__
	}

	// ====================================================================
	// public デリゲート
	// ====================================================================

	// TargetFolderInfo.IsOpen が変更された
	public delegate void IsOpenChanged(TargetFolderInfo targetFolderInfo);

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
		// その他
		// --------------------------------------------------------------------

		// DPI
		public const Single DPI = 96.0f;
	}
}
