<?php

// ============================================================================
// ゆかりすたー NEBULA 同期
// コントロールパネル：メイン処理
// Copyright (C) 2021 by SHINTA
// ============================================================================

// require
require_once('common_lib/CPCommon.php');
require_once('common_lib/CPManager.php');

// ログファイル作成
create_log_file();
create_log_file(FILE_NAME_SYNC_DETAIL_LOG);

// セッション処理
if ( !session_start() ) {
	error_die('セッションを開始できません。');
}

// 初期設定
log_message('===== '.SYSTEM_NAME.' '.SYSTEM_VER.' コントロールパネル開始。 ====='
		, LOG_LEVEL_STATUS, FALSE);
//error_reporting(E_ALL);	// 効かないみたい
mt_srand();
if ( DEBUG_FLAG ) {
	log_message('※デバッグモード', LOG_LEVEL_STATUS, FALSE);
}

// メイン
$cp_manager = new CPManager();
$cp_manager->run();

// ログ後処理
lotate_log();
lotate_log(FILE_NAME_SYNC_DETAIL_LOG);

?>