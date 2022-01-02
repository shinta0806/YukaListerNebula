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
using System.Diagnostics;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;

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
			Debug.Assert(YukariStatistics != null, "YukariStatistics table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ゆかり統計テーブル
		public DbSet<TYukariStatistics> YukariStatistics { get; set; }

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
