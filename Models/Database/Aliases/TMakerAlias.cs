// ============================================================================
// 
// 制作会社別名テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.Database.Aliases
{
	[Table(TABLE_NAME_MAKER_ALIAS)]
	public class TMakerAlias : IRcAlias
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 制作会社別名 ID
		[Key]
		[Column(FIELD_NAME_MAKER_ALIAS_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_MAKER_ALIAS_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_MAKER_ALIAS_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_MAKER_ALIAS_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_MAKER_ALIAS_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcAlias
		// --------------------------------------------------------------------

		// 制作会社別名
		[Column(FIELD_NAME_MAKER_ALIAS_ALIAS)]
		public String Alias { get; set; } = String.Empty;

		// 元の制作会社 ID ＜参照項目＞
		[Column(FIELD_NAME_MAKER_ALIAS_ORIGINAL_ID)]
		public String OriginalId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_MAKER_ALIAS = "t_maker_alias";
		public const String FIELD_PREFIX_MAKER_ALIAS = "maker_alias_";
		public const String FIELD_NAME_MAKER_ALIAS_ID = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_ID;
		public const String FIELD_NAME_MAKER_ALIAS_IMPORT = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_IMPORT;
		public const String FIELD_NAME_MAKER_ALIAS_INVALID = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_INVALID;
		public const String FIELD_NAME_MAKER_ALIAS_UPDATE_TIME = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_UPDATE_TIME;
		public const String FIELD_NAME_MAKER_ALIAS_DIRTY = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_DIRTY;
		public const String FIELD_NAME_MAKER_ALIAS_ALIAS = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_ALIAS;
		public const String FIELD_NAME_MAKER_ALIAS_ORIGINAL_ID = FIELD_PREFIX_MAKER_ALIAS + YlConstants.FIELD_SUFFIX_ORIGINAL_ID;
	}
}
