// ============================================================================
// 
// 楽曲情報データベースのコンテキスト
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
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class MusicInfoContext : YukaListerContext
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
		public static MusicInfoContext CreateContext(out DbSet<TProperty> properties,
				out DbSet<TSong> songs, out DbSet<TPerson> people, out DbSet<TTieUp> tieUps, out DbSet<TCategory> categories,
				out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TMaker> makers, out DbSet<TTag> tags,
				out DbSet<TSongAlias> songAliases, out DbSet<TPersonAlias> personAliases, out DbSet<TTieUpAlias> tieUpAliases,
				out DbSet<TCategoryAlias> categoryAliases, out DbSet<TTieUpGroupAlias> tieUpGroupAliases, out DbSet<TMakerAlias> makerAliases,
				out DbSet<TArtistSequence> artistSequences, out DbSet<TLyristSequence> lyristSequences, out DbSet<TComposerSequence> composerSequences, out DbSet<TArrangerSequence> arrangerSequences,
				out DbSet<TTieUpGroupSequence> tieUpGroupSequences, out DbSet<TTagSequence> tagSequences)
		{
			MusicInfoContext musicInfoContext = new();

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

			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TSong> songs)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out songs);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TTieUp> tieUps)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out tieUps);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TCategory> categories)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out categories);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TMaker> makers)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out makers);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TSongAlias> songAliases)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out songAliases);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TTieUpAlias> tieUpAliases)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out tieUpAliases);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContext CreateContext(out DbSet<TProperty> properties)
		{
			MusicInfoContext musicInfoContext = new();
			GetDbSet(musicInfoContext, out properties);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			BackupDatabase();
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベース初期化中...");

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

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベースを初期化しました。");
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
			modelBuilder.Entity<TSongAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TPersonAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TTieUpAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TCategoryAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TTieUpGroupAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TMakerAlias>().HasIndex(x => x.Alias).IsUnique();

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
		private const String FILE_NAME_MUSIC_INFO_DATABASE = "NebulaMusicInfo" + Common.FILE_EXT_SQLITE3;

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

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TSong> songs)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TPerson> people)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUp> tieUps)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TCategory> categories)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpGroup> tieUpGroups)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TMaker> makers)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTag> tags)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TSongAlias> songAliases)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TPersonAlias> personAliases)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpAlias> tieUpAliases)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TCategoryAlias> categoryAliases)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpGroupAlias> tieUpGroupAliases)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TMakerAlias> makerAliases)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TArtistSequence> artistSequences)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TLyristSequence> lyristSequences)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TComposerSequence> composerSequences)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TArrangerSequence> arrangerSequences)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTieUpGroupSequence> tieUpGroupSequences)
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
		private static void GetDbSet(MusicInfoContext musicInfoContext, out DbSet<TTagSequence> tagSequences)
		{
			if (musicInfoContext.TagSequences == null)
			{
				throw new Exception("タグ紐付テーブルにアクセスできません。");
			}
			tagSequences = musicInfoContext.TagSequences;
		}
	}
}
