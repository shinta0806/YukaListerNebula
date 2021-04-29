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

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;

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

		// タグフリガナ（検索用）
		[Column(FIELD_NAME_TAG_RUBY_FOR_SEARCH)]
		public String? RubyForSearch { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_TAG_KEYWORD)]
		public String? Keyword { get; set; }

		// 検索ワードフリガナ（検索用）
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみを格納
		[Column(FIELD_NAME_TAG_KEYWORD_RUBY_FOR_SEARCH)]
		public String? KeywordRubyForSearch { get; set; }

		// 同名の区別が付くように DisplayName を設定する
		[NotMapped]
		public Boolean AvoidSameName { get; set; }

		// 表示名
		public String? DisplayName
		{
			get => DbCommon.DisplayNameByDefaultAlgorithm(this);
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TAG = "t_tag";
		public const String FIELD_PREFIX_TAG = "tag_";
		public const String FIELD_NAME_TAG_ID = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_ID;
		public const String FIELD_NAME_TAG_IMPORT = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_IMPORT;
		public const String FIELD_NAME_TAG_INVALID = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_INVALID;
		public const String FIELD_NAME_TAG_UPDATE_TIME = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_UPDATE_TIME;
		public const String FIELD_NAME_TAG_DIRTY = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_DIRTY;
		public const String FIELD_NAME_TAG_NAME = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_NAME;
		public const String FIELD_NAME_TAG_RUBY = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_RUBY;
		public const String FIELD_NAME_TAG_RUBY_FOR_SEARCH = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_RUBY_FOR_SEARCH;
		public const String FIELD_NAME_TAG_KEYWORD = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_KEYWORD;
		public const String FIELD_NAME_TAG_KEYWORD_RUBY_FOR_SEARCH = FIELD_PREFIX_TAG + YlConstants.FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH;
	}
}
