﻿// ============================================================================
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
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
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
		// 楽曲情報データベースから別名を検索
		// 見つからない場合は null
		// --------------------------------------------------------------------
		public static T? SelectAliasByAlias<T>(DbSet<T> records, String? alias, Boolean includesInvalid = false) where T : class, IRcAlias
		{
			if (String.IsNullOrEmpty(alias))
			{
				return null;
			}
			return records.SingleOrDefault(x => x.Alias == alias && (includesInvalid || !x.Invalid));
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
		// 楽曲情報データベースからカテゴリー名を列挙
		// --------------------------------------------------------------------
		public static List<String> SelectCategoryNames(DbSet<TCategory> categories, Boolean includesInvalid = false)
		{
			List<String> categoryNames = new();
			foreach (TCategory category in categories)
			{
				if ((includesInvalid || !category.Invalid) && !String.IsNullOrEmpty(category.Name))
				{
					categoryNames.Add(category.Name);
				}
			}
			return categoryNames;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースから IRcMaster をすべて検索
		// --------------------------------------------------------------------
		public static List<T> SelectMastersByName<T>(DbSet<T> records, String? name, Boolean includesInvalid = false) where T : class, IRcMaster
		{
			if (String.IsNullOrEmpty(name))
			{
				return new List<T>();
			}
			return records.Where(x => x.Name == name && (includesInvalid || !x.Invalid)).ToList();
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースから楽曲に紐付く人物を検索
		// sequenceRecords の型によって歌手、作曲者、作詞者、編曲者のいずれかを検索
		// 現時点では includesInvalid == true の用途が思いつかないため引数として includesInvalid は装備しない
		// 引数として includesInvalid を装備する場合は返値を List<TPerson?> にする必要があると思う
		// --------------------------------------------------------------------
		public static List<TPerson> SelectSequencedPeopleBySongId<T>(DbSet<T> sequenceRecords, DbSet<TPerson> personRecords, String songId) where T : class, IRcSequence
		{
			List<T> sequences = SelectSequencesById(sequenceRecords, songId);
			List<TPerson> people = new();

			foreach (T sequence in sequences)
			{
				TPerson? person = SelectBaseById(personRecords, sequence.LinkId);
				if (person != null)
				{
					people.Add(person);
				}
			}

			return people;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースから楽曲に紐付くタグを検索
		// 現時点では includesInvalid == true の用途が思いつかないため引数として includesInvalid は装備しない
		// 引数として includesInvalid を装備する場合は返値を List<TTag?> にする必要があると思う
		// --------------------------------------------------------------------
		public static List<TTag> SelectSequencedTagsBySongId(DbSet<TTagSequence> tagSequenceRecords, DbSet<TTag> tagRecords, String songId)
		{
			List<TTagSequence> sequences = SelectSequencesById(tagSequenceRecords, songId);
			List<TTag> tags = new();

			foreach (TTagSequence sequence in sequences)
			{
				TTag? tag = SelectBaseById(tagRecords, sequence.LinkId);
				if (tag != null)
				{
					tags.Add(tag);
				}
			}

			return tags;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースからタイアップに紐付くタイアップグループを検索
		// 現時点では includesInvalid == true の用途が思いつかないため引数として includesInvalid は装備しない
		// 引数として includesInvalid を装備する場合は返値を List<TTieUpGroup?> にする必要があると思う
		// --------------------------------------------------------------------
		public static List<TTieUpGroup> SelectSequencedTieUpGroupsByTieUpId(DbSet<TTieUpGroupSequence> tieUpGroupSequenceRecords, DbSet<TTieUpGroup> tieUpGroupRecords, String tieUpId)
		{
			List<TTieUpGroupSequence> sequences = SelectSequencesById(tieUpGroupSequenceRecords, tieUpId);
			List<TTieUpGroup> tieUpGroups = new();

			foreach (TTieUpGroupSequence sequence in sequences)
			{
				TTieUpGroup? tieUpGroup = SelectBaseById(tieUpGroupRecords, sequence.LinkId);
				if (tieUpGroup != null)
				{
					tieUpGroups.Add(tieUpGroup);
				}
			}

			return tieUpGroups;
		}

		// --------------------------------------------------------------------
		// 紐付データベースから紐付を検索
		// 紐付更新時は includesInvalid == true で呼ばれる
		// --------------------------------------------------------------------
		public static List<T> SelectSequencesById<T>(DbSet<T> records, String id, Boolean includesInvalid = false) where T : class, IRcSequence
		{
			if (String.IsNullOrEmpty(id))
			{
				return new();
			}

			return records.Where(x => x.Id == id && (includesInvalid || !x.Invalid)).OrderBy(x => x.Sequence).ToList();
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
