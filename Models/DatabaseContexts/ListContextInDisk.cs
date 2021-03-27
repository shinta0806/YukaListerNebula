﻿// ============================================================================
// 
// リストデータベース（ディスク）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shinta;
using System;
using System.Diagnostics;
using System.IO;
using YukaLister.Models.Database;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class ListContextInDisk : ListContext
	{
		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ListContextInDisk CreateContext(out DbSet<TProperty> properties)
		{
			ListContextInDisk listContext = new();
			GetDbSet(listContext, out properties);
			return listContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "リストデータベースを準備しています...");

			// クリア
			using ListContextInDisk listContext = CreateContext(out DbSet<TProperty> properties);
			listContext.Database.EnsureDeleted();

			// 新規作成
			listContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(listContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "リストデータベースを作成しました。");
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			Debug.WriteLine("OnConfiguring() DatabasePath: " + DatabasePath());
			optionsBuilder.UseSqlite(DbCommon.Connect(DatabasePath()));
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_LIST_DATABASE_IN_DISK = "List" + Common.FILE_EXT_SQLITE3;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		private static String DatabasePath()
		{
			return DbCommon.YukariDatabaseFullFolder() + FILE_NAME_LIST_DATABASE_IN_DISK;
		}


	}
}
