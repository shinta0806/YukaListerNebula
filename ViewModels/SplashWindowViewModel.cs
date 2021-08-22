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
using System.Diagnostics;
using System.IO;
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
		// public メンバー関数
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
				String tempFolderPath = YlCommon.TempFolderPath();
				try
				{
					// 偶然以前と同じ PID となり、かつ、以前異常終了してテンポラリフォルダーが削除されていない場合に対応
					Directory.Delete(tempFolderPath, true);
				}
				catch
				{
				}
				try
				{
					Directory.CreateDirectory(tempFolderPath);
				}
				catch
				{
				}

				// 環境
				YukaListerModel.Instance.EnvModel.YlSettings.Load();
				YukaListerModel.Instance.EnvModel.TagSettings.Load();
				DbCommon.PrepareDatabases();
				YukaListerModel.Instance.EnvModel.StartAllCores();

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
				Messenger.Raise(new TransitionMessage(_mainWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_MAIN_WINDOW));
			}
			catch (Exception excep)
			{
				// YukaListerModel 未生成の可能性があるためまずはメッセージ表示のみ
				MessageBox.Show("スプラッシュウィンドウ初期化時エラー：\n" + excep.Message + "\n" + excep.StackTrace, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

				// 可能であればログする。YukaListerModel 生成中に例外が発生する可能性があるが、それについては集約エラーハンドラーに任せる
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "スプラッシュウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);

				// 継続できないのでアプリを終了する
				Environment.Exit(1);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// メインウィンドウ（アプリ終了時まで保持する必要がある）
		private MainWindowViewModel? _mainWindowViewModel;
	}
}
