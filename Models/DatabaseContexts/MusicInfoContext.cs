// ============================================================================
// 
// 楽曲情報データベースのコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Data.Common;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class MusicInfoContext : DbContext
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// データベースプロパティーテーブル
		public DbSet<TProperty>? Properties { get; set; }

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
		// データベースファイルのバックアップを作成
		// --------------------------------------------------------------------
		public static void BackupDatabase()
		{
			DbCommon.BackupDatabase(DatabasePath());
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TCategory> categories)
		{
			MusicInfoContext musicInfoContext = new();

			if (musicInfoContext.Categories == null)
			{
				throw new Exception("カテゴリーマスターテーブルにアクセスできません。");
			}
			categories = musicInfoContext.Categories;

			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TProperty> properties)
		{
			MusicInfoContext musicInfoContext = new();

			if (musicInfoContext.Properties == null)
			{
				throw new Exception("データベースプロパティーテーブルにアクセスできません。");
			}
			properties = musicInfoContext.Properties;

			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			BackupDatabase();
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベースを準備しています...");

			// クリア
			using MusicInfoContext musicInfoContext = CreateContext(out DbSet<TProperty> properties);
			musicInfoContext.Database.EnsureDeleted();
#if DEBUGz
			DbConnection connection = musicInfoContext.Database.GetDbConnection();
			SqliteConnection? sqliteConnection = connection as SqliteConnection;
#endif

			// 新規作成
			musicInfoContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(musicInfoContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベースを作成しました。");
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public static void CreateDatabaseIfNeeded()
		{
			using MusicInfoContext musicInfoContext = CreateContext(out DbSet<TProperty> properties);
			if (DbCommon.ValidPropertyExists(properties))
			{
				// 既存のデータベースがある場合はクリアしない
				return;
			}
			CreateDatabase();
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(DbCommon.Connect(DatabasePath()));
		}

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// マスターテーブル
			modelBuilder.Entity<TSong>().HasIndex(x => x.Name);
			modelBuilder.Entity<TSong>().HasIndex(x => x.CategoryId);
			modelBuilder.Entity<TSong>().HasIndex(x => x.OpEd);

			modelBuilder.Entity<TPerson>().HasIndex(x => x.Name);

			modelBuilder.Entity<TTieUp>().HasIndex(x => x.Name);
			modelBuilder.Entity<TTieUp>().HasIndex(x => x.CategoryId);

			modelBuilder.Entity<TCategory>().HasIndex(x => x.Name);
			// ToDo
			//InsertCategoryDefaultRecords();

			modelBuilder.Entity<TTieUpGroup>().HasIndex(x => x.Name);

			modelBuilder.Entity<TMaker>().HasIndex(x => x.Name);

			modelBuilder.Entity<TTag>().HasIndex(x => x.Name);

			// 別名テーブル
			modelBuilder.Entity<TSongAlias>().HasIndex(x => x.Alias);

			modelBuilder.Entity<TPersonAlias>().HasIndex(x => x.Alias);

			modelBuilder.Entity<TTieUpAlias>().HasIndex(x => x.Alias);

			modelBuilder.Entity<TCategoryAlias>().HasIndex(x => x.Alias);

			modelBuilder.Entity<TTieUpGroupAlias>().HasIndex(x => x.Alias);

			modelBuilder.Entity<TMakerAlias>().HasIndex(x => x.Alias);

			// 紐付テーブル
			modelBuilder.Entity<TArtistSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TLyristSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TComposerSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TArrangerSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TTieUpGroupSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TTagSequence>().HasKey(x => new { x.Id, x.Sequence });
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_MUSIC_INFO_DATABASE = "MusicInfo" + Common.FILE_EXT_SQLITE3;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		private static String DatabasePath()
		{
			return DbCommon.YukaListerDatabaseFullFolder() + FILE_NAME_MUSIC_INFO_DATABASE;
		}
	}
}
