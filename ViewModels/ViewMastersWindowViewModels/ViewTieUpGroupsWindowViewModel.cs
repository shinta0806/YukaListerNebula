// ============================================================================
// 
// シリーズ一覧ウィンドウの ViewModel
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
	public class ViewTieUpGroupsWindowViewModel : ViewMastersWindowViewModel<TTieUpGroup>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ViewTieUpGroupsWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TTieUpGroup> records, ObservableCollection<DataGridColumn> columns)
				: base(musicInfoContext, records, columns)
		{
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウビューモデルの作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TTieUpGroup> CreateEditMasterWindowViewModel()
		{
			return new EditTieUpGroupWindowViewModel(_musicInfoContext, _records);
		}

		// --------------------------------------------------------------------
		// 編集可能なマスター群の作成
		// --------------------------------------------------------------------
		protected override List<TTieUpGroup> CreateMasters()
		{
			return DbCommon.MastersForEdit(_records, SelectedMaster?.Name);
		}
	}
}
