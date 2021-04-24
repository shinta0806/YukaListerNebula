﻿// ============================================================================
// 
// 複数検索ウィンドウの基底 ViewModel 基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// プログラム中では本クラスではなく派生クラスを使うこと。
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditMasterWindowViewModels;
using YukaLister.ViewModels.SearchMasterWindowViewModels;

namespace YukaLister.ViewModels.EditSequenceWindowViewModels
{
	public abstract class EditSequenceWindowViewModel<T> : EditSequenceWindowViewModel where T : class, IRcMaster, new()
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditSequenceWindowViewModel(MusicInfoContext musicInfoContext, DbSet<T> records, String? captionDetail = null)
		{
			_musicInfoContext = musicInfoContext;
			_records = records;
			_caption2 = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()];
			if (String.IsNullOrEmpty(captionDetail))
			{
				_captionDetail = _caption2;
			}
			else
			{
				_captionDetail = captionDetail;
			}
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 説明
		private String? _description;
		public String? Description
		{
			get => _description;
			set => RaisePropertyChangedIfSet(ref _description, value);
		}

		// ヘルプ引数
		private String? _helpCommandParameter;
		public String? HelpCommandParameter
		{
			get => _helpCommandParameter;
			set => RaisePropertyChangedIfSet(ref _helpCommandParameter, value);
		}

		// データグリッドヘッダー
		private String? _dataGridHeader;
		public String? DataGridHeader
		{
			get => _dataGridHeader;
			set => RaisePropertyChangedIfSet(ref _dataGridHeader, value);
		}

		// 編集中のマスター群
		public ObservableCollection<T> Masters { get; set; } = new();

