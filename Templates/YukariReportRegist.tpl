<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>リストの問題を報告した結果</title>
<link rel="stylesheet" href="List.css">
<meta name="generator" content="<!-- $Generator$ -->">
<meta name="viewport" content="width=device-width,initial-scale=1.0">
<!-- $AdditionalHeader$ -->
</head>
<body>
<!-- $AdditionalNavi$ -->

<div class="indices">
<label>リストの問題を報告した結果</label>
</div>

<?php

require_once('Report_Common.php');

try
{
	$report_db = new ReportDbManager();
	$record = prepare_record($report_db);
	$report_db->check_duplication($record);
	$report_db->insert_record($record);
	output_result($record);
}
catch (Exception $excep)
{
	echo '【エラー】<br>'.$excep->getMessage().PHP_EOL;
}

// -------------------------------------------------------------------
// 登録結果を出力
// -------------------------------------------------------------------
function	output_result($record)
{
	echo <<< EOD
<p>
リストの問題が正常に報告されました。報告ありがとうございました。<br>
</p>

<div class="programs">
<label>問題の内容</label><br>
<br>

<table>
 <tr><th>問題のある内容</th><td>{$record[FIELD_NAME_REPORT_BAD_VALUE]}</td></tr>
 <tr><th>正しい内容</th><td>{$record[FIELD_NAME_REPORT_ADJUST_VALUE]}</td></tr>
 <tr><th>その他コメント等</th><td>{$record[FIELD_NAME_REPORT_REPORTER_COMMENT]}</td></tr>
 <tr><th>報告者名</th><td>{$record[FIELD_NAME_REPORT_BY]}</td></tr>
</table>
EOD;
}

// -------------------------------------------------------------------
// 登録用データを作成
// -------------------------------------------------------------------
function	prepare_record($report_db)
{
	// 1 日の上限チェック
	$report_db->check_day_limit();
	
	// 報告対象レコード存在チェック
	$uid = get_posted_parameter(PARAM_NAME_FOUND_UID);
	$list_db = new ListDbManager();
	$found = $list_db->check_uid_exists($uid);
	
	// データ作成
	$record = array();
	$record[FIELD_NAME_REPORT_IMPORT] = FALSE;
	$record[FIELD_NAME_REPORT_INVALID] = FALSE;
	$record[FIELD_NAME_REPORT_UPDATE_TIME] = invalid_mjd();
	$record[FIELD_NAME_REPORT_DIRTY] = TRUE;
	$record[FIELD_NAME_REPORT_PATH] = $found[FIELD_NAME_FOUND_PATH];
	$record[FIELD_NAME_REPORT_ADJUST_KEY] = get_posted_parameter(PARAM_NAME_REPORT_TARGET);
	switch ( $record[FIELD_NAME_REPORT_ADJUST_KEY] ) {
	case REPORT_TARGET_MISC:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = '';
		break;
	case REPORT_TARGET_CATEGORY_NAME:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_FOUND_CATEGORY_NAME];
		break;
	case REPORT_TARGET_TIE_UP_NAME:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_FOUND_TIE_UP_NAME];
		break;
	case REPORT_TARGET_OP_ED:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_SONG_OP_ED];
		break;
	case REPORT_TARGET_SONG_NAME:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_SONG_NAME];
		break;
	case REPORT_TARGET_ARTIST_NAME:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_FOUND_ARTIST_NAME];
		break;
	case REPORT_TARGET_TRACK:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = track_string($found);
		break;
	case REPORT_TARGET_WORKER:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_FOUND_WORKER];
		break;
	case REPORT_TARGET_TIE_UP_GROUP_NAME:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_TIE_UP_GROUP_NAME];
		break;
	case REPORT_TARGET_AGE_LIMIT:
		$record[FIELD_NAME_REPORT_BAD_VALUE] = $found[FIELD_NAME_TIE_UP_AGE_LIMIT];
		break;
	default:
		throw new Exception('問題のある項目の選択が不正です。');
	}
	$record[FIELD_NAME_REPORT_ADJUST_VALUE] = get_posted_parameter(PARAM_NAME_REPORT_ADJUST);
	if ( $record[FIELD_NAME_REPORT_ADJUST_VALUE] == '' ) {
		throw new Exception('「正しい内容」欄を入力して下さい。');
	}
	if ( mb_strlen($record[FIELD_NAME_REPORT_ADJUST_VALUE]) === FALSE 
			|| mb_strlen($record[FIELD_NAME_REPORT_ADJUST_VALUE]) > MAX_DATA_LENGTH ) {
		throw new Exception('「正しい内容」欄の内容が長すぎます。');
	}
	$record[FIELD_NAME_REPORT_REPORTER_COMMENT] = get_posted_parameter(PARAM_NAME_REPORT_COMMENT);
	if ( mb_strlen($record[FIELD_NAME_REPORT_REPORTER_COMMENT]) === FALSE 
			|| mb_strlen($record[FIELD_NAME_REPORT_REPORTER_COMMENT]) > MAX_DATA_LENGTH ) {
		throw new Exception('「コメント」欄の内容が長すぎます。');
	}
	$record[FIELD_NAME_REPORT_BY] = get_posted_parameter(PARAM_NAME_REPORT_REPORTER);
	if ( $record[FIELD_NAME_REPORT_BY] == '' ) {
		throw new Exception('「報告者名」欄を入力して下さい。');
	}
	if ( mb_strlen($record[FIELD_NAME_REPORT_BY]) === FALSE 
			|| mb_strlen($record[FIELD_NAME_REPORT_BY]) > MAX_DATA_LENGTH ) {
		throw new Exception('「報告者名」欄の内容が長すぎます。');
	}
	$record[FIELD_NAME_REPORT_IP] = $_SERVER["REMOTE_ADDR"];
	$record[FIELD_NAME_REPORT_HOST] = gethostbyaddr($_SERVER["REMOTE_ADDR"]);
	$record[FIELD_NAME_REPORT_STATUS_COMMENT] = '';
	$record[FIELD_NAME_REPORT_STATUS] = 0;
	$record[FIELD_NAME_REPORT_STATUS_BY] = '';

	// 少しでも競合を避けるため、Id は最後に生成する
	$report_db->set_id_and_time($record);
	
	return $record;
}

