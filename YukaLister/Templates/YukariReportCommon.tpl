<?php

// ============================================================================
// リスト問題報告フォーム
// 共通ファイル
// Copyright (C) 2019 by SHINTA
// ============================================================================

// ===================================================================
// require
// ===================================================================

require_once('Report_JulianDay.php');

// ===================================================================
// 定数定義
// ===================================================================

define('DAY_LIMIT', 100);
define('FIELD_NAME_FOUND_UID', 'found_uid');
define('FIELD_NAME_FOUND_PATH', 'found_path');
define('FIELD_NAME_FOUND_WORKER', 'found_worker');
define('FIELD_NAME_FOUND_SMART_TRACK_ON', 'found_smart_track_on');
define('FIELD_NAME_FOUND_SMART_TRACK_OFF', 'found_smart_track_off');
define('FIELD_NAME_FOUND_ARTIST_NAME', 'song_artist');
define('FIELD_NAME_FOUND_TIE_UP_NAME', 'program_name');
define('FIELD_NAME_FOUND_CATEGORY_NAME', 'program_category');
define('FIELD_NAME_REPORT_ID', 'report_id');
define('FIELD_NAME_REPORT_IMPORT', 'report_import');
define('FIELD_NAME_REPORT_INVALID', 'report_invalid');
define('FIELD_NAME_REPORT_UPDATE_TIME', 'report_update_time');
define('FIELD_NAME_REPORT_DIRTY', 'report_dirty');
define('FIELD_NAME_REPORT_PATH', 'report_path');
define('FIELD_NAME_REPORT_ADJUST_KEY', 'report_adjust_key');
define('FIELD_NAME_REPORT_BAD_VALUE', 'report_bad_value');
define('FIELD_NAME_REPORT_ADJUST_VALUE', 'report_adjust_value');
define('FIELD_NAME_REPORT_REPORTER_COMMENT', 'report_reporter_comment');
define('FIELD_NAME_REPORT_BY', 'report_by');
define('FIELD_NAME_REPORT_IP', 'report_ip');
define('FIELD_NAME_REPORT_HOST', 'report_host');
define('FIELD_NAME_REPORT_REGIST_TIME', 'report_regist_time');
define('FIELD_NAME_REPORT_STATUS_COMMENT', 'report_status_comment');
define('FIELD_NAME_REPORT_STATUS', 'report_status');
define('FIELD_NAME_REPORT_STATUS_BY', 'report_status_by');
define('FIELD_NAME_SONG_NAME', 'song_name');
define('FIELD_NAME_SONG_OP_ED', 'song_op_ed');
define('FIELD_NAME_TIE_UP_AGE_LIMIT', 'tie_up_age_limit');
define('FIELD_NAME_TIE_UP_GROUP_NAME', 'tie_up_group_name');
define('FILE_NAME_REPORT_DB', 'Report.sqlite3');
define('FILE_NAME_YUKARI_LIST_DB', 'List.sqlite3');
define('INVALID_YEAR', 1900);
define('MAX_DATA_LENGTH', 128);
define('PARAM_NAME_FOUND_UID', 'uid');
define('PARAM_NAME_REPORT_TARGET', 'target');
define('PARAM_NAME_REPORT_ADJUST', 'adjust');
define('PARAM_NAME_REPORT_COMMENT', 'comment');
define('PARAM_NAME_REPORT_REPORTER', 'reporter');
define('REPORT_TARGET_MISC', 1);
define('REPORT_TARGET_CATEGORY_NAME', 2);
define('REPORT_TARGET_TIE_UP_NAME', 3);
define('REPORT_TARGET_OP_ED', 4);
define('REPORT_TARGET_SONG_NAME', 5);
define('REPORT_TARGET_ARTIST_NAME', 6);
define('REPORT_TARGET_TRACK', 7);
define('REPORT_TARGET_WORKER', 8);
define('REPORT_TARGET_TIE_UP_GROUP_NAME', 9);
define('REPORT_TARGET_AGE_LIMIT', 10);
define('TABLE_NAME_FOUND', 't_found');
define('TABLE_NAME_REPORT', 't_report');

// ===================================================================
// 関数定義
// ===================================================================

// -------------------------------------------------------------------
// ヒアドキュメントに定数を書くためのラムダ関数
// 使用する場所で global $const_to_var; の宣言が必要
// -------------------------------------------------------------------
$const_to_var = function($constant)
{
	return $constant;
};

// -------------------------------------------------------------------
// 改行文字を全て "\n" に統一する
// ToDo: CPCommon.php と統合
// ＜引数＞ $src: 変換前の文字列
// ＜返値＞ 変換後の文字列
// -------------------------------------------------------------------
function	convert_cr_lf($src)
{
	$src = str_replace("\r\n", "\n", $src);
	$src = str_replace("\r", "\n", $src);
	return $src;
}

