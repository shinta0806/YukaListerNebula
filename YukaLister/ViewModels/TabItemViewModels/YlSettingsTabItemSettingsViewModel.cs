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
using System.Windows;

using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemSettingsViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemSettingsViewModel(YlSettingsWindowViewModel ylSettingsWindowViewModel)
				: base(ylSettingsWindowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
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
					((YlSettingsWindowViewModel)_tabControlWindowViewModel).YukariConfigPathSeedChanged(_yukariConfigPathSeed);
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

		// ゆかり用のさらなるフリガナデータを出力するか
		private Boolean _outputAdditionalYukariRuby;
		public Boolean OutputAdditionalYukariRuby
		{
			get => _outputAdditionalYukariRuby;
			set => RaisePropertyChangedIfSet(ref _outputAdditionalYukariRuby, value);
		}

		// 楽曲情報データベースが不十分な場合の誤適用を軽減
		private Boolean _applyMusicInfoIntelligently;
		public Boolean ApplyMusicInfoIntelligently
		{
			get => _applyMusicInfoIntelligently;
			set
			{
				if (_isApplyMusicInfoIntelligentlyWarningEnabled && !_applyMusicInfoIntelligently && value
						&& MessageBox.Show("このオプションを有効にすると、処理速度が遅くなります。\n\n"
						+ "楽曲情報データベースを整備するまでの暫定対応としてのみ使用し、楽曲情報データベースを整備次第、このオプションは無効にしてください。\n\n"
						+ "本当に有効にしてもよろしいですか？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
						!= MessageBoxResult.Yes)
				{
					return;
				}

				RaisePropertyChangedIfSet(ref _applyMusicInfoIntelligently, value);
			}
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

		#region プレビュードラッグの制御（ゆかり設定ファイル）
		private ListenerCommand<String[]>? _textBoxYukariConfigPathSeedPreviewDragCommand;

		public ListenerCommand<String[]> TextBoxYukariConfigPathSeedPreviewDragCommand
		{
			get
			{
				if (_textBoxYukariConfigPathSeedPreviewDragCommand == null)
				{
					_textBoxYukariConfigPathSeedPreviewDragCommand = new ListenerCommand<String[]>(TextBoxYukariConfigPathSeedPreviewDrag);
				}
				return _textBoxYukariConfigPathSeedPreviewDragCommand;
			}
		}

		public static void TextBoxYukariConfigPathSeedPreviewDrag(String[] files)
		{
			// FileDropAttachedBehavior がドラッグを許可するよう、本コマンドが存在するが、処理は行わない
		}
		#endregion

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
				String? path = _tabControlWindowViewModel.PathByOpeningDialog("ゆかり設定ファイル", "ゆかり設定ファイル|" + YlConstants.FILE_NAME_YUKARI_CONFIG, YlConstants.FILE_NAME_YUKARI_CONFIG);
				if (path != null)
				{
					YukariConfigPathSeed = path;
				}
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "ゆかり設定ファイル参照ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
		public override void CheckProperties()
		{
			base.CheckProperties();

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
		public override void PropertiesToSettings(YlSettings destSettings)
		{
			destSettings.YukariConfigPathSeed2 = YukariConfigPathSeed;
			destSettings.AddFolderOnDeviceArrived = AddFolderOnDeviceArrived;
			destSettings.ProvideYukariPreview = ProvideYukariPreview;
			destSettings.OutputAdditionalYukariAssist = OutputAdditionalYukariAssist;
			destSettings.OutputAdditionalYukariRuby = OutputAdditionalYukariRuby;
			destSettings.ApplyMusicInfoIntelligently = ApplyMusicInfoIntelligently;
			destSettings.IdPrefix = IdPrefix;
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties(YlSettings srcSettings)
		{
			YukariConfigPathSeed = srcSettings.YukariConfigPathSeed2;
			AddFolderOnDeviceArrived = srcSettings.AddFolderOnDeviceArrived;
			ProvideYukariPreview = srcSettings.ProvideYukariPreview;
			OutputAdditionalYukariAssist = srcSettings.OutputAdditionalYukariAssist;
			OutputAdditionalYukariRuby = srcSettings.OutputAdditionalYukariRuby;
			ApplyMusicInfoIntelligently = srcSettings.ApplyMusicInfoIntelligently;
			IdPrefix = srcSettings.IdPrefix;

			// 初期化完了で警告を有効にする
			_isApplyMusicInfoIntelligentlyWarningEnabled = true;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// 楽曲情報データベースが不十分な場合の誤適用を軽減を有効にしようとした時の警告を表示するか
		// 初期化時に表示されるのを抑止するため
		private Boolean _isApplyMusicInfoIntelligentlyWarningEnabled;
	}
}
