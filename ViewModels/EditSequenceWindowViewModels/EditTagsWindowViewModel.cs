// ============================================================================
// 
// 複数タグ検索ウィンドウの ViewModel
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
	public class EditTagsWindowViewModel : EditSequenceWindowViewModel<TTag>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EditTagsWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TTag> records, Boolean searchOnInitialize)
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
				HelpCommandParameter = "TagnoSentaku";

			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数タグ検索ウィンドウ初期化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウの ViewModel 作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TTag> CreateEditMasterWindowViewModel()
		{
			return new EditTagWindowViewModel(_musicInfoContext, _records);
		}
	}
}