// -------------------------------------------------------------------
// HTML 的に危険な文字をエスケープする＆改行を \n に統一
// ToDo: CPCommon.php と統合
// ＜引数＞ $src: フォームなどで入力されたパラメーター
// ＜返値＞ 安全に変換されたパラメーター
// -------------------------------------------------------------------
function	escape_input($src)
{
	return htmlspecialchars(convert_cr_lf($src), ENT_QUOTES);
}

// -------------------------------------------------------------------
// POST されたパラメータを安全に取得
// ToDo: CPManager.php と統合
// ＜引数＞ $name: パラメーター名
// ＜返値＞ 危険を無力化した後の値文字列（パラメーターが設定されていない場合は空文字列）
// -------------------------------------------------------------------
function	get_posted_parameter($name)
{
	if ( isset($_POST[$name]) ) {
		return escape_input($_POST[$name]);
	} else {
		return '';
	}
}

// -------------------------------------------------------------------
// URL で指定されたパラメータを安全に取得
// ToDo: CPCommon.php と統合
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

// -------------------------------------------------------------------
// 日付が指定されていない場合の修正ユリウス日
// -------------------------------------------------------------------
function	invalid_mjd()
{
	return date_time_to_modified_julian_date(new DateTime(INVALID_YEAR.'-01-01', new DateTimeZone('UTC')));
}

// -------------------------------------------------------------------
// 現在時刻 UTC（修正ユリウス日）
// ToDo: CPManager.php と統合
// -------------------------------------------------------------------
function	now_mjd()
{
	return date_time_to_modified_julian_date(new DateTime(null, new DateTimeZone('UTC')));
}

// -------------------------------------------------------------------
// TFound のスマートトラック情報を文字列に変換
// -------------------------------------------------------------------
function	track_string($record)
{
	return 'On：'.($record[FIELD_NAME_FOUND_SMART_TRACK_ON] ? '○' : '×')
			.' / Off：'.($record[FIELD_NAME_FOUND_SMART_TRACK_OFF] ? '○' : '×');
}

// ===================================================================
// SQLite 基底クラス
// ===================================================================

class	DbManager
{
	// ===================================================================
	// public メソッド
	// ===================================================================

	// -------------------------------------------------------------------
	// コンストラクタ
	// -------------------------------------------------------------------
	public function __construct($file_name)
	{
		$this->_pdo = new PDO('sqlite:'.$file_name);
		$this->_pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
		$this->_pdo->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);
	}

	// ===================================================================
	// protected プロパティー
	// ===================================================================

	// PDO
	protected $_pdo;
}

// ===================================================================
// List.sqlite3 操作クラス
// ===================================================================

class	ListDbManager extends DbManager
{
	// ===================================================================
	// public メソッド
	// ===================================================================

	// -------------------------------------------------------------------
	// コンストラクタ
	// -------------------------------------------------------------------
	public function __construct()
	{
		parent::__construct(FILE_NAME_YUKARI_LIST_DB);
	}
	
	// -------------------------------------------------------------------
	// 指定された uid を持つレコードがあるかチェックする
	// -------------------------------------------------------------------
	public function	check_uid_exists($uid)
	{
		$record = $this->get_record_by_uid($uid);
		if ( $record === FALSE ) {
			throw new Exception('報告対象が存在しません。');
		}
		return $record;
	}
	
	// -------------------------------------------------------------------
	// Uid が一致する 1 レコードを取得
	// ＜返値＞ カラム名で添字を付けた配列 or FALSE
	// -------------------------------------------------------------------
	public function get_record_by_uid($uid)
	{
		if ( $uid == '' ) {
			throw new Exception('報告対象が指定されていません。');
		}

		$sql = 'SELECT * FROM '.TABLE_NAME_FOUND
				.' WHERE '.FIELD_NAME_FOUND_UID.' = :uid';
		$stmt = $this->_pdo->prepare($sql);
		$stmt->bindValue(':uid', $uid, PDO::PARAM_INT);
		$stmt->execute();
		$row = $stmt->fetch();
		$stmt->closeCursor();
		return $row;
	}
}

// ===================================================================
// Report.sqlite3 操作クラス
// ===================================================================

class	ReportDbManager extends DbManager
{
	// ===================================================================
	// public メソッド
	// ===================================================================

	// -------------------------------------------------------------------
	// コンストラクタ
	// -------------------------------------------------------------------
	public function __construct()
	{
		parent::__construct(FILE_NAME_REPORT_DB);
	}

