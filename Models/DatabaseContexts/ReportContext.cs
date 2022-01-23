﻿// ============================================================================
// 
// リスト問題報告データベースのコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	internal class ReportContext : YlContext
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public ReportContext()
				: base("リスト問題報告")
		{
			Debug.Assert(Reports != null, "Reports table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// リスト問題報告テーブル
		public DbSet<TReport> Reports { get; set; }

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.ReportDatabasePath(YlModel.Instance.EnvModel.YlSettings);
		}

		// ====================================================================
		// protected 関数
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
