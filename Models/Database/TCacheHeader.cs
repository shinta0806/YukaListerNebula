// ============================================================================
// 
// キャッシュ（リストデータベースのキャッシュ）管理テーブル
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
	[Table(TABLE_NAME_CACHE_HEADER)]
	public class TCacheHeader
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// TCacheHeader 独自
		// --------------------------------------------------------------------

		// ユニーク ID
		[Key]
		[Column(FIELD_NAME_CACHE_HEADER_UID)]
		public Int64 Uid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_CACHE_HEADER_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// --------------------------------------------------------------------
		// TFound
		// --------------------------------------------------------------------

		// 親フォルダー（追加ボタンをクリックした時のフォルダー）
		[Column(TFound.FIELD_NAME_FOUND_PARENT_FOLDER)]
		public String ParentFolder { get; set; } = String.Empty;

		// ====================================================================
		// public メンバー定数
		// ====================================================================

		public const String TABLE_NAME_CACHE_HEADER = "t_cache_header";
		public const String FIELD_NAME_CACHE_HEADER_UID = "cache_header_uid";
		public const String FIELD_NAME_CACHE_HEADER_UPDATE_TIME = "cache_header_update_time";
	}
}
