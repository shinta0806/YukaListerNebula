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
using System.Windows;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.ReportWindowViewModels
{
	internal class ViewTReportsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public ViewTReportsWindowViewModel()
		{
			_reportContext = new();
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
			get => YlModel.Instance.EnvModel.HelpClickedCommand;
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
				_logWriter?.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				_logWriter?.ShowLogMessage(TraceEventType.Error, "詳細ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

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
				// タイトルバー
				Title = "リスト問題報告一覧";

				// 絞り込み
				ShowOnlyOpened = true;
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リスト問題報告一覧ウィンドウ初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// リスト問題報告データベース
		private readonly ReportContext _reportContext;

		// リスト問題テーブル
		//private readonly DbSet<TReport> _reports;

		// ====================================================================
		// private 関数
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
			if (editReportWindowViewModel.Result == MessageBoxResult.OK)
			{
				UpdateTReports();
			}
		}

		// --------------------------------------------------------------------
		// TReports を現在のオプションに合わせて上書き
		// --------------------------------------------------------------------
		private void UpdateTReports()
		{
			ReportsVisible = _reportContext.Reports.Where(x => ShowAll || x.Status <= (Int32)ReportStatus.Progress).OrderByDescending(x => x.RegistTime).ToList();
		}
	}
}
