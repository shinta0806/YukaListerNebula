<?php

// ============================================================================
// ゆかりすたー NEBULA 同期
// ログイン状態管理クラス
// Copyright (C) 2021 by SHINTA
// ============================================================================

// require
require_once(dirname(__FILE__).'/JulianDay.php');

class	CPManager
{
	// ===================================================================
	// public メソッド
	// ===================================================================

	// -------------------------------------------------------------------
	// コンストラクタ
	// -------------------------------------------------------------------
	public function __construct()
	{
		// その他
		$this->analyze_mode_and_params();
	}

	// -------------------------------------------------------------------
	// URL として渡されているエラーメッセージを返す
	// -------------------------------------------------------------------
	public function error_message()
	{
		return $this->_err_message;
	}

	// -------------------------------------------------------------------
	// ログイン状態かどうか
	// ＜引数＞ $not_logged_in_reason: 未ログインの場合の理由番号を格納
	// ＜返値＞ TRUE: ログイン中, FALSE: 未ログイン
	// -------------------------------------------------------------------
	public function is_logged_in(&$not_logged_in_reason = NULL)
	{
		if ( isset($_SESSION[SESSION_INFO_ACCOUNT_UID]) ) {
			return TRUE;
		}

		// マスターアカウントが設定されているか
		if ( !$this->master_account_exists() ) {
			// 参照が NULL の場合でも代入でエラーにならない模様
			$not_logged_in_reason = NOT_LOGGED_IN_NO_MASTER_ACCOUNT;
			return FALSE;
		}

		// 単なる未ログイン
		$not_logged_in_reason = NOT_LOGGED_IN_NO_SESSION_INFO;
		return FALSE;
	}

	// -------------------------------------------------------------------
	// 動作モードを返す
	// ＜返値＞ 動作モード
	// -------------------------------------------------------------------
	public function mode()
	{
		return $this->_mode;
	}

	// -------------------------------------------------------------------
	// URL として渡されている報告メッセージを返す
	// -------------------------------------------------------------------
	public function notice_message()
	{
		return $this->_notice_message;
	}

