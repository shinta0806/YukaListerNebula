// ============================================================================
// 
// タイアップ詳細情報編集ウィンドウの ViewModel
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
	public class EditTieUpWindowViewModel : EditCategorizableWindowViewModel<TTieUp>
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public EditTieUpWindowViewModel(MusicInfoContext musicInfoContext, DbSet<TTieUp> records)
				: base(musicInfoContext, records)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public EditTieUpWindowViewModel()
				: base(null!, null!)
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 年齢制限選択ボタンのコンテキストメニュー
		public List<MenuItem> ContextMenuButtonSelectAgeLimitItems { get; set; } = new();

		// 年齢制限
		private String? _ageLimit;
		public String? AgeLimit
		{
			get => _ageLimit;
			set => RaisePropertyChangedIfSet(ref _ageLimit, value);
		}

		// 制作会社あり
		private Boolean _hasMaker;
		public Boolean HasMaker
		{
			get => _hasMaker;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasMaker, value))
				{
					ButtonSearchMakerClickedCommand.RaiseCanExecuteChanged();
					ButtonEditMakerClickedCommand.RaiseCanExecuteChanged();
					if (!_hasMaker)
					{
						_makerId = null;
						MakerDisplayName = null;
					}
				}
			}
		}

		// 制作会社名（表示名）
		private String? _makerDisplayName;
		public String? MakerDisplayName
		{
			get => _makerDisplayName;
			set => RaisePropertyChangedIfSet(ref _makerDisplayName, value);
		}

		// タイアップグループあり
		private Boolean _hasTieUpGroup;
		public Boolean HasTieUpGroup
		{
			get => _hasTieUpGroup;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasTieUpGroup, value))
				{
					ButtonSearchTieUpGroupClickedCommand.RaiseCanExecuteChanged();
					ButtonEditTieUpGroupClickedCommand.RaiseCanExecuteChanged();
					if (!_hasTieUpGroup)
					{
						_tieUpGroupIds = null;
						TieUpGroupDisplayNames = null;
					}
				}
			}
		}

		// タイアップグループ名（表示名、カンマ区切りで複数）
		private String? _tieUpGroupDisplayNames;
		public String? TieUpGroupDisplayNames
		{
			get => _tieUpGroupDisplayNames;
			set => RaisePropertyChangedIfSet(ref _tieUpGroupDisplayNames, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 制作会社検索ボタンの制御
		private ViewModelCommand? _buttonSearchMakerClickedCommand;

		public ViewModelCommand ButtonSearchMakerClickedCommand
		{
			get
			{
				if (_buttonSearchMakerClickedCommand == null)
				{
					_buttonSearchMakerClickedCommand = new ViewModelCommand(ButtonSearchMakerClicked, CanButtonSearchMakerClicked);
				}
				return _buttonSearchMakerClickedCommand;
			}
		}

		public Boolean CanButtonSearchMakerClicked()
		{
			return HasMaker;
		}

		public void ButtonSearchMakerClicked()
		{
			try
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TMaker> makers);
				using SearchMasterWindowViewModel<TMaker> searchMasterWindowViewModel = new(makers);
				searchMasterWindowViewModel.SelectedKeyword = OriginalMakerName();
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				_isMakerSearched = true;
				SetMaker(makers, searchMasterWindowViewModel.OkSelectedMaster);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "制作会社検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 制作会社詳細編集ボタンの制御
		private ViewModelCommand? _buttonEditMakerClickedCommand;

		public ViewModelCommand ButtonEditMakerClickedCommand
		{
			get
			{
				if (_buttonEditMakerClickedCommand == null)
				{
					_buttonEditMakerClickedCommand = new ViewModelCommand(ButtonEditMakerClicked, CanButtonEditMakerClicked);
				}
				return _buttonEditMakerClickedCommand;
			}
		}

		public Boolean CanButtonEditMakerClicked()
		{
			return HasMaker;
		}

		public void ButtonEditMakerClicked()
		{
			try
			{
				if (String.IsNullOrEmpty(MakerDisplayName))
				{
					if (!_isMakerSearched)
					{
						throw new Exception("制作会社が選択されていないため新規制作会社情報作成となりますが、その前に一度、目的の制作会社が未登録かどうか検索して下さい。");
					}

					if (MessageBox.Show("制作会社が選択されていません。\n新規に制作会社情報を作成しますか？\n"
							+ "（目的の制作会社が未登録の場合（検索してもヒットしない場合）に限り、新規作成を行って下さい）", "確認",
							MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
					{
						return;
					}
				}

				// 既存レコードを用意
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TMaker> makers);
				List<TMaker> sameNameMakers = DbCommon.SelectMastersByName(makers, OriginalMakerName());

				// 新規作成用を追加
				TMaker newRecord = new()
				{
					// IRcBase
					Id = String.Empty,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IRcMaster
					Name = OriginalMakerName(),
					Ruby = null,
					Keyword = null,
				};
				sameNameMakers.Insert(0, newRecord);

				// ウィンドウを開く
				using EditMakerWindowViewModel editMakerWindowViewModel = new(_musicInfoContext, makers);
				editMakerWindowViewModel.SetMasters(sameNameMakers);
				editMakerWindowViewModel.DefaultMasterId = DbCommon.SelectBaseById(makers, _makerId)?.Id;
				Messenger.Raise(new TransitionMessage(editMakerWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_MASTER_WINDOW));

				// 後処理
				SetMaker(makers, editMakerWindowViewModel.OkSelectedMaster);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "制作会社詳細編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region タイアップグループ検索ボタンの制御
		private ViewModelCommand? _buttonSearchTieUpGroupClickedCommand;

		public ViewModelCommand ButtonSearchTieUpGroupClickedCommand
		{
			get
			{
				if (_buttonSearchTieUpGroupClickedCommand == null)
				{
					_buttonSearchTieUpGroupClickedCommand = new ViewModelCommand(ButtonSearchTieUpGroupClicked, CanmButtonSearchTieUpGroupClicked);
				}
				return _buttonSearchTieUpGroupClickedCommand;
			}
		}

		public Boolean CanmButtonSearchTieUpGroupClicked()
		{
			return HasTieUpGroup;
		}

		public void ButtonSearchTieUpGroupClicked()
		{
			try
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUpGroup> tieUpGroups);
				using SearchMasterWindowViewModel<TTieUpGroup> searchMasterWindowViewModel = new(tieUpGroups);
				searchMasterWindowViewModel.SelectedKeyword = HeadName(tieUpGroups, _tieUpGroupIds);
				Messenger.Raise(new TransitionMessage(searchMasterWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_SEARCH_MASTER_WINDOW));

				if (searchMasterWindowViewModel.OkSelectedMaster == null)
				{
					return;
				}

				_tieUpGroupIds = searchMasterWindowViewModel.OkSelectedMaster.Id;
				DbCommon.SetAvoidSameName(tieUpGroups, searchMasterWindowViewModel.OkSelectedMaster);
				TieUpGroupDisplayNames = searchMasterWindowViewModel.OkSelectedMaster.DisplayName;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "シリーズ検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 複数タイアップグループ検索ボタンの制御
		private ViewModelCommand? _buttonEditTieUpGroupClickedCommand;

		public ViewModelCommand ButtonEditTieUpGroupClickedCommand
		{
			get
			{
				if (_buttonEditTieUpGroupClickedCommand == null)
				{
					_buttonEditTieUpGroupClickedCommand = new ViewModelCommand(ButtonEditTieUpGroupClicked, CanButtonEditTieUpGroupClicked);
				}
				return _buttonEditTieUpGroupClickedCommand;
			}
		}

		public Boolean CanButtonEditTieUpGroupClicked()
		{
			return HasTieUpGroup;
		}

		public void ButtonEditTieUpGroupClicked()
		{
			try
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUpGroup> tieUpGroups);
				using EditTieUpGroupsWindowViewModel editTieUpGroupsWindowViewModel = new(_musicInfoContext, tieUpGroups);
				List<String> splitIds = YlCommon.SplitIds(_tieUpGroupIds);
				foreach (String id in splitIds)
				{
					TTieUpGroup? tieUpGroup = DbCommon.SelectBaseById(tieUpGroups, id);
					if (tieUpGroup != null)
					{
						editTieUpGroupsWindowViewModel.Masters.Add(tieUpGroup);
					}
				}
				Messenger.Raise(new TransitionMessage(editTieUpGroupsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_SEQUENCE_WINDOW));

				if (!editTieUpGroupsWindowViewModel.OkSelectedMasters.Any())
				{
					return;
				}
				(HasTieUpGroup, _tieUpGroupIds, TieUpGroupDisplayNames) = ConcatMasterIdsAndNames(tieUpGroups, editTieUpGroupsWindowViewModel.OkSelectedMasters);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "複数シリーズ検索ボタンクリック時エラー：\n" + excep.Message);
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
				// 年齢制限選択ボタンのコンテキストメニュー
				AddContextMenuItemToButtonSelectAgeLimit("全年齢対象（CERO A 相当）");
				AddContextMenuItemToButtonSelectAgeLimit(YlConstants.AGE_LIMIT_CERO_B.ToString() + " 才以上対象（CERO B 相当）");
				AddContextMenuItemToButtonSelectAgeLimit(YlConstants.AGE_LIMIT_CERO_C.ToString() + " 才以上対象（CERO C 相当）");
				AddContextMenuItemToButtonSelectAgeLimit(YlConstants.AGE_LIMIT_CERO_D.ToString() + " 才以上対象（CERO D 相当）");
				AddContextMenuItemToButtonSelectAgeLimit(YlConstants.AGE_LIMIT_CERO_Z.ToString() + " 才以上対象（CERO Z 相当）");
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイアップ詳細情報編集ウィンドウ初期化時エラー：\n" + excep.Message);
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
			if (HasMaker && String.IsNullOrEmpty(_makerId))
			{
				throw new Exception("制作会社が「あり」になっていますが指定されていません。");
			}
			List<String> tieUpGroupIds = YlCommon.SplitIds(_tieUpGroupIds);
			if (HasTieUpGroup && tieUpGroupIds.Count == 0)
			{
				throw new Exception("シリーズが「あり」になっていますが指定されていません。");
			}
		}

		// --------------------------------------------------------------------
		// プロパティーの内容を Master に格納
		// --------------------------------------------------------------------
		protected override void PropertiesToRecord(TTieUp master)
		{
			base.PropertiesToRecord(master);

			// TTieUp
			master.MakerId = _makerId;
			master.AgeLimit = Common.StringToInt32(AgeLimit);
		}

		// --------------------------------------------------------------------
		// Master の内容をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void RecordToProperties(TTieUp master)
		{
			base.RecordToProperties(master);

			// 年齢制限
			if (master.AgeLimit == 0)
			{
				AgeLimit = null;
			}
			else
			{
				AgeLimit = master.AgeLimit.ToString();
			}

			// 制作会社
			if (String.IsNullOrEmpty(master.MakerId))
			{
				HasMaker = false;
			}
			else
			{
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TMaker> makers);
				HasMaker = true;
				TMaker? maker = DbCommon.SelectBaseById(makers, master.MakerId);
				if (maker != null)
				{
					SetMaker(makers, maker);
				}
				else
				{
					_makerId = null;
					MakerDisplayName = null;
				}
			}

			// タイアップグループ
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUpGroup> tieUpGroups);
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUpGroupSequence> tieUpGroupSequences);
			List<TTieUpGroup> SequencedTieUpGroups = DbCommon.SelectSequencedTieUpGroupsByTieUpId(tieUpGroupSequences, tieUpGroups, master.Id);
			if (SequencedTieUpGroups.Count == 0)
			{
				HasTieUpGroup = false;
			}
			else
			{
				(HasTieUpGroup, _tieUpGroupIds, TieUpGroupDisplayNames) = ConcatMasterIdsAndNames(tieUpGroups, SequencedTieUpGroups);
			}
		}

		// --------------------------------------------------------------------
		// レコード保存
		// --------------------------------------------------------------------
		protected override void Save(TTieUp master)
		{
			if (master.Id == NewIdForDisplay())
			{
				// 新規登録
				AddNewRecord(master);
			}
			else
			{
				TTieUp? existRecord = DbCommon.SelectBaseById(_records, master.Id, true);
				if (existRecord == null)
				{
					throw new Exception("更新対象のタイアップレコードが見つかりません：" + master.Id);
				}
				if (DbCommon.IsRcTieUpUpdated(existRecord, master))
				{
					// 更新（既存のレコードが無効化されている場合は有効化も行う）
					UpdateExistRecord(existRecord, master);
				}
			}

			// タイアップグループ紐付け
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TTieUpGroupSequence> tieUpGroupSequences);
			DbCommon.RegisterSequence<TTieUpGroupSequence>(tieUpGroupSequences, master.Id, YlCommon.SplitIds(_tieUpGroupIds));
			_musicInfoContext.SaveChanges();
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 制作会社 ID
		private String? _makerId;

		// タイアップグループ ID（カンマ区切りで複数）
		private String? _tieUpGroupIds;

		// 制作会社を検索したかどうか
		private Boolean _isMakerSearched;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ButtonSelectAgeLimit のコンテキストメニューにアイテムを追加
		// --------------------------------------------------------------------
		private void AddContextMenuItemToButtonSelectAgeLimit(String label)
		{
			YlCommon.AddContextMenuItem(ContextMenuButtonSelectAgeLimitItems, label, ContextMenuButtonSelectAgeLimitItem_Click);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void ContextMenuButtonSelectAgeLimitItem_Click(Object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				MenuItem item = (MenuItem)sender;
				Int32 ageLimit = Common.StringToInt32((String)item.Header);
				if (ageLimit == 0)
				{
					AgeLimit = null;
				}
				else
				{
					AgeLimit = ageLimit.ToString();
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "年齢制限選択メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// MakerDisplayName は同名識別用に変更されている場合があるので _makerId から正式名称を取得する
		// --------------------------------------------------------------------
		private String? OriginalMakerName()
		{
			MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TMaker> makers);
			return DbCommon.SelectBaseById(makers, _makerId)?.Name;
		}

		// --------------------------------------------------------------------
		// _makerId などを refer に合わせて設定
		// --------------------------------------------------------------------
		private void SetMaker(DbSet<TMaker> makers, TMaker? refer)
		{
			if (refer == null)
			{
				return;
			}

			// ID
			_makerId = refer.Id;

			// 名前
			DbCommon.SetAvoidSameName(makers, refer);
			MakerDisplayName = refer.DisplayName;
		}
	}
}
