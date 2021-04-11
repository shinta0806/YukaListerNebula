// ============================================================================
// 
// フォルダー設定ウィンドウでのプレビュー情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukaLister.Models.SharedMisc
{
	public class PreviewInfo
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ファイル名（パス無）
		public String FileName { get; set; } = String.Empty;

		// 取得項目
		public String Items { get; set; } = String.Empty;

		// ファイル最終更新日時（修正ユリウス日）
		public Double LastWriteTime { get; set; }

		// 表示用：ファイル最終更新日時（修正ユリウス日）
		public String LastWriteTimeLabel
		{
			get => JulianDay.ModifiedJulianDateToDateTime(LastWriteTime).ToString(YlConstants.DATE_FORMAT);
		}

		// サブフォルダー
		public String? SubFolder { get; set; }
	}
}
