// ============================================================================
// 
// ゆかりすたー NEBULA で使用するデータベースのコンテキストの基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.IO;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public abstract class YukaListerContext : DbContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public YukaListerContext(String databaseName)
		{
			_databaseName = databaseName;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// データベースプロパティーテーブル
		public DbSet<TProperty>? Properties { get; set; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースセット取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void GetDbSet(YukaListerContext yukaListerContext, out DbSet<TProperty> properties)
		{
			if (yukaListerContext.Properties == null)
			{
				throw new Exception("データベースプロパティーテーブルにアクセスできません。");
			}
			properties = yukaListerContext.Properties;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースファイルのバックアップを作成
		// --------------------------------------------------------------------
		public void BackupDatabase()
		{
			DbCommon.BackupDatabase(DatabasePath());
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public virtual void CreateDatabase()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベース初期化中...");

			// クリア
			Database.EnsureDeleted();

			// 新規作成
			Database.EnsureCreated();
			if (Properties == null)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベースを初期化できませんでした。");
				return;
			}
			DbCommon.UpdateProperty(this, Properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベースを初期化しました。");
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合は作成しない）
		// --------------------------------------------------------------------
		public virtual void CreateDatabaseIfNeeded()
		{
			if (Properties != null && DbCommon.ValidPropertyExists(Properties))
			{
				// 既存のデータベースがある場合はクリアしない
				return;
			}
			CreateDatabase();
		}

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public abstract String DatabasePath();

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC
		// --------------------------------------------------------------------
		public DateTime LastWriteDateTime()
		{
			return new FileInfo(DatabasePath()).LastWriteTimeUtc;
		}

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC
		// --------------------------------------------------------------------
		public Double LastWriteMjd()
		{
			return JulianDay.DateTimeToModifiedJulianDate(LastWriteDateTime());
		}

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// データベース名（"データベース" は含まない）
		protected String _databaseName;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(DbCommon.Connect(DatabasePath()));
		}
	}
}
