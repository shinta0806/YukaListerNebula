// ============================================================================
// 
// カテゴリーマスターテーブル
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
	[Table(TABLE_NAME_CATEGORY)]
	public class TCategory : IRcMaster
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// カテゴリー ID
		[Key]
		[Column(FIELD_NAME_CATEGORY_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_CATEGORY_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_CATEGORY_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_CATEGORY_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_CATEGORY_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// カテゴリー名
		[Column(FIELD_NAME_CATEGORY_NAME)]
		public String? Name { get; set; }

		// カテゴリーフリガナ
		[Column(FIELD_NAME_CATEGORY_RUBY)]
		public String? Ruby { get; set; }

		// カテゴリーフリガナ（検索用）
		[Column(FIELD_NAME_CATEGORY_RUBY_FOR_SEARCH)]
		public String? RubyForSearch { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_CATEGORY_KEYWORD)]
		public String? Keyword { get; set; }

		// 検索ワードフリガナ（検索用）
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみを格納
		[Column(FIELD_NAME_CATEGORY_KEYWORD_RUBY_FOR_SEARCH)]
		public String? KeywordRubyForSearch { get; set; }

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

		public const String TABLE_NAME_CATEGORY = "t_category";
		public const String FIELD_NAME_CATEGORY_ID = "category_id";
		public const String FIELD_NAME_CATEGORY_IMPORT = "category_import";
		public const String FIELD_NAME_CATEGORY_INVALID = "category_invalid";
		public const String FIELD_NAME_CATEGORY_UPDATE_TIME = "category_update_time";
		public const String FIELD_NAME_CATEGORY_DIRTY = "category_dirty";
		public const String FIELD_NAME_CATEGORY_NAME = "category_name";
		public const String FIELD_NAME_CATEGORY_RUBY = "category_ruby";
		public const String FIELD_NAME_CATEGORY_RUBY_FOR_SEARCH = "category_ruby_for_search";
		public const String FIELD_NAME_CATEGORY_KEYWORD = "category_keyword";
		public const String FIELD_NAME_CATEGORY_KEYWORD_RUBY_FOR_SEARCH = "category_keyword_ruby_for_search";
	}
}
