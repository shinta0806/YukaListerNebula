// ============================================================================
// 
// タイアップマスターテーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using YukaLister.Models.DatabaseContexts;

namespace YukaLister.Models.Database.Masters
{
	[Table(TABLE_NAME_TIE_UP)]
	public class TTieUp : IRcCategorizable
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タイアップ ID
		[Key]
		[Column(FIELD_NAME_TIE_UP_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TIE_UP_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TIE_UP_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TIE_UP_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// タイアップ名
		[Column(FIELD_NAME_TIE_UP_NAME)]
		public String? Name { get; set; }

		// タイアップフリガナ
		[Column(FIELD_NAME_TIE_UP_RUBY)]
		public String? Ruby { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_TIE_UP_KEYWORD)]
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
						TCategory? category;
						using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TCategory> categories);
						category = DbCommon.SelectBaseById(categories, CategoryId);
						_displayName = Name + "（" + (String.IsNullOrEmpty(category?.Name) ? "カテゴリー無し" : category?.Name) + ", "
								+ (String.IsNullOrEmpty(Keyword) ? "キーワード無し" : Keyword) + "）";
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

		// カテゴリー ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_CATEGORY_ID)]
		public String? CategoryId { get; set; }

		// リリース日（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_RELEASE_DATE)]
		public Double ReleaseDate { get; set; }

		// --------------------------------------------------------------------
		// TTieUp 独自項目
		// --------------------------------------------------------------------

		// 制作会社 ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_MAKER_ID)]
		public String? MakerId { get; set; }

		// 年齢制限（○歳以上対象）
		[Column(FIELD_NAME_TIE_UP_AGE_LIMIT)]
		public Int32 AgeLimit { get; set; }

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TIE_UP = "t_tie_up";
		public const String FIELD_NAME_TIE_UP_ID = "tie_up_id";
		public const String FIELD_NAME_TIE_UP_IMPORT = "tie_up_import";
		public const String FIELD_NAME_TIE_UP_INVALID = "tie_up_invalid";
		public const String FIELD_NAME_TIE_UP_UPDATE_TIME = "tie_up_update_time";
		public const String FIELD_NAME_TIE_UP_DIRTY = "tie_up_dirty";
		public const String FIELD_NAME_TIE_UP_NAME = "tie_up_name";
		public const String FIELD_NAME_TIE_UP_RUBY = "tie_up_ruby";
		public const String FIELD_NAME_TIE_UP_KEYWORD = "tie_up_keyword";
		public const String FIELD_NAME_TIE_UP_CATEGORY_ID = "tie_up_category_id";
		public const String FIELD_NAME_TIE_UP_MAKER_ID = "tie_up_maker_id";
		public const String FIELD_NAME_TIE_UP_AGE_LIMIT = "tie_up_age_limit";
		public const String FIELD_NAME_TIE_UP_RELEASE_DATE = "tie_up_release_date";
	}
}
