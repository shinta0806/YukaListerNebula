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

using System;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;

namespace YukaLister.Models.DatabaseContexts
{
	public class ListContextInMemory : ListContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ListContextInMemory()
				: base("ゆかり用リスト（インメモリ）")
		{
		}

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ListContextInMemory CreateContext(out DbSet<TFound> founds,
				out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
				out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TTieUpGroupSequence> tieUpGroupSequences,
				out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences)
		{
			ListContextInMemory listContext = new();
			GetDbSet(listContext, out founds);
			GetDbSet(listContext, out people);
			GetDbSet(listContext, out artistSequences);
			GetDbSet(listContext, out composerSequences);
			GetDbSet(listContext, out tieUpGroups);
			GetDbSet(listContext, out tieUpGroupSequences);
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

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return FILE_NAME_LIST_DATABASE_IN_MEMORY;
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
	}
}
