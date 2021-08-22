// ============================================================================
// 
// アプリケーション
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet;

using Shinta;

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister
{
	public partial class App : Application
	{
		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 多重起動防止用
		// アプリケーション終了までガベージコレクションされないようにメンバー変数で持つ
		private Mutex? _mutex;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// スタートアップ
		// --------------------------------------------------------------------
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Livet コード
			DispatcherHelper.UIDispatcher = Dispatcher;

			// 集約エラーハンドラー設定
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			// 多重起動チェック
			_mutex = CommonWindows.ActivateAnotherProcessWindowIfNeeded(Common.SHINTA + '_' + YlConstants.APP_ID + '_' + YlConstants.APP_GENERATION);
			if (_mutex == null)
			{
				throw new MultiInstanceException();
			}
		}

		// --------------------------------------------------------------------
		// 集約エラーハンドラー
		// --------------------------------------------------------------------
		private void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
		{
			if (unhandledExceptionEventArgs.ExceptionObject is MultiInstanceException)
			{
				// 多重起動の場合は何もしない
			}
			else
			{
				if (unhandledExceptionEventArgs.ExceptionObject is Exception excep)
				{
					// YukaListerModel 未生成の可能性があるためまずはメッセージ表示のみ
					MessageBox.Show("不明なエラーが発生しました。アプリケーションを終了します。\n" + excep.Message + "\n" + excep.InnerException?.Message + "\n" + excep.StackTrace,
							"エラー", MessageBoxButton.OK, MessageBoxImage.Error);

					try
					{
						// 可能であればログする。YukaListerModel 生成中に例外が発生する可能性がある
						YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "集約エラーハンドラー：\n" + excep.Message + "\n" + excep.InnerException?.Message);
						YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
					}
					catch (Exception)
					{
						MessageBox.Show("エラーの記録ができませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}

			Environment.Exit(1);
		}
	}
}
