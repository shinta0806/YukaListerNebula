// ============================================================================
// 
// ゆかり検索対象フォルダーの情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public TargetFolderInfo(String parentPath, String path, Int32 level)
		{
			// 引数
			ParentPath = parentPath;
			Path = path;
			Level = level;

			// 初期化
			NumTotalFolders = 1;
			FolderTaskStatus = FolderTaskStatus.Queued;
		}

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
		public Int32 NumTotalFolders { get; set; }

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
					//if (IsOpenChanged != null)
					//{
					//	IsOpenChanged(this);
					//}
				}
			}
		}

		// 操作の種類
		public FolderTaskKind FolderTaskKind { get; set; }

		// 操作の詳細
		public FolderTaskDetail FolderTaskDetail { get; set; }

		// 動作状況
		public FolderTaskStatus FolderTaskStatus { get; set; }

		// UI に表示するかどうか
		public Boolean Visible { get; set; }

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


	}
}
