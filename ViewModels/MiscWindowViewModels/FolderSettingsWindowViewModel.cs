// ============================================================================
// 
// フォルダー設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	public class FolderSettingsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public FolderSettingsWindowViewModel(String folderPath)
		{
			Debug.WriteLine("FolderSettingsWindowViewModel() construct with String");
			Debug.Assert(folderPath[^1] != '\\', "FolderSettingsWindowViewModel() folderPath ends '\\'");
			FolderPath = folderPath;
			SettingsToProperties();
			CompositeDisposable.Add(_semaphoreSlim);
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public FolderSettingsWindowViewModel()
		{
			Debug.WriteLine("FolderSettingsWindowViewModel() construct no arg");
			FolderPath = String.Empty;
			CompositeDisposable.Add(_semaphoreSlim);
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 設定対象フォルダーのパス（末尾は '\\' ではない）
		public String FolderPath { get; }

		// 設定ファイルの状態
		private FolderSettingsStatus _settingsFileStatus;
		public FolderSettingsStatus SettingsFileStatus
		{
			get => _settingsFileStatus;
			set
			{
				if (RaisePropertyChangedIfSet(ref _settingsFileStatus, value))
				{
					ButtonDeleteSettingsClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// （入力中の）ファイル命名規則
		private String? _fileNameRule;
		public String? FileNameRule
		{
			get => _fileNameRule;
			set
			{
				if (RaisePropertyChangedIfSet(ref _fileNameRule, value))
				{
					ButtonAddFileNameRuleClickedCommand.RaiseCanExecuteChanged();
					ButtonReplaceFileNameRuleClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// （入力中の）ファイル命名規則選択開始位置
		private Int32 _fileNameRuleSelectionStart;
		public Int32 FileNameRuleSelectionStart
		{
			get => _fileNameRuleSelectionStart;
			set => RaisePropertyChangedIfSet(ref _fileNameRuleSelectionStart, value);
		}

		// （入力中の）ファイル命名規則選択長さ
		private Int32 _fileNameRuleSelectionLength;
		public Int32 FileNameRuleSelectionLength
		{
			get => _fileNameRuleSelectionLength;
			set => RaisePropertyChangedIfSet(ref _fileNameRuleSelectionLength, value);
		}

		// （入力中の）ファイル命名規則へのフォーカス
		private Boolean _isFileNameRuleFocused;
		public Boolean IsFileNameRuleFocused
		{
			get => _isFileNameRuleFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isFileNameRuleFocused = value;
				RaisePropertyChanged(nameof(IsFileNameRuleFocused));
			}
		}

		// タグボタンのコンテキストメニュー
		public List<Control> ContextMenuButtonVarItems { get; set; } = new();

		// ファイル命名規則
		public ObservableCollection<String> FileNameRules { get; set; } = new();

		// 選択されているファイル命名規則
		private String? _selectedFileNameRule;
		public String? SelectedFileNameRule
		{
			get => _selectedFileNameRule;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedFileNameRule, value))
				{
					// 選択された時に入力欄に値を入れる
					FileNameRule = SelectedFileNameRule;

					ButtonReplaceFileNameRuleClickedCommand.RaiseCanExecuteChanged();
					ButtonDeleteFileNameRuleClickedCommand.RaiseCanExecuteChanged();
					ButtonUpFileNameRuleClickedCommand.RaiseCanExecuteChanged();
					ButtonDownFileNameRuleClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// フォルダー固定値項目のルール名
		public List<String> FolderNameRuleNames { get; set; } = new();

		// フォルダー固定値項目のルール名選択
		private String? _selectedFolderNameRuleName;
		public String? SelectedFolderNameRuleName
		{
			get => _selectedFolderNameRuleName;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedFolderNameRuleName, value))
				{
					UpdateFolderNameRuleProperties();
					SelectedFolderNameRuleToNameAndValue();
					ButtonAddFolderNameRuleClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// フォルダー固定値のルール値（選択式）の表示状況
		private Visibility _selectedFolderNameRuleValueVisibility;
		public Visibility SelectedFolderNameRuleValueVisibility
		{
			get => _selectedFolderNameRuleValueVisibility;
			set => RaisePropertyChangedIfSet(ref _selectedFolderNameRuleValueVisibility, value);
		}

		// フォルダー固定値のルール値（選択式）
		private List<String>? _folderNameRuleValues;
		public List<String>? FolderNameRuleValues
		{
			get => _folderNameRuleValues;
			set => RaisePropertyChangedIfSet(ref _folderNameRuleValues, value);
		}

		// 選択されているフォルダー固定値項目のルール値
		private String? _selectedFolderNameRuleValue;
		public String? SelectedFolderNameRuleValue
		{
			get => _selectedFolderNameRuleValue;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedFolderNameRuleValue, value))
				{
					ButtonAddFolderNameRuleClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// フォルダー固定値のルール値（手入力）の表示状況
		private Visibility _inputFolderNameRuleValueVisibility;
		public Visibility InputFolderNameRuleValueVisibility
		{
			get => _inputFolderNameRuleValueVisibility;
			set => RaisePropertyChangedIfSet(ref _inputFolderNameRuleValueVisibility, value);
		}

		// 手入力されているフォルダー固定値項目のルール値
		private String? _inputFolderNameRuleValue;
		public String? InputFolderNameRuleValue
		{
			get => _inputFolderNameRuleValue;
			set
			{
				if (RaisePropertyChangedIfSet(ref _inputFolderNameRuleValue, value))
				{
					ButtonAddFolderNameRuleClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// フォルダー固定値（名前＋値）
		public ObservableCollection<String> FolderNameRules { get; set; } = new();

		// 選択されているフォルダー固定値
		private String? _selectedFolderNameRule;
		public String? SelectedFolderNameRule
		{
			get => _selectedFolderNameRule;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedFolderNameRule, value))
				{
					// 選択された時に入力欄に値を入れる
					SelectedFolderNameRuleToNameAndValue();

					ButtonDeleteFolderNameRuleClickedCommand.RaiseCanExecuteChanged();
					ButtonUpFolderNameRuleClickedCommand.RaiseCanExecuteChanged();
					ButtonDownFolderNameRuleClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// プログレスバーの表示状況
		private Visibility _progressBarPreviewVisibility = Visibility.Hidden;
		public Visibility ProgressBarPreviewVisibility
		{
			get => _progressBarPreviewVisibility;
			set
			{
				if (RaisePropertyChangedIfSet(ref _progressBarPreviewVisibility, value))
				{
					ButtonPreviewClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// プレビュー結果
		public DispatcherCollection<PreviewInfo> PreviewInfos { get; set; } = new(DispatcherHelper.UIDispatcher);

		// 選択中のプレビュー結果
		private PreviewInfo? _selectedPreviewInfo;
		public PreviewInfo? SelectedPreviewInfo
		{
			get => _selectedPreviewInfo;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedPreviewInfo, value))
				{
					ButtonEditInfoClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 除外設定
		private Boolean _isExcluded;
		public Boolean IsExcluded
		{
			get => _isExcluded;
			set
			{
				if (RaisePropertyChangedIfSet(ref _isExcluded, value))
				{
					if (!String.IsNullOrEmpty(FolderPath))
					{
						// _isExcluded が除外ファイルの状態と異なる場合は変更フラグをセット
						FolderExcludeSettingsStatus folderExcludeSettingsStatus = YlCommon.DetectFolderExcludeSettingsStatus(FolderPath);
						_isDirty |= (folderExcludeSettingsStatus != FolderExcludeSettingsStatus.False) != _isExcluded;
					}

					ButtonPreviewClickedCommand.RaiseCanExecuteChanged();
					ButtonJumpClickedCommand.RaiseCanExecuteChanged();
					ButtonEditInfoClickedCommand.RaiseCanExecuteChanged();
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

		#region ファイル命名規則追加ボタンの制御
		private ViewModelCommand? _buttonAddFileNameRuleClickedCommand;

		public ViewModelCommand ButtonAddFileNameRuleClickedCommand
		{
			get
			{
				if (_buttonAddFileNameRuleClickedCommand == null)
				{
					_buttonAddFileNameRuleClickedCommand = new ViewModelCommand(ButtonAddFileNameRuleClicked, CanButtonAddFileNameRuleClicked);
				}
				return _buttonAddFileNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonAddFileNameRuleClicked()
		{
			return !String.IsNullOrEmpty(FileNameRule);
		}

		public void ButtonAddFileNameRuleClicked()
		{
			try
			{
				AddFileNameRule();
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル名命名規則追加時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ファイル命名規則置換ボタンの制御
		private ViewModelCommand? _buttonReplaceFileNameRuleClickedCommand;

		public ViewModelCommand ButtonReplaceFileNameRuleClickedCommand
		{
			get
			{
				if (_buttonReplaceFileNameRuleClickedCommand == null)
				{
					_buttonReplaceFileNameRuleClickedCommand = new ViewModelCommand(ButtonReplaceFileNameRuleClicked, CanButtonReplaceFileNameRuleClicked);
				}
				return _buttonReplaceFileNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonReplaceFileNameRuleClicked()
		{
			return !String.IsNullOrEmpty(FileNameRule) && !String.IsNullOrEmpty(SelectedFileNameRule);
		}

		public void ButtonReplaceFileNameRuleClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedFileNameRule))
				{
					throw new Exception("置換対象が選択されていません。");
				}
				CheckFileNameRule(false);

				// 置換
				Debug.Assert(FileNameRule != null, "ButtonReplaceFileNameRuleClicked() FileNameRule is null");
				FileNameRules[FileNameRules.IndexOf(SelectedFileNameRule)] = FileNameRule;
				SelectedFileNameRule = FileNameRule;
				FileNameRule = null;
				_isDirty = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル名命名規則置換時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ファイル命名規則削除ボタンの制御
		private ViewModelCommand? _buttonDeleteFileNameRuleClickedCommand;

		public ViewModelCommand ButtonDeleteFileNameRuleClickedCommand
		{
			get
			{
				if (_buttonDeleteFileNameRuleClickedCommand == null)
				{
					_buttonDeleteFileNameRuleClickedCommand = new ViewModelCommand(ButtonDeleteFileNameRuleClicked, CanButtonDeleteFileNameRuleClicked);
				}
				return _buttonDeleteFileNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonDeleteFileNameRuleClicked()
		{
			return !String.IsNullOrEmpty(SelectedFileNameRule);
		}

		public void ButtonDeleteFileNameRuleClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedFileNameRule))
				{
					throw new Exception("削除対象が選択されていません。");
				}
				FileNameRules.Remove(SelectedFileNameRule);
				_isDirty = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル名命名規則削除時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ファイル命名規則上へボタンの制御
		private ViewModelCommand? _buttonUpFileNameRuleClickedCommand;

		public ViewModelCommand ButtonUpFileNameRuleClickedCommand
		{
			get
			{
				if (_buttonUpFileNameRuleClickedCommand == null)
				{
					_buttonUpFileNameRuleClickedCommand = new ViewModelCommand(ButtonUpFileNameRuleClicked, CanButtonUpFileNameRuleClicked);
				}
				return _buttonUpFileNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonUpFileNameRuleClicked()
		{
			return !String.IsNullOrEmpty(SelectedFileNameRule) && FileNameRules.IndexOf(SelectedFileNameRule) > 0;
		}

		public void ButtonUpFileNameRuleClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedFileNameRule))
				{
					throw new Exception("移動対象が選択されていません。");
				}
				String selectedFileNameRuleBak = SelectedFileNameRule;
				Int32 index = FileNameRules.IndexOf(selectedFileNameRuleBak);
				SwapListItem(FileNameRules, index - 1, index);
				SelectedFileNameRule = selectedFileNameRuleBak;
				_isDirty = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル名命名規則順番繰り上げ時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ファイル命名規則下へボタンの制御
		private ViewModelCommand? _buttonDownFileNameRuleClickedCommand;

		public ViewModelCommand ButtonDownFileNameRuleClickedCommand
		{
			get
			{
				if (_buttonDownFileNameRuleClickedCommand == null)
				{
					_buttonDownFileNameRuleClickedCommand = new ViewModelCommand(ButtonDownFileNameRuleClicked, CanButtonDownFileNameRuleClicked);
				}
				return _buttonDownFileNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonDownFileNameRuleClicked()
		{
			return !String.IsNullOrEmpty(SelectedFileNameRule) && FileNameRules.IndexOf(SelectedFileNameRule) < FileNameRules.Count - 1;
		}

		public void ButtonDownFileNameRuleClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedFileNameRule))
				{
					throw new Exception("移動対象が選択されていません。");
				}
				String selectedFileNameRuleBak = SelectedFileNameRule;
				Int32 index = FileNameRules.IndexOf(selectedFileNameRuleBak);
				SwapListItem(FileNameRules, index + 1, index);
				SelectedFileNameRule = selectedFileNameRuleBak;
				_isDirty = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル名命名規則順番繰り下げ時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー固定値追加ボタンの制御
		private ViewModelCommand? _buttonAddFolderNameRuleClickedCommand;

		public ViewModelCommand ButtonAddFolderNameRuleClickedCommand
		{
			get
			{
				if (_buttonAddFolderNameRuleClickedCommand == null)
				{
					_buttonAddFolderNameRuleClickedCommand = new ViewModelCommand(ButtonAddFolderNameRuleClicked, CanButtonAddFolderNameRuleClicked);
				}
				return _buttonAddFolderNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonAddFolderNameRuleClicked()
		{
			if (SelectedFolderNameRuleValueVisibility == Visibility.Visible)
			{
				return !String.IsNullOrEmpty(SelectedFolderNameRuleValue);
			}
			else
			{
				return !String.IsNullOrEmpty(InputFolderNameRuleValue);
			}
		}

		public void ButtonAddFolderNameRuleClicked()
		{
			try
			{
				AddFolderNameRule();
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "固定値項目追加時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー固定値削除ボタンの制御
		private ViewModelCommand? _buttonDeleteFolderNameRuleClickedCommand;

		public ViewModelCommand ButtonDeleteFolderNameRuleClickedCommand
		{
			get
			{
				if (_buttonDeleteFolderNameRuleClickedCommand == null)
				{
					_buttonDeleteFolderNameRuleClickedCommand = new ViewModelCommand(ButtonDeleteFolderNameRuleClicked, CanButtonDeleteFolderNameRuleClicked);
				}
				return _buttonDeleteFolderNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonDeleteFolderNameRuleClicked()
		{
			return !String.IsNullOrEmpty(SelectedFolderNameRule);
		}

		public void ButtonDeleteFolderNameRuleClicked()
		{
			try
			{
				if (SelectedFolderNameRule != null)
				{
					FolderNameRules.Remove(SelectedFolderNameRule);
					_isDirty = true;
				}
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "固定値項目削除時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー固定値上へボタンの制御
		private ViewModelCommand? _buttonUpFolderNameRuleClickedCommand;

		public ViewModelCommand ButtonUpFolderNameRuleClickedCommand
		{
			get
			{
				if (_buttonUpFolderNameRuleClickedCommand == null)
				{
					_buttonUpFolderNameRuleClickedCommand = new ViewModelCommand(ButtonUpFolderNameRuleClicked, CanButtonUpFolderNameRuleClicked);
				}
				return _buttonUpFolderNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonUpFolderNameRuleClicked()
		{
			return !String.IsNullOrEmpty(SelectedFolderNameRule) && FolderNameRules.IndexOf(SelectedFolderNameRule) > 0;
		}

		public void ButtonUpFolderNameRuleClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedFolderNameRule))
				{
					throw new Exception("移動対象が選択されていません。");
				}
				String selectedFolderNameRuleBak = SelectedFolderNameRule;
				Int32 index = FolderNameRules.IndexOf(selectedFolderNameRuleBak);
				SwapListItem(FolderNameRules, index - 1, index);
				SelectedFolderNameRule = selectedFolderNameRuleBak;
				_isDirty = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー固定値順番繰り上げ時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー固定値下へボタンの制御
		private ViewModelCommand? _buttonDownFolderNameRuleClickedCommand;

		public ViewModelCommand ButtonDownFolderNameRuleClickedCommand
		{
			get
			{
				if (_buttonDownFolderNameRuleClickedCommand == null)
				{
					_buttonDownFolderNameRuleClickedCommand = new ViewModelCommand(ButtonDownFolderNameRuleClicked, CanButtonDownFolderNameRuleClicked);
				}
				return _buttonDownFolderNameRuleClickedCommand;
			}
		}

		public Boolean CanButtonDownFolderNameRuleClicked()
		{
			return !String.IsNullOrEmpty(SelectedFolderNameRule) && FolderNameRules.IndexOf(SelectedFolderNameRule) < FolderNameRules.Count - 1;
		}

		public void ButtonDownFolderNameRuleClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedFolderNameRule))
				{
					throw new Exception("移動対象が選択されていません。");
				}
				String selectedFolderNameRuleBak = SelectedFolderNameRule;
				Int32 index = FolderNameRules.IndexOf(selectedFolderNameRuleBak);
				SwapListItem(FolderNameRules, index + 1, index);
				SelectedFolderNameRule = selectedFolderNameRuleBak;
				_isDirty = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー固定値順番繰り下げ時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 解析結果確認ボタンの制御
		private ViewModelCommand? _buttonPreviewClickedCommand;

		public ViewModelCommand ButtonPreviewClickedCommand
		{
			get
			{
				if (_buttonPreviewClickedCommand == null)
				{
					_buttonPreviewClickedCommand = new ViewModelCommand(ButtonPreviewClicked, CanButtonPreviewClicked);
				}
				return _buttonPreviewClickedCommand;
			}
		}

		public Boolean CanButtonPreviewClicked()
		{
			return !IsExcluded && ProgressBarPreviewVisibility == Visibility.Hidden;
		}

		public void ButtonPreviewClicked()
		{
			try
			{
				// 保存
				SaveSettingsIfNeeded();

				// 検索（async を待機しない）
				_ = YlCommon.LaunchTaskAsync<Object?>(_semaphoreSlim, UpdatePreviewResultByWorker, null, "ファイル検索");
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ファイル検索クリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 未登録検出ボタンの制御
		private ViewModelCommand? _buttonJumpClickedCommand;

		public ViewModelCommand ButtonJumpClickedCommand
		{
			get
			{
				if (_buttonJumpClickedCommand == null)
				{
					_buttonJumpClickedCommand = new ViewModelCommand(ButtonJumpClicked, CanButtonJumpClicked);
				}
				return _buttonJumpClickedCommand;
			}
		}

		public Boolean CanButtonJumpClicked()
		{
			return !IsExcluded && PreviewInfos.Count > 0;
		}

		public void ButtonJumpClicked()
		{
			try
			{
				// async を待機しない
				_ = YlCommon.LaunchTaskAsync<Object?>(_semaphoreSlim, JumpToNextCandidateByWorker, null, "未登録検出");
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "未登録検出クリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 名称の編集ボタンの制御
		private ViewModelCommand? _buttonEditInfoClickedCommand;

		public ViewModelCommand ButtonEditInfoClickedCommand
		{
			get
			{
				if (_buttonEditInfoClickedCommand == null)
				{
					_buttonEditInfoClickedCommand = new ViewModelCommand(ButtonEditInfoClicked, CanButtonEditInfoClicked);
				}
				return _buttonEditInfoClickedCommand;
			}
		}

		public Boolean CanButtonEditInfoClicked()
		{
			return !IsExcluded && SelectedPreviewInfo != null;
		}

		public void ButtonEditInfoClicked()
		{
			try
			{
				EditInfo();
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "名称の編集ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ソートの制御
		private ListenerCommand<DataGridSortingEventArgs>? _dataGridPreviewSortingCommand;

		public ListenerCommand<DataGridSortingEventArgs> DataGridPreviewSortingCommand
		{
			get
			{
				if (_dataGridPreviewSortingCommand == null)
				{
					_dataGridPreviewSortingCommand = new ListenerCommand<DataGridSortingEventArgs>(DataGridPreviewSorting);
				}
				return _dataGridPreviewSortingCommand;
			}
		}

		public void DataGridPreviewSorting(DataGridSortingEventArgs dataGridSortingEventArgs)
		{
			try
			{
				PreviewInfo? prevSelectedPreviewInfo = SelectedPreviewInfo;

				// 並び替えの方向（昇順か降順か）を決める
				ListSortDirection newDirection;
				if (dataGridSortingEventArgs.Column.SortDirection == ListSortDirection.Ascending)
				{
					newDirection = ListSortDirection.Descending;
				}
				else
				{
					newDirection = ListSortDirection.Ascending;
				}

				// データのソート
				List<PreviewInfo> newPreviewInfos = new();
				if (newDirection == ListSortDirection.Ascending)
				{
					switch (dataGridSortingEventArgs.Column.DisplayIndex)
					{
						case 0:
							// ファイル名でのソート
							newPreviewInfos = PreviewInfos.OrderBy(x => x.FileName).ToList();
							break;
						case 1:
							// 項目と値でのソート
							newPreviewInfos = PreviewInfos.OrderBy(x => x.Items).ToList();
							break;
						case 2:
							// 更新日でのソート
							newPreviewInfos = PreviewInfos.OrderBy(x => x.LastWriteTime).ToList();
							break;
						case 3:
							// サブフォルダーでのソート
							newPreviewInfos = PreviewInfos.OrderBy(x => x.SubFolder).ToList();
							break;
						default:
							Debug.Assert(false, "DataGridPreviewSorting() bad specified target item: " + dataGridSortingEventArgs.Column.DisplayIndex.ToString());
							break;
					}
				}
				else
				{
					switch (dataGridSortingEventArgs.Column.DisplayIndex)
					{
						case 0:
							// ファイル名でのソート
							newPreviewInfos = PreviewInfos.OrderByDescending(x => x.FileName).ToList();
							break;
						case 1:
							// 項目と値でのソート
							newPreviewInfos = PreviewInfos.OrderByDescending(x => x.Items).ToList();
							break;
						case 2:
							// 更新日でのソート
							newPreviewInfos = PreviewInfos.OrderByDescending(x => x.LastWriteTime).ToList();
							break;
						case 3:
							// サブフォルダーでのソート
							newPreviewInfos = PreviewInfos.OrderByDescending(x => x.SubFolder).ToList();
							break;
						default:
							Debug.Assert(false, "DataGridPreviewSorting() bad specified target item: " + dataGridSortingEventArgs.Column.DisplayIndex.ToString());
							break;
					}
				}

				// 結果の表示
				PreviewInfos.Clear();
				foreach (PreviewInfo newPreviewInfo in newPreviewInfos)
				{
					PreviewInfos.Add(newPreviewInfo);
				}
				SelectedPreviewInfo = prevSelectedPreviewInfo;

				// 並び替えグリフの表示
				dataGridSortingEventArgs.Column.SortDirection = newDirection;

				dataGridSortingEventArgs.Handled = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DGV ヘッダークリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				EditInfo();
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 設定削除ボタンの制御
		private ViewModelCommand? _buttonDeleteSettingsClickedCommand;

		public ViewModelCommand ButtonDeleteSettingsClickedCommand
		{
			get
			{
				if (_buttonDeleteSettingsClickedCommand == null)
				{
					_buttonDeleteSettingsClickedCommand = new ViewModelCommand(ButtonDeleteSettingsClicked, CanButtonDeleteSettingsClicked);
				}
				return _buttonDeleteSettingsClickedCommand;
			}
		}

		public Boolean CanButtonDeleteSettingsClicked()
		{
			return SettingsFileStatus == FolderSettingsStatus.Set;
		}

		public void ButtonDeleteSettingsClicked()
		{
			try
			{
				if (MessageBox.Show("フォルダー設定を削除します。\nよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
				{
					return;
				}

				YlCommon.DeleteFileIfExists(FolderPath + '\\' + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG);
				YlCommon.DeleteFileIfExists(FolderPath + '\\' + YlConstants.FILE_NAME_YUKA_LISTER_EXCLUDE_CONFIG);
				YlCommon.DeleteFileIfExists(FolderPath + '\\' + YlConstants.FILE_NAME_NICO_KARA_LISTER_CONFIG);

				// UI に反映
				SettingsToProperties();
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "設定削除ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOKClickedCommand;

		public ViewModelCommand ButtonOKClickedCommand
		{
			get
			{
				if (_buttonOKClickedCommand == null)
				{
					_buttonOKClickedCommand = new ViewModelCommand(ButtonOKClicked);
				}
				return _buttonOKClickedCommand;
			}
		}

		public void ButtonOKClicked()
		{
			try
			{
				SaveSettingsIfNeeded();
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
				Debug.WriteLine("Initialize() CompositeDisposable.Count: " + CompositeDisposable.Count);

				// タイトルバー
				Title = "フォルダー設定";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
#if TEST
				Title = "［テスト］" + Title;
#endif

				// タグボタンのコンテキストメニュー
				List<String> labels = CreateRuleVarLabels();
				foreach (String label in labels)
				{
					// オンボーカル・オフボーカル・タグは除外
					if (label.IndexOf(YlConstants.RULE_VAR_ON_VOCAL, StringComparison.OrdinalIgnoreCase) < 0
							&& label.IndexOf(YlConstants.RULE_VAR_OFF_VOCAL, StringComparison.OrdinalIgnoreCase) < 0
							&& label.IndexOf(YlConstants.RULE_VAR_TAG, StringComparison.OrdinalIgnoreCase) < 0)
					{
						AddContextMenuItemToButtonVar(label);
					}
				}

				// カテゴリー一覧
				using MusicInfoContextDefault musicInfoContext = new();
				musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				_cachedCategoryNames = DbCommon.SelectCategoryNames(musicInfoContext.Categories);

				// 固定値項目（カテゴリー一覧設定後に行う）
				foreach (String label in labels)
				{
					// * は除外
					if (!label.Contains(YlConstants.RULE_VAR_ANY))
					{
						FolderNameRuleNames.Add(label);
					}
				}
				SelectedFolderNameRuleName = FolderNameRuleNames[0];

				// リスナーに通知
				//RaisePropertyChanged(nameof(FolderPath));
				RaisePropertyChanged(nameof(ContextMenuButtonVarItems));
				RaisePropertyChanged(nameof(FolderNameRuleNames));
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// カテゴリー一覧の事前読み込み
		private List<String> _cachedCategoryNames = new();

		// 設定が変更された
		private Boolean _isDirty = false;

		// フォルダー設定ウィンドウ上で時間のかかるタスクが多重起動されるのを抑止する
		private readonly SemaphoreSlim _semaphoreSlim = new(1);

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ButtonVar のコンテキストメニューにアイテムを追加
		// --------------------------------------------------------------------
		private void AddContextMenuItemToButtonVar(String label)
		{
			YlCommon.AddContextMenuItem(ContextMenuButtonVarItems, label, ContextMenuButtonVarItem_Click);
		}

		// --------------------------------------------------------------------
		// テキストボックスに入力されているファイル命名規則をリストボックスに追加
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void AddFileNameRule()
		{
			CheckFileNameRule(true);

			// 追加
			Debug.Assert(FileNameRule != null, "AddFileNameRule() ");
			FileNameRules.Add(FileNameRule);
			SelectedFileNameRule = FileNameRule;
			FileNameRule = null;
			_isDirty = true;
		}

		// --------------------------------------------------------------------
		// 選択または入力されたルールを追加
		// --------------------------------------------------------------------
		private void AddFolderNameRule()
		{
			// 追加済みのフォルダー固定値と同じ項目があれば選択する
			SelectedFolderNameRule = SelectedFolderNameRuleFromSelectedFolderNameRuleName();
			String? newRule = FolderNameRuleFromProperty();
			if (newRule == null)
			{
				return;
			}

			if (SelectedFolderNameRule == null)
			{
				// 未登録なので新規登録
				FolderNameRules.Add(newRule);
			}
			else
			{
				// 既に登録済みなので置換
				FolderNameRules[FolderNameRules.IndexOf(SelectedFolderNameRule)] = newRule;
			}
			SelectedFolderNameRule = newRule;

			SelectedFolderNameRuleValue = null;
			InputFolderNameRuleValue = null;
			_isDirty = true;
		}

		// --------------------------------------------------------------------
		// 指定フォルダーのファイル情報を解析結果に追加
		// --------------------------------------------------------------------
		private void AddPreviewInfos(String folderPath)
		{
			// 検索
			String[] filePathes = Directory.GetFiles(folderPath);

			// マッチをリストに追加
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(folderPath);
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);
			using ListContextInMemory listContextInMemory = new();
			using TFoundSetter foundSetter = new(listContextInMemory);
			Dictionary<String, String> ruleMap = YlCommon.CreateRuleDictionaryWithDescription();
			foreach (String filePath in filePathes)
			{
				if (!YlModel.Instance.EnvModel.YlSettings.TargetExts.Contains(Path.GetExtension(filePath).ToLower()))
				{
					continue;
				}

				// ファイル命名規則とフォルダー固定値を適用
				Dictionary<String, String?> dic = foundSetter.MatchFileNameRulesAndFolderRuleForSearch(Path.GetFileNameWithoutExtension(filePath), folderSettingsInMemory);

				// ファイル
				PreviewInfo previewInfo = new();
				previewInfo.FileName = Path.GetFileName(filePath);
				previewInfo.LastWriteTime = JulianDay.DateTimeToModifiedJulianDate(new FileInfo(filePath).LastWriteTime);
				if (folderPath.Length > FolderPath.Length)
				{
					previewInfo.SubFolder = folderPath[(FolderPath.Length + 1)..];
				}

				// 項目と値
				StringBuilder sb = new();
				foreach (KeyValuePair<String, String?> kvp in dic)
				{
					if (kvp.Key != YlConstants.RULE_VAR_ANY && !String.IsNullOrEmpty(kvp.Value))
					{
						sb.Append(ruleMap[kvp.Key] + "=" + kvp.Value + ", ");
					}
				}
				previewInfo.Items = sb.ToString();

				// 追加
				PreviewInfos.Add(previewInfo);
#if DEBUGz
				Thread.Sleep(100);
#endif
			}

			// 単独設定のないサブフォルダーを検索
			String[] subFolders = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
			foreach (String subFolder in subFolders)
			{
				if (YlCommon.FindSettingsFolder(subFolder) != subFolder)
				{
					AddPreviewInfos(subFolder);
				}
			}
		}

		// --------------------------------------------------------------------
		// テキストボックスに入力されているファイル命名規則が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void CheckFileNameRule(Boolean checkSelectedLine)
		{
			// 入力が空の場合はボタンは押されないはずだが念のため
			if (String.IsNullOrEmpty(FileNameRule))
			{
				throw new Exception("命名規則が入力されていません。");
			}

			// 変数が含まれているか
			if (!FileNameRule.Contains(YlConstants.RULE_VAR_BEGIN))
			{
				throw new Exception("命名規則に <変数> が含まれていません。");
			}

			// 既存のものと重複していないか
			if (IsFileNameRuleAdded())
			{
				throw new Exception("同じ命名規則が既に追加されています。");
			}

			// 変数・ワイルドカードが隣り合っているとうまく解析できない
			String normalizedNewRule = NormalizeRule(FileNameRule);
			if (normalizedNewRule.Contains(YlConstants.RULE_VAR_ANY + YlConstants.RULE_VAR_ANY))
			{
				throw new Exception("<変数> や " + YlConstants.RULE_VAR_ANY + " が連続していると正常にファイル名を解析できません。\n"
						+ "間に区切り用の文字を入れてください。");
			}

			// 競合する命名規則が無いか
			for (Int32 i = 0; i < FileNameRules.Count; i++)
			{
				if (SelectedFileNameRule == FileNameRules[i] && !checkSelectedLine)
				{
					continue;
				}

				if (FileNameRules[i] != null && NormalizeRule(FileNameRules[i]!) == normalizedNewRule)
				{
					throw new Exception("競合する命名規則が既に追加されています：\n" + FileNameRules[i]);
				}
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void ContextMenuButtonVarItem_Click(Object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				MenuItem item = (MenuItem)sender;
				String? key = FindRuleVarName((String)item.Header);
				if (String.IsNullOrEmpty(key))
				{
					return;
				}
				String wrappedVarName = WrapVarName(key);

				// カーソル位置に挿入
				Int32 aSelectionStartBak = FileNameRuleSelectionStart;
				if (String.IsNullOrEmpty(FileNameRule))
				{
					FileNameRule = wrappedVarName;
				}
				else
				{
					FileNameRule = FileNameRule[..FileNameRuleSelectionStart] + wrappedVarName
							+ FileNameRule[(FileNameRuleSelectionStart + FileNameRuleSelectionLength)..];
				}

				// タグボタンにフォーカスが移っているので戻す
				IsFileNameRuleFocused = true;

				// カーソル位置変更
				FileNameRuleSelectionStart = aSelectionStartBak + wrappedVarName.Length;
				FileNameRuleSelectionLength = 0;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "変数メニュークリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// ファイル命名規則の変数の表示用文字列を生成
		// --------------------------------------------------------------------
		private static List<String> CreateRuleVarLabels()
		{
			List<String> labels = new();
			TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
			Dictionary<String, String> varMap = YlCommon.CreateRuleDictionaryWithDescription();
			foreach (KeyValuePair<String, String> kvp in varMap)
			{
				String key;
				if (kvp.Key == YlConstants.RULE_VAR_ANY)
				{
					key = kvp.Key;
				}
				else
				{
					key = YlConstants.RULE_VAR_BEGIN + textInfo.ToTitleCase(kvp.Key) + YlConstants.RULE_VAR_END;
				}
				labels.Add(key + "（" + kvp.Value + "）");
			}
			return labels;
		}

		// --------------------------------------------------------------------
		// 名称の編集ウィンドウを開く
		// --------------------------------------------------------------------
		private void EditInfo()
		{
			if (IsExcluded || SelectedPreviewInfo == null)
			{
				return;
			}

			// ViewModel 経由で名称の編集ウィンドウを開く
			String filePath = FolderPath + "\\" + SelectedPreviewInfo.FileName;
			using EditMusicInfoWindowViewModel editMusicInfoWindowViewModel = new(filePath);
			Messenger.Raise(new TransitionMessage(editMusicInfoWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_MUSIC_INFO_WINDOW));
		}

		// --------------------------------------------------------------------
		// <Name>=Value 形式の文字列から Value を返す
		// --------------------------------------------------------------------
		private static String FindRuleValue(String str)
		{
			Int32 equalPos = str.IndexOf('=');
			return str[(equalPos + 1)..];
		}

		// --------------------------------------------------------------------
		// 文字列の中に含まれている命名規則の変数名を返す
		// 文字列の中には <Name> 形式で変数名を含んでいる必要がある
		// 返す変数名には <> は含まない
		// --------------------------------------------------------------------
		private static String? FindRuleVarName(String str)
		{
			Dictionary<String, String?> varMap = YlCommon.CreateRuleDictionary();
			foreach (String kvp in varMap.Keys)
			{
				if (str.Contains(YlConstants.RULE_VAR_BEGIN + kvp + YlConstants.RULE_VAR_END, StringComparison.CurrentCultureIgnoreCase))
				{
					return kvp;
				}
			}
			if (str.Contains(YlConstants.RULE_VAR_ANY))
			{
				return YlConstants.RULE_VAR_ANY;
			}
			return null;
		}

		// --------------------------------------------------------------------
		// フォルダー固定値一覧の中からタグの行を探す
		// --------------------------------------------------------------------
		private static Int32 FindTagRule(List<String> folderNameRules)
		{
			for (Int32 i = 0; i < folderNameRules.Count; i++)
			{
				if (FindRuleVarName(folderNameRules[i]) == YlConstants.RULE_VAR_TAG)
				{
					return i;
				}
			}

			return -1;
		}

		// --------------------------------------------------------------------
		// 選択または入力された固定値（名前＋値）
		// --------------------------------------------------------------------
		private String? FolderNameRuleFromProperty()
		{
			if (String.IsNullOrEmpty(SelectedFolderNameRuleName))
			{
				return null;
			}
			String? key = FindRuleVarName(SelectedFolderNameRuleName);
			if (String.IsNullOrEmpty(key))
			{
				return null;
			}
			return WrapVarName(key) + "=" + FolderNameRuleValueFromProperty();
		}

		// --------------------------------------------------------------------
		// 選択または入力された固定値（値）
		// --------------------------------------------------------------------
		private String? FolderNameRuleValueFromProperty()
		{
			if (SelectedFolderNameRuleValueVisibility == Visibility.Visible)
			{
				return SelectedFolderNameRuleValue;
			}
			else
			{
				return InputFolderNameRuleValue;
			}
		}

		// --------------------------------------------------------------------
		// 入力中のファイル命名規則と同じものが既に追加されているか
		// --------------------------------------------------------------------
		private Boolean IsFileNameRuleAdded()
		{
			foreach (String? aRule in FileNameRules)
			{
				if (FileNameRule == aRule)
				{
					return true;
				}
			}
			return false;
		}

		// --------------------------------------------------------------------
		// 編集する必要がありそうなファイルに飛ぶ
		// （楽曲名・タイアップ名が楽曲情報データベースに未登録なファイル）
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		private Task JumpToNextCandidateByWorker(Object? _)
		{
			Int32 rowIndex;
			if (SelectedPreviewInfo == null)
			{
				rowIndex = -1;
			}
			else
			{
				rowIndex = PreviewInfos.IndexOf(SelectedPreviewInfo);
			}

			// マッチ準備
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(FolderPath);
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);
			using ListContextInMemory listContextInMemory = new();
			using TFoundSetter foundSetter = new(listContextInMemory);
			using MusicInfoContextDefault musicInfoContext = new();
			musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

			for (; ; )
			{
				rowIndex++;
				if (rowIndex >= PreviewInfos.Count)
				{
					YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "ファイル名から取得した楽曲情報・番組情報が楽曲情報データベースに未登録のファイルは見つかりませんでした。");
					break;
				}

				// ファイル命名規則とフォルダー固定値を適用
				Dictionary<String, String?> dic = foundSetter.MatchFileNameRulesAndFolderRuleForSearch(Path.GetFileNameWithoutExtension(PreviewInfos[rowIndex].FileName), folderSettingsInMemory);

				// 楽曲名が空かどうか
				if (String.IsNullOrEmpty(dic[YlConstants.RULE_VAR_TITLE]))
				{
					break;
				}

				// 楽曲名が楽曲情報データベースと不一致かどうか
				if (!String.IsNullOrEmpty(dic[YlConstants.RULE_VAR_TITLE]))
				{
					String songNameOrigin = dic[YlConstants.RULE_VAR_TITLE]!;
					TSongAlias? songAlias = DbCommon.SelectAliasByAlias(musicInfoContext.SongAliases, dic[YlConstants.RULE_VAR_TITLE]);
					if (songAlias != null)
					{
						TSong? songOrigin = DbCommon.SelectBaseById(musicInfoContext.Songs, songAlias.OriginalId);
						songNameOrigin = songOrigin?.Name ?? String.Empty;
						if (String.IsNullOrEmpty(songNameOrigin))
						{
							break;
						}
					}
					if (DbCommon.SelectMasterByName(musicInfoContext.Songs, songNameOrigin) == null)
					{
						break;
					}
				}

				// 番組名がある場合、番組名が楽曲情報データベースと不一致かどうか
				if (!String.IsNullOrEmpty(dic[YlConstants.RULE_VAR_PROGRAM]))
				{
					String programNameOrigin = dic[YlConstants.RULE_VAR_PROGRAM]!;
					TTieUpAlias? tieUpAlias = DbCommon.SelectAliasByAlias(musicInfoContext.TieUpAliases, dic[YlConstants.RULE_VAR_PROGRAM]);
					if (tieUpAlias != null)
					{
						TTieUp? tieUpOrigin = DbCommon.SelectBaseById(musicInfoContext.TieUps, tieUpAlias.OriginalId);
						programNameOrigin = tieUpOrigin?.Name ?? String.Empty;
						if (String.IsNullOrEmpty(programNameOrigin))
						{
							break;
						}
					}
					if (DbCommon.SelectMasterByName(musicInfoContext.TieUps, programNameOrigin) == null)
					{
						break;
					}
				}
			}

			if (0 <= rowIndex && rowIndex < PreviewInfos.Count)
			{
				SelectedPreviewInfo = PreviewInfos[rowIndex];
			}
			else
			{
				SelectedPreviewInfo = null;
			}
			return Task.CompletedTask;
		}

		// --------------------------------------------------------------------
		// 命名規則の変数部分を全てワイルドカードにする
		// --------------------------------------------------------------------
		private static String NormalizeRule(String rule)
		{
			return Regex.Replace(rule, @"\<.*?\>", YlConstants.RULE_VAR_ANY);
		}

		// --------------------------------------------------------------------
		// プロパティーの値を設定に格納
		// ただしタグは除く
		// --------------------------------------------------------------------
		private FolderSettingsInDisk PropertiesToSettings()
		{
			FolderSettingsInDisk folderSettings = new();

			folderSettings.AppGeneration = YlConstants.APP_GENERATION;
			folderSettings.AppVer = YlConstants.APP_VER;

			folderSettings.FileNameRules = FileNameRules.ToList();
			folderSettings.FolderNameRules = FolderNameRules.ToList();

			// タグ除外
			Int32 tagIndex = FindTagRule(folderSettings.FolderNameRules);
			if (tagIndex >= 0)
			{
				folderSettings.FolderNameRules.RemoveAt(tagIndex);
			}

			return folderSettings;
		}

		// --------------------------------------------------------------------
		// フォルダー設定を保存
		// 原則として通常属性で保存するが、既存ファイルに隠し属性等あれば同じ属性で保存する
		// --------------------------------------------------------------------
		private void SaveFolderSettingsInDisk(FolderSettingsInDisk folderSettings)
		{
			String yukaListerConfigPath = FolderPath + "\\" + YlConstants.FILE_NAME_YUKA_LISTER_CONFIG;
			FileAttributes prevAttr = YlCommon.DeleteFileIfExists(yukaListerConfigPath);
			Common.Serialize(yukaListerConfigPath, folderSettings);
			if (prevAttr != 0)
			{
				File.SetAttributes(yukaListerConfigPath, prevAttr);
			}
		}

		// --------------------------------------------------------------------
		// 設定が更新されていれば保存
		// ＜例外＞ OperationCanceledException, Exception
		// --------------------------------------------------------------------
		private void SaveSettingsIfNeeded()
		{
			// 設定途中のファイル命名規則を確認
			if (!String.IsNullOrEmpty(FileNameRule) && !IsFileNameRuleAdded())
			{
				switch (MessageBox.Show("ファイル命名規則に入力中の\n" + FileNameRule + "\nはまだ命名規則として追加されていません。\n追加しますか？",
						"確認", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation))
				{
					case MessageBoxResult.Yes:
						AddFileNameRule();
						break;
					case MessageBoxResult.No:
						break;
					case MessageBoxResult.Cancel:
						throw new OperationCanceledException("保存を中止しました。");
				}
			}

			// 設定途中のフォルダー固定値を確認
			String? folderNameRuleFromProperty = FolderNameRuleFromProperty();
			if (!String.IsNullOrEmpty(FolderNameRuleValueFromProperty())
					&& !String.IsNullOrEmpty(folderNameRuleFromProperty) && !FolderNameRules.Contains(folderNameRuleFromProperty))
			{
				switch (MessageBox.Show("固定値項目に入力中の\n" + folderNameRuleFromProperty + "\nはまだ固定値として追加されていません。\n追加しますか？",
						"確認", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation))
				{
					case MessageBoxResult.Yes:
						AddFolderNameRule();
						break;
					case MessageBoxResult.No:
						break;
					case MessageBoxResult.Cancel:
						throw new OperationCanceledException("保存を中止しました。");
				}
			}

			if (!_isDirty)
			{
				return;
			}

			// 保存（タグ以外）
			FolderSettingsInDisk folderSettings = PropertiesToSettings();
			SaveFolderSettingsInDisk(folderSettings);

			// 保存（タグを環境設定に）
			List<String> folderNameRulesList = FolderNameRules.ToList();
			Int32 tagIndex = FindTagRule(folderNameRulesList);
			String? tagKey = YlCommon.WithoutDriveLetter(FolderPath);
			if (!String.IsNullOrEmpty(tagKey))
			{
				if (tagIndex >= 0)
				{
					// 追加
					YlModel.Instance.EnvModel.TagSettings.FolderTags[tagKey] = FindRuleValue(folderNameRulesList[tagIndex]);
				}
				else
				{
					// 削除
					YlModel.Instance.EnvModel.TagSettings.FolderTags.TryRemove(tagKey, out _);
				}
			}
			YlModel.Instance.EnvModel.TagSettings.Save();

			// 保存（除外設定）
			String yukaListerExcludeConfigPath = FolderPath + '\\' + YlConstants.FILE_NAME_YUKA_LISTER_EXCLUDE_CONFIG;
			if (IsExcluded)
			{
				if (!File.Exists(yukaListerExcludeConfigPath))
				{
					File.Create(yukaListerExcludeConfigPath);
				}
			}
			else
			{
				YlCommon.DeleteFileIfExists(yukaListerExcludeConfigPath);
			}

			// ニコカラりすたーの設定ファイルがある場合は削除
			YlCommon.DeleteFileIfExists(FolderPath + '\\' + YlConstants.FILE_NAME_NICO_KARA_LISTER_CONFIG);

			// 設定ファイルの状態
			SettingsFileStatus = YlCommon.DetectFolderSettingsStatus2Ex(FolderPath);

			_isDirty = false;
		}

		// --------------------------------------------------------------------
		// 選択されているルール名から、選択されるべきルール（名前＋値）を取得
		// --------------------------------------------------------------------
		private String? SelectedFolderNameRuleFromSelectedFolderNameRuleName()
		{
			if (String.IsNullOrEmpty(SelectedFolderNameRuleName))
			{
				return null;
			}

			String? key = FindRuleVarName(SelectedFolderNameRuleName);
			if (String.IsNullOrEmpty(key))
			{
				return null;
			}
			String varName = WrapVarName(key);
			foreach (String folderNameRule in FolderNameRules)
			{
				if (folderNameRule.IndexOf(varName) == 0)
				{
					return folderNameRule;
				}
			}

			return null;
		}

		// --------------------------------------------------------------------
		// 選択されたフォルダー固定値の内容を入力欄に反映
		// --------------------------------------------------------------------
		private void SelectedFolderNameRuleToNameAndValue()
		{
			try
			{
				String value;
				if (String.IsNullOrEmpty(SelectedFolderNameRule))
				{
					// 選択されていない場合は空欄にする
					value = String.Empty;
				}
				else
				{
					String? key = FindRuleVarName(SelectedFolderNameRule);
					if (String.IsNullOrEmpty(key))
					{
						// キーに対応する値が設定されていない場合は空欄にする
						value = String.Empty;
					}
					else
					{
						// キーに対応する値を反映する
						String varName = WrapVarName(key);
						for (Int32 i = 0; i < FolderNameRuleNames.Count; i++)
						{
							if (FolderNameRuleNames[i].IndexOf(varName) == 0)
							{
								SelectedFolderNameRuleName = FolderNameRuleNames[i];
								break;
							}
						}
						value = FindRuleValue(SelectedFolderNameRule);
					}
				}

				if (SelectedFolderNameRuleValueVisibility == Visibility.Visible)
				{
					SelectedFolderNameRuleValue = value;
				}
				else
				{
					InputFolderNameRuleValue = value;
				}
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "固定値入力反映時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// フォルダー設定を読み込み、プロパティーに反映する
		// --------------------------------------------------------------------
		private void SettingsToProperties()
		{
			try
			{
				// 設定ファイルの状態
				SettingsFileStatus = YlCommon.DetectFolderSettingsStatus2Ex(FolderPath);

				// 読み込み
				FolderSettingsInDisk settings = YlCommon.LoadFolderSettings(FolderPath);

				// 設定反映
				FileNameRules.Clear();
				foreach (String fileNameRule in settings.FileNameRules)
				{
					FileNameRules.Add(fileNameRule);
				}
				FolderNameRules.Clear();
				foreach (String folderNameRule in settings.FolderNameRules)
				{
					FolderNameRules.Add(folderNameRule);
				}

				// タグ設定
				String tagKey = YlCommon.WithoutDriveLetter(FolderPath);
				if (YlModel.Instance.EnvModel.TagSettings.FolderTags.ContainsKey(tagKey))
				{
					FolderNameRules.Add(WrapVarName(YlConstants.RULE_VAR_TAG) + "=" + YlModel.Instance.EnvModel.TagSettings.FolderTags[tagKey]);
				}

				// 除外設定
				IsExcluded = YlCommon.DetectFolderExcludeSettingsStatus(FolderPath) == FolderExcludeSettingsStatus.True;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "設定読み込み時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// リストの 2 つのアイテムを入れ替える
		// --------------------------------------------------------------------
		private static void SwapListItem<T>(IList<T> list, Int32 lhsIndex, Int32 rhsIndex)
		{
			T tmp = list[lhsIndex];
			list[lhsIndex] = list[rhsIndex];
			list[rhsIndex] = tmp;
		}

		// --------------------------------------------------------------------
		// SelectedFolderNameRuleName の状況に紐付くプロパティーを更新
		// --------------------------------------------------------------------
		private void UpdateFolderNameRuleProperties()
		{
			// 追加済みのフォルダー固定値と同じ項目があれば選択する
			SelectedFolderNameRule = SelectedFolderNameRuleFromSelectedFolderNameRuleName();

			if (String.IsNullOrEmpty(SelectedFolderNameRuleName))
			{
				return;
			}
			String? ruleName = FindRuleVarName(SelectedFolderNameRuleName);
			if (ruleName == YlConstants.RULE_VAR_CATEGORY || ruleName == YlConstants.RULE_VAR_ON_VOCAL || ruleName == YlConstants.RULE_VAR_OFF_VOCAL)
			{
				// ルール値の入力は選択式
				SelectedFolderNameRuleValueVisibility = Visibility.Visible;
				InputFolderNameRuleValueVisibility = Visibility.Collapsed;

				// 選択肢の準備
				switch (ruleName)
				{
					case YlConstants.RULE_VAR_CATEGORY:
						FolderNameRuleValues = _cachedCategoryNames;
						break;
					case YlConstants.RULE_VAR_ON_VOCAL:
					case YlConstants.RULE_VAR_OFF_VOCAL:
						List<String> onOffVocalValues = new();
						onOffVocalValues.Add(YlConstants.RULE_VALUE_VOCAL_DEFAULT.ToString());
						FolderNameRuleValues = onOffVocalValues;
						break;
					default:
						Debug.Assert(false, "UpdateFolderNameRuleComponents() bad ruleName");
						break;
				}
				SelectedFolderNameRuleValue = null;
			}
			else
			{
				// ルール値の入力は手入力
				SelectedFolderNameRuleValueVisibility = Visibility.Collapsed;
				InputFolderNameRuleValueVisibility = Visibility.Visible;
			}
		}

		// --------------------------------------------------------------------
		// 検索結果を更新
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		private Task UpdatePreviewResultByWorker(Object? _)
		{
			try
			{
				// 準備
				ProgressBarPreviewVisibility = Visibility.Visible;

				// クリア
				PreviewInfos.Clear();
				ButtonJumpClickedCommand.RaiseCanExecuteChanged();

				// 追加
				AddPreviewInfos(FolderPath);

				// 結果
				if (PreviewInfos.Count == 0)
				{
					throw new Exception("フォルダー内にリスト化対象のファイルがありませんでした。");
				}
				ButtonJumpClickedCommand.RaiseCanExecuteChanged();
			}
			finally
			{
				// 後片付け
				ProgressBarPreviewVisibility = Visibility.Hidden;
			}
			return Task.CompletedTask;
		}

		// --------------------------------------------------------------------
		// 変数名を <> で囲む
		// --------------------------------------------------------------------
		private static String WrapVarName(String varName)
		{
			if (varName == YlConstants.RULE_VAR_ANY)
			{
				return YlConstants.RULE_VAR_ANY;
			}
			else
			{
				TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
				return YlConstants.RULE_VAR_BEGIN + textInfo.ToTitleCase(varName) + YlConstants.RULE_VAR_END;
			}
		}
	}
}
