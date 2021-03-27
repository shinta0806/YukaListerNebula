// ============================================================================
// 
// 編曲者紐付テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Sequences
{
	[Table(TABLE_NAME_ARRANGER_SEQUENCE)]
	public class TArrangerSequence : IRcSequence
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 楽曲 ID ＜参照項目＞
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcSequence
		// --------------------------------------------------------------------

		// 連番
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_SEQUENCE)]
		public Int32 Sequence { get; set; }

		// 人物 ID ＜参照項目＞
		[Column(FIELD_NAME_ARRANGER_SEQUENCE_LINK_ID)]
		public String LinkId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_ARRANGER_SEQUENCE = "t_arranger_sequence";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_ID = "arranger_sequence_id";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_SEQUENCE = "arranger_sequence_sequence";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_LINK_ID = "arranger_sequence_link_id";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_IMPORT = "arranger_sequence_import";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_INVALID = "arranger_sequence_invalid";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_UPDATE_TIME = "arranger_sequence_update_time";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_DIRTY = "arranger_sequence_dirty";
	}
}
