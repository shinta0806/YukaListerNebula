// ============================================================================
// 
// リストデータベースのコンテキストの基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;

namespace YukaLister.Models.DatabaseContexts
{
	public abstract class ListContext : YukaListerContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ListContext(String databaseName)
				: base(databaseName)
		{
			Debug.Assert(Founds != null, "Founds table not init");
			Debug.Assert(People != null, "People table not init");
			Debug.Assert(ArtistSequences != null, "ArtistSequences table not init");
			Debug.Assert(ComposerSequences != null, "ComposerSequences table not init");
			Debug.Assert(TieUpGroups != null, "TieUpGroups table not init");
			Debug.Assert(TieUpGroupSequences != null, "TieUpGroupSequences table not init");
			Debug.Assert(Tags != null, "Tags table not init");
			Debug.Assert(TagSequences != null, "TagSequences table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブル
		// --------------------------------------------------------------------

		// 検出ファイルリストテーブル
		public DbSet<TFound> Founds { get; set; }

		// --------------------------------------------------------------------
		// その他（楽曲情報データベース＋楽曲情報データベースにない情報を名寄せするためのレコード）
		// --------------------------------------------------------------------

		// 人物マスターテーブル
		public DbSet<TPerson> People { get; set; }

		// 歌手紐付テーブル
		public DbSet<TArtistSequence> ArtistSequences { get; set; }

		// 作曲者紐付テーブル
		public DbSet<TComposerSequence> ComposerSequences { get; set; }

		// タイアップグループマスターテーブル
		public DbSet<TTieUpGroup> TieUpGroups { get; set; }

		// タイアップグループ紐付テーブル
		public DbSet<TTieUpGroupSequence> TieUpGroupSequences { get; set; }

		// タグマスターテーブル
		public DbSet<TTag> Tags { get; set; }

		// タグ紐付テーブル
		public DbSet<TTagSequence> TagSequences { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TFound> founds)
		{
			if (listContext.Founds == null)
			{
				throw new Exception("検出ファイルリストテーブルにアクセスできません。");
			}
			founds = listContext.Founds;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TPerson> people)
		{
			if (listContext.People == null)
			{
				throw new Exception("人物マスターテーブルにアクセスできません。");
			}
			people = listContext.People;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TArtistSequence> artistSequences)
		{
			if (listContext.ArtistSequences == null)
			{
				throw new Exception("歌手紐付テーブルにアクセスできません。");
			}
			artistSequences = listContext.ArtistSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TComposerSequence> composerSequences)
		{
			if (listContext.ComposerSequences == null)
			{
				throw new Exception("作曲者紐付テーブルにアクセスできません。");
			}
			composerSequences = listContext.ComposerSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TTieUpGroup> tieUpGroups)
		{
			if (listContext.TieUpGroups == null)
			{
				throw new Exception("タイアップグループマスターテーブルにアクセスできません。");
			}
			tieUpGroups = listContext.TieUpGroups;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TTieUpGroupSequence> tieUpGroupSequences)
		{
			if (listContext.TieUpGroupSequences == null)
			{
				throw new Exception("タイアップグループ紐付テーブルにアクセスできません。");
			}
			tieUpGroupSequences = listContext.TieUpGroupSequences;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TTag> tags)
		{
			if (listContext.Tags == null)
			{
				throw new Exception("タグマスターテーブルにアクセスできません。");
			}
			tags = listContext.Tags;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ListContext listContext, out DbSet<TTagSequence> tagSequences)
		{
			if (listContext.TagSequences == null)
			{
				throw new Exception("タグ紐付テーブルにアクセスできません。");
			}
			tagSequences = listContext.TagSequences;
		}
#endif

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// 検出ファイルリストテーブル
			// TieUpAgeLimit はインデックス化しないほうが HTML リスト作成が高速だった
			modelBuilder.Entity<TFound>().HasIndex(x => x.Path);
			modelBuilder.Entity<TFound>().HasIndex(x => x.Folder);
			modelBuilder.Entity<TFound>().HasIndex(x => x.ParentFolder);
			modelBuilder.Entity<TFound>().HasIndex(x => x.Head);
			modelBuilder.Entity<TFound>().HasIndex(x => x.LastWriteTime);
			modelBuilder.Entity<TFound>().HasIndex(x => x.SongName);
			modelBuilder.Entity<TFound>().HasIndex(x => x.SongRuby);
			modelBuilder.Entity<TFound>().HasIndex(x => x.SongReleaseDate);
			modelBuilder.Entity<TFound>().HasIndex(x => x.TieUpName);
			modelBuilder.Entity<TFound>().HasIndex(x => x.TieUpRuby);
			modelBuilder.Entity<TFound>().HasIndex(x => x.Category);

			// 人物関連のテーブル
			modelBuilder.Entity<TPerson>().HasIndex(x => x.Name);

			// タイアップグループ関連のテーブル
			modelBuilder.Entity<TTieUpGroup>().HasIndex(x => x.Name);

			// タグ関連のテーブル
			modelBuilder.Entity<TTag>().HasIndex(x => x.Name);

			// 紐付テーブル
			modelBuilder.Entity<TArtistSequence>().HasKey(x => new { x.Id, x.Sequence });
			modelBuilder.Entity<TComposerSequence>().HasKey(x => new { x.Id, x.Sequence });
			modelBuilder.Entity<TTieUpGroupSequence>().HasKey(x => new { x.Id, x.Sequence });
			modelBuilder.Entity<TTagSequence>().HasKey(x => new { x.Id, x.Sequence });
		}
	}
}
