// ============================================================================
// 
// メインウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;

using Microsoft.EntityFrameworkCore;

using Shinta;
using Shinta.Behaviors;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.WebServer;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;
using YukaLister.ViewModels.ReportWindowViewModels;

namespace YukaLister.ViewModels
{
	public class MainWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel(SplashWindowViewModel splashWindowViewModel)
		{
			_splashWindowViewModel = splashWindowViewModel;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public MainWindowViewModel()
		{
			// 警告抑止用にメンバーを null! で初期化
			_splashWindowViewModel = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		#region View 通信用のプロパティー
		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ウィンドウ左端
		private Double _left;
		public Double Left
		{
			get => _left;
			set => RaisePropertyChangedIfSet(ref _left, value);
		}

		// ウィンドウ上端
		private Double _top;
		public Double Top
		{
			get => _top;
			set => RaisePropertyChangedIfSet(ref _top, value);
		}

		// ウィンドウ幅
		private Double _width;
		public Double Width
		{
			get => _width;
			set => RaisePropertyChangedIfSet(ref _width, value);
		}

		// ウィンドウ高さ
		private Double _height;
		public Double Height
		{
			get => _height;
			set => RaisePropertyChangedIfSet(ref _height, value);
		}

		// ゆかりすたー NEBULA 全体の動作状況
		private String _yukaListerStatusLabel = String.Empty;
		public String YukaListerStatusLabel
		{
			get => _yukaListerStatusLabel;
			set => RaisePropertyChangedIfSet(ref _yukaListerStatusLabel, value);
		}

		// ゆかりすたー NEBULA 全体の動作状況の背景
		private Brush _yukaListerStatusBackground = YlConstants.BRUSH_STATUS_RUNNING;
		public Brush YukaListerStatusBackground
		{
			get => _yukaListerStatusBackground;
			set => RaisePropertyChangedIfSet(ref _yukaListerStatusBackground, value);
		}

		// ゆかりすたー NEBULA 全体の動作状況のカーソル
		private Cursor? _yukaListerStatusCursor;
		public Cursor? YukaListerStatusCursor
		{
			get => _yukaListerStatusCursor;
			set => RaisePropertyChangedIfSet(ref _yukaListerStatusCursor, value);
		}

		// 検索可能ファイル数
		private String _numRecordsLabel = String.Empty;
		public String NumRecordsLabel
		{
			get => _numRecordsLabel;
			set => RaisePropertyChangedIfSet(ref _numRecordsLabel, value);
		}

		// 処理を要するリスト問題の数
		private String? _reportsBadge;
		public String? ReportsBadge
		{
			get => _reportsBadge;
			set => RaisePropertyChangedIfSet(ref _reportsBadge, value);
		}

		// DataGrid の選択
		private TargetFolderInfo? _selectedTargetFolderInfo;
		public TargetFolderInfo? SelectedTargetFolderInfo
		{
			get => _selectedTargetFolderInfo;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedTargetFolderInfo, value))
				{
					ButtonRemoveTargetFolderClickedCommand.RaiseCanExecuteChanged();
					ButtonFolderSettingsClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// ゆかり検索対象フォルダー（表示用）
		private List<TargetFolderInfo>? _targetFolderInfosVisible;
		public List<TargetFolderInfo>? TargetFolderInfosVisible
		{
			get => _targetFolderInfosVisible;
			set => RaisePropertyChangedIfSet(ref _targetFolderInfosVisible, value);
		}

		// ステータスバーメッセージ
		private String _statusBarMessage = String.Empty;
		public String StatusBarMessage
		{
			get => _statusBarMessage;
			set => RaisePropertyChangedIfSet(ref _statusBarMessage, value);
		}

		// ステータスバー文字色
		private Brush _statusBarForeground = YlConstants.BRUSH_NORMAL_STRING;
		public Brush StatusBarForeground
		{
			get => _statusBarForeground;
			set => RaisePropertyChangedIfSet(ref _statusBarForeground, value);
		}
		#endregion

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ファイルドロップの制御
		private ListenerCommand<String[]>? _windowFileDropCommand;

		public ListenerCommand<String[]> WindowFileDropCommand
		{
			get
			{
				if (_windowFileDropCommand == null)
				{
					_windowFileDropCommand = new ListenerCommand<String[]>(WindowFileDrop);
				}
				return _windowFileDropCommand;
			}
		}

		public async void WindowFileDrop(String[] files)
		{
			try
			{
				foreach (String file in files)
				{
					if (!Directory.Exists(file))
					{
						// フォルダーでない場合は何もしない
						continue;
					}

					await AddFolderAsync(file);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイルドロップ時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region リムーバブルメディア着脱の制御
		private ListenerCommand<DeviceChangeInfo>? _windowDeviceChangeCommand;

		public ListenerCommand<DeviceChangeInfo> WindowDeviceChangeCommand
		{
			get
			{
				if (_windowDeviceChangeCommand == null)
				{
					_windowDeviceChangeCommand = new ListenerCommand<DeviceChangeInfo>(WindowDeviceChange);
				}
				return _windowDeviceChangeCommand;
			}
		}

		public async void WindowDeviceChange(DeviceChangeInfo deviceChangeInfo)
		{
			try
			{
				if (String.IsNullOrEmpty(deviceChangeInfo.DriveLetter))
				{
					return;
				}

				switch (deviceChangeInfo.Kind)
				{
					case DBT.DBT_DEVICEARRIVAL:
						SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "リムーバブルドライブが接続されました：" + deviceChangeInfo.DriveLetter);
						await DeviceArrivalAsync(deviceChangeInfo.DriveLetter);
						break;
					case DBT.DBT_DEVICEREMOVECOMPLETE:
						SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "リムーバブルドライブが切断されました：" + deviceChangeInfo.DriveLetter);
						DeviceRemoveComplete(deviceChangeInfo.DriveLetter);
						break;
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "デバイス着脱時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ゆかりすたー NEBULA 全体の動作状況ラベルの制御

		private ViewModelCommand? _labelYukaListerStatusClickedCommand;

		public ViewModelCommand LabelYukaListerStatusClickedCommand
		{
			get
			{
				if (_labelYukaListerStatusClickedCommand == null)
				{
					_labelYukaListerStatusClickedCommand = new ViewModelCommand(LabelYukaListerStatusClicked);
				}
				return _labelYukaListerStatusClickedCommand;
			}
		}

		public void LabelYukaListerStatusClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(_labelYukaListerStatusUrl))
				{
					return;
				}
				Common.ShellExecute(_labelYukaListerStatusUrl);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかりすたー NEBULA 全体の動作状況ラベルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		#endregion

		#region リスト問題報告ボタンの制御

		private ViewModelCommand? _buttonReportsClickedCommand;

		public ViewModelCommand ButtonReportsClickedCommand
		{
			get
			{
				if (_buttonReportsClickedCommand == null)
				{
					_buttonReportsClickedCommand = new ViewModelCommand(ButtonReportsClicked);
				}
				return _buttonReportsClickedCommand;
			}
		}

		public void ButtonReportsClicked()
		{
			try
			{
				// ViewModel 経由でウィンドウを開く
				using ViewTReportsWindowViewModel viewTReportsWindowViewModel = new();
				Messenger.Raise(new TransitionMessage(viewTReportsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_TREPORTS_WINDOW));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト問題報告ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 環境設定ボタンの制御
		private ViewModelCommand? _buttonYukaListerSettingsClickedCommand;

		public ViewModelCommand ButtonYukaListerSettingsClickedCommand
		{
			get
			{
				if (_buttonYukaListerSettingsClickedCommand == null)
				{
					_buttonYukaListerSettingsClickedCommand = new ViewModelCommand(ButtonYukaListerSettingsClicked);
				}
				return _buttonYukaListerSettingsClickedCommand;
			}
		}

		public async void ButtonYukaListerSettingsClicked()
		{
			try
			{
				String yukariConfigPathBak = YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPath();
				Boolean provideYukariPreviewBak = YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview;
				Boolean syncMusicInfoDbBak = YukaListerModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb;
				String? syncServerBak = YukaListerModel.Instance.EnvModel.YlSettings.SyncServer;
				String? syncAccountBak = YukaListerModel.Instance.EnvModel.YlSettings.SyncAccount;
				String? syncPasswordBak = YukaListerModel.Instance.EnvModel.YlSettings.SyncPassword;
				using MusicInfoContextDefault musicInfoContextDefault = new();
				DateTime musicInfoDbTimeBak = musicInfoContextDefault.LastWriteDateTime();
				Boolean regetSyncDataNeeded;

				// ViewModel 経由でウィンドウを開く
				using YlSettingsWindowViewModel ylSettingsWindowViewModel = new();
				Messenger.Raise(new TransitionMessage(ylSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_YL_SETTINGS_WINDOW));

				if (ylSettingsWindowViewModel.Result == MessageBoxResult.OK)
				{
					SetStatusBarMessageWithInvoke(TraceEventType.Information, "環境設定を変更しました。");
				}
				regetSyncDataNeeded = ylSettingsWindowViewModel.RegetSyncDataNeeded;

				// ゆかり設定ファイルのフルパスが変更された場合は処理を行う
				if (YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPath() != yukariConfigPathBak)
				{
					YukaListerModel.Instance.EnvModel.YlSettings.AnalyzeYukariConfig();
					UpdateYukaListerEnvironmentStatus();
					DbCommon.PrepareDatabases();
					SetFileSystemWatcherYukariConfig();
					SetFileSystemWatcherYukariRequestDatabase();
					SetFileSystemWatcherReportDatabase();
					UpdateReportsBadge();
					YukaListerModel.Instance.EnvModel.Sifolin.MainEvent.Set();
				}

				// サーバー設定が変更された場合は起動・終了を行う
				if (YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview != provideYukariPreviewBak)
				{
					if (YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview)
					{
						StartWebServerIfNeeded();
					}
					else
					{
						await QuitServerIfNeededAsync();
					}
				}

				if (regetSyncDataNeeded)
				{
					// 再取得が指示された場合は再取得
					YukaListerModel.Instance.EnvModel.Syclin.IsReget = true;
					YlCommon.ActivateSyclinIfNeeded();
				}
				else
				{
					// 同期設定が変更された場合・インポートで楽曲情報データベースが更新された場合は同期を行う
					DateTime musicInfoDbTime = musicInfoContextDefault.LastWriteDateTime();
					if (YukaListerModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb != syncMusicInfoDbBak
							|| YukaListerModel.Instance.EnvModel.YlSettings.SyncServer != syncServerBak
							|| YukaListerModel.Instance.EnvModel.YlSettings.SyncAccount != syncAccountBak
							|| YukaListerModel.Instance.EnvModel.YlSettings.SyncPassword != syncPasswordBak
							|| musicInfoDbTime != musicInfoDbTimeBak)
					{
						YlCommon.ActivateSyclinIfNeeded();
					}
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "環境設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ヘルプメニューアイテムの制御
		public static ListenerCommand<String>? MenuItemHelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

		#region よくある質問メニューアイテムの制御

		private ViewModelCommand? _menuItemFaqClickedCommand;

		public ViewModelCommand MenuItemFaqClickedCommand
		{
			get
			{
				if (_menuItemFaqClickedCommand == null)
				{
					_menuItemFaqClickedCommand = new ViewModelCommand(MenuItemFaqClicked);
				}
				return _menuItemFaqClickedCommand;
			}
		}

		public static void MenuItemFaqClicked()
		{
			try
			{
				Common.ShellExecute(YlConstants.URL_FAQ);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "よくある質問メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region サンプルフォルダーメニューアイテムの制御

		private ViewModelCommand? _menuItemSampleFolderClickedCommand;

		public ViewModelCommand MenuItemSampleFolderClickedCommand
		{
			get
			{
				if (_menuItemSampleFolderClickedCommand == null)
				{
					_menuItemSampleFolderClickedCommand = new ViewModelCommand(MenuItemSampleFolderClicked);
				}
				return _menuItemSampleFolderClickedCommand;
			}
		}

		public static void MenuItemSampleFolderClicked()
		{
			try
			{
				Common.ShellExecute(YukaListerModel.Instance.EnvModel.ExeFullFolder + YlConstants.FOLDER_NAME_DOCUMENTS + YlConstants.FOLDER_NAME_SAMPLE_FOLDER_SETTINGS);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "サンプルフォルダーメニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 改訂履歴メニューアイテムの制御
		private ViewModelCommand? _menuItemHistoryClickedCommand;

		public ViewModelCommand MenuItemHistoryClickedCommand
		{
			get
			{
				if (_menuItemHistoryClickedCommand == null)
				{
					_menuItemHistoryClickedCommand = new ViewModelCommand(MenuItemHistoryClicked);
				}
				return _menuItemHistoryClickedCommand;
			}
		}

		public static void MenuItemHistoryClicked()
		{
			try
			{
				Common.ShellExecute(YukaListerModel.Instance.EnvModel.ExeFullFolder + YlConstants.FOLDER_NAME_DOCUMENTS + FILE_NAME_HISTORY);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "改訂履歴メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region バージョン情報メニューアイテムの制御
		private ViewModelCommand? _menuItemAboutClickedCommand;

		public ViewModelCommand MenuItemAboutClickedCommand
		{
			get
			{
				if (_menuItemAboutClickedCommand == null)
				{
					_menuItemAboutClickedCommand = new ViewModelCommand(MenuItemAboutClicked);
				}
				return _menuItemAboutClickedCommand;
			}
		}

		public void MenuItemAboutClicked()
		{
			try
			{
				// ViewModel 経由でウィンドウを開く
				using AboutWindowViewModel aboutWindowViewModel = new();
				Messenger.Raise(new TransitionMessage(aboutWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_ABOUT_WINDOW));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "バージョン情報メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
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
				FolderSettings();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 削除ボタンの制御
		private ViewModelCommand? _buttonRemoveTargetFolderClickedCommand;

		public ViewModelCommand ButtonRemoveTargetFolderClickedCommand
		{
			get
			{
				if (_buttonRemoveTargetFolderClickedCommand == null)
				{
					_buttonRemoveTargetFolderClickedCommand = new ViewModelCommand(ButtonRemoveTargetFolderClicked, CanButtonRemoveTargetFolderClick);
				}
				return _buttonRemoveTargetFolderClickedCommand;
			}
		}

		public Boolean CanButtonRemoveTargetFolderClick()
		{
			return SelectedTargetFolderInfo != null;
		}

		public void ButtonRemoveTargetFolderClicked()
		{
			try
			{
				if (SelectedTargetFolderInfo == null)
				{
					return;
				}

				if (MessageBox.Show(SelectedTargetFolderInfo.ParentPath + "\nおよびサブフォルダーをゆかり検索対象から除外しますか？",
						"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
				{
					return;
				}

				YukaListerModel.Instance.ProjModel.SetFolderTaskDetailOfFolderToRemove(SelectedTargetFolderInfo.ParentPath);
				UpdateDataGrid();

				// 次回 UI 更新タイミングまでに削除が完了してしまっていても検索可能ファイル数が更新されるようにする
				_prevYukaListerWholeStatus = YukaListerStatus.__End__;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "削除ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ファイル一覧ボタンの制御
		private ViewModelCommand? _buttonTFoundsClickedCommand;

		public ViewModelCommand ButtonTFoundsClickedCommand
		{
			get
			{
				if (_buttonTFoundsClickedCommand == null)
				{
					_buttonTFoundsClickedCommand = new ViewModelCommand(ButtonTFoundsClicked, CanButtonTFoundsClick);
				}
				return _buttonTFoundsClickedCommand;
			}
		}

		public Boolean CanButtonTFoundsClick()
		{
			return _numFounds > 0;
		}

		public void ButtonTFoundsClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();
				DateTime musicInfoDbTimeBak = musicInfoContextDefault.LastWriteDateTime();

				// ViewModel 経由でウィンドウを開く
				using ViewTFoundsWindowViewModel viewTFoundsWindowViewModel = new();
				Messenger.Raise(new TransitionMessage(viewTFoundsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_TFOUNDS_WINDOW));

				// 楽曲情報データベースが更新された場合は同期を行う
				if (musicInfoContextDefault.LastWriteDateTime() != musicInfoDbTimeBak)
				{
					YlCommon.ActivateSyclinIfNeeded();
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル一覧ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー設定ボタンの制御
		private ViewModelCommand? _buttonFolderSettingsClickedCommand;

		public ViewModelCommand ButtonFolderSettingsClickedCommand
		{
			get
			{
				if (_buttonFolderSettingsClickedCommand == null)
				{
					_buttonFolderSettingsClickedCommand = new ViewModelCommand(ButtonFolderSettingsClicked, CanButtonFolderSettingsClick);
				}
				return _buttonFolderSettingsClickedCommand;
			}
		}

		public Boolean CanButtonFolderSettingsClick()
		{
			return SelectedTargetFolderInfo != null;
		}

		public void ButtonFolderSettingsClicked()
		{
			try
			{
				FolderSettings();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public async void AddFolderSelected(FolderSelectionMessage folderSelectionMessage)
		{
			try
			{
				if (folderSelectionMessage.Response == null)
				{
					return;
				}
				await AddFolderAsync(folderSelectionMessage.Response[0]);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "追加フォルダー選択時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override async void Initialize()
		{
			base.Initialize();

			try
			{
				// タイトルバー
				Title = YlConstants.APP_NAME_J;
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
#if TEST
				Title = "［テスト］" + Title;
#endif

				// イベントハンドラー
				TargetFolderInfo.IsOpenChanged = TargetFolderInfoIsOpenChanged;

				// スプラッシュウィンドウを閉じる
				_splashWindowViewModel.Close();

				// プログラムエラーチェック
				Debug.Assert(YlConstants.FOLDER_SETTINGS_STATUS_LABELS.Length == (Int32)FolderSettingsStatus.__End__, "MainWindow.Initialize() bad FOLDER_SETTINGS_STATUS_TEXTS length");
				Debug.Assert(YlConstants.MUSIC_INFO_ID_SECOND_PREFIXES.Length == (Int32)MusicInfoTables.__End__, "MainWindow.Initialize() bad MUSIC_INFO_ID_SECOND_PREFIXES length");
				Debug.Assert(YlConstants.MUSIC_INFO_DB_TABLE_NAMES.Length == (Int32)MusicInfoTables.__End__, "MainWindow.Initialize() bad MUSIC_INFO_DB_TABLE_NAMES length");
				Debug.Assert(YlConstants.MUSIC_INFO_TABLE_NAME_LABELS.Length == (Int32)MusicInfoTables.__End__, "MainWindow.Initialize() bad MUSIC_INFO_TABLE_NAME_LABELS length");
				Debug.Assert(YlConstants.OUTPUT_ITEM_NAMES.Length == (Int32)OutputItems.__End__, "MainWindow.Initialize() bad OUTPUT_ITEM_NAMES length");
				Debug.Assert(YlConstants.GROUP_NAVI_NAMES.Length == (Int32)GroupNaviItems.__End__, "MainWindow.Initialize() bad GROUP_NAVI_NAMES length");
				Debug.Assert(YlConstants.YUKARI_STATISTICS_PERIOD_LABELS.Length == (Int32)YukariStatisticsPeriod.__End__, "MainWindow.Initialize() bad YUKARI_STATISTICS_PERIOD_LABELS");

				// 参照設定
				YukaListerModel.Instance.EnvModel.Kamlin.MainWindowViewModel = this;
				YukaListerModel.Instance.EnvModel.Yurelin.MainWindowViewModel = this;
				YukaListerModel.Instance.EnvModel.Syclin.MainWindowViewModel = this;

				// 環境の変化に対応
				DoVerChangedIfNeeded();

				await Task.Run(async () =>
				{
					// 動作状況
					YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Startup] = YukaListerStatus.Running;
					YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Startup]
							= YukaListerModel.Instance.EnvModel.YlSettings.AddFolderOnDeviceArrived ? "前回のゆかり検索対象フォルダーを確認中..." : "起動処理中...";
					UpdateYukaListerEnvironmentStatus();

					// ゆかり設定ファイル config.ini 監視
					CompositeDisposable.Add(_fileSystemWatcherYukariConfig);
					_fileSystemWatcherYukariConfig.Created += new FileSystemEventHandler(FileSystemWatcherYukariConfig_Changed);
					_fileSystemWatcherYukariConfig.Deleted += new FileSystemEventHandler(FileSystemWatcherYukariConfig_Changed);
					_fileSystemWatcherYukariConfig.Changed += new FileSystemEventHandler(FileSystemWatcherYukariConfig_Changed);
					SetFileSystemWatcherYukariConfig();

					// ゆかり予約ファイル request.db 監視
					CompositeDisposable.Add(_fileSystemWatcherYukariRequestDatabase);
					_fileSystemWatcherYukariRequestDatabase.Created += new FileSystemEventHandler(FileSystemWatcherYukariRequestDatabase_Changed);
					_fileSystemWatcherYukariRequestDatabase.Deleted += new FileSystemEventHandler(FileSystemWatcherYukariRequestDatabase_Changed);
					_fileSystemWatcherYukariRequestDatabase.Changed += new FileSystemEventHandler(FileSystemWatcherYukariRequestDatabase_Changed);
					SetFileSystemWatcherYukariRequestDatabase();

					// リスト問題報告データベース監視
					CompositeDisposable.Add(_fileSystemWatcherReportDatabase);
					_fileSystemWatcherReportDatabase.Created += new FileSystemEventHandler(FileSystemWatcherReportDatabase_Changed);
					_fileSystemWatcherReportDatabase.Deleted += new FileSystemEventHandler(FileSystemWatcherReportDatabase_Changed);
					_fileSystemWatcherReportDatabase.Changed += new FileSystemEventHandler(FileSystemWatcherReportDatabase_Changed);
					SetFileSystemWatcherReportDatabase();
					UpdateReportsBadge();

					// UI 更新タイマー
					_timerUpdateUi.Interval = TimeSpan.FromSeconds(1.0);
					_timerUpdateUi.Tick += new EventHandler(TimerUpdateUi_Tick);
					_timerUpdateUi.Start();

					// 接続されているドライブの自動接続
					await AutoTargetAllDrivesAsync();

					// Web サーバー
					StartWebServerIfNeeded();

					// 最新情報確認
					await CheckRssIfNeededAsync();

					// 過去の統計データが更新されるようにする
					YukaListerModel.Instance.EnvModel.Yurelin.UpdatePastYukariStatisticsKind = UpdatePastYukariStatisticsKind.Fast;
					if (YukaListerModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb)
					{
						// サーバー同期が有効なら同期する
						// 統計データ作成は遅くとも Syclin スリープ時には行われるので、明示的には作成しない
						YlCommon.ActivateSyclinIfNeeded();
					}
					else
					{
						// 統計データ作成
						YlCommon.ActivateYurelinIfNeeded();
					}
				});

#if DEBUG
				using MusicInfoContextDefault musicInfoContext = new();
				if (musicInfoContext.TieUps != null)
				{
					Debug.WriteLine("Initialize() (musicInfoContext.TieUps != null");
				}
#endif

				// スタートアップ終了
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Startup] = YukaListerStatus.Ready;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// ステータスバーにメッセージを表示
		// --------------------------------------------------------------------
		public void SetStatusBarMessageWithInvoke(TraceEventType traceEventType, String msg)
		{
			if (YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.IsCancellationRequested)
			{
				return;
			}
			DispatcherHelper.UIDispatcher.Invoke(new Action(() =>
			{
				StatusBarMessage = msg;
				if (traceEventType == TraceEventType.Error)
				{
					StatusBarForeground = YlConstants.BRUSH_ERROR_STRING;
				}
				else
				{
					StatusBarForeground = YlConstants.BRUSH_NORMAL_STRING;
				}
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(traceEventType, msg);
			}));
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_isDisposed)
			{
				return;
			}

			try
			{
				// アプリケーションの終了を通知
				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Cancel();
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "終了中...");

				// 終了処理
				// await するとその間に強制終了されてしまうようなので、await しない
				_ = YukaListerModel.Instance.EnvModel.QuitAllCoresAsync();
				_ = QuitServerIfNeededAsync().AsTask();
				SaveExitStatus();
				try
				{
					Directory.Delete(YlCommon.TempFolderPath(), true);
				}
				catch
				{
				}

				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "終了しました：" + YlConstants.APP_NAME_J + " "
						+ YlConstants.APP_VER + " --------------------");

				_isDisposed = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ破棄時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// 改訂履歴ファイル
		private const String FILE_NAME_HISTORY = "YukaListerNebula_History_JPN" + Common.FILE_EXT_TXT;

		// 「ゆかり設定ファイルが正しく指定されていません」FAQ
		private const String URL_BAD_YUKARI_CONFIG = "https://github.com/shinta0806/YukaListerNebula/issues/135";

		// ====================================================================
		// private 変数
		// ====================================================================

		// スプラッシュウィンドウ
		private readonly SplashWindowViewModel _splashWindowViewModel;

		// UI 更新用タイマー
		private readonly DispatcherTimer _timerUpdateUi = new();

		// 前回 UI 更新時のゆかりすたー NEBULA 全体の動作状況
		private YukaListerStatus _prevYukaListerWholeStatus = YukaListerStatus.__End__;

		// config.ini 監視用
		private readonly FileSystemWatcher _fileSystemWatcherYukariConfig = new();

		// request.db 監視用
		private readonly FileSystemWatcher _fileSystemWatcherYukariRequestDatabase = new();

		// request.db 更新時の遅延フラグ
		private Boolean _fileSystemWatcherYukariRequestDatabaseDelaying;

		// request.db 更新時の遅延中に再度更新があった
		private Boolean _fileSystemWatcherYukariRequestDatabaseDelayingQueue;

		// リスト問題報告データベース監視用
		private readonly FileSystemWatcher _fileSystemWatcherReportDatabase = new();

		// ゆかりすたー NEBULA 全体の動作状況ラベルクリック時に表示する URL
		private String? _labelYukaListerStatusUrl;

		// 検索可能ファイル数
		private Int32 _numFounds;

		// Web サーバー（null のままのこともあり得る）
		private WebServer? _webServer;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// フォルダーを 1 つ追加
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async ValueTask AddFolderAsync(String folderPath)
		{
			if (String.IsNullOrEmpty(folderPath))
			{
				return;
			}

			try
			{
				// AddTargetFolderAsync() に時間を要することがあるので表示を更新しておく
				Cursor = Cursors.Wait;
				await YukaListerModel.Instance.ProjModel.AddTargetFolderAsync(folderPath);
				UpdateDataGrid();

				// 次回 UI 更新タイミングまでに追加が完了してしまっていても検索可能ファイル数が更新されるようにする
				_prevYukaListerWholeStatus = YukaListerStatus.__End__;
			}
			finally
			{
				Cursor = null;
			}
		}

		// --------------------------------------------------------------------
		// 接続されているすべてのドライブで自動接続
		// --------------------------------------------------------------------
		private async Task AutoTargetAllDrivesAsync()
		{
			String[] drives = Directory.GetLogicalDrives();
			foreach (String drive in drives)
			{
				await DeviceArrivalAsync(YlCommon.DriveLetter(drive));
			}
		}

		// --------------------------------------------------------------------
		// 最新情報確認
		// --------------------------------------------------------------------
		private static async Task CheckRssIfNeededAsync()
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.IsCheckRssNeeded())
			{
				return;
			}
			await YlCommon.CheckLatestInfoAsync(false);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：デバイスが接続された
		// ＜引数＞ driveLetter: "D:" のようにコロンまで
		// --------------------------------------------------------------------
		private async ValueTask DeviceArrivalAsync(String driveLetter)
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.AddFolderOnDeviceArrived)
			{
				return;
			}

			await DeviceArrivalCoreAsync(driveLetter);

			// 次回 UI 更新タイミングまでに追加が完了してしまっていても検索可能ファイル数が更新されるようにする
			_prevYukaListerWholeStatus = YukaListerStatus.__End__;
		}

		// --------------------------------------------------------------------
		// デバイスが接続された
		// ＜引数＞ driveLetter: "D:" のようにコロンまで
		// --------------------------------------------------------------------
		private static async Task DeviceArrivalCoreAsync(String driveLetter)
		{
			Debug.Assert(driveLetter.Length == 2, "DeviceArrivalCoreAsync() bad driveLetter: " + driveLetter);
			AutoTargetInfo autoTargetInfo = new(driveLetter);
			autoTargetInfo.Load();
			foreach (String folder in autoTargetInfo.Folders)
			{
				try
				{
					await YukaListerModel.Instance.ProjModel.AddTargetFolderAsync(driveLetter + folder);
				}
				catch (Exception excep)
				{
					// 前回からフォルダー名が変更されている場合等にエラーとなるが、自動処理なのでエラー表示は抑止する
					// ここで捕捉しておかないと、Initialize() の後半が実行されない
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "デバイス接続時時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			}
		}

		// --------------------------------------------------------------------
		// ネビュラコアからのエラーを表示
		// 大量にエラーが出た場合もユーザーがなんとか操作できるよう、1 度に出すエラーは 1 つ
		// --------------------------------------------------------------------
		private static void DisplayNebulaCoreError()
		{
			if (!YukaListerModel.Instance.EnvModel.NebulaCoreErrors.TryTake(out String? error))
			{
				return;
			}

			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, error);
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースが未登録ならサンプルをインポート
		// --------------------------------------------------------------------
		private static void ImportSampleIfNeeded()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();
				if (musicInfoContextDefault.TieUps.Any())
				{
					return;
				}

				Importer importer = new(YukaListerModel.Instance.EnvModel.ExeFullFolder
						+ YlConstants.FOLDER_NAME_DOCUMENTS + YlConstants.FOLDER_NAME_SAMPLE_FOLDER_SETTINGS + YlConstants.FOLDER_NAME_SAMPLE_IMPORT + YlConstants.FILE_NAME_YUKA_LISTER_INFO_SAMPLE,
						true, true, null, YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token);
				importer.Import();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "サンプルインポート時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void DeviceRemoveComplete(String driveLetter)
		{
			YukaListerModel.Instance.ProjModel.SetFolderTaskDetailOfDriveToRemove(driveLetter);

			// 次回 UI 更新タイミングまでに削除が完了してしまっていても検索可能ファイル数が更新されるようにする
			_prevYukaListerWholeStatus = YukaListerStatus.__End__;
		}

		// --------------------------------------------------------------------
		// バージョン更新時の処理
		// --------------------------------------------------------------------
		private void DoVerChangedIfNeeded()
		{
			// 更新起動時とパス変更時の記録
			// 新規起動時は、両フラグが立つのでダブらないように注意
			String prevLaunchVer = YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer;
			Boolean verChanged = prevLaunchVer != YlConstants.APP_VER || YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchGeneration != YlConstants.APP_GENERATION;
			if (verChanged)
			{
				// ユーザーにメッセージ表示する前にログしておく
				if (String.IsNullOrEmpty(prevLaunchVer))
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "新規起動：" + YlConstants.APP_VER);
				}
				else
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "更新起動：" + prevLaunchVer + "→" + YlConstants.APP_VER);
				}
			}
			String prevLaunchPath = YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchPath;
			Boolean pathChanged = (String.Compare(prevLaunchPath, YukaListerModel.Instance.EnvModel.ExeFullPath, true) != 0);
			if (pathChanged && !String.IsNullOrEmpty(prevLaunchPath))
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "パス変更起動：" + prevLaunchPath + "→" + YukaListerModel.Instance.EnvModel.ExeFullPath);
			}

			// 更新起動時とパス変更時の処理
			if (verChanged || pathChanged)
			{
				YlCommon.LogEnvironmentInfo();
			}
			if (verChanged)
			{
				NewVersionLaunched();
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void FileSystemWatcherReportDatabase_Changed(Object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			SetStatusBarMessageWithInvoke(TraceEventType.Information, "リスト問題報告データベースが更新されました。");
			UpdateReportsBadge();
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void FileSystemWatcherYukariConfig_Changed(Object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			SetStatusBarMessageWithInvoke(TraceEventType.Information, "ゆかり設定ファイルが更新されました。");
			YukaListerModel.Instance.EnvModel.YlSettings.AnalyzeYukariConfig();
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private async void FileSystemWatcherYukariRequestDatabase_Changed(Object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			Debug.WriteLine("FileSystemWatcherYukariRequestDatabase_Changed() event " + Environment.TickCount.ToString("#,0"));

			// 再生曲遷移時やリスト操作時は頻繁にファイルが更新されるが、そのたびに Yurelin をアクティブ化するのは高負荷なため、ある程度まとめてアクティブ化する
			if (_fileSystemWatcherYukariRequestDatabaseDelaying)
			{
				_fileSystemWatcherYukariRequestDatabaseDelayingQueue = true;
				return;
			}

			// 初回はすぐにアクティブ化（全消去時にすみやかに検知できるように）
			_fileSystemWatcherYukariRequestDatabaseDelaying = true;
			_fileSystemWatcherYukariRequestDatabaseDelayingQueue = false;
			YlCommon.ActivateYurelinIfNeeded();

			await Task.Delay(YlConstants.UPDATE_YUKARI_STATISTICS_DELAY_TIME);
			if (_fileSystemWatcherYukariRequestDatabaseDelayingQueue)
			{
				// 遅延中に更新があれば再度アクティブ化
				YlCommon.ActivateYurelinIfNeeded();
			}
			_fileSystemWatcherYukariRequestDatabaseDelaying = false;
		}

		// --------------------------------------------------------------------
		// フォルダー設定
		// --------------------------------------------------------------------
		private void FolderSettings()
		{
			if (SelectedTargetFolderInfo == null)
			{
				return;
			}

			using MusicInfoContextDefault musicInfoContextDefault = new();
			DateTime musicInfoDbTimeBak = musicInfoContextDefault.LastWriteDateTime();

			// ViewModel 経由でフォルダー設定ウィンドウを開く
			using FolderSettingsWindowViewModel folderSettingsWindowViewModel = new(SelectedTargetFolderInfo.TargetPath);
			Messenger.Raise(new TransitionMessage(folderSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_FOLDER_SETTINGS_WINDOW));

			// フォルダー設定の有無の表示を更新
			// キャンセルでも実行（設定削除→キャンセルの場合はフォルダー設定の有無が変わる）
			YukaListerModel.Instance.ProjModel.SetFolderSettingsStatusToUnchecked(SelectedTargetFolderInfo.TargetPath);
			UpdateDataGrid();

			// 楽曲情報データベースが更新された場合は同期を行う
			if (musicInfoContextDefault.LastWriteDateTime() != musicInfoDbTimeBak)
			{
				YlCommon.ActivateSyclinIfNeeded();
			}
		}

#if false
		// --------------------------------------------------------------------
		// インストールフォルダーについての警告メッセージ
		// --------------------------------------------------------------------
		private static String? InstallWarningMessage()
		{
			if (YukaListerModel.Instance.EnvModel.ExeFullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
					|| YukaListerModel.Instance.EnvModel.ExeFullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)))
			{
				// 自動更新できない
				return YlConstants.APP_NAME_J + " が Program Files フォルダー配下にインストールされているため、正常に動作しません。\n"
						+ "他のフォルダー（例えば C:\\xampp\\htdocs）配下にインストールしてください。";
			}
			return null;
		}
#endif

		// --------------------------------------------------------------------
		// 新バージョンで初回起動された時の処理を行う
		// --------------------------------------------------------------------
		private void NewVersionLaunched()
		{
			String newVerMsg;
			TraceEventType type = TraceEventType.Information;

			// α・β警告、ならびに、更新時のメッセージ（2022/01/16）
			// 新規・更新のご挨拶
			if (String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer))
			{
				// 新規
				newVerMsg = "【初回起動】\n\n";
				newVerMsg += YlConstants.APP_NAME_J + "をダウンロードしていただき、ありがとうございます。";
			}
			else
			{
				// 更新
				newVerMsg = "【更新起動】\n\n";
				newVerMsg += YlConstants.APP_NAME_J + "が更新されました。\n";
				newVerMsg += "更新内容については［ヘルプ→改訂履歴］メニューをご参照ください。";
			}

			// α・βの注意
			if (YlConstants.APP_VER.Contains('α'))
			{
				newVerMsg += "\n\nこのバージョンは開発途上のアルファバージョンです。\n"
						+ "使用前にヘルプをよく読み、注意してお使い下さい。";
				type = TraceEventType.Warning;
			}
			else if (YlConstants.APP_VER.Contains('β'))
			{
				newVerMsg += "\n\nこのバージョンは開発途上のベータバージョンです。\n"
						+ "使用前にヘルプをよく読み、注意してお使い下さい。";
				type = TraceEventType.Warning;
			}

			// 表示
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(type, newVerMsg);
			SaveExitStatus();

#if !DISTRIB_STORE
			// Zone ID 削除
			CommonWindows.DeleteZoneID(YukaListerModel.Instance.EnvModel.ExeFullFolder, SearchOption.AllDirectories);

			// パスの注意
			String? installMsg = InstallWarningMessage();
			if (!String.IsNullOrEmpty(installMsg))
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Warning, installMsg);
			}
#endif

			// サンプルインポート
			ImportSampleIfNeeded();
		}

		// --------------------------------------------------------------------
		// プレビュー用サーバーが実行中なら終了
		// --------------------------------------------------------------------
		private async ValueTask QuitServerIfNeededAsync()
		{
			if (_webServer == null)
			{
				return;
			}

			Task task = _webServer.QuitAsync();
			_webServer = null;
			await task;
		}

		// --------------------------------------------------------------------
		// 終了時の状態を保存
		// --------------------------------------------------------------------
		private void SaveExitStatus()
		{
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchPath = YukaListerModel.Instance.EnvModel.ExeFullPath;
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchGeneration = YlConstants.APP_GENERATION;
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer = YlConstants.APP_VER;
			YukaListerModel.Instance.EnvModel.YlSettings.DesktopBounds = new Rect(Left, Top, Width, Height);
			YukaListerModel.Instance.EnvModel.YlSettings.Save();
		}

		// --------------------------------------------------------------------
		// リスト問題報告データベースの監視設定
		// --------------------------------------------------------------------
		private void SetFileSystemWatcherReportDatabase()
		{
			if (YukaListerModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				String path = DbCommon.ReportDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings);
				String? folder = Path.GetDirectoryName(path);
				if (!String.IsNullOrEmpty(folder))
				{
					_fileSystemWatcherReportDatabase.Path = folder;
					_fileSystemWatcherReportDatabase.Filter = Path.GetFileName(path);
					_fileSystemWatcherReportDatabase.EnableRaisingEvents = true;
					return;
				}
			}

			_fileSystemWatcherReportDatabase.EnableRaisingEvents = false;
		}

		// --------------------------------------------------------------------
		// ゆかり設定ファイル config.ini の監視設定
		// --------------------------------------------------------------------
		private void SetFileSystemWatcherYukariConfig()
		{
			if (YukaListerModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				String? path = Path.GetDirectoryName(YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPath());
				String filter = Path.GetFileName(YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPath());
				if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(filter))
				{
					_fileSystemWatcherYukariConfig.Path = path;
					_fileSystemWatcherYukariConfig.Filter = filter;
					_fileSystemWatcherYukariConfig.EnableRaisingEvents = true;
					return;
				}
			}

			_fileSystemWatcherYukariConfig.EnableRaisingEvents = false;
		}

		// --------------------------------------------------------------------
		// ゆかり予約ファイル request.db の監視設定
		// --------------------------------------------------------------------
		private void SetFileSystemWatcherYukariRequestDatabase()
		{
			if (YukaListerModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid()
					&& YukaListerModel.Instance.EnvModel.YlSettings.IsYukariRequestDatabasePathValid())
			{
				String? path = Path.GetDirectoryName(YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath());
				String filter = Path.GetFileName(YukaListerModel.Instance.EnvModel.YlSettings.YukariRequestDatabasePath());
				if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(filter))
				{
					_fileSystemWatcherYukariRequestDatabase.Path = path;
					_fileSystemWatcherYukariRequestDatabase.Filter = filter;
					_fileSystemWatcherYukariRequestDatabase.EnableRaisingEvents = true;
					return;
				}
			}

			_fileSystemWatcherYukariRequestDatabase.EnableRaisingEvents = false;
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体の動作状況に応じて背景を設定
		// --------------------------------------------------------------------
		private void SetYukaListerStatusBackground(YukaListerStatus currentWholeStatus)
		{
			YukaListerStatusBackground = currentWholeStatus switch
			{
				YukaListerStatus.Error => YlConstants.BRUSH_STATUS_ERROR,
				YukaListerStatus.Running => YlConstants.BRUSH_STATUS_RUNNING,
				_ => YlConstants.BRUSH_STATUS_DONE,
			};
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体の動作状況 URL に応じてカーソルを設定
		// --------------------------------------------------------------------
		private void SetYukaListerStatusCursor()
		{
			if (String.IsNullOrEmpty(_labelYukaListerStatusUrl))
			{
				YukaListerStatusCursor = null;
			}
			else
			{
				YukaListerStatusCursor = Cursors.Hand;
			}
		}

		// --------------------------------------------------------------------
		// Web サーバー設定が有効ならゆかり用の Web サーバーを開始
		// --------------------------------------------------------------------
		private void StartWebServerIfNeeded()
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview)
			{
				return;
			}
			if (_webServer != null)
			{
				return;
			}

			_webServer = new WebServer();
			_webServer.Start();
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：IsOpen が変更された
		// --------------------------------------------------------------------
		private void TargetFolderInfoIsOpenChanged(TargetFolderInfo targetFolderInfo)
		{
			try
			{
				YukaListerModel.Instance.ProjModel.UpdateTargetFolderInfosVisible(targetFolderInfo);
				UpdateDataGrid();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "IsOpen 変更時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：UI 表示を更新
		// --------------------------------------------------------------------
		private void TimerUpdateUi_Tick(Object? sender, EventArgs e)
		{
			try
			{
				_timerUpdateUi.Stop();

				// 常に EnvModel.YukaListerWholeStatus を参照していると、表示中は Running だったのに _prevYukaListerWholeStatus に代入する頃には Ready になっており、
				// 次回更新時に Ready 同士と判定されて更新されない、という事態が起こりえるので、一度参照した EnvModel.YukaListerWholeStatus を継承させるようにする
				YukaListerStatus currentWholeStatus = YukaListerModel.Instance.EnvModel.YukaListerWholeStatus;
				UpdateUi(currentWholeStatus);
				_prevYukaListerWholeStatus = currentWholeStatus;

				DisplayNebulaCoreError();

				_timerUpdateUi.Start();
			}
			catch (Exception excep)
			{
				// 定期的にタイマーエラーが表示されることのないよう、エラー発生時はタイマーを再開しない
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイマー時エラー：\n" + excep.Message + "\n再起動してください。");
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// DataGrid 表示を更新
		// --------------------------------------------------------------------
		private void UpdateDataGrid()
		{
			// 先に Dirty フラグをクリア（後にすると、更新中に他のスレッドが立てたフラグもクリアしてしまうため）
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = false;
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = false;

			// 更新
			TargetFolderInfosVisible = YukaListerModel.Instance.ProjModel.TargetFolderInfosVisible();
		}

		// --------------------------------------------------------------------
		// 検索可能ファイル数を更新
		// --------------------------------------------------------------------
		private void UpdateNumRecordsLabel()
		{
			if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
			{
				_numFounds = 0;
			}
			else
			{
				using ListContextInDisk listContextInDisk = new();
				_numFounds = listContextInDisk.Founds.Count();
			}
			NumRecordsLabel = _numFounds.ToString("#,0");
			ButtonTFoundsClickedCommand.RaiseCanExecuteChanged();
		}

		// --------------------------------------------------------------------
		// リスト問題報告のバッジを更新
		// --------------------------------------------------------------------
		public void UpdateReportsBadge()
		{
			Int32 numProgress = 0;
			if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
			{
			}
			else
			{
				using ReportContext reportContext = new();
				reportContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				numProgress = reportContext.Reports.Where(x => x.Status <= (Int32)ReportStatus.Progress).Count();
			}

#if DEBUGz
			numProgress = 999;
#endif
			if (numProgress == 0)
			{
				ReportsBadge = null;
			}
			else
			{
				ReportsBadge = numProgress.ToString();
			}
		}

		// --------------------------------------------------------------------
		// UI 表示を更新
		// --------------------------------------------------------------------
		private void UpdateUi(YukaListerStatus currentWholeStatus)
		{
			// ウィンドウ上部の情報
			if (currentWholeStatus == YukaListerStatus.Ready && _prevYukaListerWholeStatus == YukaListerStatus.Ready)
			{
				// 引き続き Ready の場合は更新不要
			}
			else
			{
				UpdateYukaListerStatusLabel(currentWholeStatus);
				UpdateNumRecordsLabel();
			}

			// DataGrid
			if (YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged || YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated)
			{
				UpdateDataGrid();
			}
		}

		// --------------------------------------------------------------------
		// 環境系のステータスを更新
		// ToDo: YlCommon に移動し、スプラッシュウィンドウ等でもステータスを利用できるようにしたいが、YukaListerStatusLabel 等もからむのが厄介
		// --------------------------------------------------------------------
		private void UpdateYukaListerEnvironmentStatus()
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				// ゆかり設定ファイルエラー
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Environment] = YukaListerStatus.Error;
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Environment]
						= YukaListerStatusLabel = "ゆかり設定ファイルが正しく指定されていません。";
				_labelYukaListerStatusUrl = URL_BAD_YUKARI_CONFIG;
			}
			else
			{
				// 正常
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Environment] = YukaListerStatus.Ready;
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Environment]
						= YlConstants.APP_NAME_J + "は正常に動作しています。";
				_labelYukaListerStatusUrl = null;
			}

			// 表示を強制更新
			_prevYukaListerWholeStatus = YukaListerStatus.__End__;
			UpdateUi(YukaListerModel.Instance.EnvModel.YukaListerWholeStatus);
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体の動作ラベルを更新
		// --------------------------------------------------------------------
		private void UpdateYukaListerStatusLabel(YukaListerStatus currentWholeStatus)
		{
			switch (currentWholeStatus)
			{
				case YukaListerStatus.Ready:
					YukaListerStatusLabel = YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Environment];
					break;
				case YukaListerStatus.Running:
					if (YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Startup] == YukaListerStatus.Running)
					{
						// 起動時処理
						YukaListerStatusLabel = YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Startup];
					}
					else if (YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Sifolin] == YukaListerStatus.Running)
					{
						// Sifolin
						TargetFolderInfo? targetFolderInfo = YukaListerModel.Instance.ProjModel.RunningTargetFolderInfo();
						if (targetFolderInfo == null)
						{
							// タイミングによっては一時的に null になることがありえる
							YukaListerStatusLabel = "検索データ作成中...";
						}
						else
						{
							YukaListerStatusLabel = targetFolderInfo.FolderTaskDetail switch
							{
								FolderTaskDetail.CacheToDisk => YlConstants.RUNNING_CACHE_TO_DISK,
								FolderTaskDetail.FindSubFolders => YlConstants.RUNNING_FIND_SUB_FOLDERS,
								FolderTaskDetail.AddFileNames => YlConstants.RUNNING_ADD_FILE_NAMES,
								FolderTaskDetail.AddInfos => YlConstants.RUNNING_ADD_INFOS,
								FolderTaskDetail.Remove => YlConstants.RUNNING_REMOVE,
								_ => String.Empty,
							} + "...\n" + targetFolderInfo.TargetPath;
						}
					}
					else if (YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Kamlin] == YukaListerStatus.Running)
					{
						YukaListerStatusLabel = "リスト更新中...";
					}
					else
					{
						// 残り香の状態
						YukaListerStatusLabel = "処理中...";
					}
					break;
				case YukaListerStatus.Error:
					YukaListerStatusLabel = YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Environment];
					break;
				default:
					Debug.Assert(false, "UpdateYukaListerStatusLabel() bad status");
					break;
			}
			SetYukaListerStatusBackground(currentWholeStatus);
			SetYukaListerStatusCursor();
		}
	}
}
