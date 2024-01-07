// ============================================================================
// 
// サムネイルキャッシュテーブル
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
	internal class TCacheThumb
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// TCacheThumb 独自
		// --------------------------------------------------------------------

		// キャッシュサムネイルユニーク ID
		[Key]
		[Column(FIELD_NAME_CACHE_THUMB_UID)]
		public Int64 Uid { get; set; }

		// ファイル名（パス無し）
		[Column(FIELD_NAME_CACHE_THUMB_FILE_NAME)]
		public String FileName { get; set; } = String.Empty;

		// サムネイル横サイズ
		[Column(FIELD_NAME_CACHE_THUMB_WIDTH)]
		public Int32 Width { get; set; }

		// サムネイル画像データ
		[Column(FIELD_NAME_CACHE_THUMB_IMAGE)]
		public Byte[] Image { get; set; } = Array.Empty<Byte>();

		// 動画ファイル最終更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_CACHE_THUMB_FILE_LAST_WRITE_TIME)]
		public Double FileLastWriteTime { get; set; }

		// サムネイル最終更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_CACHE_THUMB_THUMB_LAST_WRITE_TIME)]
		public Double ThumbLastWriteTime { get; set; }

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_CACHE_THUMB = "t_cache_thumb";
		public const String FIELD_NAME_CACHE_THUMB_UID = "cache_thumb_uid";
		public const String FIELD_NAME_CACHE_THUMB_FILE_NAME = "cache_thumb_file_name";
		public const String FIELD_NAME_CACHE_THUMB_WIDTH = "cache_thumb_width";
		public const String FIELD_NAME_CACHE_THUMB_IMAGE = "cache_thumb_image";
		public const String FIELD_NAME_CACHE_THUMB_FILE_LAST_WRITE_TIME = "cache_thumb_file_last_write_time";
		public const String FIELD_NAME_CACHE_THUMB_THUMB_LAST_WRITE_TIME = "cache_thumb_thumb_last_write_time";
	}
}
