// ============================================================================
// 
// 楽曲マスターテーブル
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

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

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
		[Column(FIELD_NAME_SONG_KEYWORD_RUBY_FOR_SEARCH)]
		public String? KeywordRubyForSearch { get; set; }

		// 同名の区別が付くように DisplayName を設定する
		[NotMapped]
		public Boolean AvoidSameName { get; set; }

		// 表示名
		public String? DisplayName
		{
			get => DbCommon.DisplayNameByDefaultAlgorithm(this);
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
		// TSong 独自項目
		// --------------------------------------------------------------------

		// タイアップ ID ＜参照項目＞
		[Column(FIELD_NAME_SONG_TIE_UP_ID)]
		public String? TieUpId { get; set; }

		// 摘要
		[Column(FIELD_NAME_SONG_OP_ED)]
		public String? OpEd { get; set; }

		// 表示タイアップ名（マスター一覧ウィンドウ用）
		private String? _displayTieUpName;
		public String? DisplayTieUpName
		{
			get
			{
				if (_displayTieUpName == null && TieUpId != null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					_displayTieUpName = DbCommon.SelectBaseById(musicInfoContextDefault.TieUps, TieUpId)?.Name;
				}
				return _displayTieUpName;
			}
		}

		// 表示歌手名（マスター一覧ウィンドウ用）
		private String? _displayArtistNames;
		public String? DisplayArtistNames
		{
			get
			{
				if (_displayArtistNames == null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					_displayArtistNames = DisplayPeopleNames(musicInfoContextDefault.ArtistSequences, musicInfoContextDefault.People);
				}
				return _displayArtistNames;
			}
		}

		// 表示作詞者名（マスター一覧ウィンドウ用）
		private String? _displayLyristNames;
		public String? DisplayLyristNames
		{
			get
			{
				if (_displayLyristNames == null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					_displayLyristNames = DisplayPeopleNames(musicInfoContextDefault.LyristSequences, musicInfoContextDefault.People);
				}
				return _displayLyristNames;
			}
		}

		// 表示作曲者名（マスター一覧ウィンドウ用）
		private String? _displayComposerNames;
		public String? DisplayComposerNames
		{
			get
			{
				if (_displayComposerNames == null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					_displayComposerNames = DisplayPeopleNames(musicInfoContextDefault.ComposerSequences, musicInfoContextDefault.People);
				}
				return _displayComposerNames;
			}
		}

		// 表示編曲者名（マスター一覧ウィンドウ用）
		private String? _displayArrangerNames;
		public String? DisplayArrangerNames
		{
			get
			{
				if (_displayArrangerNames == null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					_displayArrangerNames = DisplayPeopleNames(musicInfoContextDefault.ArrangerSequences, musicInfoContextDefault.People);
				}
				return _displayArrangerNames;
			}
		}

		// 表示タグ名（マスター一覧ウィンドウ用）
		private String? _displayTagNames;
		public String? DisplayTagNames
		{
			get
			{
				if (_displayTagNames == null)
				{
					using MusicInfoContextDefault musicInfoContextDefault = new();
					List<TTag> sequencedTags = DbCommon.SelectSequencedTagsBySongId(musicInfoContextDefault.TagSequences, musicInfoContextDefault.Tags, Id);
					_displayTagNames = String.Join(YlConstants.VAR_VALUE_DELIMITER, sequencedTags.Select(x => x.Name));
				}
				return _displayTagNames;
			}
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_SONG = "t_song";
		public const String FIELD_PREFIX_SONG = "song_";
		public const String FIELD_NAME_SONG_ID = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_ID;
		public const String FIELD_NAME_SONG_IMPORT = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_IMPORT;
		public const String FIELD_NAME_SONG_INVALID = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_INVALID;
		public const String FIELD_NAME_SONG_UPDATE_TIME = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_UPDATE_TIME;
		public const String FIELD_NAME_SONG_DIRTY = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_DIRTY;
		public const String FIELD_NAME_SONG_NAME = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_NAME;
		public const String FIELD_NAME_SONG_RUBY = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_RUBY;
		public const String FIELD_NAME_SONG_RUBY_FOR_SEARCH = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_RUBY_FOR_SEARCH;
		public const String FIELD_NAME_SONG_KEYWORD = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_KEYWORD;
		public const String FIELD_NAME_SONG_KEYWORD_RUBY_FOR_SEARCH = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH;
		public const String FIELD_NAME_SONG_CATEGORY_ID = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_CATEGORY_ID;
		public const String FIELD_NAME_SONG_RELEASE_DATE = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_RELEASE_DATE;
		public const String FIELD_NAME_SONG_TIE_UP_ID = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_TIE_UP_ID;
		public const String FIELD_NAME_SONG_OP_ED = FIELD_PREFIX_SONG + YlConstants.FIELD_SUFFIX_OP_ED;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 表示用人物名
		// --------------------------------------------------------------------
		private String DisplayPeopleNames<T>(DbSet<T> peopleSequences, DbSet<TPerson> people) where T : class, IRcSequence
		{
			List<TPerson> sequencedPeople = DbCommon.SelectSequencedPeopleBySongId(peopleSequences, people, Id);
			return String.Join(YlConstants.VAR_VALUE_DELIMITER, sequencedPeople.Select(x => x.Name));
		}
	}
}
