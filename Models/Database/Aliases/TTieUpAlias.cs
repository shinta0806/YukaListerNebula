// ============================================================================
// 
// タイアップ別名テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Aliases
{
	[Table(TABLE_NAME_TIE_UP_ALIAS)]
	public class TTieUpAlias : IRcAlias
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タイアップ別名 ID
		[Key]
		[Column(FIELD_NAME_TIE_UP_ALIAS_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TIE_UP_ALIAS_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TIE_UP_ALIAS_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_ALIAS_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TIE_UP_ALIAS_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcAlias
		// --------------------------------------------------------------------

		// タイアップ別名
		[Column(FIELD_NAME_TIE_UP_ALIAS_ALIAS)]
		public String Alias { get; set; } = String.Empty;

		// 元のタイアップ ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_ALIAS_ORIGINAL_ID)]
		public String OriginalId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TIE_UP_ALIAS = "t_tie_up_alias";
		public const String FIELD_NAME_TIE_UP_ALIAS_ID = "tie_up_alias_id";
		public const String FIELD_NAME_TIE_UP_ALIAS_IMPORT = "tie_up_alias_import";
		public const String FIELD_NAME_TIE_UP_ALIAS_INVALID = "tie_up_alias_invalid";
		public const String FIELD_NAME_TIE_UP_ALIAS_UPDATE_TIME = "tie_up_alias_update_time";
		public const String FIELD_NAME_TIE_UP_ALIAS_DIRTY = "tie_up_alias_dirty";
		public const String FIELD_NAME_TIE_UP_ALIAS_ALIAS = "tie_up_alias_alias";
		public const String FIELD_NAME_TIE_UP_ALIAS_ORIGINAL_ID = "tie_up_alias_original_id";
	}
}
