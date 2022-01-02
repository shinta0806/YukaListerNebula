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
			Debug.Assert(Reports != null, "Reports table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// リスト問題報告テーブル
		public DbSet<TReport> Reports { get; set; }

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
