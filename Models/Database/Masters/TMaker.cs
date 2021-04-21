// ============================================================================
// 
// 制作会社マスターテーブル
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
	[Table(TABLE_NAME_MAKER)]
	public class TMaker : IRcMaster
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 制作会社 ID
		[Key]
		[Column(FIELD_NAME_MAKER_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_MAKER_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_MAKER_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_MAKER_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_MAKER_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// 制作会社名
		[Column(FIELD_NAME_MAKER_NAME)]
		public String? Name { get; set; }

		// 制作会社フリガナ
		[Column(FIELD_NAME_MAKER_RUBY)]
		public String? Ruby { get; set; }

		// 制作会社フリガナ（検索用）
		[Column(FIELD_NAME_MAKER_RUBY_FOR_SEARCH)]
		public String? RubyForSearch { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_MAKER_KEYWORD)]
		public String? Keyword { get; set; }

		// 検索ワードフリガナ（検索用）
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみを格納
		[Column(FIELD_NAME_MAKER_KEYWORD_RUBY_FOR_SEARCH)]
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

		public const String TABLE_NAME_MAKER = "t_maker";
		public const String FIELD_NAME_MAKER_ID = "maker_id";
		public const String FIELD_NAME_MAKER_IMPORT = "maker_import";
		public const String FIELD_NAME_MAKER_INVALID = "maker_invalid";
		public const String FIELD_NAME_MAKER_UPDATE_TIME = "maker_update_time";
		public const String FIELD_NAME_MAKER_DIRTY = "maker_dirty";
		public const String FIELD_NAME_MAKER_NAME = "maker_name";
		public const String FIELD_NAME_MAKER_RUBY = "maker_ruby";
		public const String FIELD_NAME_MAKER_RUBY_FOR_SEARCH = "maker_ruby_for_search";
		public const String FIELD_NAME_MAKER_KEYWORD = "maker_keyword";
		public const String FIELD_NAME_MAKER_KEYWORD_RUBY_FOR_SEARCH = "maker_keyword_ruby_for_search";
	}
}
