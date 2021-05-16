// ============================================================================
// 
// サムネイルキャッシュデータベースのコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class ThumbContext : YukaListerContext
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// サムネイルキャッシュテーブル
		public DbSet<TCacheThumb>? CacheThumbs { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ThumbContext CreateContext(out DbSet<TCacheThumb> cacheThumbs)
		{
			ThumbContext thumbContext = new();
			GetDbSet(thumbContext, out cacheThumbs);
			return thumbContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ThumbContext CreateContext(out DbSet<TProperty> properties)
		{
			ThumbContext thumbContext = new();
			GetDbSet(thumbContext, out properties);
			return thumbContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "サムネイルキャッシュデータベース初期化中...");

			// クリア
			using ThumbContext thumbContext = CreateContext(out DbSet<TProperty> properties);
			thumbContext.Database.EnsureDeleted();

			// 新規作成
			thumbContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(thumbContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "サムネイルキャッシュデータベースを初期化しました。");
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public static void CreateDatabaseIfNeeded()
		{
			using ThumbContext thumbContext = CreateContext(out DbSet<TProperty> properties);
			if (DbCommon.ValidPropertyExists(properties))
			{
				// 既存のデータベースがある場合はクリアしない
				return;
			}
			CreateDatabase();
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(ThumbContext thumbContext, out DbSet<TCacheThumb> cacheThumbs)
		{
			if (thumbContext.CacheThumbs == null)
			{
				throw new Exception("サムネイルキャッシュテーブルにアクセスできません。");
			}
			cacheThumbs = thumbContext.CacheThumbs;
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(DbCommon.Connect(DbCommon.ThumbDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings)));
		}

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// サムネイルキャッシュテーブル
			modelBuilder.Entity<TCacheThumb>().HasIndex(x => new { x.FileName, x.Width }).IsUnique();
			modelBuilder.Entity<TCacheThumb>().HasIndex(x => x.FileName);
			modelBuilder.Entity<TCacheThumb>().HasIndex(x => x.ThumbLastWriteTime);
		}
	}
}
