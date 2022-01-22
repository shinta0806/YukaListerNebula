// ============================================================================
// 
// スプラッシュウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 本クラスは YlModel 未生成時に生成されるため、YlViewModel を継承しない
// ----------------------------------------------------------------------------

using Livet;
using Livet.Messaging;
using Livet.Messaging.Windows;

using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class SplashWindowViewModel : ViewModel
	{
		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウを閉じる
		// --------------------------------------------------------------------
		public void Close()
		{
			Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public void Initialize()
		{
			try
			{
				// マテリアルデザインの外観を変更
				IEnumerable<Swatch> swatches = new SwatchesProvider().Swatches;
				PaletteHelper paletteHelper = new();
				ITheme theme = paletteHelper.GetTheme();
				Swatch? orangeSwatch = swatches.FirstOrDefault(x => x.Name == "orange");
				if (orangeSwatch != null)
				{
					theme.SetPrimaryColor(orangeSwatch.ExemplarHue.Color);
				}
				Swatch? limeSwatch = swatches.FirstOrDefault(x => x.Name == "yellow");
				if (limeSwatch != null)
				{
					theme.SetSecondaryColor(limeSwatch.ExemplarHue.Color);
				}
				paletteHelper.SetTheme(theme);

				// テンポラリフォルダー準備
				Common.InitializeTempFolder();

				// 環境
				YlModel.Instance.EnvModel.YlSettings.Load();
				YlModel.Instance.EnvModel.YlSettings.SetLogWriter(YlModel.Instance.EnvModel.LogWriter);
				YlModel.Instance.EnvModel.TagSettings.Load();
				YlModel.Instance.EnvModel.TagSettings.SetLogWriter(YlModel.Instance.EnvModel.LogWriter);
				DbCommon.PrepareDatabases();
				YlModel.Instance.EnvModel.StartAllCores();

				// メインウィンドウ表示
				_mainWindowViewModel = new MainWindowViewModel(this);
				if (YlModel.Instance.EnvModel.YlSettings.DesktopBounds.Width == 0.0 || YlModel.Instance.EnvModel.YlSettings.DesktopBounds.Height == 0.0)
				{
					// デフォルトウィンドウサイズ
				}
				else
				{
					// 前回のウィンドウサイズ
					Rect adjustedRect = CommonWindows.AdjustWindowRect(YlModel.Instance.EnvModel.YlSettings.DesktopBounds);
					_mainWindowViewModel.Left = adjustedRect.Left;
					_mainWindowViewModel.Top = adjustedRect.Top;
					_mainWindowViewModel.Width = adjustedRect.Width;
					_mainWindowViewModel.Height = adjustedRect.Height;
				}
				Messenger.Raise(new TransitionMessage(_mainWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_MAIN_WINDOW));
			}
			catch (Exception excep)
			{
				// YlModel 未生成の可能性があるためまずはメッセージ表示のみ
				MessageBox.Show("スプラッシュウィンドウ初期化時エラー：\n" + excep.Message + "\n" + excep.StackTrace, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

				// 可能であればログする。YlModel 生成中に例外が発生する可能性があるが、それについては集約エラーハンドラーに任せる
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "スプラッシュウィンドウ初期化時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);

				// 継続できないのでアプリを終了する
				Environment.Exit(1);
			}
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// メインウィンドウ（アプリ終了時まで保持する必要がある）
		private MainWindowViewModel? _mainWindowViewModel;
	}
}
