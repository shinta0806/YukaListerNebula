// ============================================================================
// 
// 楽曲マスターテーブル
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
	[Table(TABLE_NAME_SONG)]
	public class TSong : IRcCategorizable
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 楽曲 ID
		[Key]
		[Column(FIELD_NAME_SONG_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_SONG_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_SONG_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_SONG_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_SONG_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// 楽曲名
		[Column(FIELD_NAME_SONG_NAME)]
		public String? Name { get; set; }

		// 楽曲フリガナ
		[Column(FIELD_NAME_SONG_RUBY)]
		public String? Ruby { get; set; }

		// 楽曲フリガナ（検索用）
		[Column(FIELD_NAME_SONG_RUBY_FOR_SEARCH)]
		public String? RubyForSearch { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_SONG_KEYWORD)]
		public String? Keyword { get; set; }

		// 検索ワードフリガナ（検索用）
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみを格納
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

		// --------------------------------------------------------------------
		// IRcCategorizable
		// --------------------------------------------------------------------

		// カテゴリー ID ＜参照項目＞（タイアップ ID が null の場合のみ）
		[Column(FIELD_NAME_SONG_CATEGORY_ID)]
		public String? CategoryId { get; set; }

		// リリース日（修正ユリウス日）
		[Column(FIELD_NAME_SONG_RELEASE_DATE)]
		public Double ReleaseDate { get; set; }

		// --------------------------------------------------------------------
		// TSong 独自項目
		// --------------------------------------------------------------------

		// タイアップ ID ＜参照項目＞
		[Column(FIELD_NAME_SONG_TIE_UP_ID)]
		public String? TieUpId { get; set; }

		// 摘要
		[Column(FIELD_NAME_SONG_OP_ED)]
		public String? OpEd { get; set; }

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_SONG = "t_song";
		public const String FIELD_NAME_SONG_ID = "song_id";
		public const String FIELD_NAME_SONG_IMPORT = "song_import";
		public const String FIELD_NAME_SONG_INVALID = "song_invalid";
		public const String FIELD_NAME_SONG_UPDATE_TIME = "song_update_time";
		public const String FIELD_NAME_SONG_DIRTY = "song_dirty";
		public const String FIELD_NAME_SONG_NAME = "song_name";
		public const String FIELD_NAME_SONG_RUBY = "song_ruby";
		public const String FIELD_NAME_SONG_RUBY_FOR_SEARCH = "song_ruby_for_search";
		public const String FIELD_NAME_SONG_KEYWORD = "song_keyword";
		public const String FIELD_NAME_SONG_RELEASE_DATE = "song_release_date";
		public const String FIELD_NAME_SONG_TIE_UP_ID = "song_tie_up_id";
		public const String FIELD_NAME_SONG_CATEGORY_ID = "song_category_id";
		public const String FIELD_NAME_SONG_OP_ED = "song_op_ed";
	}
}
