// ============================================================================
// 
// データベースプロパティーテーブル
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
	[Table(TABLE_NAME_PROPERTY)]
	public class TProperty
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// データベース更新時のアプリケーション ID
		[Key]
		[Column(FIELD_NAME_PROPERTY_APP_ID)]
		public String AppId { get; set; } = String.Empty;

		// データベース更新時のアプリケーションのバージョン
		[Column(FIELD_NAME_PROPERTY_APP_VER)]
		public String AppVer { get; set; } = String.Empty;

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_PROPERTY = "t_property";
		public const String FIELD_NAME_PROPERTY_APP_ID = "property_app_id";
		public const String FIELD_NAME_PROPERTY_APP_VER = "property_app_ver";
	}
}
