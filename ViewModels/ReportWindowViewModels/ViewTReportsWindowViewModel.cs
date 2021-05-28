// ============================================================================
// 
// リスト問題報告ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.ReportWindowViewModels
{
	public class ViewTReportsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ViewTReportsWindowViewModel()
		{
			_reportContext = ReportContext.CreateContext(out _reports);
			_reportContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 要対応のみ表示
		private Boolean _showOnlyOpened;
		public Boolean ShowOnlyOpened
		{
			get => _showOnlyOpened;
			set
			{
				if (RaisePropertyChangedIfSet(ref _showOnlyOpened, value))
				{
					UpdateTReports();
				}
			}
		}

		// すべて表示
		private Boolean _showAll;
		public Boolean ShowAll
		{
			get => _showAll;
			set
			{
				if (RaisePropertyChangedIfSet(ref _showAll, value))
				{
					UpdateTReports();
				}
			}
		}

		// 報告群（表示用）
		private List<TReport>? _reportsVisible;
		public List<TReport>? ReportsVisible
		{
			get => _reportsVisible;
			set => RaisePropertyChangedIfSet(ref _reportsVisible, value);
		}

		// 選択された報告
		private TReport? _selectedReport;
		public TReport? SelectedReport
		{
			get => _selectedReport;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedReport, value))
				{
					ButtonEditDetailClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

		#region データグリッドダブルクリックの制御
		private ViewModelCommand? _dataGridDoubleClickedCommand;

		public ViewModelCommand DataGridDoubleClickedCommand
		{
			get
			{
				if (_dataGridDoubleClickedCommand == null)
				{
					_dataGridDoubleClickedCommand = new ViewModelCommand(DataGridDoubleClicked);
				}
				return _dataGridDoubleClickedCommand;
			}
		}

		public void DataGridDoubleClicked()
		{
			try
			{
				EditDetail();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 詳細ボタンの制御
		private ViewModelCommand? _buttonEditDetailClickedCommand;

		public ViewModelCommand ButtonEditDetailClickedCommand
		{
			get
			{
				if (_buttonEditDetailClickedCommand == null)
				{
					_buttonEditDetailClickedCommand = new ViewModelCommand(ButtonEditDetailClicked, CanButtonEditDetailClicked);
				}
				return _buttonEditDetailClickedCommand;
			}
		}

		public Boolean CanButtonEditDetailClicked()
		{
			return SelectedReport != null;
		}

		public void ButtonEditDetailClicked()
		{
			try
			{
				EditDetail();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "詳細ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

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
				// タイトルバー
				Title = "リスト問題報告一覧";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
				// 絞り込み
				ShowOnlyOpened = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト問題報告一覧ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// リスト問題報告データベース
		private readonly ReportContext _reportContext;

		// リスト問題テーブル
		private readonly DbSet<TReport> _reports;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 詳細編集
		// --------------------------------------------------------------------
		private void EditDetail()
		{
			if (SelectedReport == null)
			{
				return;
			}

			// ViewModel 経由でリスト問題報告編集ウィンドウを開く
			using EditReportWindowViewModel editReportWindowViewModel = new(SelectedReport);
			Messenger.Raise(new TransitionMessage(editReportWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_REPORT_WINDOW));

			// 報告が更新された場合は一覧を更新
			if (editReportWindowViewModel.IsOk)
			{
				UpdateTReports();
			}
		}

		// --------------------------------------------------------------------
		// TReports を現在のオプションに合わせて上書き
		// --------------------------------------------------------------------
		private void UpdateTReports()
		{
			ReportsVisible = _reports.Where(x => ShowAll || x.Status <= (Int32)ReportStatus.Progress).OrderByDescending(x => x.RegistTime).ToList();
		}
	}
}
