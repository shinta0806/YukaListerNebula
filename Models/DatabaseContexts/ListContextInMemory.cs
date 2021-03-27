// ============================================================================
// 
// リストデータベース（メモリ）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shinta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.Database;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class ListContextInMemory : ListContext
	{
		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ListContextInMemory CreateContext(out DbSet<TProperty> properties)
		{
			ListContextInMemory listContext = new();
			GetDbSet(listContext, out properties);
			return listContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存は無い前提）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インメモリデータベースを準備しています...");

			// 新規作成
			YukaListerModel.Instance.EnvModel.ListContextInMemory = CreateContext(out DbSet<TProperty> properties);
			YukaListerModel.Instance.EnvModel.ListContextInMemory.Database.EnsureCreated();
			DbCommon.UpdateProperty(YukaListerModel.Instance.EnvModel.ListContextInMemory, properties);

#if DEBUGz
			Debug.WriteLine("CreateDatabase() count: " + properties.Count());
			using ListContextInMemory listContext2 = CreateContext(out DbSet<TProperty> properties2);
			Debug.WriteLine("CreateDatabase() count2: " + properties2.Count());
#endif

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インメモリデータベースを作成しました。");
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			SqliteConnectionStringBuilder stringBuilder = new()
			{
				DataSource = DatabasePath(),
				Mode = SqliteOpenMode.Memory,
				Cache = SqliteCacheMode.Shared,
			};
			SqliteConnection sqliteConnection = new SqliteConnection(stringBuilder.ToString());
			sqliteConnection.Open();
			optionsBuilder.UseSqlite(sqliteConnection);
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_LIST_DATABASE_IN_MEMORY = "ListInMemory";

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		private static String DatabasePath()
		{
			return FILE_NAME_LIST_DATABASE_IN_MEMORY;
		}

	}
}
