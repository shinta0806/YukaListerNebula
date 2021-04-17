﻿// ============================================================================
// 
// 楽曲情報等編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging.Windows;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class EditMusicInfoWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public EditMusicInfoWindowViewModel(String filePath, Dictionary<String, String?> dicByFile)
		{
			// 引数
			_filePath = filePath;
			_dicByFile = dicByFile;

			// 自動設定
			FileName = Path.GetFileName(_filePath);
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（TransitionMessage で使われる）
		// --------------------------------------------------------------------
		public EditMusicInfoWindowViewModel()
		{
			_filePath = String.Empty;
			_dicByFile = YlCommon.DicByFile(String.Empty);
			FileName = String.Empty;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ファイル名（パス無し）
		public String FileName { get; }

		// タイアップ名が登録されているか
		public Boolean IsTieUpNameRegistered
		{
			get
			{
				if (_dicByFile[YlConstants.RULE_VAR_PROGRAM] == null)
				{
					return false;
				}
				using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TTieUp> tieUps);
				return DbCommon.SelectMasterByName(tieUps, _dicByFile[YlConstants.RULE_VAR_PROGRAM]) != null;
			}
		}

		// 楽曲名が登録されているか
		public Boolean IsSongNameRegistered
		{
			get
			{
				if (_dicByFile[YlConstants.RULE_VAR_TITLE] == null)
				{
					return false;
				}
				using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TSong> songs);
				return DbCommon.SelectMasterByName(songs, _dicByFile[YlConstants.RULE_VAR_TITLE]) != null;
			}
		}

		// タイアップ名を揃える
		private Boolean _useTieUpAlias;
		public Boolean UseTieUpAlias
		{
			get => _useTieUpAlias;
			set
			{
				if (value && IsTieUpNameRegistered)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error,
							"ファイル名・フォルダー固定値から取得したタイアップ名はデータベースに登録済みのため、タイアップ名を揃えるのは不要です。");
					return;
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
		}

		// 元のタイアップ名
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
				if (value && IsSongNameRegistered)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error,
							"ファイル名・フォルダー固定値から取得した楽曲名はデータベースに登録済みのため、楽曲名を揃えるのは不要です。");
					return;
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
		}

		// 元の楽曲名
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
#if false
				using (SearchMusicInfoWindowViewModel aSearchMusicInfoWindowViewModel = new SearchMusicInfoWindowViewModel())
				{
					aSearchMusicInfoWindowViewModel.Environment = Environment!;
					aSearchMusicInfoWindowViewModel.ItemName = "タイアップ名の正式名称";
					aSearchMusicInfoWindowViewModel.TableIndex = MusicInfoDbTables.TTieUp;
					aSearchMusicInfoWindowViewModel.SelectedKeyword = TieUpOrigin;
					Messenger.Raise(new TransitionMessage(aSearchMusicInfoWindowViewModel, "OpenSearchMusicInfoWindow"));

					mIsTieUpSearched = true;
					if (!String.IsNullOrEmpty(aSearchMusicInfoWindowViewModel.DecidedName))
					{
						TieUpOrigin = aSearchMusicInfoWindowViewModel.DecidedName;
					}
				}

				UpdateListItems();