		// 選択されたマスター
		private T? _selectedMaster;
		public T? SelectedMaster
		{
			get => _selectedMaster;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedMaster, value))
				{
					ButtonRemoveClickedCommand.RaiseCanExecuteChanged();
					ButtonUpClickedCommand.RaiseCanExecuteChanged();
					ButtonDownClickedCommand.RaiseCanExecuteChanged();
					ButtonEditClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 編集ボタンのコンテンツ
		private String? _buttonEditContent;
		public String? ButtonEditContent
		{
			get => _buttonEditContent;
			set => RaisePropertyChangedIfSet(ref _buttonEditContent, value);
		}

		// 新規作成ボタンのコンテンツ
		private String? _buttonNewContent;
		public String? ButtonNewContent
		{
			get => _buttonNewContent;
			set => RaisePropertyChangedIfSet(ref _buttonNewContent, value);
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// OK ボタンが押された時のマスター群
		public List<T> OkSelectedMasters { get; private set; } = new();

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ヘルプリンクの制御
		public ListenerCommand<String>? HelpClickedCommand
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
				Edit();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "データグリッドダブルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 検索して追加ボタンの制御
		private ViewModelCommand? _buttonAddClickedCommand;

		public ViewModelCommand ButtonAddClickedCommand
		{
			get
			{
				if (_buttonAddClickedCommand == null)
				{
					_buttonAddClickedCommand = new ViewModelCommand(ButtonAddClicked);
				}
				return _buttonAddClickedCommand;
			}
		}

		public void ButtonAddClicked()
		{
			try
			{
				Add();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "追加ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 削除ボタンの制御
		private ViewModelCommand? _buttonRemoveClickedCommand;

		public ViewModelCommand ButtonRemoveClickedCommand
		{
			get
			{
				if (_buttonRemoveClickedCommand == null)
				{
					_buttonRemoveClickedCommand = new ViewModelCommand(ButtonRemoveClicked, CanButtonRemoveClicked);
				}
				return _buttonRemoveClickedCommand;
			}
		}

		public Boolean CanButtonRemoveClicked()
		{
			return SelectedMaster != null;
		}

		public void ButtonRemoveClicked()
		{
			try
			{
				if (SelectedMaster != null)
				{
					Masters.Remove(SelectedMaster);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "削除ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 上へボタンの制御
		private ViewModelCommand? _buttonUpClickedCommand;

		public ViewModelCommand ButtonUpClickedCommand
		{
			get
			{
				if (_buttonUpClickedCommand == null)
				{
					_buttonUpClickedCommand = new ViewModelCommand(ButtonUpClicked, CanButtonUpClicked);
				}
				return _buttonUpClickedCommand;
			}
		}

		public Boolean CanButtonUpClicked()
		{
			if (SelectedMaster == null)
			{
				return false;
			}
			return Masters.IndexOf(SelectedMaster) >= 1;
		}

		public void ButtonUpClicked()
		{
			try
			{
				if (SelectedMaster == null)
				{
					return;
				}
				Int32 selectedIndex = Masters.IndexOf(SelectedMaster);
				if (selectedIndex < 1)
				{
					return;
				}
				T item = SelectedMaster;
				Masters.Remove(item);
				Masters.Insert(selectedIndex - 1, item);
				SelectedMaster = item;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "上へボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 下へボタンの制御
		private ViewModelCommand? _buttonDownClickedCommand;

		public ViewModelCommand ButtonDownClickedCommand
		{
			get
			{
				if (_buttonDownClickedCommand == null)
				{
					_buttonDownClickedCommand = new ViewModelCommand(ButtonDownClicked, CanButtonDownClicked);
				}
				return _buttonDownClickedCommand;
			}
		}

		public Boolean CanButtonDownClicked()
		{
			if (SelectedMaster == null)
			{
				return false;
			}
			Int32 aIndex = Masters.IndexOf(SelectedMaster);
			return 0 <= aIndex && aIndex < Masters.Count - 1;
		}

		public void ButtonDownClicked()
		{
			try
			{
				if (SelectedMaster == null)
				{
					return;
				}
				Int32 selectedIndex = Masters.IndexOf(SelectedMaster);
				if (selectedIndex < 0 || selectedIndex >= Masters.Count - 1)
				{
					return;
				}
				T item = SelectedMaster;
				Masters.Remove(item);
				Masters.Insert(selectedIndex + 1, item);
				SelectedMaster = item;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "下へボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region マスター詳細編集ボタンの制御
		private ViewModelCommand? _buttonEditClickedCommand;

		public ViewModelCommand ButtonEditClickedCommand
		{
			get
			{
				if (_buttonEditClickedCommand == null)
				{
					_buttonEditClickedCommand = new ViewModelCommand(ButtonEditClicked, CanButtonEditClicked);
				}
				return _buttonEditClickedCommand;
			}
		}

		public Boolean CanButtonEditClicked()
		{
			return SelectedMaster != null;
		}

		public void ButtonEditClicked()
		{
			try
			{
				Edit();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 新規マスター作成ボタンの制御
		private ViewModelCommand? _buttonNewClickedCommand;

		public ViewModelCommand ButtonNewClickedCommand
		{
			get
			{
				if (_buttonNewClickedCommand == null)
				{
					_buttonNewClickedCommand = new ViewModelCommand(ButtonNewClicked);
				}
				return _buttonNewClickedCommand;
			}
		}

		public void ButtonNewClicked()
		{
			try
			{
				New();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "新規作成ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOkClickedCommand;

		public ViewModelCommand ButtonOkClickedCommand
		{
			get
			{
				if (_buttonOkClickedCommand == null)
				{
					_buttonOkClickedCommand = new ViewModelCommand(ButtonOkClicked);
				}
				return _buttonOkClickedCommand;
			}
		}

		public void ButtonOkClicked()
		{
			try
			{
				OkSelectedMasters = Masters.ToList();
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
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
				Title = "複数" + _captionDetail + "の検索";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// ラベル
				Description = "「検索して追加」ボタンで" + _caption2 + "を追加して下さい。複数追加も可能です。";
				DataGridHeader = _caption2;
				ButtonEditContent = _caption2 + "詳細編集 (_E)";
				ButtonNewContent = "新規" + _caption2 + "作成 (_N)";

				// 表示名
				foreach (T master in Masters)
				{
					DbCommon.SetAvoidSameName(_records, master);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "紐付編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// 楽曲情報データベースのコンテキスト
		protected MusicInfoContext _musicInfoContext;

		// 編集対象データベースレコード
		protected DbSet<T> _records;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスター編集ウィンドウの ViewModel 作成
		// --------------------------------------------------------------------
		protected abstract EditMasterWindowViewModel<T> CreateEditMasterWindowViewModel();

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 編集対象の名称
		// 例）_captionDetail が "歌手" の場合、_caption は "人物"
		private String _caption2;

		// 編集対象の名称詳細
		private String _captionDetail;

		// 検索したかどうか
		private Boolean _isMasterSearched;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスターを検索して追加
		// --------------------------------------------------------------------
		private void Add()
		{
			using SearchMasterWindowViewModel<T> searchMasterWindowViewModel = new(_records, _captionDetail);
			searchMasterWindowViewModel.SelectedKeyword = SelectedMaster?.Name;
			Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

			_isMasterSearched = true;
			if (searchMasterWindowViewModel.OkSelectedMaster == null)
			{
				return;
			}

			if (Masters.FirstOrDefault(x => x.Id == searchMasterWindowViewModel.OkSelectedMaster.Id) != null)
			{
				throw new Exception(searchMasterWindowViewModel.OkSelectedMaster.Name + "は既に追加されています。");
			}

			DbCommon.SetAvoidSameName(_records, searchMasterWindowViewModel.OkSelectedMaster);
			Masters.Add(searchMasterWindowViewModel.OkSelectedMaster);
			SelectedMaster = searchMasterWindowViewModel.OkSelectedMaster;
		}

		// --------------------------------------------------------------------
		// マスターを編集
		// --------------------------------------------------------------------
		private void Edit()
		{
			if (SelectedMaster == null)
			{
				return;
			}

			// 既存レコードを用意
			List<T> sameNameMasters = DbCommon.SelectMastersByName(_records, SelectedMaster.Name);

			// 新規作成用を追加
			T newRecord = new()
			{
				// IRcBase
				Id = String.Empty,
				Import = false,
				Invalid = false,
				UpdateTime = YlConstants.INVALID_MJD,
				Dirty = true,

				// IRcMaster
				Name = SelectedMaster.Name,
				Ruby = null,
				Keyword = null,
			};
			sameNameMasters.Insert(0, newRecord);

			// ウィンドウを開く
			T? result = OpenEditMasterWindow(sameNameMasters);

			// 後処理
			if (result == null)
			{
				return;
			}

			Masters[Masters.IndexOf(SelectedMaster)] = result;
			SelectedMaster = result;
		}

		// --------------------------------------------------------------------
		// マスターを新規作成
		// --------------------------------------------------------------------
		private void New()
		{
			if (!_isMasterSearched)
			{
				throw new Exception("新規" + _caption2 + "作成の前に一度、目的の" + _caption2 + "が未登録かどうか検索して下さい。");
			}

			if (MessageBox.Show("目的の" + _caption2 + "が未登録の場合（検索してもヒットしない場合）に限り、新規" + _caption2 + "作成を行って下さい。\n"
					+ "新規" + _caption2 + "作成を行いますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
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

			// ウィンドウを開く
			T? result = OpenEditMasterWindow(masters);

			// 後処理
			if (result == null)
			{
				return;
			}

			DbCommon.SetAvoidSameName(_records, result);
			Masters.Add(result);
			SelectedMaster = result;
		}

		// --------------------------------------------------------------------
		// マスター編集ウィンドウを開く
		// --------------------------------------------------------------------
		private T? OpenEditMasterWindow(List<T> masters)
		{
			using EditMasterWindowViewModel<T> editMasterWindowViewModel = CreateEditMasterWindowViewModel();
			editMasterWindowViewModel.SetMasters(masters);
			editMasterWindowViewModel.DefaultMasterId = DbCommon.SelectBaseById(_records, SelectedMaster?.Id)?.Id;
			Messenger.Raise(new TransitionMessage(editMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_MASTER_WINDOW));
			return editMasterWindowViewModel.OkSelectedMaster;
		}
	}
}
