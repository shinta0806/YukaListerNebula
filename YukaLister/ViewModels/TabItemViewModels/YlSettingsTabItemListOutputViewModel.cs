﻿// ============================================================================
// 
// リスト出力タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;

using Shinta;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.OutputWriters;
using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;
using YukaLister.ViewModels.OutputSettingsWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemListOutputViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemListOutputViewModel(YlSettingsWindowViewModel ylSettingsWindowViewModel)
				: base(ylSettingsWindowViewModel)
		{
			CompositeDisposable.Add(_semaphoreSlim);
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public YlSettingsTabItemListOutputViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 警告表示
		private Visibility _yukariListWarningVisibility = Visibility.Collapsed;
		public Visibility YukariListWarningVisibility
		{
			get => _yukariListWarningVisibility;
			set => RaisePropertyChangedIfSet(ref _yukariListWarningVisibility, value);
		}

		// ゆかり用リスト出力先フォルダー
		private String? _yukariListFolder;
		public String? YukariListFolder
		{
			get => _yukariListFolder;
			set => RaisePropertyChangedIfSet(ref _yukariListFolder, value);
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

		// プログレスバー表示
		private Visibility _progressBarOutputListVisibility;
		public Visibility ProgressBarOutputListVisibility
		{
			get => _progressBarOutputListVisibility;
			set
			{
				if (RaisePropertyChangedIfSet(ref _progressBarOutputListVisibility, value))
				{
					ButtonOutputListClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ゆかり用リスト出力設定ボタンの制御
		private ViewModelCommand? _buttonYukariListSettingsClickedCommand;

		public ViewModelCommand ButtonYukariListSettingsClickedCommand
		{
			get
			{
				if (_buttonYukariListSettingsClickedCommand == null)
				{
					_buttonYukariListSettingsClickedCommand = new ViewModelCommand(ButtonYukariListSettingsClicked, CanButtonYukariListSettingsClicked);
				}
				return _buttonYukariListSettingsClickedCommand;
			}
		}

		public static Boolean CanButtonYukariListSettingsClicked()
		{
			return YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error;
		}

		public void ButtonYukariListSettingsClicked()
		{
			try
			{
				YukariOutputWriter yukariOutputWriter = new();
				yukariOutputWriter.OutputSettings.Load();

				// ViewModel 経由でリスト出力設定ウィンドウを開く
				using OutputSettingsWindowViewModel outputSettingsWindowViewModel = yukariOutputWriter.CreateOutputSettingsWindowViewModel();
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(outputSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_OUTPUT_SETTINGS_WINDOW));

				if (outputSettingsWindowViewModel.Result != MessageBoxResult.OK)
				{
					return;
				}

				// 設定変更をすべての出力者に反映
				LoadOutputSettings();
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "ゆかりリクエスト用リスト出力設定ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(outputSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_OUTPUT_SETTINGS_WINDOW));

				if (outputSettingsWindowViewModel.Result != MessageBoxResult.OK)
				{
					return;
				}

				// 設定変更をすべての出力者に反映
				LoadOutputSettings();
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "閲覧用出力設定ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region プレビュードラッグの制御（リスト出力先フォルダー）
		private ListenerCommand<String[]>? _textBoxListFolderPreviewDragCommand;

		public ListenerCommand<String[]> TextBoxListFolderPreviewDragCommand
		{
			get
			{
				if (_textBoxListFolderPreviewDragCommand == null)
				{
					_textBoxListFolderPreviewDragCommand = new ListenerCommand<String[]>(TextBoxListFolderPreviewDrag);
				}
				return _textBoxListFolderPreviewDragCommand;
			}
		}

		public static void TextBoxListFolderPreviewDrag(String[] files)
		{
			// FileDropAttachedBehavior がドラッグを許可するよう、本コマンドが存在するが、処理は行わない
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
					_buttonOutputListClickedCommand = new ViewModelCommand(ButtonOutputListClicked, CanButtonOutputListClicked);
				}
				return _buttonOutputListClickedCommand;
			}
		}

		public Boolean CanButtonOutputListClicked()
		{
			return ProgressBarOutputListVisibility != Visibility.Visible;
		}

		public async void ButtonOutputListClicked()
		{
			try
			{
				// 確認
				if (String.IsNullOrEmpty(ListFolder))
				{
					ListFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" + YlConstants.FOLDER_NAME_LIST_OUTPUT;
				}

				// 出力クラスに出力先が渡るようにする
				// ウィンドウのキャンセルボタンが押された場合でも確定していることになる
				YlModel.Instance.EnvModel.YlSettings.ListOutputFolder = ListFolder;

				if (SelectedOutputWriter == null)
				{
					throw new Exception("出力形式を選択してください。");
				}

				if (YlModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Running)
				{
					if (MessageBox.Show("データ更新中のため、今すぐリスト出力しても完全なリストにはなりません。\n今すぐリスト出力しますか？",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						return;
					}
				}

				// 出力
				ProgressBarOutputListVisibility = Visibility.Visible;
				await YlCommon.LaunchTaskAsync(_semaphoreSlim, OutputListByWorker, SelectedOutputWriter, "閲覧用リスト出力");
				_logWriter?.ShowLogMessage(TraceEventType.Information, "リスト出力が完了しました。");

				// 表示
				String outputFilePath = YlModel.Instance.EnvModel.YlSettings.ListOutputFolder + SelectedOutputWriter.TopFileName;
				try
				{
					Common.ShellExecute(outputFilePath);
				}
				catch (Exception)
				{
					throw new Exception("出力先ファイルを開けませんでした。\n" + outputFilePath);
				}
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リスト出力ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				ProgressBarOutputListVisibility = Visibility.Hidden;
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

			// 警告表示
			if (YlModel.Instance.EnvModel.YukaListerWholeStatus == YukaListerStatus.Error)
			{
				YukariListWarningVisibility = Visibility.Visible;
			}

			// リスト出力形式
			HtmlOutputWriter htmlOutputWriter = new();
			CompositeDisposable.Add(htmlOutputWriter);
			OutputWriters.Add(htmlOutputWriter);
			CsvOutputWriter csvOutputWriter = new();
			CompositeDisposable.Add(csvOutputWriter);
			OutputWriters.Add(csvOutputWriter);

			// プログレスバー
			ProgressBarOutputListVisibility = Visibility.Hidden;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public void ListFolderSelected(FolderSelectionMessage folderSelectionMessage)
		{
			try
			{
				if (folderSelectionMessage.Response == null)
				{
					return;
				}
				ListFolder = folderSelectionMessage.Response[0];
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リスト出力先フォルダー選択時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：ファイルやフォルダーがドロップされた
		// --------------------------------------------------------------------
		public override void PathDropped(String[] pathes)
		{
			ListFolder = DroppedFolder(pathes);
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings(YlSettings destSettings)
		{
			destSettings.ListOutputFolder = ListFolder;
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties(YlSettings srcSettings)
		{
			// リスト出力タブ
			SelectedOutputWriter = OutputWriters[0];
			ListFolder = srcSettings.ListOutputFolder;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public void YukariConfigPathSeedChanged(String newSeed)
		{
			try
			{
				// ゆかり用リスト出力先フォルダーの算出
				YlSettings tempYlSettings = new();
				tempYlSettings.YukariConfigPathSeed2 = newSeed;
				YukariListFolder = Path.GetDirectoryName(DbCommon.ListDatabasePath(tempYlSettings));
			}
			catch (Exception)
			{
				// エラーは無視する
			}
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// タスクが多重起動されるのを抑止する
		private readonly SemaphoreSlim _semaphoreSlim = new(1);

		// ====================================================================
		// private 関数
		// ====================================================================

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
		// リスト出力処理
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		private Task OutputListByWorker(OutputWriter outputWriter)
		{
			outputWriter.Output();
			return Task.CompletedTask;
		}
	}
}
