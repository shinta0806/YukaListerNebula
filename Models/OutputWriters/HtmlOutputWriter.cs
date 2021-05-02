﻿// ============================================================================
// 
// HTML リスト出力クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Diagnostics;
using System.IO;
using YukaLister.Models.YukaListerModels;

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
			OutputSettings = new HtmlOutputSettings();

			// メンバー変数
			_additionalDescription = null;
			_additionalHeader = null;
			_additionalNavi = null;
			_listLinkArg = null;
		}

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
		// コンストラクターでは行えない準備などを実施
		// --------------------------------------------------------------------
		protected override void PrepareOutput()
		{
			base.PrepareOutput();

			// 出力先フォルダー
			Debug.Assert(!String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder), "PrepareOutput bad output folder");
			if (!Directory.Exists(YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder))
			{
				Directory.CreateDirectory(YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder);
			}
			_folderPath = YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder;
		}
	}
}
