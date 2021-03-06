// ============================================================================
// 
// メインウィンドウの ViewModel 
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Diagnostics;
using System.Windows;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class MainWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel(SplashWindowViewModel splashWindowViewModel)
		{
			_splashWindowViewModel = splashWindowViewModel;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel()
		{
			// 警告抑止用にメンバーを null! で初期化
			_splashWindowViewModel = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ウィンドウ左端
		private Double _left;
		public Double Left
		{
			get => _left;
			set => RaisePropertyChangedIfSet(ref _left, value);
		}

		// ウィンドウ上端
		private Double _top;
		public Double Top
		{
			get => _top;
			set => RaisePropertyChangedIfSet(ref _top, value);
		}

		// ウィンドウ幅
		private Double _width;
		public Double Width
		{
			get => _width;
			set => RaisePropertyChangedIfSet(ref _width, value);
		}

		// ウィンドウ高さ
		private Double _height;
		public Double Height
		{
			get => _height;
			set => RaisePropertyChangedIfSet(ref _height, value);
		}

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

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
				// スプラッシュウィンドウを閉じる
				_splashWindowViewModel.Close();

				// 環境の変化に対応
				//DoVerChangedIfNeeded();
				//LaunchUpdaterIfNeeded();

#if false
				// インストール場所についての警告
				String? installMsg = InstallWarningMessage();
				if (!String.IsNullOrEmpty(installMsg))
				{
					List<String> miscWarnings = new List<String>();
					miscWarnings.Add(installMsg);
					NicoKaraMaker2Model.Instance.EnvModel.MiscWarnings = miscWarnings;
				}
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_isDisposed)
			{
				return;
			}

			try
			{
				SaveExitStatus();
				_isDisposed = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ破棄時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// スプラッシュウィンドウ
		private readonly SplashWindowViewModel _splashWindowViewModel;

		// Dispose フラグ
		private Boolean _isDisposed = false;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 終了時の状態を保存
		// --------------------------------------------------------------------
		private void SaveExitStatus()
		{
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchPath = YukaListerModel.Instance.EnvModel.ExeFullPath;
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchGeneration = YlConstants.APP_GENERATION;
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer = YlConstants.APP_VER;
			YukaListerModel.Instance.EnvModel.YlSettings.DesktopBounds = new Rect(Left, Top, Width, Height);
			YukaListerModel.Instance.EnvModel.YlSettings.Save();
		}


	}
}
