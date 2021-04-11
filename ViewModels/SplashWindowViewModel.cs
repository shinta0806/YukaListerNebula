// ============================================================================
// 
// スプラッシュウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 本クラスは YukaListerModel 未生成時に生成されるため、YlViewModel を継承しない
// ----------------------------------------------------------------------------

using Livet;
using Livet.Messaging;
using Livet.Messaging.Windows;

using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

using Shinta;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

using YukaLister.Models.Database;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class SplashWindowViewModel : ViewModel
	{
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウを閉じる
		// --------------------------------------------------------------------
		public void Close()
		{
			_timer?.Stop();
			Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public void Initialize()
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

			// 環境
			YukaListerModel.Instance.EnvModel.YlSettings.Load();
			YukaListerModel.Instance.EnvModel.TagSettings.Load();
			DbCommon.PrepareDatabases();
			YukaListerModel.Instance.EnvModel.StartAllCores();

#if DEBUGz
			Thread.Sleep(3000);
#endif

			// メインウィンドウ表示
			_mainWindowViewModel = new MainWindowViewModel(this);
			if (YukaListerModel.Instance.EnvModel.YlSettings.DesktopBounds.Width == 0.0 || YukaListerModel.Instance.EnvModel.YlSettings.DesktopBounds.Height == 0.0)
			{
				// デフォルトウィンドウサイズ
			}
			else
			{
				// 前回のウィンドウサイズ
				Rect adjustedRect = CommonWindows.AdjustWindowRect(YukaListerModel.Instance.EnvModel.YlSettings.DesktopBounds);
				_mainWindowViewModel.Left = adjustedRect.Left;
				_mainWindowViewModel.Top = adjustedRect.Top;
				_mainWindowViewModel.Width = adjustedRect.Width;
				_mainWindowViewModel.Height = adjustedRect.Height;
			}
			OpenMainWindow();

			// メッセージがうまく伝播されないのかメインウィンドウが開かないことがあるかもしれないため、時間差で再度開くようにする（不要かもしれないが念のため）
			_timer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromSeconds(1.0),
			};
			_timer.Tick += (s, e) =>
			{
				OpenMainWindow();
			};
			_timer.Start();
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// メインウィンドウ
		private MainWindowViewModel? _mainWindowViewModel;

		// メインウィンドウを確実に開くためのタイマー
		private DispatcherTimer? _timer;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// メインウィンドウを開く
		// --------------------------------------------------------------------
		private void OpenMainWindow()
		{
			Messenger.Raise(new TransitionMessage(_mainWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_MAIN_WINDOW));
		}
	}
}
