// ============================================================================
// 
// 楽曲情報データベースのコンテキスト基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;

namespace YukaLister.Models.DatabaseContexts
{
	public abstract class MusicInfoContext : YukaListerContext
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// マスターテーブル
		// --------------------------------------------------------------------

		// 楽曲マスターテーブル
		public DbSet<TSong>? Songs { get; set; }

		// 人物マスターテーブル
		public DbSet<TPerson>? People { get; set; }

		// タイアップマスターテーブル
		public DbSet<TTieUp>? TieUps { get; set; }

		// カテゴリーマスターテーブル
		public DbSet<TCategory>? Categories { get; set; }

		// タイアップグループマスターテーブル
		public DbSet<TTieUpGroup>? TieUpGroups { get; set; }

		// 制作会社マスターテーブル
		public DbSet<TMaker>? Makers { get; set; }

		// タグマスターテーブル
		public DbSet<TTag>? Tags { get; set; }

		// --------------------------------------------------------------------
		// 別名テーブル
		// --------------------------------------------------------------------

		// 楽曲別名テーブル
		public DbSet<TSongAlias>? SongAliases { get; set; }

		// 人物別名テーブル
		public DbSet<TPersonAlias>? PersonAliases { get; set; }

		// タイアップ別名テーブル
		public DbSet<TTieUpAlias>? TieUpAliases { get; set; }

		// カテゴリー別名テーブル
		public DbSet<TCategoryAlias>? CategoryAliases { get; set; }

		// タイアップグループ別名テーブル
		public DbSet<TTieUpGroupAlias>? TieUpGroupAliases { get; set; }

		// 制作会社別名テーブル
		public DbSet<TMakerAlias>? MakerAliases { get; set; }

		// --------------------------------------------------------------------
		// 紐付テーブル
		// --------------------------------------------------------------------

		// 歌手紐付テーブル
		public DbSet<TArtistSequence>? ArtistSequences { get; set; }

		// 作詞者紐付テーブル
		public DbSet<TLyristSequence>? LyristSequences { get; set; }

		// 作曲者紐付テーブル
		public DbSet<TComposerSequence>? ComposerSequences { get; set; }

		// 編曲者紐付テーブル
		public DbSet<TArrangerSequence>? ArrangerSequences { get; set; }

		// タイアップグループ紐付テーブル
		public DbSet<TTieUpGroupSequence>? TieUpGroupSequences { get; set; }

