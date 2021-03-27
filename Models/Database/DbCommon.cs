// ============================================================================
// 
// データベース共通で使用する関数
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.Database
{
	public class DbCommon
	{
		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースファイルのバックアップを作成
		// --------------------------------------------------------------------
		public static void BackupDatabase(String srcPath)
		{
			String? srcFolder = Path.GetDirectoryName(srcPath);
			if (String.IsNullOrEmpty(srcFolder))
			{
				return;
			}
			if (!File.Exists(srcPath))
			{
				return;
			}

			String fileNameForLog = Path.GetFileNameWithoutExtension(srcPath);
			try
			{
				// バックアップ先の決定（既に存在する場合はバックアップをスキップ：1 日 1 回まで）
				FileInfo srcFileInfo = new(srcPath);
				String backupPath = YukaListerDatabaseFullFolder() + Path.GetFileNameWithoutExtension(srcPath) + "_(bak)_" + srcFileInfo.LastWriteTime.ToString("yyyy_MM_dd") + Common.FILE_EXT_BAK;
				if (File.Exists(backupPath))
				{
					return;
				}

				// バックアップ
				File.Copy(srcPath, backupPath);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "データベース " + fileNameForLog + " のバックアップ作成：" + backupPath);

				// 溢れたバックアップを削除
				List<FileInfo> backupFileInfos = new();
				String[] backupFiles = Directory.GetFiles(srcFolder, Path.GetFileNameWithoutExtension(srcPath) + "_(bak)_*" + Common.FILE_EXT_BAK);
				foreach (String backupFile in backupFiles)
				{
					backupFileInfos.Add(new FileInfo(backupFile));
				}
				backupFileInfos.Sort((a, b) => -a.LastWriteTime.CompareTo(b.LastWriteTime));
				for (Int32 i = backupFileInfos.Count - 1; i >= NUM_DB_BACKUP_GENERATIONS; i--)
				{
					File.Delete(backupFileInfos[i].FullName);
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "データベース " + fileNameForLog + " のバックアップ削除：" + backupFileInfos[i].FullName);
				}
			}
			catch (Exception excep)
			{
				// スプラッシュウィンドウに隠れる恐れがあるため表示はしない
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "データベース " + fileNameForLog + " バックアップ時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// データベース接続
		// --------------------------------------------------------------------
		public static SqliteConnection Connect(String path)
		{
			SqliteConnectionStringBuilder stringBuilder = new()
			{
				DataSource = path,
				//BusyTimeout = 100, // default = 0
				//PrepareRetries = 10, // default = 0
			};
			return new SqliteConnection(stringBuilder.ToString());
		}

		// --------------------------------------------------------------------
		// データベースファイルを準備
		// --------------------------------------------------------------------
		public static void PrepareDatabases()
		{
			try
			{
				Directory.CreateDirectory(YukaListerDatabaseFullFolder());
				Directory.CreateDirectory(YukariDatabaseFullFolder());
				MusicInfoContext.CreateDatabaseIfNeeded();
				ListContextInDisk.CreateDatabase();
				ListContextInMemory.CreateDatabase();
			}
			catch (Exception excep)
			{
				// スプラッシュウィンドウに隠れる恐れがあるため表示はしない
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "データベース準備時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// データベースのプロパティーを取得
		// --------------------------------------------------------------------
		public static TProperty Property(DbSet<TProperty> properties)
		{
			try
			{
				return properties.First();
			}
			catch (Exception)
			{
				return new TProperty();
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースから IRcBase を検索
		// 見つからない場合は null
		// --------------------------------------------------------------------
		public static T? SelectBaseById<T>(DbSet<T> records, String? id, Boolean includesInvalid = false) where T : class, IRcBase
		{
			if (String.IsNullOrEmpty(id))
			{
				return null;
			}
			return records.SingleOrDefault(x => x.Id == id && (includesInvalid || !x.Invalid));
		}

		// --------------------------------------------------------------------
		// データベースのプロパティーを更新（存在しない場合は新規作成）
		// --------------------------------------------------------------------
		public static void UpdateProperty(DbContext context, DbSet<TProperty> properties)
		{
			// 古いプロパティーを削除
			properties.RemoveRange(properties);
			context.SaveChanges();

			// 新しいプロパティーを追加
			TProperty property = new() { AppId = YlConstants.APP_ID, AppVer = YlConstants.APP_GENERATION + "," + YlConstants.APP_VER };
			properties.Add(property);
			context.SaveChanges();
		}

		// --------------------------------------------------------------------
		// データベース中に有効なプロパティー情報が存在するか
		// --------------------------------------------------------------------
		public static Boolean ValidPropertyExists(DbSet<TProperty> properties)
		{
			TProperty property = Property(properties);
			return property.AppId == YlConstants.APP_ID;
		}

		// --------------------------------------------------------------------
		// ゆかりすたーデータベースを保存するフォルダーのフルパス（末尾 '\\'）
		// --------------------------------------------------------------------
		public static String YukaListerDatabaseFullFolder()
		{
			return YukaListerModel.Instance.EnvModel.ExeFullFolder + YlConstants.FOLDER_NAME_DATABASE;
		}

		// --------------------------------------------------------------------
		// ゆかり用データベースを保存するフォルダーのフルパス（末尾 '\\'）
		// --------------------------------------------------------------------
		public static String YukariDatabaseFullFolder()
		{
			return Path.GetDirectoryName(YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPath()) + "\\" + YlConstants.FOLDER_NAME_LIST;
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// バックアップ世代数
		private const Int32 NUM_DB_BACKUP_GENERATIONS = 31;

	}
}
