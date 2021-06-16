// ============================================================================
// 
// HTML / PHP リスト出力設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.OutputWriters;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.OutputSettingsWindowViewModels
{
	public class WebOutputSettingsWindowViewModel : OutputSettingsWindowViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public WebOutputSettingsWindowViewModel(OutputWriter outputWriter)
				: base(outputWriter)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public WebOutputSettingsWindowViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		#region HTML タブのプロパティー

		// 新着の使用
		private Boolean _enableNew;
		public Boolean EnableNew
		{
			get => _enableNew;
			set => RaisePropertyChangedIfSet(ref _enableNew, value);
		}

		// 新着の日数
		private String? _newDays;
		public String? NewDays
		{
			get => _newDays;
			set => RaisePropertyChangedIfSet(ref _newDays, value);
		}

		// 頭文字その他出力
		private Boolean _outputHeadMisc;
		public Boolean OutputHeadMisc
		{
			get => _outputHeadMisc;
			set => RaisePropertyChangedIfSet(ref _outputHeadMisc, value);
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
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "HTML / PHP リスト出力設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// タブアイテムにタブを追加
		// --------------------------------------------------------------------
		protected override void AddTabItems()
		{
			base.AddTabItems();

			AddTabItem("OutputSettingsTabItemWeb", "HTML");
		}

		// --------------------------------------------------------------------
		// 設定画面に入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected override void CheckInput()
		{
			base.CheckInput();

			// 新着の日数
			if (EnableNew)
			{
				Int32 newDays = Common.StringToInt32(NewDays);
				if (newDays < YlConstants.NEW_DAYS_MIN)
				{
					throw new Exception("新着の日数は " + YlConstants.NEW_DAYS_MIN.ToString() + " 以上を指定して下さい。");
				}
			}
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		protected override void PropertiesToSettings()
		{
			base.PropertiesToSettings();

			if (_outputWriter.OutputSettings is not WebOutputSettings webOutputSettings)
			{
				return;
			}

			// 新着の使用
			webOutputSettings.EnableNew = EnableNew;

			// 新着の日数
			if (webOutputSettings.EnableNew)
			{
				webOutputSettings.NewDays = Common.StringToInt32(NewDays);
			}

			// 頭文字その他出力
			webOutputSettings.OutputHeadMisc = OutputHeadMisc;
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void SettingsToProperties()
		{
			base.SettingsToProperties();

			if (_outputWriter.OutputSettings is not WebOutputSettings webOutputSettings)
			{
				return;
			}

			// 新着の使用
			EnableNew = webOutputSettings.EnableNew;

			// 新着の日数
			NewDays = webOutputSettings.NewDays.ToString();

			// 頭文字その他出力
			OutputHeadMisc = webOutputSettings.OutputHeadMisc;
		}
	}
}
