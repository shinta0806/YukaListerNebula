// ============================================================================
// 
// 楽曲情報データベースマスター一覧ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// プログラム中では本クラスではなく派生クラスを使うこと。
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditMasterWindowViewModels;

namespace YukaLister.ViewModels.ViewMastersWindowViewModels
{
	public abstract class ViewMastersWindowViewModel<T> : ViewMastersWindowViewModel where T : class, IRcMaster, new()
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ViewMastersWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<T> records, ObservableCollection<DataGridColumn> columns)
		{
			_musicInfoContext = musicInfoContext;
			_records = records;
			Columns = columns;

			UpdateMasters();
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 説明
		private String _description = String.Empty;
		public String Description
		{
			get => _description;
			set => RaisePropertyChangedIfSet(ref _description, value);
		}

		// 列
		public ObservableCollection<DataGridColumn> Columns { get; set; }

		// 選択行
		private T? _selectedMaster;
		public T? SelectedMaster
		{
			get => _selectedMaster;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedMaster, value))
				{
					ButtonEditMasterClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// ファイル群
		private List<T> _masters = new();
		public List<T> Masters
		{
			get => _masters;
			set => RaisePropertyChangedIfSet(ref _masters, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region DataGrid ダブルクリックの制御

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
				EditMaster();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 編集ボタンの制御

		private ViewModelCommand? _buttonEditMasterClickedCommand;

		public ViewModelCommand ButtonEditMasterClickedCommand
		{
			get
			{
				if (_buttonEditMasterClickedCommand == null)
				{
					_buttonEditMasterClickedCommand = new ViewModelCommand(ButtonEditMasterClicked, CanButtonEditMasterClicked);
				}
				return _buttonEditMasterClickedCommand;
			}
		}

		public Boolean CanButtonEditMasterClicked()
		{
			return SelectedMaster != null;
		}

		public void ButtonEditMasterClicked()
		{
			try
			{
				EditMaster();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "編集ボタンクリック時エラー：\n" + excep.Message);
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
				Title = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "一覧";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲情報データベースマスター一覧ウィンドウ <T> 初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// 楽曲情報データベースのコンテキスト（外部から指定されたもの）
		protected MusicInfoContextDefault _musicInfoContext;

		// 検索対象データベースレコード
		protected DbSet<T> _records;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウビューモデルの作成
		// --------------------------------------------------------------------
		protected abstract EditMasterWindowViewModel<T> CreateEditMasterWindowViewModel();

		// --------------------------------------------------------------------
		// 編集可能なマスター群の作成
		// --------------------------------------------------------------------
		protected abstract List<T> CreateMasters();

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集
		// --------------------------------------------------------------------
		private void EditMaster()
		{
			if (SelectedMaster == null)
			{
				return;
			}

			// ViewModel 経由で楽曲情報データベースマスター編集ウィンドウを開く
			using EditMasterWindowViewModel<T> editMasterWindowViewModel = CreateEditMasterWindowViewModel();
			editMasterWindowViewModel.SetMasters(CreateMasters());
			editMasterWindowViewModel.DefaultMasterId = SelectedMaster.Id;
			Messenger.Raise(new TransitionMessage(editMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_MASTER_WINDOW));
		}

		// --------------------------------------------------------------------
		// マスター一覧を更新
		// --------------------------------------------------------------------
		private void UpdateMasters()
		{
			Masters = _records.AsNoTracking().ToList();
		}
	}
}
