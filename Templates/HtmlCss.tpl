@charset "utf-8";

/* ============================================================================
** ゆかりすたー HTML 出力のスタイルシート
============================================================================ */

/* ----------------------------------------------------------------------------
-- 共通
---------------------------------------------------------------------------- */

input.accparent
{
	display: none;
}

input.report
{
	display: inline;
}

.additionalnavi
{
	background-color: #222222;
	margin-bottom: 10px;
	padding: 10px;
}

.additionalnavilink
{
	border-radius: 5px;
	color: #ffffff;
	padding: 5px 10px 5px 10px;
	text-decoration: none;
}

.additionalnavilink:hover
{
	background-color: #555555;
}

.accchild
{
	height: 0;
	padding: 0;
	overflow-x: visible;
	overflow-y: hidden;
	opacity: 0;
	transition: 0.8s;
}

.accparent:checked + label + .accchild
{
	height: auto;
	padding: 5px 5px 5px 15px;
	opacity: 1;
}

.centering
{
	margin: auto;
	text-align: center;
}

table
{
	border-collapse: collapse;
	border-spacing: 0;
}

p.generator
{
	text-align: right;
	color: #cccccc;
	font-size: 90%;
}

/* ----------------------------------------------------------------------------
-- トップファイル
---------------------------------------------------------------------------- */

div.indices label
{
	display: block;
	position: relative;
	background-color: #dfefff;
	box-shadow: 0px 0px 0px 5px #dfefff;
	border: dashed 2px white;
	padding: 0.2em 0.8em;
	color: #555555;
	font-size: 130%;
	font-weight: bold;
	margin-bottom: 10px;
}

div.indices .accparent:checked + label
{
	border: dashed 2px #98b0ff;
}

div.indices label:after
{
	display: block;
	position: absolute;
	content: '';
	left: -7px;
	top: -7px;
	border-width: 0 0 15px 15px;
	border-style: solid;
	border-color: #ffffff #ffffff #a8d4ff;
}

a.series
{
	color: #aa4020;
	font-size: 70%;
	text-decoration-line: none;
}

div.indices table
{
	margin-bottom: 10px;
}

div.indices table.invisible
{
	display: none;
}

div.indices td
{
	border: 1px solid #cccccc;
	padding: 0px;
	color: #cccccc;
	min-width: 2em;
	height: 2em;
	text-align: center;
	vertical-align: middle;
}

div.indices td.exist
{
	background-color: #f0ffe0;
}

div.indices a
{
	display: block;
	width: 100%;
	height: 100%;
	border: 2px solid transparent;
	box-sizing: border-box;
}

div.indices a:link
{
	color: #508030;
}

div.indices a:hover, div.indices a:active, div.indices a:visited
{
	color: #805030;
}

div.indices a:hover
{
	border: 2px solid #805030;
}

div.indices a:active
{
	background-color: #fff0e0;
	border: 2px solid #c05030;
}

/* ----------------------------------------------------------------------------
-- 頭文字別ファイル
---------------------------------------------------------------------------- */

div.programs label
{
	display: block;
	width: 100%;
	background: linear-gradient(#ffddaa, #ff8060);
	padding: 0.1em;
	color: #505050;
	display: inline-block;
	vertical-align: middle;
	border-radius: 25px 0px 0px 25px;
	font-size: 120%;
	cursor: pointer;
}

div.programs label:before
{
	content: '●';
	color: white;
	margin-right: 8px;
}

div.programs .accparent:checked + label:before
{
	color: #4f81bd;
}

div.programs th
{
	border: 1px solid #95b3d7;
	background-color: #4f81bd;
	padding: 5px;
	color: white;
}

div.programs td
{
	border: 1px solid #95b3d7;
	padding: 5px;
	line-height: 100%;
}

div.programs td.small
{
	font-size: 75%;
}

div.programs tr.even
{
	background-color: #dce6f1;
}

div.programs tr.odd
{
	background-color: white;
}