	// -------------------------------------------------------------------
	// 各種動作を行う
	// ＜返値＞ void
	// -------------------------------------------------------------------
	public function run()
	{
		// ログイン状態に関わらず受け付けるコマンド
		if ( $this->_mode == MODE_NAME_DOWNLOAD_POST_ERROR ) {
			$this->download_post_error();
			return;
		}
		
		// ログイン状態の取得
		$not_logged_in_reason = 0;
		$is_logged_in = $this->is_logged_in($not_logged_in_reason);
		//log_message('run() mode: '.$this->_mode, LOG_LEVEL_STATUS, FALSE);

		// ログインしていない場合
		if ( !$is_logged_in ) {
			// ログインしようとしている場合はログイン処理
			if ( $this->_mode == MODE_NAME_LOGIN ) {
				$this->check_login();
				return;
			}
			// マスターアカウントが作成されていないかつアカウント作成モードの場合は、
			// マスターアカウント作成
			if ( $not_logged_in_reason == NOT_LOGGED_IN_NO_MASTER_ACCOUNT
					&& $this->_mode == MODE_NAME_NEW_ACCOUNT ) {
				$this->create_master_account();
				return;
			}
			// ログイン画面に遷移
			header('Location:'.FILE_NAME_CP_LOGIN);
			return;
		}

		// 管理者アカウントでログインしている場合
		if ( $_SESSION[SESSION_INFO_ACCOUNT_ADMIN] ) {
			switch ( $this->_mode ) {
			case MODE_NAME_CHANGE_ACCOUNT:
				$this->change_account();
				return;
			case MODE_NAME_DELETE_ACCOUNT:
				$this->delete_account();
				return;
			case MODE_NAME_INIT_ACCOUNT:
				$this->init_account();
				return;
			case MODE_NAME_MANAGE_USERS:
				$this->manage_users();
				return;
			case MODE_NAME_NEW_ACCOUNT:
				$this->create_account();
				return;
			case MODE_NAME_VIEW_LOG:
				$this->view_log();
				return;
			case MODE_NAME_VIEW_NUM_COMMIT:
				$this->view_num_commit();
				return;
			case MODE_NAME_VIEW_NUM_DATA:
				$this->view_num_data();
				return;
			}
		}

		// 管理者アカウントでログインしていてかつデバッグモードの場合
		if ( $_SESSION[SESSION_INFO_ACCOUNT_ADMIN] && DEBUG_FLAG ) {
			switch ( $this->_mode ) {
			case MODE_NAME_UPLOAD_TEST:
				$this->upload_test();
				return;
			}
		}

		// ログインしている場合全般
		switch ( $this->_mode ) {
		case MODE_NAME_CHANGE_CURRENT_PASSWORD:
			$this->change_current_password();
			return;
		case MODE_NAME_CHANGE_CURRENT_PASSWORD_FORM:
			$this->change_current_password_form();
			return;
		case MODE_NAME_DOWNLOAD_REJECT_DATE:
			$this->download_reject_date();
			return;
		case MODE_NAME_DOWNLOAD_SYNC_DATA:
			$this->download_sync_data();
			return;
		case MODE_NAME_LOGOUT:
			$this->logout();
			return;
		case MODE_NAME_UPLOAD_SYNC_DATA:
			$this->upload_sync_data();
			return;
		case MODE_NAME_VIEW_STATUS:
			$this->view_status();
			return;
		default:
			// 不適切なモードの場合は状態表示画面に遷移
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_VIEW_STATUS);
			return;
		}

	}

	// ===================================================================
	// private メソッド
	// ===================================================================

	// -------------------------------------------------------------------
	// ログイン中のユーザーの受諾・拒否件数を加算
	// -------------------------------------------------------------------
	private function add_statistics_accepts_and_rejects($num_accepts, $num_rejects)
	{
		$now_month_mjd = $this->now_month_mjd();
		$row = $this->select_statistics($now_month_mjd);

		$pdo = $this->connect_db();
		if ( $row === FALSE ) {
			// 今月初めての通信
			$sql = 'INSERT INTO '.TABLE_NAME_STATISTICS.' VALUES ('
					.$_SESSION[SESSION_INFO_ACCOUNT_UID].','
					.$now_month_mjd.','
					.'0,'
					.$num_accepts.','
					.$num_rejects.');';
			//log_message('add_statistics_accepts_and_rejects() '.$sql, LOG_LEVEL_STATUS, FALSE);
			$pdo->exec($sql);
		} else {
			// 更新
			$sql = 'UPDATE '.TABLE_NAME_STATISTICS
					.' SET '.FIELD_NAME_STATISTICS_ACCEPTS.' = '.($row[FIELD_NAME_STATISTICS_ACCEPTS]+$num_accepts)
					.', '.FIELD_NAME_STATISTICS_REJECTS.' = '.($row[FIELD_NAME_STATISTICS_REJECTS]+$num_rejects)
					.' WHERE '.FIELD_NAME_STATISTICS_UID.' = '.$_SESSION[SESSION_INFO_ACCOUNT_UID]
					.' AND '.FIELD_NAME_STATISTICS_MONTH.' = '.$now_month_mjd.';';
			//log_message('add_statistics_accepts_and_rejects() '.$sql, LOG_LEVEL_STATUS, FALSE);
			$pdo->exec($sql);
		}
	}

	// -------------------------------------------------------------------
	// ログイン中のユーザーの通信量を加算
	// -------------------------------------------------------------------
	private function add_statistics_bytes($bytes)
	{
		if ( !$this->is_logged_in() ) {
			return;
		}
	
		$now_month_mjd = $this->now_month_mjd();
		$row = $this->select_statistics($now_month_mjd);

		$pdo = $this->connect_db();
		if ( $row === FALSE ) {
			// 今月初めての通信
			$sql = 'INSERT INTO '.TABLE_NAME_STATISTICS.' VALUES ('
					.$_SESSION[SESSION_INFO_ACCOUNT_UID].','
					.$now_month_mjd.','
					.$bytes.','
					.'0,'
					.'0);';
			//log_message('add_statistics_bytes() '.$sql, LOG_LEVEL_STATUS, FALSE);
			$pdo->exec($sql);
		} else {
			// 更新
			$sql = 'UPDATE '.TABLE_NAME_STATISTICS
				.' SET '.FIELD_NAME_STATISTICS_BYTES.' = '.($row[FIELD_NAME_STATISTICS_BYTES]+$bytes)
				.' WHERE '.FIELD_NAME_STATISTICS_UID.' = '.$_SESSION[SESSION_INFO_ACCOUNT_UID]
				.' AND '.FIELD_NAME_STATISTICS_MONTH.' = '.$now_month_mjd.';';
			//log_message('add_statistics_bytes() '.$sql, LOG_LEVEL_STATUS, FALSE);
			$pdo->exec($sql);
		}
	}

	// -------------------------------------------------------------------
	// 動作モード等を取得
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function analyze_mode_and_params()
	{
		// モード
		$this->_mode = $this->get_posted_or_url_parameter(PARAM_NAME_MODE);

		// 報告メッセージ
		$this->_notice_message = urldecode(get_url_parameter(PARAM_NAME_NOTICE_MESSAGE));

		// エラーメッセージ
		$this->_err_message = urldecode(get_url_parameter(PARAM_NAME_ERROR_MESSAGE));
	}

	// -------------------------------------------------------------------
	// アカウント変更
	// -------------------------------------------------------------------
	private function change_account()
	{
		try {
			// POST されたパラメーター取得
			$posted_uid = $this->get_posted_parameter(PARAM_NAME_UID);
			if ( $posted_uid == '' ) {
				throw new Exception('アカウント番号が指定されていません。ユーザー一覧の変更リンクをクリックして下さい。');
			}
			$posted_uid = (int)$posted_uid;
			$posted_name = $this->get_posted_parameter(PARAM_NAME_NAME);
			$posted_admin = ($this->get_posted_parameter(PARAM_NAME_ADMIN) == '1');
			
			// マスターアカウント確認
			if ( $posted_uid == 0 && !$posted_admin ) {
				throw new Exception('アカウント番号 0 の管理者権限は外せません。');
			}
			
			// アカウント名の確認
			$this->check_account_name($posted_name);
			
			// 変更
			$pdo = $this->connect_db();
			$sql = 'UPDATE '.TABLE_NAME_ACCOUNT
					.' SET '.FIELD_NAME_ACCOUNT_NAME.' = :name,'
					.FIELD_NAME_ACCOUNT_ADMIN.' = :admin,'
					.FIELD_NAME_ACCOUNT_UPDATE_TIME.' = :update_time'
					.' WHERE '.FIELD_NAME_ACCOUNT_UID.' = :uid';
			log_message('change_account() '.$sql, LOG_LEVEL_DEBUG, FALSE);
			$stmt = $pdo->prepare($sql);
			$stmt->bindValue(':name', $posted_name, PDO::PARAM_STR);
			$stmt->bindValue(':admin', var_to_string($posted_admin), PDO::PARAM_INT);
			$stmt->bindValue(':update_time', $this->now_mjd(), PDO::PARAM_STR);
			$stmt->bindValue(':uid', $posted_uid, PDO::PARAM_INT);
			$stmt->execute();

			// 結果出力
			$message = 'アカウント #'.$posted_uid.'（'.$posted_name.'）を変更しました。 ';
			log_message($message, LOG_LEVEL_NOTICE, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_NOTICE_MESSAGE.'='.urlencode($message));
		} catch (Exception $excep) {
			log_message($excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode($excep->getMessage()));
		}
	}

	// -------------------------------------------------------------------
	// ログイン中のアカウントのパスワード変更
	// -------------------------------------------------------------------
	private function change_current_password()
	{
		try {
			// POST されたパラメーター取得
			$posted_current_pw = $this->get_posted_parameter(PARAM_NAME_CURRENT_PASSWORD);
			$posted_pw = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD);
			$posted_pw_confirm = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD_CONFIRM);
			
			// 現在のパスワードが合っているか確認
			$pdo = $this->connect_db();
			$sql = 'SELECT * FROM '.TABLE_NAME_ACCOUNT
					.' WHERE '.FIELD_NAME_ACCOUNT_UID.' = :uid';
			$stmt = $pdo->prepare($sql);
			$stmt->bindValue(':uid', $_SESSION[SESSION_INFO_ACCOUNT_UID], PDO::PARAM_INT);
			$stmt->execute();
			$row = $stmt->fetch(PDO::FETCH_ASSOC);
			$stmt->closeCursor();
			if ( $row === FALSE ) {
				throw new Exception('アカウント情報を取得できませんでした。');
			}
			if ( !password_verify($posted_current_pw, $row[FIELD_NAME_ACCOUNT_PASSWORD]) ) {
				throw new Exception('現在のパスワードが間違っています。');
			}
			
			// 確認パスワードが一致しているか検証
			if ( $posted_pw != $posted_pw_confirm ) {
				throw new Exception('新しいパスワードと確認用パスワードが異なっています。');
			}

			// パスワードの確認
			$this->check_account_password($posted_pw);

			// 変更
			$sql = 'UPDATE '.TABLE_NAME_ACCOUNT
					.' SET '.FIELD_NAME_ACCOUNT_PASSWORD.' = :password,'
					.FIELD_NAME_ACCOUNT_UPDATE_TIME.' = :update_time'
					.' WHERE '.FIELD_NAME_ACCOUNT_UID.' = :uid';
			log_message('change_current_password() '.$sql, LOG_LEVEL_DEBUG, FALSE);
			$stmt = $pdo->prepare($sql);
			$stmt->bindValue(':password', password_hash($posted_pw, PASSWORD_DEFAULT), PDO::PARAM_STR);
			$stmt->bindValue(':update_time', $this->now_mjd(), PDO::PARAM_STR);
			$stmt->bindValue(':uid', $_SESSION[SESSION_INFO_ACCOUNT_UID], PDO::PARAM_INT);
			$stmt->execute();

			// 結果出力
			$message = 'パスワードを変更しました。 ';
			log_message($message, LOG_LEVEL_NOTICE, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_CHANGE_CURRENT_PASSWORD_FORM
					.'&'.PARAM_NAME_NOTICE_MESSAGE.'='.urlencode($message));
		} catch (Exception $excep) {
			log_message($excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_CHANGE_CURRENT_PASSWORD_FORM
					.'&'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode($excep->getMessage()));
		}
	}

	// -------------------------------------------------------------------
	// パスワード変更フォーム表示
	// -------------------------------------------------------------------
	private function change_current_password_form()
	{
		// テンプレート適用
		$vars = array();

		// フォーム表示
		$this->show_form('パスワード変更フォーム', FILE_NAME_TEMPLATE_CHANGE_CURRENT_PASSWORD_FORM, $vars);
	}

	// -------------------------------------------------------------------
	// 入力されたアカウント名が命名規則の範囲内か確認
	// ＜返値＞ void
	// ＜例外＞ Exception
	// -------------------------------------------------------------------
	private function check_account_name($posted_name)
	{
		if ( var_to_string($posted_name) == '' ) {
			throw new Exception('アカウント名を入力して下さい。');
		}
		if ( preg_match('/^[a-zA-Z0-9\.]+$/', $posted_name) != 1 ) {
			throw new Exception('アカウント名には半角英数字と半角ピリオド "." のみが使えます。');
		}
		if ( strlen($posted_name) > ACCOUNT_NAME_MAX_LENGTH ) {
			throw new Exception('アカウント名は '.ACCOUNT_NAME_MAX_LENGTH.' 文字以下にして下さい。');
		}
	
	}

	// -------------------------------------------------------------------
	// 入力されたパスワードが命名規則の範囲内か確認
	// ＜返値＞ void
	// ＜例外＞ Exception
	// -------------------------------------------------------------------
	private function check_account_password($posted_pw)
	{
		if ( var_to_string($posted_pw) == '' ) {
			throw new Exception('パスワードを入力して下さい。');
		}
		if ( preg_match('/[\r\n\t]+/', $posted_pw) == 1 ) {
			throw new Exception('パスワードに改行などの制御文字を入れないで下さい。');
		}
		if ( strlen($posted_pw) < ACCOUNT_PASSWORD_MIN_LENGTH ) {
			throw new Exception('パスワードは '.ACCOUNT_PASSWORD_MIN_LENGTH.' 文字以上にして下さい。');
		}
	}

	// -------------------------------------------------------------------
	// パスワードが正しいか判定してログイン処理
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function check_login()
	{
		// POST されたパラメーター取得
		$posted_name = $this->get_posted_parameter(PARAM_NAME_NAME);
		$posted_pw = $this->get_posted_parameter(PARAM_NAME_PASSWORD);
		$posted_app_generation = $this->get_posted_parameter(PARAM_NAME_APP_GENERATION);

		if ( $posted_app_generation != SYSTEM_APP_GENERATION ) {
			// クライアントの世代がサーバーと異なる
			$this->insert_login(FALSE);

			// ログインできないのでログイン画面に戻る
			header('Location:'.FILE_NAME_CP_LOGIN.'?'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode('クライアントの互換性がありません。'));
			return;
		}

		$row = $this->select_account_by_name_and_password($posted_name, $posted_pw);
		if ( $row === FALSE ) {
			$this->insert_login(FALSE);
		
			// ログインできないのでログイン画面に戻る
			header('Location:'.FILE_NAME_CP_LOGIN.'?'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode('アカウント名またはパスワードが違います。'));
			return;
		}

		// ログイン成功
		$this->login($row);
		$this->insert_login(TRUE);
		header('Location:'.FILE_NAME_CP_MAIN.$this->get_posted_parameter(CP_MAIN_PARAM));
	}

	// ----------------------------------------------------------------------------
	// データベースに接続
	// ＜例外＞ Exception
	// ----------------------------------------------------------------------------
	private function connect_db()
	{
		try {
			$pdo = new PDO('mysql:host='.MY_SQL_HOST.';dbname='.MY_SQL_DATABASE.';charset=utf8', MY_SQL_USER_NAME, MY_SQL_PASSWORD,
					array(PDO::ATTR_EMULATE_PREPARES => false));
			$pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
		} catch (PDOException $excep) {
			throw new Exception('データベースに接続できませんでした：'.$excep->getMessage());
		}
		
		return $pdo;
	}

	// -------------------------------------------------------------------
	// アカウント作成
	// -------------------------------------------------------------------
	private function create_account()
	{
		try {
			// POST されたパラメーター取得
			$posted_name = $this->get_posted_parameter(PARAM_NAME_NAME);
			$posted_pw = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD);
			$posted_pw_confirm = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD_CONFIRM);
			$posted_admin = ($this->get_posted_parameter(PARAM_NAME_ADMIN) == '1');
			
			// アカウント名の確認
			$this->check_account_name($posted_name);
			
			// 確認パスワードが一致しているか検証
			if ( $posted_pw != $posted_pw_confirm ) {
				throw new Exception('新しいパスワードと確認用パスワードが異なっています。');
			}

			// パスワードの確認
			$this->check_account_password($posted_pw);

			// UID
			$pdo = $this->connect_db();
			$sql = 'SELECT MAX('.FIELD_NAME_ACCOUNT_UID.') AS max_uid FROM '.TABLE_NAME_ACCOUNT.';';
			$stmt = $pdo->query($sql);
			$row = $stmt->fetch(PDO::FETCH_ASSOC);
			if ( $row === FALSE ) {
				throw new Exception('アカウント番号を取得できませんでした。');
			}

			// 登録
			$this->insert_account($row['max_uid']+1, $posted_name, $posted_pw, $posted_admin);

			// 結果出力
			$message = 'アカウント '.$posted_name.' を登録しました。';
			log_message($message, LOG_LEVEL_NOTICE, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_NOTICE_MESSAGE.'='.urlencode($message));
		} catch (Exception $excep) {
			log_message($excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode($excep->getMessage()));
		}
	}

	// -------------------------------------------------------------------
	// マスターアカウント（UID が 0 のアカウント）作成
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function create_master_account()
	{
		try {
			// 実際にマスターアカウントが未作成か確認
			$table_exists = FALSE;
			if ( $this->master_account_exists($table_exists) ) {
				throw new Exception('管理者アカウント（最初に作成するアカウント）は既に作成されています。');
			}
			
			// テーブル未作成の場合は作成
			if( !$table_exists ) {
				$this->create_tables();
			}
			
			// POST されたパラメーター取得
			$posted_name = $this->get_posted_parameter(PARAM_NAME_NAME);
			$posted_pw = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD);
			$posted_pw_confirm = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD_CONFIRM);
			
			// アカウント名の確認
			$this->check_account_name($posted_name);
			
			// 確認パスワードが一致しているか検証
			if ( $posted_pw != $posted_pw_confirm ) {
				throw new Exception('新しいパスワードと確認用パスワードが異なっています。');
			}

			// パスワードの確認
			$this->check_account_password($posted_pw);

			// 登録
			$this->insert_account(0, $posted_name, $posted_pw, TRUE);
			log_message('管理者アカウント（最初に作成するアカウント）を登録しました。', LOG_LEVEL_NOTICE, FALSE);

			// 結果出力
			$headline = get_headline_notice_html('管理者アカウント（最初に作成するアカウント）の登録が完了しました。');
			$contents  = '<h1>完了</h1>'."\n";
			$contents .= '<p>管理者アカウント（最初に作成するアカウント）を登録しました。</p>'."\n";

			$vars = array();
			$vars[ATCV_CONTAINER_FILE] = FILE_NAME_TEMPLATE_CONTAINER_MAIN;
			$vars[TMPL_MARK_HEADLINE] = $headline;
			$output = apply_template_container($contents, $vars);
			if ( $output === FALSE ) {
				error_die('内部エラー：結果表示不正');
			}
			echo($output);
		} catch (Exception $excep) {
			log_message($excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
			header('Location:'.FILE_NAME_CP_LOGIN.'?'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode($excep->getMessage()));
		}
	}

	// -------------------------------------------------------------------
	// TAlias カラム作成用 SQL
	// -------------------------------------------------------------------
	private function create_table_talias_sql($field_prefix)
	{
		return $field_prefix.FIELD_NAME_SUFFIX_ALIAS.' VARCHAR(65535),'
				.$field_prefix.FIELD_NAME_SUFFIX_ORIGINAL_ID.' VARCHAR(255),';
	}

	// -------------------------------------------------------------------
	// TBase カラム作成用 SQL
	// -------------------------------------------------------------------
	private function create_table_tbase_sql($field_prefix)
	{
		return $field_prefix.FIELD_NAME_SUFFIX_ID.' VARCHAR(255) NOT NULL,'
				.$field_prefix.FIELD_NAME_SUFFIX_IMPORT.' TINYINT(1) NOT NULL,'
				.$field_prefix.FIELD_NAME_SUFFIX_INVALID.' TINYINT(1) NOT NULL,'
				.$field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME.' DOUBLE NOT NULL,'
				.$field_prefix.FIELD_NAME_SUFFIX_SYNC_BY.' INT NOT NULL,';
	}

	// -------------------------------------------------------------------
	// TMaster カラム作成用 SQL
	// -------------------------------------------------------------------
	private function create_table_tmaster_sql($field_prefix)
	{
		return $field_prefix.FIELD_NAME_SUFFIX_NAME.' VARCHAR(65535),'
				.$field_prefix.FIELD_NAME_SUFFIX_RUBY.' VARCHAR(65535),'
				.$field_prefix.FIELD_NAME_SUFFIX_RUBY_FOR_SEARCH.' VARCHAR(65535),'
				.$field_prefix.FIELD_NAME_SUFFIX_KEYWORD.' VARCHAR(65535),'
				.$field_prefix.FIELD_NAME_SUFFIX_KEYWORD_RUBY_FOR_SEARCH.' VARCHAR(65535),';
	}

	// -------------------------------------------------------------------
	// TSequence カラム作成用 SQL
	// -------------------------------------------------------------------
	private function create_table_tsequence_sql($field_prefix)
	{
		return $field_prefix.FIELD_NAME_SUFFIX_SEQUENCE.' INT NOT NULL,'
				.$field_prefix.FIELD_NAME_SUFFIX_LINK_ID.' VARCHAR(255) NOT NULL,';
	}

	// -------------------------------------------------------------------
	// データベース上に各種テーブルを作成
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function create_tables()
	{
		$pdo = $this->connect_db();
		
		// TSong
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_SONG.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_SONG_PREFIX)
				.$this->create_table_tmaster_sql(FIELD_NAME_SONG_PREFIX)
				.FIELD_NAME_SONG_RELEASE_DATE.' DOUBLE,'
				.FIELD_NAME_SONG_TIE_UP_ID.' VARCHAR(255),'
				.FIELD_NAME_SONG_CATEGORY_ID.' VARCHAR(255),'
				.FIELD_NAME_SONG_OP_ED.' VARCHAR(65535),'
				.'PRIMARY KEY('.FIELD_NAME_SONG_ID.'),'
				.'INDEX('.FIELD_NAME_SONG_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TSong '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TPserson
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_PERSON.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_PERSON_PREFIX)
				.$this->create_table_tmaster_sql(FIELD_NAME_PERSON_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_PERSON_ID.'),'
				.'INDEX('.FIELD_NAME_PERSON_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TPerson '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TTieUp
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_TIE_UP.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_TIE_UP_PREFIX)
				.$this->create_table_tmaster_sql(FIELD_NAME_TIE_UP_PREFIX)
				.FIELD_NAME_TIE_UP_CATEGORY_ID.' VARCHAR(255),'
				.FIELD_NAME_TIE_UP_MAKER_ID.' VARCHAR(255),'
				.FIELD_NAME_TIE_UP_AGE_LIMIT.' INT,'
				.FIELD_NAME_TIE_UP_RELEASE_DATE.' DOUBLE,'
				.'PRIMARY KEY('.FIELD_NAME_TIE_UP_ID.'),'
				.'INDEX('.FIELD_NAME_TIE_UP_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TTieUp '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TCategory
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_CATEGORY.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_CATEGORY_PREFIX)
				.$this->create_table_tmaster_sql(FIELD_NAME_CATEGORY_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_CATEGORY_ID.'),'
				.'INDEX('.FIELD_NAME_CATEGORY_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TCategory '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TTieUpGroup
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_TIE_UP_GROUP.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_TIE_UP_GROUP_PREFIX)
				.$this->create_table_tmaster_sql(FIELD_NAME_TIE_UP_GROUP_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_TIE_UP_GROUP_ID.'),'
				.'INDEX('.FIELD_NAME_TIE_UP_GROUP_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TTieUpGroup '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TMaker
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_MAKER.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_MAKER_PREFIX)
				.$this->create_table_tmaster_sql(FIELD_NAME_MAKER_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_MAKER_ID.'),'
				.'INDEX('.FIELD_NAME_MAKER_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TMaker '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TSongAlias
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_SONG_ALIAS.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_SONG_ALIAS_PREFIX)
				.$this->create_table_talias_sql(FIELD_NAME_SONG_ALIAS_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_SONG_ALIAS_ID.'),'
				.'INDEX('.FIELD_NAME_SONG_ALIAS_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TSongAlias '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TTieUpAlias
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_TIE_UP_ALIAS.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_TIE_UP_ALIAS_PREFIX)
				.$this->create_table_talias_sql(FIELD_NAME_TIE_UP_ALIAS_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_TIE_UP_ALIAS_ID.'),'
				.'INDEX('.FIELD_NAME_TIE_UP_ALIAS_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TTieUpAlias '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TArtistSequence
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_ARTIST_SEQUENCE.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_ARTIST_SEQUENCE_PREFIX)
				.$this->create_table_tsequence_sql(FIELD_NAME_ARTIST_SEQUENCE_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_ARTIST_SEQUENCE_ID.','.FIELD_NAME_ARTIST_SEQUENCE_SEQUENCE.'),'
				.'INDEX('.FIELD_NAME_ARTIST_SEQUENCE_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TArtistSequence '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TLyristSequence
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_LYRIST_SEQUENCE.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_LYRIST_SEQUENCE_PREFIX)
				.$this->create_table_tsequence_sql(FIELD_NAME_LYRIST_SEQUENCE_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_LYRIST_SEQUENCE_ID.','.FIELD_NAME_LYRIST_SEQUENCE_SEQUENCE.'),'
				.'INDEX('.FIELD_NAME_LYRIST_SEQUENCE_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TLyristSequence '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TComposerSequence
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_COMPOSER_SEQUENCE.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_COMPOSER_SEQUENCE_PREFIX)
				.$this->create_table_tsequence_sql(FIELD_NAME_COMPOSER_SEQUENCE_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_COMPOSER_SEQUENCE_ID.','.FIELD_NAME_COMPOSER_SEQUENCE_SEQUENCE.'),'
				.'INDEX('.FIELD_NAME_COMPOSER_SEQUENCE_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TComposerSequence '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TArrangerSequence
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_ARRANGER_SEQUENCE.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_ARRANGER_SEQUENCE_PREFIX)
				.$this->create_table_tsequence_sql(FIELD_NAME_ARRANGER_SEQUENCE_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_ARRANGER_SEQUENCE_ID.','.FIELD_NAME_ARRANGER_SEQUENCE_SEQUENCE.'),'
				.'INDEX('.FIELD_NAME_ARRANGER_SEQUENCE_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TArrangerSequence '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TTieUpGroupSequence
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_TIE_UP_GROUP_SEQUENCE.' ('
				.$this->create_table_tbase_sql(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX)
				.$this->create_table_tsequence_sql(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_PREFIX)
				.'PRIMARY KEY('.FIELD_NAME_TIE_UP_GROUP_SEQUENCE_ID.','.FIELD_NAME_TIE_UP_GROUP_SEQUENCE_SEQUENCE.'),'
				.'INDEX('.FIELD_NAME_TIE_UP_GROUP_SEQUENCE_UPDATE_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TTieUpGroupSequence '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TAccount
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_ACCOUNT.' ('
				.FIELD_NAME_ACCOUNT_UID.' INT NOT NULL,'
				.FIELD_NAME_ACCOUNT_NAME.' VARCHAR(255) NOT NULL,'
				.FIELD_NAME_ACCOUNT_PASSWORD.' VARCHAR(255) NOT NULL,'
				.FIELD_NAME_ACCOUNT_ADMIN.' TINYINT(1) NOT NULL,'
				.FIELD_NAME_ACCOUNT_LOGIN_TIME.' DOUBLE NOT NULL,'
				.FIELD_NAME_ACCOUNT_UPDATE_TIME.' DOUBLE NOT NULL,'
				.'PRIMARY KEY ('.FIELD_NAME_ACCOUNT_UID.'),'
				.'UNIQUE ('.FIELD_NAME_ACCOUNT_NAME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TAccount '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TLogin
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_LOGIN.' ('
				.FIELD_NAME_LOGIN_NAME.' VARCHAR(255) NOT NULL,'
				.FIELD_NAME_LOGIN_TIME.' DOUBLE NOT NULL,'
				.FIELD_NAME_LOGIN_SUCCESS.' TINYINT(1) NOT NULL,'
				.FIELD_NAME_LOGIN_APP_GENERATION.' VARCHAR(255),'
				.FIELD_NAME_LOGIN_APP_VER.' VARCHAR(255),'
				.FIELD_NAME_LOGIN_ID_PREFIX.' VARCHAR(255),'
				.FIELD_NAME_LOGIN_SID.' VARCHAR(255),'
				.'PRIMARY KEY ('.FIELD_NAME_LOGIN_NAME.','.FIELD_NAME_LOGIN_TIME.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TLogin '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		// TStatistics
		$sql = 'CREATE TABLE IF NOT EXISTS '.TABLE_NAME_STATISTICS.' ('
				.FIELD_NAME_STATISTICS_UID.' INT NOT NULL,'
				.FIELD_NAME_STATISTICS_MONTH.' DOUBLE NOT NULL,'
				.FIELD_NAME_STATISTICS_BYTES.' BIGINT NOT NULL,'
				.FIELD_NAME_STATISTICS_ACCEPTS.' INT NOT NULL,'
				.FIELD_NAME_STATISTICS_REJECTS.' INT NOT NULL,'
				.'PRIMARY KEY ('.FIELD_NAME_STATISTICS_UID.','.FIELD_NAME_STATISTICS_MONTH.')'
				.') '.CREATE_TABLE_OPTIONS.';';
		log_message('create_tables() TStatistics '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$pdo->exec($sql);
		
		if ( DEBUG_FLAG && FALSE ) {
			// サンプルデータ（TSong）
			$sql = 'INSERT INTO '.TABLE_NAME_SONG.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-01', new DateTimeZone('UTC'))).', 0,'
					.'"TestSong11A", "テストソング", NULL, '.$this->invalid_mjd().', NULL, NULL, "OP");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_SONG.' VALUES ('
					.'"SyncTest_S_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-01', new DateTimeZone('UTC'))).', 0,'
					.'"TestSong11B", "テストソング", NULL, '.$this->invalid_mjd().', NULL, NULL, "OP");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_SONG.' VALUES ('
					.'"SyncTest_S_3", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-02', new DateTimeZone('UTC'))).', 0,'
					.'"TestSong12A", "テストソング", NULL, '.$this->invalid_mjd().', NULL, NULL, "OP");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_SONG.' VALUES ('
					.'"SyncTest_S_4", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-02', new DateTimeZone('UTC'))).', 0,'
					.'"TestSong12B", "テストソング", NULL, '.$this->invalid_mjd().', NULL, NULL, "OP");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_SONG.' VALUES ('
					.'"SyncTest_S_5", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-05 18:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestSong15A", "テストソング", NULL, '.$this->invalid_mjd().', NULL, NULL, "OP");';
			$pdo->exec($sql);

			// サンプルデータ（TPerson）
			$sql = 'INSERT INTO '.TABLE_NAME_PERSON.' VALUES ('
					.'"SyncTest_P_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-01 01:01:01', new DateTimeZone('UTC'))).', 0,'
					.'"TestPerson11あ", "テストヒト", NULL);';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_PERSON.' VALUES ('
					.'"SyncTest_P_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-03 02:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestPerson13い", "テストヒト", NULL);';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_PERSON.' VALUES ('
					.'"SyncTest_P_3", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-05 03:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestPerson15う", "テストヒト", NULL);';
			$pdo->exec($sql);

			// サンプルデータ（TTieUp）
			$sql = 'INSERT INTO '.TABLE_NAME_TIE_UP.' VALUES ('
					.'"SyncTest_T_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 03:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestTieUp0110", "テストタイアップ", NULL, NULL, NULL, 18, '.$this->invalid_mjd().');';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_TIE_UP.' VALUES ('
					.'"SyncTest_T_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 03:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestTieUp0110-2", "テストタイアップ", NULL, NULL, NULL, 18, '.$this->invalid_mjd().');';
			$pdo->exec($sql);

			// サンプルデータ（TTieUpGroup）
			$sql = 'INSERT INTO '.TABLE_NAME_TIE_UP_GROUP.' VALUES ('
					.'"SyncTest_G_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 04:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestTieUpGroup0110", "テストタイアップグループ", NULL);';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_TIE_UP_GROUP.' VALUES ('
					.'"SyncTest_G_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 04:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestTieUpGroup0110-2", "テストタイアップグループ", NULL);';
			$pdo->exec($sql);

			// サンプルデータ（TMaker）
			$sql = 'INSERT INTO '.TABLE_NAME_MAKER.' VALUES ('
					.'"SyncTest_M_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 05:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestMaker0110", "テストメーカー", NULL);';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_MAKER.' VALUES ('
					.'"SyncTest_M_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 05:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"TestMaker0110-2", "テストメーカー", NULL);';
			$pdo->exec($sql);

			// サンプルデータ（TSongAlias）
			$sql = 'INSERT INTO '.TABLE_NAME_SONG_ALIAS.' VALUES ('
					.'"SyncTest_SA_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 06:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"S_1の別名", "SyncTest_S_1");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_SONG_ALIAS.' VALUES ('
					.'"SyncTest_SA_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 06:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"S_1の別名-2", "SyncTest_S_1");';
			$pdo->exec($sql);

			// サンプルデータ（TTieUpAlias）
			$sql = 'INSERT INTO '.TABLE_NAME_TIE_UP_ALIAS.' VALUES ('
					.'"SyncTest_TA_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 07:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"T_1の別名", "SyncTest_T_1");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_TIE_UP_ALIAS.' VALUES ('
					.'"SyncTest_TA_2", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 07:00:00', new DateTimeZone('UTC'))).', 0,'
					.'"T_1の別名-2", "SyncTest_T_1");';
			$pdo->exec($sql);

			// サンプルデータ（TArtistSequence）
			$sql = 'INSERT INTO '.TABLE_NAME_ARTIST_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 08:00:00', new DateTimeZone('UTC'))).', 0,'
					.'0, "SyncTest_P_1");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_ARTIST_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 08:00:00', new DateTimeZone('UTC'))).', 0,'
					.'1, "SyncTest_P_2");';
			$pdo->exec($sql);

			// サンプルデータ（TLyristSequence）
			$sql = 'INSERT INTO '.TABLE_NAME_LYRIST_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 09:00:00', new DateTimeZone('UTC'))).', 0,'
					.'0, "SyncTest_P_2");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_LYRIST_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 09:00:00', new DateTimeZone('UTC'))).', 0,'
					.'1, "SyncTest_P_3");';
			$pdo->exec($sql);

			// サンプルデータ（TComposerSequence）
			$sql = 'INSERT INTO '.TABLE_NAME_COMPOSER_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 10:00:00', new DateTimeZone('UTC'))).', 0,'
					.'0, "SyncTest_P_3");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_COMPOSER_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 10:00:00', new DateTimeZone('UTC'))).', 0,'
					.'1, "SyncTest_P_4");';
			$pdo->exec($sql);

			// サンプルデータ（TArrangerSequence）
			$sql = 'INSERT INTO '.TABLE_NAME_ARRANGER_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 11:00:00', new DateTimeZone('UTC'))).', 0,'
					.'0, "SyncTest_P_4");';
			$pdo->exec($sql);
			$sql = 'INSERT INTO '.TABLE_NAME_ARRANGER_SEQUENCE.' VALUES ('
					.'"SyncTest_S_1", 0, 0, '
					.date_time_to_modified_julian_date(new DateTime('2018-01-10 11:00:00', new DateTimeZone('UTC'))).', 0,'
					.'1, "SyncTest_P_5");';
			$pdo->exec($sql);
		}
	}

	// -------------------------------------------------------------------
	// "20180123" 形式の文字列を DateTime に変換
	// "" も変換できる
	// -------------------------------------------------------------------
	private function date_num_string_to_date_time($str)
	{
		$date_num = (int)$str;
		$year = floor($date_num / 10000);
		$now_year = $this->now_year();
		if ( $year < INVALID_YEAR ) {
			$year = INVALID_YEAR;
		} else if ( $year > $now_year ) {
			$year = $now_year;
		}
		$month = floor($date_num / 100) % 100;
		if ( $month < 1 ) {
			$month = 1;
		} else if ( $month > 12 ) {
			$month = 12;
		}
		$day = $date_num % 100;
		if ( $day < 1 ) {
			$day = 1;
		} else if ( $day > 31 ) {
			$day = 31;
		}
		$date_time = new DateTime(null, new DateTimeZone('UTC'));
		$date_time->setDate($year, $month, $day);
		$date_time->setTime(0, 0, 0);
		return $date_time;
	}

	// -------------------------------------------------------------------
	// アカウント削除
	// -------------------------------------------------------------------
	private function delete_account()
	{
		try {
			// POST されたパラメーター取得
			$posted_uid = $this->get_posted_parameter(PARAM_NAME_UID);
			if ( $posted_uid == '' ) {
				throw new Exception('アカウント番号が指定されていません。ユーザー一覧の削除リンクをクリックして下さい。');
			}
			$posted_uid = (int)$posted_uid;
			
			// マスターアカウント確認
			if ( $posted_uid == 0 ) {
				throw new Exception('アカウント番号 0 のユーザーは削除できません。');
			}
			
			// 統計削除
			$pdo = $this->connect_db();
			$sql = 'DELETE FROM '.TABLE_NAME_STATISTICS
					.' WHERE '.FIELD_NAME_STATISTICS_UID.' = :uid;';
			log_message('delete_account() '.$sql, LOG_LEVEL_DEBUG, FALSE);
			$stmt = $pdo->prepare($sql);
			$stmt->bindValue(':uid', $posted_uid, PDO::PARAM_INT);
			$stmt->execute();
			
			// アカウント削除
			$sql = 'DELETE FROM '.TABLE_NAME_ACCOUNT
					.' WHERE '.FIELD_NAME_ACCOUNT_UID.' = :uid;';
			log_message('delete_account() '.$sql, LOG_LEVEL_DEBUG, FALSE);
			$stmt = $pdo->prepare($sql);
			$stmt->bindValue(':uid', $posted_uid, PDO::PARAM_INT);
			$stmt->execute();

			// 結果出力
			$message = 'アカウント #'.$posted_uid.' を削除しました。 ';
			log_message($message, LOG_LEVEL_NOTICE, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_NOTICE_MESSAGE.'='.urlencode($message));
		} catch (Exception $excep) {
			log_message($excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode($excep->getMessage()));
		}
	}

	// -------------------------------------------------------------------
	// フォルダー削除
	// -------------------------------------------------------------------
	private function delete_folder($folder)
	{
		$files = array_diff(scandir($folder), array('.','..'));
		foreach ($files as $file) {
			(is_dir("$folder/$file")) ? $this->delete_folder("$folder/$file") : unlink("$folder/$file");
		}
		return rmdir($folder);
	}
  
	// -------------------------------------------------------------------
	// 更新データがないことを知らせるメッセージをダウンロードさせる
	// -------------------------------------------------------------------
	private function download_no_data()
	{
		$this->download_text(NO_DATA.'.txt', NO_DATA);
	}

	// -------------------------------------------------------------------
	// 前回 POST 時のエラー発生状況をダウンロードさせる
	// 正常終了：先頭が '0'
	// エラー：先頭が '0' 以外、後続にエラーメッセージ
	// -------------------------------------------------------------------
	private function download_post_error()
	{
		$error = '0'.SYSTEM_APP_GENERATION;
		
		if ( !$this->is_logged_in() ) {
			$error = '1ログインエラー。';
		} else if ( isset($_SESSION[SESSION_INFO_POST_ERROR_EXISTS]) 
				&& $_SESSION[SESSION_INFO_POST_ERROR_EXISTS] ) {
			$error = '1'.$_SESSION[SESSION_INFO_POST_ERROR_MESSAGE];
		}

		$this->download_text('PostError.txt', $error);
	}

	// -------------------------------------------------------------------
	// アップロードを拒否したレコードの更新日時をダウンロード
	// 該当レコードが無い場合は本日の日付
	// -------------------------------------------------------------------
	private function download_reject_date()
	{
		$mjd = $this->now_mjd();
		if ( isset($_SESSION[SESSION_INFO_REJECT_UPDATE_TIME]) ) {
			$mjd = $_SESSION[SESSION_INFO_REJECT_UPDATE_TIME];
		}
		$date_time = modified_julian_date_to_date_time($mjd);
		$date = $date_time->format('Ymd');
		$this->download_text('RejectDate.txt', $date);
	}

	// -------------------------------------------------------------------
	// レコードのダウンロード（1 日分）
	// ?Mode=Download&Date=20180123 で 2018/01/23 以降最初に更新データがある日をダウンロード
	// -------------------------------------------------------------------
	private function download_sync_data()
	{
		$th_mjd = date_time_to_modified_julian_date(
				$this->date_num_string_to_date_time(get_url_parameter(PARAM_NAME_DATE)));
		//log_message('download() specified th_mjd: '.$th_mjd, LOG_LEVEL_STATUS, FALSE);
	
		// 指定日以降に最初に更新データがある日を探す
		$update_times = array();
		$pdo = $this->connect_db();
		$sync_tables = $this->sync_tables();
		for ( $i = 0 ; $i < count($sync_tables) ; $i++ ) {
			$table = $sync_tables[$i];
			$field_prefix = $this->field_name_prefix($table);
			$field_update_time = $field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME;
			$sql = 'SELECT MIN('.$field_update_time.') AS min_time FROM '.$table
					.' WHERE '.$field_update_time.' >= '.$th_mjd.';';
			//debug_show_message('download() th sql: '.$sql);
			$stmt = $pdo->query($sql);
			$row = $stmt->fetch(PDO::FETCH_ASSOC);
			if ( $row !== FALSE && $row['min_time'] != NULL ) {
				$update_times[] = $row['min_time'];
				//log_message('download() '.$table.' min_time: '.$row['min_time'], LOG_LEVEL_STATUS, FALSE);
			}
			$stmt->closeCursor();
		}
		
		// 更新データがない場合はその旨のデータをダウンロードさせる
		if ( count($update_times) == 0 ) {
			$th_date_string = modified_julian_date_to_date_time($th_mjd)->format('Ymd');
			log_message('同期データダウンロード：'.$_SESSION[SESSION_INFO_ACCOUNT_NAME].' / '
					.$th_date_string.' / 無し', LOG_LEVEL_STATUS, FALSE);
			$this->download_no_data();
			return;
		}
		
		// 最小の更新日を選択
		sort($update_times, SORT_NUMERIC);
		$th_mjd = floor($update_times[0]);
		//log_message('download() adjusted th_mjd: '.$th_mjd, LOG_LEVEL_STATUS, FALSE);
		$th_date_string = modified_julian_date_to_date_time($th_mjd)->format('Ymd');

		// 一時フォルダー
		$folder_name = $this->temp_path(FILE_NAME_DOWNLOAD_PREFIX);
		mkdir($folder_name);
		
		// 更新データ保存
		$num_records = 0;
		for ( $i = 0 ; $i < count($sync_tables) ; $i++ ) {
			$num_records += $this->save_download_csv($th_mjd, $sync_tables[$i], $folder_name);
		}
		
		// 更新データ情報
		file_put_contents($folder_name.'/'.FILE_NAME_SYNC_INFO, 'Date='.$th_date_string."\n");
		
		// zip 圧縮
		$zip = new ZipArchive();
		$zip_file_name = $folder_name.'.zip';
		if ( $zip->open($zip_file_name, ZipArchive::CREATE) !== TRUE ) {
			throw new Exception('同期データの圧縮準備ができませんでした。');
		}
		$files = glob($folder_name.'/*');
		foreach ( $files as $file ) {
			if ( !$zip->addFile($file, basename($file)) ) {
				throw new Exception('同期データの圧縮ができませんでした。');
			}
		}
		$zip->close();
		log_message('同期データダウンロード：'.$_SESSION[SESSION_INFO_ACCOUNT_NAME].' / '
				.$th_date_string.' / '.$num_records.' 件', LOG_LEVEL_STATUS, FALSE);
		
		// ダウンロード
		header(HEADER_CONTENT_TYPE);
		header(HEADER_CONTENT_LENGTH.filesize($zip_file_name));
		header(HEADER_CONTENT_DISPOSITION.'"'.FILE_NAME_DOWNLOAD_PREFIX.$th_date_string.'.zip"');
		readfile($zip_file_name);
		$this->add_statistics_bytes(filesize($zip_file_name));
		
		// 後片付け
		$this->delete_folder($folder_name);
		unlink($zip_file_name);
		//log_message('download_sync_data() end', LOG_LEVEL_STATUS, FALSE);
	}

	// -------------------------------------------------------------------
	// テキストデータをダウンロードさせる
	// -------------------------------------------------------------------
	private function download_text($file_name, $data)
	{
		header(HEADER_CONTENT_TYPE);
		header(HEADER_CONTENT_LENGTH.strlen($data));
		header(HEADER_CONTENT_DISPOSITION.'"'.$file_name.'"');
		echo($data);
		$this->add_statistics_bytes(strlen($data));
	}

	// -------------------------------------------------------------------
	// CSV の値として格納できる形に変換
	// 改行・ダブルクオート・\・カンマ が含まれる場合はダブルクオートで括る
	// -------------------------------------------------------------------
	private function escape_csv_column($column)
	{
		if ( $column == NULL || $column == '' ) {
			return '';
		}

		$column = str_replace("\"", "\\\"", $column);

		if ( strpbrk($column, "\r\n\"\\,") !== FALSE ) {
			return "\"".$column."\"";
		}

		return $column;
	}

	// -------------------------------------------------------------------
	// カラム名のプレフィックスを算出
	// -------------------------------------------------------------------
	private function field_name_prefix($table_name)
	{
		return substr($table_name, strpos($table_name, '_') + 1).'_';
	}

	// -------------------------------------------------------------------
	// POST されたまたは URL のパラメータを安全に取得
	// ＜引数＞ $name: パラメーター名
	// ＜返値＞ 危険を無力化した後の値文字列（パラメーターが設定されていない場合は空文字列）
	// -------------------------------------------------------------------
	private function get_posted_or_url_parameter($name)
	{
		$parameter = $this->get_posted_parameter($name);
		if ( $parameter != '' ) {
			return $parameter;
		}
		return get_url_parameter($name);
	}

	// -------------------------------------------------------------------
	// POST されたパラメータを安全に取得
	// ＜引数＞ $name: パラメーター名
	// ＜返値＞ 危険を無力化した後の値文字列（パラメーターが設定されていない場合は空文字列）
	// -------------------------------------------------------------------
	private function get_posted_parameter($name)
	{
		if ( isset($_POST[$name]) ) {
			return escape_input($_POST[$name]);
		} else {
			return '';
		}
	}

	// -------------------------------------------------------------------
	// パスワード初期化
	// -------------------------------------------------------------------
	private function init_account()
	{
		try {
			// POST されたパラメーター取得
			$posted_uid = $this->get_posted_parameter(PARAM_NAME_UID);
			if ( $posted_uid == '' ) {
				throw new Exception('アカウント番号が指定されていません。ユーザー一覧の初期化リンクをクリックして下さい。');
			}
			$posted_uid = (int)$posted_uid;
			$posted_pw = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD);
			$posted_pw_confirm = $this->get_posted_parameter(PARAM_NAME_NEW_PASSWORD_CONFIRM);
			
			// 確認パスワードが一致しているか検証
			if ( $posted_pw != $posted_pw_confirm ) {
				throw new Exception('新しいパスワードと確認用パスワードが異なっています。');
			}

			// パスワードの確認
			$this->check_account_password($posted_pw);

			// 変更
			$pdo = $this->connect_db();
			$sql = 'UPDATE '.TABLE_NAME_ACCOUNT
					.' SET '.FIELD_NAME_ACCOUNT_PASSWORD.' = :password,'
					.FIELD_NAME_ACCOUNT_UPDATE_TIME.' = :update_time'
					.' WHERE '.FIELD_NAME_ACCOUNT_UID.' = :uid';
			log_message('init_account() '.$sql, LOG_LEVEL_DEBUG, FALSE);
			$stmt = $pdo->prepare($sql);
			$stmt->bindValue(':password', password_hash($posted_pw, PASSWORD_DEFAULT), PDO::PARAM_STR);
			$stmt->bindValue(':update_time', $this->now_mjd(), PDO::PARAM_STR);
			$stmt->bindValue(':uid', $posted_uid, PDO::PARAM_INT);
			$stmt->execute();

			// 結果出力
			$message = 'アカウント #'.$posted_uid.' のパスワードを初期化しました。 ';
			log_message($message, LOG_LEVEL_NOTICE, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_NOTICE_MESSAGE.'='.urlencode($message));
		} catch (Exception $excep) {
			log_message($excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
			header('Location:'.FILE_NAME_CP_MAIN.'?'.PARAM_NAME_MODE.'='.MODE_NAME_MANAGE_USERS
					.'&'.PARAM_NAME_ERROR_MESSAGE.'='.urlencode($excep->getMessage()));
		}
	}

	// -------------------------------------------------------------------
	// アカウント新規登録
	// -------------------------------------------------------------------
	private function insert_account($uid, $name, $raw_pw, $is_admin)
	{
		$pdo = $this->connect_db();
		$sql = 'INSERT INTO '.TABLE_NAME_ACCOUNT.' VALUES ('
				.$uid.','
				.' "'.$name.'",'
				.' "'.password_hash($raw_pw, PASSWORD_DEFAULT).'",'
				.' '.var_to_string($is_admin).','
				.' '.$this->invalid_mjd().','
				.' '.$this->now_mjd().');';
		log_message('insert_account() '.$sql, LOG_LEVEL_STATUS, FALSE);
		$pdo->exec($sql);
	}

	// -------------------------------------------------------------------
	// ログイン結果記録
	// -------------------------------------------------------------------
	private function insert_login($is_success)
	{
		$pdo = $this->connect_db();
		$sql = 'INSERT INTO '.TABLE_NAME_LOGIN.' VALUES ('
				.':name,'
				.' '.$this->now_mjd().','
				.' '.var_to_string($is_success).','
				.':app_generation,'
				.':app_ver,'
				.':id_prefix,'
				.':sid);';
		$stmt = $pdo->prepare($sql);
		$stmt->bindValue(':name', $this->get_posted_parameter(PARAM_NAME_NAME), PDO::PARAM_STR);
		$stmt->bindValue(':app_generation', $this->get_posted_parameter(PARAM_NAME_APP_GENERATION), PDO::PARAM_STR);
		$stmt->bindValue(':app_ver', $this->get_posted_parameter(PARAM_NAME_APP_VER), PDO::PARAM_STR);
		$stmt->bindValue(':id_prefix', $this->get_posted_parameter(PARAM_NAME_ID_PREFIX), PDO::PARAM_STR);
		$stmt->bindValue(':sid', $this->get_posted_parameter(PARAM_NAME_SID), PDO::PARAM_STR);
		$stmt->execute();
	}

	// -------------------------------------------------------------------
	// 連想配列の内容を INSERT
	// ＜返値＞ 成功：TRUE
	// -------------------------------------------------------------------
	private function insert_record(&$pdo, $table, $record)
	{
		$sql = 'INSERT INTO '.$table.' (';
		
		// カラム名
		$field_prefix = $this->field_name_prefix($table);
		$field_update_time = $field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME;
		$field_sync_by = $field_prefix.FIELD_NAME_SUFFIX_SYNC_BY;
		foreach ( $record as $key => $value ) {
			//$sql .= '"'.$key.'",';
			$sql .= $key.',';
		}
		
		// 同期アカウントのカラム名
		//$sql .= '"'.$field_sync_by.'") VALUES (';
		$sql .= $field_sync_by.') VALUES (';
		
		// 値プレースホルダー
		foreach ( $record as $key => $value ) {
			$sql .= ':'.$key.',';
		}
		
		// 同期アカウントのプレースホルダー
		$sql .= ':'.$field_sync_by.');';
		//log_message('insert_record() sql: '.$sql, LOG_LEVEL_ERROR, FALSE);
		
		// 値
		$stmt = $pdo->prepare($sql);
		foreach ( $record as $key => $value ) {
			if ( $key == $field_update_time ) {
				$stmt->bindValue(':'.$key, $this->now_mjd());
			} else {
				$stmt->bindValue(':'.$key, $value);
			}
		}
		
		// 同期アカウントの値
		$stmt->bindValue(':'.$field_sync_by, $_SESSION[SESSION_INFO_ACCOUNT_UID]);

		// 挿入
		try {
			$stmt->execute();
			return TRUE;
		} catch (Exception $excep) {
			return FALSE;
		}
	}

	// -------------------------------------------------------------------
	// 無効時刻
	// -------------------------------------------------------------------
	private function invalid_mjd()
	{
		return date_time_to_modified_julian_date(new DateTime(INVALID_YEAR.'-01-01', new DateTimeZone('UTC')));
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定
	// （内容が同一と思われるレコードが既に存在する場合は拒否する）
	// ＜返値＞ TRUE：挿入可能、FALSE：挿入拒否
	// -------------------------------------------------------------------
	private function is_insertable_record(&$pdo, $table, $record)
	{
		switch ( $table ) {
			case TABLE_NAME_SONG:
				return $this->is_insertable_record_song($pdo, $record);
			case TABLE_NAME_PERSON:
				return $this->is_insertable_record_person($pdo, $record);
			case TABLE_NAME_TIE_UP:
				return $this->is_insertable_record_tie_up($pdo, $record);
			case TABLE_NAME_TIE_UP_GROUP:
				return $this->is_insertable_record_tie_up_group($pdo, $record);
			case TABLE_NAME_MAKER:
				return $this->is_insertable_record_maker($pdo, $record);
			case TABLE_NAME_SONG_ALIAS:
				return $this->is_insertable_record_song_alias($pdo, $record);
			case TABLE_NAME_TIE_UP_ALIAS:
				return $this->is_insertable_record_tie_up_alias($pdo, $record);
			
		}
	
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TMaker）
	// 制作会社名が似ている場合、既存に検索キーワードが同じものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_maker(&$pdo, $record)
	{
		$rows = $this->select_similar_records($pdo, TABLE_NAME_MAKER, FIELD_NAME_MAKER_NAME,
				$record[FIELD_NAME_MAKER_NAME]);
		foreach ( $rows as $row ) {
			if ( $row[FIELD_NAME_MAKER_KEYWORD] == $record[FIELD_NAME_MAKER_KEYWORD] ) {
				return FALSE;
			}
		}
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TPerson）
	// 人物名が似ている場合、新規レコードがインポート、または、
	// 既存に検索キーワードが同じものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_person(&$pdo, $record)
	{
		$rows = $this->select_similar_records($pdo, TABLE_NAME_PERSON, FIELD_NAME_PERSON_NAME,
				$record[FIELD_NAME_PERSON_NAME]);
		foreach ( $rows as $row ) {
			if ( $record[FIELD_NAME_PERSON_IMPORT] ) {
				return FALSE;
			}

			if ( $row[FIELD_NAME_PERSON_KEYWORD] == $record[FIELD_NAME_PERSON_KEYWORD] ) {
				return FALSE;
			}
		}
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TSong）
	// 楽曲名が似ている場合、新規レコードがインポート、または、
	// 既存にタイアップ名・検索キーワードが同じものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_song(&$pdo, $record)
	{
		// タイアップ名の解決
		$tie_up_name = NULL;
		if ( $record[FIELD_NAME_SONG_TIE_UP_ID] != NULL ) {
			$tie_up_row = $this->select_tie_up_by_id($pdo, $record[FIELD_NAME_SONG_TIE_UP_ID]);
			if ( $tie_up_row !== FALSE ) {
				$tie_up_name = $tie_up_row[FIELD_NAME_TIE_UP_NAME];
			}
		}
	
		$rows = $this->select_similar_records($pdo, TABLE_NAME_SONG, FIELD_NAME_SONG_NAME,
				$record[FIELD_NAME_SONG_NAME]);
		foreach ( $rows as $row ) {
			if ( $record[FIELD_NAME_SONG_IMPORT] ) {
				return FALSE;
			}

			$tie_up_name_2 = NULL;
			if ( $row[FIELD_NAME_SONG_TIE_UP_ID] != NULL ) {
				$tie_up_row_2 = $this->select_tie_up_by_id($pdo, $row[FIELD_NAME_SONG_TIE_UP_ID]);
				if ( $tie_up_row_2 !== FALSE ) {
					$tie_up_name_2 = $tie_up_row_2[FIELD_NAME_TIE_UP_NAME];
				}
			}
				
			if ( $tie_up_name_2 == $tie_up_name
					&& $row[FIELD_NAME_SONG_KEYWORD] == $record[FIELD_NAME_SONG_KEYWORD] ) {
				return FALSE;
			}
		}
		
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TSongAlias）
	// エイリアス名が同じものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_song_alias(&$pdo, $record)
	{
		$rows = $this->select_same_records($pdo, TABLE_NAME_SONG_ALIAS, FIELD_NAME_SONG_ALIAS_ALIAS,
				$record[FIELD_NAME_SONG_ALIAS_ALIAS]);
		foreach ( $rows as $row ) {
			return FALSE;
		}
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TTieUp）
	// タイアップ名が似ている場合、新規レコードがインポート、または、
	// 既存に検索キーワードが同じものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_tie_up(&$pdo, $record)
	{
		$rows = $this->select_similar_records($pdo, TABLE_NAME_TIE_UP, FIELD_NAME_TIE_UP_NAME,
				$record[FIELD_NAME_TIE_UP_NAME]);
		foreach ( $rows as $row ) {
			if ( $record[FIELD_NAME_TIE_UP_IMPORT] ) {
				return FALSE;
			}

			if ( $row[FIELD_NAME_TIE_UP_KEYWORD] == $record[FIELD_NAME_TIE_UP_KEYWORD] ) {
				return FALSE;
			}
		}
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TTieUpAlias）
	// エイリアス名が同じものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_tie_up_alias(&$pdo, $record)
	{
		$rows = $this->select_same_records($pdo, TABLE_NAME_TIE_UP_ALIAS, FIELD_NAME_TIE_UP_ALIAS_ALIAS,
				$record[FIELD_NAME_TIE_UP_ALIAS_ALIAS]);
		foreach ( $rows as $row ) {
			return FALSE;
		}
		return TRUE;
	}

	// -------------------------------------------------------------------
	// 新規レコードを挿入して良いかどうか判定（TTieUpGroup）
	// タイアップグループ名が似ているものがあると拒否
	// -------------------------------------------------------------------
	private function is_insertable_record_tie_up_group(&$pdo, $record)
	{
		$rows = $this->select_similar_records($pdo, TABLE_NAME_TIE_UP_GROUP, FIELD_NAME_TIE_UP_GROUP_NAME,
				$record[FIELD_NAME_TIE_UP_GROUP_NAME]);
		foreach ( $rows as $row ) {
			return FALSE;
		}
		return TRUE;
	}

	// -------------------------------------------------------------------
	// CSV ファイル読み込み
	// -------------------------------------------------------------------
	private function load_csv($path)
	{
		$header = NULL;
		$contents = array();
		$spl_file = new SplFileObject($path);
		$spl_file->setFlags(SplFileObject::READ_CSV);
		foreach ( $spl_file as $line ) {
			if ( empty($line[0]) ) {
				continue;
			}
			
			if ( $header == NULL ) {
				// 最初の行はヘッダー行
				$header = $line;
				$header[0] = $this->remove_bom($header[0]);
			} else {
				// 仕様変更によりカラム順序が変わっても対応できるよう、連想配列にする
				$record = array();
				for ( $i = 0 ; $i < count($header) ; $i++ ) {
					$record[$header[$i]] = str_replace("\\\"", "\"", $line[$i]);
				}
				$contents[] = $record;
			}
		}
		
		if ( DEBUG_FLAG && FALSE ) {
			var_dump($contents);
		}
		
		return $contents;
	}

	// -------------------------------------------------------------------
	// ログイン状態にする
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function login($row)
	{
		$_SESSION[SESSION_INFO_ACCOUNT_UID] = $row[FIELD_NAME_ACCOUNT_UID];
		$_SESSION[SESSION_INFO_ACCOUNT_NAME] = $row[FIELD_NAME_ACCOUNT_NAME];
		$_SESSION[SESSION_INFO_ACCOUNT_ADMIN] = $row[FIELD_NAME_ACCOUNT_ADMIN] != 0;
		$_SESSION[SESSION_INFO_ACCOUNT_LOGIN_TIME] = $row[FIELD_NAME_ACCOUNT_LOGIN_TIME];
		$_SESSION[SESSION_INFO_POST_ERROR_EXISTS] = FALSE;
		
		// ログイン時刻はログインテーブルから取得する
		$last_login = $this->select_last_login($row[FIELD_NAME_ACCOUNT_NAME]);
		if ( $last_login !== FALSE) {
			$_SESSION[SESSION_INFO_ACCOUNT_LOGIN_TIME] = $last_login[FIELD_NAME_LOGIN_TIME];
		}

		log_message('ログイン完了'.$this->user_info(), LOG_LEVEL_NOTICE, FALSE);
	}

	// -------------------------------------------------------------------
	// ログアウトしてログイン画面に遷移
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function logout()
	{
		$uid = $_SESSION[SESSION_INFO_ACCOUNT_UID];
		$name = $_SESSION[SESSION_INFO_ACCOUNT_NAME];
	
		// セッション変数を破棄
		$_SESSION = array();

		// クッキーを破棄
		if ( isset($_COOKIE[COOKIE_NAME_PHP_SESSION_ID]) ) {
			setcookie(COOKIE_NAME_PHP_SESSION_ID, '', time()-3600, '/');
    	}

		// セッションを破棄
		session_destroy();
		log_message('ログアウト ［ユーザー ID：'.$uid.' / '.$name.'］', LOG_LEVEL_NOTICE, FALSE);

		// 遷移
		header('Location:'.FILE_NAME_CP_LOGIN.'?'.PARAM_NAME_NOTICE_MESSAGE.'='.urlencode('ログアウトしました。'));
	}

	// -------------------------------------------------------------------
	// ユーザー管理フォーム表示
	// -------------------------------------------------------------------
	private function manage_users()
	{
		// ユーザー一覧とユーザー情報関数
		$user_list = '';
		$function_get_user_info = '';
		$pdo = $this->connect_db();
		$sql = 'SELECT *'
				.' FROM '.TABLE_NAME_ACCOUNT
				.' LEFT JOIN '
				.'('
					.'SELECT li1.'.FIELD_NAME_LOGIN_NAME.' AS ln, li1.'.FIELD_NAME_LOGIN_TIME.' AS lt'
					.' FROM '.TABLE_NAME_LOGIN.' AS li1'
					.' LEFT JOIN '.TABLE_NAME_LOGIN.' AS li2'
					.' ON (li1.'.FIELD_NAME_LOGIN_NAME.' = li2.'.FIELD_NAME_LOGIN_NAME
					.' AND li1.'.FIELD_NAME_LOGIN_TIME.' < li2.'.FIELD_NAME_LOGIN_TIME.')'
					.' WHERE li1.'.FIELD_NAME_LOGIN_SUCCESS.' = 1 AND li2.'.FIELD_NAME_LOGIN_TIME.' IS NULL'
				.') AS result'
				.' ON '.TABLE_NAME_ACCOUNT.'.'.FIELD_NAME_ACCOUNT_NAME.' = ln'
				.' ORDER BY '.TABLE_NAME_ACCOUNT.'.'.FIELD_NAME_ACCOUNT_UID;
		$stmt = $pdo->query($sql);
		while ( ($row = $stmt->fetch(PDO::FETCH_ASSOC)) !== FALSE ) {
			$login_time = modified_julian_date_to_date_time($row['lt']);
			$login_time->setTimeZone(new DateTimeZone(date_default_timezone_get()));
			$update_time = modified_julian_date_to_date_time($row[FIELD_NAME_ACCOUNT_UPDATE_TIME]);
			$update_time->setTimeZone(new DateTimeZone(date_default_timezone_get()));
			$user_list .= '<tr><th>'.$row[FIELD_NAME_ACCOUNT_UID].'</th>'
					.'<td>'.$row[FIELD_NAME_ACCOUNT_NAME].'</td>'
					.'<td>'.($row[FIELD_NAME_ACCOUNT_ADMIN] ? '○' : '×').'</td>'
					.'<td>'.$login_time->format('Y/m/d H:i:s').'</td>'
					.'<td>'.$update_time->format('Y/m/d H:i:s').'</td>'
					.'<td><a href="#Change" onClick="changeUser('.$row[FIELD_NAME_ACCOUNT_UID].');">変更</a></td>'
					.'<td><a href="#Init" onClick="initUser('.$row[FIELD_NAME_ACCOUNT_UID].');">初期化</a></td>'
					.'<td><a href="#Delete" onClick="deleteUser('.$row[FIELD_NAME_ACCOUNT_UID].');">削除</a></td></tr>';
			$function_get_user_info .= 'user'.$row[FIELD_NAME_ACCOUNT_UID].' = new Object();'
					.'user'.$row[FIELD_NAME_ACCOUNT_UID].'["Name"] = "'.$row[FIELD_NAME_ACCOUNT_NAME].'";'
					.'user'.$row[FIELD_NAME_ACCOUNT_UID].'["Admin"] = "'.var_to_string($row[FIELD_NAME_ACCOUNT_ADMIN]).'";'
					.'userInfo['.$row[FIELD_NAME_ACCOUNT_UID].'] = user'.$row[FIELD_NAME_ACCOUNT_UID].';';
		}
		$stmt->closeCursor();

		// テンプレート適用
		$vars = array();
		$vars[TMPL_MARK_USER_LIST] = $user_list;
		$vars[TMPL_MARK_FUNCTION_GET_USER_INFO] = $function_get_user_info;

		// フォーム表示
		$this->show_form('ユーザー管理フォーム', FILE_NAME_TEMPLATE_MANAGE_USERS, $vars);
	}

	// -------------------------------------------------------------------
	// マスターアカウントかデータベース上存在しているか
	// ＜引数＞ $table_exists: データベースにテーブルがあるかを格納
	// ＜返値＞ TRUE: 存在する, FALSE: 存在しない
	// -------------------------------------------------------------------
	private function master_account_exists(&$table_exists = NULL)
	{
		$table_exists = FALSE;
		$account_exists = FALSE;
		$pdo = $this->connect_db();
		$sql = 'SELECT * FROM '.TABLE_NAME_ACCOUNT
				.' WHERE '.FIELD_NAME_ACCOUNT_UID.' = 0';
		try {
			$stmt = $pdo->query($sql);
			$table_exists = TRUE;
			$account_exists = $stmt->fetch(PDO::FETCH_ASSOC) !== FALSE;
			$stmt->closeCursor();
		} catch (PDOException $excep) {
			// TABLE_NAME_ACCOUNT 未作成の場合は例外が発生する
		}
		
		return $account_exists;
	}

	// -------------------------------------------------------------------
	// 現在時刻 UTC（修正ユリウス日）
	// -------------------------------------------------------------------
	private function now_mjd()
	{
		return date_time_to_modified_julian_date(new DateTime(null, new DateTimeZone('UTC')));
	}

	// -------------------------------------------------------------------
	// 当月 1 日 UTC（修正ユリウス日）
	// -------------------------------------------------------------------
	private function now_month_mjd()
	{
		$now_month = new DateTime(null, new DateTimeZone('UTC'));
		$now_month->setDate($now_month->format('Y'), $now_month->format('n'), 1);
		$now_month->setTime(0, 0, 0);
		return date_time_to_modified_julian_date($now_month);
	}

	// -------------------------------------------------------------------
	// 現在年
	// -------------------------------------------------------------------
	private function now_year()
	{
		$date = new DateTime(null, new DateTimeZone('UTC'));
		return (int)$date->format('Y');
	}

	// -------------------------------------------------------------------
	// 先頭の BOM を除去
	// -------------------------------------------------------------------
	private function remove_bom($str)
	{
		$bom = "\xEF\xBB\xBF";
		if ( strpos($str, $bom) === FALSE ) {
			return $str;
		}
		return substr($str, strlen($bom));
	}

	// -------------------------------------------------------------------
	// 更新データを CSV に保存
	// ＜返値＞ 保存した件数
	// -------------------------------------------------------------------
	private function save_download_csv($th_mjd, $table, $folder_name)
	{
		$field_prefix = $this->field_name_prefix($table);
		$field_update_time = $field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME;
		$field_sync_by = $field_prefix.FIELD_NAME_SUFFIX_SYNC_BY;

		$pdo = $this->connect_db();
		$sql = 'SELECT * FROM '.$table
				.' WHERE '.$th_mjd.' <= '.$field_update_time
				.' AND '.$field_update_time.' < '.($th_mjd+1).';';
		$stmt = $pdo->query($sql);

		$contents = '';
		$num_records = 0;
		while ( ($row = $stmt->fetch(PDO::FETCH_ASSOC)) !== FALSE ) {
			// 同期アカウント UID 列は無視する
			unset($row[$field_sync_by]);
			if ( $contents == '' ) {
				// タイトル行
				$this->save_download_csv_add_record($contents, array_keys($row));
			}
			$this->save_download_csv_add_record($contents, $row);
			$num_records++;
		}
		
		if ( $contents == '' ) {
			return $num_records;
		}
		
		file_put_contents($folder_name.'/'.$table.'.csv', $contents);
		return $num_records;
	}
	
	// -------------------------------------------------------------------
	// 更新データ 1 レコード分を追加
	// -------------------------------------------------------------------
	private function save_download_csv_add_record(&$contents, $row)
	{
		foreach ( $row as $column ) {
			$contents .= $this->escape_csv_column($column).',';
		}
	
		// 末尾のカンマを改行に変更
		$contents[strlen($contents) - 1] = "\n";
	}
	
	// -------------------------------------------------------------------
	// アカウント名とパスワードの組み合わせがあるか検索
	// ＜返値＞ アカウントレコード、見つからない場合は FALSE
	// -------------------------------------------------------------------
	private function select_account_by_name_and_password($name, $raw_pw)
	{
		$pdo = $this->connect_db();
		$sql = 'SELECT * FROM '.TABLE_NAME_ACCOUNT
				.' WHERE '.FIELD_NAME_ACCOUNT_NAME.' = :name';
		$stmt = $pdo->prepare($sql);
		$stmt->bindValue(':name', $name, PDO::PARAM_STR);
		$stmt->execute();
		$row = $stmt->fetch(PDO::FETCH_ASSOC);
		$stmt->closeCursor();
		if ( $row === FALSE ) {
			return FALSE;
		}

		if ( !password_verify($raw_pw, $row[FIELD_NAME_ACCOUNT_PASSWORD]) ) {
			return FALSE;
		}
		
		return $row;
	}

	// -------------------------------------------------------------------
	// 前回ログインに成功した時の情報を返す
	// -------------------------------------------------------------------
	private function select_last_login($name)
	{
		$pdo = $this->connect_db();
		$sql = 'SELECT * FROM '.TABLE_NAME_LOGIN
				.' WHERE '.FIELD_NAME_LOGIN_NAME.' = :name'
				.' AND '.FIELD_NAME_LOGIN_SUCCESS.' = 1'
				.' ORDER BY '.FIELD_NAME_LOGIN_TIME.' DESC';
		$stmt = $pdo->prepare($sql);
		$stmt->bindValue(':name', $name, PDO::PARAM_STR);
		$stmt->execute();
		$row = $stmt->fetch(PDO::FETCH_ASSOC);
		$stmt->closeCursor();
		if ( $row === FALSE ) {
			return FALSE;
		}
		return $row;
	}

	// -------------------------------------------------------------------
	// $table の中から、$field が $value と同じレコードを列挙
	// invalid カラムが FALSE のもののみ
	// -------------------------------------------------------------------
	private function select_same_records(&$pdo, $table, $field, $value)
	{
		$records = array();
		$sql = 'SELECT * FROM '.$table
				.' WHERE '.$this->field_name_prefix($table).FIELD_NAME_SUFFIX_INVALID.' = 0'
				.' AND '.$field.' = :'.$field.';';
		//log_message('select_same_records() sql: '.$sql, LOG_LEVEL_STATUS, FALSE);
		$stmt = $pdo->prepare($sql);
		$stmt->bindValue(':'.$field, $value);
		$stmt->execute();
		
		while ( ($row = $stmt->fetch(PDO::FETCH_ASSOC)) !== FALSE ) {
			$records[] = $row;
		}
		$stmt->closeCursor();

		return $records;
	}

	// -------------------------------------------------------------------
	// $table の中から、$field が $value に似ているレコードを列挙
	// 空白を無視し、大文字小文字を区別せずに比較する
	// invalid カラムが FALSE のもののみ
	// -------------------------------------------------------------------
	private function select_similar_records(&$pdo, $table, $field, $value)
	{
		$records = array();
		$sql = 'SELECT * FROM '.$table
				.' WHERE '.$this->field_name_prefix($table).FIELD_NAME_SUFFIX_INVALID.' = 0';
		$value_parts = explode(' ', $value);
		for ( $i = 0 ; $i < count($value_parts) ; $i++ ) {
			$sql .= ' AND '.$field.' COLLATE utf8_general_ci LIKE :'.$field.$i;
		}
		$sql .= ';';
		//log_message('select_similar_records() sql: '.$sql, LOG_LEVEL_DEBUG, FALSE);
		$stmt = $pdo->prepare($sql);
		for ( $i = 0 ; $i < count($value_parts) ; $i++ ) {
			$stmt->bindValue(':'.$field.$i, '%'.$value_parts[$i].'%');
		}
		$stmt->execute();
		
		$value_no_space = str_replace(' ', '', $value);
		while ( ($row = $stmt->fetch(PDO::FETCH_ASSOC)) !== FALSE ) {
			//log_message('select_similar_records() match: '.$row[$field], LOG_LEVEL_DEBUG, FALSE);
			if ( strcasecmp($value_no_space, str_replace(' ', '', $row[$field])) == 0 ) {
				$records[] = $row;
			}
		}
		$stmt->closeCursor();

		return $records;
	}

	// -------------------------------------------------------------------
	// ログイン中のアカウントの指定月の統計レコード
	// ＜返値＞ 統計レコード、見つからない場合は FALSE
	// -------------------------------------------------------------------
	private function select_statistics($month_mjd)
	{
		$pdo = $this->connect_db();
		$sql = 'SELECT * FROM '.TABLE_NAME_STATISTICS
				.' WHERE '.FIELD_NAME_STATISTICS_UID.' = '.$_SESSION[SESSION_INFO_ACCOUNT_UID]
				.' AND '.FIELD_NAME_STATISTICS_MONTH.' = '.$month_mjd.';';
		$stmt = $pdo->query($sql);
		$row = $stmt->fetch(PDO::FETCH_ASSOC);
		$stmt->closeCursor();
		return $row;
	}

	// -------------------------------------------------------------------
	// 登録済みの同期データを返す
	// ＜返値＞ 同期レコード、見つからない場合は FALSE
	// -------------------------------------------------------------------
	private function select_sync_data(&$pdo, $table, $record, $field_id, $field_sequence)
	{
		$is_sequence = strpos($table, 'sequence') !== FALSE;
		$sql = 'SELECT * FROM '.$table
				.' WHERE '.$field_id.' = :id';
		if ( $is_sequence ) {
			$sql .= ' AND '.$field_sequence.' = :sequence';
		}
		$stmt = $pdo->prepare($sql);
		$stmt->bindValue(':id', $record[$field_id], PDO::PARAM_STR);
		if ( $is_sequence ) {
			$stmt->bindValue(':sequence', $record[$field_sequence], PDO::PARAM_INT);
		}
		$stmt->execute();
		$row = $stmt->fetch(PDO::FETCH_ASSOC);
		$stmt->closeCursor();
		return $row;
	}

	// -------------------------------------------------------------------
	// タイアップレコードを返す
	// ＜返値＞ タイアップレコード、見つからない場合は FALSE
	// -------------------------------------------------------------------
	private function select_tie_up_by_id(&$pdo, $tie_up_id)
	{
		$sql = 'SELECT * FROM '.TABLE_NAME_TIE_UP
				.' WHERE '.FIELD_NAME_TIE_UP_ID.' = :id';
		$stmt = $pdo->prepare($sql);
		$stmt->bindValue(':id', $tie_up_id, PDO::PARAM_STR);
		$stmt->execute();
		$row = $stmt->fetch(PDO::FETCH_ASSOC);
		$stmt->closeCursor();
		return $row;
	}

	// -------------------------------------------------------------------
	// クライアントからの同期を拒否したレコードの更新日時
	// 最も古い更新日時を残す（その日からのダウンロードを推奨するため）
	// -------------------------------------------------------------------
	private function set_reject_update_time($update_time)
	{
		if ( !isset($_SESSION[SESSION_INFO_REJECT_UPDATE_TIME]) ) {
			$_SESSION[SESSION_INFO_REJECT_UPDATE_TIME] = $update_time;
			return;
		}
	
		if ( $update_time < $_SESSION[SESSION_INFO_REJECT_UPDATE_TIME] ) {
			$_SESSION[SESSION_INFO_REJECT_UPDATE_TIME] = $update_time;
		}
	}

	// -------------------------------------------------------------------
	// フォーム表示用共通関数
	// ＜引数＞ form_name: 表示用のフォーム名, template_file_name: テンプレートファイル名
	//          vars: apply_template_container() に渡す置換用文字列群
	// ＜返値＞ void
	// -------------------------------------------------------------------
	private function show_form($form_name, $template_file_name, &$vars)
	{
		// コンテンツ作成
		$contents = file_get_contents(FOLDER_NAME_TEMPLATE.$template_file_name);
		if ( $contents === FALSE ) {
			error_die($form_name.'を表示できません。(1)');
		}

		// ヘッドライン
		$headline = '';
		if ( $this->_notice_message != '' ) {
			$headline = get_headline_notice_html($this->_notice_message);
		}
		if ( $this->_err_message != '' ) {
			$headline = get_headline_error_html($this->_err_message);
		}

		// メニュー
		$menu_admin = '';
		if ( $_SESSION[SESSION_INFO_ACCOUNT_ADMIN] ) {
			$menu_admin = '<div class="SideMenuTitle">管理者メニュー</div>'
					.'<div class="SideMenuItem"><a href="?Mode='.MODE_NAME_MANAGE_USERS.'">ユーザー管理</a></div>'
					.'<div class="SideMenuItem"><a href="?Mode='.MODE_NAME_VIEW_NUM_DATA.'">データ数表示</a></div>'
					.'<div class="SideMenuItem"><a href="?Mode='.MODE_NAME_VIEW_NUM_COMMIT.'">コミット数表示</a></div>'
					.'<div class="SideMenuItem"><a href="?Mode='.MODE_NAME_VIEW_LOG.'">ログ表示</a></div>';
		}
		$menu_item_admin_debug = '';
		if ( $_SESSION[SESSION_INFO_ACCOUNT_ADMIN] && DEBUG_FLAG ) {
			$menu_item_admin_debug = '<div class="SideMenuItem"><a href="?Mode='
					.MODE_NAME_UPLOAD_TEST.'">アップロードテスト</a></div>';
		}

		// テンプレート適用
		$vars[ATCV_CONTAINER_FILE] = FILE_NAME_TEMPLATE_CONTAINER_MAIN;
		$vars[TMPL_MARK_HEADLINE] = $headline;
		$vars[TMPL_MARK_MENU_ADMIN] = $menu_admin;
		$output = apply_template_container($contents, $vars);
		if ( $output === FALSE ) {
			error_die($form_name.'を表示できません。(2)');
		}
		echo($output);
	}

	// -------------------------------------------------------------------
	// 同期対象テーブル群
	// -------------------------------------------------------------------
	private function sync_tables()
	{
		return array(TABLE_NAME_SONG, TABLE_NAME_PERSON, TABLE_NAME_TIE_UP, TABLE_NAME_CATEGORY,
				TABLE_NAME_TIE_UP_GROUP, TABLE_NAME_MAKER,
				TABLE_NAME_SONG_ALIAS, TABLE_NAME_TIE_UP_ALIAS,
				TABLE_NAME_ARTIST_SEQUENCE, TABLE_NAME_LYRIST_SEQUENCE, TABLE_NAME_COMPOSER_SEQUENCE,
				TABLE_NAME_ARRANGER_SEQUENCE, TABLE_NAME_TIE_UP_GROUP_SEQUENCE);
	}

	// -------------------------------------------------------------------
	// 一時ファイル・フォルダーとして使えるユニークなパス
	// -------------------------------------------------------------------
	private function temp_path($prefix)
	{
		return dirname(__FILE__).'/../'.FOLDER_NAME_DATA.$prefix.uniqid().'_'.mt_rand(1000, 9999);
	}

	// -------------------------------------------------------------------
	// 連想配列の内容を UPDATE
	// ＜返値＞ 成功：TRUE
	// -------------------------------------------------------------------
	private function update_record(&$pdo, $table, $record)
	{
		$is_sequence = strpos($table, 'sequence') !== FALSE;

		$sql = 'UPDATE '.$table.' SET ';
		
		// カラム名と値プレースホルダー
		$field_prefix = $this->field_name_prefix($table);
		$field_id = $field_prefix.FIELD_NAME_SUFFIX_ID;
		$field_sequence = $field_prefix.FIELD_NAME_SUFFIX_SEQUENCE;
		$field_update_time = $field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME;
		$field_sync_by = $field_prefix.FIELD_NAME_SUFFIX_SYNC_BY;
		foreach ( $record as $key => $value ) {
			if ( $key != $field_id ) {
				//$sql .= '"'.$key.'" = :'.$key.',';
				$sql .= $key.' = :'.$key.',';
			}
		}
		
		// 同期アカウントのカラム名と値プレースホルダー
		$sql .= $field_sync_by.' = :'.$field_sync_by;
		
		// WHERE
		$sql .= ' WHERE '.$field_id.' = :'.$field_id
				.' AND '.$field_update_time.' = :'.$field_update_time.'2';
		if ( $is_sequence ) {
			$sql .= ' AND '.$field_sequence.' = :'.$field_sequence.'2';
		}
		$sql .= ';';
		//log_message('update_record() sql: '.$sql, LOG_LEVEL_STATUS, FALSE);
		
		// 値
		$stmt = $pdo->prepare($sql);
		foreach ( $record as $key => $value ) {
			if ( $key == $field_update_time ) {
				$stmt->bindValue(':'.$key, $this->now_mjd());
			} else {
				$stmt->bindValue(':'.$key, $value);
			}
		}
		
		// 同期アカウントの値
		$stmt->bindValue(':'.$field_sync_by, $_SESSION[SESSION_INFO_ACCOUNT_UID]);

		// WHERE の更新日時の値
		$stmt->bindValue(':'.$field_update_time.'2', $record[$field_update_time]);

		// WHERE の連番の値
		if ( $is_sequence ) {
			$stmt->bindValue(':'.$field_sequence.'2', $record[$field_sequence]);
		}

		// 更新
		$stmt->execute();
		return ( $stmt->rowCount() == 1 );
	}

	// -------------------------------------------------------------------
	// レコードのアップロード（1 テーブル）
	// 更新はサーバーに保持されているレコードの「更新日時」とクライアントから送られてくる
	// レコードの「更新日時」が一致する場合のみ許可する
	// このため、サーバーデータをダウンロードしてから一番早いクライアントのみが更新できる
	// -------------------------------------------------------------------
	private function upload_sync_data()
	{
		try {
			$_SESSION[SESSION_INFO_POST_ERROR_EXISTS] = FALSE;
			//log_message('upload_sync_data()', LOG_LEVEL_STATUS, FALSE);
			if ( !isset($_FILES[PARAM_NAME_FILE]) ) {
				throw new Exception('アップロードされるファイルが指定されていません。');
			}
			
			// アップロードされたファイルを一時ファイルに保存
			$csv_file = $this->temp_path(FILE_NAME_UPLOAD_PREFIX);
			if ( !move_uploaded_file($_FILES[PARAM_NAME_FILE]['tmp_name'], $csv_file) ) {
				throw new Exception('アップロードされたファイルが扱えません。');
			}
			$this->add_statistics_bytes(filesize($csv_file));

			// CSV 読み込み
			$contents = $this->load_csv($csv_file);

			// 更新または新規登録
			$table = pathinfo($_FILES[PARAM_NAME_FILE]['name'], PATHINFO_FILENAME);
			//log_message('upload_sync_data() table_name: '.$table, LOG_LEVEL_STATUS, FALSE);
			$field_prefix = $this->field_name_prefix($table);
			$field_id = $field_prefix.FIELD_NAME_SUFFIX_ID;
			$field_sequence = $field_prefix.FIELD_NAME_SUFFIX_SEQUENCE;
			$field_update_time = $field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME;
			$field_invalid = $field_prefix.FIELD_NAME_SUFFIX_INVALID;
			$pdo = $this->connect_db();
			$pdo->beginTransaction();
			
			$num_accepts = 0;
			$num_rejects = 0;
			foreach ( $contents as $record ) {
				log_message('アップロード中...', LOG_LEVEL_ERROR, FALSE);
				$row = $this->select_sync_data($pdo, $table, $record, $field_id, $field_sequence);
				if ( $row === FALSE ) {
					if ( DEBUG_FLAG && $_SESSION[SESSION_INFO_ACCOUNT_UID] == 1 && FALSE) {
						sleep(10);
					}
					if ( $this->is_insertable_record($pdo, $table, $record) ) {
						if ( $this->insert_record($pdo, $table, $record) ) {
							// 新規登録
							$num_accepts++;
							log_message('アップロード受諾（新規）'.$this->user_info()
									.' '.$table.' / '.$record[$field_id], LOG_LEVEL_STATUS, FALSE,
									FILE_NAME_SYNC_DETAIL_LOG);
						} else {
							$num_rejects++;
							log_message('アップロード拒否（新規）'.$this->user_info()
									.' '.$table.' / '.$record[$field_id], LOG_LEVEL_STATUS, FALSE,
									FILE_NAME_SYNC_DETAIL_LOG);
						}
					} else {
						// INVALID フラグを立てて登録する
						$record[$field_invalid] = TRUE;
						$this->insert_record($pdo, $table, $record);
						$num_rejects++;
						log_message('アップロード拒否（新規）'.$this->user_info()
								.' '.$table.' / '.$record[$field_id], LOG_LEVEL_STATUS, FALSE,
								FILE_NAME_SYNC_DETAIL_LOG);
					}
				} else {
					if ( DEBUG_FLAG && $_SESSION[SESSION_INFO_ACCOUNT_UID] == 1 && FALSE ) {
						sleep(10);
					}
					if ( $record[$field_update_time] == $row[$field_update_time]
							&& $this->update_record($pdo, $table, $record) ) {
						// 更新
						$num_accepts++;
						log_message('アップロード受諾（更新）'.$this->user_info()
								.' '.$table.' / '.$record[$field_id], LOG_LEVEL_STATUS, FALSE,
								FILE_NAME_SYNC_DETAIL_LOG);
					} else {
						$this->set_reject_update_time($row[$field_update_time]);
						$num_rejects++;
						log_message('アップロード拒否（更新）'.$this->user_info()
								.' '.$table.' / '.$record[$field_id], LOG_LEVEL_STATUS, FALSE,
								FILE_NAME_SYNC_DETAIL_LOG);
					}
				}
			}
			
			// 後片付け
			unlink($csv_file);

			// コミット
			if ( !$pdo->commit() ) {
				throw new Exception('コミットできませんでした。時間をおいて再度試してみて下さい。');
			}
			$this->add_statistics_accepts_and_rejects($num_accepts, $num_rejects);

		} catch (Exception $excep) {
			$_SESSION[SESSION_INFO_POST_ERROR_EXISTS] = TRUE;
			$_SESSION[SESSION_INFO_POST_ERROR_MESSAGE] = $excep->getMessage();
			log_message('アップロードエラー：'.$excep->getMessage(), LOG_LEVEL_ERROR, FALSE);
		}
	
	
	}

	// -------------------------------------------------------------------
	// アップロードテスト
	// -------------------------------------------------------------------
	private function upload_test()
	{
		// テンプレート適用
		$vars = array();
		$vars[TMPL_MARK_TITLE_DETAIL] = 'アップロードテスト';

		// フォーム表示
		$this->show_form('アップロードテストフォーム', FILE_NAME_TEMPLATE_UPLOAD_TEST, $vars);
	}

	// -------------------------------------------------------------------
	// ログ用ユーザー情報
	// -------------------------------------------------------------------
	private function user_info()
	{
		return ' ［ユーザー ID：'.$_SESSION[SESSION_INFO_ACCOUNT_UID]
				.' / '.$_SESSION[SESSION_INFO_ACCOUNT_NAME].'］';
	}

	// -------------------------------------------------------------------
	// ログ表示
	// -------------------------------------------------------------------
	private function view_log()
	{
		// テンプレート適用
		$vars = array();

		// ログ
		$log = file_get_contents(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.FILE_NAME_DEFAULT_CP_LOG);
		if ( $log !== FALSE ) {
			$vars[TMPL_MARK_LOG] = $log;
		}

		// 同期詳細ログ
		$sync_detail_log = file_get_contents(dirname(__FILE__).'/../'.FOLDER_NAME_DATA.FILE_NAME_SYNC_DETAIL_LOG);
		if ( $sync_detail_log !== FALSE ) {
			$vars[TMPL_MARK_SYNC_DETAIL_LOG] = $sync_detail_log;
		}

		// フォーム表示
		$this->show_form('ログ表示フォーム', FILE_NAME_TEMPLATE_VIEW_LOG, $vars);
	}

	// -------------------------------------------------------------------
	// コミット数表示
	// -------------------------------------------------------------------
	private function view_num_commit()
	{
		// 集計表と追加ヘッダー
		$pdo = $this->connect_db();
		$sql = 'SELECT * FROM '.TABLE_NAME_STATISTICS.' LEFT JOIN '.TABLE_NAME_ACCOUNT
				.' ON '.TABLE_NAME_STATISTICS.'.'.FIELD_NAME_STATISTICS_UID.' = '.TABLE_NAME_ACCOUNT.'.'.FIELD_NAME_ACCOUNT_UID
				.' ORDER BY '.FIELD_NAME_STATISTICS_MONTH.' DESC, '.FIELD_NAME_ACCOUNT_UID.';';
		$stmt = $pdo->query($sql);
		$contents = '';
		$header = '<script src="jquery-3.6.0.min.js"></script>'
				.'<script src="jquery.tablesorter.min.js"></script>'
				.'<script>'
				.'$(document).ready(function() {';
		$prev_month = 0;
		$num_months = 0;
		while (($row = $stmt->fetch(PDO::FETCH_ASSOC)) !== FALSE ) {
			if ( $row[FIELD_NAME_STATISTICS_MONTH] != $prev_month ) {
				if ( $prev_month != 0 ) {
					$this->view_num_commit_end_month($contents);
					$num_months++;
					if ( $num_months >= 3 ) {
						$prev_month = 0;
						break;
					}
				}
				$this->view_num_commit_begin_month($header, $contents, $row[FIELD_NAME_STATISTICS_MONTH]);
			}
			$contents .= '<tr><td>'.$row[FIELD_NAME_ACCOUNT_UID].'</td>'
					.'<td>'.$row[FIELD_NAME_ACCOUNT_NAME].'</td>'
					.'<td>'.number_format($row[FIELD_NAME_STATISTICS_ACCEPTS]).'</td>'
					.'<td>'.number_format($row[FIELD_NAME_STATISTICS_REJECTS]).'</td>'
					.'<td>'.number_format($row[FIELD_NAME_STATISTICS_BYTES]).'</td></tr>';
			
			$prev_month = $row[FIELD_NAME_STATISTICS_MONTH];
		}
		$stmt->closeCursor();
		if ( $prev_month != 0 ) {
			$this->view_num_commit_end_month($contents);
		}
		$header .= '} );'
				.'</script>';
	
		// テンプレート適用
		$vars = array();
		$vars[TMPL_MARK_ADD_HEADER] = $header;
		$vars[TMPL_MARK_NUM_COMMIT] = $contents;
	
		// フォーム表示
		$this->show_form('コミット数表示フォーム', FILE_NAME_TEMPLATE_VIEW_NUM_COMMIT, $vars);
	}

	// -------------------------------------------------------------------
	// コミット数表示：一月の区切りの始まり
	// -------------------------------------------------------------------
	private function view_num_commit_begin_month(&$header, &$contents, $month)
	{
		$month_date_time = modified_julian_date_to_date_time($month);
		$header .= '$("#'.$month.'").tablesorter();';
		$contents .= '<h3>'.$month_date_time->format('Y 年 m 月').'</h3>'
				.'<table id="'.$month.'">'
				.'<thead>'
				.'<tr><th>番号</th><th>アカウント名</th><th>コミット数</th><th>競合数</th><th>通信量</th></tr>'
				.'</thead>'
				.'<tbody>';
	}
	
	// -------------------------------------------------------------------
	// コミット数表示：一月の区切りの終わり
	// -------------------------------------------------------------------
	private function view_num_commit_end_month(&$contents)
	{
		$contents .= '</tbody></table>';
	}

	// -------------------------------------------------------------------
	// データ数表示
	// -------------------------------------------------------------------
	private function view_num_data()
	{
		// テンプレート適用
		$vars = array();

		// データ数
		$num_data = '<table>'
				.'<tr><th>テーブル</th><th>有効データ数</th><th>全データ数</th></tr>';
		$num_total_data_sum = 0;
		$num_valid_data_sum = 0;
		$pdo = $this->connect_db();
		$sync_tables = $this->sync_tables();
		for ( $i = 0 ; $i < count($sync_tables) ; $i++ ) {
			$table = $sync_tables[$i];
			$field_prefix = $this->field_name_prefix($table);
			$field_update_time = $field_prefix.FIELD_NAME_SUFFIX_UPDATE_TIME;
			
			// 全データ数
			$num_total_data = 0;
			$sql = 'SELECT COUNT('.$field_update_time.') AS count FROM '.$table;
			$stmt = $pdo->query($sql);
			$row = $stmt->fetch(PDO::FETCH_ASSOC);
			if ( $row !== FALSE && $row['count'] != NULL ) {
				$num_total_data = $row['count'];
			}
			$stmt->closeCursor();
			$num_total_data_sum += $num_total_data;
			
			// 有効データ数
			$num_valid_data = 0;
			$field_invalid = $field_prefix.FIELD_NAME_SUFFIX_INVALID;
			$sql .= ' WHERE '.$field_invalid.' = 0';
			$stmt = $pdo->query($sql);
			$row = $stmt->fetch(PDO::FETCH_ASSOC);
			if ( $row !== FALSE && $row['count'] != NULL ) {
				$num_valid_data = $row['count'];
			}
			$stmt->closeCursor();
			$num_valid_data_sum += $num_valid_data;
			
			$num_data .= '<tr><th>'.$table.'</th><td>'.number_format($num_valid_data).'</td>'
					.'<td>'.number_format($num_total_data).'</td></tr>';
		}
		$num_data .= '<th>合計</th><td>'.number_format($num_valid_data_sum).'</td>'
				.'<td>'.number_format($num_total_data_sum).'</td></tr>'
				.'</table>';
		$vars[TMPL_MARK_NUM_DATA] = $num_data;
	
		// フォーム表示
		$this->show_form('データ数表示フォーム', FILE_NAME_TEMPLATE_VIEW_NUM_DATA, $vars);
	}

	// -------------------------------------------------------------------
	// 状態表示
	// -------------------------------------------------------------------
	private function view_status()
	{
		// テンプレート適用
		$vars = array();
		$vars[TMPL_MARK_INTRO] = 'ようこそ '.$_SESSION[SESSION_INFO_ACCOUNT_NAME].' さん。';
		if( $_SESSION[SESSION_INFO_ACCOUNT_ADMIN] ) {
			$vars[TMPL_MARK_INTRO] .= '　【管理者アカウントでログイン中】';
		}
		$login_time = modified_julian_date_to_date_time($_SESSION[SESSION_INFO_ACCOUNT_LOGIN_TIME]);
		$login_time->setTimeZone(new DateTimeZone(date_default_timezone_get()));
		$vars[TMPL_MARK_INTRO] .= '<br><br>前回ログインまたは同期日時：'
				.$login_time->format('Y/m/d H:i:s');
		$vars[TMPL_MARK_PROGRAM_INFO] = SYSTEM_NAME.'　　'.SYSTEM_VER.'<br>'.SYSTEM_COPYRIGHT;
		$vars[TMPL_MARK_PHP_INFO] = phpversion();
		$vars[TMPL_MARK_TIME_ZONE] = date_default_timezone_get();

		// フォーム表示
		$this->show_form('状態表示フォーム', FILE_NAME_TEMPLATE_VIEW_STATUS, $vars);
	}

	// ===================================================================
	// private プロパティ
	// ===================================================================

	// エラーメッセージ
	private $_err_message;

	// 動作モード
	private $_mode;

	// 報告メッセージ
	private $_notice_message;



}

?>