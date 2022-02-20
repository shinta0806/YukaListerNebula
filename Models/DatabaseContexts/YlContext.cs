// ============================================================================
// 
// ゆかりすたー NEBULA で使用するデータベースのコンテキストの基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
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
	internal abstract class YlContext : DbContext
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public YlContext(String databaseName, Boolean useWal = false)
		{
			Debug.Assert(Properties != null, "Properties table not init");

			_databaseName = databaseName;
			_useWal = useWal;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// データベースプロパティーテーブル
		public DbSet<TProperty> Properties { get; set; }

		// ====================================================================
		// public 関数
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
			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベース初期化中...");

			// クリア
			Database.EnsureDeleted();

			// 新規作成
			Database.EnsureCreated();

			// ジャーナルモード設定
			SetJournalModeIfNeeded();

			if (Properties == null)
			{
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベースを初期化できませんでした。");
				return;
			}
			DbCommon.UpdateProperty(this, Properties);

			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベースを初期化しました。");
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
			String databasePath = DatabasePath();
			DateTime dateTime = new FileInfo(databasePath).LastWriteTimeUtc;
			try
			{
				LastWriteDateTimeSub(Path.ChangeExtension(databasePath, Common.FILE_EXT_SQLITE3_SHM), ref dateTime);
				LastWriteDateTimeSub(Path.ChangeExtension(databasePath, Common.FILE_EXT_SQLITE3_WAL), ref dateTime);
			}
			catch (Exception)
			{
			}
			Debug.WriteLine("LastWriteDateTime() " + Path.GetFileName(databasePath) + " " + dateTime.ToString());
			return dateTime;
		}

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC
		// --------------------------------------------------------------------
		public Double LastWriteMjd()
		{
			return JulianDay.DateTimeToModifiedJulianDate(LastWriteDateTime());
		}

		// --------------------------------------------------------------------
		// ジャーナルモードを設定する
		// --------------------------------------------------------------------
		public void SetJournalModeIfNeeded()
		{
			// EF Core のデフォルトは WAL なので、WAL を使いたい場合は設定不要
			if (_useWal)
			{
				return;
			}

			// DB ファイルが存在しない場合は設定しない
			if (!File.Exists(DatabasePath()))
			{
				return;
			}

			Database.EnsureCreated();

			if (Database.GetDbConnection() is not SqliteConnection sqliteConnection)
			{
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, _databaseName + "データベースの接続を取得できませんでした。");
				return;
			}

			// ToDo: 既存の DB に対して Open() すると、アプリ終了時にエラーが発生する（新規作成時は問題ない）
			// Collection was modified; enumeration operation may not execute.
			// at Microsoft.Data.Sqlite.SqliteConnectionPool.ReclaimLeakedConnections()
			sqliteConnection.Open();
			using SqliteCommand command = sqliteConnection.CreateCommand();
			command.CommandText = @"PRAGMA journal_mode = 'delete'";
			command.ExecuteNonQuery();
			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _databaseName + "データベースのジャーナルモードを DELETE にしました。");
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// データベース名（"データベース" は含まない）
		protected String _databaseName;

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			SqliteConnection sqliteConnection = DbCommon.Connect(DatabasePath());
			optionsBuilder.UseSqlite(sqliteConnection);
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// WAL を使用するかどうか
		private readonly Boolean _useWal;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ファイルの最終更新日時 UTC を更新
		// --------------------------------------------------------------------
		private static void LastWriteDateTimeSub(String path, ref DateTime dateTime)
		{
			DateTime subDateTime = new FileInfo(path).LastWriteTimeUtc;
			if (subDateTime > dateTime)
			{
				dateTime = subDateTime;
			}
		}
	}
}
