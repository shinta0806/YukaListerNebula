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

using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.YukaListerModels
{
	public class ProjectModel : NotificationObject
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ProjectModel()
		{

		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// リスト更新タスク安全中断用
		public CancellationTokenSource? ListCancellationTokenSource { get; set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダー追加
		// 指定された親フォルダーのみを追加し、サブフォルダーは追加しない
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public void AddTargetFolder(String parentFolder)
		{
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
			targetFolderInfo.FolderTaskKind = FolderTaskKind.Add;
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.CacheToDisk;
			targetFolderInfo.Visible = true;
			lock (_targetFolderInfos)
			{
				_targetFolderInfos.Add(targetFolderInfo);
				//TargetFolderInfos.Sort(TargetFolderInfo.Compare);
			}

			// 通知
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
			YukaListerModel.Instance.EnvModel.Sifolin.MainEvent.Set();
			ListCancellationTokenSource?.Cancel();
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダーにサブフォルダー群を追加
		// --------------------------------------------------------------------
		public void AddTargetSubFolders(TargetFolderInfo parentFolder, List<TargetFolderInfo> subFolders)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(parentFolder.Path);
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
					if (IndexOfTargetFolderInfoWithoutLock(folders[i].Path) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダーから削除
		// --------------------------------------------------------------------
		public Boolean RemoveTargetFolder(String path)
		{
			lock (_targetFolderInfos)
			{
				Int32 index = IndexOfTargetFolderInfoWithoutLock(path);
				if (index < 0)
				{
					return false;
				}
				_targetFolderInfos.RemoveAt(index);
			}
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;
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
		// すべての DoneInMemory を DoneInDisk にする
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
		// 親フォルダーの IsOpen をサブフォルダーの Visible に反映
		// --------------------------------------------------------------------
		public void UpdateTargetFolderInfosVisible(TargetFolderInfo parentFolder)
		{
			lock (_targetFolderInfos)
			{
				Int32 parentIndex = IndexOfTargetFolderInfoWithoutLock(parentFolder.Path);
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
		// private メンバー変数
		// ====================================================================

		// ゆかり検索対象フォルダー（全部）
		// この中から絞って VM の表示用に渡す
		// アクセス時はロックが必要
		private List<TargetFolderInfo> _targetFolderInfos = new();

		// ====================================================================
		// private メンバー関数
		// ====================================================================

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
				if (YlCommon.IsSamePath(path, _targetFolderInfos[i].Path))
				{
					return i;
				}
			}
			return -1;
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
