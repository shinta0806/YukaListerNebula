// ============================================================================
// 
// 環境設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.OutputWriters;
using YukaLister.Models.SerializableSettings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.OutputSettingsWindowViewModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	public class YlSettingsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public YlSettingsWindowViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		#region ウィンドウのプロパティー

		// 選択されているタブ（ドロップ先識別用）
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

		// UpdaterLauncher
		private UpdaterLauncher? _updaterLauncher;
		public UpdaterLauncher? UpdaterLauncher
		{
			get => _updaterLauncher;
			set => RaisePropertyChangedIfSet(ref _updaterLauncher, value);
		}

		#endregion

		#region 設定タブのプロパティー

		// ゆかり設定ファイル
		private String _yukariConfigPathSeed = String.Empty;
		public String YukariConfigPathSeed
		{
			get => _yukariConfigPathSeed;
			set
			{
				if (RaisePropertyChangedIfSet(ref _yukariConfigPathSeed, value))
				{
					try
					{
						// ゆかり用リスト出力先フォルダーの算出
						YlSettings tempYlSettings = new();
						tempYlSettings.YukariConfigPathSeed = _yukariConfigPathSeed;
						YukariListFolder = Path.GetDirectoryName(DbCommon.ListDatabasePath(tempYlSettings));
					}
					catch (Exception)
					{
						// エラーは無視する
					}
				}
			}
		}

		// リムーバブルメディア接続時、前回のフォルダーを自動的に追加する
		private Boolean _addFolderOnDeviceArrived;
		public Boolean AddFolderOnDeviceArrived
		{
			get => _addFolderOnDeviceArrived;
			set => RaisePropertyChangedIfSet(ref _addFolderOnDeviceArrived, value);
		}

		// ゆかりでのプレビューを可能にするか
		private Boolean _provideYukariPreview;
		public Boolean ProvideYukariPreview
		{
			get => _provideYukariPreview;
			set => RaisePropertyChangedIfSet(ref _provideYukariPreview, value);
		}

		// ID 接頭辞
		private String? _idPrefix;
		public String? IdPrefix
		{
			get => _idPrefix;
			set => RaisePropertyChangedIfSet(ref _idPrefix, value);
		}

		#endregion

		#region リスト対象タブのプロパティー

		// リスト化対象ファイルの拡張子
		public ObservableCollection<String> TargetExts { get; set; } = new();

		// リストで選択されている拡張子
		private String? _selectedTargetExt;
		public String? SelectedTargetExt
		{
			get => _selectedTargetExt;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedTargetExt, value))
				{
					ButtonRemoveExtClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 追加したい拡張子
		private String? _addingTargetExt;
		public String? AddingTargetExt
		{
			get => _addingTargetExt;
			set
			{
				if (RaisePropertyChangedIfSet(ref _addingTargetExt, value))
				{
					ButtonAddExtClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}
		#endregion

		#region リスト出力タブのプロパティー

		// ゆかり用リスト出力先フォルダー
		private String? _yukariListFolder;
		public String? YukariListFolder
		{
			get => _yukariListFolder;
			set => RaisePropertyChangedIfSet(ref _yukariListFolder, value);
		}

		// ゆかりリクエスト用リスト出力前に確認する
		private Boolean _confirmOutputYukariList;
		public Boolean ConfirmOutputYukariList
		{
			get => _confirmOutputYukariList;
			set => RaisePropertyChangedIfSet(ref _confirmOutputYukariList, value);
		}

		// リスト出力クラス群
		public ObservableCollection<OutputWriter> OutputWriters { get; set; } = new();

		// 選択されたリスト出力クラス
		private OutputWriter? _selectedOutputWriter;
		public OutputWriter? SelectedOutputWriter
		{
			get => _selectedOutputWriter;
			set => RaisePropertyChangedIfSet(ref _selectedOutputWriter, value);
		}

		// リスト出力先フォルダー
		private String? _listFolder;
		public String? ListFolder
		{
			get => _listFolder;
			set => RaisePropertyChangedIfSet(ref _listFolder, value);
		}

		#endregion

		#region メンテナンスタブのプロパティー

		// ゆかりすたーの最新情報・更新版を自動的に確認する
		private Boolean _checkRss;
		public Boolean CheckRss
		{
			get => _checkRss;
			set
			{
				if (_checkRss && !value
						&& MessageBox.Show("最新情報・更新版の確認を無効にすると、" + YlConstants.APP_NAME_J
						+ "の新版がリリースされても自動的にインストールされず、古いバージョンを使い続けることになります。\n"
						+ "本当に無効にしてもよろしいですか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
						!= MessageBoxResult.Yes)
				{
					return;
				}

				RaisePropertyChangedIfSet(ref _checkRss, value);
			}
		}

		// プログレスバー表示
		private Visibility _progressBarCheckRssVisibility;
		public Visibility ProgressBarCheckRssVisibility
		{
			get => _progressBarCheckRssVisibility;
			set
			{
				if (RaisePropertyChangedIfSet(ref _progressBarCheckRssVisibility, value))
				{
					ButtonCheckRssClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

#if false
		// デフォルトログファイル名
		public String DefaultLogFileName
		{
			get => "YukaListerLog_" + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss");
			set { }
		}
#endif

		// 楽曲情報データベースを同期する
		private Boolean _syncMusicInfoDb;
		public Boolean SyncMusicInfoDb
		{
			get => _syncMusicInfoDb;
			set
			{
				if (RaisePropertyChangedIfSet(ref _syncMusicInfoDb, value))
				{
					ButtonRegetClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// サーバー URL
		private String? _syncServer;
		public String? SyncServer
		{
			get => _syncServer;
			set => RaisePropertyChangedIfSet(ref _syncServer, value);
		}

		// アカウント名
		private String? _syncAccount;
		public String? SyncAccount
		{
			get => _syncAccount;
			set => RaisePropertyChangedIfSet(ref _syncAccount, value);
		}

		// パスワード
		private String? _syncPassword;
		public String? SyncPassword
		{
			get => _syncPassword;
			set => RaisePropertyChangedIfSet(ref _syncPassword, value);
		}

		#endregion

		#region インポートタブのプロパティー

		// ゆかりすたーでエクスポートしたファイルをインポート
		private Boolean _importYukaListerMode;
		public Boolean ImportYukaListerMode
		{
			get => _importYukaListerMode;
			set
			{
				if (RaisePropertyChangedIfSet(ref _importYukaListerMode, value))
				{
					ButtonBrowseImportYukaListerClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// ゆかりすたーでエクスポートしたファイルのパス
		private String? _importYukaListerPath;
		public String? ImportYukaListerPath
		{
			get => _importYukaListerPath;
			set => RaisePropertyChangedIfSet(ref _importYukaListerPath, value);
		}

		// タグ情報をインポートする
		private Boolean _importTag;
		public Boolean ImportTag
		{
			get => _importTag;
			set => RaisePropertyChangedIfSet(ref _importTag, value);
		}

		#endregion

		#region エクスポートタブのプロパティー

		// ゆかりすたー情報ファイルのパス
		private String? _exportYukaListerPath;
		public String? ExportYukaListerPath
		{
			get => _exportYukaListerPath;
			set => RaisePropertyChangedIfSet(ref _exportYukaListerPath, value);
		}

		#endregion

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// ゆかり用リストデータベース（作業用インメモリ）
		//public YukariListDatabaseInMemory? YukariListDbInMemory { get; set; }

		// 強制再取得をユーザーから指示されたか
		public Boolean RegetSyncDataNeeded { get; set; }

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ウィンドウのコマンド

		#region ヘルプリンクの制御
		public ListenerCommand<String>? HelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
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

		public void TabControlFileDrop(String[] files)
		{
			try
			{
				switch (SelectedTabIndex)
				{
					case 0:
						TabItemSettingsFileDrop(files);
						break;
					case 4:
						TabItemImportFileDrop(files);
						break;
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タブコントロールファイルドロップ時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ちょちょいと自動更新の UI が表示された
		private ViewModelCommand? _updaterUiDisplayedCommand;

		public ViewModelCommand UpdaterUiDisplayedCommand
		{
			get
			{
				if (_updaterUiDisplayedCommand == null)
				{
					_updaterUiDisplayedCommand = new ViewModelCommand(UpdaterUiDisplayed);
				}
				return _updaterUiDisplayedCommand;
			}
		}

		public void UpdaterUiDisplayed()
		{
			Debug.WriteLine("UpdaterUiDisplayed()");
			ProgressBarCheckRssVisibility = Visibility.Hidden;
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
				YukaListerModel.Instance.EnvModel.YlSettings.Save();
				IsOk = true;
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#endregion

		#region 設定タブのコマンド

		#region ゆかり設定ファイル参照ボタンの制御
		private ViewModelCommand? _buttonBrowseYukariConfigPathSeedClickedCommand;

		public ViewModelCommand ButtonBrowseYukariConfigPathSeedClickedCommand
		{
			get
			{
				if (_buttonBrowseYukariConfigPathSeedClickedCommand == null)
				{
					_buttonBrowseYukariConfigPathSeedClickedCommand = new ViewModelCommand(ButtonBrowseYukariConfigPathSeedClicked);
				}
				return _buttonBrowseYukariConfigPathSeedClickedCommand;
			}
		}

		public void ButtonBrowseYukariConfigPathSeedClicked()
		{
			try
			{
				String? path = PathByOpeningDialog("ゆかり設定ファイル", "ゆかり設定ファイル|" + YlConstants.FILE_NAME_YUKARI_CONFIG, YlConstants.FILE_NAME_YUKARI_CONFIG);
				if (path != null)
				{
					YukariConfigPathSeed = path;
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかり設定ファイル参照ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#endregion

		#region リスト対象タブのコマンド

		#region 追加ボタンの制御
		private ViewModelCommand? _buttonAddExtClickedCommand;

		public ViewModelCommand ButtonAddExtClickedCommand
		{
			get
			{
				if (_buttonAddExtClickedCommand == null)
				{
					_buttonAddExtClickedCommand = new ViewModelCommand(ButtonAddExtClicked, CanButtonAddExtClicked);
				}
				return _buttonAddExtClickedCommand;
			}
		}

		public Boolean CanButtonAddExtClicked()
		{
			return !String.IsNullOrEmpty(AddingTargetExt);
		}

		public void ButtonAddExtClicked()
		{
			try
			{
				String? ext = AddingTargetExt;

				// 入力が空の場合はボタンは押されないはずだが念のため
				if (String.IsNullOrEmpty(ext))
				{
					throw new Exception("拡張子を入力して下さい。");
				}

				// ワイルドカード等を除去
				ext = ext?.Replace("*", "");
				ext = ext?.Replace("?", "");
				ext = ext?.Replace(".", "");

				// 除去で空になっていないか
				if (String.IsNullOrEmpty(ext))
				{
					throw new Exception("有効な拡張子を入力して下さい。");
				}

				// 先頭にピリオド付加
				ext = "." + ext;

				// 小文字化
				ext = ext.ToLower();

				// 重複チェック
				if (TargetExts.Contains(ext))
				{
					throw new Exception("既に追加されています。");
				}

				// 追加
				TargetExts.Add(ext);
				SelectedTargetExt = ext;
				AddingTargetExt = null;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "追加ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 削除ボタンの制御
		private ViewModelCommand? _buttonRemoveExtClickedCommand;

		public ViewModelCommand ButtonRemoveExtClickedCommand
		{
			get
			{
				if (_buttonRemoveExtClickedCommand == null)
				{
					_buttonRemoveExtClickedCommand = new ViewModelCommand(ButtonRemoveExtClicked, CanButtonRemoveExtClicked);
				}
				return _buttonRemoveExtClickedCommand;
			}
		}

		public Boolean CanButtonRemoveExtClicked()
		{
			return !String.IsNullOrEmpty(SelectedTargetExt);
		}

		public void ButtonRemoveExtClicked()
		{
			try
			{
				// 選択されていない場合はボタンが押されないはずだが念のため
				if (String.IsNullOrEmpty(SelectedTargetExt))
				{
					throw new Exception("削除したい拡張子を選択してください。");
				}

				// 削除
				TargetExts.Remove(SelectedTargetExt!);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "削除ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#endregion

		#region リスト出力タブのコマンド

		#region ゆかり用リスト出力設定ボタンの制御
		private ViewModelCommand? _buttonYukariListSettingsClickedCommand;

		public ViewModelCommand ButtonYukariListSettingsClickedCommand
		{
			get
			{
				if (_buttonYukariListSettingsClickedCommand == null)
				{
					_buttonYukariListSettingsClickedCommand = new ViewModelCommand(ButtonYukariListSettingsClicked);
				}
				return _buttonYukariListSettingsClickedCommand;
			}
		}

		public void ButtonYukariListSettingsClicked()
		{
			try
			{
				YukariOutputWriter yukariOutputWriter = new();
				yukariOutputWriter.OutputSettings.Load();

				// ViewModel 経由でリスト出力設定ウィンドウを開く
				using OutputSettingsWindowViewModel outputSettingsWindowViewModel = yukariOutputWriter.CreateOutputSettingsWindowViewModel();
				Messenger.Raise(new TransitionMessage(outputSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_OUTPUT_SETTINGS_WINDOW));

				if (!outputSettingsWindowViewModel.IsOk)
				{
					return;
				}

				// 設定変更をすべての出力者に反映
				LoadOutputSettings();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかりリクエスト用リスト出力設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 閲覧用リスト出力設定ボタンの制御
		private ViewModelCommand? _buttonListSettingsClickedCommand;

		public ViewModelCommand ButtonListSettingsClickedCommand
		{
			get
			{
				if (_buttonListSettingsClickedCommand == null)
				{
					_buttonListSettingsClickedCommand = new ViewModelCommand(ButtonListSettingsClicked);
				}
				return _buttonListSettingsClickedCommand;
			}
		}

		public void ButtonListSettingsClicked()
		{
			try
			{
				if (SelectedOutputWriter == null)
				{
					return;
				}
				SelectedOutputWriter.OutputSettings.Load();

				// ViewModel 経由でリスト出力設定ウィンドウを開く
				using OutputSettingsWindowViewModel outputSettingsWindowViewModel = SelectedOutputWriter.CreateOutputSettingsWindowViewModel();
				Messenger.Raise(new TransitionMessage(outputSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_OUTPUT_SETTINGS_WINDOW));

				if (!outputSettingsWindowViewModel.IsOk)
				{
					return;
				}

				// 設定変更をすべての出力者に反映
				LoadOutputSettings();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "閲覧用出力設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 閲覧用リスト出力ボタンの制御
		private ViewModelCommand? _buttonOutputListClickedCommand;

		public ViewModelCommand ButtonOutputListClickedCommand
		{
			get
			{
				if (_buttonOutputListClickedCommand == null)
				{
					_buttonOutputListClickedCommand = new ViewModelCommand(ButtonOutputListClicked);
				}
				return _buttonOutputListClickedCommand;
			}
		}

		public void ButtonOutputListClicked()
		{
			try
			{
				// 確認
				if (String.IsNullOrEmpty(ListFolder))
				{
					throw new Exception("リスト出力先フォルダーを指定してください。");
				}

				// 出力クラスに出力先が渡るようにする
				// ウィンドウのキャンセルボタンが押された場合でも確定していることになる
				YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder = ListFolder;

				if (SelectedOutputWriter == null)
				{
					throw new Exception("出力形式を選択してください。");
				}

				if (YukaListerModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Running)
				{
					if (MessageBox.Show("データ更新中のため、今すぐリスト出力しても完全なリストにはなりません。\n今すぐリスト出力しますか？",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						return;
					}
				}

				// 出力
				SelectedOutputWriter.Output();
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "リスト出力が完了しました。");

				// 表示
				String outputFilePath = YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder + SelectedOutputWriter.TopFileName;
				try
				{
					YlCommon.ShellExecute(outputFilePath);
				}
				catch (Exception)
				{
					throw new Exception("出力先ファイルを開けませんでした。\n" + outputFilePath);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト出力ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#endregion

		#region メンテナンスタブのコマンド

		#region 今すぐ最新情報を確認するボタンの制御
		private ViewModelCommand? _buttonCheckRssClickedCommand;

		public ViewModelCommand ButtonCheckRssClickedCommand
		{
			get
			{
				if (_buttonCheckRssClickedCommand == null)
				{
					_buttonCheckRssClickedCommand = new ViewModelCommand(ButtonCheckRssClicked, CanButtonCheckRssClicked);
				}
				return _buttonCheckRssClickedCommand;
			}
		}

		public Boolean CanButtonCheckRssClicked()
		{
			return ProgressBarCheckRssVisibility != Visibility.Visible;
		}

		public void ButtonCheckRssClicked()
		{
			try
			{
				ProgressBarCheckRssVisibility = Visibility.Visible;
				UpdaterLauncher = YlCommon.CreateUpdaterLauncher(true, true, true, false);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "最新情報確認時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 設定のバックアップボタンの制御
		private ViewModelCommand? _buttonBackupClickedCommand;

		public ViewModelCommand ButtonBackupClickedCommand
		{
			get
			{
				if (_buttonBackupClickedCommand == null)
				{
					_buttonBackupClickedCommand = new ViewModelCommand(ButtonBackupClicked);
				}
				return _buttonBackupClickedCommand;
			}
		}

		public void ButtonBackupClicked()
		{
			try
			{
				String? path = PathBySavingDialog("設定のバックアップ", YlConstants.DIALOG_FILTER_SETTINGS_ARCHIVE, "YukaListerSettings_" + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss"));
				if (path == null)
				{
					return;
				}

				YlCommon.LogEnvironmentInfo();
				ZipFile.CreateFromDirectory(Common.UserAppDataFolderPath(), path, CompressionLevel.Optimal, true);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "設定のバックアップが完了しました。");
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ログ保存時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 強制的に合わせるボタンの制御
		private ViewModelCommand? _buttonRegetClickedCommand;

		public ViewModelCommand ButtonRegetClickedCommand
		{
			get
			{
				if (_buttonRegetClickedCommand == null)
				{
					_buttonRegetClickedCommand = new ViewModelCommand(ButtonRegetClicked, CanButtonRegetClicked);
				}
				return _buttonRegetClickedCommand;
			}
		}

		public Boolean CanButtonRegetClicked()
		{
			return SyncMusicInfoDb;
		}

		public void ButtonRegetClicked()
		{
			try
			{
				if (YukaListerModel.Instance.EnvModel.Syclin.IsActive)
				{
					throw new Exception("現在、同期処理を実行中のため、合わせられません。\n同期処理が終了してから合わせてください。");
				}

				if (MessageBox.Show("ローカルの楽曲情報データベースを全て削除してから、内容をサーバーに合わせます。\n"
						+ "タグ情報および、サーバーにアップロードしていないデータは全て失われます。\n"
						+ "事前にエクスポートすることをお薦めします。\n内容をサーバーに合わせてよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					return;
				}

				YukaListerModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = 0.0;
				RegetSyncDataNeeded = true;
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "環境設定ウィンドウを閉じると処理を開始します。");
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "強制的に合わせる時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#endregion

		#region インポートタブのコマンド

		#region ゆかりすたーファイル参照ボタンの制御
		private ViewModelCommand? _buttonBrowseImportYukaListerClickedCommand;

		public ViewModelCommand ButtonBrowseImportYukaListerClickedCommand
		{
			get
			{
				if (_buttonBrowseImportYukaListerClickedCommand == null)
				{
					_buttonBrowseImportYukaListerClickedCommand = new ViewModelCommand(ButtonBrowseImportYukaListerClicked, CanButtonBrowseImportYukaListerClicked);
				}
				return _buttonBrowseImportYukaListerClickedCommand;
			}
		}

		public Boolean CanButtonBrowseImportYukaListerClicked()
		{
			return ImportYukaListerMode;
		}

		public void ButtonBrowseImportYukaListerClicked()
		{
#if false
			try
			{
				String? aPath = PathByOpeningDialog("ゆかりすたー情報ファイル", "ゆかりすたー情報ファイル|*" + YlConstants.FILE_EXT_YLINFO + "|楽曲情報データベースバックアップ|*" + Common.FILE_EXT_BAK);
				if (aPath != null)
				{
					ImportYukaListerPath = aPath;
				}
			}
			catch (Exception oExcep)
			{
				Environment!.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかりすたー情報ファイル参照ボタンクリック時エラー：\n" + oExcep.Message);
				Environment.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + oExcep.StackTrace);
			}
#endif
		}
		#endregion

		#region インポートボタンの制御
		private ViewModelCommand? _buttonImportClickedCommand;

		public ViewModelCommand ButtonImportClickedCommand
		{
			get
			{
				if (_buttonImportClickedCommand == null)
				{
					_buttonImportClickedCommand = new ViewModelCommand(ButtonImportClicked);
				}
				return _buttonImportClickedCommand;
			}
		}

		public void ButtonImportClicked()
		{
#if false
			try
			{
				using (ImportWindowViewModel aImportWindowViewModel = new ImportWindowViewModel())
				{
					aImportWindowViewModel.Environment = Environment;

					// ゆかりすたーでエクスポートしたファイルをインポート
					aImportWindowViewModel.ImportYukaListerMode = ImportYukaListerMode;
					aImportWindowViewModel.ImportYukaListerPath = ImportYukaListerPath;
					aImportWindowViewModel.ImportTag = ImportTag;

					Messenger.Raise(new TransitionMessage(aImportWindowViewModel, "OpenImportExportWindow"));

					// IdPrefix の更新を反映
					IdPrefix = Environment!.YukaListerSettings.IdPrefix;
				}
			}
			catch (Exception oExcep)
			{
				Environment!.LogWriter.ShowLogMessage(TraceEventType.Error, "インポートボタンクリック時エラー：\n" + oExcep.Message);
				Environment.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + oExcep.StackTrace);
			}
#endif
		}
		#endregion

		#endregion

		#region エクスポートタブのコマンド

		#region 参照ボタンの制御
		private ViewModelCommand? _buttonBrowseExportYukaListerClickedCommand;

		public ViewModelCommand ButtonBrowseExportYukaListerClickedCommand
		{
			get
			{
				if (_buttonBrowseExportYukaListerClickedCommand == null)
				{
					_buttonBrowseExportYukaListerClickedCommand = new ViewModelCommand(ButtonBrowseExportYukaListerClicked);
				}
				return _buttonBrowseExportYukaListerClickedCommand;
			}
		}

		public void ButtonBrowseExportYukaListerClicked()
		{
#if false
			try
			{
				String? aPath = PathBySavingDialog("エクスポート", "ゆかりすたー情報ファイル|*" + YlConstants.FILE_EXT_YLINFO, "YukaListerInfo_" + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss"));
				if (aPath != null)
				{
					ExportYukaListerPath = aPath;
				}
			}
			catch (Exception oExcep)
			{
				Environment!.LogWriter.ShowLogMessage(TraceEventType.Error, "ログ保存時エラー：\n" + oExcep.Message);
				Environment.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + oExcep.StackTrace);
			}
#endif
		}
		#endregion

		#region エクスポートボタンの制御
		private ViewModelCommand? _buttonExportClickedCommand;

		public ViewModelCommand ButtonExportClickedCommand
		{
			get
			{
				if (_buttonExportClickedCommand == null)
				{
					_buttonExportClickedCommand = new ViewModelCommand(ButtonExportClicked);
				}
				return _buttonExportClickedCommand;
			}
		}

		public void ButtonExportClicked()
		{
#if false
			try
			{
				using (ExportWindowViewModel aExportWindowViewModel = new ExportWindowViewModel())
				{
					aExportWindowViewModel.Environment = Environment;
					aExportWindowViewModel.ExportYukaListerPath = ExportYukaListerPath;

					Messenger.Raise(new TransitionMessage(aExportWindowViewModel, "OpenImportExportWindow"));
				}
			}
			catch (Exception oExcep)
			{
				Environment!.LogWriter.ShowLogMessage(TraceEventType.Error, "エクスポートボタンクリック時エラー：\n" + oExcep.Message);
				Environment.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + oExcep.StackTrace);
			}
#endif
		}
		#endregion

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
				Title = "環境設定";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// リスト出力形式
				HtmlOutputWriter htmlOutputWriter = new();
				CompositeDisposable.Add(htmlOutputWriter);
				OutputWriters.Add(htmlOutputWriter);
				CsvOutputWriter csvOutputWriter = new();
				CompositeDisposable.Add(csvOutputWriter);
				OutputWriters.Add(csvOutputWriter);

				// プログレスバー
				ProgressBarCheckRssVisibility = Visibility.Hidden;

				SettingsToProperties();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "環境設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public void ListFolderSelected(FolderSelectionMessage folderSelectionMessage)
		{
			try
			{
				if (String.IsNullOrEmpty(folderSelectionMessage.Response))
				{
					return;
				}
				ListFolder = folderSelectionMessage.Response;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト出力先フォルダー選択時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void CheckInput()
		{
			// 設定タブ
			if (String.IsNullOrEmpty(YukariConfigPathSeed))
			{
				throw new Exception("ゆかり設定ファイルを指定して下さい。");
			}
			IdPrefix = YlCommon.CheckIdPrefix(IdPrefix, true);

			// リスト対象タブ
			if (!TargetExts.Any())
			{
				throw new Exception("リスト化対象ファイルの拡張子を指定して下さい。");
			}

			// メンテナンスタブ
			if (SyncMusicInfoDb)
			{
				if (String.IsNullOrEmpty(SyncServer) || SyncServer == "http://" || SyncServer == "https://")
				{
					throw new Exception("同期用のサーバー URL を指定して下さい。");
				}
				if (SyncServer?.Contains("http://") != true && SyncServer?.Contains("https://") != true)
				{
					throw new Exception("http:// または https:// で始まる同期用のサーバー URL を指定して下さい。");
				}
				if (String.IsNullOrEmpty(SyncAccount))
				{
					throw new Exception("同期用のアカウント名を指定して下さい。");
				}
				if (String.IsNullOrEmpty(SyncPassword))
				{
					throw new Exception("同期用のパスワードを指定して下さい。");
				}

				// 補完
				if (SyncServer[SyncServer.Length - 1] != '/')
				{
					SyncServer += "/";
				}
			}
		}

		// --------------------------------------------------------------------
		// すべての出力クラスの OutputSettings を読み込む
		// --------------------------------------------------------------------
		private void LoadOutputSettings()
		{
			foreach (OutputWriter outputWriter in OutputWriters)
			{
				outputWriter.OutputSettings.Load();
			}
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		private void PropertiesToSettings()
		{
			// 設定タブ
			YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPathSeed = YukariConfigPathSeed;
			YukaListerModel.Instance.EnvModel.YlSettings.AddFolderOnDeviceArrived = AddFolderOnDeviceArrived;
			YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview = ProvideYukariPreview;
			YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix = IdPrefix;

			// リスト対象タブ
			YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.Clear();
			YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.AddRange(TargetExts);
			YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.Sort();

			// リスト出力タブ
			YukaListerModel.Instance.EnvModel.YlSettings.ConfirmOutputYukariList = ConfirmOutputYukariList;
			YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder = ListFolder;

			// メンテナンスタブ
			YukaListerModel.Instance.EnvModel.YlSettings.CheckRss = CheckRss;
			YukaListerModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb = SyncMusicInfoDb;
			YukaListerModel.Instance.EnvModel.YlSettings.SyncServer = SyncServer;
			YukaListerModel.Instance.EnvModel.YlSettings.SyncAccount = SyncAccount;
			YukaListerModel.Instance.EnvModel.YlSettings.SyncPassword = YlCommon.Encrypt(SyncPassword);
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		private void SettingsToProperties()
		{
			// 設定タブ
			YukariConfigPathSeed = YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPathSeed;
			AddFolderOnDeviceArrived = YukaListerModel.Instance.EnvModel.YlSettings.AddFolderOnDeviceArrived;
			ProvideYukariPreview = YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview;
			IdPrefix = YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix;

			// リスト対象タブ
			foreach (String ext in YukaListerModel.Instance.EnvModel.YlSettings.TargetExts)
			{
				TargetExts.Add(ext);
			}

			// リスト出力タブ
			ConfirmOutputYukariList = YukaListerModel.Instance.EnvModel.YlSettings.ConfirmOutputYukariList;
			SelectedOutputWriter = OutputWriters[0];
			ListFolder = YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder;

			// メンテナンスタブ
			CheckRss = YukaListerModel.Instance.EnvModel.YlSettings.CheckRss;
			SyncMusicInfoDb = YukaListerModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb;
			SyncServer = YukaListerModel.Instance.EnvModel.YlSettings.SyncServer;
			SyncAccount = YukaListerModel.Instance.EnvModel.YlSettings.SyncAccount;
			SyncPassword = YlCommon.Decrypt(YukaListerModel.Instance.EnvModel.YlSettings.SyncPassword);

			// インポートタブ
			ImportYukaListerMode = true;
		}

		// --------------------------------------------------------------------
		// インポートタブのファイルドロップ
		// --------------------------------------------------------------------
		private void TabItemImportFileDrop(String[] files)
		{
			String? notHandledFiles = null;
			foreach (String file in files)
			{
				if (!File.Exists(file))
				{
					continue;
				}

				// ToDo: 未実装
				notHandledFiles += Path.GetFileName(file) + "\n";
			}
			if (!String.IsNullOrEmpty(notHandledFiles))
			{
				throw new Exception("ドロップされたファイルの種類を自動判定できませんでした。\n参照ボタンからファイルを指定して下さい。\n" + notHandledFiles);
			}
		}

		// --------------------------------------------------------------------
		// 設定タブのファイルドロップ
		// --------------------------------------------------------------------
		private void TabItemSettingsFileDrop(String[] files)
		{
			String? notHandledFiles = null;
			foreach (String file in files)
			{
				if (!File.Exists(file))
				{
					continue;
				}

				String ext = Path.GetExtension(file).ToLower();
				if (ext == Common.FILE_EXT_INI)
				{
					YukariConfigPathSeed = file;
				}
				else
				{
					notHandledFiles += Path.GetFileName(file) + "\n";
				}
			}
			if (!String.IsNullOrEmpty(notHandledFiles))
			{
				throw new Exception("ドロップされたファイルの種類を自動判定できませんでした。\n参照ボタンからファイルを指定して下さい。\n" + notHandledFiles);
			}
		}

	}
}
