// ============================================================================
// 
// 環境設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Diagnostics;
using System.Windows;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.TabItemViewModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	internal class YlSettingsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsWindowViewModel()
		{
			// タブアイテムの ViewModel 初期化
			_ylSettingsTabItemSettingsViewModel = new YlSettingsTabItemSettingsViewModel(this);
			_ylSettingsTabItemListTargetViewModel = new YlSettingsTabItemListTargetViewModel(this);
			_ylSettingsTabItemListOutputViewModel = new YlSettingsTabItemListOutputViewModel(this);
			_ylSettingsTabItemMaintenanceViewModel = new YlSettingsTabItemMaintenanceViewModel(this);
			_ylSettingsTabItemMusicInfoListViewModel = new YlSettingsTabItemMusicInfoListViewModel(this);
			_ylSettingsTabItemMusicInfoBatchViewModel = new YlSettingsTabItemMusicInfoBatchViewModel(this);
			_ylSettingsTabItemYukariStatisticsViewModel = new YlSettingsTabItemYukariStatisticsViewModel(this);
			_ylSettingsTabItemSyncViewModel = new YlSettingsTabItemSyncViewModel(this);
			_ylSettingsTabItemViewModels = new TabItemViewModel[]
			{
				_ylSettingsTabItemSettingsViewModel,
				_ylSettingsTabItemListTargetViewModel,
				_ylSettingsTabItemListOutputViewModel,
				_ylSettingsTabItemMaintenanceViewModel,
				_ylSettingsTabItemMusicInfoListViewModel,
				_ylSettingsTabItemMusicInfoBatchViewModel,
				_ylSettingsTabItemYukariStatisticsViewModel,
				_ylSettingsTabItemSyncViewModel,
			};
			Debug.Assert(_ylSettingsTabItemViewModels.Length == (Int32)YlSettingsTabItem.__End__, "YlSettingsWindowViewModel() bad tab vm nums");
			for (Int32 i = 0; i < _ylSettingsTabItemViewModels.Length; i++)
			{
				CompositeDisposable.Add(_ylSettingsTabItemViewModels[i]);
			}
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// タブアイテム：設定
		private YlSettingsTabItemSettingsViewModel _ylSettingsTabItemSettingsViewModel;
		public YlSettingsTabItemSettingsViewModel YlSettingsTabItemSettingsViewModel
		{
			get => _ylSettingsTabItemSettingsViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemSettingsViewModel, value);
		}

		// タブアイテム：リスト対象
		private YlSettingsTabItemListTargetViewModel _ylSettingsTabItemListTargetViewModel;
		public YlSettingsTabItemListTargetViewModel YlSettingsTabItemListTargetViewModel
		{
			get => _ylSettingsTabItemListTargetViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemListTargetViewModel, value);
		}

		// タブアイテム：リスト出力
		private YlSettingsTabItemListOutputViewModel _ylSettingsTabItemListOutputViewModel;
		public YlSettingsTabItemListOutputViewModel YlSettingsTabItemListOutputViewModel
		{
			get => _ylSettingsTabItemListOutputViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemListOutputViewModel, value);
		}

		// タブアイテム：メンテナンス
		private YlSettingsTabItemMaintenanceViewModel _ylSettingsTabItemMaintenanceViewModel;
		public YlSettingsTabItemMaintenanceViewModel YlSettingsTabItemMaintenanceViewModel
		{
			get => _ylSettingsTabItemMaintenanceViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemMaintenanceViewModel, value);
		}

		// タブアイテム：楽曲情報一覧
		private YlSettingsTabItemMusicInfoListViewModel _ylSettingsTabItemMusicInfoListViewModel;
		public YlSettingsTabItemMusicInfoListViewModel YlSettingsTabItemMusicInfoListViewModel
		{
			get => _ylSettingsTabItemMusicInfoListViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemMusicInfoListViewModel, value);
		}

		// タブアイテム：楽曲情報一括操作
		private YlSettingsTabItemMusicInfoBatchViewModel _ylSettingsTabItemMusicInfoBatchViewModel;
		public YlSettingsTabItemMusicInfoBatchViewModel YlSettingsTabItemMusicInfoBatchViewModel
		{
			get => _ylSettingsTabItemMusicInfoBatchViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemMusicInfoBatchViewModel, value);
		}

		// タブアイテム：ゆかり統計
		private YlSettingsTabItemYukariStatisticsViewModel _ylSettingsTabItemYukariStatisticsViewModel;
		public YlSettingsTabItemYukariStatisticsViewModel YlSettingsTabItemYukariStatisticsViewModel
		{
			get => _ylSettingsTabItemYukariStatisticsViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemYukariStatisticsViewModel, value);
		}

		// タブアイテム：同期
		private YlSettingsTabItemSyncViewModel _ylSettingsTabItemSyncViewModel;
		public YlSettingsTabItemSyncViewModel YlSettingsTabItemSyncViewModel
		{
			get => _ylSettingsTabItemSyncViewModel;
			set => RaisePropertyChangedIfSet(ref _ylSettingsTabItemSyncViewModel, value);
		}

		// 選択されているタブ（ドロップ先識別用等）
		private Int32 _selectedTabIndex;
		public Int32 SelectedTabIndex
		{
			get => _selectedTabIndex;
			set => RaisePropertyChangedIfSet(ref _selectedTabIndex, value);
		}

		// OK ボタンフォーカス
		private Boolean _isButtonOkFocused;
		public Boolean IsButtonOkFocused
		{
			get => _isButtonOkFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isButtonOkFocused = value;
				RaisePropertyChanged(nameof(IsButtonOkFocused));
			}
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// 強制再取得をユーザーから指示されたか
		public Boolean RegetSyncDataNeeded { get; set; }

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YlModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

		#region ファイルドロップの制御
		private ListenerCommand<String[]>? _tabControlFileDropCommand;

		public ListenerCommand<String[]> TabControlFileDropCommand
		{
			get
			{
				if (_tabControlFileDropCommand == null)
				{
					_tabControlFileDropCommand = new ListenerCommand<String[]>(TabControlFileDrop);
				}
				return _tabControlFileDropCommand;
			}
		}

		public void TabControlFileDrop(String[] pathes)
		{
			try
			{
				if (SelectedTabIndex < 0 || SelectedTabIndex >= _ylSettingsTabItemViewModels.Length)
				{
					return;
				}
				_ylSettingsTabItemViewModels[SelectedTabIndex].PathDropped(pathes);
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タブコントロールファイルドロップ時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				// Enter キーでボタンが押された場合はテキストボックスからフォーカスが移らずプロパティーが更新されないため強制フォーカス
				IsButtonOkFocused = true;

				CheckInput();
				PropertiesToSettings();
				YlModel.Instance.EnvModel.YlSettings.Save();
				Result = MessageBoxResult.OK;
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				Title = "環境設定";

				for (Int32 i = 0; i < _ylSettingsTabItemViewModels.Length; i++)
				{
					_ylSettingsTabItemViewModels[i].Initialize();
				}

				SettingsToProperties();
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "環境設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public void YukariConfigPathSeedChanged(String newSeed)
		{
			YlSettingsTabItemListOutputViewModel.YukariConfigPathSeedChanged(newSeed);
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// タブアイテムの ViewModel
		private readonly TabItemViewModel[] _ylSettingsTabItemViewModels;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void CheckInput()
		{
			for (Int32 i = 0; i < _ylSettingsTabItemViewModels.Length; i++)
			{
				_ylSettingsTabItemViewModels[i].CheckInput();
			}
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		private void PropertiesToSettings()
		{
			for (Int32 i = 0; i < _ylSettingsTabItemViewModels.Length; i++)
			{
				_ylSettingsTabItemViewModels[i].PropertiesToSettings();
			}
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		private void SettingsToProperties()
		{
			for (Int32 i = 0; i < _ylSettingsTabItemViewModels.Length; i++)
			{
				_ylSettingsTabItemViewModels[i].SettingsToProperties();
			}
		}
	}
}
