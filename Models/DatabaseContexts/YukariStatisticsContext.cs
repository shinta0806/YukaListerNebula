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
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public YukariStatisticsContext()
				: base("ゆかり統計")
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ゆかり統計テーブル
		public DbSet<TYukariStatistics>? YukariStatistics { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

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

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.YukaListerDatabaseFullFolder() + FILE_NAME_YUKARI_STATISTICS_DATABASE;
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

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
	}
}
