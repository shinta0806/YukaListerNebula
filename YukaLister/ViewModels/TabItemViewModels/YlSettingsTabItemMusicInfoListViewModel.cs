// ============================================================================
// 
// 楽曲情報一覧タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;

using Shinta;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;
using YukaLister.ViewModels.ViewMastersWindowViewModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	internal class YlSettingsTabItemMusicInfoListViewModel : YlTabItemViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemMusicInfoListViewModel(YlSettingsWindowViewModel ylSettingsWindowViewModel)
				: base(ylSettingsWindowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemMusicInfoListViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region タイアップ一覧ボタンの制御
		private ViewModelCommand? _buttonTieUpsClickedCommand;

		public ViewModelCommand ButtonTieUpsClickedCommand
		{
			get
			{
				if (_buttonTieUpsClickedCommand == null)
				{
					_buttonTieUpsClickedCommand = new ViewModelCommand(ButtonTieUpsClicked);
				}
				return _buttonTieUpsClickedCommand;
			}
		}

		public void ButtonTieUpsClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();

				// ViewModel 経由で楽曲情報データベースマスター一覧ウィンドウを開く
				using ViewTieUpsWindowViewModel viewTieUpsWindowViewModel = new(musicInfoContextDefault, musicInfoContextDefault.TieUps, CreateTieUpColumns());
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(viewTieUpsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_MASTERS_WINDOW));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タイアップ一覧ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 制作会社一覧ボタンの制御
		private ViewModelCommand? _buttonMakersClickedCommand;

		public ViewModelCommand ButtonMakersClickedCommand
		{
			get
			{
				if (_buttonMakersClickedCommand == null)
				{
					_buttonMakersClickedCommand = new ViewModelCommand(ButtonMastersClicked);
				}
				return _buttonMakersClickedCommand;
			}
		}

		public void ButtonMastersClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();

				// ViewModel 経由で楽曲情報データベースマスター一覧ウィンドウを開く
				using ViewMakersWindowViewModel viewMastersWindowViewModel = new(musicInfoContextDefault, musicInfoContextDefault.Makers, CreateMasterColumns<TMaker>());
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(viewMastersWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_MASTERS_WINDOW));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "制作会社一覧ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region シリーズ一覧ボタンの制御

		private ViewModelCommand? _buttonTieUpGroupsClickedCommand;

		public ViewModelCommand ButtonTieUpGroupsClickedCommand
		{
			get
			{
				if (_buttonTieUpGroupsClickedCommand == null)
				{
					_buttonTieUpGroupsClickedCommand = new ViewModelCommand(ButtonTieUpGroupsClicked);
				}
				return _buttonTieUpGroupsClickedCommand;
			}
		}

		public void ButtonTieUpGroupsClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();

				// ViewModel 経由で楽曲情報データベースマスター一覧ウィンドウを開く
				using ViewTieUpGroupsWindowViewModel viewTieUpGroupsWindowViewModel = new(musicInfoContextDefault, musicInfoContextDefault.TieUpGroups, CreateMasterColumns<TTieUpGroup>());
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(viewTieUpGroupsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_MASTERS_WINDOW));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "シリーズ一覧ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 楽曲一覧ボタンの制御
		private ViewModelCommand? _buttonSongsClickedCommand;

		public ViewModelCommand ButtonSongsClickedCommand
		{
			get
			{
				if (_buttonSongsClickedCommand == null)
				{
					_buttonSongsClickedCommand = new ViewModelCommand(ButtonSongsClicked);
				}
				return _buttonSongsClickedCommand;
			}
		}

		public void ButtonSongsClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();

				// ViewModel 経由で楽曲情報データベースマスター一覧ウィンドウを開く
				using ViewSongsWindowViewModel viewSongsWindowViewModel = new(musicInfoContextDefault, musicInfoContextDefault.Songs, CreateSongColumns());
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(viewSongsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_MASTERS_WINDOW));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "楽曲一覧ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 人物一覧ボタンの制御
		private ViewModelCommand? _buttonPeopleClickedCommand;

		public ViewModelCommand ButtonPeopleClickedCommand
		{
			get
			{
				if (_buttonPeopleClickedCommand == null)
				{
					_buttonPeopleClickedCommand = new ViewModelCommand(ButtonPeopleClicked);
				}
				return _buttonPeopleClickedCommand;
			}
		}

		public void ButtonPeopleClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();

				// ViewModel 経由で楽曲情報データベースマスター一覧ウィンドウを開く
				using ViewPeopleWindowViewModel viewPeopleWindowViewModel = new(musicInfoContextDefault, musicInfoContextDefault.People, CreateMasterColumns<TPerson>());
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(viewPeopleWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_MASTERS_WINDOW));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "人物一覧ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region タグ一覧ボタンの制御
		private ViewModelCommand? _buttonTagsClickedCommand;

		public ViewModelCommand ButtonTagsClickedCommand
		{
			get
			{
				if (_buttonTagsClickedCommand == null)
				{
					_buttonTagsClickedCommand = new ViewModelCommand(ButtonTagsClicked);
				}
				return _buttonTagsClickedCommand;
			}
		}

		public void ButtonTagsClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContextDefault = new();

				// ViewModel 経由で楽曲情報データベースマスター一覧ウィンドウを開く
				using ViewTagsWindowViewModel viewTagsWindowViewModel = new(musicInfoContextDefault, musicInfoContextDefault.Tags, CreateMasterColumns<TTag>());
				_tabControlWindowViewModel.Messenger.Raise(new TransitionMessage(viewTagsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_VIEW_MASTERS_WINDOW));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タグ一覧ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 楽曲情報データベースマスター一覧ウィンドウの列を作成
		// --------------------------------------------------------------------
		private static ObservableCollection<DataGridColumn> CreateCategorizableColumns<T>() where T : class, IRcCategorizable
		{
			ObservableCollection<DataGridColumn> columns = CreateMasterColumns<T>();
			DataGridTextColumn column;

			// カテゴリー
			column = new();
			column.Binding = new Binding(nameof(IRcCategorizable.DisplayCategoryName));
			column.Header = "カテゴリー";
			columns.Add(column);

			// リリース日
			column = new();
			column.Binding = new Binding(nameof(IRcCategorizable.DisplayReleaseDate));
			column.Header = "リリース日";
			columns.Add(column);

			return columns;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースマスター一覧ウィンドウの列を作成
		// --------------------------------------------------------------------
		private static ObservableCollection<DataGridColumn> CreateMasterColumns<T>() where T : class, IRcMaster
		{
			ObservableCollection<DataGridColumn> columns = new();
			DataGridTextColumn column;

			// 名
			column = new();
			column.Binding = new Binding(nameof(IRcMaster.Name));
			column.Header = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "名";
			columns.Add(column);

			// フリガナ
			column = new();
			column.Binding = new Binding(nameof(IRcMaster.Ruby));
			column.Header = "フリガナ";
			columns.Add(column);

			// 検索ワード
			column = new();
			column.Binding = new Binding(nameof(IRcMaster.Keyword));
			column.Header = "検索ワード";
			columns.Add(column);

			return columns;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースマスター一覧ウィンドウの列を作成
		// --------------------------------------------------------------------
		private static ObservableCollection<DataGridColumn> CreateSongColumns()
		{
			ObservableCollection<DataGridColumn> columns = CreateCategorizableColumns<TSong>();
			DataGridTextColumn column;

			// タイアップ名
			column = new();
			column.Binding = new Binding(nameof(TSong.DisplayTieUpName));
			column.Header = "タイアップ名";
			columns.Add(column);

			// 摘要
			column = new();
			column.Binding = new Binding(nameof(TSong.OpEd));
			column.Header = "摘要";
			columns.Add(column);

			// 歌手名
			column = new();
			column.Binding = new Binding(nameof(TSong.DisplayArtistNames));
			column.Header = "歌手名";
			columns.Add(column);

			// 作詞者名
			column = new();
			column.Binding = new Binding(nameof(TSong.DisplayLyristNames));
			column.Header = "作詞者名";
			columns.Add(column);

			// 作曲者名
			column = new();
			column.Binding = new Binding(nameof(TSong.DisplayComposerNames));
			column.Header = "作曲者名";
			columns.Add(column);

			// 編曲者名
			column = new();
			column.Binding = new Binding(nameof(TSong.DisplayArrangerNames));
			column.Header = "編曲者名";
			columns.Add(column);

			// タグ名
			column = new();
			column.Binding = new Binding(nameof(TSong.DisplayTagNames));
			column.Header = "タグ名";
			columns.Add(column);

			return columns;
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベースマスター一覧ウィンドウの列を作成
		// --------------------------------------------------------------------
		private static ObservableCollection<DataGridColumn> CreateTieUpColumns()
		{
			ObservableCollection<DataGridColumn> columns = CreateCategorizableColumns<TTieUp>();
			DataGridTextColumn column;

			// 制作会社名
			column = new();
			column.Binding = new Binding(nameof(TTieUp.DisplayMakerName));
			column.Header = "制作会社名";
			columns.Add(column);

			// 年齢制限
			column = new();
			column.Binding = new Binding(nameof(TTieUp.DisplayAgeLimit));
			column.Header = "年齢制限";
			columns.Add(column);

			// シリーズ
			column = new();
			column.Binding = new Binding(nameof(TTieUp.DisplayTieUpGroupNames));
			column.Header = "シリーズ";
			columns.Add(column);

			return columns;
		}
	}
}
