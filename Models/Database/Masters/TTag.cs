// ============================================================================
// 
// タグマスターテーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Masters
{
	[Table(TABLE_NAME_TAG)]
	public class TTag : IRcMaster
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タグ ID
		[Key]
		[Column(FIELD_NAME_TAG_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TAG_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TAG_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TAG_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TAG_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// タグ名
		[Column(FIELD_NAME_TAG_NAME)]
		public String? Name { get; set; }

		// タグフリガナ
		[Column(FIELD_NAME_TAG_RUBY)]
		public String? Ruby { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_TAG_KEYWORD)]
		public String? Keyword { get; set; }

		// 同名の区別が付くように DisplayName を設定する
		[NotMapped]
		public Boolean AvoidSameName { get; set; }

		// 表示名
		private String? _displayName;
		public String? DisplayName
		{
			get
			{
				if (String.IsNullOrEmpty(_displayName))
				{
					if (AvoidSameName)
					{
						_displayName = Name + "（" + (String.IsNullOrEmpty(Keyword) ? "キーワード無し" : Keyword) + "）";
					}
					else
					{
						_displayName = Name;
					}
				}
				return _displayName;
			}
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TAG = "t_tag";
		public const String FIELD_NAME_TAG_ID = "tag_id";
		public const String FIELD_NAME_TAG_IMPORT = "tag_import";
		public const String FIELD_NAME_TAG_INVALID = "tag_invalid";
		public const String FIELD_NAME_TAG_UPDATE_TIME = "tag_update_time";
		public const String FIELD_NAME_TAG_DIRTY = "tag_dirty";
		public const String FIELD_NAME_TAG_NAME = "tag_name";
		public const String FIELD_NAME_TAG_RUBY = "tag_ruby";
		public const String FIELD_NAME_TAG_KEYWORD = "tag_keyword";
	}
}
