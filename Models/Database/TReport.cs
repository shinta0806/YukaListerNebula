// ============================================================================
// 
// リスト問題報告テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.Database
{
	[Table(TABLE_NAME_REPORT)]
	public class TReport : IRcBase
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 報告 ID
		[Key]
		[Column(FIELD_NAME_REPORT_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_REPORT_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_REPORT_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_REPORT_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_REPORT_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// TReport 独自項目
		// --------------------------------------------------------------------

		// 対象ファイルフルパス
		[Column(FIELD_NAME_REPORT_PATH)]
		public String Path { get; set; } = String.Empty;

		// 修正項目インデックス
		[Column(FIELD_NAME_REPORT_ADJUST_KEY)]
		public Int32 AdjustKey { get; set; }

		// 修正前の値
		[Column(FIELD_NAME_REPORT_BAD_VALUE)]
		public String? BadValue { get; set; }

		// 修正後の値
		[Column(FIELD_NAME_REPORT_ADJUST_VALUE)]
		public String AdjustValue { get; set; } = String.Empty;

		// 報告コメント
		[Column(FIELD_NAME_REPORT_REPORTER_COMMENT)]
		public String? ReporterComment { get; set; }

		// 報告者名
		[Column(FIELD_NAME_REPORT_BY)]
		public String By { get; set; } = String.Empty;

		// 報告者 IP
		[Column(FIELD_NAME_REPORT_IP)]
		public String Ip { get; set; } = String.Empty;

		// 報告者ホスト
		[Column(FIELD_NAME_REPORT_HOST)]
		public String? Host { get; set; }

		// 報告日時 UTC（最初に登録した時の日時）
		[Column(FIELD_NAME_REPORT_REGIST_TIME)]
		public Double RegistTime { get; set; }

		// 対応コメント
		[Column(FIELD_NAME_REPORT_STATUS_COMMENT)]
		public String? StatusComment { get; set; }

		// 対応状況インデックス
		[Column(FIELD_NAME_REPORT_STATUS)]
		public Int32 Status { get; set; }

		// 対応者
		[Column(FIELD_NAME_REPORT_STATUS_BY)]
		public String? StatusBy { get; set; }

		// --------------------------------------------------------------------
		// ViewTReportsWindow 表示用
		// --------------------------------------------------------------------

		// パス無しのファイル名
		public String FileName
		{
			get => System.IO.Path.GetFileName(Path);
		}

		// 問題項目名
		public String? AdjustKeyName
		{
			get
			{
				if (AdjustKey < (Int32)ReportAdjustKey.Invalid || AdjustKey >= (Int32)ReportAdjustKey.__End__)
				{
					return null;
				}
				return YlConstants.REPORT_ADJUST_KEY_NAMES[AdjustKey];
			}
		}

		// 報告日文字列
		public String RegistDateString
		{
			get
			{
				DateTime registTime = JulianDay.ModifiedJulianDateToDateTime(RegistTime);
				return TimeZoneInfo.ConvertTimeFromUtc(registTime, TimeZoneInfo.Local).ToString(YlConstants.DATE_FORMAT);
			}
		}

		// 対応状況名
		public String? StatusName
		{
			get
			{
				if (Status < 0 || Status >= (Int32)ReportStatus.__End__)
				{
					return null;
				}
				return YlConstants.REPORT_STATUS_NAMES[Status];
			}
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_REPORT = "t_report";
		public const String FIELD_NAME_REPORT_ID = "report_id";
		public const String FIELD_NAME_REPORT_IMPORT = "report_import";
		public const String FIELD_NAME_REPORT_INVALID = "report_invalid";
		public const String FIELD_NAME_REPORT_UPDATE_TIME = "report_update_time";
		public const String FIELD_NAME_REPORT_DIRTY = "report_dirty";
		public const String FIELD_NAME_REPORT_PATH = "report_path";
		public const String FIELD_NAME_REPORT_ADJUST_KEY = "report_adjust_key";
		public const String FIELD_NAME_REPORT_BAD_VALUE = "report_bad_value";
		public const String FIELD_NAME_REPORT_ADJUST_VALUE = "report_adjust_value";
		public const String FIELD_NAME_REPORT_REPORTER_COMMENT = "report_reporter_comment";
		public const String FIELD_NAME_REPORT_BY = "report_by";
		public const String FIELD_NAME_REPORT_IP = "report_ip";
		public const String FIELD_NAME_REPORT_HOST = "report_host";
		public const String FIELD_NAME_REPORT_REGIST_TIME = "report_regist_time";
		public const String FIELD_NAME_REPORT_STATUS_COMMENT = "report_status_comment";
		public const String FIELD_NAME_REPORT_STATUS = "report_status";
		public const String FIELD_NAME_REPORT_STATUS_BY = "report_status_by";
	}
}
