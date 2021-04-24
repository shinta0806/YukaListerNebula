// ============================================================================
// 
// タイアップグループマスターテーブル
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
	[Table(TABLE_NAME_TIE_UP_GROUP)]
	public class TTieUpGroup : IRcMaster
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タイアップグループ ID
		[Key]
		[Column(FIELD_NAME_TIE_UP_GROUP_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_GROUP_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// タイアップグループ名
		[Column(FIELD_NAME_TIE_UP_GROUP_NAME)]
		public String? Name { get; set; }

		// タイアップグループフリガナ
		[Column(FIELD_NAME_TIE_UP_GROUP_RUBY)]
		public String? Ruby { get; set; }

		// タイアップグループフリガナ（検索用）
		[Column(FIELD_NAME_TIE_UP_GROUP_RUBY_FOR_SEARCH)]
		public String? RubyForSearch { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_TIE_UP_GROUP_KEYWORD)]
		public String? Keyword { get; set; }

		// 検索ワードフリガナ（検索用）
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみを格納
		[Column(FIELD_NAME_TIE_UP_GROUP_KEYWORD_RUBY_FOR_SEARCH)]
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

		public const String TABLE_NAME_TIE_UP_GROUP = "t_tie_up_group";
		public const String FIELD_NAME_TIE_UP_GROUP_ID = "tie_up_group_id";
		public const String FIELD_NAME_TIE_UP_GROUP_IMPORT = "tie_up_group_import";
		public const String FIELD_NAME_TIE_UP_GROUP_INVALID = "tie_up_group_invalid";
		public const String FIELD_NAME_TIE_UP_GROUP_UPDATE_TIME = "tie_up_group_update_time";
		public const String FIELD_NAME_TIE_UP_GROUP_DIRTY = "tie_up_group_dirty";
		public const String FIELD_NAME_TIE_UP_GROUP_NAME = "tie_up_group_name";
		public const String FIELD_NAME_TIE_UP_GROUP_RUBY = "tie_up_group_ruby";
		public const String FIELD_NAME_TIE_UP_GROUP_RUBY_FOR_SEARCH = "tie_up_group_ruby_for_search";
		public const String FIELD_NAME_TIE_UP_GROUP_KEYWORD = "tie_up_group_keyword";
		public const String FIELD_NAME_TIE_UP_GROUP_KEYWORD_RUBY_FOR_SEARCH = "tie_up_group_keyword_ruby_for_search";
	}
}
