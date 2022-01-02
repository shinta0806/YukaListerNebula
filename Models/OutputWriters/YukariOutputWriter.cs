// ============================================================================
// 
// ゆかり用リスト出力クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.OutputWriters
{
	public class YukariOutputWriter : WebOutputWriter
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public YukariOutputWriter()
				: base(Common.FILE_EXT_PHP)
		{
			// プロパティー
			FormatName = "ゆかり用 PHP";
			TopFileName = "index" + Common.FILE_EXT_PHP;
			//OutputSettings = new YukariOutputSettings();

			// メンバー変数
			String listLinkArg = ListLinkArg();

			_additionalDescription = "ファイル名をクリックすると、ゆかりでリクエストできます。<br>";
			_additionalHeader = "<?php\n"
					+ "$yukarisearchlink = '';\n"
					+ "if (array_key_exists('yukarihost', $_REQUEST)) {\n"
					+ "    $yukarihost = $_REQUEST['yukarihost'];\n"
					+ "    $yukarisearchlink = 'http://'.$yukarihost.'/search_listerdb_filelist.php?anyword=';\n"
					+ "}\n"
					+ "?>\n";
			_additionalNavi = "<div class=\"additionalnavi\">"
					+ "<a class=\"additionalnavilink\" href=\"/search.php" + listLinkArg + "\">検索</a> "
					+ "<a class=\"additionalnavilink\" href=\"/requestlist_only.php" + listLinkArg + "\">予約一覧</a> "
					+ "</div>";
			_listLinkArg = listLinkArg;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 曲情報を文字列に追加する際のテーブル内容を追加
		// --------------------------------------------------------------------
		protected override void AppendSongInfoAddTd(StringBuilder stringBuilder, OutputItems chapterItem, TFound found)
		{
			base.AppendSongInfoAddTd(stringBuilder, chapterItem, found);
			stringBuilder.Append("<td class=\"small\"><a href=\"" + FILE_NAME_REPORT_ENTRY + ListLinkArg(YlConstants.SERVER_OPTION_NAME_UID + "=" + found.Uid) + "\">報告</a></td>");
		}

		// --------------------------------------------------------------------
		// 章を開始する際のテーブル見出しを追加
		// --------------------------------------------------------------------
		protected override void BeginChapterAddTh(StringBuilder stringBuilder, OutputItems chapterItem)
		{
			base.BeginChapterAddTh(stringBuilder, chapterItem);
			stringBuilder.Append("<th>報告</th>");
		}

		// --------------------------------------------------------------------
		// その他のファイルの削除
		// --------------------------------------------------------------------
		protected override void DeleteMisc()
		{
			Debug.Assert(!String.IsNullOrEmpty(_folderPath), "DeleteMisc() bad FolderPath");
			String[] reportPathes = Directory.GetFiles(_folderPath, "Report_*" + Common.FILE_EXT_PHP);

			foreach (String path in reportPathes)
			{
				try
				{
					File.Delete(path);
				}
				catch (Exception)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "古い報告ファイル " + Path.GetFileName(path) + " を削除できませんでした。");
				}
			}
		}

		// --------------------------------------------------------------------
		// リストに出力するファイル名の表現
		// ファイル名エスケープに関する備忘
		//   PHP print "" の中
		//     \ と " はファイル名として使われないので気にしなくて良い
		//     ' は "" の中であればエスケープ不要
		//     →従ってエスケープ不要
		//   HTML href "" の中
		//     \ < > はファイル名として使われない
		//     & ' 半角スペースがあっても動作する
		//     →従ってエスケープしなくても動作するようだが、UrlEncode() するほうが作法が良いのでしておく
		// --------------------------------------------------------------------
		protected override String? FileNameDescription(String? fileName)
		{
			if (String.IsNullOrEmpty(fileName))
			{
				return null;
			}

			return "<?php empty($yukarisearchlink) ? print \"" + fileName + "\" : print \"<a href=\\\"\".$yukarisearchlink.\"" + HttpUtility.UrlEncode(fileName)
					+ "\\\">" + fileName + "</a>\";?>";
		}

		// --------------------------------------------------------------------
		// 出力設定を生成
		// --------------------------------------------------------------------
		protected override void GenerateOutputSettingsCore()
		{
			OutputSettings = new YukariOutputSettings();
		}

		// --------------------------------------------------------------------
		// その他のファイルの出力
		// --------------------------------------------------------------------
		protected override void OutputMisc()
		{
			OutputReportCommon();
			OutputReportEntry();
			OutputReportRegist();
			CopySyncServerPhp();
		}

		// --------------------------------------------------------------------
		// コンストラクターでは行えない準備などを実施
		// --------------------------------------------------------------------
		protected override void PrepareOutput()
		{
			base.PrepareOutput();

			// 出力先フォルダー
			_folderPath = Path.GetDirectoryName(DbCommon.ListDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings)) + '\\';
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// 同期フォルダー
		private const String FOLDER_NAME_SYNC_SERVER = "SyncServer\\";

		// 同期フォルダーの共通ライブラリフォルダー
		private const String FOLDER_NAME_COMMON_LIB = "common_lib\\";

		// 報告用フォーム（共通）
		private const String FILE_NAME_REPORT_COMMON = "Report_Common" + Common.FILE_EXT_PHP;

		// 報告用フォーム（STEP 1：情報入力）
		private const String FILE_NAME_REPORT_ENTRY = "Report_Entry" + Common.FILE_EXT_PHP;

		// 報告用フォーム（STEP 2：情報登録）
		private const String FILE_NAME_REPORT_REGIST = "Report_Regist" + Common.FILE_EXT_PHP;

		// HTML テンプレートに記載されている変数
		private const String HTML_VAR_ID_PREFIX = "<!-- $IdPrefix$ -->";

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// SyncServer フォルダーから PHP をコピー
		// --------------------------------------------------------------------
		private void CopySyncServerPhp()
		{
			String srcFilder = YukaListerModel.Instance.EnvModel.ExeFullFolder + FOLDER_NAME_SYNC_SERVER + FOLDER_NAME_COMMON_LIB;
			File.Copy(srcFilder + "JulianDay.php", _folderPath + "Report_JulianDay.php");
		}

		// --------------------------------------------------------------------
		// リストのリンクの引数
		// oAdditionalArgs: "hoge=1&fuga=2" の形式
		// --------------------------------------------------------------------
		private static String ListLinkArg(String? additionalArgs = null)
		{
			return "<?php empty($yukarisearchlink) ? print \"" + (String.IsNullOrEmpty(additionalArgs) ? null : "?" + additionalArgs)
					+ "\" : print \"?yukarihost=\".$yukarihost" + (String.IsNullOrEmpty(additionalArgs) ? null : ".\"&" + additionalArgs + "\"") + ";?>";
		}

		// --------------------------------------------------------------------
		// Report_Common.php 出力
		// --------------------------------------------------------------------
		private void OutputReportCommon()
		{
			if (String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix))
			{
				throw new Exception("ID 先頭付与文字列が設定されていません。");
			}

			String template = LoadTemplate("YukariReportCommon");
			template = template.Replace(HTML_VAR_ID_PREFIX, YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix);
			File.WriteAllText(_folderPath + FILE_NAME_REPORT_COMMON, template, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// Report_Entry.php 出力
		// --------------------------------------------------------------------
		private void OutputReportEntry()
		{
			String template = LoadTemplate("YukariReportEntry");
			template = ReplacePhpContents(template);
			File.WriteAllText(_folderPath + FILE_NAME_REPORT_ENTRY, template, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// Report_Regist.php 出力
		// --------------------------------------------------------------------
		private void OutputReportRegist()
		{
			String template = LoadTemplate("YukariReportRegist");
			template = ReplacePhpContents(template);
			File.WriteAllText(_folderPath + FILE_NAME_REPORT_REGIST, template, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// ページ内容を置換
		// --------------------------------------------------------------------
		private String ReplacePhpContents(String template)
		{
			template = template.Replace(HTML_VAR_ADDITIONAL_NAVI, _additionalNavi);
			template = template.Replace(HTML_VAR_GENERATOR, YlConstants.APP_NAME_J + "  " + YlConstants.APP_VER);
			return template;
		}
	}
}
