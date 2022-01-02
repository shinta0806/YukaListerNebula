// ============================================================================
// 
// ゆかり request.db のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
// データベースプロパティーテーブルが存在しないため YukaListerContext の派生にはしない
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Diagnostics;
using System.IO;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class YukariRequestContext : DbContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public YukariRequestContext()
		{
			Debug.Assert(YukariRequests != null, "YukariRequests table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ゆかり予約テーブル
		public DbSet<TYukariRequest> YukariRequests { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static YukariRequestContext CreateContext(out DbSet<TYukariRequest> yukariRequests)
		{
			YukariRequestContext requestDbContext = new();
			GetDbSet(requestDbContext, out yukariRequests);
			return requestDbContext;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(YukariRequestContext requestDbContext, out DbSet<TYukariRequest> yukariRequests)
		{
			if (requestDbContext.YukariRequests == null)
			{
				throw new Exception("ゆかり予約テーブルにアクセスできません。");
			}
			yukariRequests = requestDbContext.YukariRequests;
		}
#endif

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC（修正ユリウス日）
		// --------------------------------------------------------------------
		public static Double LastWriteMjd()
		{
			return JulianDay.DateTimeToModifiedJulianDate(new FileInfo(YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath()).LastWriteTimeUtc);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			using SqliteConnection sqliteConnection = DbCommon.Connect(YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath());
			optionsBuilder.UseSqlite(sqliteConnection);
		}

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
		}
	}
}
