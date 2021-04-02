// ============================================================================
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
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
				if (YukaListerModel.Instance.EnvModel.YukaListerStatus == YukaListerStatus.Error)
				{
					continue;
				}
				YukaListerModel.Instance.EnvModel.YukaListerStatus = YukaListerStatus.Running;
				Debug.WriteLine("Sifolin.CoreMain() 進行");

				try
				{
					while (true)
					{
						YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();

						TargetFolderInfo? targetFolderInfo;

						// ToDo: 削除系未実装

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

						// メモリー DB → ディスク DB
						if (_isMemoryDbDirty)
						{
							MemoryToDisk();
							continue;
						}

						// やることが無くなったのでループを抜けて待機へ向かう
						// 念のため最後に表示を更新
						YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
						break;
					}
				}
				catch (OperationCanceledException)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " の稼働を終了します。");
					return;
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, GetType().Name + "ループ稼働時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}

				Debug.WriteLine("Sifolin.CoreMain() 待機");
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
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
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.AddInfos;
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
			if (YlCommon.DetectFolderExcludeSettingsStatus(targetFolderInfo.Path) == FolderExcludeSettingsStatus.True)
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
				allPathes = Directory.GetFiles(targetFolderInfo.Path);
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
			}
		}

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルにファイル情報を追加
		// --------------------------------------------------------------------
		private void AddInfos(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			AddInfosCore(targetFolderInfo);
#if DEBUG
			Thread.Sleep(500);
#endif

			// 動作状況設定
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.Done;
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
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings2Ex(targetFolderInfo.Path);
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);

			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds);
			using TFoundSetter foundSetter = new(founds);

			// 指定フォルダーの全レコード
			IQueryable<TFound> targetRecords = founds.Where(x => x.Folder == targetFolderInfo.Path);

			// 情報付与
			foreach (TFound record in targetRecords)
			{
				FileInfo fileInfo = new(record.Path);
				record.LastWriteTime = JulianDay.DateTimeToModifiedJulianDate(fileInfo.LastWriteTime);
				record.FileSize = fileInfo.Length;

				// ToDo: aTFoundSetter

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			// ToDo: タグ付与

			// コミット
			listContextInMemory.SaveChanges();
		}

		// --------------------------------------------------------------------
		// キャッシュ DB からディスク DB へコピー
		// --------------------------------------------------------------------
		private void CacheToDisk(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			// ToDo: 未実装

			// 動作状況設定
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.FindSubFolders;
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
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
				String[] subFolderPathes = Directory.GetDirectories(parentFolder.Path, "*", SearchOption.TopDirectoryOnly);
				foreach (String subFolderPath in subFolderPathes)
				{
					// サブフォルダー追加
					TargetFolderInfo subFolder = new(parentFolder.ParentPath, subFolderPath, parentFolder.Level + 1);
					subFolder.FolderTaskKind = FolderTaskKind.Add;
					subFolder.FolderTaskDetail = FolderTaskDetail.AddFileNames;
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
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.AddFileNames;
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
				YukaListerModel.Instance.ProjModel.RemoveTargetFolder(targetFolderInfo.Path);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, targetFolderInfo.Path
						+ "\nのサブフォルダーが既に追加されています。\nサブフォルダーを一旦削除してから追加しなおして下さい。");
				return;
			}

			// 子の追加
			YukaListerModel.Instance.ProjModel.AddTargetSubFolders(targetFolderInfo, subFolders);

			// 親設定
			targetFolderInfo.HasChildren = subFolders.Any();
			targetFolderInfo.NumTotalFolders = 1 + subFolders.Count();

			// その他
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.Path
					+ "\n" + targetFolderInfo.NumTotalFolders + " 個のフォルダーをキューに追加しました。");
		}

		// --------------------------------------------------------------------
		// メモリー DB → ディスク DB
		// --------------------------------------------------------------------
		private void MemoryToDisk()
		{
			using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> f);
#if DEBUG
			Debug.WriteLine("MemoryToDisk test: " + (f.FirstOrDefault()?.LastWriteTime ?? 0).ToString());
#endif
			using SqliteConnection? sqliteConnectionInMemory = listContextInMemory.Database.GetDbConnection() as SqliteConnection;
			using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> _);
			using SqliteConnection? sqliteConnectionInDisk = listContextInDisk.Database.GetDbConnection() as SqliteConnection;
			if (sqliteConnectionInMemory != null && sqliteConnectionInDisk != null)
			{
				sqliteConnectionInMemory.BackupDatabase(sqliteConnectionInDisk);
				YukaListerModel.Instance.ProjModel.SetAllFolderTaskStatusToDoneInDisk();
				_isMemoryDbDirty = false;
			}
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
