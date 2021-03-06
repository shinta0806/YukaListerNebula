// ============================================================================
// 
// キャッシュデータベース（リストデータベースのキャッシュ）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
// キャッシュへの更新（追加）時は、CreateContext() ではなくインスタンスメソッドを使用すること
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.DatabaseContexts
{
	public class CacheContext : YukaListerContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public CacheContext(String driveLetter)
				: base(driveLetter + " キャッシュ")
		{
			Debug.Assert(driveLetter.Length == 2, "CacheContext() bad drive letter");
			_driveLetter = driveLetter;
			CreateDatabaseIfNeeded();
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブル
		// --------------------------------------------------------------------

		// 検出ファイルリストテーブル
		public DbSet<TFound>? Founds { get; set; }

		// --------------------------------------------------------------------
		// キャッシュ管理テーブル
		// --------------------------------------------------------------------

		// キャッシュ管理テーブル
		public DbSet<TCacheHeader>? CacheHeaders { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static CacheContext CreateContext(String driveLetter, out DbSet<TFound> founds)
		{
			CacheContext cacheContext = new(driveLetter);

			// 検出ファイルリストテーブル
			GetDbSet(cacheContext, out founds);

			return cacheContext;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(CacheContext cacheContext, out DbSet<TFound> founds)
		{
			if (cacheContext.Founds == null)
			{
				throw new Exception("検出ファイルリストテーブルにアクセスできません。");
			}
			founds = cacheContext.Founds;
		}

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(CacheContext cacheContext, out DbSet<TCacheHeader> cacheHeaders)
		{
			if (cacheContext.CacheHeaders == null)
			{
				throw new Exception("キャッシュ管理テーブルにアクセスできません。");
			}
			cacheHeaders = cacheContext.CacheHeaders;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public override void CreateDatabaseIfNeeded()
		{
			if (Properties != null && DbCommon.ValidPropertyExists(Properties))
			{
				TProperty property = DbCommon.Property(Properties);
				if (Common.CompareVersionString(property.AppVer, "Ver 1.18 α") >= 0)
				{
					// 既存のデータベースがあり、キャッシュデータの互換性がある場合はクリアしない
					return;
				}
			}
			CreateDatabase();
		}

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return YlCommon.YukaListerStatusFolderPath(_driveLetter, true) + FILE_NAME_CACHE_DATABASE;
		}

		// --------------------------------------------------------------------
		// キャッシュ更新（追加）
		// records の ParentFolder はすべて同じ前提
		// --------------------------------------------------------------------
		public void UpdateCache(List<TFound> records)
		{
			String parentFolder = records.First().ParentFolder;

			// 追加しようとしているキャッシュと同じ親フォルダーの旧キャッシュ削除
			GetDbSet(this, out DbSet<TFound> founds);
			IQueryable<TFound> removes = founds.Where(x => x.ParentFolder == parentFolder);
			founds.RemoveRange(removes);

			// 追加しようとしているキャッシュとドライブレターが異なるキャッシュ削除
			removes = founds.Where(x => !x.ParentFolder.Contains(YlCommon.DriveLetter(parentFolder)));
			founds.RemoveRange(removes);
			SaveChanges();

			// 新キャッシュ追加
			foreach (TFound record in records)
			{
				// Uid を初期化して自動的に Uid を振ってもらうようにする
				record.Uid = 0;
			}
			founds.AddRange(records);

			// キャッシュ管理テーブル更新
			GetDbSet(this, out DbSet<TCacheHeader> cacheHeaders);
			TCacheHeader? cacheHeader = cacheHeaders.FirstOrDefault(x => x.ParentFolder == parentFolder);
			Boolean needAdd = false;
			if (cacheHeader == null)
			{
				cacheHeader = new()
				{
					ParentFolder = parentFolder,
				};
				needAdd = true;
			}
			cacheHeader.UpdateTime = YlCommon.UtcNowMjd();
			if (needAdd)
			{
				cacheHeaders.Add(cacheHeader);
			}

			SaveChanges();
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// 検出ファイルリストテーブル
			modelBuilder.Entity<TFound>().HasIndex(x => x.ParentFolder);

			// キャッシュ管理テーブル
			modelBuilder.Entity<TCacheHeader>().HasIndex(x => x.UpdateTime);
			modelBuilder.Entity<TCacheHeader>().HasIndex(x => x.ParentFolder).IsUnique();
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_CACHE_DATABASE = YlConstants.APP_ID + "Cache" + Common.FILE_EXT_SQLITE3;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 対象ドライブ（"D:" のようにコロンまで）
		private readonly String _driveLetter;
	}
}
