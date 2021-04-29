// ============================================================================
// 
// リストデータベース（作業用：インメモリ）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shinta;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
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
		public static ListContextInMemory CreateContext(out DbSet<TFound> founds,
				out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
				out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences)
		{
			ListContextInMemory listContext = new();
			GetDbSet(listContext, out founds);
			GetDbSet(listContext, out people);
			GetDbSet(listContext, out artistSequences);
			GetDbSet(listContext, out composerSequences);
			GetDbSet(listContext, out tags);
			GetDbSet(listContext, out tagSequences);
			return listContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ListContextInMemory CreateContext(out DbSet<TFound> founds)
		{
			ListContextInMemory listContext = new();
			GetDbSet(listContext, out founds);
			return listContext;
		}

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
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インメモリデータベース初期化中...");

			// 新規作成
			_listContextInMemory = CreateContext(out DbSet<TProperty> properties);
			_listContextInMemory.Database.EnsureCreated();
			DbCommon.UpdateProperty(_listContextInMemory, properties);

#if DEBUGz
			Debug.WriteLine("CreateDatabase() count: " + properties.Count());
			using ListContextInMemory listContext2 = CreateContext(out DbSet<TProperty> properties2);
			Debug.WriteLine("CreateDatabase() count2: " + properties2.Count());
#endif

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インメモリデータベースを初期化しました。");
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
			SqliteConnection sqliteConnection = new(stringBuilder.ToString());
			sqliteConnection.Open();
			optionsBuilder.UseSqlite(sqliteConnection);
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_LIST_DATABASE_IN_MEMORY = "ListInMemory";

		// ====================================================================
		// private static メンバー変数
		// ====================================================================

		// インメモリデータベースが生存し続けるようにインスタンスを保持
		// マルチスレッドで安全に使用できるよう、本変数は使用せず、CreateContext() で新たなコンテキストを作成すること
		private static ListContextInMemory? _listContextInMemory;

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
