// ============================================================================
// 
// リスト問題報告データベースのコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class ReportContext : YukaListerContext
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// リスト問題報告テーブル
		public DbSet<TReport>? Reports { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ReportContext CreateContext(out DbSet<TReport> reports)
		{
			ReportContext reportContext = new();
			GetDbSet(reportContext, out reports);
			return reportContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ReportContext CreateContext(out DbSet<TProperty> properties)
		{
			ReportContext reportContext = new();
			GetDbSet(reportContext, out properties);
			return reportContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "リスト問題報告データベース初期化中...");

			// クリア
			using ReportContext reportContext = CreateContext(out DbSet<TProperty> properties);
			reportContext.Database.EnsureDeleted();

			// 新規作成
			reportContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(reportContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "リスト問題報告データベースを初期化しました。");
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public static void CreateDatabaseIfNeeded()
		{
			using ReportContext reportContext = CreateContext(out DbSet<TProperty> properties);
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
		public static void GetDbSet(ReportContext reportContext, out DbSet<TReport> reports)
		{
			if (reportContext.Reports == null)
			{
				throw new Exception("リスト問題報告テーブルにアクセスできません。");
			}
			reports = reportContext.Reports;
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(DbCommon.Connect(DbCommon.ReportDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings)));
		}

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// サムネイルキャッシュテーブル
			modelBuilder.Entity<TReport>().HasIndex(x => x.RegistTime);
			modelBuilder.Entity<TReport>().HasIndex(x => x.Status);
		}
	}
}
