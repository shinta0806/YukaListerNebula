// ============================================================================
// 
// 複数人物編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは EditSequenceWindow を使う。
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using Microsoft.EntityFrameworkCore;
using Shinta;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using YukaLister.Models;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditMasterWindowViewModels;

namespace YukaLister.ViewModels.EditSequenceWindowViewModels
{
	public class EditPeopleWindowViewModel : EditSequenceWindowViewModel<TPerson>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditPeopleWindowViewModel(MusicInfoContext musicInfoContext, DbSet<TPerson> records, String caption)
				: base(musicInfoContext, records, caption, "人物")
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
				// ヘルプ
				HelpCommandParameter = "KasyuSakushisyaSakkyokusyaHenkyokusyanoSentaku";

			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数人物編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウの ViewModel 作成
		// --------------------------------------------------------------------
		protected override EditMasterWindowViewModel<TPerson> CreateEditMasterWindowViewModel()
		{
			return new EditPersonWindowViewModel(_musicInfoContext, _records);
		}

	}
}
