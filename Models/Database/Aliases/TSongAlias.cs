// ============================================================================
// 
// 楽曲別名テーブル
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
	[Table(TABLE_NAME_SONG_ALIAS)]
	public class TSongAlias : IRcAlias
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 楽曲別名 ID
		[Key]
		[Column(FIELD_NAME_SONG_ALIAS_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_SONG_ALIAS_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_SONG_ALIAS_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_SONG_ALIAS_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_SONG_ALIAS_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcAlias
		// --------------------------------------------------------------------

		// 楽曲別名
		[Column(FIELD_NAME_SONG_ALIAS_ALIAS)]
		public String Alias { get; set; } = String.Empty;

		// 元の楽曲 ID ＜参照項目＞
		[Column(FIELD_NAME_SONG_ALIAS_ORIGINAL_ID)]
		public String OriginalId { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_SONG_ALIAS = "t_song_alias";
		public const String FIELD_NAME_SONG_ALIAS_ID = "song_alias_id";
		public const String FIELD_NAME_SONG_ALIAS_IMPORT = "song_alias_import";
		public const String FIELD_NAME_SONG_ALIAS_INVALID = "song_alias_invalid";
		public const String FIELD_NAME_SONG_ALIAS_UPDATE_TIME = "song_alias_update_time";
		public const String FIELD_NAME_SONG_ALIAS_SYNC_TIME = "song_alias_sync_time";
		public const String FIELD_NAME_SONG_ALIAS_DIRTY = "song_alias_dirty";
		public const String FIELD_NAME_SONG_ALIAS_ALIAS = "song_alias_alias";
		public const String FIELD_NAME_SONG_ALIAS_ORIGINAL_ID = "song_alias_original_id";
	}
}
