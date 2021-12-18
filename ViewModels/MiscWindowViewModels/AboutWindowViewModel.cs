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
	public class AboutWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
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

		// アプリケーション名
		private String _appName = YlConstants.APP_NAME_J;
		public String AppName
		{
			get => _appName;
			set => RaisePropertyChangedIfSet(ref _appName, value);
		}

		// バージョン
		private String _appVer = YlConstants.APP_VER;
		public String AppVer
		{
			get => _appVer;
			set => RaisePropertyChangedIfSet(ref _appVer, value);
		}

		// 配布
		private String _appDistrib = YlConstants.APP_DISTRIB;
		public String AppDistrib
		{
			get => _appDistrib;
			set => RaisePropertyChangedIfSet(ref _appDistrib, value);
		}

		// コピーライト
		private String _copyright = YlConstants.COPYRIGHT_J;
		public String Copyright
		{
			get => _copyright;
			set => RaisePropertyChangedIfSet(ref _copyright, value);
		}

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

		public static void LinkClicked(String parameter)
		{
			try
			{
				YlCommon.ShellExecute(parameter);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リンククリック時エラー：\n" + excep.Message);
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
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "バージョン情報ウィンドウを開きます。");

				// 表示
				Title = YlConstants.APP_NAME_J + " のバージョン情報";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
#if TEST
				Title = "［テスト］" + Title;
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "バージョン情報ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
	}
}
