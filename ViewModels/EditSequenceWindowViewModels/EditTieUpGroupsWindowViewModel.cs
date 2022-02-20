// ============================================================================
// 
// 複数タイアップグループ検索ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは EditSequenceWindow を使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditMasterWindowViewModels;

namespace YukaLister.ViewModels.EditSequenceWindowViewModels
{
	internal class EditTieUpGroupsWindowViewModel : EditSequenceWindowViewModel<TTieUpGroup>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EditTieUpGroupsWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TTieUpGroup> records, Boolean searchOnInitialize)
				: base(musicInfoContext, records, searchOnInitialize)
		{
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				// ヘルプ
				HelpCommandParameter = "SeriesnoSentaku";
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "複数タイアップグループ検索ウィンドウ初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウの ViewModel 作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TTieUpGroup> CreateEditMasterWindowViewModel()
		{
			return new EditTieUpGroupWindowViewModel(_musicInfoContext, _records);
		}
	}
}
