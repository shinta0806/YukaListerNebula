// ============================================================================
// 
// リスト対象タブアイテムの ViewModel
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
using System.Linq;

using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemListTargetViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemListTargetViewModel(YlSettingsWindowViewModel ylSettingsWindowViewModel)
				: base(ylSettingsWindowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public YlSettingsTabItemListTargetViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

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

		// オフボーカルと見なす単語
		public ObservableCollection<String> OffVocalWords { get; set; } = new();

		// リストで選択されているオフボーカルと見なす単語
		private String? _selectedOffVocalWord;
		public String? SelectedOffVocalWord
		{
			get => _selectedOffVocalWord;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedOffVocalWord, value))
				{
					ButtonRemoveOffVocalWordClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 追加したいオフボーカルと見なす単語
		private String? _addingOffVocalWord;
		public String? AddingOffVocalWord
		{
			get => _addingOffVocalWord;
			set
			{
				if (RaisePropertyChangedIfSet(ref _addingOffVocalWord, value))
				{
					ButtonAddOffVocalWordClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// オンボーカル・オフボーカル両方と見なす単語
		public ObservableCollection<String> BothVocalWords { get; set; } = new();

		// リストで選択されているオンボーカル・オフボーカル両方と見なす単語
		private String? _selectedBothVocalWord;
		public String? SelectedBothVocalWord
		{
			get => _selectedBothVocalWord;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedBothVocalWord, value))
				{
					ButtonRemoveBothVocalWordClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 追加したいオンボーカル・オフボーカル両方と見なす単語
		private String? _addingBothVocalWord;
		public String? AddingBothVocalWord
		{
			get => _addingBothVocalWord;
			set
			{
				if (RaisePropertyChangedIfSet(ref _addingBothVocalWord, value))
				{
					ButtonAddBothVocalWordClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region リスト化対象ファイルの拡張子追加ボタンの制御
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
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リスト化対象ファイルの拡張子追加ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region リスト化対象ファイルの拡張子削除ボタンの制御
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
				TargetExts.Remove(SelectedTargetExt);
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リスト化対象ファイルの拡張子削除ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region オフボーカルと見なす単語追加ボタンの制御
		private ViewModelCommand? _buttonAddOffVocalWordClickedCommand;

		public ViewModelCommand ButtonAddOffVocalWordClickedCommand
		{
			get
			{
				if (_buttonAddOffVocalWordClickedCommand == null)
				{
					_buttonAddOffVocalWordClickedCommand = new ViewModelCommand(ButtonAddOffVocalWordClicked, CanButtonAddOffVocalWordExtClicked);
				}
				return _buttonAddOffVocalWordClickedCommand;
			}
		}

		public Boolean CanButtonAddOffVocalWordExtClicked()
		{
			return !String.IsNullOrEmpty(AddingOffVocalWord);
		}

		public void ButtonAddOffVocalWordClicked()
		{
			try
			{
				String? word = AddingOffVocalWord;

				// 入力が空の場合はボタンは押されないはずだが念のため
				if (String.IsNullOrEmpty(word))
				{
					throw new Exception("オフボーカルと見なす単語を入力して下さい。");
				}

				// 小文字化
				word = word.ToLower();

				// 重複チェック
				if (OffVocalWords.Contains(word))
				{
					throw new Exception("既に追加されています。");
				}

				// 追加
				OffVocalWords.Add(word);
				SelectedOffVocalWord = word;
				AddingOffVocalWord = null;
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "オフボーカルと見なす単語追加ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region オフボーカルと見なす単語削除ボタンの制御
		private ViewModelCommand? _buttonRemoveOffVocalWordClickedCommand;

		public ViewModelCommand ButtonRemoveOffVocalWordClickedCommand
		{
			get
			{
				if (_buttonRemoveOffVocalWordClickedCommand == null)
				{
					_buttonRemoveOffVocalWordClickedCommand = new ViewModelCommand(ButtonRemoveOffVocalWordClicked, CanButtonRemoveOffVocalWordClicked);
				}
				return _buttonRemoveOffVocalWordClickedCommand;
			}
		}

		public Boolean CanButtonRemoveOffVocalWordClicked()
		{
			return !String.IsNullOrEmpty(SelectedOffVocalWord);
		}

		public void ButtonRemoveOffVocalWordClicked()
		{
			try
			{
				// 選択されていない場合はボタンが押されないはずだが念のため
				if (String.IsNullOrEmpty(SelectedOffVocalWord))
				{
					throw new Exception("削除したいオフボーカルと見なす単語を選択してください。");
				}

				// 削除
				OffVocalWords.Remove(SelectedOffVocalWord);
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "オフボーカルと見なす単語削除ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region オンボーカル・オフボーカル両方と見なす単語追加ボタンの制御
		private ViewModelCommand? _buttonAddBothVocalWordClickedCommand;

		public ViewModelCommand ButtonAddBothVocalWordClickedCommand
		{
			get
			{
				if (_buttonAddBothVocalWordClickedCommand == null)
				{
					_buttonAddBothVocalWordClickedCommand = new ViewModelCommand(ButtonAddBothVocalWordClicked, CanButtonAddBothVocalWordExtClicked);
				}
				return _buttonAddBothVocalWordClickedCommand;
			}
		}

		public Boolean CanButtonAddBothVocalWordExtClicked()
		{
			return !String.IsNullOrEmpty(AddingBothVocalWord);
		}

		public void ButtonAddBothVocalWordClicked()
		{
			try
			{
				String? word = AddingBothVocalWord;

				// 入力が空の場合はボタンは押されないはずだが念のため
				if (String.IsNullOrEmpty(word))
				{
					throw new Exception("オンボーカル・オフボーカル両方と見なす単語を入力して下さい。");
				}

				// 小文字化
				word = word.ToLower();

				// 重複チェック
				if (BothVocalWords.Contains(word))
				{
					throw new Exception("既に追加されています。");
				}

				// 追加
				BothVocalWords.Add(word);
				SelectedBothVocalWord = word;
				AddingBothVocalWord = null;
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "オンボーカル・オフボーカル両方と見なす単語追加ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region オンボーカル・オフボーカル両方と見なす単語削除ボタンの制御
		private ViewModelCommand? _buttonRemoveBothVocalWordClickedCommand;

		public ViewModelCommand ButtonRemoveBothVocalWordClickedCommand
		{
			get
			{
				if (_buttonRemoveBothVocalWordClickedCommand == null)
				{
					_buttonRemoveBothVocalWordClickedCommand = new ViewModelCommand(ButtonRemoveBothVocalWordClicked, CanButtonRemoveBothVocalWordClicked);
				}
				return _buttonRemoveBothVocalWordClickedCommand;
			}
		}

		public Boolean CanButtonRemoveBothVocalWordClicked()
		{
			return !String.IsNullOrEmpty(SelectedBothVocalWord);
		}

		public void ButtonRemoveBothVocalWordClicked()
		{
			try
			{
				// 選択されていない場合はボタンが押されないはずだが念のため
				if (String.IsNullOrEmpty(SelectedBothVocalWord))
				{
					throw new Exception("削除したいオンボーカル・オフボーカル両方と見なす単語を選択してください。");
				}

				// 削除
				BothVocalWords.Remove(SelectedBothVocalWord);
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "オンボーカル・オフボーカル両方と見なす単語削除ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public override void CheckProperties()
		{
			base.CheckProperties();

			if (!TargetExts.Any())
			{
				throw new Exception("リスト化対象ファイルの拡張子を指定して下さい。");
			}
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings(YlSettings destSettings)
		{
			destSettings.TargetExts.Clear();
			destSettings.TargetExts.AddRange(TargetExts);
			destSettings.TargetExts.Sort();

			// SMART_TRACK_SEPARATOR が埋め込まれている場合は分割する
			destSettings.OffVocalWords.Clear();
			destSettings.OffVocalWords.AddRange(String.Join(YlConstants.SMART_TRACK_SEPARATOR, OffVocalWords).Split(YlConstants.SMART_TRACK_SEPARATOR));
			destSettings.OffVocalWords.Sort();

			// SMART_TRACK_SEPARATOR が埋め込まれている場合は分割する
			destSettings.BothVocalWords.Clear();
			destSettings.BothVocalWords.AddRange(String.Join(YlConstants.SMART_TRACK_SEPARATOR, BothVocalWords).Split(YlConstants.SMART_TRACK_SEPARATOR));
			destSettings.BothVocalWords.Sort();
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties(YlSettings srcSettings)
		{
			TargetExts.Clear();
			foreach (String ext in srcSettings.TargetExts)
			{
				TargetExts.Add(ext);
			}
			OffVocalWords.Clear();
			foreach (String word in srcSettings.OffVocalWords)
			{
				OffVocalWords.Add(word);
			}
			BothVocalWords.Clear();
			foreach (String word in srcSettings.BothVocalWords)
			{
				BothVocalWords.Add(word);
			}
		}
	}
}
