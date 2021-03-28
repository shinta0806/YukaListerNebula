// ============================================================================
// 
// ゆかり検索対象フォルダーの情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Diagnostics;

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
		// public プロパティー
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

		// UI に表示するかどうか
		public Boolean Visible { get; set; }

		// 表示用：パス
		public String PathLabel { get; }

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

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

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// ボリュームシリアル番号のセパレーター（パスとして使えない文字）
		private const String SEPARATOR = "|";
	}
}
