// ============================================================================
// 
// ゆかり統計データベースのコンテキスト
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
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class YukariStatisticsContext : YukaListerContext
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ゆかり統計テーブル
		public DbSet<TYukariStatistics>? YukariStatistics { get; set; }

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
		public static YukariStatisticsContext CreateContext(out DbSet<TProperty> properties, out DbSet<TYukariStatistics> yukariStatistics)
		{
			YukariStatisticsContext yukariStatisticsContext = new();
			GetDbSet(yukariStatisticsContext, out properties);
			GetDbSet(yukariStatisticsContext, out yukariStatistics);
			return yukariStatisticsContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static YukariStatisticsContext CreateContext(out DbSet<TYukariStatistics> yukariStatistics)
		{
			YukariStatisticsContext yukariStatisticsContext = new();
			GetDbSet(yukariStatisticsContext, out yukariStatistics);
			return yukariStatisticsContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static YukariStatisticsContext CreateContext(out DbSet<TProperty> properties)
		{
			YukariStatisticsContext yukariStatisticsContext = new();
			GetDbSet(yukariStatisticsContext, out properties);
			return yukariStatisticsContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり統計データベース初期化中...");

			// クリア
			using YukariStatisticsContext yukariStatisticsContext = CreateContext(out DbSet<TProperty> properties);
			yukariStatisticsContext.Database.EnsureDeleted();

			// 新規作成
			yukariStatisticsContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(yukariStatisticsContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり統計データベースを初期化しました。");
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public static void CreateDatabaseIfNeeded()
		{
			using YukariStatisticsContext yukariStatisticsContext = CreateContext(out DbSet<TProperty> properties);
			if (DbCommon.ValidPropertyExists(properties))
			{
				// 既存のデータベースがある場合はクリアしない
				return;
			}
			CreateDatabase();
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(YukariStatisticsContext yukariStatisticsContext, out DbSet<TYukariStatistics> yukariStatistics)
		{
			if (yukariStatisticsContext.YukariStatistics == null)
			{
				throw new Exception("ゆかり統計テーブルにアクセスできません。");
			}
			yukariStatistics = yukariStatisticsContext.YukariStatistics;
		}

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC（修正ユリウス日）
		// --------------------------------------------------------------------
		public static Double LastWriteTime()
		{
			return JulianDay.DateTimeToModifiedJulianDate(new FileInfo(DatabasePath()).LastWriteTimeUtc);
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
			modelBuilder.Entity<TYukariStatistics>().HasIndex(x => x.RequestDatabasePath);
			modelBuilder.Entity<TYukariStatistics>().HasIndex(x => x.RequestTime);
			modelBuilder.Entity<TYukariStatistics>().HasIndex(x => x.RequestId);
			modelBuilder.Entity<TYukariStatistics>().HasIndex(x => x.RequestMoviePath);
			modelBuilder.Entity<TYukariStatistics>().HasIndex(x => x.RequestSinger);
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_YUKARI_STATISTICS_DATABASE = "YukariStatistics" + Common.FILE_EXT_SQLITE3;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		private static String DatabasePath()
		{
			return DbCommon.YukaListerDatabaseFullFolder() + FILE_NAME_YUKARI_STATISTICS_DATABASE;
		}
	}
}
