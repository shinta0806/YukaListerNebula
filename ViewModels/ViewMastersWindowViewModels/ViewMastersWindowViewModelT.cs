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
using System.Windows;
using System.Windows.Controls;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditMasterWindowViewModels;
using YukaLister.ViewModels.SearchMasterWindowViewModels;

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
			_caption = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()];
			Columns = columns;

			UpdateAll(null);
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

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

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

		#region 検索ボタンの制御

		private ViewModelCommand? _buttonSearchMasterClickedCommand;

		public ViewModelCommand ButtonSearchMasterClickedCommand
		{
			get
			{
				if (_buttonSearchMasterClickedCommand == null)
				{
					_buttonSearchMasterClickedCommand = new ViewModelCommand(ButtonSearchMasterClicked);
				}
				return _buttonSearchMasterClickedCommand;
			}
		}

		public void ButtonSearchMasterClicked()
		{
			try
			{
				using SearchMasterWindowViewModel<T> searchMasterWindowViewModel = new(_records);
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				_isSearched = true;
				if (!searchMasterWindowViewModel.IsOk)
				{
					return;
				}
				SelectedMaster = Masters.FirstOrDefault(x => x.Id == searchMasterWindowViewModel.OkSelectedMaster?.Id);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "検索ボタンクリック時エラー：\n" + excep.Message);
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

		#region 新規作成ボタンの制御

		private ViewModelCommand? _buttonNewMasterClickedCommand;

		public ViewModelCommand ButtonNewMasterClickedCommand
		{
			get
			{
				if (_buttonNewMasterClickedCommand == null)
				{
					_buttonNewMasterClickedCommand = new ViewModelCommand(ButtonNewMasterClicked);
				}
				return _buttonNewMasterClickedCommand;
			}
		}

		public void ButtonNewMasterClicked()
		{
			try
			{
				if (!_isSearched)
				{
					throw new Exception("新規作成の前に一度、目的の" + _caption + "が未登録かどうか検索して下さい。");
				}

				if (MessageBox.Show("新規に" + _caption + "情報を作成しますか？\n"
						+ "（目的の" + _caption + "が未登録の場合（検索してもヒットしない場合）に限り、新規作成を行って下さい）", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					return;
				}

				// 新規マスター
				List<T> masters = new();
				T newRecord = new()
				{
					// IRcBase
					Id = String.Empty,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IRcMaster
					Name = null,
					Ruby = null,
					Keyword = null,
				};
				masters.Insert(0, newRecord);

				// ViewModel 経由で楽曲情報データベースマスター編集ウィンドウを開く
				using EditMasterWindowViewModel<T> editMasterWindowViewModel = CreateEditMasterWindowViewModel();
				editMasterWindowViewModel.SetMasters(masters);
				Messenger.Raise(new TransitionMessage(editMasterWindowViewModel, MessageKeyOpenEditWindow()));

				if (editMasterWindowViewModel.IsOk)
				{
					UpdateAll(editMasterWindowViewModel.OkSelectedMaster?.Id);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "新規作成ボタンクリック時エラー：\n" + excep.Message);
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
				Title = _caption + "一覧";
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

		// --------------------------------------------------------------------
		// ウィンドウを開くメッセージ
		// --------------------------------------------------------------------
		protected virtual String MessageKeyOpenEditWindow()
		{
			return YlConstants.MESSAGE_KEY_OPEN_EDIT_MASTER_WINDOW;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// マスター名
		private readonly String _caption;

		// レコード数
		private Int32 _prevNumRecords;

		// 検索したかどうか
		private Boolean _isSearched;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 有効なマスターの数
		// --------------------------------------------------------------------
		private Int32 CountMasters()
		{
			return _records.AsNoTracking().Where(x => !x.Invalid).Count();
		}

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
			Messenger.Raise(new TransitionMessage(editMasterWindowViewModel, MessageKeyOpenEditWindow()));

			if (editMasterWindowViewModel.IsOk || CountMasters() != _prevNumRecords)
			{
				UpdateAll(editMasterWindowViewModel.OkSelectedMaster?.Id);
			}
		}

		// --------------------------------------------------------------------
		// すべてを更新
		// --------------------------------------------------------------------
		private void UpdateAll(String? selectId)
		{
			UpdateDescription();
			UpdateMasters();
			SelectedMaster = Masters.FirstOrDefault(x => x.Id == selectId);
		}

		// --------------------------------------------------------------------
		// 説明を更新
		// --------------------------------------------------------------------
		private void UpdateDescription()
		{
			_prevNumRecords = CountMasters();
			Description = _prevNumRecords.ToString("#,0") + " 個の" + _caption + "が登録されています。ソートには時間がかかる場合があります。";
		}

		// --------------------------------------------------------------------
		// マスター一覧を更新
		// --------------------------------------------------------------------
		private void UpdateMasters()
		{
			Masters = _records.AsNoTracking().Where(x => !x.Invalid).ToList();

			// ソート（OrderBy だと大文字小文字を区別してしまうため、区別しないソートを行う）
			Masters.Sort(YlCommon.MasterComparisonByName);
		}
	}
}
