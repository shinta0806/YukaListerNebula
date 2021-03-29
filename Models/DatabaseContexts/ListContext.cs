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

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;

namespace YukaLister.Models.DatabaseContexts
{
	public abstract class ListContext : YukaListerContext
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブル
		// --------------------------------------------------------------------

		// 検出ファイルリストテーブル
		public DbSet<TFound>? Founds { get; set; }

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// 人物マスターテーブル
		public DbSet<TPerson>? People { get; set; }

		// 歌手紐付テーブル
		public DbSet<TArtistSequence>? ArtistSequences { get; set; }

		// 作曲者紐付テーブル
		public DbSet<TComposerSequence>? ComposerSequences { get; set; }

		// タグマスターテーブル
		public DbSet<TTag>? Tags { get; set; }

		// タグ紐付テーブル
		public DbSet<TTagSequence>? TagSequences { get; set; }

		// ====================================================================
		// protected static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected static void GetDbSet(ListContext listContext, out DbSet<TFound> founds)
		{
			if (listContext.Founds == null)
			{
				throw new Exception("検出ファイルリストテーブルにアクセスできません。");
			}
			founds = listContext.Founds;
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// 検出ファイルリストテーブル
			modelBuilder.Entity<TFound>().HasIndex(x => x.Path);
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

			// タグ関連のテーブル
			modelBuilder.Entity<TTag>().HasIndex(x => x.Name);
		}
	}
}
