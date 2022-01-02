// ============================================================================
// 
// 処理の内容を管理する
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 一般的なプロジェクトとは異なり、保存・切替の概念は無い
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// ・TargetFolderInfos にアクセスする時は TargetFolderInfos のロック必須
// ・UI スレッドとワーカースレッドで TargetFolderInfos のロックと UI スレッドの占有（Dispatcher.Invoke()）の順序が異なるとデッドロックになる
// ・データバインディング機構は恐らく UI スレッドを使う
// 　→ワーカースレッドで Dispatcher.Invoke() とロックをしたい場合は、Dispatcher.Invoke() してからロックする
// 　　（TargetFolderInfos をロックしながら Dispatcher.Invoke() するのはダメ）
// 　　（TargetFolderInfos をロックしながら mLogWriter.ShowLogMessage() でメッセージボックスを表示するのもダメ）
// 　→UI スレッドでロックをしたい場合はそのままロックする
// ----------------------------------------------------------------------------

using Livet;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.YukaListerModels
{
	public class ProjectModel : NotificationObject
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public ProjectModel()
		{
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダー追加
		// 指定された親フォルダーのみを追加し、サブフォルダーは追加しない
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public async Task AddTargetFolderAsync(String parentFolder)
		{
			await Task.Run(() =>
			{
#if DEBUGz
				Thread.Sleep(2000);
#endif
				// フォルダーチェック
				if (String.IsNullOrEmpty(parentFolder))
				{
					throw new Exception("追加するフォルダーの名前が空です。");
				}
				if (!Directory.Exists(parentFolder))
				{
					throw new Exception("指定されたフォルダーが存在しません：" + parentFolder);
				}

				// 親の重複チェック
				Boolean parentAdded = IndexOfTargetFolderInfoWithLock(parentFolder) >= 0;
				if (parentAdded)
				{
					throw new Exception(parentFolder + " は既に追加されています。");
				}

				// 親の追加
				TargetFolderInfo targetFolderInfo = new(parentFolder);
				lock (_targetFolderInfos)
				{
					_targetFolderInfos.Add(targetFolderInfo);
				}

				// 通知
				YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
				YukaListerModel.Instance.EnvModel.Sifolin.MainEvent.Set();
				SetLomolinTargetDrives();

				// スリープ状態のデバイスだとここで時間がかかる
				AdjustAutoTargetInfoIfNeeded(YlCommon.DriveLetter(parentFolder));
			});
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダーにサブフォルダー群を追加
		// --------------------------------------------------------------------
		public void AddTargetSubFolders(TargetFolderInfo parentFolder, List<TargetFolderInfo> subFolders)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(parentFolder.TargetPath);
				if (parentIndex < 0)
				{
					return;
				}
				_targetFolderInfos.InsertRange(parentIndex + 1, subFolders);
			}

			// サブフォルダーは非表示なのでアイテム数は変わらない、親のノブ表示が変わる
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
		}

		// --------------------------------------------------------------------
		// TargetFolderInfos の中から指定された FolderTaskDetail を持つ TargetFolderInfo を探す
		// --------------------------------------------------------------------
		public TargetFolderInfo? FindTargetFolderInfo(FolderTaskDetail folderTaskDetail)
		{
			lock (_targetFolderInfos)
			{
				return _targetFolderInfos.FirstOrDefault(x => x.FolderTaskDetail == folderTaskDetail);
			}
		}

		// --------------------------------------------------------------------
		// folders が既に TargetFolderInfos に追加されているかどうか
		// --------------------------------------------------------------------
		public Boolean IsTargetFolderAdded(List<TargetFolderInfo> folders)
		{
			lock (_targetFolderInfos)
			{
				for (Int32 i = 0; i < folders.Count; i++)
				{
					if (IndexOfTargetFolderInfoWithoutLock(folders[i].TargetPath) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダーから削除（サブフォルダー含む）
		// TargetFolderInfo のみの削除で、データベースはいじらない
		// --------------------------------------------------------------------
		public Boolean RemoveTargetFolders(String parentFolder)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(parentFolder);
				if (parentIndex < 0)
				{
					return false;
				}
				Debug.Assert(_targetFolderInfos[parentIndex].IsParent, "RemoveTargetFolders() not parent");
				_targetFolderInfos.RemoveRange(parentIndex, _targetFolderInfos[parentIndex].NumTotalFolders);
			}
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
			SetLomolinTargetDrives();
			return true;
		}

		// --------------------------------------------------------------------
		// FolderTaskStatus が Running の TargetFolderInfo を取得
		// --------------------------------------------------------------------
		public TargetFolderInfo? RunningTargetFolderInfo()
		{
			lock (_targetFolderInfos)
			{
				return _targetFolderInfos.FirstOrDefault(x => x.FolderTaskStatus == FolderTaskStatus.Running);
			}
		}

		// --------------------------------------------------------------------
		// サブフォルダーも含めて FolderSettingsStatus と FolderExcludeSettingsStatus を Unchecked にする
		// folder は IsParent でなくても構わない
		// --------------------------------------------------------------------
		public Boolean SetFolderSettingsStatusToUnchecked(String folder)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(folder);
				if (parentIndex < 0)
				{
					return false;
				}
				for (Int32 i = parentIndex; i < parentIndex + _targetFolderInfos[parentIndex].NumTotalFolders; i++)
				{
					_targetFolderInfos[i].FolderExcludeSettingsStatus = FolderExcludeSettingsStatus.Unchecked;
					_targetFolderInfos[i].FolderSettingsStatus = FolderSettingsStatus.Unchecked;
				}
			}

			// 通知
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
			//ListCancellationTokenSource?.Cancel();
			return true;
		}

		// --------------------------------------------------------------------
		// 指定ドライブの FolderTaskDetail を Remove にする
		// --------------------------------------------------------------------
		public Boolean SetFolderTaskDetailOfDriveToRemove(String driveLetter)
		{
			List<TargetFolderInfo> removeTargets;
			lock (_targetFolderInfos)
			{
				removeTargets = _targetFolderInfos.Where(x => x.IsParent && x.TargetPath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase)).ToList();
			}

			Boolean result = false;
			foreach (TargetFolderInfo removeTarget in removeTargets)
			{
				result |= SetFolderTaskDetailOfFolderToRemove(removeTarget.TargetPath);
			}
			return result;
		}

		// --------------------------------------------------------------------
		// サブフォルダーも含めて FolderTaskDetail を Remove にする
		// --------------------------------------------------------------------
		public Boolean SetFolderTaskDetailOfFolderToRemove(String parentFolder)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(parentFolder);
				if (parentIndex < 0)
				{
					return false;
				}
				Debug.Assert(_targetFolderInfos[parentIndex].IsParent, "SetFolderTaskDetailToRemove() not parent");
				for (Int32 i = parentIndex; i < parentIndex + _targetFolderInfos[parentIndex].NumTotalFolders; i++)
				{
					_targetFolderInfos[i].FolderTaskKind = FolderTaskKind.Remove;
					_targetFolderInfos[i].SetFolderTaskDetail(FolderTaskDetail.Remove);
					_targetFolderInfos[i].FolderTaskStatus = FolderTaskStatus.Queued;
				}
			}

			// 通知
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
			YukaListerModel.Instance.EnvModel.Sifolin.MainEvent.Set();
			AdjustAutoTargetInfoIfNeeded(YlCommon.DriveLetter(parentFolder));
			//ListCancellationTokenSource?.Cancel();
			return true;
		}

		// --------------------------------------------------------------------
		// すべての FolderTaskStatus.DoneInMemory を DoneInDisk にする
		// --------------------------------------------------------------------
		public void SetAllFolderTaskStatusToDoneInDisk()
		{
			lock (_targetFolderInfos)
			{
				for (Int32 i = 0; i < _targetFolderInfos.Count; i++)
				{
					if (_targetFolderInfos[i].FolderTaskStatus == FolderTaskStatus.DoneInMemory)
					{
						Debug.Assert(_targetFolderInfos[i].FolderTaskDetail == FolderTaskDetail.Done, "SetAllFolderTaskStatusToDoneInDisk() not done");
						_targetFolderInfos[i].FolderTaskStatus = FolderTaskStatus.DoneInDisk;
					}
				}
			}
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダーのうち、UI に表示するもののみを取得
		// --------------------------------------------------------------------
		public List<TargetFolderInfo> TargetFolderInfosVisible()
		{
			lock (_targetFolderInfos)
			{
				return _targetFolderInfos.Where(x => x.Visible).ToList();
			}
		}

		// --------------------------------------------------------------------
		// FolderTaskStatus が DoneInDisk 以外の TargetFolderInfo を取得
		// --------------------------------------------------------------------
		public TargetFolderInfo? UndoneTargetFolderInfo()
		{
			lock (_targetFolderInfos)
			{
				return _targetFolderInfos.FirstOrDefault(x => x.FolderTaskStatus != FolderTaskStatus.DoneInDisk);
			}
		}

		// --------------------------------------------------------------------
		// 親フォルダーの IsOpen をサブフォルダーの Visible に反映
		// --------------------------------------------------------------------
		public void UpdateTargetFolderInfosVisible(TargetFolderInfo parentFolder)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(parentFolder.TargetPath);
				if (parentIndex < 0)
				{
					return;
				}

				if (parentFolder.IsOpen == true)
				{
					SetTargetFolderInfosVisibleToTrue(parentIndex);
				}
				else
				{
					// すべてのサブフォルダーを非表示にする
					for (Int32 i = parentIndex + 1; i < parentIndex + parentFolder.NumTotalFolders; i++)
					{
						_targetFolderInfos[i].Visible = false;
					}
				}
			}
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// ゆかり検索対象フォルダー（全部）
		// この中から絞って VM の表示用に渡す
		// アクセス時はロックが必要
		private readonly List<TargetFolderInfo> _targetFolderInfos = new();

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 自動追加フォルダーを最適化
		// --------------------------------------------------------------------
		private void AdjustAutoTargetInfoIfNeeded(String driveLetter)
		{
			Debug.Assert(driveLetter.Length == 2, "AdjustAutoTargetInfoIfNeeded() bad driveLetter");
			if (!IsAutoTargetDrive(driveLetter))
			{
				return;
			}

			AutoTargetInfo autoTargetInfo = new(driveLetter);
			lock (_targetFolderInfos)
			{
				IEnumerable<TargetFolderInfo> targets
						= _targetFolderInfos.Where(x => x.IsParent && x.FolderTaskKind == FolderTaskKind.Add && x.TargetPath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase));
				foreach (TargetFolderInfo target in targets)
				{
					autoTargetInfo.Folders.Add(YlCommon.WithoutDriveLetter(target.TargetPath));
				}
			}
			autoTargetInfo.Folders.Sort();
			autoTargetInfo.Save();
		}

		// --------------------------------------------------------------------
		// TargetFolderInfos の中から path を持つ TargetFolderInfo を探してインデックスを返す
		// --------------------------------------------------------------------
		private Int32 IndexOfTargetFolderInfoWithLock(String path)
		{
			lock (_targetFolderInfos)
			{
				return IndexOfTargetFolderInfoWithoutLock(path);
			}
		}

		// --------------------------------------------------------------------
		// TargetFolderInfos の中から path を持つ TargetFolderInfo を探してインデックスを返す
		// 呼び出し元において lock(TargetFolderInfos) 必須
		// ゆかりすたー METEOR の FindTargetFolderInfo2Ex3All() に相当
		// --------------------------------------------------------------------
		private Int32 IndexOfTargetFolderInfoWithoutLock(String path)
		{
			Debug.Assert(Monitor.IsEntered(_targetFolderInfos), "IndexOfTargetFolderInfoWithoutLock() not locked");
			for (Int32 i = 0; i < _targetFolderInfos.Count; i++)
			{
				if (YlCommon.IsSamePath(path, _targetFolderInfos[i].TargetPath))
				{
					return i;
				}
			}
			return -1;
		}

		// --------------------------------------------------------------------
		// 自動追加対象のドライブかどうか
		// --------------------------------------------------------------------
		private static Boolean IsAutoTargetDrive(String driveLetter)
		{
			DriveInfo driveInfo = new(driveLetter);
			if (!driveInfo.IsReady)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Verbose, "IsAutoTargetDrive() 準備ができていない：" + driveLetter);
				return false;
			}

			// リムーバブルドライブのみを対象としたいが、ポータブル HDD/SSD も Fixed 扱いになるため、Fixed も対象とする
			switch (driveInfo.DriveType)
			{
				case DriveType.Fixed:
				case DriveType.Removable:
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Verbose, "IsAutoTargetDrive() 対象：" + driveLetter);
					return true;
				default:
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Verbose, "IsAutoTargetDrive() 非対象：" + driveLetter + ", " + driveInfo.DriveType.ToString());
					return false;
			}
		}

		// --------------------------------------------------------------------
		// Lomolin の TargetDrives を設定
		// --------------------------------------------------------------------
		private void SetLomolinTargetDrives()
		{
			List<TargetFolderInfo> parents;
			lock (_targetFolderInfos)
			{
				parents = _targetFolderInfos.Where(x => x.IsParent).ToList();
			}
			List<String> drives = new();

			// C ドライブはデータベースを保持しているので常に測定対象とする
			drives.Add("C");

			foreach (TargetFolderInfo parent in parents)
			{
				if (drives.FirstOrDefault(x => x == parent.TargetPath[0..1].ToUpper()) == null)
				{
					drives.Add(parent.TargetPath[0..1].ToUpper());
				}
			}

			YukaListerModel.Instance.EnvModel.Lomolin.TargetDrives = String.Join(',', drives);
		}

		// --------------------------------------------------------------------
		// サブフォルダーの Visible を true に設定する
		// --------------------------------------------------------------------
		private void SetTargetFolderInfosVisibleToTrue(Int32 parentIndex)
		{
			Debug.Assert(Monitor.IsEntered(_targetFolderInfos), "SetTargetFolderInfosVisibleToTrue() not locked");
			Int32 index = parentIndex + 1;
			while (index < parentIndex + _targetFolderInfos[parentIndex].NumTotalFolders)
			{
				_targetFolderInfos[index].Visible = true;
				if (_targetFolderInfos[index].IsOpen == true)
				{
					SetTargetFolderInfosVisibleToTrue(index);
				}
				index += _targetFolderInfos[index].NumTotalFolders;
			}
		}
	}
}
