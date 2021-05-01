﻿// ============================================================================
// 
// ネビュラコア：検索データ作成担当
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
using System.Threading;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Sifolin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Sifolin()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（検索データ作成）のメインルーチン
		// --------------------------------------------------------------------
		protected override void CoreMain()
		{
			while (true)
			{
				MainEvent.WaitOne();
				if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
				{
					continue;
				}

				Int32 startTick = Environment.TickCount;
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Sifolin] = YukaListerStatus.Running;
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");

				try
				{
					while (true)
					{
#if DEBUGz
						Thread.Sleep(1000);
#endif
						YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();

						TargetFolderInfo? targetFolderInfo;

						// 削除
						targetFolderInfo = YukaListerModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.Remove);
						if (targetFolderInfo != null)
						{
							Remove(targetFolderInfo);
							continue;
						}

						// キャッシュ活用
						targetFolderInfo = YukaListerModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.CacheToDisk);
						if (targetFolderInfo != null)
						{
							CacheToDisk(targetFolderInfo);
							continue;
						}

						// サブフォルダー検索
						targetFolderInfo = YukaListerModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.FindSubFolders);
						if (targetFolderInfo != null)
						{
							FindSubFolders(targetFolderInfo);
							continue;
						}

						// ファイル名追加
						targetFolderInfo = YukaListerModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.AddFileNames);
						if (targetFolderInfo != null)
						{
							AddFileNames(targetFolderInfo);
							continue;
						}

						// ファイル情報追加
						targetFolderInfo = YukaListerModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.AddInfos);
						if (targetFolderInfo != null)
						{
							AddInfos(targetFolderInfo);
							continue;
						}

						// メモリー DB → ディスク DB とキャッシュ DB
						if (_isMemoryDbDirty)
						{
							MemoryToDisk();
							MemoryToCache();
							continue;
						}

						// やることが無くなったのでループを抜けて待機へ向かう
						break;
					}
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, GetType().Name + " ループ稼働時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}

				// 念のため最後に表示を更新
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Sifolin] = YukaListerStatus.Ready;
				YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;

				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
		}

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// メモリ DB 更新フラグ
		private Boolean _isMemoryDbDirty;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルにファイル名を追加
		// --------------------------------------------------------------------
		private void AddFileNames(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			AddFileNamesCore(targetFolderInfo);

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.AddFileNames, FolderTaskDetail.AddInfos);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
		}

		// --------------------------------------------------------------------
		// 指定フォルダ内のファイルを検索してゆかり用データベースに追加
		// ユニーク ID、フルパス、フォルダーのみ記入する
		// ファイルは再帰検索しない
		// --------------------------------------------------------------------
		private void AddFileNamesCore(TargetFolderInfo targetFolderInfo)
		{
			// フォルダー除外設定を読み込む
			if (YlCommon.DetectFolderExcludeSettingsStatus(targetFolderInfo.TargetPath) == FolderExcludeSettingsStatus.True)
			{
				return;
			}

			// Uid
			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds);
			Int64 uid = founds.Any() ? founds.Max(x => x.Uid) + 1 : 1;

			// 検索
			String[] allPathes;
			try
			{
				allPathes = Directory.GetFiles(targetFolderInfo.TargetPath);
			}
			catch (Exception)
			{
				return;
			}

			// 追加準備
			List<TFound> addRecords = new();
			addRecords.Capacity = allPathes.Length;
			foreach (String path in allPathes)
			{
				if (!YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.Contains(Path.GetExtension(path).ToLower()))
				{
					continue;
				}

				TFound record = new();
				record.Uid = uid;
				record.Path = path;
				record.Folder = Path.GetDirectoryName(path) ?? String.Empty;
				record.ParentFolder = targetFolderInfo.ParentPath;

				// 楽曲名とファイルサイズが両方とも初期値だと、ゆかりが検索結果をまとめてしまうため、ダミーのファイルサイズを入れる
				// （文字列である楽曲名を入れると処理が遅くなるので処理が遅くなりにくい数字のファイルサイズをユニークにする）
				record.FileSize = -uid;

				addRecords.Add(record);
				uid++;
			}

			// メモリー DB に追加
			founds.AddRange(addRecords);
			listContextInMemory.SaveChanges();
			_isMemoryDbDirty = true;

			// キャッシュが使われていない場合はディスク DB にも追加
			if (!targetFolderInfo.IsCacheUsed)
			{
				using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> diskFounds);
				diskFounds.AddRange(addRecords);
				listContextInDisk.SaveChanges();
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり用リストデータベースにファイル名を追加しました。" + targetFolderInfo.TargetPath);
			}
		}

		// --------------------------------------------------------------------
		// フォルダー設定で指定されているタグを TFound とゆかり用リストデータベースに付与する
		// --------------------------------------------------------------------
		private void AddFolderTagsInfo(TargetFolderInfo targetFolderInfo, IQueryable<TFound> records, DbSet<TTag> tags, DbSet<TTagSequence> tagSequences)
		{
			try
			{
				String tagKey = YlCommon.WithoutDriveLetter(targetFolderInfo.TargetPath);
				if (!YukaListerModel.Instance.EnvModel.TagSettings.FolderTags.ContainsKey(tagKey))
				{
					return;
				}

				// TTag にフォルダー設定のタグ情報と同名のタグがあるか？
				String tagValue = YukaListerModel.Instance.EnvModel.TagSettings.FolderTags[tagKey];
				TTag? tagRecord = DbCommon.SelectMasterByName(tags, tagValue);
				if (tagRecord == null)
				{
					// 同名のタグが無いので、tagKey を Id とするタグがまだ存在しなければ作成
					String tagId = YlConstants.TEMP_ID_PREFIX + tagKey;
					tagRecord = DbCommon.SelectBaseById(tags, tagId);
					if (tagRecord == null)
					{
						tagRecord = new()
						{
							// IRcBase
							Id = tagId,
							Import = false,
							Invalid = false,
							UpdateTime = YlConstants.INVALID_MJD,
							Dirty = true,

							// IRcMaster
							Name = tagValue,
							Ruby = null,
							Keyword = null,
						};
						tags.Add(tagRecord);
					}
				}

				Dictionary<String, Boolean> addedIds = new();
				foreach (TFound record in records)
				{
					// TFound にタグ情報を追加
					// 楽曲情報データベースで付与されたものと同じ場合は重複連結となるが、ゆかりが検索するためのものなので問題ない
					record.TagName += "," + tagRecord.Name;
					if (!String.IsNullOrEmpty(tagRecord.Ruby))
					{
						record.TagRuby += "," + tagRecord.Ruby;
					}

					// ゆかり用リストデータベースの TTagSequence にタグ情報を追加
					// 1 つのフォルダー内に同じ曲が複数個存在する場合があるので、既に作業済みの曲はスキップ
					if (String.IsNullOrEmpty(record.SongId) || addedIds.ContainsKey(record.SongId))
					{
						continue;
					}

					// TTagSequence にフォルダー設定のタグ情報が無ければ保存
					List<TTag> songTags = DbCommon.SelectSequencedTagsBySongId(tagSequences, tags, record.SongId);
					if (songTags.FirstOrDefault(x => x.Name == tagRecord.Name) == null)
					{
						IQueryable<Int32> sequenceResults = tagSequences.Where(x => x.Id == record.SongId).Select(x => x.Sequence);
						Int32 seqMax = sequenceResults.Any() ? sequenceResults.Max() : -1;
						TTagSequence tagSequenceRecord = new()
						{
							// IDbBase
							Id = record.SongId,
							Import = false,
							Invalid = false,
							UpdateTime = YlConstants.INVALID_MJD,
							Dirty = true,

							// IDbSequence
							Sequence = seqMax + 1,
							LinkId = tagRecord.Id,
						};
						tagSequences.Add(tagSequenceRecord);
						addedIds[record.SongId] = true;
					}
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定タグ付与時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルに属性を追加
		// --------------------------------------------------------------------
		private void AddInfos(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "属性確認中... " + targetFolderInfo.TargetPath);

			// 作業
			AddInfosCore(targetFolderInfo);
#if DEBUGz
			Thread.Sleep(500);
#endif

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.AddInfos, FolderTaskDetail.Done);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.DoneInMemory);
		}

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルにファイル情報を追加
		// AddFileNames() で追加されない情報をすべて付与する
		// ファイルは再帰検索しない
		// --------------------------------------------------------------------
		private void AddInfosCore(TargetFolderInfo targetFolderInfo)
		{
			// フォルダー設定を読み込む
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings2Ex(targetFolderInfo.TargetPath);
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);

			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds,
					out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
					out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences);
			using TFoundSetter foundSetter = new(listContextInMemory, founds, people, artistSequences, composerSequences, tags, tagSequences);

			// 指定フォルダーの全レコード
			IQueryable<TFound> targetRecords = founds.Where(x => x.Folder == targetFolderInfo.TargetPath);

			// 情報付与
			foreach (TFound record in targetRecords)
			{
				FileInfo fileInfo = new(record.Path);
				record.LastWriteTime = JulianDay.DateTimeToModifiedJulianDate(fileInfo.LastWriteTime);
				record.FileSize = fileInfo.Length;
				foundSetter.SetTFoundValues(record, folderSettingsInMemory);

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}
			AddFolderTagsInfo(targetFolderInfo, targetRecords, tags, tagSequences);

			// コミット
			listContextInMemory.SaveChanges();
		}

		// --------------------------------------------------------------------
		// キャッシュ DB からディスク DB へコピー
		// --------------------------------------------------------------------
		private void CacheToDisk(TargetFolderInfo targetFolderInfo)
		{
			Debug.Assert(targetFolderInfo.IsParent, "CacheToDisk() not parent");

			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			CacheToDiskCore(targetFolderInfo);

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.CacheToDisk, FolderTaskDetail.FindSubFolders);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
		}

		// --------------------------------------------------------------------
		// キャッシュ DB からディスク DB へコピー
		// --------------------------------------------------------------------
		private void CacheToDiskCore(TargetFolderInfo targetFolderInfo)
		{
			using CacheContext cacheContext = CacheContext.CreateContext(YlCommon.DriveLetter(targetFolderInfo.TargetPath), out DbSet<TFound> cacheFounds);
			IQueryable<TFound> cacheRecords = cacheFounds.Where(x => x.ParentFolder == targetFolderInfo.TargetPath);
			if (!cacheRecords.Any())
			{
				// キャッシュが見つからない場合、ドライブレター以外の部分で合致するか再度検索
				cacheRecords = cacheFounds.Where(x => x.ParentFolder.Contains(targetFolderInfo.TargetPath.Substring(1)));
				if (!cacheRecords.Any())
				{
					return;
				}

				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
						+ "\nキャッシュのドライブレターを変換しています...");
				String drive = targetFolderInfo.TargetPath[0..1];
				foreach (TFound cacheRecord in cacheRecords)
				{
					cacheRecord.Path = drive + cacheRecord.Path.Substring(1);
					cacheRecord.Folder = drive + cacheRecord.Folder.Substring(1);
					cacheRecord.ParentFolder = drive + cacheRecord.ParentFolder.Substring(1);
				}
			}

			using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> diskFounds);
			diskFounds.AddRange(cacheRecords);
			listContextInDisk.SaveChanges();
			targetFolderInfo.IsCacheUsed = true;
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
					+ "\nキャッシュをゆかり用リストデータベースに反映しました。");
