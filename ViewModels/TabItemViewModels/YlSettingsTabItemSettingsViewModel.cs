// ============================================================================
// 
// 設定タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;

using Shinta;

using System;
using System.Diagnostics;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	public class YlSettingsTabItemSettingsViewModel : TabItemViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemSettingsViewModel(YlViewModel windowViewModel)
				: base(windowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemSettingsViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ゆかり設定ファイル
		private String _yukariConfigPathSeed = String.Empty;
		public String YukariConfigPathSeed
		{
			get => _yukariConfigPathSeed;
			set
			{
				if (RaisePropertyChangedIfSet(ref _yukariConfigPathSeed, value))
				{
					((YlSettingsWindowViewModel)_windowViewModel).YukariConfigPathSeedChanged(_yukariConfigPathSeed);
				}
			}
		}

		// リムーバブルメディア接続時、前回のフォルダーを自動的に追加する
		private Boolean _addFolderOnDeviceArrived;
		public Boolean AddFolderOnDeviceArrived
		{
			get => _addFolderOnDeviceArrived;
			set => RaisePropertyChangedIfSet(ref _addFolderOnDeviceArrived, value);
		}

		// ゆかりでのプレビューを可能にするか
		private Boolean _provideYukariPreview;
		public Boolean ProvideYukariPreview
		{
			get => _provideYukariPreview;
			set => RaisePropertyChangedIfSet(ref _provideYukariPreview, value);
		}

		// ゆかり用のさらなる検索支援データを出力するか
		private Boolean _outputAdditionalYukariAssist;
		public Boolean OutputAdditionalYukariAssist
		{
			get => _outputAdditionalYukariAssist;
			set => RaisePropertyChangedIfSet(ref _outputAdditionalYukariAssist, value);
		}

		// ID 接頭辞
		private String? _idPrefix;
		public String? IdPrefix
		{
			get => _idPrefix;
			set => RaisePropertyChangedIfSet(ref _idPrefix, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ゆかり設定ファイル参照ボタンの制御
		private ViewModelCommand? _buttonBrowseYukariConfigPathSeedClickedCommand;

		public ViewModelCommand ButtonBrowseYukariConfigPathSeedClickedCommand
		{
			get
			{
				if (_buttonBrowseYukariConfigPathSeedClickedCommand == null)
				{
					_buttonBrowseYukariConfigPathSeedClickedCommand = new ViewModelCommand(ButtonBrowseYukariConfigPathSeedClicked);
				}
				return _buttonBrowseYukariConfigPathSeedClickedCommand;
			}
		}

		public void ButtonBrowseYukariConfigPathSeedClicked()
		{
			try
			{
				String? path = _windowViewModel.PathByOpeningDialog("ゆかり設定ファイル", "ゆかり設定ファイル|" + YlConstants.FILE_NAME_YUKARI_CONFIG, YlConstants.FILE_NAME_YUKARI_CONFIG);
				if (path != null)
				{
					YukariConfigPathSeed = path;
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかり設定ファイル参照ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public override void CheckInput()
		{
			if (String.IsNullOrEmpty(YukariConfigPathSeed))
			{
				throw new Exception("ゆかり設定ファイルを指定して下さい。");
			}
			IdPrefix = YlCommon.CheckIdPrefix(IdPrefix, true);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：ファイルやフォルダーがドロップされた
		// --------------------------------------------------------------------
		public override void PathDropped(String[] pathes)
		{
			YukariConfigPathSeed = DroppedFile(pathes, new String[] { Common.FILE_EXT_INI });
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings()
		{
			YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPathSeed = YukariConfigPathSeed;
			YukaListerModel.Instance.EnvModel.YlSettings.AddFolderOnDeviceArrived = AddFolderOnDeviceArrived;
			YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview = ProvideYukariPreview;
			YukaListerModel.Instance.EnvModel.YlSettings.OutputAdditionalYukariAssist = OutputAdditionalYukariAssist;
			YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix = IdPrefix;
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties()
		{
			YukariConfigPathSeed = YukaListerModel.Instance.EnvModel.YlSettings.YukariConfigPathSeed;
			AddFolderOnDeviceArrived = YukaListerModel.Instance.EnvModel.YlSettings.AddFolderOnDeviceArrived;
			ProvideYukariPreview = YukaListerModel.Instance.EnvModel.YlSettings.ProvideYukariPreview;
			OutputAdditionalYukariAssist = YukaListerModel.Instance.EnvModel.YlSettings.OutputAdditionalYukariAssist;
			IdPrefix = YukaListerModel.Instance.EnvModel.YlSettings.IdPrefix;
		}
	}
}
