// ============================================================================
// 
// 環境設定ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;

using Shinta;
using Shinta.ViewModels;

using System;
using System.Diagnostics;

using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.TabItemViewModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	internal class YlSettingsWindowViewModel : TabControlWindowViewModel<YlSettings>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsWindowViewModel()
				: base(YlModel.Instance.EnvModel.LogWriter)
		{
			Debug.Assert(_tabItemViewModels.Length == (Int32)YlSettingsTabItem.__End__, "YlSettingsWindowViewModel() bad tab vm nums");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// タブアイテム：設定
		public YlSettingsTabItemSettingsViewModel YlSettingsTabItemSettingsViewModel
		{
			get => (YlSettingsTabItemSettingsViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.Settings];
		}

		// タブアイテム：リスト対象
		public YlSettingsTabItemListTargetViewModel YlSettingsTabItemListTargetViewModel
		{
			get => (YlSettingsTabItemListTargetViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.ListTarget];
		}

		// タブアイテム：リスト出力
		public YlSettingsTabItemListOutputViewModel YlSettingsTabItemListOutputViewModel
		{
			get => (YlSettingsTabItemListOutputViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.ListOutput];
		}

		// タブアイテム：メンテナンス
		public YlSettingsTabItemMaintenanceViewModel YlSettingsTabItemMaintenanceViewModel
		{
			get => (YlSettingsTabItemMaintenanceViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.Maintenance];
		}

		// タブアイテム：楽曲情報一覧
		public YlSettingsTabItemMusicInfoListViewModel YlSettingsTabItemMusicInfoListViewModel
		{
			get => (YlSettingsTabItemMusicInfoListViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.MusicInfoList];
		}

		// タブアイテム：楽曲情報一括操作
		public YlSettingsTabItemMusicInfoBatchViewModel YlSettingsTabItemMusicInfoBatchViewModel
		{
			get => (YlSettingsTabItemMusicInfoBatchViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.MusicInfoBatch];
		}

		// タブアイテム：ゆかり統計
		public YlSettingsTabItemYukariStatisticsViewModel YlSettingsTabItemYukariStatisticsViewModel
		{
			get => (YlSettingsTabItemYukariStatisticsViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.YukariStatistics];
		}

		// タブアイテム：同期
		public YlSettingsTabItemSyncViewModel YlSettingsTabItemSyncViewModel
		{
			get => (YlSettingsTabItemSyncViewModel)_tabItemViewModels[(Int32)YlSettingsTabItem.Sync];
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// 強制再取得をユーザーから指示されたか
		public Boolean RegetSyncDataNeeded { get; set; }

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YlModel.Instance.EnvModel.HelpClickedCommand;
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
				Title = "環境設定";

				// 設定
				SettingsToProperties(YlModel.Instance.EnvModel.YlSettings);
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "環境設定ウィンドウ初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public void YukariConfigPathSeedChanged(String newSeed)
		{
			YlSettingsTabItemListOutputViewModel.YukariConfigPathSeedChanged(newSeed);
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// タブアイテムの ViewModel を生成
		// --------------------------------------------------------------------
		protected override TabItemViewModel<YlSettings>[] CreateTabItemViewModels()
		{
			return new TabItemViewModel<YlSettings>[]
			{
				new YlSettingsTabItemSettingsViewModel(this),
				new YlSettingsTabItemListTargetViewModel(this),
				new YlSettingsTabItemListOutputViewModel(this),
				new YlSettingsTabItemMaintenanceViewModel(this),
				new YlSettingsTabItemMusicInfoListViewModel(this),
				new YlSettingsTabItemMusicInfoBatchViewModel(this),
				new YlSettingsTabItemYukariStatisticsViewModel(this),
				new YlSettingsTabItemSyncViewModel(this),
			};
		}

		// --------------------------------------------------------------------
		// プロパティーを設定に反映
		// --------------------------------------------------------------------
		protected override void PropertiesToSettings()
		{
			base.PropertiesToSettings();

			PropertiesToSettings(YlModel.Instance.EnvModel.YlSettings);
		}

		// --------------------------------------------------------------------
		// 設定を保存
		// --------------------------------------------------------------------
		protected override void SaveSettings()
		{
			base.SaveSettings();

			YlModel.Instance.EnvModel.YlSettings.Save();
		}
	}
}
