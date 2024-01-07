// ============================================================================
// 
// 楽曲一覧ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは ViewMastersWindow を使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.ViewModels.EditMasterWindowViewModels;

namespace YukaLister.ViewModels.ViewMastersWindowViewModels
{
	internal class ViewSongsWindowViewModel : ViewMastersWindowViewModel<TSong>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public ViewSongsWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TSong> records, ObservableCollection<DataGridColumn> columns)
				: base(musicInfoContext, records, columns)
		{
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウビューモデルの作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TSong> CreateEditMasterWindowViewModel()
		{
			return new EditSongWindowViewModel(_musicInfoContext, _records);
		}

		// --------------------------------------------------------------------
		// 編集可能なマスター群の作成
		// --------------------------------------------------------------------
		protected override List<TSong> CreateMasters()
		{
			return DbCommon.MastersForEdit(_records, SelectedMaster?.Name);
		}

		// --------------------------------------------------------------------
		// ウィンドウを開くメッセージ
		// --------------------------------------------------------------------
		protected override String MessageKeyOpenEditWindow()
		{
			return YlConstants.MESSAGE_KEY_OPEN_EDIT_SONG_WINDOW;
		}
	}
}
