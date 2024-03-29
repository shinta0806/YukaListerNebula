﻿// ============================================================================
// 
// リスト出力設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;

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
	internal class OutputSettingsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
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

		// 出力される項目
		public ObservableCollection<String> AddedOutputItems { get; set; } = new();

		#endregion

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 基本設定タブのコマンド

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YlModel.Instance.EnvModel.HelpClickedCommand;
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
				_logWriter?.ShowLogMessage(TraceEventType.Error, "初期化ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				Title = "出力設定：" + _outputWriter.FormatName;

				AddTabItems();
				_outputWriter.PrepareOutputSettings();
				SettingsToProperties();
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リスト出力設定ウィンドウ初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// リスト出力者
		protected OutputWriter _outputWriter;

		// ====================================================================
		// protected 関数
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
				_logWriter?.LogMessage(TraceEventType.Error, "リソースを読み込めませんでした：" + controlName);
				return;
			}
			using StreamReader reader = new(stream);
			using XmlReader xml = XmlReader.Create(reader.BaseStream);
			FrameworkElement? element = XamlReader.Load(xml) as FrameworkElement;
			if (element == null)
			{
				_logWriter?.LogMessage(TraceEventType.Error, "リソースからコントロールを生成できませんでした：" + controlName);
			}
			TabItem tabItem = new()
			{
				Header = caption,
				Content = element,
			};
			TabItems.Add(tabItem);
		}

		// --------------------------------------------------------------------
		// タブアイテムにタブを追加
		// --------------------------------------------------------------------
		protected virtual void AddTabItems()
		{
			AddTabItem("OutputSettingsTabItemBasic", "基本設定");
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		protected override void PropertiesToSettings()
		{
			base.PropertiesToSettings();

			// 出力項目のタイプ
			_outputWriter.OutputSettings.OutputAllItems = OutputAllItems;

			// 出力項目のリスト
			_outputWriter.OutputSettings.SelectedOutputItems.Clear();
			for (Int32 i = 0; i < AddedOutputItems.Count; i++)
			{
				Int32 item = Array.IndexOf(YlConstants.OUTPUT_ITEM_NAMES, (String)AddedOutputItems[i]);
				if (item < 0)
				{
					continue;
				}
				_outputWriter.OutputSettings.SelectedOutputItems.Add((OutputItems)item);
			}
		}

		// --------------------------------------------------------------------
		// 設定を保存
		// --------------------------------------------------------------------
		protected override void SaveSettings()
		{
			_outputWriter.OutputSettings.Save();
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void SettingsToProperties()
		{
			base.SettingsToProperties();

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
