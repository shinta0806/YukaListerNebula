// ============================================================================
// 
// タイアップグループ紐付テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Sequences
{
	[Table(TABLE_NAME_TIE_UP_GROUP_SEQUENCE)]
	public class TTieUpGroupSequence : IRcSequence
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タイアップ ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcSequence
		// --------------------------------------------------------------------

		// 連番
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_SEQUENCE)]
		public Int32 Sequence { get; set; }

		// タイアップグループ ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_GROUP_SEQUENCE_LINK_ID)]
		public String LinkId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TIE_UP_GROUP_SEQUENCE = "t_tie_up_group_sequence";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_ID = "tie_up_group_sequence_id";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_SEQUENCE = "tie_up_group_sequence_sequence";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_LINK_ID = "tie_up_group_sequence_link_id";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_IMPORT = "tie_up_group_sequence_import";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_INVALID = "tie_up_group_sequence_invalid";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_UPDATE_TIME = "tie_up_group_sequence_update_time";
		public const String FIELD_NAME_TIE_UP_GROUP_SEQUENCE_DIRTY = "tie_up_group_sequence_dirty";
	}
}
