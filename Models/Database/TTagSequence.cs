// ============================================================================
// 
// タグ紐付テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database
{
	[Table(TABLE_NAME_TAG_SEQUENCE)]
	public class TTagSequence : IRcSequence
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 楽曲 ID ＜参照項目＞
		[Column(FIELD_NAME_TAG_SEQUENCE_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TAG_SEQUENCE_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TAG_SEQUENCE_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TAG_SEQUENCE_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TAG_SEQUENCE_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcSequence
		// --------------------------------------------------------------------

		// 連番
		[Column(FIELD_NAME_TAG_SEQUENCE_SEQUENCE)]
		public Int32 Sequence { get; set; }

		// タグ ID ＜参照項目＞
		[Column(FIELD_NAME_TAG_SEQUENCE_LINK_ID)]
		public String LinkId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TAG_SEQUENCE = "t_tag_sequence";
		public const String FIELD_NAME_TAG_SEQUENCE_ID = "tag_sequence_id";
		public const String FIELD_NAME_TAG_SEQUENCE_SEQUENCE = "tag_sequence_sequence";
		public const String FIELD_NAME_TAG_SEQUENCE_LINK_ID = "tag_sequence_link_id";
		public const String FIELD_NAME_TAG_SEQUENCE_IMPORT = "tag_sequence_import";
		public const String FIELD_NAME_TAG_SEQUENCE_INVALID = "tag_sequence_invalid";
		public const String FIELD_NAME_TAG_SEQUENCE_UPDATE_TIME = "tag_sequence_update_time";
		public const String FIELD_NAME_TAG_SEQUENCE_DIRTY = "tag_sequence_dirty";
	}
}