// -------------------------------------------------------------------
// 報告内容入力フォームを出力
// -------------------------------------------------------------------
function	output_form()
{
	$uid = get_url_parameter(PARAM_NAME_FOUND_UID);
	if ( $uid == '' ) {
		throw new Exception('報告対象が指定されていません。');
	}
	$list_db = new ListDbManager();
	$record = $list_db->get_record_by_uid($uid);
	if ( $record === FALSE ) {
		throw new Exception('報告対象が存在しません。');
	}

	echo <<< EOD
<p>
リストに問題を発見したので報告します。<br>
</p>

<div class="programs">
<label>対象ファイル</label><br>
<br>
EOD;
	output_form_target_file($record);
	echo <<< EOD
<br>
</div>
EOD;
	output_form_entry($uid);

}

// -------------------------------------------------------------------
// 問題の内容を入力する部分を出力
// -------------------------------------------------------------------
function	output_form_entry($uid)
{
	echo <<< EOD
<div class="programs">
<label>問題の内容</label><br>
<p>
リストの問題の内容を以下に具体的に入力してください。<br>
※印は入力必須項目です。<br>
</p>
<form action="Report_Regist.php" method="post">
<table>
  <tr>
   <th>問題のある項目</th>
   <td class="small">※</td>
   <td>
    <select id="target" required>
     <option></option>
     <option value="2">カテゴリー</option>
     <option value="3">タイアップ名</option>
     <option value="4">摘要</option>
     <option value="5">楽曲名</option>
     <option value="6">歌手名</option>
     <option value="7">トラック</option>
     <option value="8">制作</option>
     <option value="9">シリーズ</option>
     <option value="10">年齢制限</option>
     <option value="1">その他</option>
    </select>
   </td>
  </tr>
  <tr>
   <th>正しい内容</th>
   <td class="small">※</td>
   <td><input class="report" type="text" id="correct" size="30" required></td>
  </tr>
  <tr>
   <th>その他コメント等</th>
   <td></td>
   <td><input class="report" type="text" id="comment" size="30"></td>
  </tr>
  <tr>
   <th>報告者名</th>
   <td class="small">※</td>
   <td><input class="report" type="text" id="reporter" size="30" required></td>
  </tr>
</table>
<br>
<input class="report" type="submit" value="問題を報告する">
<input type="hidden" id="uid" value="{$uid}">
</form>
</div>
EOD;
}

// -------------------------------------------------------------------
// 対象ファイルの情報をテーブルで出力
// -------------------------------------------------------------------
function	output_form_target_file(&$record)
{
	echo '<table>'.PHP_EOL;
	echo '<tr class="even"><th>ファイル名</th><td>'
			.pathinfo($record[FIELD_NAME_FOUND_PATH], PATHINFO_BASENAME).'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>カテゴリー</th><td>'
			.$record[FIELD_NAME_FOUND_CATEGORY_NAME].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>タイアップ名</th><td>'
			.$record[FIELD_NAME_FOUND_TIE_UP_NAME].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>摘要</th><td>'
			.$record[FIELD_NAME_SONG_OP_ED].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>楽曲名</th><td>'
			.$record[FIELD_NAME_SONG_NAME].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>歌手名</th><td>'
			.$record[FIELD_NAME_FOUND_ARTIST_NAME].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>トラック</th><td>On：'
			.($record[FIELD_NAME_FOUND_SMART_TRACK_ON] ? '○' : '×').' / Off：'
			.($record[FIELD_NAME_FOUND_SMART_TRACK_OFF] ? '○' : '×').'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>制作</th><td>'
			.$record[FIELD_NAME_FOUND_WORKER].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>シリーズ</th><td>'
			.$record[FIELD_NAME_TIE_UP_GROUP_NAME].'</td></tr>'.PHP_EOL;
	echo '<tr class="even"><th>年齢制限</th><td>'
			.($record[FIELD_NAME_TIE_UP_AGE_LIMIT] == 0 ? '全年齢' : $record[FIELD_NAME_TIE_UP_AGE_LIMIT].' 歳以上')
			.'</td></tr>'.PHP_EOL;
	echo '</table>'.PHP_EOL;
}

?>

</body>
</html>
