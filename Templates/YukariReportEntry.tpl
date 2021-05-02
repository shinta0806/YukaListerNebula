<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>リストの問題を報告</title>
<link rel="stylesheet" href="List.css">
<meta name="generator" content="<!-- $Generator$ -->">
<meta name="viewport" content="width=device-width,initial-scale=1.0">
<!-- $AdditionalHeader$ -->
</head>
<body>
<!-- $AdditionalNavi$ -->

<div class="indices">
<label>リストの問題を報告</label>
</div>

<?php

require_once('Report_Common.php');

try
{
	output_form();
}
catch (Exception $excep)
{
	echo '【エラー】<br>'.$excep->getMessage().PHP_EOL;
}

// -------------------------------------------------------------------
// 報告内容入力フォームを出力
// -------------------------------------------------------------------
function	output_form()
{
	$uid = get_url_parameter(PARAM_NAME_FOUND_UID);
	$list_db = new ListDbManager();
	$record = $list_db->check_uid_exists($uid);

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
	global $const_to_var;
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
    <select id="target" name="target" required>
     <option></option>
     <option value="{$const_to_var(REPORT_TARGET_CATEGORY_NAME)}">カテゴリー名</option>
     <option value="{$const_to_var(REPORT_TARGET_TIE_UP_NAME)}">タイアップ名</option>
     <option value="{$const_to_var(REPORT_TARGET_OP_ED)}">摘要</option>
     <option value="{$const_to_var(REPORT_TARGET_SONG_NAME)}">楽曲名</option>
     <option value="{$const_to_var(REPORT_TARGET_ARTIST_NAME)}">歌手名</option>
     <option value="{$const_to_var(REPORT_TARGET_TRACK)}">トラック</option>
     <option value="{$const_to_var(REPORT_TARGET_WORKER)}">制作</option>
     <option value="{$const_to_var(REPORT_TARGET_TIE_UP_GROUP_NAME)}">シリーズ</option>
     <option value="{$const_to_var(REPORT_TARGET_AGE_LIMIT)}">年齢制限</option>
     <option value="{$const_to_var(REPORT_TARGET_MISC)}">その他</option>
    </select>
   </td>
  </tr>
  <tr>
   <th>正しい内容</th>
   <td class="small">※</td>
   <td><input class="report" type="text" id="adjust" name="adjust" size="30" required maxlength="{$const_to_var(MAX_DATA_LENGTH)}"></td>
  </tr>
  <tr>
   <th>その他コメント等</th>
   <td></td>
   <td><input class="report" type="text" id="comment" name="comment" size="30" maxlength="{$const_to_var(MAX_DATA_LENGTH)}"></td>
  </tr>
  <tr>
   <th>報告者名</th>
   <td class="small">※</td>
   <td><input class="report" type="text" id="reporter" name="reporter" size="30" required maxlength="{$const_to_var(MAX_DATA_LENGTH)}"></td>
  </tr>
</table>
<br>
<input class="report" type="submit" value="問題を報告する">
<input type="hidden" id="uid" name="uid" value="{$uid}">
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
	echo '<tr class="even"><th>トラック</th><td>'
			.track_string($record).'</td></tr>'.PHP_EOL;
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
