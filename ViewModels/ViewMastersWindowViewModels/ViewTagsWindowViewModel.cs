// ============================================================================
// 
// タグ一覧ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは ViewMastersWindow を使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.ViewModels.EditMasterWindowViewModels;

namespace YukaLister.ViewModels.ViewMastersWindowViewModels
{
	public class ViewTagsWindowViewModel : ViewMastersWindowViewModel<TTag>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ViewTagsWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TTag> records, ObservableCollection<DataGridColumn> columns)
				: base(musicInfoContext, records, columns)
		{
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウビューモデルの作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TTag> CreateEditMasterWindowViewModel()
		{
			return new EditTagWindowViewModel(_musicInfoContext, _records);
		}

		// --------------------------------------------------------------------
		// 編集可能なマスター群の作成
		// --------------------------------------------------------------------
		protected override List<TTag> CreateMasters()
		{
			return DbCommon.MastersForEdit(_records, SelectedMaster?.Name);
		}
	}
}
