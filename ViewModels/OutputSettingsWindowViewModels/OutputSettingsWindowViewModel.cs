// ============================================================================
// 
// リスト出力設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;

using YukaLister.Models.OutputWriters;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.OutputSettingsWindowViewModels
{
	public class OutputSettingsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public OutputSettingsWindowViewModel(OutputWriter outputWriter)
		{
			_outputWriter = outputWriter;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public OutputSettingsWindowViewModel()
		{
			_outputWriter = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		#region ウィンドウのプロパティー

		// タブアイテム
		public ObservableCollection<TabItem> TabItems { get; set; } = new();

		// 選択タブ
		private Int32 _selectedTabIndex;
		public Int32 SelectedTabIndex
		{
			get => _selectedTabIndex;
			set => RaisePropertyChangedIfSet(ref _selectedTabIndex, value);
		}

		// タブコントロールの高さ
		private Double _actualTabControlHeight;
		public Double ActualTabControlHeight
		{
			get => _actualTabControlHeight;
			set
			{
				if (RaisePropertyChangedIfSet(ref _actualTabControlHeight, value))
				{
					MinTabControlHeight = _actualTabControlHeight;
				}
			}
		}

		// タブコントロールの最小高さ
		private Double _minTabControlHeight;
		public Double MinTabControlHeight
		{
			get => _minTabControlHeight;
			set => RaisePropertyChangedIfSet(ref _minTabControlHeight, value);
		}

		// タブコントロールの幅
		private Double _actualTabControlWidth;
		public Double ActualTabControlWidth
		{
			get => _actualTabControlWidth;
			set
			{
				if (RaisePropertyChangedIfSet(ref _actualTabControlWidth, value))
				{
					MinTabControlWidth = _actualTabControlWidth;
				}
			}
		}

		// タブコントロールの最小幅
		private Double _minTabControlWidth;
		public Double MinTabControlWidth
		{
			get => _minTabControlWidth;
			set => RaisePropertyChangedIfSet(ref _minTabControlWidth, value);
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

		#endregion

		#region 基本設定タブのプロパティー

		// 出力項目のタイプ
		private Boolean _outputAllItems;
		public Boolean OutputAllItems
		{
			get => _outputAllItems;
			set => RaisePropertyChangedIfSet(ref _outputAllItems, value);
		}

		// 出力項目のタイプの逆
		// 動的 XAML 読み込みで BooleanInvertConverter が使えないために必要となる
		private Boolean _outputAllItemsInvert;
		public Boolean OutputAllItemsInvert
		{
			get => _outputAllItemsInvert;
			set => RaisePropertyChangedIfSet(ref _outputAllItemsInvert, value);
		}

		// 出力されない項目
		public ObservableCollection<String> RemovedOutputItems { get; set; } = new();

		// 選択されている出力されない項目
		private String? _selectedRemovedOutputItem;
		public String? SelectedRemovedOutputItem
		{
			get => _selectedRemovedOutputItem;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedRemovedOutputItem, value))
				{
					ButtonAddOutputItemClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 出力される項目
		public ObservableCollection<String> AddedOutputItems { get; set; } = new();

		// 選択されている出力される項目
		private String? _selectedAddedOutputItem;
		public String? SelectedAddedOutputItem
		{
			get => _selectedAddedOutputItem;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedAddedOutputItem, value))
				{
					ButtonRemoveOutputItemClickedCommand.RaiseCanExecuteChanged();
					ButtonUpOutputItemClickedCommand.RaiseCanExecuteChanged();
					ButtonDownOutputItemClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		#endregion

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 基本設定タブのコマンド

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

		#region 出力項目追加ボタンの制御
		private ViewModelCommand? _buttonAddOutputItemClickedCommand;

		public ViewModelCommand ButtonAddOutputItemClickedCommand
		{
			get
			{
				if (_buttonAddOutputItemClickedCommand == null)
				{
					_buttonAddOutputItemClickedCommand = new ViewModelCommand(ButtonAddOutputItemClicked, CanButtonAddOutputItemClicked);
				}
				return _buttonAddOutputItemClickedCommand;
			}
		}

		public Boolean CanButtonAddOutputItemClicked()
		{
			return SelectedRemovedOutputItem != null;
		}

		public void ButtonAddOutputItemClicked()
		{
			try
			{
				if (SelectedRemovedOutputItem == null)
				{
					return;
				}

				AddedOutputItems.Add(SelectedRemovedOutputItem);
				SelectedAddedOutputItem = SelectedRemovedOutputItem;
				RemovedOutputItems.Remove(SelectedRemovedOutputItem);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "出力項目追加ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 出力項目削除ボタンの制御
		private ViewModelCommand? _buttonRemoveOutputItemClickedCommand;

		public ViewModelCommand ButtonRemoveOutputItemClickedCommand
		{
			get
			{
				if (_buttonRemoveOutputItemClickedCommand == null)
				{
					_buttonRemoveOutputItemClickedCommand = new ViewModelCommand(ButtonRemoveOutputItemClicked, CanButtonRemoveOutputItemClicked);
				}
				return _buttonRemoveOutputItemClickedCommand;
			}
		}

		public Boolean CanButtonRemoveOutputItemClicked()
		{
			return SelectedAddedOutputItem != null;
		}

		public void ButtonRemoveOutputItemClicked()
		{
			try
			{
				if (SelectedAddedOutputItem == null)
				{
					return;
				}

				RemovedOutputItems.Add(SelectedAddedOutputItem);
				SelectedRemovedOutputItem = SelectedAddedOutputItem;
				AddedOutputItems.Remove(SelectedAddedOutputItem);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "出力項目削除ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 出力項目上へボタンの制御
		private ViewModelCommand? _buttonUpOutputItemClickedCommand;

		public ViewModelCommand ButtonUpOutputItemClickedCommand
		{
			get
			{
				if (_buttonUpOutputItemClickedCommand == null)
				{
					_buttonUpOutputItemClickedCommand = new ViewModelCommand(ButtonUpOutputItemClicked, CanButtonUpOutputItemClicked);
				}
				return _buttonUpOutputItemClickedCommand;
			}
		}

		public Boolean CanButtonUpOutputItemClicked()
		{
			if (String.IsNullOrEmpty(SelectedAddedOutputItem))
			{
				return false;
			}
			Int32 index = AddedOutputItems.IndexOf(SelectedAddedOutputItem);
			return index >= 1;
		}

		public void ButtonUpOutputItemClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedAddedOutputItem))
				{
					return;
				}
				Int32 selectedIndex = AddedOutputItems.IndexOf(SelectedAddedOutputItem!);
				if (selectedIndex < 1)
				{
					return;
				}
				String aItem = SelectedAddedOutputItem!;
				AddedOutputItems.Remove(aItem);
				AddedOutputItems.Insert(selectedIndex - 1, aItem);
				SelectedAddedOutputItem = aItem;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "出力項目上へボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 出力項目下へボタンの制御
		private ViewModelCommand? _buttonDownOutputItemClickedCommand;

		public ViewModelCommand ButtonDownOutputItemClickedCommand
		{
			get
			{
				if (_buttonDownOutputItemClickedCommand == null)
				{
					_buttonDownOutputItemClickedCommand = new ViewModelCommand(ButtonDownOutputItemClicked, CanButtonDownOutputItemClicked);
				}
				return _buttonDownOutputItemClickedCommand;
			}
		}

		public Boolean CanButtonDownOutputItemClicked()
		{
			if (String.IsNullOrEmpty(SelectedAddedOutputItem))
			{
				return false;
			}
			Int32 index = AddedOutputItems.IndexOf(SelectedAddedOutputItem);
			return 0 <= index && index < AddedOutputItems.Count - 1;
		}

		public void ButtonDownOutputItemClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SelectedAddedOutputItem))
				{
					return;
				}
				Int32 selectedIndex = AddedOutputItems.IndexOf(SelectedAddedOutputItem);
				if (selectedIndex < 0 || selectedIndex >= AddedOutputItems.Count - 1)
				{
					return;
				}
				String item = SelectedAddedOutputItem!;
				AddedOutputItems.Remove(item);
				AddedOutputItems.Insert(selectedIndex + 1, item);
				SelectedAddedOutputItem = item;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "出力項目下へボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#endregion

		#region 初期化ボタンの制御

		private ViewModelCommand? _buttonDefaultClickedCommand;

		public ViewModelCommand ButtonDefaultClickedCommand
		{
			get
			{
				if (_buttonDefaultClickedCommand == null)
				{
					_buttonDefaultClickedCommand = new ViewModelCommand(ButtonDefaultClicked);
				}
				return _buttonDefaultClickedCommand;
			}
		}

		public void ButtonDefaultClicked()
		{
			try
			{
				if (MessageBox.Show("出力設定をすべて初期設定に戻します。\nよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
				{
					return;
				}

				// 初期値で生成
				_outputWriter.GenerateOutputSettings();
				SettingsToProperties();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "初期化ボタンクリック時エラー：\n" + excep.Message);
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
				// Enter キーでボタンが押された場合はテキストボックスからフォーカスが移らずプロパティーが更新されないため強制フォーカス
				IsButtonOkFocused = true;

				CheckInput();
				PropertiesToSettings();
				_outputWriter.OutputSettings.Save();
				IsOk = true;
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (OperationCanceledException)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "設定変更を中止しました。");
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
				Title = "出力設定：" + _outputWriter.FormatName;
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				AddTabItems();
				_outputWriter.PrepareOutputSettings();
				SettingsToProperties();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト出力設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// リスト出力者
		protected OutputWriter _outputWriter;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソースからユーザーコントロールを読み込んでタブとして追加
		// --------------------------------------------------------------------
		protected void AddTabItem(String controlName, String caption)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Stream? stream = assembly.GetManifestResourceStream("YukaLister.Views.OutputSettingsControls." + controlName + Common.FILE_EXT_XAML);
			if (stream == null)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "リソースを読み込めませんでした：" + controlName);
				return;
			}
			using StreamReader reader = new(stream);
			{
				XmlReader xml = XmlReader.Create(reader.BaseStream);
				FrameworkElement? element = XamlReader.Load(xml) as FrameworkElement;
				if (element == null)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "リソースからコントロールを生成できませんでした：" + controlName);
				}
				TabItem tabItem = new()
				{
					Header = caption,
					Content = element,
				};
				TabItems.Add(tabItem);
			}
		}

		// --------------------------------------------------------------------
		// タブアイテムにタブを追加
		// --------------------------------------------------------------------
		protected virtual void AddTabItems()
		{
			AddTabItem("OutputSettingsTabItemBasic", "基本設定");
		}

		// --------------------------------------------------------------------
		// 設定画面に入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected virtual void CheckInput()
		{
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		protected virtual void PropertiesToSettings()
		{
			// 出力項目のタイプ
			_outputWriter.OutputSettings.OutputAllItems = OutputAllItems;

			// 出力項目のリスト
			_outputWriter.OutputSettings.SelectedOutputItems?.Clear();
			for (Int32 i = 0; i < AddedOutputItems.Count; i++)
			{
				Int32 item = Array.IndexOf(YlConstants.OUTPUT_ITEM_NAMES, (String)AddedOutputItems[i]);
				if (item < 0)
				{
					continue;
				}
				_outputWriter.OutputSettings.SelectedOutputItems?.Add((OutputItems)item);
			}
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		protected virtual void SettingsToProperties()
		{
			// 出力項目のタイプ
			OutputAllItems = _outputWriter.OutputSettings.OutputAllItems;
			OutputAllItemsInvert = !OutputAllItems;

			// 出力されない項目
			OutputItems[] outputItems = (OutputItems[])Enum.GetValues(typeof(OutputItems));
			RemovedOutputItems.Clear();
			for (Int32 i = 0; i < outputItems.Length - 1; i++)
			{
				if (!_outputWriter.OutputSettings.SelectedOutputItems.Contains(outputItems[i]))
				{
					RemovedOutputItems.Add(YlConstants.OUTPUT_ITEM_NAMES[(Int32)outputItems[i]]);
				}
			}

			// 出力される項目
			AddedOutputItems.Clear();
			for (Int32 i = 0; i < _outputWriter.OutputSettings.SelectedOutputItems.Count; i++)
			{
				AddedOutputItems.Add(YlConstants.OUTPUT_ITEM_NAMES[(Int32)_outputWriter.OutputSettings.SelectedOutputItems[i]]);
			}
		}
	}
}
