// ============================================================================
// 
// 楽曲情報データベース（通常用）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.IO;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class MusicInfoContextDefault : MusicInfoContext
	{
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
		public static MusicInfoContextDefault CreateContext(out DbSet<TProperty> properties,
				out DbSet<TSong> songs, out DbSet<TPerson> people, out DbSet<TTieUp> tieUps, out DbSet<TCategory> categories,
				out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TMaker> makers, out DbSet<TTag> tags,
				out DbSet<TSongAlias> songAliases, out DbSet<TPersonAlias> personAliases, out DbSet<TTieUpAlias> tieUpAliases,
				out DbSet<TCategoryAlias> categoryAliases, out DbSet<TTieUpGroupAlias> tieUpGroupAliases, out DbSet<TMakerAlias> makerAliases,
				out DbSet<TArtistSequence> artistSequences, out DbSet<TLyristSequence> lyristSequences, out DbSet<TComposerSequence> composerSequences, out DbSet<TArrangerSequence> arrangerSequences,
				out DbSet<TTieUpGroupSequence> tieUpGroupSequences, out DbSet<TTagSequence> tagSequences)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out properties,
					out songs, out people, out tieUps, out categories,
					out tieUpGroups, out makers, out tags,
					out songAliases, out personAliases, out tieUpAliases,
					out categoryAliases, out tieUpGroupAliases, out makerAliases,
					out artistSequences, out lyristSequences, out composerSequences, out arrangerSequences,
					out tieUpGroupSequences, out tagSequences);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TSong> songs)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out songs);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TTieUp> tieUps)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out tieUps);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TCategory> categories)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out categories);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TMaker> makers)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out makers);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TSongAlias> songAliases)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out songAliases);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TTieUpAlias> tieUpAliases)
		{
			MusicInfoContextDefault musicInfoContext = new();
			GetDbSet(musicInfoContext, out tieUpAliases);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextDefault CreateContext(out DbSet<TProperty> properties)
		{
			MusicInfoContextDefault musicInfoContext = new();
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
			using MusicInfoContextDefault musicInfoContext = CreateContext(out DbSet<TProperty> properties);
			musicInfoContext.Database.EnsureDeleted();

			// 新規作成
			musicInfoContext.Database.EnsureCreated();
			InsertCategoryDefaultRecords(musicInfoContext);
			DbCommon.UpdateProperty(musicInfoContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベースを初期化しました。");
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public static void CreateDatabaseIfNeeded()
		{
			using MusicInfoContextDefault musicInfoContext = CreateContext(out DbSet<TProperty> properties);
			if (DbCommon.ValidPropertyExists(properties))
			{
				// 既存のデータベースがある場合はクリアしない
				return;
			}
			CreateDatabase();
		}

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC
		// --------------------------------------------------------------------
		public static DateTime LastWriteTime()
		{
			return new FileInfo(DatabasePath()).LastWriteTimeUtc;
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
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// カテゴリーテーブルのレコードを作成
		// --------------------------------------------------------------------
		private static TCategory CreateCategoryRecord(Int32 idNumber, String name, String? ruby = null)
		{
			String? normalizedName = YlCommon.NormalizeDbString(name);
			(String? normalizedRubyForMusicInfo, _, _) = YlCommon.NormalizeDbRubyForMusicInfo(ruby);
			if (String.IsNullOrEmpty(normalizedRubyForMusicInfo))
			{
				normalizedRubyForMusicInfo = normalizedName;
			}
			(String? normalizedRubyForSearch, _, _) = YlCommon.NormalizeDbRubyForSearch(normalizedRubyForMusicInfo);

			return new TCategory()
			{
				// IRcBase
				Id = YlConstants.MUSIC_INFO_SYSTEM_ID_PREFIX + YlConstants.MUSIC_INFO_ID_SECOND_PREFIXES[(Int32)MusicInfoTables.TCategory] + idNumber.ToString("D3"),
				Import = false,
				Invalid = false,
				UpdateTime = YlConstants.INVALID_MJD,
				Dirty = true,

				// IRcMaster
				Name = normalizedName,
				Ruby = normalizedRubyForMusicInfo,
				RubyForSearch = normalizedRubyForSearch,
				Keyword = null,
				KeywordRubyForSearch = null,
			};
		}

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		private static String DatabasePath()
		{
			return DbCommon.YukaListerDatabaseFullFolder() + FILE_NAME_MUSIC_INFO_DATABASE;
		}

		// --------------------------------------------------------------------
		// カテゴリーマスターテーブルの既定レコードを挿入
		// ニコニコ動画のカテゴリータグおよび anison.info のカテゴリーから主要な物を抽出
		// --------------------------------------------------------------------
		private static void InsertCategoryDefaultRecords(MusicInfoContextDefault musicInfoContext)
		{
			MusicInfoContextDefault.GetDbSet(musicInfoContext, out DbSet<TCategory> categories);

			// 主にタイアップ用
			categories.Add(CreateCategoryRecord(1, "アニメ"));
			categories.Add(CreateCategoryRecord(2, "イベント/舞台/公演", "イベントブタイコウエン"));
			categories.Add(CreateCategoryRecord(3, "ゲーム"));
			categories.Add(CreateCategoryRecord(4, "時代劇", "ジダイゲキ"));
			categories.Add(CreateCategoryRecord(5, "特撮", "トクサツ"));
			categories.Add(CreateCategoryRecord(6, "ドラマ"));
			categories.Add(CreateCategoryRecord(7, "ラジオ"));

			// 主にタイアップの無い楽曲用
			categories.Add(CreateCategoryRecord(101, "VOCALOID", "ボーカロイド"));
			// 102 は欠番（旧：歌ってみた）
			categories.Add(CreateCategoryRecord(103, "一般", "イッパン"));

			musicInfoContext.SaveChanges();
		}
	}
}
