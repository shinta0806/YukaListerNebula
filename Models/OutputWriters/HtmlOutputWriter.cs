// ============================================================================
// 
// HTML リスト出力クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;

namespace YukaLister.Models.OutputWriters
{
	public class HtmlOutputWriter : WebOutputWriter
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public HtmlOutputWriter()
				: base(Common.FILE_EXT_HTML)
		{
			// プロパティー
			FormatName = "HTML";
			TopFileName = "index" + Common.FILE_EXT_HTML;
			//OutputSettings = new HtmlOutputSettings();

			// メンバー変数
			_additionalDescription = null;
			_additionalHeader = null;
			_additionalNavi = null;
			_listLinkArg = null;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リストに出力するファイル名の表現
		// --------------------------------------------------------------------
		protected override String? FileNameDescription(String? fileName)
		{
			return fileName;
		}

		// --------------------------------------------------------------------
		// 出力設定を生成
		// --------------------------------------------------------------------
		protected override void GenerateOutputSettingsCore()
		{
			OutputSettings = new HtmlOutputSettings();
		}

		// --------------------------------------------------------------------
		// コンストラクターでは行えない準備などを実施
		// --------------------------------------------------------------------
		protected override void PrepareOutput()
		{
			base.PrepareOutput();

			// 出力先フォルダー
			SetFolderPathByYlSettings();
		}
	}
}
