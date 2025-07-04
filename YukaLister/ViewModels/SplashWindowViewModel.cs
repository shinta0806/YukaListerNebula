// ============================================================================
// 
// スプラッシュウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 本クラスは YlModel 未生成時に生成されるため、YlViewModel の派生にせず、基底クラスに LogWriter を渡さない
// ----------------------------------------------------------------------------

using Livet.Messaging;

using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

using Shinta;
using Shinta.Wpf;
using Shinta.Wpf.ViewModels;

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
	internal class SplashWindowViewModel : BasicWindowViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public SplashWindowViewModel()
		{
		}

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
				// インスタンス生成
				_ = YlModel.Instance;

#if false
				// マテリアルデザインの外観を変更
				IEnumerable<Swatch> swatches = new SwatchesProvider().Swatches;
				PaletteHelper paletteHelper = new();
				Theme theme = paletteHelper.GetTheme();
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
#endif

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
					Rect adjustedRect = WpfCommon.AdjustWindowRect(YlModel.Instance.EnvModel.YlSettings.DesktopBounds);
					_mainWindowViewModel.Left = adjustedRect.Left;
					_mainWindowViewModel.Top = adjustedRect.Top;
					_mainWindowViewModel.Width = adjustedRect.Width;
					_mainWindowViewModel.Height = adjustedRect.Height;
				}
				Messenger.Raise(new TransitionMessage(_mainWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_MAIN_WINDOW));
			}
			catch (Exception ex)
			{
				// YlModel 未生成の可能性があるためまずはメッセージ表示のみ
				MessageBox.Show("スプラッシュウィンドウ初期化時エラー：\n" + ex.Message + "\n" + ex.StackTrace, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

				// 可能であればログする。YlModel 生成中に例外が発生する可能性があるが、それについては集約エラーハンドラーに任せる
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "スプラッシュウィンドウ初期化時エラー：\n" + ex.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);

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
