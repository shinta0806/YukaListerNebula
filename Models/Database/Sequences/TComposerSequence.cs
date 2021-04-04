// ============================================================================
// 
// 作曲者紐付テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 主キーは複合キーなので Fluent API で設定する
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Sequences
{
	[Table(TABLE_NAME_COMPOSER_SEQUENCE)]
	public class TComposerSequence : IRcSequence
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 楽曲 ID ＜参照項目＞
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcSequence
		// --------------------------------------------------------------------

		// 連番
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_SEQUENCE)]
		public Int32 Sequence { get; set; }

		// 人物 ID ＜参照項目＞
		[Column(FIELD_NAME_COMPOSER_SEQUENCE_LINK_ID)]
		public String LinkId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_COMPOSER_SEQUENCE = "t_composer_sequence";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_ID = "composer_sequence_id";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_SEQUENCE = "composer_sequence_sequence";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_LINK_ID = "composer_sequence_link_id";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_IMPORT = "composer_sequence_import";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_INVALID = "composer_sequence_invalid";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_UPDATE_TIME = "composer_sequence_update_time";
		public const String FIELD_NAME_COMPOSER_SEQUENCE_DIRTY = "composer_sequence_dirty";
	}
}
