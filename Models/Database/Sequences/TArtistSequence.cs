// ============================================================================
// 
// 歌手紐付テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Sequences
{
	[Table(TABLE_NAME_ARTIST_SEQUENCE)]
	public class TArtistSequence : IRcSequence
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 楽曲 ID ＜参照項目＞
		[Column(FIELD_NAME_ARTIST_SEQUENCE_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_ARTIST_SEQUENCE_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_ARTIST_SEQUENCE_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_ARTIST_SEQUENCE_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_ARTIST_SEQUENCE_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcSequence
		// --------------------------------------------------------------------

		// 連番
		[Column(FIELD_NAME_ARTIST_SEQUENCE_SEQUENCE)]
		public Int32 Sequence { get; set; }

		// 人物 ID ＜参照項目＞
		[Column(FIELD_NAME_ARTIST_SEQUENCE_LINK_ID)]
		public String LinkId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_ARTIST_SEQUENCE = "t_artist_sequence";
		public const String FIELD_NAME_ARTIST_SEQUENCE_ID = "artist_sequence_id";
		public const String FIELD_NAME_ARTIST_SEQUENCE_SEQUENCE = "artist_sequence_sequence";
		public const String FIELD_NAME_ARTIST_SEQUENCE_LINK_ID = "artist_sequence_link_id";
		public const String FIELD_NAME_ARTIST_SEQUENCE_IMPORT = "artist_sequence_import";
		public const String FIELD_NAME_ARTIST_SEQUENCE_INVALID = "artist_sequence_invalid";
		public const String FIELD_NAME_ARTIST_SEQUENCE_UPDATE_TIME = "artist_sequence_update_time";
		public const String FIELD_NAME_ARTIST_SEQUENCE_DIRTY = "artist_sequence_dirty";
	}
}
