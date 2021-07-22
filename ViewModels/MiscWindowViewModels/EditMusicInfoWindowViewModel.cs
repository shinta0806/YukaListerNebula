// ============================================================================
// 
// 楽曲情報等編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditMasterWindowViewModels;
using YukaLister.ViewModels.SearchMasterWindowViewModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	public class EditMusicInfoWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public EditMusicInfoWindowViewModel(String filePath)
		{
			// 引数
			_filePath = filePath;

			// 自動設定
			FileName = Path.GetFileName(_filePath);
			FolderName = Path.GetDirectoryName(_filePath) + "\\";
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public EditMusicInfoWindowViewModel()
		{
			_filePath = String.Empty;
			FileName = String.Empty;
			FolderName = String.Empty;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ファイル名（パス無し）
		public String FileName { get; }

		// フォルダー名
		public String FolderName { get; }

		// ファイル名から取得したタイアップ名
		private String? _tieUpNameByFileName;
		public String? TieUpNameByFileName
		{
			get => _tieUpNameByFileName;
			set => RaisePropertyChangedIfSet(ref _tieUpNameByFileName, value);
		}

		// ファイル名から取得したタイアップ名が楽曲情報データベースに登録されているか
		public Boolean IsTieUpNameRegistered
		{
			get
			{
				// コンポーネントや楽曲情報データベースによるエイリアスを指定しない状態での情報を使うため、YlCommon を使う
				Dictionary<String, String?> dicByFilePure = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(_filePath);
				if (dicByFilePure[YlConstants.RULE_VAR_PROGRAM] == null)
				{
					return false;
				}
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TTieUp> tieUps);
				musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				return DbCommon.SelectMasterByName(tieUps, dicByFilePure[YlConstants.RULE_VAR_PROGRAM]) != null;
			}
		}

		// ファイル名から取得した楽曲名
		private String? _songNameByFileName;
		public String? SongNameByFileName
		{
			get => _songNameByFileName;
			set => RaisePropertyChangedIfSet(ref _songNameByFileName, value);
		}

		// ファイル名から取得した楽曲名が楽曲情報データベースに登録されているか
		public Boolean IsSongNameRegistered
		{
			get
			{
				// コンポーネントや楽曲情報データベースによるエイリアスを指定しない状態での情報を使うため、YlCommon を使う
				Dictionary<String, String?> dicByFilePure = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(_filePath);
				if (dicByFilePure[YlConstants.RULE_VAR_TITLE] == null)
				{
					return false;
				}
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TSong> songs);
				musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				return DbCommon.SelectMasterByName(songs, dicByFilePure[YlConstants.RULE_VAR_TITLE]) != null;
			}
		}

		// タイアップ名を揃える
		private Boolean _useTieUpAlias;
		public Boolean UseTieUpAlias
		{
			get => _useTieUpAlias;
			set
			{
				try
				{
					if (String.IsNullOrEmpty(TieUpNameByFileName))
					{
						throw new Exception("ファイル名・フォルダー固定値からタイアップ名を取得できなかったため、タイアップ名を揃えられません。");
					}
					if (value && IsTieUpNameRegistered)
					{
						throw new Exception("ファイル名・フォルダー固定値から取得したタイアップ名はデータベースに登録済みのため、タイアップ名を揃えるのは不要です。");
					}
					if (RaisePropertyChangedIfSet(ref _useTieUpAlias, value))
					{
						ButtonSearchTieUpOriginClickedCommand.RaiseCanExecuteChanged();
						UpdateListItems();
						if (!_useTieUpAlias)
						{
							TieUpOrigin = null;
						}
					}
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイアップ名を揃えるクリック時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			}
		}

		// 元のタイアップ名（揃えた後のタイアップ名）
		private String? _tieUpOrigin;
		public String? TieUpOrigin
		{
			get => _tieUpOrigin;
			set => RaisePropertyChangedIfSet(ref _tieUpOrigin, value);
		}

		// 楽曲名を揃える
		private Boolean _useSongAlias;
		public Boolean UseSongAlias
		{
			get => _useSongAlias;
			set
			{
				try
				{
					if (String.IsNullOrEmpty(SongNameByFileName))
					{
						throw new Exception("ファイル名・フォルダー固定値から楽曲名を取得できなかったため、楽曲名を揃えられません。");
					}
					if (value && IsSongNameRegistered)
					{
						throw new Exception("ファイル名・フォルダー固定値から取得した楽曲名はデータベースに登録済みのため、楽曲名を揃えるのは不要です。");
					}
					if (RaisePropertyChangedIfSet(ref _useSongAlias, value))
					{
						ButtonSearchSongOriginClickedCommand.RaiseCanExecuteChanged();
						UpdateListItems();
						if (!_useSongAlias)
						{
							SongOrigin = null;
						}
					}
				}
				catch (Exception excep)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲名を揃えるクリック時エラー：\n" + excep.Message);
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			}
		}

		// 元の楽曲名（揃えた後の楽曲名）
		private String? _songOrigin;
		public String? SongOrigin
		{
			get => _songOrigin;
			set => RaisePropertyChangedIfSet(ref _songOrigin, value);
		}

		// リスト出力時のカテゴリー名
		private String? _listCategoryName;
		public String? ListCategoryName
		{
			get => _listCategoryName;
			set => RaisePropertyChangedIfSet(ref _listCategoryName, value);
		}

		// リスト出力時のタイアップ名
		private String? _listTieUpName;
		public String? ListTieUpName
		{
			get => _listTieUpName;
			set => RaisePropertyChangedIfSet(ref _listTieUpName, value);
		}

		// リスト出力時の楽曲名
		private String? _listSongName;
		public String? ListSongName
		{
			get => _listSongName;
			set => RaisePropertyChangedIfSet(ref _listSongName, value);
		}

		// リスト出力時の歌手名
		private String? _listArtistName;
		public String? ListArtistName
		{
			get => _listArtistName;
			set => RaisePropertyChangedIfSet(ref _listArtistName, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region タイアップ名検索ボタンの制御
		private ViewModelCommand? _buttonSearchTieUpOriginClickedCommand;

		public ViewModelCommand ButtonSearchTieUpOriginClickedCommand
		{
			get
			{
				if (_buttonSearchTieUpOriginClickedCommand == null)
				{
					_buttonSearchTieUpOriginClickedCommand = new ViewModelCommand(ButtonSearchTieUpOriginClicked, CanButtonSearchTieUpOriginClicked);
				}
				return _buttonSearchTieUpOriginClickedCommand;
			}
		}

		public Boolean CanButtonSearchTieUpOriginClicked()
		{
			return UseTieUpAlias;
		}

		public void ButtonSearchTieUpOriginClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TTieUp> tieUps);
				using SearchMasterWindowViewModel<TTieUp> searchMasterWindowViewModel = new(tieUps, "タイアップ名の正式名称");
				searchMasterWindowViewModel.SelectedKeyword = TieUpOrigin ?? TieUpNameByFileName;
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				_isTieUpSearched = true;
				TieUpOrigin = searchMasterWindowViewModel.OkSelectedMaster?.Name ?? TieUpOrigin;
				UpdateListItems();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイアップ名検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region タイアップ詳細編集ボタンの制御
		private ViewModelCommand? _buttonEditTieUpClickedCommand;

		public ViewModelCommand ButtonEditTieUpClickedCommand
		{
			get
			{
				if (_buttonEditTieUpClickedCommand == null)
				{
					_buttonEditTieUpClickedCommand = new ViewModelCommand(ButtonEditTieUpClicked);
				}
				return _buttonEditTieUpClickedCommand;
			}
		}

		public void ButtonEditTieUpClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(TieUpNameByFileName))
				{
					throw new Exception("ファイル名・フォルダー固定値からタイアップ名を取得できなかったため、詳細編集できません。");
				}

				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TTieUp> tieUps);
				MusicInfoContextDefault.GetDbSet(musicInfoContext, out DbSet<TCategory> categories);

				using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds,
						out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
						out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TTieUpGroupSequence> tieUpGroupSequences,
						out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences);
				using TFoundSetterAliasSpecify foundSetterAliasSpecify = new(listContextInMemory, people, artistSequences, composerSequences, tieUpGroups, tieUpGroupSequences, tags, tagSequences,
						UseTieUpAlias ? TieUpOrigin : null, UseSongAlias ? SongOrigin : null);
				Dictionary<String, String?> dicByFile = DicByFile(foundSetterAliasSpecify);

				// タイアップ名が未登録でかつ未検索は検索を促す
				if (DbCommon.SelectMasterByName(tieUps, dicByFile[YlConstants.RULE_VAR_PROGRAM]) == null && String.IsNullOrEmpty(TieUpOrigin))
				{
					if (!_isTieUpSearched)
					{
						throw new Exception("タイアップの正式名称が選択されていないため新規タイアップ情報作成となりますが、その前に一度、目的のタイアップが未登録かどうか検索して下さい。");
					}

					if (MessageBox.Show("タイアップの正式名称が選択されていません。\n新規にタイアップ情報を作成しますか？\n"
							+ "（目的のタイアップが未登録の場合（検索してもヒットしない場合）に限り、新規作成を行って下さい）", "確認",
							MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
					{
						return;
					}
				}

				// 編集対象レコードを用意
				List<TTieUp> sameNameTieUps = DbCommon.MastersForEdit(tieUps, dicByFile[YlConstants.RULE_VAR_PROGRAM]);

				// 新規作成レコードの情報を補完
				MusicInfoContextDefault.GetDbSet(musicInfoContext, out DbSet<TSong> songs);
				TSong? song = DbCommon.SelectMasterByName(songs, dicByFile[YlConstants.RULE_VAR_TITLE]);
				TCategory? category = DbCommon.SelectMasterByName(categories, dicByFile[YlConstants.RULE_VAR_CATEGORY]);
				sameNameTieUps[0].CategoryId = category?.Id;
				sameNameTieUps[0].AgeLimit = Common.StringToInt32(dicByFile[YlConstants.RULE_VAR_AGE_LIMIT]);

				// ウィンドウの準備
				using EditTieUpWindowViewModel editTieUpWindowViewModel = new(musicInfoContext, tieUps);
				editTieUpWindowViewModel.SetMasters(sameNameTieUps);

				// デフォルト ID の指定
				if (sameNameTieUps.Count == 1)
				{
					// 新規作成のみの場合は指定しない
				}
				else
				{
					List<TSong> songsByFoundSetter = foundSetterAliasSpecify.FindSongsByMusicInfoDatabase(dicByFile);
					if (songsByFoundSetter.Count == 0)
					{
						// 楽曲が見当たらない場合はタイアップのみで判定する
						if (sameNameTieUps.Count == 2)
						{
							// 新規以外が 1 つならそれをデフォルトにする
							editTieUpWindowViewModel.DefaultMasterId = sameNameTieUps[1].Id;
						}
						else
						{
							// 新規以外が複数ある場合は新規をデフォルトにする
							editTieUpWindowViewModel.DefaultMasterId = sameNameTieUps[0].Id;
						}
					}
					else if (songsByFoundSetter.Count == 1)
					{
						// 楽曲を 1 つに絞り込めた場合はそれに紐付くタイアップをデフォルトにする
						editTieUpWindowViewModel.DefaultMasterId = songsByFoundSetter[0].TieUpId;
					}
					else
					{
						// 1 つに絞り込めなかった場合は新規をデフォルトにする
						editTieUpWindowViewModel.DefaultMasterId = sameNameTieUps[0].Id;
					}
				}

				Messenger.Raise(new TransitionMessage(editTieUpWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_TIE_UP_WINDOW));

				// 後処理
				if (editTieUpWindowViewModel.OkSelectedMaster != null)
				{
					Dictionary<String, String?> dicByFilePure = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(_filePath);
					if (String.IsNullOrEmpty(dicByFilePure[YlConstants.RULE_VAR_PROGRAM]) || editTieUpWindowViewModel.OkSelectedMaster.Name == dicByFilePure[YlConstants.RULE_VAR_PROGRAM])
					{
						UseTieUpAlias = false;
					}
					else
					{
						UseTieUpAlias = true;
						TieUpOrigin = editTieUpWindowViewModel.OkSelectedMaster.Name;
					}
				}

				// タイアップが削除された場合もあるので常に更新する
				RaisePropertyChanged(nameof(IsTieUpNameRegistered));
				UpdateListItems();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイアップ詳細編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 楽曲名検索ボタンの制御
		private ViewModelCommand? _buttonSearchSongOriginClickedCommand;

		public ViewModelCommand ButtonSearchSongOriginClickedCommand
		{
			get
			{
				if (_buttonSearchSongOriginClickedCommand == null)
				{
					_buttonSearchSongOriginClickedCommand = new ViewModelCommand(ButtonSearchSongOriginClicked, CanButtonSearchSongOriginClicked);
				}
				return _buttonSearchSongOriginClickedCommand;
			}
		}

		public Boolean CanButtonSearchSongOriginClicked()
		{
			return UseSongAlias;
		}

		public void ButtonSearchSongOriginClicked()
		{
			try
			{
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TSong> songs);
				using SearchMasterWindowViewModel<TSong> searchMasterWindowViewModel = new(songs, "楽曲名の正式名称");
				searchMasterWindowViewModel.SelectedKeyword = SongOrigin ?? SongNameByFileName;
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				_isSongSearched = true;
				SongOrigin = searchMasterWindowViewModel.OkSelectedMaster?.Name ?? SongOrigin;
				UpdateListItems();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲名検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 楽曲詳細編集ボタンの制御
		private ViewModelCommand? _buttonEditSongClickedCommand;

		public ViewModelCommand ButtonEditSongClickedCommand
		{
			get
			{
				if (_buttonEditSongClickedCommand == null)
				{
					_buttonEditSongClickedCommand = new ViewModelCommand(ButtonEditSongClicked);
				}
				return _buttonEditSongClickedCommand;
			}
		}

		public void ButtonEditSongClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(SongNameByFileName))
				{
					throw new Exception("ファイル名・フォルダー固定値から楽曲名を取得できなかったため、詳細編集できません。");
				}

				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TSong> songs);

				using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds,
						out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
						out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TTieUpGroupSequence> tieUpGroupSequences,
						out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences);
				using TFoundSetterAliasSpecify foundSetterAliasSpecify = new(listContextInMemory, people, artistSequences, composerSequences, tieUpGroups, tieUpGroupSequences, tags, tagSequences,
						UseTieUpAlias ? TieUpOrigin : null, UseSongAlias ? SongOrigin : null);
				Dictionary<String, String?> dicByFile = DicByFile(foundSetterAliasSpecify);

				// 楽曲名が未登録でかつ未検索は検索を促す
				if (DbCommon.SelectMasterByName(songs, dicByFile[YlConstants.RULE_VAR_TITLE]) == null && String.IsNullOrEmpty(SongOrigin))
				{
					if (!_isSongSearched)
					{
						throw new Exception("楽曲の正式名称が選択されていないため新規楽曲情報作成となりますが、その前に一度、目的の楽曲が未登録かどうか検索して下さい。");
					}

					if (MessageBox.Show("楽曲の正式名称が選択されていません。\n新規に楽曲情報を作成しますか？\n"
							+ "（目的の楽曲が未登録の場合（検索してもヒットしない場合）に限り、新規作成を行って下さい）", "確認",
							MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
					{
						return;
					}
				}

				// 情報準備
				List<TSong> sameNameSongs = DbCommon.SelectMastersByName(songs, dicByFile[YlConstants.RULE_VAR_TITLE]);
				MusicInfoContextDefault.GetDbSet(musicInfoContext, out DbSet<TTieUp> tieUps);
				TTieUp? tieUp = DbCommon.SelectMasterByName(tieUps, dicByFile[YlConstants.RULE_VAR_PROGRAM]);
				MusicInfoContextDefault.GetDbSet(musicInfoContext, out DbSet<TCategory> categories);
				TCategory? category = null;
				if (tieUp == null)
				{
					category = DbCommon.SelectMasterByName(categories, dicByFile[YlConstants.RULE_VAR_CATEGORY]);
					switch (category?.Name)
					{
						// タイアップの無いカテゴリーの場合は、ファイル名から取得したカテゴリーを採用する
						case YlConstants.CATEGORY_NAME_VOCALOID:
						case YlConstants.CATEGORY_NAME_GENERAL:
							break;

						// タイアップのあるカテゴリーの場合は、タイアップを選択できるよう、カテゴリーは付与しない
						default:
							category = null;
							break;
					}
				}

				// 新規作成用の追加
				TSong newSong = new()
				{
					// IRcBase
					Id = String.Empty,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IRcMaster
					Name = dicByFile[YlConstants.RULE_VAR_TITLE],
					Ruby = dicByFile[YlConstants.RULE_VAR_TITLE_RUBY],
					Keyword = null,

					// TSong
					ReleaseDate = YlConstants.INVALID_MJD,
					TieUpId = tieUp?.Id,
					CategoryId = category?.Id,
					OpEd = dicByFile[YlConstants.RULE_VAR_OP_ED],
				};
				sameNameSongs.Insert(0, newSong);

				// ウィンドウの準備
				using EditSongWindowViewModel editSongWindowViewModel = new(musicInfoContext, songs);
				editSongWindowViewModel.SetMasters(sameNameSongs);

				// デフォルト ID の指定
				if (sameNameSongs.Count == 1)
				{
					// 新規作成のみの場合は指定しない
				}
				else
				{
					List<TSong> songsByFoundSetter = foundSetterAliasSpecify.FindSongsByMusicInfoDatabase(dicByFile);
					if (songsByFoundSetter.Count == 1)
					{
						// 1 つに絞り込めた場合はそれをデフォルトにする
						editSongWindowViewModel.DefaultMasterId = songsByFoundSetter[0].Id;
					}
					else
					{
						// 1 つに絞り込めなかった場合は新規をデフォルトにする
						editSongWindowViewModel.DefaultMasterId = sameNameSongs[0].Id;
					}
				}

				// ウィンドウを開く
				Messenger.Raise(new TransitionMessage(editSongWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_SONG_WINDOW));

				// 後処理
				if (editSongWindowViewModel.OkSelectedMaster != null)
				{
					Dictionary<String, String?> dicByFilePure = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(_filePath);
					if (String.IsNullOrEmpty(dicByFilePure[YlConstants.RULE_VAR_TITLE]) || editSongWindowViewModel.OkSelectedMaster.Name == dicByFilePure[YlConstants.RULE_VAR_TITLE])
					{
						UseSongAlias = false;
					}
					else
					{
						UseSongAlias = true;
						SongOrigin = editSongWindowViewModel.OkSelectedMaster.Name;
					}
				}

				// 楽曲が削除された場合もあるので常に更新する
				RaisePropertyChanged(nameof(IsSongNameRegistered));
				UpdateListItems();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲詳細編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOkClickedCommand;

		public ViewModelCommand ButtonOkClickedCommand
		{
			get
			{
				if (_buttonOkClickedCommand == null)
				{
					_buttonOkClickedCommand = new ViewModelCommand(ButtonOkClicked);
				}
				return _buttonOkClickedCommand;
			}
		}

		public async void ButtonOkClicked()
		{
			try
			{
				(String? songOriginalId, String? tieUpOriginalId) = CheckInput();
				await Save(songOriginalId, tieUpOriginalId);
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (OperationCanceledException excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "OK ボタンクリック時中止");
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
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
				// タイトルバー
				Title = "名称の編集";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// コンポーネントや楽曲情報データベースによるエイリアスを指定しない状態での情報を使うため、YlCommon を使う
				Dictionary<String, String?> dicByFilePure = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(_filePath);
				TieUpNameByFileName = dicByFilePure[YlConstants.RULE_VAR_PROGRAM];
				SongNameByFileName = dicByFilePure[YlConstants.RULE_VAR_TITLE];
				RaisePropertyChanged(nameof(IsTieUpNameRegistered));
				RaisePropertyChanged(nameof(IsSongNameRegistered));

				// コンポーネントによるエイリアスを指定しない状態での情報を使うため、TFoundSetterAliasSpecify ではなく TFoundSetter を使う
				using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds,
						out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
						out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TTieUpGroupSequence> tieUpGroupSequences,
						out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences);
				using TFoundSetter foundSetter = new(listContextInMemory, people, artistSequences, composerSequences, tieUpGroups, tieUpGroupSequences, tags, tagSequences);
				ApplyTieUpAlias(foundSetter, dicByFilePure);
				ApplySongAlias(foundSetter, dicByFilePure);

				// リスト表示予定項目
				UpdateListItems();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲情報等編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// パス
		private readonly String _filePath;

		// タイアップを検索したかどうか
		private Boolean _isTieUpSearched;

		// 楽曲を検索したかどうか
		private Boolean _isSongSearched;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 適用可能な楽曲名の別名を検索してコンポーネントに反映
		// --------------------------------------------------------------------
		private void ApplySongAlias(TFoundSetter foundSetter, Dictionary<String, String?> dicByFilePure)
		{
			if (String.IsNullOrEmpty(dicByFilePure[YlConstants.RULE_VAR_TITLE]))
			{
				return;
			}

			String? songOrigin = foundSetter.SongOrigin(dicByFilePure[YlConstants.RULE_VAR_TITLE]);
			if (songOrigin != dicByFilePure[YlConstants.RULE_VAR_TITLE])
			{
				TSong? song = DbCommon.SelectMasterByName(foundSetter.Songs, songOrigin);
				if (song != null)
				{
					// 有効なエイリアスが設定されている
					UseSongAlias = true;
					SongOrigin = song.Name;
					return;
				}

			}

			if (DbCommon.SelectMasterByName(foundSetter.Songs, dicByFilePure[YlConstants.RULE_VAR_TITLE]) == null)
			{
				// ファイル名から取得された情報が登録されていない
				UseSongAlias = true;
				SongOrigin = null;
			}
		}

		// --------------------------------------------------------------------
		// 適用可能なタイアップ名の別名を検索してコンポーネントに反映
		// --------------------------------------------------------------------
		private void ApplyTieUpAlias(TFoundSetter foundSetter, Dictionary<String, String?> dicByFilePure)
		{
			if (String.IsNullOrEmpty(dicByFilePure[YlConstants.RULE_VAR_PROGRAM]))
			{
				return;
			}

			String? programOrigin = foundSetter.ProgramOrigin(dicByFilePure[YlConstants.RULE_VAR_PROGRAM]);
			if (programOrigin != dicByFilePure[YlConstants.RULE_VAR_PROGRAM])
			{
				TTieUp? tieUp = DbCommon.SelectMasterByName(foundSetter.TieUps, programOrigin);
				if (tieUp != null)
				{
					// 有効なエイリアスが設定されている
					UseTieUpAlias = true;
					TieUpOrigin = tieUp.Name;
					return;
				}

			}

			if (DbCommon.SelectMasterByName(foundSetter.TieUps, dicByFilePure[YlConstants.RULE_VAR_PROGRAM]) == null)
			{
				// ファイル名から取得された情報が登録されていない
				UseTieUpAlias = true;
				TieUpOrigin = null;
			}
		}

		// --------------------------------------------------------------------
		// 入力値の確認（別名に関するもののみ）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private (String? songOriginalId, String? tieUpOriginalId) CheckInput()
		{
			String? songOriginalId = null;
			String? tieUpOriginalId = null;

			// 楽曲別名
			if (UseSongAlias)
			{
				if (String.IsNullOrEmpty(SongOrigin))
				{
					throw new Exception("楽曲名の正式名称を検索して指定して下さい。");
				}
				if (SongOrigin == SongNameByFileName)
				{
					throw new Exception("ファイル名・フォルダー固定値から取得した楽曲名と正式名称が同じです。\n"
							+ "楽曲名を揃えるのが不要の場合は、「楽曲名を揃える」のチェックを外して下さい。");
				}
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TSong> songs);
				musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				TSong? song = DbCommon.SelectMasterByName(songs, SongOrigin);
				if (song == null)
				{
					throw new Exception("楽曲名の正式名称が正しく検索されていません。");
				}
				songOriginalId = song.Id;
			}

			// タイアップ別名
			if (UseTieUpAlias)
			{
				if (String.IsNullOrEmpty(TieUpOrigin))
				{
					throw new Exception("タイアップ名の正式名称を検索して指定して下さい。");
				}
				if (TieUpOrigin == TieUpNameByFileName)
				{
					throw new Exception("ファイル名・フォルダー固定値から取得したタイアップ名と正式名称が同じです。\n"
							+ "タイアップ名を揃えるのが不要の場合は、「タイアップ名を揃える」のチェックを外して下さい。");
				}
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TTieUp> tieUps);
				musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				TTieUp? tieUp = DbCommon.SelectMasterByName(tieUps, TieUpOrigin);
				if (tieUp == null)
				{
					throw new Exception("タイアップ名の正式名称が正しく検索されていません。");
				}
				tieUpOriginalId = tieUp.Id;
			}

			return (songOriginalId, tieUpOriginalId);
		}

		// --------------------------------------------------------------------
		// foundSetter から DicByFile を取得
		// --------------------------------------------------------------------
		private Dictionary<String, String?> DicByFile(TFoundSetter foundSetter)
		{
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(Path.GetDirectoryName(_filePath));
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);
			return foundSetter.MatchFileNameRulesAndFolderRuleForSearch(Path.GetFileNameWithoutExtension(_filePath), folderSettingsInMemory);
		}

		// --------------------------------------------------------------------
		// 別名を保存
		// --------------------------------------------------------------------
		private async Task Save(String? songOriginalId, String? tieUpOriginalId)
		{
			// コンポーネントや楽曲情報データベースによるエイリアスを指定しない状態での情報を使うため、YlCommon を使う
			Dictionary<String, String?> dicByFilePure = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(_filePath);

			// 楽曲別名
			if (!String.IsNullOrEmpty(dicByFilePure[YlConstants.RULE_VAR_TITLE]))
			{
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TSongAlias> songAliases);
				if (UseSongAlias && !String.IsNullOrEmpty(songOriginalId))
				{
					TSongAlias? existSongAlias = DbCommon.SelectAliasByAlias(songAliases, dicByFilePure[YlConstants.RULE_VAR_TITLE], true);
					TSongAlias newSongAlias = new()
					{
						// TBase
						Id = String.Empty,
						Import = false,
						Invalid = false,
						UpdateTime = YlConstants.INVALID_MJD,
						Dirty = true,

						// TAlias
						Alias = dicByFilePure[YlConstants.RULE_VAR_TITLE]!,
						OriginalId = songOriginalId,
					};

					if (existSongAlias == null)
					{
						// 新規登録
						await YlCommon.InputIdPrefixIfNeededWithInvoke(this);
						newSongAlias.Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareLastId(songAliases);
						songAliases.Add(newSongAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲別名テーブル新規登録：" + newSongAlias.Id + " / " + newSongAlias.Alias);
					}
					else if (DbCommon.IsRcAliasUpdated(existSongAlias, newSongAlias))
					{
						// 更新（既存のレコードが無効化されている場合は有効化も行う）
						newSongAlias.Id = existSongAlias.Id;
						newSongAlias.UpdateTime = existSongAlias.UpdateTime;
						Common.ShallowCopyFields(newSongAlias, existSongAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲別名テーブル更新：" + newSongAlias.Id + " / " + newSongAlias.Alias);
					}
				}
				else
				{
					TSongAlias? existSongAlias = DbCommon.SelectAliasByAlias(songAliases, dicByFilePure[YlConstants.RULE_VAR_TITLE], false);
					if (existSongAlias != null)
					{
						// 無効化
						existSongAlias.Invalid = true;
						existSongAlias.Dirty = true;
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲別名テーブル無効化：" + existSongAlias.Id + " / " + existSongAlias.Alias);
					}
				}
				musicInfoContext.SaveChanges();
			}

			// タイアップ別名
			if (!String.IsNullOrEmpty(dicByFilePure[YlConstants.RULE_VAR_PROGRAM]))
			{
				using MusicInfoContextDefault musicInfoContext = MusicInfoContextDefault.CreateContext(out DbSet<TTieUpAlias> tieUpAliases);
				if (UseTieUpAlias && !String.IsNullOrEmpty(tieUpOriginalId))
				{
					TTieUpAlias? existTieUpAlias = DbCommon.SelectAliasByAlias(tieUpAliases, dicByFilePure[YlConstants.RULE_VAR_PROGRAM], true);
					TTieUpAlias newTieUpAlias = new()
					{
						// TBase
						Id = String.Empty,
						Import = false,
						Invalid = false,
						UpdateTime = YlConstants.INVALID_MJD,
						Dirty = true,

						// TAlias
						Alias = dicByFilePure[YlConstants.RULE_VAR_PROGRAM]!,
						OriginalId = tieUpOriginalId,
					};

					if (existTieUpAlias == null)
					{
						// 新規登録
						await YlCommon.InputIdPrefixIfNeededWithInvoke(this);
						newTieUpAlias.Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareLastId(tieUpAliases);
						tieUpAliases.Add(newTieUpAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "タイアップ別名テーブル新規登録：" + newTieUpAlias.Id + " / " + newTieUpAlias.Alias);
					}
					else if (DbCommon.IsRcAliasUpdated(existTieUpAlias, newTieUpAlias))
					{
						// 更新（既存のレコードが無効化されている場合は有効化も行う）
						newTieUpAlias.Id = existTieUpAlias.Id;
						newTieUpAlias.UpdateTime = existTieUpAlias.UpdateTime;
						Common.ShallowCopyFields(newTieUpAlias, existTieUpAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "タイアップ別名テーブル更新：" + newTieUpAlias.Id + " / " + newTieUpAlias.Alias);
					}
				}
				else
				{
					TTieUpAlias? existTieUpAlias = DbCommon.SelectAliasByAlias(tieUpAliases, dicByFilePure[YlConstants.RULE_VAR_PROGRAM], false);
					if (existTieUpAlias != null)
					{
						// 無効化
						existTieUpAlias.Invalid = true;
						existTieUpAlias.Dirty = true;
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "タイアップ別名テーブル無効化：" + existTieUpAlias.Id + " / " + existTieUpAlias.Alias);
					}
				}
				musicInfoContext.SaveChanges();
			}
		}

		// --------------------------------------------------------------------
		// リストに表示される項目を更新
		// --------------------------------------------------------------------
		private void UpdateListItems()
		{
			try
			{
				FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(Path.GetDirectoryName(_filePath));
				FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);
				TFound found = new();
				found.Path = _filePath;

				using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds,
						out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
						out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TTieUpGroupSequence> tieUpGroupSequences,
						out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences);
				using TFoundSetterAliasSpecify foundSetterAliasSpecify = new(listContextInMemory, people, artistSequences, composerSequences, tieUpGroups, tieUpGroupSequences, tags, tagSequences,
						UseTieUpAlias ? TieUpOrigin : null, UseSongAlias ? SongOrigin : null);
				foundSetterAliasSpecify.SetTFoundValues(found, folderSettingsInMemory);

				ListCategoryName = found.Category;
				ListTieUpName = found.TieUpName;
				ListSongName = found.SongName;
				ListArtistName = found.ArtistName;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト表示予定項目更新時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
	}
}
