<?php

// ============================================================================
// ゆかりすたー NEBULA 同期
// コントロールパネル：共通モジュール
// Copyright (C) 2021 by SHINTA
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

// require
require_once(dirname(__FILE__).'/../settings/DatabaseSettings.php');
require_once(dirname(__FILE__).'/../settings/DebugFlag.php');

// 定数定義
define('ACCOUNT_NAME_MAX_LENGTH', 20);
define('ACCOUNT_PASSWORD_MIN_LENGTH', 4);
define('ATCV_CONTAINER_FILE', 'ContainerFile');
define('COOKIE_NAME_PHP_SESSION_ID', 'PHPSESSID');
define('CREATE_TABLE_OPTIONS', 'CHARACTER SET utf8 COLLATE utf8_bin');
define('FIELD_NAME_SONG_PREFIX', 'song_');
define('FIELD_NAME_PERSON_PREFIX', 'person_');
define('FIELD_NAME_TIE_UP_PREFIX', 'tie_up_');
define('FIELD_NAME_CATEGORY_PREFIX', 'category_');
define('FIELD_NAME_TIE_UP_GROUP_PREFIX', 'tie_up_group_');
define('FIELD_NAME_MAKER_PREFIX', 'maker_');
define('FIELD_NAME_SONG_ALIAS_PREFIX', 'song_alias_');
define('FIELD_NAME_TIE_UP_ALIAS_PREFIX', 'tie_up_alias_');
define('FIELD_NAME_ARTIST_SEQUENCE_PREFIX', 'artist_sequence_');
define('FIELD_NAME_LYRIST_SEQUENCE_PREFIX', 'lyrist_sequence_');
define('FIELD_NAME_COMPOSER_SEQUENCE_PREFIX', 'composer_sequence_');
define('FIELD_NAME_ARRANGER_SEQUENCE_PREFIX', 'arranger_sequence_');
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX', 'tie_up_group_sequence_');
define('FIELD_NAME_ACCOUNT_PREFIX', 'account_');
define('FIELD_NAME_LOGIN_PREFIX', 'login_');
define('FIELD_NAME_STATISTICS_PREFIX', 'statistics_');
define('FIELD_NAME_SUFFIX_ACCEPTS', 'accepts');
define('FIELD_NAME_SUFFIX_ADMIN', 'admin');
define('FIELD_NAME_SUFFIX_AGE_LIMIT', 'age_limit');
define('FIELD_NAME_SUFFIX_ALIAS', 'alias');
define('FIELD_NAME_SUFFIX_APP_GENERATION', 'app_generation');
define('FIELD_NAME_SUFFIX_APP_VER', 'app_ver');
define('FIELD_NAME_SUFFIX_BYTES', 'bytes');
define('FIELD_NAME_SUFFIX_CATEGORY_ID', 'category_id');
define('FIELD_NAME_SUFFIX_ID', 'id');
define('FIELD_NAME_SUFFIX_ID_PREFIX', 'id_prefix');
define('FIELD_NAME_SUFFIX_IMPORT', 'import');
define('FIELD_NAME_SUFFIX_INVALID', 'invalid');
define('FIELD_NAME_SUFFIX_KEYWORD', 'keyword');
define('FIELD_NAME_SUFFIX_KEYWORD_RUBY_FOR_SEARCH', 'keyword_ruby_for_search');
define('FIELD_NAME_SUFFIX_LINK_ID', 'link_id');
define('FIELD_NAME_SUFFIX_LOGIN_TIME', 'login_time');
define('FIELD_NAME_SUFFIX_MAKER_ID', 'maker_id');
define('FIELD_NAME_SUFFIX_MONTH', 'month');
define('FIELD_NAME_SUFFIX_NAME', 'name');
define('FIELD_NAME_SUFFIX_OP_ED', 'op_ed');
define('FIELD_NAME_SUFFIX_ORIGINAL_ID', 'original_id');
define('FIELD_NAME_SUFFIX_REJECTS', 'rejects');
define('FIELD_NAME_SUFFIX_RELEASE_DATE', 'release_date');
define('FIELD_NAME_SUFFIX_RUBY', 'ruby');
define('FIELD_NAME_SUFFIX_RUBY_FOR_SEARCH', 'ruby_for_search');
define('FIELD_NAME_SUFFIX_PASSWORD', 'password');
define('FIELD_NAME_SUFFIX_PERMISSION', 'permission');
define('FIELD_NAME_SUFFIX_SEQUENCE', 'sequence');
define('FIELD_NAME_SUFFIX_SID', 'sid');
define('FIELD_NAME_SUFFIX_SUCCESS', 'success');
define('FIELD_NAME_SUFFIX_SYNC_BY', 'sync_by');
define('FIELD_NAME_SUFFIX_TIE_UP_ID', 'tie_up_id');
define('FIELD_NAME_SUFFIX_TIME', 'time');
define('FIELD_NAME_SUFFIX_UID', 'uid');
define('FIELD_NAME_SUFFIX_UPDATE_TIME', 'update_time');
define('FILE_NAME_CP_LOGIN', 'CPLogin.php');
define('FILE_NAME_CP_MAIN', 'CPMain.php');
define('FILE_NAME_DEFAULT_CP_LOG', 'Log_CP.php');
define('FILE_NAME_DOWNLOAD_PREFIX', 'SyncDown_');
define('FILE_NAME_SYNC_DETAIL_LOG', 'Log_SyncDetail.php');
define('FILE_NAME_SYNC_INFO', 'SyncInfo.txt');
define('FILE_NAME_UPLOAD_PREFIX', 'SyncUp_');
define('FILE_NAME_TEMPLATE_CHANGE_CURRENT_PASSWORD_FORM', 'TemplateChangeCurrentPWForm.html');
define('FILE_NAME_TEMPLATE_CONTAINER_LOGIN', 'TemplateContainerLogin.html');
define('FILE_NAME_TEMPLATE_CONTAINER_MAIN', 'TemplateContainerMain.html');
define('FILE_NAME_TEMPLATE_CREATE_MASTER_ACCOUNT_FORM', 'TemplateCreateMasterAccountForm.html');
define('FILE_NAME_TEMPLATE_LOGIN_FORM', 'TemplateLoginForm.html');
define('FILE_NAME_TEMPLATE_MANAGE_USERS', 'TemplateManageUsers.html');
define('FILE_NAME_TEMPLATE_VIEW_LOG', 'TemplateViewLog.html');
define('FILE_NAME_TEMPLATE_VIEW_NUM_COMMIT', 'TemplateViewNumCommit.html');
define('FILE_NAME_TEMPLATE_VIEW_NUM_DATA', 'TemplateViewNumData.html');
define('FILE_NAME_TEMPLATE_VIEW_STATUS', 'TemplateViewStatus.html');
define('FILE_NAME_TEMPLATE_UPLOAD_TEST', 'TemplateUploadTest.html');
define('FOLDER_NAME_DATA', 'data/');
define('FOLDER_NAME_SETTINGS', 'settings/');
define('FOLDER_NAME_TEMPLATE', 'template/');
define('HEADER_CONTENT_DISPOSITION', 'Content-Disposition: attachment; filename=');
define('HEADER_CONTENT_LENGTH', 'Content-Length: ');
define('HEADER_CONTENT_TYPE', 'Content-Type: application/force-download');
define('HEADLINE_TIME_FORMAT', 'H:i:s');
define('INVALID_YEAR', 1900);
define('INVALID_MJD', 15020);
define('LOG_FILE_SIZE_MAX', 1*1024*1024);
define('LOG_LEVEL_DEBUG', 0);	// デバッグ用ログ（DEBUG_FLAG オンの場合のみ）
define('LOG_LEVEL_STATUS', 1);	// 動作状況（通常運転）
define('LOG_LEVEL_NOTICE', 2);	// ユーザーへの報告事項
define('LOG_LEVEL_WARNING', 3);	// 警告（今回の動作には乗り切れる）
define('LOG_LEVEL_ERROR', 4);	// エラー（目的の動作ができない）
define('MODE_NAME_CHANGE_ACCOUNT', 'ChangeAccount');
define('MODE_NAME_CHANGE_CURRENT_PASSWORD', 'ChangeCurrentPW');
define('MODE_NAME_CHANGE_CURRENT_PASSWORD_FORM', 'ChangeCurrentPWForm');
define('MODE_NAME_DELETE_ACCOUNT', 'DeleteAccount');
define('MODE_NAME_DOWNLOAD_POST_ERROR', 'DownloadPostError');
define('MODE_NAME_DOWNLOAD_REJECT_DATE', 'DownloadRejectDate');
define('MODE_NAME_DOWNLOAD_SYNC_DATA', 'DownloadSyncData');
define('MODE_NAME_INIT_ACCOUNT', 'InitAccount');
define('MODE_NAME_MANAGE_USERS', 'ManageUsers');
define('MODE_NAME_LOGIN', 'Login');
define('MODE_NAME_LOGOUT', 'Logout');
define('MODE_NAME_NEW_ACCOUNT', 'NewAccount');
define('MODE_NAME_UPLOAD_SYNC_DATA', 'UploadSyncData');
define('MODE_NAME_UPLOAD_TEST', 'UploadTest');
define('MODE_NAME_VIEW_LOG', 'ViewLog');
define('MODE_NAME_VIEW_NUM_COMMIT', 'ViewNumCommit');
define('MODE_NAME_VIEW_NUM_DATA', 'ViewNumData');
define('MODE_NAME_VIEW_STATUS', 'ViewStatus');
define('NO_DATA', 'NoData');
define('NOT_LOGGED_IN_NO_MASTER_ACCOUNT', 1);
define('NOT_LOGGED_IN_NO_SESSION_INFO', 2);
define('OLD_LOG_MAX_GENERATION', 10);
define('OLD_LOG_SUFFIX', '_Old_');
define('PARAM_NAME_ADMIN', 'Admin');
define('PARAM_NAME_APP_GENERATION', 'AppGeneration');
define('PARAM_NAME_APP_VER', 'AppVer');
define('PARAM_NAME_CURRENT_PASSWORD', 'CurrentPW');
define('PARAM_NAME_DATE', 'Date');
define('PARAM_NAME_ERROR_MESSAGE', 'MsgE');
define('PARAM_NAME_FILE', 'File');
define('PARAM_NAME_ID_PREFIX', 'IdPrefix');
define('PARAM_NAME_MODE', 'Mode');
define('PARAM_NAME_NAME', 'Name');
define('PARAM_NAME_NEW_PASSWORD', 'NewPW');
define('PARAM_NAME_NEW_PASSWORD_CONFIRM', 'NewPWConfirm');
define('PARAM_NAME_NOTICE_MESSAGE', 'MsgN');
define('PARAM_NAME_PASSWORD', 'PW');
define('PARAM_NAME_SID', 'Sid');
define('PARAM_NAME_UID', 'Uid');
define('PERMISSION_DOWNLOAD', 1);
define('PERMISSION_UPLOAD', 2);
define('PHP_FOOTER', '?>');
define('PHP_HEADER', '<?php');
define('PTB_BOT', '$data = <<<');
define('PTB_EOT', '___PHP_Text_Bridge_EOT___');
define('RUN_TIME_DATE_FORMAT', 'Y/m/d H:i:s O');
define('SESSION_INFO_ACCOUNT_UID', 'AccountUid');
define('SESSION_INFO_ACCOUNT_NAME', 'AccountName');
define('SESSION_INFO_ACCOUNT_ADMIN', 'AccountAdmin');
define('SESSION_INFO_ACCOUNT_LOGIN_TIME', 'AccountLoginTime');
define('SESSION_INFO_POST_ERROR_EXISTS', 'PostErrorExists');
define('SESSION_INFO_POST_ERROR_MESSAGE', 'PostErrorMessage');
define('SESSION_INFO_REJECT_UPDATE_TIME', 'RejectUpdateTime');
define('SYSTEM_NAME', 'ゆかりすたー NEBULA 同期');
define('SYSTEM_APP_GENERATION', 'NEBULA');
define('SYSTEM_VER', 'Ver 3.00');
define('SYSTEM_COPYRIGHT', 'Copyright (C) 2021 by SHINTA');
define('TABLE_NAME_SONG', 't_song');
define('TABLE_NAME_PERSON', 't_person');
define('TABLE_NAME_TIE_UP', 't_tie_up');
define('TABLE_NAME_CATEGORY', 't_category');
define('TABLE_NAME_TIE_UP_GROUP', 't_tie_up_group');
define('TABLE_NAME_MAKER', 't_maker');
define('TABLE_NAME_SONG_ALIAS', 't_song_alias');
define('TABLE_NAME_TIE_UP_ALIAS', 't_tie_up_alias');
define('TABLE_NAME_ARTIST_SEQUENCE', 't_artist_sequence');
define('TABLE_NAME_LYRIST_SEQUENCE', 't_lyrist_sequence');
define('TABLE_NAME_COMPOSER_SEQUENCE', 't_composer_sequence');
define('TABLE_NAME_ARRANGER_SEQUENCE', 't_arranger_sequence');
define('TABLE_NAME_TIE_UP_GROUP_SEQUENCE', 't_tie_up_group_sequence');
define('TABLE_NAME_ACCOUNT', 't_account');
define('TABLE_NAME_LOGIN', 't_login');
define('TABLE_NAME_STATISTICS', 't_statistics');
define('TMPL_MARK_ADD_HEADER', '<!-- $AddHeader$ -->');
define('TMPL_MARK_BODY_PROPERTY', '<!-- $BodyProperty$ -->');
define('TMPL_MARK_CP_MAIN_PARAM', '<!-- $CPMainParam$ -->');
define('TMPL_MARK_CP_TOP_PATH', '<!-- $CPTopPath$ -->');
define('TMPL_MARK_FUNCTION_GET_USER_INFO', '<!-- $FunctionGetUserInfo$ -->');
define('TMPL_MARK_HEADLINE', '<!-- $Headline$ -->');
define('TMPL_MARK_INDENT','<!-- $Indent$ -->');
define('TMPL_MARK_INTRO', '<!-- $Intro$ -->');
define('TMPL_MARK_LOG', '<!-- $Log$ -->');
define('TMPL_MARK_MAIN_CONTENTS', '<!-- $MainContents$ -->');
define('TMPL_MARK_MENU_ADMIN', '<!-- $MenuAdmin$ -->');
define('TMPL_MARK_NUM_COMMIT', '<!-- $NumCommit$ -->');
define('TMPL_MARK_NUM_DATA', '<!-- $NumData$ -->');
define('TMPL_MARK_PHP_INFO', '<!-- $PHPInfo$ -->');
define('TMPL_MARK_PROGRAM_INFO', '<!-- $ProgramInfo$ -->');
define('TMPL_MARK_SYNC_DETAIL_LOG', '<!-- $SyncDetailLog$ -->');
define('TMPL_MARK_TIME_ZONE', '<!-- $TimeZone$ -->');
define('TMPL_MARK_TITLE_DETAIL', '<!-- $TitleDetail$ -->');
define('TMPL_MARK_TITLE_DETAIL_PREFIX', '<!-- $TitleDetailPrefix$ -->');
define('TMPL_MARK_USER_LIST', '<!-- $UserList$ -->');

