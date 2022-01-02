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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

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

		// タイアップフリガナ（検索用）
		[Column(FIELD_NAME_TIE_UP_RUBY_FOR_SEARCH)]
		public String? RubyForSearch { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_TIE_UP_KEYWORD)]
		public String? Keyword { get; set; }

		// 検索ワードフリガナ（検索用）
		// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみを格納
		[Column(FIELD_NAME_TIE_UP_KEYWORD_RUBY_FOR_SEARCH)]
		public String? KeywordRubyForSearch { get; set; }

		// 同名の区別が付くように DisplayName を設定する
		[NotMapped]
		public Boolean AvoidSameName { get; set; }

		// 表示名
		public String? DisplayName
		{
			get
			{
				if (AvoidSameName)
				{
					TCategory? category;
					using MusicInfoContextDefault musicInfoContextDefault = new();
					musicInfoContextDefault.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
					category = DbCommon.SelectBaseById(musicInfoContextDefault.Categories, CategoryId);
					return Name + "（" + (String.IsNullOrEmpty(category?.Name) ? "カテゴリー無し" : category?.Name) + ", "
							+ (String.IsNullOrEmpty(Keyword) ? "キーワード無し" : Keyword) + "）";
				}
				else
				{
					return Name;
				}
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

		// 表示カテゴリー名（マスター一覧ウィンドウ用）
		private String? _displayCategoryName;
		public String? DisplayCategoryName
		{
			get
			{
				_displayCategoryName = DbCommon.DisplayCategoryNameByDefaultAlgorithm(_displayCategoryName, CategoryId);
				return _displayCategoryName;
			}
		}

		// 表示リリース日（マスター一覧ウィンドウ用）
		public String? DisplayReleaseDate
		{
			get => DbCommon.DisplayReleaseDateByDefaultAlgorithm(this);
		}

		// --------------------------------------------------------------------
		// TTieUp 独自項目
		// --------------------------------------------------------------------

		// 制作会社 ID ＜参照項目＞
		[Column(FIELD_NAME_TIE_UP_MAKER_ID)]
		public String? MakerId { get; set; }

		// 年齢制限（○歳以上対象）
		[Column(FIELD_NAME_TIE_UP_AGE_LIMIT)]
		public Int32 AgeLimit { get; set; }

		// 表示制作会社名（マスター一覧ウィンドウ用）
		private String? _displayMakerName;
		public String? DisplayMakerName
		{
			get
			{
				if (_displayMakerName == null && MakerId != null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					_displayMakerName = DbCommon.SelectBaseById(musicInfoContextDefault.Makers, MakerId)?.Name;
				}
				return _displayMakerName;
			}
		}

		// 表示年齢制限（マスター一覧ウィンドウ用）
		public String? DisplayAgeLimit
		{
			get
			{
				if (AgeLimit == 0)
				{
					return null;
				}
				else
				{
					return AgeLimit.ToString();
				}
			}
		}

		// 表示シリーズ（マスター一覧ウィンドウ用）
		private String? _displayTieUpGroupNames;
		public String? DisplayTieUpGroupNames
		{
			get
			{
				if (_displayTieUpGroupNames == null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					List<TTieUpGroup> sequencedTieUpGroups = DbCommon.SelectSequencedTieUpGroupsByTieUpId(musicInfoContextDefault.TieUpGroupSequences, musicInfoContextDefault.TieUpGroups, Id);
					_displayTieUpGroupNames = String.Join(YlConstants.VAR_VALUE_DELIMITER, sequencedTieUpGroups.Select(x => x.Name));
				}
				return _displayTieUpGroupNames;
			}
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_TIE_UP = "t_tie_up";
		public const String FIELD_PREFIX_TIE_UP = "tie_up_";
		public const String FIELD_NAME_TIE_UP_ID = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_ID;
		public const String FIELD_NAME_TIE_UP_IMPORT = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_IMPORT;
		public const String FIELD_NAME_TIE_UP_INVALID = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_INVALID;
		public const String FIELD_NAME_TIE_UP_UPDATE_TIME = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_UPDATE_TIME;
		public const String FIELD_NAME_TIE_UP_DIRTY = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_DIRTY;
		public const String FIELD_NAME_TIE_UP_NAME = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_NAME;
		public const String FIELD_NAME_TIE_UP_RUBY = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_RUBY;
		public const String FIELD_NAME_TIE_UP_RUBY_FOR_SEARCH = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_RUBY_FOR_SEARCH;
		public const String FIELD_NAME_TIE_UP_KEYWORD = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_KEYWORD;
		public const String FIELD_NAME_TIE_UP_KEYWORD_RUBY_FOR_SEARCH = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH;
		public const String FIELD_NAME_TIE_UP_CATEGORY_ID = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_CATEGORY_ID;
		public const String FIELD_NAME_TIE_UP_RELEASE_DATE = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_RELEASE_DATE;
		public const String FIELD_NAME_TIE_UP_MAKER_ID = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_MAKER_ID;
		public const String FIELD_NAME_TIE_UP_AGE_LIMIT = FIELD_PREFIX_TIE_UP + YlConstants.FIELD_SUFFIX_AGE_LIMIT;
	}
}
