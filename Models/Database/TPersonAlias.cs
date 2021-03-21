// ============================================================================
// 
// 人物別名テーブル
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
	[Table(TABLE_NAME_PERSON_ALIAS)]
	public class TPersonAlias : IRcAlias
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 人物別名 ID
		[Key]
		[Column(FIELD_NAME_PERSON_ALIAS_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_PERSON_ALIAS_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_PERSON_ALIAS_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_PERSON_ALIAS_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_PERSON_ALIAS_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcAlias
		// --------------------------------------------------------------------

		// 人物別名
		[Column(FIELD_NAME_PERSON_ALIAS_ALIAS)]
		public String Alias { get; set; } = String.Empty;

		// 元の人物 ID ＜参照項目＞
		[Column(FIELD_NAME_PERSON_ALIAS_ORIGINAL_ID)]
		public String OriginalId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_PERSON_ALIAS = "t_person_alias";
		public const String FIELD_NAME_PERSON_ALIAS_ID = "person_alias_id";
		public const String FIELD_NAME_PERSON_ALIAS_IMPORT = "person_alias_import";
		public const String FIELD_NAME_PERSON_ALIAS_INVALID = "person_alias_invalid";
		public const String FIELD_NAME_PERSON_ALIAS_UPDATE_TIME = "person_alias_update_time";
		public const String FIELD_NAME_PERSON_ALIAS_DIRTY = "person_alias_dirty";
		public const String FIELD_NAME_PERSON_ALIAS_ALIAS = "person_alias_alias";
		public const String FIELD_NAME_PERSON_ALIAS_ORIGINAL_ID = "person_alias_original_id";
	}
}
