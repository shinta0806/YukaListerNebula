// ============================================================================
// 
// 楽曲情報一括操作タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;

using Shinta;

using System;
using System.Diagnostics;
using System.Windows;

using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.ImportExportWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemMusicInfoBatchViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemMusicInfoBatchViewModel(YlViewModel windowViewModel)
				: base(windowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemMusicInfoBatchViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// インポート元（ゆかりすたーでエクスポートしたファイル）のパス
		private String? _importYukaListerPath = YlConstants.FOLDER_NAME_DOCUMENTS + YlConstants.FOLDER_NAME_SAMPLE_FOLDER_SETTINGS + YlConstants.FOLDER_NAME_SAMPLE_IMPORT
				+ YlConstants.FILE_NAME_YUKA_LISTER_INFO_SAMPLE;
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

		// 同名の情報も極力インポートする
		private Boolean _importSameName;
		public Boolean ImportSameName
		{
			get => _importSameName;
			set => RaisePropertyChangedIfSet(ref _importSameName, value);
		}

		// エクスポート先（ゆかりすたー情報ファイル）のパス
		private String? _exportYukaListerPath;
		public String? ExportYukaListerPath
		{
			get => _exportYukaListerPath;
			set => RaisePropertyChangedIfSet(ref _exportYukaListerPath, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region プレビュードラッグの制御（インポート元ファイル）
		private ListenerCommand<String[]>? _textBoxImportYukaListerPathPreviewDragCommand;

		public ListenerCommand<String[]> TextBoxImportYukaListerPathPreviewDragCommand
		{
			get
			{
				if (_textBoxImportYukaListerPathPreviewDragCommand == null)
				{
					_textBoxImportYukaListerPathPreviewDragCommand = new ListenerCommand<String[]>(TextBoxImportYukaListerPathPreviewDrag);
				}
				return _textBoxImportYukaListerPathPreviewDragCommand;
			}
		}

		public static void TextBoxImportYukaListerPathPreviewDrag(String[] files)
		{
			// FileDropAttachedBehavior がドラッグを許可するよう、本コマンドが存在するが、処理は行わない
		}
		#endregion

		#region インポート参照ボタンの制御
		private ViewModelCommand? _buttonBrowseImportYukaListerClickedCommand;

		public ViewModelCommand ButtonBrowseImportYukaListerClickedCommand
		{
			get
			{
				if (_buttonBrowseImportYukaListerClickedCommand == null)
				{
					_buttonBrowseImportYukaListerClickedCommand = new ViewModelCommand(ButtonBrowseImportYukaListerClicked);
				}
				return _buttonBrowseImportYukaListerClickedCommand;
			}
		}

		public void ButtonBrowseImportYukaListerClicked()
		{
			try
			{
				String? path = _windowViewModel.PathByOpeningDialog("インポート", "インポート可能ファイル|*" + YlConstants.FILE_EXT_YL_EXPORT_ARCHIVE + ";" + YlConstants.FILE_NAME_MUSIC_INFO_DATABASE
						+ "|" + YlConstants.DIALOG_FILTER_YL_EXPORT_ARCHIVE + "|楽曲情報データベース|" + YlConstants.FILE_NAME_MUSIC_INFO_DATABASE, null);
				if (path != null)
				{
					ImportYukaListerPath = path;
				}
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "インポート元参照ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
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
			try
			{
				if (String.IsNullOrEmpty(ImportYukaListerPath))
				{
					throw new Exception("インポート元ファイルを指定して下さい。");
				}

				// ViewModel 経由でインポート・エクスポートウィンドウを開く
				using ImportWindowViewModel importWindowViewModel = new(ImportYukaListerPath, ImportTag, ImportSameName);
				_windowViewModel.Messenger.Raise(new TransitionMessage(importWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_IMPORT_EXPORT_WINDOW));
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "インポートボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region エクスポート参照ボタンの制御
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
			try
			{
				String? path = _windowViewModel.PathBySavingDialog("エクスポート", YlConstants.DIALOG_FILTER_YL_EXPORT_ARCHIVE, "YukaListerInfo_" + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss"));
				if (path != null)
				{
					ExportYukaListerPath = path;
				}
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "エクスポート先参照ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
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
			try
			{
				if (String.IsNullOrEmpty(ExportYukaListerPath))
				{
					throw new Exception("エクスポート先ファイルを指定してください。");
				}

				// ViewModel 経由でインポート・エクスポートウィンドウを開く
				using ExportWindowViewModel exportWindowViewModel = new(ExportYukaListerPath);
				_windowViewModel.Messenger.Raise(new TransitionMessage(exportWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_IMPORT_EXPORT_WINDOW));
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "エクスポートボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region すべて削除ボタンの制御

		private ViewModelCommand? _buttonDeleteAllClickedCommand;

		public ViewModelCommand ButtonDeleteAllClickedCommand
		{
			get
			{
				if (_buttonDeleteAllClickedCommand == null)
				{
					_buttonDeleteAllClickedCommand = new ViewModelCommand(ButtonDeleteAllClicked);
				}
				return _buttonDeleteAllClickedCommand;
			}
		}

		public static void ButtonDeleteAllClicked()
		{
			try
			{
				if (YlModel.Instance.EnvModel.Syclin.IsActive)
				{
					throw new Exception("現在、同期処理を実行中のため、削除できません。\n同期処理が終了してから削除してください。");
				}

				if (MessageBox.Show("楽曲情報データベースをすべて削除します。\n復活できません。事前にエクスポートすることをお薦めします。\nすべて削除してよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					return;
				}

				using MusicInfoContextDefault musicInfoContextDefault = new();
				musicInfoContextDefault.CreateDatabase();
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "楽曲情報データベースを削除しました。");
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "すべて削除ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー：ファイルやフォルダーがドロップされた
		// --------------------------------------------------------------------
		public override void PathDropped(String[] pathes)
		{
			ImportYukaListerPath = DroppedFile(pathes, new String[] { YlConstants.FILE_EXT_YL_EXPORT_ARCHIVE });
		}
	}
}