// 定数定義（他に依存するもの）
define('FIELD_NAME_SONG_ID', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_SONG_NAME', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_SONG_RUBY', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_RUBY);
define('FIELD_NAME_SONG_RELEASE_DATE', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_RELEASE_DATE);
define('FIELD_NAME_SONG_TIE_UP_ID', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_TIE_UP_ID);
define('FIELD_NAME_SONG_CATEGORY_ID', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_CATEGORY_ID);
define('FIELD_NAME_SONG_OP_ED', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_OP_ED);
define('FIELD_NAME_SONG_KEYWORD', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_KEYWORD);
define('FIELD_NAME_SONG_IMPORT', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_SONG_INVALID', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_SONG_UPDATE_TIME', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_SONG_SYNC_BY', FIELD_NAME_SONG_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_PERSON_ID', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_PERSON_NAME', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_PERSON_RUBY', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_RUBY);
define('FIELD_NAME_PERSON_KEYWORD', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_KEYWORD);
define('FIELD_NAME_PERSON_IMPORT', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_PERSON_INVALID', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_PERSON_UPDATE_TIME', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_PERSON_SYNC_BY', FIELD_NAME_PERSON_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_TIE_UP_ID', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_TIE_UP_NAME', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_TIE_UP_RUBY', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_RUBY);
define('FIELD_NAME_TIE_UP_CATEGORY_ID', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_CATEGORY_ID);
define('FIELD_NAME_TIE_UP_MAKER_ID', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_MAKER_ID);
define('FIELD_NAME_TIE_UP_AGE_LIMIT', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_AGE_LIMIT);
define('FIELD_NAME_TIE_UP_RELEASE_DATE', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_RELEASE_DATE);
define('FIELD_NAME_TIE_UP_KEYWORD', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_KEYWORD);
define('FIELD_NAME_TIE_UP_IMPORT', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_TIE_UP_INVALID', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_TIE_UP_UPDATE_TIME', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_TIE_UP_SYNC_BY', FIELD_NAME_TIE_UP_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_CATEGORY_ID', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_CATEGORY_NAME', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_CATEGORY_RUBY', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_RUBY);
define('FIELD_NAME_CATEGORY_KEYWORD', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_KEYWORD);
define('FIELD_NAME_CATEGORY_IMPORT', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_CATEGORY_INVALID', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_CATEGORY_UPDATE_TIME', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_CATEGORY_SYNC_BY', FIELD_NAME_CATEGORY_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_TIE_UP_GROUP_ID', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_TIE_UP_GROUP_NAME', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_TIE_UP_GROUP_RUBY', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_RUBY);
define('FIELD_NAME_TIE_UP_GROUP_KEYWORD', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_KEYWORD);
define('FIELD_NAME_TIE_UP_GROUP_IMPORT', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_TIE_UP_GROUP_INVALID', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_TIE_UP_GROUP_UPDATE_TIME', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_TIE_UP_GROUP_SYNC_BY', FIELD_NAME_TIE_UP_GROUP_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_MAKER_ID', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_MAKER_NAME', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_MAKER_RUBY', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_RUBY);
define('FIELD_NAME_MAKER_KEYWORD', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_KEYWORD);
define('FIELD_NAME_MAKER_IMPORT', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_MAKER_INVALID', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_MAKER_UPDATE_TIME', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_MAKER_SYNC_BY', FIELD_NAME_MAKER_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_SONG_ALIAS_ID', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_SONG_ALIAS_ALIAS', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_ALIAS);
define('FIELD_NAME_SONG_ALIAS_ORIGINAL_ID', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_ORIGINAL_ID);
define('FIELD_NAME_SONG_ALIAS_IMPORT', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_SONG_ALIAS_INVALID', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_SONG_ALIAS_UPDATE_TIME', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_SONG_ALIAS_SYNC_BY', FIELD_NAME_SONG_ALIAS_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_TIE_UP_ALIAS_ID', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_TIE_UP_ALIAS_ALIAS', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_ALIAS);
define('FIELD_NAME_TIE_UP_ALIAS_ORIGINAL_ID', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_ORIGINAL_ID);
define('FIELD_NAME_TIE_UP_ALIAS_IMPORT', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_TIE_UP_ALIAS_INVALID', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_TIE_UP_ALIAS_UPDATE_TIME', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_TIE_UP_ALIAS_SYNC_BY', FIELD_NAME_TIE_UP_ALIAS_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_ARTIST_SEQUENCE_ID', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_ARTIST_SEQUENCE_SEQUENCE', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SEQUENCE);
define('FIELD_NAME_ARTIST_SEQUENCE_LINK_ID', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_LINK_ID);
define('FIELD_NAME_ARTIST_SEQUENCE_IMPORT', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_ARTIST_SEQUENCE_INVALID', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_ARTIST_SEQUENCE_UPDATE_TIME', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_ARTIST_SEQUENCE_SYNC_BY', FIELD_NAME_ARTIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_LYRIST_SEQUENCE_ID', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_LYRIST_SEQUENCE_SEQUENCE', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SEQUENCE);
define('FIELD_NAME_LYRIST_SEQUENCE_LINK_ID', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_LINK_ID);
define('FIELD_NAME_LYRIST_SEQUENCE_IMPORT', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_LYRIST_SEQUENCE_INVALID', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_LYRIST_SEQUENCE_UPDATE_TIME', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_LYRIST_SEQUENCE_SYNC_BY', FIELD_NAME_LYRIST_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_COMPOSER_SEQUENCE_ID', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_COMPOSER_SEQUENCE_SEQUENCE', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SEQUENCE);
define('FIELD_NAME_COMPOSER_SEQUENCE_LINK_ID', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_LINK_ID);
define('FIELD_NAME_COMPOSER_SEQUENCE_IMPORT', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_COMPOSER_SEQUENCE_INVALID', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_COMPOSER_SEQUENCE_UPDATE_TIME', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_COMPOSER_SEQUENCE_SYNC_BY', FIELD_NAME_COMPOSER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_ARRANGER_SEQUENCE_ID', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_ARRANGER_SEQUENCE_SEQUENCE', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SEQUENCE);
define('FIELD_NAME_ARRANGER_SEQUENCE_LINK_ID', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_LINK_ID);
define('FIELD_NAME_ARRANGER_SEQUENCE_IMPORT', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_ARRANGER_SEQUENCE_INVALID', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_ARRANGER_SEQUENCE_UPDATE_TIME', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_ARRANGER_SEQUENCE_SYNC_BY', FIELD_NAME_ARRANGER_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_ID', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_ID);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_SEQUENCE', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SEQUENCE);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_LINK_ID', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_LINK_ID);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_IMPORT', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_IMPORT);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_INVALID', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_INVALID);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_UPDATE_TIME', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_TIE_UP_GROUP_SEQUENCE_SYNC_BY', FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX.FIELD_NAME_SUFFIX_SYNC_BY);
define('FIELD_NAME_ACCOUNT_UID', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_UID);
define('FIELD_NAME_ACCOUNT_NAME', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_ACCOUNT_PASSWORD', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_PASSWORD);
define('FIELD_NAME_ACCOUNT_ADMIN', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_ADMIN);
define('FIELD_NAME_ACCOUNT_LOGIN_TIME', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_LOGIN_TIME);
define('FIELD_NAME_ACCOUNT_PERMISSION', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_PERMISSION);
define('FIELD_NAME_ACCOUNT_UPDATE_TIME', FIELD_NAME_ACCOUNT_PREFIX.FIELD_NAME_SUFFIX_UPDATE_TIME);
define('FIELD_NAME_LOGIN_NAME', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_NAME);
define('FIELD_NAME_LOGIN_TIME', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_TIME);
define('FIELD_NAME_LOGIN_SUCCESS', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_SUCCESS);
define('FIELD_NAME_LOGIN_APP_GENERATION', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_APP_GENERATION);
define('FIELD_NAME_LOGIN_APP_VER', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_APP_VER);
define('FIELD_NAME_LOGIN_ID_PREFIX', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_ID_PREFIX);
define('FIELD_NAME_LOGIN_SID', FIELD_NAME_LOGIN_PREFIX.FIELD_NAME_SUFFIX_SID);
define('FIELD_NAME_STATISTICS_UID', FIELD_NAME_STATISTICS_PREFIX.FIELD_NAME_SUFFIX_UID);
define('FIELD_NAME_STATISTICS_MONTH', FIELD_NAME_STATISTICS_PREFIX.FIELD_NAME_SUFFIX_MONTH);
define('FIELD_NAME_STATISTICS_BYTES', FIELD_NAME_STATISTICS_PREFIX.FIELD_NAME_SUFFIX_BYTES);
define('FIELD_NAME_STATISTICS_ACCEPTS', FIELD_NAME_STATISTICS_PREFIX.FIELD_NAME_SUFFIX_ACCEPTS);
define('FIELD_NAME_STATISTICS_REJECTS', FIELD_NAME_STATISTICS_PREFIX.FIELD_NAME_SUFFIX_REJECTS);


// ----------------------------------------------------------------------------
// FILE_TMPL_CONTAINER に内容を当てはめて文字列を生成
// ＜引数＞
// $contents: コンテンツ
// $vars で受け付ける値（$vars 自体は必須だが、個々の要素はオプショナル）
//   $vars[ATCV_CONTAINER_FILE]: テンプレートコンテナファイル名　※必須
//   $vars[TMPL_MARK_xxxx]: テンプレートマークを文字列に置換
// ＜返値＞　生成文字列（失敗時は FALSE）
// ----------------------------------------------------------------------------
function	apply_template_container(&$contents, &$vars)
{
	// テンプレートのロード
	$tmpl_contents_container = file_get_contents(FOLDER_NAME_TEMPLATE.$vars[ATCV_CONTAINER_FILE]);
	if ( $tmpl_contents_container === FALSE ) {
		return FALSE;
	}

	// コンテンツ置換
	$tmpl_contents_container = str_replace(TMPL_MARK_MAIN_CONTENTS, $contents
			, $tmpl_contents_container);

	// 指定されていない場合は空欄に置換するテンプレートマーク
	$std_tmpl_marks = array(
			TMPL_MARK_ADD_HEADER,		// <head></head> の間に追加する内容
			TMPL_MARK_BODY_PROPERTY,	// <body> タグのオプション
			TMPL_MARK_CP_TOP_PATH,		// トップページへの相対パス
			TMPL_MARK_TITLE_DETAIL,		// タイトル詳細
			);
	foreach ( $std_tmpl_marks as $tmpl_mark ) {
		if ( !isset($vars[$tmpl_mark]) ) {
			$vars[$tmpl_mark] = '';
		}
	}
	$vars[TMPL_MARK_INDENT] = '&nbsp;　&nbsp;　';
	if ( $vars[TMPL_MARK_TITLE_DETAIL] == '' ) {
		$vars[TMPL_MARK_TITLE_DETAIL_PREFIX] = '';
	} else {
		$vars[TMPL_MARK_TITLE_DETAIL_PREFIX] = ' ▷ ';
	}

	// テンプレートマークの置換
	foreach ( $vars as $key => $value ) {
		if ( $key == ATCV_CONTAINER_FILE ) {
			continue;
		}
		$tmpl_contents_container = str_replace($key, $value, $tmpl_contents_container);
	}

	// 返す
	return $tmpl_contents_container;
}

// -------------------------------------------------------------------
// 改行文字を全て "\n" に統一する
// ＜引数＞ $src: 変換前の文字列
// ＜返値＞ 変換後の文字列
// -------------------------------------------------------------------
function	convert_cr_lf($src)
{
	$src = str_replace("\r\n", "\n", $src);
	$src = str_replace("\r", "\n", $src);
	return $src;
}

// ----------------------------------------------------------------------------
// ログファイルが無ければ作成する
// ----------------------------------------------------------------------------
function	create_log_file($log_file_name = FILE_NAME_DEFAULT_CP_LOG)
{
	if ( file_exists(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$log_file_name) ) {
		return;
	}
	$save_contents  = PHP_HEADER."\n";
	$save_contents .= PTB_BOT.PTB_EOT."\n";
	if ( !file_put_contents(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$log_file_name
			, $save_contents) ) {
		log_message('ログを作成できませんでした：'.$log_file_name, LOG_LEVEL_WARNING);
	}
}

// ----------------------------------------------------------------------------
// エラーメッセージを表示して強制終了
// ----------------------------------------------------------------------------
function	error_die($message)
{
	if ( DEBUG_FLAG ) {
		var_dump(debug_backtrace());
	}
	log_message($message, LOG_LEVEL_ERROR);
	log_message('プログラムを中断します。', LOG_LEVEL_ERROR);
	die();
}

// -------------------------------------------------------------------
// HTML 的に危険な文字をエスケープする＆改行を \n に統一
// ＜引数＞ $src: フォームなどで入力されたパラメーター
// ＜返値＞ 安全に変換されたパラメーター
// -------------------------------------------------------------------
function	escape_input($src)
{
	return htmlspecialchars(convert_cr_lf($src), ENT_QUOTES);
}

// -------------------------------------------------------------------
// ヘッドラインに表示するエラーメッセージの HTML を生成
// ＜引数＞ $message: メッセージ
// ＜返値＞ HTML
// -------------------------------------------------------------------
function	get_headline_error_html($message)
{
	return '<div class="HeadlineErr">'.$message.' ('.date(HEADLINE_TIME_FORMAT).')</div>';
}

// -------------------------------------------------------------------
// ヘッドラインに表示する報告メッセージの HTML を生成
// ＜引数＞ $message: メッセージ
// ＜返値＞ HTML
// -------------------------------------------------------------------
function	get_headline_notice_html($message)
{
	return '<div class="HeadlineNotice">'.$message.' ('.date(HEADLINE_TIME_FORMAT).')</div>';
}

// ----------------------------------------------------------------------------
// ログメッセージの画面表示用 HTML を取得
// ----------------------------------------------------------------------------
function	get_log_message_html($message, $level)
{
	switch ( $level ) {
	case LOG_LEVEL_WARNING:
		$message = '【警告】　'.$message;
		break;
	case LOG_LEVEL_ERROR:
		$message = '<b>【エラー】　'.$message.'</b>';
		break;
	}
	$html = '<font color=';
	switch ( $level ) {
	case LOG_LEVEL_STATUS:
		$html .= '"LightGray"';
		break;
	case LOG_LEVEL_NOTICE:
		$html .= '"Black"';
		break;
	case LOG_LEVEL_WARNING:
		$html .= '"DarkGreen"';
		break;
	case LOG_LEVEL_ERROR:
		$html .= '"Red"';
		break;
	}
	$html .= '>'.$message.'</font>';
	return $html;
}

// -------------------------------------------------------------------
// 古いログファイル名を取得
// ＜引数＞ $log_file_name: 現行のログファイル名
// ＜返値＞ 古いログファイル名
// -------------------------------------------------------------------
function	get_old_log_file_name($log_file_name, $generation)
{
	return pathinfo($log_file_name, PATHINFO_FILENAME).OLD_LOG_SUFFIX
			.sprintf('%02d', $generation).'.'
			.pathinfo($log_file_name, PATHINFO_EXTENSION);
}

// -------------------------------------------------------------------
// URL で指定されたパラメータを安全に取得
// ＜引数＞ $name: パラメーター名
// ＜返値＞ 危険を無力化した後の値文字列（パラメーターが設定されていない場合は空文字列）
// -------------------------------------------------------------------
function	get_url_parameter($name)
{
	if ( isset($_GET[$name]) ) {
		return escape_input($_GET[$name]);
	} else {
		return '';
	}
}

// ----------------------------------------------------------------------------
// メッセージを表示＆ログ保存
// 注意：error_die() からもコールされるので、ここで error_die() をコールしてはいけない
// ----------------------------------------------------------------------------
function	log_message($message, $level, $echo_enabled = TRUE, $log_file_name = '')
{
	if ( $level == LOG_LEVEL_DEBUG && !DEBUG_FLAG ) {
		return;
	}

	// ブラウザへの表示
	if ( $echo_enabled ) {
		echo(get_log_message_html($message, $level));
	}
	
	// ログファイル名
	if ( $log_file_name == '' ) {
		$log_file_name = FILE_NAME_DEFAULT_CP_LOG;
	}
	
	// ログファイルに追記
	if ( !file_put_contents(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$log_file_name
			, $level.','.date(RUN_TIME_DATE_FORMAT).','.$message."\n", FILE_APPEND)
			&& $echo_enabled ) {
		echo(get_log_message_html('ログを保存できませんでした：'.$log_file_name
				, LOG_LEVEL_WARNING));
	}
}

// ----------------------------------------------------------------------------
// ログファイルを一定サイズごとに区切る
// ----------------------------------------------------------------------------
function	lotate_log($log_file_name = FILE_NAME_DEFAULT_CP_LOG)
{
	if ( filesize(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$log_file_name)
			<= LOG_FILE_SIZE_MAX ) {
		return;
	}
	
	// 最古のログを削除
	$old_log_file_name = get_old_log_file_name($log_file_name, OLD_LOG_MAX_GENERATION);
	if ( file_exists(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$old_log_file_name)
			&& !unlink(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$old_log_file_name) ) {
		log_message('古いログを削除できませんでした：'.$old_log_file_name, LOG_LEVEL_WARNING);
	}
	
	// リネーム
	for ( $i = OLD_LOG_MAX_GENERATION-1 ; $i > 0 ; $i-- ) {
		$before_log_file_name = get_old_log_file_name($log_file_name, $i);
		$after_log_file_name = get_old_log_file_name($log_file_name, $i+1);
		if ( !rename(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$before_log_file_name
				, dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$after_log_file_name) ) {
			log_message('ログのローテーションができませんでした：'.$before_log_file_name, LOG_LEVEL_WARNING);
		}
	}
	$after_log_file_name = get_old_log_file_name($log_file_name, 1);
	if ( !rename(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$log_file_name
			, dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$after_log_file_name) ) {
		log_message('ログのローテーションができませんでした：'.$log_file_name, LOG_LEVEL_WARNING);
	}
	
	create_log_file($log_file_name);
}

// ----------------------------------------------------------------------------
// 変数の値を文字列に変換
// ※ var_export() だと文字列が '' で括られてしまうので
// ----------------------------------------------------------------------------
function	var_to_string($var)
{
	if ( is_bool($var) ) {
		if ( $var ) {
			return '1';
		} else {
			return '0';
		}
	}
	return (string)$var;
}

// ============================================================================
// 【デバッグ用】デバッグメッセージを表示
// ============================================================================
function	debug_show_message($message)
{
	if ( DEBUG_FLAG ) {
		echo('<font color="#9999FF">'.$message.'</font>');
	}
}

?>