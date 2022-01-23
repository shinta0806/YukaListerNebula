// ============================================================================
// 
// キーワード検索ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	internal class FindKeywordWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public FindKeywordWindowViewModel(ViewTFoundsWindowViewModel viewTFoundsWindowViewModel)
		{
			_viewTFoundsWindowViewModel = viewTFoundsWindowViewModel;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public FindKeywordWindowViewModel()
		{
			_viewTFoundsWindowViewModel = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// アクティブ
		private Boolean _isActive;
		public Boolean IsActive
		{
			get => _isActive;
			set => RaisePropertyChangedIfSet(ref _isActive, value);
		}

		// キーワード
		private String? keyword;
		public String? Keyword
		{
			get => keyword;
			set
			{
				if (RaisePropertyChangedIfSet(ref keyword, value))
				{
					ButtonFindClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// キーワードフォーカス
		private Boolean _isKeywordFocused;
		public Boolean IsKeywordFocused
		{
			get => _isKeywordFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isKeywordFocused = value;
				RaisePropertyChanged(nameof(IsKeywordFocused));
			}
		}

		// キーワード選択
		private String? _selectedKeyword;
		public String? SelectedKeyword
		{
			get => _selectedKeyword;
			set => RaisePropertyChangedIfSet(ref _selectedKeyword, value);
		}

		// 大文字小文字の区別
		private Boolean _caseSensitive;
		public Boolean CaseSensitive
		{
			get => _caseSensitive;
			set => RaisePropertyChangedIfSet(ref _caseSensitive, value);
		}

		// 全体一致
		private Boolean _wholeMatch;
		public Boolean WholeMatch
		{
			get => _wholeMatch;
			set => RaisePropertyChangedIfSet(ref _wholeMatch, value);
		}


		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// 検索方向
		public Int32 Direction { get; set; }

		// ウィンドウが閉じられた
		public Boolean IsClosed { get; set; }

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 次を検索・前を検索ボタンの制御
		private ListenerCommand<String>? _buttonFindClickedCommand;

		public ListenerCommand<String> ButtonFindClickedCommand
		{
			get
			{
				if (_buttonFindClickedCommand == null)
				{
					_buttonFindClickedCommand = new ListenerCommand<String>(ButtonFindClicked, CanButtonFindClicked);
				}
				return _buttonFindClickedCommand;
			}
		}

		public Boolean CanButtonFindClicked()
		{
			return !String.IsNullOrEmpty(Keyword);
		}

		public void ButtonFindClicked(String parameter)
		{
			try
			{
				Direction = parameter == YlConstants.FIND_DIRECTION_BACKWARD ? -1 : 1;
				_viewTFoundsWindowViewModel.Messenger.Raise(new InteractionMessage(YlConstants.MESSAGE_KEY_FIND_KEYWORD));
				IsActive = true;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "検索ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 閉じるボタンの制御
		private ViewModelCommand? _buttonCancelClickedCommand;

		public ViewModelCommand ButtonCancelClickedCommand
		{
			get
			{
				if (_buttonCancelClickedCommand == null)
				{
					_buttonCancelClickedCommand = new ViewModelCommand(ButtonCancelClicked);
				}
				return _buttonCancelClickedCommand;
			}
		}

		public void ButtonCancelClicked()
		{
			try
			{
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "閉じるボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウをアクティブ化する
		// --------------------------------------------------------------------
		public void Activate()
		{
			try
			{
				IsActive = true;
				IsKeywordFocused = true;

				// キーワード全選択
				String? bak = Keyword;
				Keyword = null;
				SelectedKeyword = bak;
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "アクティブ化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 設定されるべきプロパティーをコピー
		// --------------------------------------------------------------------
		public void CopyFrom(FindKeywordWindowViewModel source)
		{
			Keyword = source.Keyword;
			CaseSensitive = source.CaseSensitive;
			WholeMatch = source.WholeMatch;
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				// タイトルバー
				Title = "キーワード検索";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
#if TEST
				Title = "［テスト］" + Title;
#endif
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "環境設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean disposing)
		{
			IsClosed = true;
			base.Dispose(disposing);
		}

		// ====================================================================
		// private 関数
		// ====================================================================

		// ファイル一覧ウィンドウの ViewModel
		private readonly ViewModel _viewTFoundsWindowViewModel;
	}
}
