// ============================================================================
// 
// タイアップグループ別名テーブル
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
	[Table(TABLE_NAME_TIE_UP_GROUP_ALIAS)]
	public class TTieUpGroupAlias : IRcAlias
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タイアップグループ別名 ID
		[Key]
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcAlias
		// --------------------------------------------------------------------

		// タイアップグループ別名
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_ALIAS)]
		public String Alias { get; set; } = String.Empty;

		// 元のタイアップグループ ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_GROUP_ALIAS_ORIGINAL_ID)]
		public String OriginalId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TIE_UP_GROUP_ALIAS = "t_tie_up_group_alias";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_ID = "tie_up_group_alias_id";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_IMPORT = "tie_up_group_alias_import";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_INVALID = "tie_up_group_alias_invalid";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_UPDATE_TIME = "tie_up_group_alias_update_time";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_DIRTY = "tie_up_group_alias_dirty";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_ALIAS = "tie_up_group_alias_alias";
		public const String FIELD_NAME_TIE_UP_GROUP_ALIAS_ORIGINAL_ID = "tie_up_group_alias_original_id";
	}
}
