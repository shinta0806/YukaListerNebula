// ============================================================================
// 
// ID 接頭辞入力ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	internal class InputIdPrefixWindowViewModel : YlViewModel
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ID 接頭辞
		private String? _idPrefix;
		public String? IdPrefix
		{
			get => _idPrefix;
			set
			{
				if (RaisePropertyChangedIfSet(ref _idPrefix, value))
				{
					ButtonOKClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// ID 接頭辞フォーカス
		private Boolean _isIdPrefixFocused;
		public Boolean IsIdPrefixFocused
		{
			get => _isIdPrefixFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isIdPrefixFocused = value;
				RaisePropertyChanged(nameof(IsIdPrefixFocused));
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

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOKClickedCommand;

		public ViewModelCommand ButtonOKClickedCommand
		{
			get
			{
				if (_buttonOKClickedCommand == null)
				{
					_buttonOKClickedCommand = new ViewModelCommand(CButtonOKClicked, CanButtonOKClicked);
				}
				return _buttonOKClickedCommand;
			}
		}

		public Boolean CanButtonOKClicked()
		{
			return !String.IsNullOrEmpty(IdPrefix);
		}

		public void CButtonOKClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(IdPrefix))
				{
					throw new Exception("ID 接頭辞を入力してください。");
				}

				YlModel.Instance.EnvModel.YlSettings.IdPrefix = YlCommon.CheckIdPrefix(IdPrefix, false);
				YlModel.Instance.EnvModel.YlSettings.Save();

				Messenger.Raise(new WindowActionMessage(Common.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
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
				Title = "ID 接頭辞の設定";

				// フォーカス
				IsIdPrefixFocused = true;
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "ID 接頭辞入力ウィンドウビューモデル初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
	}
}
