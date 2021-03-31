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
using System.Windows.Media;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SharedMisc
{
	public class TargetFolderInfo
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public TargetFolderInfo(String parentPath)
		{
			// 引数
			ParentPath = parentPath;

			// 自動設定
			Path = ParentPath;
			PathLabel = ParentPath;
			Level = 0;
			//WindowsApi.GetVolumeInformation(ParentPath[0..3], null, 0, out UInt32 volumeSerialNumber, out UInt32 maximumComponentLength, out FSF fileSystemFlags, null, 0);
			//VolumeSerialNumber = volumeSerialNumber.ToString("X8") + SEPARATOR;
		}

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public TargetFolderInfo(String parentPath, String path, Int32 level)
		{
			Debug.Assert(level > 0, "TargetFolderInfo() bad level");

			// 引数
			//VolumeSerialNumber = volumeSerialNumber;
			ParentPath = parentPath;
			Path = path;
			Level = level;

			// 自動設定
			PathLabel = System.IO.Path.GetFileName(Path);
		}

		// ====================================================================
		// public static プロパティー
		// ====================================================================

		// IsOpen が変更された時のイベントハンドラー
		public static IsOpenChanged IsOpenChanged { get; set; } = delegate { };

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// フォルダーパス
		public String Path { get; }

		// 親フォルダーのパス（ソート用）（親の場合は Path と同じ値にすること）
		public String ParentPath { get; }

		// 親フォルダーからの深さ（親フォルダーは 0）
		public Int32 Level { get; }

		// サブフォルダーがあるかどうか
		public Boolean HasChildren { get; set; }

		// 自分＋サブフォルダーの数（サブフォルダーが無い場合は 1 となる）
		public Int32 NumTotalFolders { get; set; } = 1;

		// サブフォルダーがある場合のみ有効：サブフォルダーを表示しているかどうか
		private Boolean _isOpen;
		public Boolean? IsOpen
		{
			get
			{
				if (HasChildren && NumTotalFolders > 1)
				{
					return _isOpen;
				}
				return null;
			}
			set
			{
				if (HasChildren && NumTotalFolders > 1 && value != null && value != _isOpen)
				{
					_isOpen = (Boolean)value;
					IsOpenChanged(this);
				}
			}
		}

		// ボリュームシリアル番号とセパレーター
		//public String VolumeSerialNumber { get; set; }

		// キャッシュ DB からディスク DB へコピーにコピー済かどうか
		// 親でない場合は、常に親フォルダーの IsCacheUsed と同じ値とする
		public Boolean IsCacheUsed { get; set; }

		// 操作の種類
		public FolderTaskKind FolderTaskKind { get; set; }

		// 操作の詳細
		public FolderTaskDetail FolderTaskDetail { get; set; }

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
					_folderExcludeSettingsStatus = YlCommon.DetectFolderExcludeSettingsStatus(Path);
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
					_folderSettingsStatus = YlCommon.DetectFolderSettingsStatus2Ex(Path);
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
			get => FolderTaskStatusLabelAndBrush().Item2;
		}

		// 表示用：パス
		public String PathLabel { get; }

		// 表示用：フォルダー設定の状態
		public String FolderSettingsStatusLabel
		{
			get => YlConstants.FOLDER_SETTINGS_STATUS_TEXTS[(Int32)FolderSettingsStatus];
		}

		// 表示用：動作状況
		public String FolderTaskStatusLabel
		{
			get => FolderTaskStatusLabelAndBrush().Item1;
		}

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// ソート用比較関数
		// 例えば @"C:\A" 配下と @"C:\A 2" を正しく並べ替えるために ParentPath が必要
		// --------------------------------------------------------------------
		public static Int32 Compare(TargetFolderInfo lhs, TargetFolderInfo rhs)
		{
			if (lhs.ParentPath != rhs.ParentPath)
			{
				return String.Compare(lhs.ParentPath, rhs.ParentPath);
			}
			return String.Compare(lhs.Path, rhs.Path);
		}
#endif

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// ボリュームシリアル番号のセパレーター（パスとして使えない文字）
		private const String SEPARATOR = "|";

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 動作状況のラベルと背景色
		// --------------------------------------------------------------------
		private (String, Brush) FolderTaskStatusLabelAndBrush()
		{
			// 全体がエラーの場合はフォルダーもエラー
			if (YukaListerModel.Instance.EnvModel.YukaListerStatus == YukaListerStatus.Error)
			{
				return ("エラー解決待ち", YlConstants.BRUSH_STATUS_ERROR);
			}

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
						label = "キャッシュ有効";
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
				case FolderTaskStatus.Error:
					label = "エラー";
					brush = YlConstants.BRUSH_STATUS_ERROR;
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
