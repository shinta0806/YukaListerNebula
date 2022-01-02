// ============================================================================
// 
// 制作会社一覧ウィンドウの ViewModel
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
	public class ViewMakersWindowViewModel : ViewMastersWindowViewModel<TMaker>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public ViewMakersWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TMaker> records, ObservableCollection<DataGridColumn> columns)
				: base(musicInfoContext, records, columns)
		{
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウビューモデルの作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TMaker> CreateEditMasterWindowViewModel()
		{
			return new EditMakerWindowViewModel(_musicInfoContext, _records);
		}

		// --------------------------------------------------------------------
		// 編集可能なマスター群の作成
		// --------------------------------------------------------------------
		protected override List<TMaker> CreateMasters()
		{
			return DbCommon.MastersForEdit(_records, SelectedMaster?.Name);
		}
	}
}
