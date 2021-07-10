// ============================================================================
// 
// リスト問題報告データベースのコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class ReportContext : YukaListerContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ReportContext()
				: base("リスト問題報告")
		{
		}

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
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.ReportDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<TReport>().HasIndex(x => x.RegistTime);
			modelBuilder.Entity<TReport>().HasIndex(x => x.Status);
		}
	}
}
