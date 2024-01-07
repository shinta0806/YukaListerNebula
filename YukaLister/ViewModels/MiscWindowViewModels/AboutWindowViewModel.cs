// ============================================================================
// 
// バージョン情報ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	internal class AboutWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public AboutWindowViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region リンククリックの制御
		private ListenerCommand<String>? _linkClickedCommand;

		public ListenerCommand<String> LinkClickedCommand
		{
			get
			{
				if (_linkClickedCommand == null)
				{
					_linkClickedCommand = new ListenerCommand<String>(LinkClicked);
				}
				return _linkClickedCommand;
			}
		}

		public void LinkClicked(String parameter)
		{
			try
			{
				Common.ShellExecute(parameter);
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "リンククリック時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
		#endregion

		#region 更新プログラムの確認ボタンの制御
		private ViewModelCommand? _buttonCheckUpdateClickedCommand;

		public ViewModelCommand ButtonCheckUpdateClickedCommand
		{
			get
			{
				if (_buttonCheckUpdateClickedCommand == null)
				{
					_buttonCheckUpdateClickedCommand = new ViewModelCommand(ButtonCheckUpdateClicked);
				}
				return _buttonCheckUpdateClickedCommand;
			}
		}

		public void ButtonCheckUpdateClicked()
		{
			try
			{
				Common.OpenMicrosoftStore(YlConstants.STORE_PRODUCT_ID);
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "更新プログラムの確認ボタンクリック時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
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
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "バージョン情報ウィンドウを開きます。");

				// 表示
				Title = YlConstants.APP_NAME_J + " のバージョン情報";
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "バージョン情報ウィンドウ初期化時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
	}
}