	// -------------------------------------------------------------------
	// 1 日当たりの登録上限に既に達しているかチェック
	// -------------------------------------------------------------------
	public function check_day_limit()
	{
		$today = new DateTime(null, new DateTimeZone('UTC'));
		$today->setTime(0, 0);
		$today_mjd = date_time_to_modified_julian_date($today);

		$sql = 'SELECT COUNT(*) FROM '.TABLE_NAME_REPORT
				.' WHERE '.FIELD_NAME_REPORT_REGIST_TIME.' >= :rtime';
		$stmt = $this->_pdo->prepare($sql);
		$stmt->bindValue(':rtime', $today_mjd, PDO::PARAM_STR);
		$stmt->execute();
		$row = $stmt->fetch();
		$stmt->closeCursor();
		
		$today_num = $row['COUNT(*)'];
		if ( $today_num >= DAY_LIMIT ) {
			throw new Exception('本日の報告数上限（'.DAY_LIMIT.'）に達したため登録できません。明日以降の登録をお願いします。');
		}
	}
	
	// -------------------------------------------------------------------
	// 同内容の報告が既に存在しているかチェック
	// -------------------------------------------------------------------
	public function check_duplication($record)
	{
		$sql = 'SELECT * FROM '.TABLE_NAME_REPORT
				.' WHERE '.FIELD_NAME_REPORT_INVALID.' = 0'
				.' AND '.FIELD_NAME_REPORT_PATH.' = :path'
				.' AND '.FIELD_NAME_REPORT_ADJUST_KEY.' = :adjust_key'
				.' AND '.FIELD_NAME_REPORT_BAD_VALUE.' = :bad_value'
				.' AND '.FIELD_NAME_REPORT_ADJUST_VALUE.' = :adjust_value'
				.' AND '.FIELD_NAME_REPORT_REPORTER_COMMENT.' = :comment';
		$stmt = $this->_pdo->prepare($sql);
		$stmt->bindValue(':path', $record[FIELD_NAME_REPORT_PATH], PDO::PARAM_STR);
		$stmt->bindValue(':adjust_key', $record[FIELD_NAME_REPORT_ADJUST_KEY], PDO::PARAM_INT);
		$stmt->bindValue(':bad_value', $record[FIELD_NAME_REPORT_BAD_VALUE], PDO::PARAM_STR);
		$stmt->bindValue(':adjust_value', $record[FIELD_NAME_REPORT_ADJUST_VALUE], PDO::PARAM_STR);
		$stmt->bindValue(':comment', $record[FIELD_NAME_REPORT_REPORTER_COMMENT], PDO::PARAM_STR);
		$stmt->execute();
		$row = $stmt->fetch();
		$stmt->closeCursor();
	
		if ( $row !== FALSE ) {
			throw new Exception('この問題は既に報告されています。');
		}
	}
	
	// -------------------------------------------------------------------
	// 次に登録するレコード用の Id
	// obsolete
	// -------------------------------------------------------------------
	public function get_next_id()
	{
		// 既存の最大 Id を検索
		$i = 0;
		$max = 0;
		$sql = 'SELECT * FROM '.TABLE_NAME_REPORT
				.' ORDER BY '.FIELD_NAME_REPORT_REGIST_TIME.' DESC';
		$stmt = $this->_pdo->prepare($sql);
		$stmt->execute();
		while ( ($row = $stmt->fetch()) !== FALSE ) {
			$matches = array();
			preg_match('/.*_([0-9]*)/', $a, $matches);
			$num = intval($matches[1]);
			if ( $num > $max ) {
				$max = $num;
			}
			
			// Id は報告日時順に並んでいるはずであるが、念のため複数レコードチェックする
			$i++;
			if ( $i >= 5 ) {
				break;
			}
		}
		$stmt->closeCursor();
		
		// Id プレフィックスの設定
		$prefix = '<!-- $IdPrefix$ -->';
		$prefix .= '_R_';
		
		// Id 生成
		return $prefix.($max+1);
	}

