// ============================================================================
// 
// ゆかり検索対象フォルダーの情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media;

using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SharedMisc
{
	internal class TargetFolderInfo
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター（親フォルダー追加用）
		// --------------------------------------------------------------------
		public TargetFolderInfo(String parentPath)
		{
			// 引数
			ParentPath = parentPath.TrimEnd('\\');

			// 自動設定
			TargetPath = ParentPath;
			TargetPathLabel = ParentPath;
			Level = 0;
			_folderTaskKind = (Int32)FolderTaskKind.Add;
			_folderTaskDetail = (Int32)(YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error ? FolderTaskDetail.CacheToDisk : FolderTaskDetail.FindSubFolders);
			Visible = true;
		}

		// --------------------------------------------------------------------
		// コンストラクター（子フォルダー追加用）
		// --------------------------------------------------------------------
		public TargetFolderInfo(String parentPath, String targetPath, Int32 level)
		{
			Debug.Assert(level > 0, "TargetFolderInfo() bad level");
			Debug.Assert(parentPath[^1] != '\\', "TargetFolderInfo() parentPath ends '\\'");
			Debug.Assert(targetPath[^1] != '\\', "TargetFolderInfo() path ends '\\'");

			// 引数
			ParentPath = parentPath;
			TargetPath = targetPath;
			Level = level;

			// 自動設定
			_folderTaskKind = (Int32)FolderTaskKind.Add;
			_folderTaskDetail = (Int32)FolderTaskDetail.AddFileNames;
			TargetPathLabel = Path.GetFileName(TargetPath);
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// IsOpen が変更された時のイベントハンドラー
		public static IsOpenChanged IsOpenChanged { get; set; } = delegate { };

		// 対象フォルダーパス（末尾は '\\' ではない）
		public String TargetPath { get; }

		// 親フォルダーのパス（削除用）（親の場合は TargetPath と同じ値にすること）（末尾は '\\' ではない）
		public String ParentPath { get; }

		// 親フォルダーからの深さ（親フォルダーは 0）
		public Int32 Level { get; }

		// 親フォルダーかどうか
		public Boolean IsParent
		{
			get => Level == 0;
		}

		// サブフォルダーがあるかどうか
		public Boolean HasChildren
		{
			get => NumTotalFolders > 1;
		}

		// 自分＋サブフォルダーの数（サブフォルダーが無い場合は 1 となる）
		public Int32 NumTotalFolders { get; set; } = 1;

		// サブフォルダーがある場合のみ有効：サブフォルダーを表示しているかどうか
		private Boolean _isOpen;
		public Boolean? IsOpen
		{
			get
			{
				if (HasChildren)
				{
					return _isOpen;
				}
				return null;
			}
			set
			{
				if (!HasChildren && value == true)
				{
					return;
				}
				if (value != null && value != _isOpen)
				{
					_isOpen = (Boolean)value;
					IsOpenChanged(this);
				}
			}
		}

		// キャッシュ DB からディスク DB へコピーにコピー済かどうか
		// 親でない場合は、基本的には親フォルダーの IsCacheUsed と同じ値だが、更新すると false となる
		public Boolean IsCacheUsed { get; set; }

		// 操作の種類
		private volatile Int32 _folderTaskKind;
		public FolderTaskKind FolderTaskKind
		{
			get => (FolderTaskKind)_folderTaskKind;
		}

		// 操作の詳細
		private volatile Int32 _folderTaskDetail;
		public FolderTaskDetail FolderTaskDetail
		{
			get => (FolderTaskDetail)_folderTaskDetail;
		}

		// 動作状況
		public FolderTaskStatus FolderTaskStatus { get; set; } = FolderTaskStatus.Queued;

		// フォルダー除外設定の状態
		private FolderExcludeSettingsStatus _folderExcludeSettingsStatus = FolderExcludeSettingsStatus.Unchecked;
		public FolderExcludeSettingsStatus FolderExcludeSettingsStatus
		{
			get
			{
				if (_folderExcludeSettingsStatus == FolderExcludeSettingsStatus.Unchecked)
				{
					_folderExcludeSettingsStatus = YlCommon.DetectFolderExcludeSettingsStatus(TargetPath);
				}
				return _folderExcludeSettingsStatus;
			}
			set => _folderExcludeSettingsStatus = value;
		}

		// フォルダー設定の状態
		private FolderSettingsStatus _folderSettingsStatus = FolderSettingsStatus.Unchecked;
		public FolderSettingsStatus FolderSettingsStatus
		{
			get
			{
				if (_folderSettingsStatus == FolderSettingsStatus.Unchecked)
				{
					_folderSettingsStatus = YlCommon.DetectFolderSettingsStatus2Ex(TargetPath);
				}
				return _folderSettingsStatus;
			}
			set => _folderSettingsStatus = value;
		}

		// UI に表示するかどうか
		public Boolean Visible { get; set; }

		// 表示用：背景色
		public Brush Background
		{
			get => FolderTaskStatusLabelAndBrush().brush;
		}

		// 表示用：パス
		public String TargetPathLabel { get; }

		// 表示用：フォルダー設定の状態
		public String FolderSettingsStatusLabel
		{
			get => YlConstants.FOLDER_SETTINGS_STATUS_LABELS[(Int32)FolderSettingsStatus];
		}

		// 表示用：動作状況
		public String FolderTaskStatusLabel
		{
			get => FolderTaskStatusLabelAndBrush().label;
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// FolderTaskDetail を指定値に設定
		// --------------------------------------------------------------------
		public void SetFolderTaskDetail(FolderTaskDetail folderTaskDetail)
		{
			_folderTaskDetail = (Int32)folderTaskDetail;
		}

		// --------------------------------------------------------------------
		// FolderTaskKind を指定値に設定
		// --------------------------------------------------------------------
		public void SetFolderTaskKind(FolderTaskKind folderTaskKind)
		{
			_folderTaskKind = (Int32)folderTaskKind;
		}

		// --------------------------------------------------------------------
		// FolderTaskDetail を from から to に変更
		// 現在値が from と等しくない場合は変更しない
		// ユーザーの意思により現在値が別の値に変更されている場合に、ユーザーの意思を継続するための関数
		// --------------------------------------------------------------------
		public void SetFolderTaskDetail(FolderTaskDetail from, FolderTaskDetail to)
		{
			Interlocked.CompareExchange(ref _folderTaskDetail, (Int32)to, (Int32)from);
		}

		// --------------------------------------------------------------------
		// SetFolderTaskKind を from から to に変更
		// 現在値が from と等しくない場合は変更しない
		// ユーザーの意思により現在値が別の値に変更されている場合に、ユーザーの意思を継続するための関数
		// --------------------------------------------------------------------
		public void SetFolderTaskKind(FolderTaskKind from, FolderTaskKind to)
		{
			Interlocked.CompareExchange(ref _folderTaskKind, (Int32)to, (Int32)from);
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ボリュームシリアル番号のセパレーター（パスとして使えない文字）
		//private const String SEPARATOR = "|";

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 動作状況のラベルと背景色
		// --------------------------------------------------------------------
		private (String label, Brush brush) FolderTaskStatusLabelAndBrush()
		{
			// 対象外かどうか
			if (FolderExcludeSettingsStatus == FolderExcludeSettingsStatus.True)
			{
				return ("対象外", YlConstants.BRUSH_EXCLUDE);
			}

			String label;
			Brush brush;
			switch (FolderTaskStatus)
			{
				case FolderTaskStatus.Queued:
					if (IsCacheUsed)
					{
						if (FolderTaskDetail == FolderTaskDetail.Remove)
						{
							label = "削除予定";
						}
						else
						{
							label = "キャッシュ有効";
						}
					}
					else
					{
						switch (FolderTaskDetail)
						{
							case FolderTaskDetail.CacheToDisk:
							case FolderTaskDetail.FindSubFolders:
							case FolderTaskDetail.AddFileNames:
								label = "追加予定";
								break;
							case FolderTaskDetail.AddInfos:
								label = "ファイル名検索可";
								break;
							case FolderTaskDetail.Remove:
								label = "削除予定";
								break;
							case FolderTaskDetail.UpdateRemove:
							case FolderTaskDetail.UpdateFindSubFolders:
								label = "更新予定";
								break;
							case FolderTaskDetail.Done:
								Debug.Assert(false, "FolderTaskStatusLabelAndBrush() done but Queued");
								label = String.Empty;
								break;
							default:
								Debug.Assert(false, "FolderTaskStatusLabelAndBrush() bad FolderTaskDetail in FolderTaskStatus.Queued");
								label = String.Empty;
								break;
						}
					}
					brush = YlConstants.BRUSH_STATUS_QUEUED;
					break;
				case FolderTaskStatus.Running:
					switch (FolderTaskDetail)
					{
						case FolderTaskDetail.CacheToDisk:
							label = YlConstants.RUNNING_CACHE_TO_DISK;
							break;
						case FolderTaskDetail.FindSubFolders:
							label = YlConstants.RUNNING_FIND_SUB_FOLDERS;
							break;
						case FolderTaskDetail.AddFileNames:
							label = YlConstants.RUNNING_ADD_FILE_NAMES;
							break;
						case FolderTaskDetail.AddInfos:
							label = YlConstants.RUNNING_ADD_INFOS;
							break;
						case FolderTaskDetail.Remove:
							label = YlConstants.RUNNING_REMOVE;
							break;
						case FolderTaskDetail.Done:
							Debug.Assert(false, "FolderTaskStatusLabelAndBrush() done but Running");
							label = String.Empty;
							break;
						default:
							Debug.Assert(false, "FolderTaskStatusLabelAndBrush() bad FolderTaskDetail in FolderTaskStatus.Running");
							label = String.Empty;
							break;
					}
					brush = YlConstants.BRUSH_STATUS_RUNNING;
					break;
				case FolderTaskStatus.DoneInMemory:
					switch (FolderTaskKind)
					{
						case FolderTaskKind.Add:
							label = "追加準備完了";
							break;
						case FolderTaskKind.Remove:
							label = "削除準備完了";
							break;
						default:
							Debug.Assert(false, "FolderTaskStatusLabelAndBrush() bad FolderTaskKind in FolderTaskStatus.DoneInMemory");
							label = String.Empty;
							break;
					}
					brush = YlConstants.BRUSH_STATUS_RUNNING;
					break;
				case FolderTaskStatus.DoneInDisk:
					switch (FolderTaskKind)
					{
						case FolderTaskKind.Add:
							label = "追加完了";
							break;
						case FolderTaskKind.Remove:
							label = "削除完了";
							break;
						default:
							Debug.Assert(false, "FolderTaskStatusLabelAndBrush() bad FolderTaskKind in FolderTaskStatus.DoneInDisk");
							label = String.Empty;
							break;
					}
					brush = YlConstants.BRUSH_STATUS_DONE;
					break;
				default:
					Debug.Assert(false, "FolderTaskStatusLabelAndBrush() bad FolderTaskStatus");
					label = String.Empty;
					brush = YlConstants.BRUSH_STATUS_ERROR;
					break;
			}
			return (label, brush);
		}
	}
}
