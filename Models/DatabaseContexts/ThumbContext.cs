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
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
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
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.ThumbDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings);
		}

		// ====================================================================
		// protected 関数
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
