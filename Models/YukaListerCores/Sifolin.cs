// ============================================================================
// 
// ネビュラコア：検索データ作成担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
					}

				}
				catch (OperationCanceledException)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " の稼働を終了します。");
					return;
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, GetType().Name + "稼働時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// メモリ DB 更新フラグ
		private Boolean _isMemoryDbDirty;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// キャッシュ DB からディスク DB へコピー
		// --------------------------------------------------------------------
		private void CacheToDisk(TargetFolderInfo targetFolderInfo)
		{
			// ToDo: 未実装
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.FindSubFolders;
			Debug.WriteLine("キャッシュ活用完了：" + targetFolderInfo.ParentPath + " 配下");
		}

		// --------------------------------------------------------------------
		// リスト化対象フォルダーのサブフォルダーを列挙
		// --------------------------------------------------------------------
		private List<TargetFolderInfo> EnumSubFolders(TargetFolderInfo parentFolder)
		{
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
					subFolder.FolderTaskDetail = FolderTaskDetail.AddFileName;
					folders.Add(subFolder);

					// サブフォルダーのサブフォルダー
					List<TargetFolderInfo> subSubFolders = EnumSubFolders(subFolder);
					folders.AddRange(subSubFolders);

					// サブフォルダーの情報
					subFolder.HasChildren = subSubFolders.Any();
					subFolder.NumTotalFolders = 1 + subSubFolders.Count();
				}
			}
			catch (Exception)
			{
			}
			return folders;
		}

#if false
		// --------------------------------------------------------------------
		// リスト化対象フォルダーのサブフォルダーを列挙
		// SearchOption.AllDirectories 付きで Directory.GetDirectories を呼び出すと、
		// ごみ箱のようにアクセス権限の無いフォルダーの中も列挙しようとして例外が
		// 発生し中断してしまう。
		// 面倒だが 1 フォルダーずつ列挙する
		// --------------------------------------------------------------------
		private List<TargetFolderInfo> EnumSubFolders(String parentFolder)
		{
			List<TargetFolderInfo> folders = new();
			EnumSubFoldersSub(folders, parentFolder);
			return folders;
		}

		// --------------------------------------------------------------------
		// EnumSubFolders() の子関数
		// --------------------------------------------------------------------
		private void EnumSubFoldersSub(List<TargetFolderInfo> folders, TargetFolderInfo folder)
		{
			YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();

			// 指定フォルダー
			folders.Add(folder);

			// 指定フォルダーのサブフォルダー
			try
			{
				String[] subFolders = Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly);
				foreach (String subFolder in subFolders)
				{
					EnumSubFoldersSub(folders, subFolder);
				}
			}
			catch (Exception)
			{
			}
		}
#endif

		// --------------------------------------------------------------------
		// サブフォルダーを検索して追加
		// --------------------------------------------------------------------
		private void FindSubFolders(TargetFolderInfo targetFolderInfo)
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
			targetFolderInfo.FolderTaskDetail = FolderTaskDetail.AddFileName;

			// その他
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridDirty = true;
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.Path
					+ "\n" + targetFolderInfo.NumTotalFolders + " 個のフォルダーを検索対象に追加予定としました。");
		}

	}
}
