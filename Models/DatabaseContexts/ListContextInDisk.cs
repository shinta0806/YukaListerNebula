// ============================================================================
// 
// リストデータベース（ゆかり用：ディスク）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
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
		public static ListContextInDisk CreateContext(out DbSet<TFound> founds)
		{
			ListContextInDisk listContext = new();
			GetDbSet(listContext, out founds);
			return listContext;
		}

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
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり用リストデータベース初期化中...");

			// クリア
			using ListContextInDisk listContext = CreateContext(out DbSet<TProperty> properties);
			listContext.Database.EnsureDeleted();

			// 新規作成
			listContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(listContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり用リストデータベースを初期化しました。");
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(DbCommon.Connect(DbCommon.ListDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings)));
		}
	}
}
