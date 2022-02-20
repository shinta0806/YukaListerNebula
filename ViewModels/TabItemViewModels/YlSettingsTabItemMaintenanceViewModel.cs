// ============================================================================
// 
// メンテナンスタブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;

using Shinta;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemMaintenanceViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemMaintenanceViewModel(YlSettingsWindowViewModel ylSettingsWindowViewModel)
				: base(ylSettingsWindowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public YlSettingsTabItemMaintenanceViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ゆかりすたーの最新情報・更新版を自動的に確認する
		private Boolean _checkRss;
		public Boolean CheckRss
		{
			get => _checkRss;
			set
			{
				if (_checkRss && !value
						&& MessageBox.Show("最新情報の確認を無効にすると、" + YlConstants.APP_NAME_J
						+ "の新版がリリースされた際の更新内容などが表示されません。\n\n"
						+ "本当に無効にしてもよろしいですか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
						!= MessageBoxResult.Yes)
				{
					return;
				}

				RaisePropertyChangedIfSet(ref _checkRss, value);
			}
		}

		// プログレスバー表示
		private Visibility _progressBarCheckRssVisibility;
		public Visibility ProgressBarCheckRssVisibility
		{
			get => _progressBarCheckRssVisibility;
			set
			{
				if (RaisePropertyChangedIfSet(ref _progressBarCheckRssVisibility, value))
				{
					ButtonCheckRssClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 今すぐ最新情報を確認するボタンの制御
		private ViewModelCommand? _buttonCheckRssClickedCommand;

		public ViewModelCommand ButtonCheckRssClickedCommand
		{
			get
			{
				if (_buttonCheckRssClickedCommand == null)
				{
					_buttonCheckRssClickedCommand = new ViewModelCommand(ButtonCheckRssClicked, CanButtonCheckRssClicked);
				}
				return _buttonCheckRssClickedCommand;
			}
		}

		public Boolean CanButtonCheckRssClicked()
		{
			return ProgressBarCheckRssVisibility != Visibility.Visible;
		}

		public async void ButtonCheckRssClicked()
		{
			try
			{
				ProgressBarCheckRssVisibility = Visibility.Visible;
				await YlCommon.CheckLatestInfoAsync(true);
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "最新情報確認時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				ProgressBarCheckRssVisibility = Visibility.Hidden;
			}
		}
		#endregion

		#region 設定のバックアップボタンの制御
		private ViewModelCommand? _buttonBackupClickedCommand;

		public ViewModelCommand ButtonBackupClickedCommand
		{
			get
			{
				if (_buttonBackupClickedCommand == null)
				{
					_buttonBackupClickedCommand = new ViewModelCommand(ButtonBackupClicked);
				}
				return _buttonBackupClickedCommand;
			}
		}

		public void ButtonBackupClicked()
		{
			try
			{
				String? path = _tabControlWindowViewModel.PathBySavingDialog("設定のバックアップ", YlConstants.DIALOG_FILTER_SETTINGS_ARCHIVE, "YukaListerSettings_" + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss"));
				if (path == null)
				{
					return;
				}

				YlCommon.LogEnvironmentInfo();
				ZipFile.CreateFromDirectory(Common.UserAppDataFolderPath(), path, CompressionLevel.Optimal, true);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "設定のバックアップが完了しました。");
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "設定のバックアップボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 設定の復元ボタンの制御

		private ViewModelCommand? _buttonRestoreClickedCommand;

		public ViewModelCommand ButtonRestoreClickedCommand
		{
			get
			{
				if (_buttonRestoreClickedCommand == null)
				{
					_buttonRestoreClickedCommand = new ViewModelCommand(ButtonRestoreClicked);
				}
				return _buttonRestoreClickedCommand;
			}
		}

		public void ButtonRestoreClicked()
		{
			try
			{
				String? path = _tabControlWindowViewModel.PathByOpeningDialog("設定の復元", YlConstants.DIALOG_FILTER_SETTINGS_ARCHIVE, null);
				if (path == null)
				{
					return;
				}

				if (MessageBox.Show("現在の設定は破棄され、" + Path.GetFileName(path) + " の設定に変更されます。\nよろしいですか？", "確認", MessageBoxButton.YesNo,
						MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
				{
					return;
				}

				// 解凍
				String unzipFolder = YlCommon.TempPath() + "\\";
				Directory.CreateDirectory(unzipFolder);
				ZipFile.ExtractToDirectory(path, unzipFolder);

				// 設定更新
				String settingsFilePath = unzipFolder + Path.GetFileName(Path.GetDirectoryName(Common.UserAppDataFolderPath())) + "\\" + Path.GetFileName(YlSettings.YlSettingsPath());
				File.Copy(settingsFilePath, YlSettings.YlSettingsPath(), true);
				YlModel.Instance.EnvModel.YlSettings.Load();
				_tabControlWindowViewModel.Initialize();
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "設定を復元しました。");
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "設定の復元ボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
			// プログレスバー
			ProgressBarCheckRssVisibility = Visibility.Hidden;
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings(YlSettings destSettings)
		{
			destSettings.CheckRss = CheckRss;
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties(YlSettings srcSettings)
		{
			CheckRss = srcSettings.CheckRss;
		}
	}
}
