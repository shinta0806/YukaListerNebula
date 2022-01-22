// ============================================================================
// 
// 人物詳細編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは EditMasterWindow を使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditPersonWindowViewModel : EditMasterWindowViewModel<TPerson>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EditPersonWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TPerson> records)
				: base(musicInfoContext, records)
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
				// ヒント
				NameHint = "一人分の人物名のみを入力して下さい（複数名をまとめないで下さい）。";
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "人物詳細情報編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// レコード無効化
		// --------------------------------------------------------------------
		protected override void Invalidate(TPerson master)
		{
			base.Invalidate(master);

			// タイアップグループ紐付け
			DbCommon.InvalidateSequenceByLinkId(_musicInfoContext.ArtistSequences, master.Id);
			DbCommon.InvalidateSequenceByLinkId(_musicInfoContext.LyristSequences, master.Id);
			DbCommon.InvalidateSequenceByLinkId(_musicInfoContext.ComposerSequences, master.Id);
			DbCommon.InvalidateSequenceByLinkId(_musicInfoContext.ArrangerSequences, master.Id);
			_musicInfoContext.SaveChanges();
		}
	}
}
