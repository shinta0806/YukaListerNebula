// ============================================================================
// 
// タイアップグループ詳細編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは EditMasterWindow を使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Diagnostics;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditTieUpGroupWindowViewModel : EditMasterWindowViewModel<TTieUpGroup>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditTieUpGroupWindowViewModel(MusicInfoContext musicInfoContext, DbSet<TTieUpGroup> records)
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
				NameHint = "シリーズ名に「" + YlConstants.TIE_UP_GROUP_SUFFIX + "」は含めないで下さい。";
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "制作会社詳細情報編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// レコード無効化
		// --------------------------------------------------------------------
		protected override void Invalidate(TTieUpGroup master)
		{
			base.Invalidate(master);

			// タイアップグループ紐付け
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUpGroupSequence> tieUpGroupSequences);
			DbCommon.InvalidateSequenceByLinkId(tieUpGroupSequences, master.Id);
			_musicInfoContext.SaveChanges();
		}
	}
}