	// -------------------------------------------------------------------
	// レコードを挿入
	// -------------------------------------------------------------------
	public function insert_record($record)
	{
		$sql = 'INSERT INTO '.TABLE_NAME_REPORT
				.'('.FIELD_NAME_REPORT_ID.','
				.FIELD_NAME_REPORT_IMPORT.','
				.FIELD_NAME_REPORT_INVALID.','
				.FIELD_NAME_REPORT_UPDATE_TIME.','
				.FIELD_NAME_REPORT_DIRTY.','
				.FIELD_NAME_REPORT_PATH.','
				.FIELD_NAME_REPORT_ADJUST_KEY.','
				.FIELD_NAME_REPORT_BAD_VALUE.','
				.FIELD_NAME_REPORT_ADJUST_VALUE.','
				.FIELD_NAME_REPORT_REPORTER_COMMENT.','
				.FIELD_NAME_REPORT_BY.','
				.FIELD_NAME_REPORT_IP.','
				.FIELD_NAME_REPORT_HOST.','
				.FIELD_NAME_REPORT_REGIST_TIME.','
				.FIELD_NAME_REPORT_STATUS_COMMENT.','
				.FIELD_NAME_REPORT_STATUS.','
				.FIELD_NAME_REPORT_STATUS_BY.') VALUES ('
				.':id, :import, :invalid, :update_time, :dirty, :path, '
				.':adjust_key, :bad_value, :adjust_value, :reporter_comment, :by, '
				.':ip, :host, :regist_time, :status_comment, :status, :status_by)';
		$stmt = $this->_pdo->prepare($sql);
		$stmt->bindValue(':id', $record[FIELD_NAME_REPORT_ID], PDO::PARAM_STR);
		$stmt->bindValue(':import', $record[FIELD_NAME_REPORT_IMPORT], PDO::PARAM_BOOL);
		$stmt->bindValue(':invalid', $record[FIELD_NAME_REPORT_INVALID], PDO::PARAM_BOOL);
		$stmt->bindValue(':update_time', $record[FIELD_NAME_REPORT_UPDATE_TIME], PDO::PARAM_STR);
		$stmt->bindValue(':dirty', $record[FIELD_NAME_REPORT_DIRTY], PDO::PARAM_BOOL);
		$stmt->bindValue(':path', $record[FIELD_NAME_REPORT_PATH], PDO::PARAM_STR);
		$stmt->bindValue(':adjust_key', $record[FIELD_NAME_REPORT_ADJUST_KEY], PDO::PARAM_INT);
		$stmt->bindValue(':bad_value', $record[FIELD_NAME_REPORT_BAD_VALUE], PDO::PARAM_STR);
		$stmt->bindValue(':adjust_value', $record[FIELD_NAME_REPORT_ADJUST_VALUE], PDO::PARAM_STR);
		$stmt->bindValue(':reporter_comment', $record[FIELD_NAME_REPORT_REPORTER_COMMENT], PDO::PARAM_STR);
		$stmt->bindValue(':by', $record[FIELD_NAME_REPORT_BY], PDO::PARAM_STR);
		$stmt->bindValue(':ip', $record[FIELD_NAME_REPORT_IP], PDO::PARAM_STR);
		$stmt->bindValue(':host', $record[FIELD_NAME_REPORT_HOST], PDO::PARAM_STR);
		$stmt->bindValue(':regist_time', $record[FIELD_NAME_REPORT_REGIST_TIME], PDO::PARAM_STR);
		$stmt->bindValue(':status_comment', $record[FIELD_NAME_REPORT_STATUS_COMMENT], PDO::PARAM_STR);
		$stmt->bindValue(':status', $record[FIELD_NAME_REPORT_STATUS], PDO::PARAM_INT);
		$stmt->bindValue(':status_by', $record[FIELD_NAME_REPORT_STATUS_BY], PDO::PARAM_STR);
		$stmt->execute();
	}

	// -------------------------------------------------------------------
	// レコードの ID と報告日時を設定
	// -------------------------------------------------------------------
	public function set_id_and_time(&$record)
	{
		// 報告日時
		$record[FIELD_NAME_REPORT_REGIST_TIME] = now_mjd();
		
		// 既存レコードの報告日時が現在未満であることを確認
		$sql = 'SELECT * FROM '.TABLE_NAME_REPORT
				.' ORDER BY '.FIELD_NAME_REPORT_REGIST_TIME.' DESC';
		$stmt = $this->_pdo->prepare($sql);
		$stmt->execute();
		$row = $stmt->fetch();
		$stmt->closeCursor();
		if ( $row !== FALSE && $row[FIELD_NAME_REPORT_REGIST_TIME] >= $record[FIELD_NAME_REPORT_REGIST_TIME] ) {
			throw new Exception('未来日付で登録されている報告があるため登録できません。');
		}
		
		// 既存の最大 Id を検索
		$max = 0;
		if ( $row !== FALSE ) {
			$matches = array();
			preg_match('/.*_([0-9]*)/', $row[FIELD_NAME_REPORT_ID], $matches);
			$max = intval($matches[1]);
		}
		
		// Id プレフィックスの設定
		$prefix = '<!-- $IdPrefix$ -->';
		$prefix .= '_R_';
		
		// Id 生成
		$record[FIELD_NAME_REPORT_ID] = $prefix.($max+1);
	}


}

?>