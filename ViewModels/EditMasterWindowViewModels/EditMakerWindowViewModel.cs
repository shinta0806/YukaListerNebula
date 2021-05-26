// ============================================================================
// 
// 制作会社詳細編集ウィンドウの ViewModel
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
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditMakerWindowViewModel : EditMasterWindowViewModel<TMaker>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditMakerWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TMaker> records)
				: base(musicInfoContext, records)
		{
		}

		// ====================================================================
		// public メンバー関数
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
				NameHint = "株式会社・有限会社などの法人格は入力しないで下さい。";
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "制作会社詳細情報編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
	}
}
