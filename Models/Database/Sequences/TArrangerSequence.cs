﻿// ============================================================================
// 
// 編曲者紐付テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 主キーは複合キーなので Fluent API で設定する
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;

using YukaLister.Models.SharedMisc;

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
		public const String FIELD_PREFIX_ARRANGER_SEQUENCE = "arranger_sequence_";
		public const String FIELD_NAME_ARRANGER_SEQUENCE_ID = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_ID;
		public const String FIELD_NAME_ARRANGER_SEQUENCE_IMPORT = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_IMPORT;
		public const String FIELD_NAME_ARRANGER_SEQUENCE_INVALID = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_INVALID;
		public const String FIELD_NAME_ARRANGER_SEQUENCE_UPDATE_TIME = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_UPDATE_TIME;
		public const String FIELD_NAME_ARRANGER_SEQUENCE_DIRTY = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_DIRTY;
		public const String FIELD_NAME_ARRANGER_SEQUENCE_SEQUENCE = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_SEQUENCE;
		public const String FIELD_NAME_ARRANGER_SEQUENCE_LINK_ID = FIELD_PREFIX_ARRANGER_SEQUENCE + YlConstants.FIELD_SUFFIX_LINK_ID;
	}
}