#endif
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
#if false
				using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
				{
					// ファイル名から取得したタイアップ名が未登録でかつ未検索は検索を促す
					if (DicByFile != null
							&& YlCommon.SelectMastersByName<TTieUp>(aMusicInfoDbInDisk.Connection, DicByFile[YlConstants.RULE_VAR_PROGRAM]).Count == 0 && String.IsNullOrEmpty(TieUpOrigin))
					{
						if (!mIsTieUpSearched)
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
				}

				// 対象タイアップ名の選択
				String? aTieUpName;
				if (!String.IsNullOrEmpty(TieUpOrigin))
				{
					aTieUpName = TieUpOrigin;
				}
				else
				{
					aTieUpName = DicByFile?[YlConstants.RULE_VAR_PROGRAM];
				}

				// 情報準備
				List<TTieUp> aTieUps;
				List<TCategory> aCategories;
				using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
				{
					aTieUps = YlCommon.SelectMastersByName<TTieUp>(aMusicInfoDbInDisk.Connection, aTieUpName);
					aCategories = YlCommon.SelectMastersByName<TCategory>(aMusicInfoDbInDisk.Connection, DicByFile?[YlConstants.RULE_VAR_CATEGORY]);
				}

				// 新規作成用の追加
				TTieUp aNewTieUp = new TTieUp
				{
					// IRcBase
					Id = null,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IRcMaster
					Name = aTieUpName,
					Ruby = null,
					Keyword = null,

					// TTieUp
					CategoryId = aCategories.Count > 0 ? aCategories[0].Id : null,
					MakerId = null,
					AgeLimit = Common.StringToInt32(DicByFile?[YlConstants.RULE_VAR_AGE_LIMIT]),
					ReleaseDate = YlConstants.INVALID_MJD,
				};
				aTieUps.Insert(0, aNewTieUp);

				using (EditTieUpWindowViewModel aEditTieUpWindowViewModel = new EditTieUpWindowViewModel())
				{
					aEditTieUpWindowViewModel.Environment = Environment;
					aEditTieUpWindowViewModel.SetMasters(aTieUps);
					if (aTieUps.Count > 1)
					{
						aEditTieUpWindowViewModel.DefaultId = aTieUps[1].Id;
					}
					Messenger.Raise(new TransitionMessage(aEditTieUpWindowViewModel, "OpenEditTieUpWindow"));

					if (String.IsNullOrEmpty(aEditTieUpWindowViewModel.OkSelectedId))
					{
						return;
					}

					using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
					{
						TTieUp? aTieUp = YlCommon.SelectBaseById<TTieUp>(aMusicInfoDbInDisk.Connection, aEditTieUpWindowViewModel.OkSelectedId);
						if (aTieUp != null)
						{
							if (String.IsNullOrEmpty(DicByFile?[YlConstants.RULE_VAR_PROGRAM]) || aTieUp.Name == DicByFile?[YlConstants.RULE_VAR_PROGRAM])
							{
								UseTieUpAlias = false;
							}
							else
							{
								UseTieUpAlias = true;
								TieUpOrigin = aTieUp.Name;
							}
						}
						RaisePropertyChanged(nameof(IsTieUpNameRegistered));
					}
					UpdateListItems();
				}
#endif
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
#if false
				using (SearchMusicInfoWindowViewModel aSearchMusicInfoWindowViewModel = new SearchMusicInfoWindowViewModel())
				{
					aSearchMusicInfoWindowViewModel.Environment = Environment!;
					aSearchMusicInfoWindowViewModel.ItemName = "楽曲名の正式名称";
					aSearchMusicInfoWindowViewModel.TableIndex = MusicInfoDbTables.TSong;
					aSearchMusicInfoWindowViewModel.SelectedKeyword = SongOrigin;
					Messenger.Raise(new TransitionMessage(aSearchMusicInfoWindowViewModel, "OpenSearchMusicInfoWindow"));

					mIsSongSearched = true;
					if (!String.IsNullOrEmpty(aSearchMusicInfoWindowViewModel.DecidedName))
					{
						SongOrigin = aSearchMusicInfoWindowViewModel.DecidedName;
					}
				}

				UpdateListItems();
#endif
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
#if false
				using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
				{
					// ファイル名から取得した楽曲名が未登録でかつ未検索は検索を促す
					if (YlCommon.SelectMastersByName<TSong>(aMusicInfoDbInDisk.Connection, DicByFile?[YlConstants.RULE_VAR_TITLE]).Count == 0 && String.IsNullOrEmpty(SongOrigin))
					{
						if (!mIsSongSearched)
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
				}

				// 対象楽曲名の選択
				String? aSongName;
				if (!String.IsNullOrEmpty(SongOrigin))
				{
					aSongName = SongOrigin;
				}
				else
				{
					aSongName = DicByFile?[YlConstants.RULE_VAR_TITLE];
				}

				// タイアップ名の選択（null もありえる）
				String? aTieUpName;
				if (!String.IsNullOrEmpty(TieUpOrigin))
				{
					aTieUpName = TieUpOrigin;
				}
				else
				{
					aTieUpName = DicByFile?[YlConstants.RULE_VAR_PROGRAM];
				}

				// 情報準備
				List<TSong> aSongs;
				List<TTieUp> aTieUps;
				List<TCategory> aCategories;
				using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
				{
					aSongs = YlCommon.SelectMastersByName<TSong>(aMusicInfoDbInDisk.Connection, aSongName);
					aTieUps = YlCommon.SelectMastersByName<TTieUp>(aMusicInfoDbInDisk.Connection, aTieUpName);
					aCategories = YlCommon.SelectMastersByName<TCategory>(aMusicInfoDbInDisk.Connection, DicByFile?[YlConstants.RULE_VAR_CATEGORY]);
				}

				// 新規作成用の追加
				TSong aNewSong = new TSong
				{
					// IRcBase
					Id = null,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IRcMaster
					Name = aSongName,
					Ruby = DicByFile?[YlConstants.RULE_VAR_TITLE_RUBY],
					Keyword = null,

					// TSong
					ReleaseDate = YlConstants.INVALID_MJD,
					TieUpId = aTieUps.Count > 0 ? aTieUps[0].Id : null,
					CategoryId = aTieUps.Count == 0 && aCategories.Count > 0 ? aCategories[0].Id : null,
					OpEd = DicByFile?[YlConstants.RULE_VAR_OP_ED],
				};
				aSongs.Insert(0, aNewSong);

				using (EditSongWindowViewModel aEditSongWindowViewModel = new EditSongWindowViewModel())
				{
					aEditSongWindowViewModel.Environment = Environment;
					aEditSongWindowViewModel.SetMasters(aSongs);

					using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
					{
						// デフォルト ID の指定
						if (aSongs.Count == 1)
						{
							// 新規作成のみの場合は指定しない
						}
						else if (aSongs.Count == 2 && String.IsNullOrEmpty(aTieUpName))
						{
							// 既存楽曲が 1 つのみの場合で、タイアップが指定されていない場合は、既存楽曲のタイアップに関わらずデフォルトに指定する
							aEditSongWindowViewModel.DefaultId = aSongs[1].Id;
						}
						else
						{
							// 既存楽曲が 1 つ以上の場合は、タイアップ名が一致するものがあれば優先し、そうでなければ新規をデフォルトにする
							for (Int32 i = 1; i < aSongs.Count; i++)
							{
								TTieUp? aTieUpOfSong = YlCommon.SelectBaseById<TTieUp>(aMusicInfoDbInDisk.Connection, aSongs[i].TieUpId);
								if (aTieUpOfSong == null && String.IsNullOrEmpty(aTieUpName) || aTieUpOfSong != null && aTieUpOfSong.Name == aTieUpName)
								{
									aEditSongWindowViewModel.DefaultId = aSongs[i].Id;
									break;
								}
							}
						}
					}

					Messenger.Raise(new TransitionMessage(aEditSongWindowViewModel, "OpenEditSongWindow"));
					if (String.IsNullOrEmpty(aEditSongWindowViewModel.OkSelectedId))
					{
						return;
					}

					using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment!))
					{
						TSong? aSong = YlCommon.SelectBaseById<TSong>(aMusicInfoDbInDisk.Connection, aEditSongWindowViewModel.OkSelectedId);
						if (aSong != null)
						{
							if (aSong.Name == DicByFile?[YlConstants.RULE_VAR_TITLE])
							{
								UseSongAlias = false;
							}
							else
							{
								UseSongAlias = true;
								SongOrigin = aSong.Name;
							}
						}
						RaisePropertyChanged(nameof(IsSongNameRegistered));
					}
					UpdateListItems();
				}
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲詳細編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ヘルプリンクの制御
		public ListenerCommand<String>? HelpClickedCommand
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

		public void ButtonOkClicked()
		{
			try
			{
				(String? songOriginalId, String? tieUpOriginalId) = CheckInput();
				Save(songOriginalId, tieUpOriginalId);
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
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
			try
			{
				// タイトルバー
				Title = "名称の編集";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
				// 別名解決
				using ListContextInMemory listContextInMemory = ListContextInMemory.CreateContext(out DbSet<TFound> founds,
						out DbSet<TPerson> people, out DbSet<TArtistSequence> artistSequences, out DbSet<TComposerSequence> composerSequences,
						out DbSet<TTag> tags, out DbSet<TTagSequence> tagSequences);
				using TFoundSetter foundSetter = new(listContextInMemory, founds, people, artistSequences, composerSequences, tags, tagSequences);
				ApplySongAlias(foundSetter);
				ApplyTieUpAlias(foundSetter);

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
		private String _filePath;

		// ファイル名から取得した情報
		private Dictionary<String, String?> _dicByFile { get; }

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 適用可能な楽曲名の別名を検索してコンポーネントに反映
		// --------------------------------------------------------------------
		private void ApplySongAlias(TFoundSetter foundSetter)
		{
			if (String.IsNullOrEmpty(_dicByFile[YlConstants.RULE_VAR_TITLE]))
			{
				return;
			}

			String? songOrigin = foundSetter.SongOrigin(_dicByFile[YlConstants.RULE_VAR_TITLE]);
			if (songOrigin != _dicByFile[YlConstants.RULE_VAR_TITLE])
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

			if (DbCommon.SelectMasterByName(foundSetter.Songs, _dicByFile[YlConstants.RULE_VAR_TITLE]) == null)
			{
				// ファイル名から取得された情報が登録されていない
				UseSongAlias = true;
				SongOrigin = null;
			}
		}

		// --------------------------------------------------------------------
		// 適用可能なタイアップ名の別名を検索してコンポーネントに反映
		// --------------------------------------------------------------------
		private void ApplyTieUpAlias(TFoundSetter foundSetter)
		{
			if (String.IsNullOrEmpty(_dicByFile[YlConstants.RULE_VAR_PROGRAM]))
			{
				return;
			}

			String? programOrigin = foundSetter.ProgramOrigin(_dicByFile[YlConstants.RULE_VAR_PROGRAM]);
			if (programOrigin != _dicByFile[YlConstants.RULE_VAR_PROGRAM])
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

			if (DbCommon.SelectMasterByName(foundSetter.TieUps, _dicByFile[YlConstants.RULE_VAR_PROGRAM]) == null)
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
				if (SongOrigin == _dicByFile[YlConstants.RULE_VAR_TITLE])
				{
					throw new Exception("ファイル名・フォルダー固定値から取得した楽曲名と正式名称が同じです。\n"
							+ "楽曲名を揃えるのが不要の場合は、「楽曲名を揃える」のチェックを外して下さい。");
				}
				using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TSong> songs);
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
				if (TieUpOrigin == _dicByFile[YlConstants.RULE_VAR_PROGRAM])
				{
					throw new Exception("ファイル名・フォルダー固定値から取得したタイアップ名と正式名称が同じです。\n"
							+ "タイアップ名を揃えるのが不要の場合は、「タイアップ名を揃える」のチェックを外して下さい。");
				}
				using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TTieUp> tieUps);
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
		// 別名を保存
		// --------------------------------------------------------------------
		private void Save(String? songOriginalId, String? tieUpOriginalId)
		{
			// 楽曲別名
			if (!String.IsNullOrEmpty(_dicByFile[YlConstants.RULE_VAR_TITLE]))
			{
				using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TSongAlias> songAliases);
				if (UseSongAlias && !String.IsNullOrEmpty(songOriginalId))
				{
					TSongAlias? existSongAlias = DbCommon.SelectAliasByAlias(songAliases, _dicByFile[YlConstants.RULE_VAR_TITLE], true);
					TSongAlias newSongAlias = new()
					{
						// TBase
						Id = String.Empty,
						Import = false,
						Invalid = false,
						UpdateTime = YlConstants.INVALID_MJD,
						Dirty = true,

						// TAlias
						Alias = _dicByFile[YlConstants.RULE_VAR_TITLE]!,
						OriginalId = songOriginalId,
					};

					if (existSongAlias == null)
					{
						// 新規登録
						YlCommon.InputIdPrefixIfNeededWithInvoke(this);
						newSongAlias.Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareLastId(songAliases);
						songAliases.Add(newSongAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲別名テーブル新規登録：" + newSongAlias.Id + " / " + newSongAlias.Alias);
					}
					else if (DbCommon.IsRcAliasUpdated(existSongAlias, newSongAlias))
					{
						// 更新（既存のレコードが無効化されている場合は有効化も行う）
						newSongAlias.Id = existSongAlias.Id;
						newSongAlias.UpdateTime = existSongAlias.UpdateTime;
						Common.ShallowCopy(newSongAlias, existSongAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲別名テーブル更新：" + newSongAlias.Id + " / " + newSongAlias.Alias);
					}
				}
				else
				{
					TSongAlias? existSongAlias = DbCommon.SelectAliasByAlias(songAliases, _dicByFile[YlConstants.RULE_VAR_TITLE], false);
					if (existSongAlias != null)
					{
						// 無効化
						existSongAlias.Invalid = true;
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲別名テーブル無効化：" + existSongAlias.Id + " / " + existSongAlias.Alias);
					}
				}
				musicInfoContext.SaveChanges();
			}

			// タイアップ別名
			if (!String.IsNullOrEmpty(_dicByFile[YlConstants.RULE_VAR_PROGRAM]))
			{
				using MusicInfoContext musicInfoContext = MusicInfoContext.CreateContext(out DbSet<TTieUpAlias> tieUpAliases);
				if (UseTieUpAlias && !String.IsNullOrEmpty(tieUpOriginalId))
				{
					TTieUpAlias? existTieUpAlias = DbCommon.SelectAliasByAlias(tieUpAliases, _dicByFile[YlConstants.RULE_VAR_PROGRAM], true);
					TTieUpAlias newTieUpAlias = new()
					{
						// TBase
						Id = String.Empty,
						Import = false,
						Invalid = false,
						UpdateTime = YlConstants.INVALID_MJD,
						Dirty = true,

						// TAlias
						Alias = _dicByFile[YlConstants.RULE_VAR_PROGRAM]!,
						OriginalId = tieUpOriginalId,
					};

					if (existTieUpAlias == null)
					{
						// 新規登録
						YlCommon.InputIdPrefixIfNeededWithInvoke(this);
						newTieUpAlias.Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareLastId(tieUpAliases);
						tieUpAliases.Add(newTieUpAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "タイアップ別名テーブル新規登録：" + newTieUpAlias.Id + " / " + newTieUpAlias.Alias);
					}
					else if (DbCommon.IsRcAliasUpdated(existTieUpAlias, newTieUpAlias))
					{
						// 更新（既存のレコードが無効化されている場合は有効化も行う）
						newTieUpAlias.Id = existTieUpAlias.Id;
						newTieUpAlias.UpdateTime = existTieUpAlias.UpdateTime;
						Common.ShallowCopy(newTieUpAlias, existTieUpAlias);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "タイアップ別名テーブル更新：" + newTieUpAlias.Id + " / " + newTieUpAlias.Alias);
					}
				}
				else
				{
					TTieUpAlias? existTieUpAlias = DbCommon.SelectAliasByAlias(tieUpAliases, _dicByFile[YlConstants.RULE_VAR_PROGRAM], false);
					if (existTieUpAlias != null)
					{
						// 無効化
						existTieUpAlias.Invalid = true;
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
#if false
				FolderSettingsInDisk aFolderSettingsInDisk = YlCommon.LoadFolderSettings2Ex(Path.GetDirectoryName(PathExLen));
				FolderSettingsInMemory aFolderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(aFolderSettingsInDisk);
				TFound aTFound = new TFound();
				aTFound.Path = Environment!.ShortenPath(PathExLen);

				using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(Environment))
				using (TFoundSetterAliasSpecify aTFoundSetterAliasSpecify = new TFoundSetterAliasSpecify(aMusicInfoDbInDisk))
				{
					// エイリアス指定
					if (UseTieUpAlias)
					{
						aTFoundSetterAliasSpecify.SpecifiedProgramOrigin = TieUpOrigin;
					}
					if (UseSongAlias)
					{
						aTFoundSetterAliasSpecify.SpecifiedSongOrigin = SongOrigin;
					}

					aTFoundSetterAliasSpecify.SetTFoundValue(aTFound, aFolderSettingsInMemory);
				}

				ListCategoryName = aTFound.Category;
				ListTieUpName = aTFound.TieUpName;
				ListSongName = aTFound.SongName;
				ListArtistName = aTFound.ArtistName;
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト表示予定項目更新時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

	}
}