#if DEBUGz
			Thread.Sleep(30 * 1000);
#endif
		}

		// --------------------------------------------------------------------
		// リスト化対象フォルダーのサブフォルダーを列挙
		// --------------------------------------------------------------------
		private List<TargetFolderInfo> EnumSubFolders(TargetFolderInfo parentFolder)
		{
			YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			List<TargetFolderInfo> folders = new();
			try
			{
				// SearchOption.AllDirectories 付きで Directory.GetDirectories を呼び出すと、
				// ごみ箱のようにアクセス権限の無いフォルダーの中も列挙しようとして例外が
				// 発生し中断してしまう。
				// 面倒だが 1 フォルダーずつ列挙する
				String[] subFolderPathes = Directory.GetDirectories(parentFolder.TargetPath, "*", SearchOption.TopDirectoryOnly);
				foreach (String subFolderPath in subFolderPathes)
				{
					// サブフォルダー追加
					TargetFolderInfo subFolder = new(parentFolder.ParentPath, subFolderPath, parentFolder.Level + 1);
					subFolder.IsCacheUsed = parentFolder.IsCacheUsed;
					folders.Add(subFolder);

					// サブフォルダーのサブフォルダー
					List<TargetFolderInfo> subSubFolders = EnumSubFolders(subFolder);
					folders.AddRange(subSubFolders);

					// サブフォルダーの情報
					subFolder.HasChildren = subSubFolders.Any();
					subFolder.NumTotalFolders = 1 + subSubFolders.Count();
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception)
			{
			}
			return folders;
		}

		// --------------------------------------------------------------------
		// サブフォルダーを検索して追加
		// --------------------------------------------------------------------
		private void FindSubFolders(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			FindSubFoldersCore(targetFolderInfo);

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.FindSubFolders, FolderTaskDetail.AddFileNames);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
		}

		// --------------------------------------------------------------------
		// サブフォルダーを検索して追加
		// --------------------------------------------------------------------
		private void FindSubFoldersCore(TargetFolderInfo targetFolderInfo)
		{
			// 子の検索と重複チェック
			List<TargetFolderInfo> subFolders = EnumSubFolders(targetFolderInfo);
			Boolean childAdded = YukaListerModel.Instance.ProjModel.IsTargetFolderAdded(subFolders);
			if (childAdded)
			{
				// 追加済みの親を削除
				YukaListerModel.Instance.ProjModel.SetFolderTaskDetailOfFolderToRemove(targetFolderInfo.ParentPath);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, targetFolderInfo.TargetPath
						+ "\nのサブフォルダーが既に追加されています。\nサブフォルダーを一旦削除してから追加しなおして下さい。");
				return;
			}

			// 子の追加
			YukaListerModel.Instance.ProjModel.AddTargetSubFolders(targetFolderInfo, subFolders);

			// 親設定
			targetFolderInfo.HasChildren = subFolders.Any();
			targetFolderInfo.NumTotalFolders = 1 + subFolders.Count();

			// その他
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
					+ "\n" + targetFolderInfo.NumTotalFolders + " 個のフォルダーをキューに追加しました。");
		}

		// --------------------------------------------------------------------
		// メモリー DB → キャッシュ DB
		// --------------------------------------------------------------------
		private void MemoryToCache()
		{
			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds);
			IQueryable<String> parentFolders = founds.GroupBy(x => x.ParentFolder).Select(x => x.Key);
			foreach (String parentFolder in parentFolders)
			{
				using CacheContext cacheContext = new(YlCommon.DriveLetter(parentFolder));
				IQueryable<TFound> records = founds.Where(x => x.ParentFolder == parentFolder);
				cacheContext.UpdateCache(records);
			}
		}

		// --------------------------------------------------------------------
		// メモリー DB → ディスク DB
		// --------------------------------------------------------------------
		private void MemoryToDisk()
		{
			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> _);
			using SqliteConnection? sqliteConnectionInMemory = listContextInMemory.Database.GetDbConnection() as SqliteConnection;
			using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> _);
			using SqliteConnection? sqliteConnectionInDisk = listContextInDisk.Database.GetDbConnection() as SqliteConnection;
			if (sqliteConnectionInMemory != null && sqliteConnectionInDisk != null)
			{
				sqliteConnectionInMemory.BackupDatabase(sqliteConnectionInDisk);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり用リストデータベースの作成が完了しました。");
				YukaListerModel.Instance.ProjModel.SetAllFolderTaskStatusToDoneInDisk();
				_isMemoryDbDirty = false;
			}
		}

		// --------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------
		private void Remove(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			RemoveCore(targetFolderInfo);

			// 動作状況設定は削除済みのため不要
		}

		// --------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------
		private void RemoveCore(TargetFolderInfo targetFolderInfo)
		{
			if (!targetFolderInfo.IsParent)
			{
				// 親が一括削除するので、親でない場合は削除しない
				return;
			}

#if DEBUGz
			Thread.Sleep(3000);
#endif

			// まずディスク DB から削除
			using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> diskFounds);
			diskFounds.RemoveRange(diskFounds.Where(x => x.ParentFolder == targetFolderInfo.ParentPath));
			listContextInDisk.SaveChanges();

			// メモリ DB から削除
			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> memoryFounds);
			memoryFounds.RemoveRange(memoryFounds.Where(x => x.ParentFolder == targetFolderInfo.ParentPath));
			listContextInMemory.SaveChanges();

			// TargetFolderInfo 削除
			YukaListerModel.Instance.ProjModel.RemoveTargetFolders(targetFolderInfo.ParentPath);

			// その他
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.ParentPath
					+ "\nとその配下のフォルダーをゆかり検索対象から削除しました。");
		}

		// --------------------------------------------------------------------
		// フォルダーの動作状況を設定
		// --------------------------------------------------------------------
		private void SetFolderTaskStatus(TargetFolderInfo targetFolderInfo, FolderTaskStatus folderTaskStatus)
		{
			targetFolderInfo.FolderTaskStatus = folderTaskStatus;
			if (targetFolderInfo.Visible)
			{
				YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
			}
		}

	}
}
