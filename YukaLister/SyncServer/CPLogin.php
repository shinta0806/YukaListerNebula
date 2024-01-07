<?php

// ============================================================================
// ゆかりすたー NEBULA 同期
// コントロールパネル：ログイン処理
// Copyright (C) 2021 by SHINTA
// ============================================================================

// ----------------------------------------------------------------------------
// SyncServer のすべてのファイルは BOM 無とする
// BOM があるとダウンロードの際のゴミとなる
// ToDo: ログファイルの排他制御
// ----------------------------------------------------------------------------

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
log_message('===== '.SYSTEM_NAME.' '.SYSTEM_VER.' ログイン画面開始。 ====='
		, LOG_LEVEL_STATUS, FALSE);

// コントロールパネル（ログイン状態の判定用として使用）
$cp_manager = new CPManager();

// ログイン状態の判定
$not_logged_in_reason = 0;
if ( $cp_manager->is_logged_in($not_logged_in_reason) ) {
	// ログイン状態なのでメインへ転送
	header('Location:'.FILE_NAME_CP_MAIN);
} else {
	// 状態別に画面表示
	switch ( $not_logged_in_reason ) {
	case NOT_LOGGED_IN_NO_MASTER_ACCOUNT:
		show_create_master_account_form($cp_manager->error_message());
		break;
	case NOT_LOGGED_IN_NO_SESSION_INFO:
		show_login_form($cp_manager);
		break;
	default:
		error_die('内部エラー：未ログイン状態の区分が不正');
	}
}

// ログ後処理
lotate_log();
lotate_log(FILE_NAME_SYNC_DETAIL_LOG);

goto EOP;

// ----------------------------------------------------------------------------
// マスターアカウント作成フォームを表示
// ----------------------------------------------------------------------------
function	show_create_master_account_form($err)
{
	// コンテンツ作成
	$contents = file_get_contents(FOLDER_NAME_TEMPLATE.FILE_NAME_TEMPLATE_CREATE_MASTER_ACCOUNT_FORM);
	if ( $contents === FALSE ) {
		error_die('マスターアカウント作成フォームを表示できません。(1)');
	}

	// イントロメッセージ
	if ( $err == '' ) {
		$headline = '';
		$intro  = '<h1>ようこそ！</h1>';
		$intro .= '<p>'."\n";
		$intro .= 'ようこそ '.SYSTEM_NAME.' コントロールパネルへ<br><br>'."\n";
		$intro .= 'まず最初に、管理者アカウントを設定して下さい。'."\n";
		$intro .= '</p>'."\n";
	} else {
		$headline  = get_headline_error_html('アカウントを作成できませんでした。');
		$intro  = '<h1>アカウント作成エラー</h1>';
		$intro .= '<p>'."\n";
		$intro .= '<span class="Err">'.$err.'</span><br><br>'."\n";
		$intro .= '再度、管理者アカウントを設定して下さい。'."\n";
		$intro .= '</p>'."\n";
	}

	// テンプレート適用
	$vars = array();
	$vars[ATCV_CONTAINER_FILE] = FILE_NAME_TEMPLATE_CONTAINER_LOGIN;
	$vars[TMPL_MARK_HEADLINE] = $headline;
	$vars[TMPL_MARK_INTRO] = $intro;
	$output = apply_template_container($contents, $vars);
	if ( $output === FALSE ) {
		error_die('マスターアカウント作成フォームを表示できません。(2)');
	}
	echo($output);
}

// ----------------------------------------------------------------------------
// パスワード入力フォームを表示
// ----------------------------------------------------------------------------
function	show_login_form($cp_manager)
{
	// コンテンツ作成
	$contents = file_get_contents(FOLDER_NAME_TEMPLATE.FILE_NAME_TEMPLATE_LOGIN_FORM);
	if ( $contents === FALSE ) {
		error_die('ログインフォームを表示できません。(1)');
	}

	// イントロメッセージ
	$headline = '';
	if ( $cp_manager->error_message() != '' ) {
		$headline = get_headline_error_html('ログインできませんでした。');
		$intro  = '<h1>ログインエラー</h1>';
		$intro .= '<p>'."\n";
		$intro .= '<span class="Err">'.$cp_manager->error_message().'</span><br><br>'."\n";
		$intro .= '再度、コントロールパネルにログインするためのパスワードを入力して下さい。'."\n";
		$intro .= '</p>'."\n";
	} else {
		if ( $cp_manager->notice_message() != '' ) {
			$headline = get_headline_notice_html($cp_manager->notice_message());
		}
		$intro  = '<h1>ログイン</h1>';
		$intro .= '<p>'."\n";
		$intro .= SYSTEM_NAME.' コントロールパネルにログインするためのパスワードを入力して下さい。'."\n";
		$intro .= '</p>'."\n";
	}

	// テンプレート適用
	$vars = array();
	$vars[ATCV_CONTAINER_FILE] = FILE_NAME_TEMPLATE_CONTAINER_LOGIN;
	$vars[TMPL_MARK_HEADLINE] = $headline;
	$vars[TMPL_MARK_INTRO] = $intro;
	$output = apply_template_container($contents, $vars);
	if ( $output === FALSE ) {
		error_die('ログインフォームを表示できません。(2)');
	}
	echo($output);
}

EOP:

?>