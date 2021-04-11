// ============================================================================
// 
// メインウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SerializableSettings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class MainWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel(SplashWindowViewModel splashWindowViewModel)
		{
			_splashWindowViewModel = splashWindowViewModel;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel()
		{
			// 警告抑止用にメンバーを null! で初期化
			_splashWindowViewModel = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

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

		// 検索可能ファイル数
		private String _numRecordsLabel = String.Empty;
		public String NumRecordsLabel
		{
			get => _numRecordsLabel;
			set => RaisePropertyChangedIfSet(ref _numRecordsLabel, value);
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

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

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
						SetStatusBarMessage(Common.TRACE_EVENT_TYPE_STATUS, "リムーバブルドライブが接続されました：" + deviceChangeInfo.DriveLetter);
						await DeviceArrivalAsync(deviceChangeInfo.DriveLetter);
						break;
					case DBT.DBT_DEVICEREMOVECOMPLETE:
						SetStatusBarMessage(Common.TRACE_EVENT_TYPE_STATUS, "リムーバブルドライブが切断されました：" + deviceChangeInfo.DriveLetter);
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

				if (MessageBox.Show(SelectedTargetFolderInfo.ParentPath + "\nおよびサブフォルダーをゆかり検索対象から削除しますか？",
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
				if (SelectedTargetFolderInfo == null)
				{
					return;
				}

				using FolderSettingsWindowViewModel folderSettingsWindowViewModel = new();
				folderSettingsWindowViewModel.TargetPath = SelectedTargetFolderInfo.Path;
				Messenger.Raise(new TransitionMessage(folderSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_FOLDER_SETTINGS_WINDOW));
#if false
				using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(mEnvironment))
				{
					DateTime aMusicInfoDbTimeBak = aMusicInfoDbInDisk.LastWriteTime();

					// ViewModel 経由でフォルダー設定ウィンドウを開く
					using (FolderSettingsWindowViewModel aFolderSettingsWindowViewModel = new FolderSettingsWindowViewModel())
					{
						aFolderSettingsWindowViewModel.Environment = mEnvironment;
						aFolderSettingsWindowViewModel.PathExLen = aTargetFolderInfo.Path;
						mMainWindowViewModel.Messenger.Raise(new TransitionMessage(aFolderSettingsWindowViewModel, "OpenFolderSettingsWindow"));
					}

					// フォルダー設定の有無の表示を更新
					// キャンセルでも実行（設定削除→キャンセルの場合はフォルダー設定の有無が変わる）
					lock (mTargetFolderInfos)
					{
						Int32 aIndex = mTargetFolderInfos.IndexOf(aTargetFolderInfo);
						if (aIndex < 0)
						{
							throw new Exception("フォルダー設定有無を更新する対象が見つかりません。");
						}
						while (aIndex < mTargetFolderInfos.Count)
						{
							if (!mTargetFolderInfos[aIndex].Path.StartsWith(aTargetFolderInfo.Path))
							{
								break;
							}
							mTargetFolderInfos[aIndex].FolderExcludeSettingsStatus = FolderExcludeSettingsStatus.Unchecked;
							mTargetFolderInfos[aIndex].FolderSettingsStatus = FolderSettingsStatus.Unchecked;
							mDirtyDg = true;
							aIndex++;
						}
					}
					UpdateDirtyDgWithInvoke();

					// 楽曲情報データベースが更新された場合は同期を行う
					DateTime aMusicInfoDbTime = aMusicInfoDbInDisk.LastWriteTime();
					if (aMusicInfoDbTime != aMusicInfoDbTimeBak)
					{
						RunSyncClientIfNeeded();
					}
				}
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public async void AddFolderSelected(FolderSelectionMessage folderSelectionMessage)
		{
			try
			{
				if (String.IsNullOrEmpty(folderSelectionMessage.Response))
				{
					return;
				}

				// AddTargetFolderAsync() に時間を要することがあるので表示を更新しておく
				Cursor = Cursors.Wait;
				await YukaListerModel.Instance.ProjModel.AddTargetFolderAsync(folderSelectionMessage.Response);
				UpdateDataGrid();

				// 次回 UI 更新タイミングまでに追加が完了してしまっていても検索可能ファイル数が更新されるようにする
				_prevYukaListerWholeStatus = YukaListerStatus.__End__;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "追加フォルダー選択時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				Cursor = null;
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

				// イベントハンドラー
				TargetFolderInfo.IsOpenChanged = TargetFolderInfoIsOpenChanged;

				// ステータスバー
				//ClearStatusBarMessage();

				// スプラッシュウィンドウを閉じる
				_splashWindowViewModel.Close();

				// プログラムエラーチェック
				Debug.Assert(YlConstants.FOLDER_SETTINGS_STATUS_TEXTS.Length == (Int32)FolderSettingsStatus.__End__, "MainWindow.Initialize() bad FOLDER_SETTINGS_STATUS_TEXTS length");

				// 環境の変化に対応
				DoVerChangedIfNeeded();
				//LaunchUpdaterIfNeeded();

				// 動作状況
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Startup] = YukaListerStatus.Running;
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Startup] = "前回のゆかり検索対象フォルダーを確認中...";
				UpdateYukaListerEnvironmentStatus();
				UpdateUi(YukaListerModel.Instance.EnvModel.YukaListerWholeStatus);

				// UI 更新タイマー
				_timerUpdateUi.Interval = TimeSpan.FromSeconds(1.0);
#if DEBUGz
				_timerUpdateUi.Interval = TimeSpan.FromSeconds(5.0);
#endif
				_timerUpdateUi.Tick += new EventHandler(TimerUpdateUi_Tick);
				_timerUpdateUi.Start();

				// 時間がかかるかもしれない処理を非同期で実行
				await AutoTargetAllDrivesAsync();

#if DEBUGz
				CacheContext.CreateContext("D:", out _);
#endif

#if DEBUGz
				Debug.WriteLine("Exists 1: " + File.Exists(@"D:\TempD\TestYl\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\a.txt"));
				Debug.WriteLine("Exists 2: " + File.Exists(@"D:\TempD\TestYl\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\12345678901234567890123456789\FolderNameLength.txt"));
#endif

#if DEBUGz
				TargetFolderInfo t1 = new("D:\\hoge\\folder", "D:\\hoge\\folder", 0);
				t1.HasChildren = true;
				t1.NumTotalFolders = 2;
				//targetFolderInfo.IsOpen = true;
				TestTargetFolderInfo = t1;

				List<TargetFolderInfo> targetFolderInfos = new();
				targetFolderInfos.Add(t1);
				TargetFolderInfo t2 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブA", 1);
				t2.HasChildren = true;
				t2.NumTotalFolders = 2;
				targetFolderInfos.Add(t2);
				TargetFolderInfo t3 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブA\\さらにサブ", 2);
				targetFolderInfos.Add(t3);
				TargetFolderInfo t4 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブA\\さらにサブ2", 2);
				targetFolderInfos.Add(t4);
				TargetFolderInfo t5 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブB", 1);
				t5.HasChildren = true;
				t5.NumTotalFolders = 2;
				targetFolderInfos.Add(t5);
				TargetFolderInfosVisible = targetFolderInfos;
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

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override async void Dispose(bool disposing)
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

				// 終了処理
				await YukaListerModel.Instance.EnvModel.QuitAllCoresAsync();
				SaveExitStatus();
				_isDisposed = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ破棄時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// スプラッシュウィンドウ
		private readonly SplashWindowViewModel _splashWindowViewModel;

		// UI 更新用タイマー
		private DispatcherTimer _timerUpdateUi = new();

		// 前回 UI 更新時のゆかりすたー NEBULA 全体の動作状況
		private YukaListerStatus _prevYukaListerWholeStatus = YukaListerStatus.__End__;

		// Dispose フラグ
		private Boolean _isDisposed = false;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

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
#if DEBUGz
			await Task.Delay(1000);
#endif
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
		private async Task DeviceArrivalCoreAsync(String driveLetter)
		{
			AutoTargetInfo autoTargetInfo = new(driveLetter);
			autoTargetInfo.Load();
			foreach (String folder in autoTargetInfo.Folders)
			{
				await YukaListerModel.Instance.ProjModel.AddTargetFolderAsync(driveLetter + folder);
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
		// インストールフォルダーについての警告メッセージ
		// --------------------------------------------------------------------
		private String? InstallWarningMessage()
		{
			if (YukaListerModel.Instance.EnvModel.ExeFullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
					|| YukaListerModel.Instance.EnvModel.ExeFullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)))
			{
				// 自動更新できない
				return YlConstants.APP_NAME_J + " が Program Files フォルダー配下にインストールされているため、正常に動作しません。\n"
						+ "他のフォルダー（例えば C:\\MyApp）配下にインストールしてください。";
			}
			return null;
		}

		// --------------------------------------------------------------------
		// 新バージョンで初回起動された時の処理を行う
		// --------------------------------------------------------------------
		private void NewVersionLaunched()
		{
			String newVerMsg;
			TraceEventType type = TraceEventType.Information;

			// α・β警告、ならびに、更新時のメッセージ（2017/01/09）
			// 新規・更新のご挨拶
			if (String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer))
			{
				// 新規
				newVerMsg = "【初回起動】\n\n";
				newVerMsg += YlConstants.APP_NAME_J + "をダウンロードしていただき、ありがとうございます。";
			}
			else
			{
				newVerMsg = "【更新起動】\n\n";
				newVerMsg += YlConstants.APP_NAME_J + "を更新していただき、ありがとうございます。\n";
				newVerMsg += "更新内容については［ヘルプ→改訂履歴］メニューをご参照ください。";
			}

			// α・βの注意
			if (YlConstants.APP_VER.Contains("α"))
			{
				newVerMsg += "\n\nこのバージョンは開発途上のアルファバージョンです。\n"
						+ "使用前にヘルプをよく読み、注意してお使い下さい。";
				type = TraceEventType.Warning;
			}
			else if (YlConstants.APP_VER.Contains("β"))
			{
				newVerMsg += "\n\nこのバージョンは開発途上のベータバージョンです。\n"
						+ "使用前にヘルプをよく読み、注意してお使い下さい。";
				type = TraceEventType.Warning;
			}

			// 表示
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(type, newVerMsg);
			SaveExitStatus();

			// Zone ID 削除
			CommonWindows.DeleteZoneID(YukaListerModel.Instance.EnvModel.ExeFullFolder, SearchOption.AllDirectories);

			// パスの注意
			String? installMsg = InstallWarningMessage();
			if (!String.IsNullOrEmpty(installMsg))
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Warning, installMsg);
			}
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
		// ステータスバーにメッセージを表示
		// --------------------------------------------------------------------
		public void SetStatusBarMessage(TraceEventType traceEventType, String msg)
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
			// ToDo: IsMainWindowDataGridItemUpdated のみが立っていた場合は効率よい処理方法があるのではないか
			TargetFolderInfosVisible = YukaListerModel.Instance.ProjModel.TargetFolderInfosVisible();
		}

		// --------------------------------------------------------------------
		// 検索可能ファイル数を更新
		// --------------------------------------------------------------------
		private void UpdateNumRecordsLabel()
		{
			using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> founds);
			NumRecordsLabel = founds.Count().ToString("#,0");
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
		// --------------------------------------------------------------------
		private void UpdateYukaListerEnvironmentStatus()
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				// ゆかり設定ファイルエラー
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Environment] = YukaListerStatus.Error;
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Environment]
						= YukaListerStatusLabel = "ゆかり設定ファイルが正しく指定されていません。";
			}
			else
			{
				// 正常
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Environment] = YukaListerStatus.Ready;
				YukaListerModel.Instance.EnvModel.YukaListerPartsStatusMessage[(Int32)YukaListerPartsStatusIndex.Environment]
						= YlConstants.APP_NAME_J + "は正常に動作しています。";
			}
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
					else
					{
						TargetFolderInfo? targetFolderInfo = YukaListerModel.Instance.ProjModel.RunningTargetFolderInfo();
						if (targetFolderInfo == null)
						{
							// タイミングによっては一時的に null になることがありえるが、その場合は何もしない
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
							} + "...\n" + targetFolderInfo.Path;
						}
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
		}
	}
}
