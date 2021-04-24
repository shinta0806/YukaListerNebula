// ============================================================================
// 
// 楽曲詳細情報編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.EditSequenceWindowViewModels;
using YukaLister.ViewModels.SearchMasterWindowViewModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditSongWindowViewModel : EditCategorizableWindowViewModel<TSong>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public EditSongWindowViewModel(MusicInfoContext musicInfoContext, DbSet<TSong> records)
				: base(musicInfoContext, records)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public EditSongWindowViewModel()
				: base(null!, null!)
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// タイアップチェックボックスの有効無効
		private Boolean _isTieUpEnabled;
		public Boolean IsTieUpEnabled
		{
			get => _isTieUpEnabled;
			set
			{
				if (RaisePropertyChangedIfSet(ref _isTieUpEnabled, value))
				{
					SetIsCategoryEnabled();
				}
			}
		}

		// タイアップあり
		private Boolean _hasTieUp;
		public Boolean HasTieUp
		{
			get => _hasTieUp;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasTieUp, value))
				{
					ButtonSearchTieUpClickedCommand.RaiseCanExecuteChanged();
					ButtonEditTieUpClickedCommand.RaiseCanExecuteChanged();
					if (!_hasTieUp)
					{
						_tieUpId = null;
						TieUpName = null;
					}
					SetIsCategoryEnabled();
				}
			}
		}

		// タイアップ名
		private String? _tieUpName;
		public String? TieUpName
		{
			get => _tieUpName;
			set => RaisePropertyChangedIfSet(ref _tieUpName, value);
		}

		// 摘要選択ボタンのコンテキストメニュー
		public List<MenuItem> ContextMenuButtonSelectOpEdItems { get; set; } = new();

		// 摘要
		private String? _opEd;
		public String? OpEd
		{
			get => _opEd;
			set => RaisePropertyChangedIfSet(ref _opEd, value);
		}

		// カテゴリーチェックボックスの有効無効
		private Boolean _isCategoryEnabled;
		public Boolean IsCategoryEnabled
		{
			get => _isCategoryEnabled;
			set
			{
				if (RaisePropertyChangedIfSet(ref _isCategoryEnabled, value))
				{
					SetIsTieUpEnabled();
				}
			}
		}

		// タグあり
		private Boolean _hasTag;
		public Boolean HasTag
		{
			get => _hasTag;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasTag, value))
				{
					ButtonSearchTagClickedCommand.RaiseCanExecuteChanged();
					ButtonEditTagClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// タグ名（カンマ区切りで複数）
		private String? _tagNames;
		public String? TagNames
		{
			get => _tagNames;
			set => RaisePropertyChangedIfSet(ref _tagNames, value);
		}

		// 歌手あり
		private Boolean _hasArtist;
		public Boolean HasArtist
		{
			get => _hasArtist;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasArtist, value))
				{
					ButtonSearchArtistClickedCommand.RaiseCanExecuteChanged();
					ButtonEditArtistClickedCommand.RaiseCanExecuteChanged();
					if (!_hasArtist)
					{
						_artistIds = null;
						ArtistNames = null;
					}
				}
			}
		}

		// 歌手名（カンマ区切りで複数）
		private String? _artistNames;
		public String? ArtistNames
		{
			get => _artistNames;
			set => RaisePropertyChangedIfSet(ref _artistNames, value);
		}

		// 作詞者あり
		private Boolean _hasLyrist;
		public Boolean HasLyrist
		{
			get => _hasLyrist;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasLyrist, value))
				{
					ButtonSearchLyristClickedCommand.RaiseCanExecuteChanged();
					ButtonSameLyristClickedCommand.RaiseCanExecuteChanged();
					ButtonEditLyristClickedCommand.RaiseCanExecuteChanged();
					if (!_hasLyrist)
					{
						_lyristIds = null;
						LyristNames = null;
					}
				}
			}
		}

		// 作詞者名（カンマ区切りで複数）
		private String? _lyristNames;
		public String? LyristNames
		{
			get => _lyristNames;
			set => RaisePropertyChangedIfSet(ref _lyristNames, value);
		}

		// 作曲者あり
		private Boolean _hasComposer;
		public Boolean HasComposer
		{
			get => _hasComposer;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasComposer, value))
				{
					ButtonSearchComposerClickedCommand.RaiseCanExecuteChanged();
					ButtonSameComposerClickedCommand.RaiseCanExecuteChanged();
					ButtonEditComposerClickedCommand.RaiseCanExecuteChanged();
					if (!_hasComposer)
					{
						_composerIds = null;
						ComposerNames = null;
					}
				}
			}
		}

		// 作曲者名（カンマ区切りで複数）
		private String? _composerNames;
		public String? ComposerNames
		{
			get => _composerNames;
			set => RaisePropertyChangedIfSet(ref _composerNames, value);
		}

		// 編曲者あり
		private Boolean _hasArranger;
		public Boolean HasArranger
		{
			get => _hasArranger;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasArranger, value))
				{
					ButtonSearchArrangerClickedCommand.RaiseCanExecuteChanged();
					ButtonSameArrangerClickedCommand.RaiseCanExecuteChanged();
					ButtonEditArrangerClickedCommand.RaiseCanExecuteChanged();
					if (!_hasArranger)
					{
						_arrangerIds = null;
						ArrangerNames = null;
					}
				}
			}
		}

		// 編曲者名（カンマ区切りで複数）
		private String? _arrangerNames;
		public String? ArrangerNames
		{
			get => _arrangerNames;
			set => RaisePropertyChangedIfSet(ref _arrangerNames, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region タイアップ検索ボタンの制御
		private ViewModelCommand? _buttonSearchTieUpClickedCommand;

		public ViewModelCommand ButtonSearchTieUpClickedCommand
		{
			get
			{
				if (_buttonSearchTieUpClickedCommand == null)
				{
					_buttonSearchTieUpClickedCommand = new ViewModelCommand(ButtonSearchTieUpClicked, CanButtonSearchTieUpClicked);
				}
				return _buttonSearchTieUpClickedCommand;
			}
		}

		public Boolean CanButtonSearchTieUpClicked()
		{
			return HasTieUp;
		}

		public void ButtonSearchTieUpClicked()
		{
			try
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUp> tieUps);
				using SearchMasterWindowViewModel<TTieUp> searchMasterWindowViewModel = new(tieUps);
				searchMasterWindowViewModel.SelectedKeyword = OriginalTieUpName();
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				_isTieUpSearched = true;
				SetTieUp(tieUps, searchMasterWindowViewModel.OkSelectedMaster);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイアップ検索ボタンクリック時エラー：\n" + excep.Message);
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
					_buttonEditTieUpClickedCommand = new ViewModelCommand(ButtonEditTieUpClicked, CanButtonEditTieUpClicked);
				}
				return _buttonEditTieUpClickedCommand;
			}
		}

		public Boolean CanButtonEditTieUpClicked()
		{
			return HasTieUp;
		}

		public void ButtonEditTieUpClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(_tieUpId))
				{
					if (!_isTieUpSearched)
					{
						throw new Exception("タイアップが選択されていないため新規タイアップ情報作成となりますが、その前に一度、目的のタイアップが未登録かどうか検索して下さい。");
					}

					if (MessageBox.Show("タイアップが選択されていません。\n新規にタイアップ情報を作成しますか？\n"
							+ "（目的のタイアップが未登録の場合（検索してもヒットしない場合）に限り、新規作成を行って下さい）", "確認",
							MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
					{
						return;
					}
				}

				// 既存レコードを用意
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUp> tieUps);
				List<TTieUp> sameNameTieUps = DbCommon.SelectMastersByName(tieUps, OriginalTieUpName());

				// 新規作成用を追加
				TTieUp newRecord = new()
				{
					// IRcBase
					Id = String.Empty,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IRcMaster
					Name = OriginalTieUpName(),
					Ruby = null,
					Keyword = null,
				};
				sameNameTieUps.Insert(0, newRecord);

				// ウィンドウを開く
				using EditTieUpWindowViewModel editTieUpWindowViewModel = new(_musicInfoContext, tieUps);
				editTieUpWindowViewModel.SetMasters(sameNameTieUps);
				editTieUpWindowViewModel.DefaultMaster = DbCommon.SelectBaseById(tieUps, _tieUpId);
				Messenger.Raise(new TransitionMessage(editTieUpWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_TIE_UP_WINDOW));

				// 後処理
				SetTieUp(tieUps, editTieUpWindowViewModel.OkSelectedMaster);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイアップ詳細編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}

		}
		#endregion

		#region 摘要選択ボタンの制御
		private ViewModelCommand? _buttonSelectOpEdClickedCommand;

		public ViewModelCommand ButtonSelectOpEdClickedCommand
		{
			get
			{
				if (_buttonSelectOpEdClickedCommand == null)
				{
					_buttonSelectOpEdClickedCommand = new ViewModelCommand(ButtonSelectOpEdClicked);
				}
				return _buttonSelectOpEdClickedCommand;
			}
		}

		public void ButtonSelectOpEdClicked()
		{
		}
		#endregion

		#region タグ検索ボタンの制御
		private ViewModelCommand? _buttonSearchTagClickedCommand;

		public ViewModelCommand ButtonSearchTagClickedCommand
		{
			get
			{
				if (_buttonSearchTagClickedCommand == null)
				{
					_buttonSearchTagClickedCommand = new ViewModelCommand(ButtonSearchTagClicked, CanButtonSearchTagClicked);
				}
				return _buttonSearchTagClickedCommand;
			}
		}

		public Boolean CanButtonSearchTagClicked()
		{
			return HasTag;
		}

		public void ButtonSearchTagClicked()
		{
			try
			{
				// タグが複数指定されている場合は先頭のみで検索
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTag> tags);
				using SearchMasterWindowViewModel<TTag> searchMasterWindowViewModel = new(tags);
				searchMasterWindowViewModel.SelectedKeyword = HeadName(TagNames);
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				if (searchMasterWindowViewModel.OkSelectedMaster == null)
				{
					return;
				}

				_tagIds = searchMasterWindowViewModel.OkSelectedMaster.Id;
				TagNames = searchMasterWindowViewModel.OkSelectedMaster.Name;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タグ検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 複数タグ検索ボタンの制御
		private ViewModelCommand? _buttonEditTagClickedCommand;

		public ViewModelCommand ButtonEditTagClickedCommand
		{
			get
			{
				if (_buttonEditTagClickedCommand == null)
				{
					_buttonEditTagClickedCommand = new ViewModelCommand(ButtonEditTagClicked, CanButtonEditTagClicked);
				}
				return _buttonEditTagClickedCommand;
			}
		}

		public Boolean CanButtonEditTagClicked()
		{
			return HasTag;
		}

		public void ButtonEditTagClicked()
		{
			try
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTag> tags);
				using EditTagsWindowViewModel editTagsWindowViewModel = new(_musicInfoContext, tags);
				List<String> splitIds = YlCommon.SplitIds(_tagIds);
				foreach (String id in splitIds)
				{
					TTag? tag = DbCommon.SelectBaseById(tags, id);
					if (tag != null)
					{
						editTagsWindowViewModel.Masters.Add(tag);
					}
				}
				Messenger.Raise(new TransitionMessage(editTagsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_SEQUENCE_WINDOW));

				if (!editTagsWindowViewModel.OkSelectedMasters.Any())
				{
					return;
				}
				(HasTag, _tagIds, TagNames) = ConcatMasterIdsAndNames(editTagsWindowViewModel.OkSelectedMasters);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数タグ検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 歌手検索ボタンの制御
		private ViewModelCommand? _buttonSearchArtistClickedCommand;

		public ViewModelCommand ButtonSearchArtistClickedCommand
		{
			get
			{
				if (_buttonSearchArtistClickedCommand == null)
				{
					_buttonSearchArtistClickedCommand = new ViewModelCommand(ButtonSearchArtistClicked, CanButtonSearchArtistClicked);
				}
				return _buttonSearchArtistClickedCommand;
			}
		}

		public Boolean CanButtonSearchArtistClicked()
		{
			return HasArtist;
		}

		public void ButtonSearchArtistClicked()
		{
			try
			{
				(_artistIds, ArtistNames) = SearchPerson("歌手", ArtistNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "歌手検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 複数歌手検索ボタンの制御
		private ViewModelCommand? mButtonEditArtistClickedCommand;

		public ViewModelCommand ButtonEditArtistClickedCommand
		{
			get
			{
				if (mButtonEditArtistClickedCommand == null)
				{
					mButtonEditArtistClickedCommand = new ViewModelCommand(ButtonEditArtistClicked, CanButtonEditArtistClicked);
				}
				return mButtonEditArtistClickedCommand;
			}
		}

		public Boolean CanButtonEditArtistClicked()
		{
			return HasArtist;
		}

		public void ButtonEditArtistClicked()
		{
			try
			{
				(HasArtist, _artistIds, ArtistNames) = EditPeople("歌手", _artistIds, ArtistNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数歌手検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 作詞者検索ボタンの制御
		private ViewModelCommand? _buttonSearchLyristClickedCommand;

		public ViewModelCommand ButtonSearchLyristClickedCommand
		{
			get
			{
				if (_buttonSearchLyristClickedCommand == null)
				{
					_buttonSearchLyristClickedCommand = new ViewModelCommand(ButtonSearchLyristClicked, CanButtonSearchLyristClicked);
				}
				return _buttonSearchLyristClickedCommand;
			}
		}

		public Boolean CanButtonSearchLyristClicked()
		{
			return HasLyrist;
		}

		public void ButtonSearchLyristClicked()
		{
			try
			{
				(_lyristIds, LyristNames) = SearchPerson("作詞者", LyristNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "作詞者検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 作詞者同上ボタンの制御
		private ViewModelCommand? _buttonSameLyristClickedCommand;

		public ViewModelCommand ButtonSameLyristClickedCommand
		{
			get
			{
				if (_buttonSameLyristClickedCommand == null)
				{
					_buttonSameLyristClickedCommand = new ViewModelCommand(ButtonSameLyristClicked, CanButtonSameLyristClicked);
				}
				return _buttonSameLyristClickedCommand;
			}
		}

		public Boolean CanButtonSameLyristClicked()
		{
			return HasLyrist;
		}

		public void ButtonSameLyristClicked()
		{
			try
			{
				_lyristIds = _artistIds;
				LyristNames = ArtistNames;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "作詞者同上ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 複数作詞者検索ボタンの制御
		private ViewModelCommand? _buttonEditLyristClickedCommand;

		public ViewModelCommand ButtonEditLyristClickedCommand
		{
			get
			{
				if (_buttonEditLyristClickedCommand == null)
				{
					_buttonEditLyristClickedCommand = new ViewModelCommand(ButtonEditLyristClicked, CanButtonEditLyristClicked);
				}
				return _buttonEditLyristClickedCommand;
			}
		}

		public Boolean CanButtonEditLyristClicked()
		{
			return HasLyrist;
		}

		public void ButtonEditLyristClicked()
		{
			try
			{
				(HasLyrist, _lyristIds, LyristNames) = EditPeople("作詞者", _lyristIds, LyristNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数作詞者検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 作曲者検索ボタンの制御
		private ViewModelCommand? mButtonSearchComposerClickedCommand;

		public ViewModelCommand ButtonSearchComposerClickedCommand
		{
			get
			{
				if (mButtonSearchComposerClickedCommand == null)
				{
					mButtonSearchComposerClickedCommand = new ViewModelCommand(ButtonSearchComposerClicked, CanButtonSearchComposerClicked);
				}
				return mButtonSearchComposerClickedCommand;
			}
		}

		public Boolean CanButtonSearchComposerClicked()
		{
			return HasComposer;
		}

		public void ButtonSearchComposerClicked()
		{
			try
			{
				(_composerIds, ComposerNames) = SearchPerson("作曲者", ComposerNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "作曲者検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 作曲者同上ボタンの制御
		private ViewModelCommand? _buttonSameComposerClickedCommand;

		public ViewModelCommand ButtonSameComposerClickedCommand
		{
			get
			{
				if (_buttonSameComposerClickedCommand == null)
				{
					_buttonSameComposerClickedCommand = new ViewModelCommand(ButtonSameComposerClicked, CanButtonSameComposerClicked);
				}
				return _buttonSameComposerClickedCommand;
			}
		}

		public Boolean CanButtonSameComposerClicked()
		{
			return HasComposer;
		}

		public void ButtonSameComposerClicked()
		{
			try
			{
				_composerIds = _lyristIds;
				ComposerNames = LyristNames;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "作曲者同上ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 複数作曲者検索ボタンの制御
		private ViewModelCommand? _buttonEditComposerClickedCommand;

		public ViewModelCommand ButtonEditComposerClickedCommand
		{
			get
			{
				if (_buttonEditComposerClickedCommand == null)
				{
					_buttonEditComposerClickedCommand = new ViewModelCommand(ButtonEditComposerClicked, CanButtonEditComposerClicked);
				}
				return _buttonEditComposerClickedCommand;
			}
		}

		public Boolean CanButtonEditComposerClicked()
		{
			return HasComposer;
		}

		public void ButtonEditComposerClicked()
		{
			try
			{
				(HasComposer, _composerIds, ComposerNames) = EditPeople("作曲者", _composerIds, ComposerNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数作曲者検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 編曲者検索ボタンの制御
		private ViewModelCommand? _buttonSearchArrangerClickedCommand;

		public ViewModelCommand ButtonSearchArrangerClickedCommand
		{
			get
			{
				if (_buttonSearchArrangerClickedCommand == null)
				{
					_buttonSearchArrangerClickedCommand = new ViewModelCommand(ButtonSearchArrangerClicked, CanButtonSearchArrangerClicked);
				}
				return _buttonSearchArrangerClickedCommand;
			}
		}

		public Boolean CanButtonSearchArrangerClicked()
		{
			return HasArranger;
		}

		public void ButtonSearchArrangerClicked()
		{
			try
			{
				(_arrangerIds, ArrangerNames) = SearchPerson("編曲者", ArrangerNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "編曲者検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 編曲者同上ボタンの制御
		private ViewModelCommand? _buttonSameArrangerClickedCommand;

		public ViewModelCommand ButtonSameArrangerClickedCommand
		{
			get
			{
				if (_buttonSameArrangerClickedCommand == null)
				{
					_buttonSameArrangerClickedCommand = new ViewModelCommand(ButtonSameArrangerClicked, CanButtonSameArrangerClicked);
				}
				return _buttonSameArrangerClickedCommand;
			}
		}

		public Boolean CanButtonSameArrangerClicked()
		{
			return HasArranger;
		}

		public void ButtonSameArrangerClicked()
		{
			try
			{
				_arrangerIds = _composerIds;
				ArrangerNames = ComposerNames;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "編曲者同上ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 複数編曲者検索ボタンの制御
		private ViewModelCommand? _buttonEditArrangerClickedCommand;

		public ViewModelCommand ButtonEditArrangerClickedCommand
		{
			get
			{
				if (_buttonEditArrangerClickedCommand == null)
				{
					_buttonEditArrangerClickedCommand = new ViewModelCommand(ButtonEditArrangerClicked, CanButtonEditArrangerClicked);
				}
				return _buttonEditArrangerClickedCommand;
			}
		}

		public Boolean CanButtonEditArrangerClicked()
		{
			return HasArranger;
		}

		public void ButtonEditArrangerClicked()
		{
			try
			{
				(HasArranger, _arrangerIds, ArrangerNames) = EditPeople("編曲者", _arrangerIds, ArrangerNames);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数編曲者検索ボタンクリック時エラー：\n" + excep.Message);
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
				// 摘要ボタンのコンテキストメニュー
				AddContextMenuItemToButtonSelectOpEd("OP（オープニング）");
				AddContextMenuItemToButtonSelectOpEd("ED（エンディング）");
				AddContextMenuItemToButtonSelectOpEd("IN（挿入歌）");
				AddContextMenuItemToButtonSelectOpEd("IM（イメージソング）");
				AddContextMenuItemToButtonSelectOpEd("CH（キャラクターソング）");
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲詳細情報編集ウィンドウビューモデル初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力値を確認する
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected override void CheckInput()
		{
			base.CheckInput();

			// チェックされているのに指定されていない項目を確認
			if (HasTieUp && String.IsNullOrEmpty(_tieUpId))
			{
				throw new Exception("タイアップが「あり」になっていますが指定されていません。");
			}
			List<String> tagIds = YlCommon.SplitIds(_tagIds);
			if (HasTag && tagIds.Count == 0)
			{
				throw new Exception("タグが「あり」になっていますが指定されていません。");
			}
			List<String> artistIds = YlCommon.SplitIds(_artistIds);
			if (HasArtist && artistIds.Count == 0)
			{
				throw new Exception("歌手が「あり」になっていますが指定されていません。");
			}
			List<String> lyristIds = YlCommon.SplitIds(_lyristIds);
			if (HasLyrist && lyristIds.Count == 0)
			{
				throw new Exception("作詞者が「あり」になっていますが指定されていません。");
			}
			List<String> composerIds = YlCommon.SplitIds(_composerIds);
			if (HasComposer && composerIds.Count == 0)
			{
				throw new Exception("作曲者が「あり」になっていますが指定されていません。");
			}
			List<String> arrangerIds = YlCommon.SplitIds(_arrangerIds);
			if (HasArranger && arrangerIds.Count == 0)
			{
				throw new Exception("編曲者が「あり」になっていますが指定されていません。");
			}
		}

		// --------------------------------------------------------------------
		// HasCategory が変更された
		// --------------------------------------------------------------------
		protected override void HasCategoryChanged()
		{
			SetIsTieUpEnabled();
		}

		// --------------------------------------------------------------------
		// プロパティーの内容を Master に格納
		// --------------------------------------------------------------------
		protected override void PropertiesToRecord(TSong master)
		{
			base.PropertiesToRecord(master);

			// TSong
			master.TieUpId = _tieUpId;
			master.OpEd = OpEd;
		}

		// --------------------------------------------------------------------
		// Master の内容をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void RecordToProperties(TSong master)
		{
			base.RecordToProperties(master);

			// タイアップ関係
			if (String.IsNullOrEmpty(master.TieUpId))
			{
				HasTieUp = false;
			}
			else
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUp> tieUps);
				HasTieUp = true;
				TTieUp? tieUp = DbCommon.SelectBaseById(tieUps, master.TieUpId);
				if (tieUp != null)
				{
					SetTieUp(tieUps, tieUp);
				}
				else
				{
					_tieUpId = null;
					TieUpName = null;
				}
			}

			// 摘要
			OpEd = master.OpEd;

			// タグ
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTagSequence> tagSequences);
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTag> tags);
			(HasTag, _tagIds, TagNames) = ConcatMasterIdsAndNames(DbCommon.SelectSequencedTagsBySongId(tagSequences, tags, master.Id));

			// 人物関係
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TArtistSequence> artistSequences);
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TPerson> people);
			(HasArtist, _artistIds, ArtistNames) = ConcatMasterIdsAndNames(DbCommon.SelectSequencedPeopleBySongId(artistSequences, people, master.Id));

			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TLyristSequence> lyristSequences);
			(HasLyrist, _lyristIds, LyristNames) = ConcatMasterIdsAndNames(DbCommon.SelectSequencedPeopleBySongId(lyristSequences, people, master.Id));

			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TComposerSequence> composerSequences);
			(HasComposer, _composerIds, ComposerNames) = ConcatMasterIdsAndNames(DbCommon.SelectSequencedPeopleBySongId(composerSequences, people, master.Id));

			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TArrangerSequence> arrangerSequences);
			(HasArranger, _arrangerIds, ArrangerNames) = ConcatMasterIdsAndNames(DbCommon.SelectSequencedPeopleBySongId(arrangerSequences, people, master.Id));

			SetIsTieUpEnabled();
			SetIsCategoryEnabled();
		}

		// --------------------------------------------------------------------
		// レコード保存
		// --------------------------------------------------------------------
		protected override void Save(TSong master)
		{
			if (master.Id == NewIdForDisplay())
			{
				// 新規登録
				AddNewRecord(master);
			}
			else
			{
				TSong? existRecord = DbCommon.SelectBaseById(_records, master.Id, true);
				if (existRecord == null)
				{
					throw new Exception("更新対象の楽曲レコードが見つかりません：" + master.Id);
				}
				if (DbCommon.IsRcSongUpdated(existRecord, master))
				{
					// 更新（既存のレコードが無効化されている場合は有効化も行う）
					UpdateExistRecord(existRecord, master);
				}
			}

			// タグ紐付け
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTagSequence> tagSequences);
			DbCommon.RegisterSequence<TTagSequence>(tagSequences, master.Id, YlCommon.SplitIds(_tagIds));

			// 人物紐付け
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TArtistSequence> artistSequences);
			DbCommon.RegisterSequence<TArtistSequence>(artistSequences, master.Id, YlCommon.SplitIds(_artistIds));

			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TLyristSequence> lyristSequences);
			DbCommon.RegisterSequence<TLyristSequence>(lyristSequences, master.Id, YlCommon.SplitIds(_lyristIds));

			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TComposerSequence> composerSequences);
			DbCommon.RegisterSequence<TComposerSequence>(composerSequences, master.Id, YlCommon.SplitIds(_composerIds));

			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TArrangerSequence> arrangerSequences);
			DbCommon.RegisterSequence<TArrangerSequence>(arrangerSequences, master.Id, YlCommon.SplitIds(_arrangerIds));

			_musicInfoContext.SaveChanges();
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// タイアップ ID
		private String? _tieUpId;

		// タグ ID（カンマ区切りで複数）
		private String? _tagIds;

		// 歌手 ID（カンマ区切りで複数）
		private String? _artistIds;

		// 作詞者 ID（カンマ区切りで複数）
		private String? _lyristIds;

		// 作曲者 ID（カンマ区切りで複数）
		private String? _composerIds;

		// 編曲者 ID（カンマ区切りで複数）
		private String? _arrangerIds;

		// タイアップを検索したかどうか
		private Boolean _isTieUpSearched;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ButtonSelectOpEd のコンテキストメニューにアイテムを追加
		// --------------------------------------------------------------------
		private void AddContextMenuItemToButtonSelectOpEd(String label)
		{
			YlCommon.AddContextMenuItem(ContextMenuButtonSelectOpEdItems, label, ContextMenuButtonSelectOpEd_Click);
		}

		// --------------------------------------------------------------------
		// 複数の IRcMaster の ID と名前をカンマで結合
		// --------------------------------------------------------------------
		private (Boolean has, String? ids, String? names) ConcatMasterIdsAndNames<T>(List<T> masters) where T : IRcMaster
		{
			String ids = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], masters.Select(x => x.Id));
			String names = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], masters.Select(x => x.Name));
			return (!String.IsNullOrEmpty(ids), String.IsNullOrEmpty(ids) ? null : ids, String.IsNullOrEmpty(names) ? null : names);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void ContextMenuButtonSelectOpEd_Click(Object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				MenuItem item = (MenuItem)sender;
				String itemLabel = (String)item.Header;
				Int32 pos = itemLabel.IndexOf("（");
				if (pos >= 0)
				{
					OpEd = itemLabel[0..pos];
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "摘要選択メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// カンマ区切りで連結されている name のうち先頭のみを抽出
		// --------------------------------------------------------------------
		private String? HeadName(String? name)
		{
			return String.IsNullOrEmpty(name) ? null : name.Split(YlConstants.VAR_VALUE_DELIMITER[0], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		}

		// --------------------------------------------------------------------
		// TieUpName は同名識別用に変更されている場合があるので TieUpId から正式名称を取得する
		// --------------------------------------------------------------------
		private String? OriginalTieUpName()
		{
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUp> tieUps);
			return DbCommon.SelectBaseById(tieUps, _tieUpId)?.Name;
		}

		// --------------------------------------------------------------------
		// 人物を検索して結果を取得
		// --------------------------------------------------------------------
		private (String? id, String? name) SearchPerson(String caption, String? srcName)
		{
			// 人物が複数指定されている場合は先頭のみで検索
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TPerson> people);
			using SearchMasterWindowViewModel<TPerson> searchMasterWindowViewModel = new(people, caption);
			searchMasterWindowViewModel.SelectedKeyword = HeadName(srcName);
			Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

			if (searchMasterWindowViewModel.OkSelectedMaster == null)
			{
				return (null, null);
			}

			return (searchMasterWindowViewModel.OkSelectedMaster.Id, searchMasterWindowViewModel.OkSelectedMaster.Name);
		}

		// --------------------------------------------------------------------
		// 人物詳細編集
		// --------------------------------------------------------------------
		private (Boolean has, String? ids, String? names) EditPeople(String caption, String? srcIds, String? srcNames)
		{
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TPerson> people);
			using EditPeopleWindowViewModel editPeopleWindowViewModel = new(_musicInfoContext, people, caption);
			List<String> splitIds = YlCommon.SplitIds(srcIds);
			foreach (String id in splitIds)
			{
				TPerson? person = DbCommon.SelectBaseById(people, id);
				if (person != null)
				{
					editPeopleWindowViewModel.Masters.Add(person);
				}
			}
			Messenger.Raise(new TransitionMessage(editPeopleWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_SEQUENCE_WINDOW));

			if (!editPeopleWindowViewModel.OkSelectedMasters.Any())
			{
				return (!String.IsNullOrEmpty(srcIds), srcIds, srcNames);
			}
			return ConcatMasterIdsAndNames(editPeopleWindowViewModel.OkSelectedMasters);
		}

		// --------------------------------------------------------------------
		// IsCategoryEnabled の設定
		// --------------------------------------------------------------------
		private void SetIsCategoryEnabled()
		{
			IsCategoryEnabled = !(IsTieUpEnabled && HasTieUp);
		}

		// --------------------------------------------------------------------
		// IsTieUpEnabled の設定
		// --------------------------------------------------------------------
		private void SetIsTieUpEnabled()
		{
			IsTieUpEnabled = !(IsCategoryEnabled && HasCategory);
		}

		// --------------------------------------------------------------------
		// _tieUpId などを refer に合わせて設定
		// --------------------------------------------------------------------
		private void SetTieUp(DbSet<TTieUp> tieUps, TTieUp? refer)
		{
			if (refer == null)
			{
				return;
			}

			// ID
			_tieUpId = refer.Id;

			// 名前
			refer.AvoidSameName = DbCommon.SelectMastersByName(tieUps, refer.Name).Count > 1;
			TieUpName = refer.DisplayName;
		}
	}
}
