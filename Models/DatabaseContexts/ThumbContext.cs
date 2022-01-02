// ============================================================================
// 
// サムネイルキャッシュデータベースのコンテキスト
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
	public class ThumbContext : YukaListerContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ThumbContext()
				: base("サムネイルキャッシュ")
		{
			Debug.Assert(CacheThumbs != null, "CacheThumbs table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// サムネイルキャッシュテーブル
		public DbSet<TCacheThumb> CacheThumbs { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

#if false
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
#endif

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.ThumbDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

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
