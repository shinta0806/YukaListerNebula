// ============================================================================
// 
// 同期タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;

using Shinta;

using System;
using System.Diagnostics;
using System.Windows;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemSyncViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemSyncViewModel(YlViewModel windowViewModel)
				: base(windowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemSyncViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 楽曲情報データベースを同期する
		private Boolean _syncMusicInfoDb;
		public Boolean SyncMusicInfoDb
		{
			get => _syncMusicInfoDb;
			set
			{
				if (RaisePropertyChangedIfSet(ref _syncMusicInfoDb, value))
				{
					ButtonRegetClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// サーバー URL
		private String? _syncServer;
		public String? SyncServer
		{
			get => _syncServer;
			set => RaisePropertyChangedIfSet(ref _syncServer, value);
		}

		// アカウント名
		private String? _syncAccount;
		public String? SyncAccount
		{
			get => _syncAccount;
			set => RaisePropertyChangedIfSet(ref _syncAccount, value);
		}

		// パスワード
		private String? _syncPassword;
		public String? SyncPassword
		{
			get => _syncPassword;
			set => RaisePropertyChangedIfSet(ref _syncPassword, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 強制的に合わせるボタンの制御
		private ViewModelCommand? _buttonRegetClickedCommand;

		public ViewModelCommand ButtonRegetClickedCommand
		{
			get
			{
				if (_buttonRegetClickedCommand == null)
				{
					_buttonRegetClickedCommand = new ViewModelCommand(ButtonRegetClicked, CanButtonRegetClicked);
				}
				return _buttonRegetClickedCommand;
			}
		}

		public Boolean CanButtonRegetClicked()
		{
			return SyncMusicInfoDb;
		}

		public void ButtonRegetClicked()
		{
			try
			{
				if (YlModel.Instance.EnvModel.Syclin.IsActive)
				{
					throw new Exception("現在、同期処理を実行中のため、合わせられません。\n同期処理が終了してから合わせてください。");
				}

				if (MessageBox.Show("ローカルの楽曲情報データベース・ゆかり統計データベースをすべて削除してから、内容をサーバーに合わせます。\n\n"
						+ "【注意】\n"
						+ "サーバーにアップロードしていないデータおよび、楽曲情報データベースのタグ情報は全て失われます。\n"
						+ "事前に楽曲情報データベースをエクスポートすることをお薦めします。\n\n"
						+ "内容をサーバーに合わせてよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					return;
				}

				YlModel.Instance.EnvModel.YlSettings.LastSyncDownloadDate = 0.0;
				((YlSettingsWindowViewModel)_windowViewModel).RegetSyncDataNeeded = true;
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "環境設定ウィンドウを閉じると処理を開始します。");
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "強制的に合わせるボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region サーバープログラムボタンの制御
		private ViewModelCommand? _buttonServerClickedCommand;

		public ViewModelCommand ButtonServerClickedCommand
		{
			get
			{
				if (_buttonServerClickedCommand == null)
				{
					_buttonServerClickedCommand = new ViewModelCommand(ButtonServerClicked);
				}
				return _buttonServerClickedCommand;
			}
		}

		public static void ButtonServerClicked()
		{
			try
			{
				Common.ShellExecute(YlModel.Instance.EnvModel.ExeFullFolder + FOLDER_NAME_SYNC_SERVER);
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "サーバープログラムボタンクリック時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public override void CheckInput()
		{
			if (SyncMusicInfoDb)
			{
				if (String.IsNullOrEmpty(SyncServer) || SyncServer == "http://" || SyncServer == "https://")
				{
					throw new Exception("同期用のサーバー URL を指定して下さい。");
				}
				if (SyncServer?.Contains("http://") != true && SyncServer?.Contains("https://") != true)
				{
					throw new Exception("http:// または https:// で始まる同期用のサーバー URL を指定して下さい。");
				}
				if (String.IsNullOrEmpty(SyncAccount))
				{
					throw new Exception("同期用のアカウント名を指定して下さい。");
				}
				if (String.IsNullOrEmpty(SyncPassword))
				{
					throw new Exception("同期用のパスワードを指定して下さい。");
				}

				// 補完
				if (SyncServer[^1] != '/')
				{
					SyncServer += "/";
				}
			}
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings()
		{
			YlModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb = SyncMusicInfoDb;
			YlModel.Instance.EnvModel.YlSettings.SyncServer = SyncServer;
			YlModel.Instance.EnvModel.YlSettings.SyncAccount = SyncAccount;
			YlModel.Instance.EnvModel.YlSettings.SyncPassword = YlCommon.Encrypt(SyncPassword);
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties()
		{
			SyncMusicInfoDb = YlModel.Instance.EnvModel.YlSettings.SyncMusicInfoDb;
			SyncServer = YlModel.Instance.EnvModel.YlSettings.SyncServer;
			SyncAccount = YlModel.Instance.EnvModel.YlSettings.SyncAccount;
			SyncPassword = YlCommon.Decrypt(YlModel.Instance.EnvModel.YlSettings.SyncPassword);
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// サーバープログラムフォルダー
		private const String FOLDER_NAME_SYNC_SERVER = "SyncServer";
	}
}
