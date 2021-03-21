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

		// ゆかり検索対象フォルダー（全部）
		// この中から絞って VM の表示用に渡す
		// アクセス時はロックが必要
		public List<TargetFolderInfo> TargetFolderInfos { get; set; } = new();

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
		public void AddFolder(String parentFolder)
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
			Boolean parentAdded;
			lock (TargetFolderInfos)
			{
				parentAdded = FindTargetFolderInfo(parentFolder) >= 0;
			}
			if (parentAdded)
			{
				throw new Exception(parentFolder + " は既に追加されています。");
			}

			// 親の追加
			TargetFolderInfo targetFolderInfo = new(parentFolder, parentFolder, 0);
			targetFolderInfo.FolderTaskKind = FolderTaskKind.Add;
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.FindSubFolders;
			targetFolderInfo.Visible = true;
			lock (TargetFolderInfos)
			{
				TargetFolderInfos.Add(targetFolderInfo);
				TargetFolderInfos.Sort(TargetFolderInfo.Compare);
			}

			// 通知
			ListCancellationTokenSource?.Cancel();
		}

		// --------------------------------------------------------------------
		// TargetFolderInfos の中から path を持つ TargetFolderInfo を探してインデックスを返す
		// 呼び出し元において lock(TargetFolderInfos) 必須
		// --------------------------------------------------------------------
		public Int32 FindTargetFolderInfo(String path)
		{
			Debug.Assert(Monitor.IsEntered(TargetFolderInfos), "FindTargetFolderInfo() not locked");
			for (Int32 i = 0; i < TargetFolderInfos.Count; i++)
			{
				if (YlCommon.IsSamePath(path, TargetFolderInfos[i].Path))
				{
					return i;
				}
			}

			return -1;
		}

		// --------------------------------------------------------------------
		// ゆかり検索対象フォルダーのうち、UI に表示するもののみを取得
		// --------------------------------------------------------------------
		public List<TargetFolderInfo> TargetFolderInfosVisible()
		{
			List<TargetFolderInfo> targetFolderInfosVisible;
			lock (TargetFolderInfos)
			{
				targetFolderInfosVisible = TargetFolderInfos.Where(x => x.Visible).ToList();
			}
			return targetFolderInfosVisible;
		}

	}
}
