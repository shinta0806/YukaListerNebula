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

namespace YukaLister.Models.DatabaseContexts;

internal partial class ListContextInMemory : ListContext
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	// --------------------------------------------------------------------
	// メインコンストラクター
	// --------------------------------------------------------------------
	public ListContextInMemory()
			: base("ゆかり用リスト（インメモリ）", true)
	{
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// データベースのフルパス
	// --------------------------------------------------------------------
	public override String DatabasePath()
	{
		return FILE_NAME_LIST_DATABASE_IN_MEMORY;
	}

	// ====================================================================
	// protected 関数
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

		// sqliteConnection は using にしない（テーブルが見つからなくなりエラーとなる）
		SqliteConnection sqliteConnection = new(stringBuilder.ToString());
		sqliteConnection.Open();
		optionsBuilder
#if DEBUG
			.EnableSensitiveDataLogging()
#endif
			.UseSqlite(sqliteConnection);
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	// データベースファイル名
	private const String FILE_NAME_LIST_DATABASE_IN_MEMORY = "ListInMemory";
}
