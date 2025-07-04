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

using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.YukaListerModels;

internal class ProjectModel : NotificationObject
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
			YlModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
			YlModel.Instance.EnvModel.Sifolin.MainEvent.Set();
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
		YlModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
	}

	// --------------------------------------------------------------------
	// targetFolderInfo の NumTotalFolders が delta 増減したので、上位の NumTotalFolders を調整する
	// targetFolderInfo の NumTotalFolders はいじらない
	// --------------------------------------------------------------------
	public void AdjustNumTotalFolders(TargetFolderInfo targetFolderInfo, Int32 delta)
	{
		lock (_targetFolderInfos)
		{
			Int32 index = IndexOfTargetFolderInfoWithoutLock(targetFolderInfo.TargetPath);
			if (index < 0)
			{
				return;
			}
			AdjustNumTotalFoldersWithoutLock(index, delta);
		}
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
	// parentFolder は IsParent である必要がある
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
		YlModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
		SetLomolinTargetDrives();
		return true;
	}

	// --------------------------------------------------------------------
	// ゆかり検索対象フォルダーからサブフォルダーを削除（当該フォルダーは削除しない）
	// folder は IsParent でなくても構わない
	// TargetFolderInfo のみの削除で、データベースはいじらない
	// --------------------------------------------------------------------
	public Boolean RemoveTargetSubFolders(String folder)
	{
		lock (_targetFolderInfos)
		{
			Int32 index = IndexOfTargetFolderInfoWithoutLock(folder);
			if (index < 0)
			{
				return false;
			}
			if (_targetFolderInfos[index].NumTotalFolders == 1)
			{
				return false;
			}
			_targetFolderInfos.RemoveRange(index + 1, _targetFolderInfos[index].NumTotalFolders - 1);
			AdjustNumTotalFoldersWithoutLock(index, 1 - _targetFolderInfos[index].NumTotalFolders);
			_targetFolderInfos[index].NumTotalFolders = 1;
			_targetFolderInfos[index].IsOpen = false;
		}
		YlModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
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
		YlModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
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
		YlModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
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
	// parentFolder は IsParent である必要がある
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
				_targetFolderInfos[i].SetFolderTaskKind(FolderTaskKind.Remove);
				_targetFolderInfos[i].SetFolderTaskDetail(FolderTaskDetail.Remove);
				_targetFolderInfos[i].FolderTaskStatus = FolderTaskStatus.Queued;
			}
		}

		// 通知
		YlModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
		YlModel.Instance.EnvModel.Sifolin.MainEvent.Set();
		AdjustAutoTargetInfoIfNeeded(YlCommon.DriveLetter(parentFolder));
		return true;
	}

	// --------------------------------------------------------------------
	// FolderTaskDetail を UpdateRemove にする（サブフォルダーは含めない）
	// folder は IsParent でなくても構わない
	// --------------------------------------------------------------------
	public Boolean SetFolderTaskDetailToUpdateRemove(String folder)
	{
		lock (_targetFolderInfos)
		{
			Int32 index = IndexOfTargetFolderInfoWithoutLock(folder);
			if (index < 0)
			{
				return false;
			}
			if (_targetFolderInfos[index].FolderTaskKind == FolderTaskKind.Remove)
			{
				return false;
			}
			_targetFolderInfos[index].SetFolderTaskKind(FolderTaskKind.Update);
			_targetFolderInfos[index].SetFolderTaskDetail(FolderTaskDetail.UpdateRemove);
			_targetFolderInfos[index].FolderTaskStatus = FolderTaskStatus.Queued;
			_targetFolderInfos[index].IsCacheUsed = false;
		}

		// 通知
		YlModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
		YlModel.Instance.EnvModel.Sifolin.MainEvent.Set();
		return true;
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
		YlModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
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
					= _targetFolderInfos.Where(x => x.IsParent && (x.FolderTaskKind == FolderTaskKind.Add || x.FolderTaskKind == FolderTaskKind.Update)
					&& x.TargetPath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase));
			foreach (TargetFolderInfo target in targets)
			{
				autoTargetInfo.Folders.Add(YlCommon.WithoutDriveLetter(target.TargetPath));
			}
		}
		autoTargetInfo.Folders.Sort();
		autoTargetInfo.Save();
	}

	// --------------------------------------------------------------------
	// index 番目の TargetFolderInfos の NumTotalFolders が delta 増減したので、上位の NumTotalFolders を調整する
	// index 番目の TargetFolderInfos の NumTotalFolders はいじらない
	// --------------------------------------------------------------------
	private void AdjustNumTotalFoldersWithoutLock(Int32 changingIndex, Int32 delta)
	{
		Debug.Assert(Monitor.IsEntered(_targetFolderInfos), "AdjustNumTotalFoldersWithoutLock() not locked");
#if DEBUGz
		for (Int32 i = 0; i < changingIndex; i++)
		{
			YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Verbose, "AdjustNumTotalFoldersWithoutLock() Before " + _targetFolderInfos[i].NumTotalFolders + " " + _targetFolderInfos[i].TargetPath);
		}
#endif
		for (Int32 i = 0; i < changingIndex; i++)
		{
			if (_targetFolderInfos[i].ParentPath == _targetFolderInfos[changingIndex].ParentPath
					&& _targetFolderInfos[i].Level < _targetFolderInfos[changingIndex].Level)
			{
				_targetFolderInfos[i].NumTotalFolders += delta;
			}
		}
#if DEBUGz
		for (Int32 i = 0; i < changingIndex; i++)
		{
			YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Verbose, "AdjustNumTotalFoldersWithoutLock() After " + _targetFolderInfos[i].NumTotalFolders + " " + _targetFolderInfos[i].TargetPath);
		}
#endif
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
			Log.Debug("IsAutoTargetDrive() 準備ができていない：" + driveLetter);
			return false;
		}

		// リムーバブルドライブのみを対象としたいが、ポータブル HDD/SSD も Fixed 扱いになるため、Fixed も対象とする
		switch (driveInfo.DriveType)
		{
			case DriveType.Fixed:
			case DriveType.Removable:
				Log.Debug("IsAutoTargetDrive() 対象：" + driveLetter);
				return true;
			default:
				Log.Debug("IsAutoTargetDrive() 非対象：" + driveLetter + ", " + driveInfo.DriveType.ToString());
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

		YlModel.Instance.EnvModel.Lomolin.TargetDrives = String.Join(',', drives);
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
