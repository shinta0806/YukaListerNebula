<?php

// ============================================================================
// ゆかりすたー NEBULA 同期
// 修正ユリウス日モジュール
// Copyright (C) 2021 by SHINTA
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

// require

// 定数定義
define('MJD_DELTA', 2400000.5);
define('SECONDS_PER_DAY', 24 * 60 * 60);

// ----------------------------------------------------------------------------
// DateTime（UTC での日付時刻）→（修正じゃない）ユリウス日
// ----------------------------------------------------------------------------
function	date_time_to_julian_day($date_time)
{
	$date_time->setTimeZone(new DateTimeZone('UTC'));
	$year = $date_time->format('Y');
	$month = $date_time->format('n');
	$day = $date_time->format('j');

	return gregoriantojd($month, $day, $year) - 0.5
			+ (double)($date_time->getTimestamp()%SECONDS_PER_DAY) / SECONDS_PER_DAY;
}

// ----------------------------------------------------------------------------
// DateTime（UTC での日付時刻）→修正ユリウス日
// ----------------------------------------------------------------------------
function	date_time_to_modified_julian_date($date_time)
{
	return date_time_to_julian_day($date_time) - MJD_DELTA;
}

// ----------------------------------------------------------------------------
// （修正じゃない）ユリウス日→DateTime（UTC）
// ----------------------------------------------------------------------------
function	julian_day_to_date_time($julian_day)
{
	$date_time = DateTime::createFromFormat('n/j/Y', jdtogregorian($julian_day + 0.5), new DateTimeZone('UTC'));
	$seconds = round(($julian_day + 0.5 - (int)($julian_day + 0.5)) * SECONDS_PER_DAY);
	$date_time->setTime(floor($seconds/(60 * 60)), floor($seconds / 60) % 60, $seconds % 60);
	return $date_time;
}

// ----------------------------------------------------------------------------
// 修正ユリウス日→DateTime（UTC）
// ----------------------------------------------------------------------------
function	modified_julian_date_to_date_time($mjd)
{
	return julian_day_to_date_time($mjd + MJD_DELTA);
}

?>