		// タグ紐付テーブル
		public DbSet<TTagSequence>? TagSequences { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TProperty> properties,
				out DbSet<TSong> songs, out DbSet<TPerson> people, out DbSet<TTieUp> tieUps, out DbSet<TCategory> categories,
				out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TMaker> makers, out DbSet<TTag> tags,
				out DbSet<TSongAlias> songAliases, out DbSet<TPersonAlias> personAliases, out DbSet<TTieUpAlias> tieUpAliases,
				out DbSet<TCategoryAlias> categoryAliases, out DbSet<TTieUpGroupAlias> tieUpGroupAliases, out DbSet<TMakerAlias> makerAliases,
				out DbSet<TArtistSequence> artistSequences, out DbSet<TLyristSequence> lyristSequences, out DbSet<TComposerSequence> composerSequences, out DbSet<TArrangerSequence> arrangerSequences,
				out DbSet<TTieUpGroupSequence> tieUpGroupSequences, out DbSet<TTagSequence> tagSequences)
		{
			// データベースプロパティーテーブル
			GetDbSet(musicInfoContext, out properties);

			// マスターテーブル
			GetDbSet(musicInfoContext, out songs);
			GetDbSet(musicInfoContext, out people);
			GetDbSet(musicInfoContext, out tieUps);
			GetDbSet(musicInfoContext, out categories);
			GetDbSet(musicInfoContext, out tieUpGroups);
			GetDbSet(musicInfoContext, out makers);
			GetDbSet(musicInfoContext, out tags);

			// 別名テーブル
			GetDbSet(musicInfoContext, out songAliases);
			GetDbSet(musicInfoContext, out personAliases);
			GetDbSet(musicInfoContext, out tieUpAliases);
			GetDbSet(musicInfoContext, out categoryAliases);
			GetDbSet(musicInfoContext, out tieUpGroupAliases);
			GetDbSet(musicInfoContext, out makerAliases);

			// 紐付テーブル
			GetDbSet(musicInfoContext, out artistSequences);
			GetDbSet(musicInfoContext, out lyristSequences);
			GetDbSet(musicInfoContext, out composerSequences);
			GetDbSet(musicInfoContext, out arrangerSequences);
			GetDbSet(musicInfoContext, out tieUpGroupSequences);
			GetDbSet(musicInfoContext, out tagSequences);
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TSong> songs)
		{
			if (musicInfoContext.Songs == null)
			{
				throw new Exception("楽曲マスターテーブルにアクセスできません。");
			}
			songs = musicInfoContext.Songs;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TPerson> people)
		{
			if (musicInfoContext.People == null)
			{
				throw new Exception("人物マスターテーブルにアクセスできません。");
			}
			people = musicInfoContext.People;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUp> tieUps)
		{
			if (musicInfoContext.TieUps == null)
			{
				throw new Exception("タイアップマスターテーブルにアクセスできません。");
			}
			tieUps = musicInfoContext.TieUps;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TCategory> categories)
		{
			if (musicInfoContext.Categories == null)
			{
				throw new Exception("カテゴリーマスターテーブルにアクセスできません。");
			}
			categories = musicInfoContext.Categories;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpGroup> tieUpGroups)
		{
			if (musicInfoContext.TieUpGroups == null)
			{
				throw new Exception("タイアップグループマスターテーブルにアクセスできません。");
			}
			tieUpGroups = musicInfoContext.TieUpGroups;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TMaker> makers)
		{
			if (musicInfoContext.Makers == null)
			{
				throw new Exception("制作会社マスターテーブルにアクセスできません。");
			}
			makers = musicInfoContext.Makers;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTag> tags)
		{
			if (musicInfoContext.Tags == null)
			{
				throw new Exception("タグマスターテーブルにアクセスできません。");
			}
			tags = musicInfoContext.Tags;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TSongAlias> songAliases)
		{
			if (musicInfoContext.SongAliases == null)
			{
				throw new Exception("楽曲別名テーブルにアクセスできません。");
			}
			songAliases = musicInfoContext.SongAliases;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TPersonAlias> personAliases)
		{
			if (musicInfoContext.PersonAliases == null)
			{
				throw new Exception("人物別名テーブルにアクセスできません。");
			}
			personAliases = musicInfoContext.PersonAliases;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpAlias> tieUpAliases)
		{
			if (musicInfoContext.TieUpAliases == null)
			{
				throw new Exception("タイアップ別名テーブルにアクセスできません。");
			}
			tieUpAliases = musicInfoContext.TieUpAliases;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TCategoryAlias> categoryAliases)
		{
			if (musicInfoContext.CategoryAliases == null)
			{
				throw new Exception("カテゴリー別名テーブルにアクセスできません。");
			}
			categoryAliases = musicInfoContext.CategoryAliases;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpGroupAlias> tieUpGroupAliases)
		{
			if (musicInfoContext.TieUpGroupAliases == null)
			{
				throw new Exception("タイアップグループ別名テーブルにアクセスできません。");
			}
			tieUpGroupAliases = musicInfoContext.TieUpGroupAliases;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TMakerAlias> makerAliases)
		{
			if (musicInfoContext.MakerAliases == null)
			{
				throw new Exception("制作会社別名テーブルにアクセスできません。");
			}
			makerAliases = musicInfoContext.MakerAliases;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TArtistSequence> artistSequences)
		{
			if (musicInfoContext.ArtistSequences == null)
			{
				throw new Exception("歌手紐付テーブルにアクセスできません。");
			}
			artistSequences = musicInfoContext.ArtistSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TLyristSequence> lyristSequences)
		{
			if (musicInfoContext.LyristSequences == null)
			{
				throw new Exception("作詞者紐付テーブルにアクセスできません。");
			}
			lyristSequences = musicInfoContext.LyristSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TComposerSequence> composerSequences)
		{
			if (musicInfoContext.ComposerSequences == null)
			{
				throw new Exception("作曲者紐付テーブルにアクセスできません。");
			}
			composerSequences = musicInfoContext.ComposerSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TArrangerSequence> arrangerSequences)
		{
			if (musicInfoContext.ArrangerSequences == null)
			{
				throw new Exception("編曲者紐付テーブルにアクセスできません。");
			}
			arrangerSequences = musicInfoContext.ArrangerSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpGroupSequence> tieUpGroupSequences)
		{
			if (musicInfoContext.TieUpGroupSequences == null)
			{
				throw new Exception("タイアップグループ紐付テーブルにアクセスできません。");
			}
			tieUpGroupSequences = musicInfoContext.TieUpGroupSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTagSequence> tagSequences)
		{
			if (musicInfoContext.TagSequences == null)
			{
				throw new Exception("タグ紐付テーブルにアクセスできません。");
			}
			tagSequences = musicInfoContext.TagSequences;
		}

		// ====================================================================
		// protected メンバー定数
		// ====================================================================

		// データベースファイル名
		protected const String FILE_NAME_MUSIC_INFO_DATABASE = "NebulaMusicInfo" + Common.FILE_EXT_SQLITE3;
	}
}
