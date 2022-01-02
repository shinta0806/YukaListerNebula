// ============================================================================
// 
// ファイル一覧ウィンドウの ViewModel
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.OutputWriters;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.MiscWindowViewModels
{
	public class ViewTFoundsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ViewTFoundsWindowViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 列
		public ObservableCollection<DataGridColumn> Columns { get; set; } = new();

		// 選択行
		private TFound? _selectedFound;
		public TFound? SelectedFound
		{
			get => _selectedFound;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedFound, value))
				{
					ButtonEditMusicInfoClickedCommand.RaiseCanExecuteChanged();
					ButtonFolderSettingsClickedCommand.RaiseCanExecuteChanged();
					ButtonExplorerClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// カレントセル位置
		private Point _currentCellLocation;
		public Point CurrentCellLocation
		{
			get => _currentCellLocation;
			set => RaisePropertyChangedIfSet(ref _currentCellLocation, value);
		}

		// ファイル群
		private List<TFound> _founds = new();
		public List<TFound> Founds
		{
			get => _founds;
			set => RaisePropertyChangedIfSet(ref _founds, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YukaListerModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

		#region データグリッドダブルクリックの制御
		private ViewModelCommand? _dataGridDoubleClickedCommand;

		public ViewModelCommand DataGridDoubleClickedCommand
		{
			get
			{
				if (_dataGridDoubleClickedCommand == null)
				{
					_dataGridDoubleClickedCommand = new ViewModelCommand(DataGridDoubleClicked);
				}
				return _dataGridDoubleClickedCommand;
			}
		}

		public void DataGridDoubleClicked()
		{
			try
			{
				EditMusicInfo();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ソートの制御
		private ListenerCommand<DataGridSortingEventArgs>? _dataGridListSortingCommand;

		public ListenerCommand<DataGridSortingEventArgs> DataGridListSortingCommand
		{
			get
			{
				if (_dataGridListSortingCommand == null)
				{
					_dataGridListSortingCommand = new ListenerCommand<DataGridSortingEventArgs>(DataGridListSorting);
				}
				return _dataGridListSortingCommand;
			}
		}

		public void DataGridListSorting(DataGridSortingEventArgs dataGridSortingEventArgs)
		{
			try
			{
				TFound? prevSelectedTFound = SelectedFound;

				// 並び替えの方向（昇順か降順か）を決める
				ListSortDirection newDirection;
				if (dataGridSortingEventArgs.Column.SortDirection == ListSortDirection.Ascending)
				{
					newDirection = ListSortDirection.Descending;
				}
				else
				{
					newDirection = ListSortDirection.Ascending;
				}

				// データのソート
				if (newDirection == ListSortDirection.Ascending)
				{
					switch (_runtimeOutputItems[dataGridSortingEventArgs.Column.DisplayIndex])
					{
						case OutputItems.Path:
							Founds.Sort((x, y) => String.Compare(x.Path, y.Path, true));
							break;
						case OutputItems.FileName:
							Founds.Sort((x, y) => String.Compare(x.FileName, y.FileName, true));
							break;
						case OutputItems.Head:
							Founds.Sort((x, y) => String.Compare(x.Head, y.Head, true));
							break;
						case OutputItems.Worker:
							Founds.Sort((x, y) => String.Compare(x.Worker, y.Worker, true));
							break;
						case OutputItems.Track:
							Founds.Sort((x, y) => String.Compare(x.Track, y.Track, true));
							break;
						case OutputItems.SmartTrack:
							Founds.Sort((x, y) => SmartTrackToInt32(y) - SmartTrackToInt32(x));
							break;
						case OutputItems.Comment:
							Founds.Sort((x, y) => String.Compare(x.Comment, y.Comment, true));
							break;
						case OutputItems.LastWriteTime:
							Founds.Sort((x, y) => x.LastWriteTime.CompareTo(y.LastWriteTime));
							break;
						case OutputItems.FileSize:
							Founds.Sort((x, y) => x.FileSize.CompareTo(y.FileSize));
							break;
						case OutputItems.SongName:
							Founds.Sort((x, y) => String.Compare(x.SongName, y.SongName, true));
							break;
						case OutputItems.SongRuby:
							Founds.Sort((x, y) => String.Compare(x.SongRuby, y.SongRuby, true));
							break;
						case OutputItems.SongOpEd:
							Founds.Sort((x, y) => String.Compare(x.SongOpEd, y.SongOpEd, true));
							break;
						case OutputItems.SongReleaseDate:
							Founds.Sort((x, y) => x.SongReleaseDate.CompareTo(y.SongReleaseDate));
							break;
						case OutputItems.ArtistName:
							Founds.Sort((x, y) => String.Compare(x.ArtistName, y.ArtistName, true));
							break;
						case OutputItems.ArtistRuby:
							Founds.Sort((x, y) => String.Compare(x.ArtistRuby, y.ArtistRuby, true));
							break;
						case OutputItems.LyristName:
							Founds.Sort((x, y) => String.Compare(x.LyristName, y.LyristName, true));
							break;
						case OutputItems.LyristRuby:
							Founds.Sort((x, y) => String.Compare(x.LyristRuby, y.LyristRuby, true));
							break;
						case OutputItems.ComposerName:
							Founds.Sort((x, y) => String.Compare(x.ComposerName, y.ComposerName, true));
							break;
						case OutputItems.ComposerRuby:
							Founds.Sort((x, y) => String.Compare(x.ComposerRuby, y.ComposerRuby, true));
							break;
						case OutputItems.ArrangerName:
							Founds.Sort((x, y) => String.Compare(x.ArrangerName, y.ArrangerName, true));
							break;
						case OutputItems.ArrangerRuby:
							Founds.Sort((x, y) => String.Compare(x.ArrangerRuby, y.ArrangerRuby, true));
							break;
						case OutputItems.TieUpName:
							Founds.Sort((x, y) => String.Compare(x.TieUpName, y.TieUpName, true));
							break;
						case OutputItems.TieUpRuby:
							Founds.Sort((x, y) => String.Compare(x.TieUpRuby, y.TieUpRuby, true));
							break;
						case OutputItems.TieUpAgeLimit:
							Founds.Sort((x, y) => y.TieUpAgeLimit - x.TieUpAgeLimit);
							break;
						case OutputItems.Category:
							Founds.Sort((x, y) => String.Compare(x.Category, y.Category, true));
							break;
						case OutputItems.TieUpGroupName:
							Founds.Sort((x, y) => String.Compare(x.TieUpGroupName, y.TieUpGroupName, true));
							break;
						case OutputItems.TieUpGroupRuby:
							Founds.Sort((x, y) => String.Compare(x.TieUpGroupRuby, y.TieUpGroupRuby, true));
							break;
						case OutputItems.MakerName:
							Founds.Sort((x, y) => String.Compare(x.MakerName, y.MakerName, true));
							break;
						case OutputItems.MakerRuby:
							Founds.Sort((x, y) => String.Compare(x.MakerRuby, y.MakerRuby, true));
							break;
						default:
							Debug.Assert(false, "DataGridViewList_ColumnHeaderMouseClick() bad specified target item: " + _runtimeOutputItems[dataGridSortingEventArgs.Column.DisplayIndex].ToString());
							break;
					}
				}
				else
				{
					switch (_runtimeOutputItems[dataGridSortingEventArgs.Column.DisplayIndex])
					{
						case OutputItems.Path:
							Founds.Sort((x, y) => -String.Compare(x.Path, y.Path, true));
							break;
						case OutputItems.FileName:
							Founds.Sort((x, y) => -String.Compare(x.FileName, y.FileName, true));
							break;
						case OutputItems.Head:
							Founds.Sort((x, y) => -String.Compare(x.Head, y.Head, true));
							break;
						case OutputItems.Worker:
							Founds.Sort((x, y) => -String.Compare(x.Worker, y.Worker, true));
							break;
						case OutputItems.Track:
							Founds.Sort((x, y) => -String.Compare(x.Track, y.Track, true));
							break;
						case OutputItems.SmartTrack:
							Founds.Sort((x, y) => SmartTrackToInt32(x) - SmartTrackToInt32(y));
							break;
						case OutputItems.Comment:
							Founds.Sort((x, y) => -String.Compare(x.Comment, y.Comment, true));
							break;
						case OutputItems.LastWriteTime:
							Founds.Sort((x, y) => -x.LastWriteTime.CompareTo(y.LastWriteTime));
							break;
						case OutputItems.FileSize:
							Founds.Sort((x, y) => -x.FileSize.CompareTo(y.FileSize));
							break;
						case OutputItems.SongName:
							Founds.Sort((x, y) => -String.Compare(x.SongName, y.SongName, true));
							break;
						case OutputItems.SongRuby:
							Founds.Sort((x, y) => -String.Compare(x.SongRuby, y.SongRuby, true));
							break;
						case OutputItems.SongOpEd:
							Founds.Sort((x, y) => -String.Compare(x.SongOpEd, y.SongOpEd, true));
							break;
						case OutputItems.SongReleaseDate:
							Founds.Sort((x, y) => -x.SongReleaseDate.CompareTo(y.SongReleaseDate));
							break;
						case OutputItems.ArtistName:
							Founds.Sort((x, y) => -String.Compare(x.ArtistName, y.ArtistName, true));
							break;
						case OutputItems.ArtistRuby:
							Founds.Sort((x, y) => -String.Compare(x.ArtistRuby, y.ArtistRuby, true));
							break;
						case OutputItems.LyristName:
							Founds.Sort((x, y) => -String.Compare(x.LyristName, y.LyristName, true));
							break;
						case OutputItems.LyristRuby:
							Founds.Sort((x, y) => -String.Compare(x.LyristRuby, y.LyristRuby, true));
							break;
						case OutputItems.ComposerName:
							Founds.Sort((x, y) => -String.Compare(x.ComposerName, y.ComposerName, true));
							break;
						case OutputItems.ComposerRuby:
							Founds.Sort((x, y) => -String.Compare(x.ComposerRuby, y.ComposerRuby, true));
							break;
						case OutputItems.ArrangerName:
							Founds.Sort((x, y) => -String.Compare(x.ArrangerName, y.ArrangerName, true));
							break;
						case OutputItems.ArrangerRuby:
							Founds.Sort((x, y) => -String.Compare(x.ArrangerRuby, y.ArrangerRuby, true));
							break;
						case OutputItems.TieUpName:
							Founds.Sort((x, y) => -String.Compare(x.TieUpName, y.TieUpName, true));
							break;
						case OutputItems.TieUpRuby:
							Founds.Sort((x, y) => -String.Compare(x.TieUpRuby, y.TieUpRuby, true));
							break;
						case OutputItems.TieUpAgeLimit:
							Founds.Sort((x, y) => x.TieUpAgeLimit - y.TieUpAgeLimit);
							break;
						case OutputItems.Category:
							Founds.Sort((x, y) => -String.Compare(x.Category, y.Category, true));
							break;
						case OutputItems.TieUpGroupName:
							Founds.Sort((x, y) => -String.Compare(x.TieUpGroupName, y.TieUpGroupName, true));
							break;
						case OutputItems.TieUpGroupRuby:
							Founds.Sort((x, y) => -String.Compare(x.TieUpGroupRuby, y.TieUpGroupRuby, true));
							break;
						case OutputItems.MakerName:
							Founds.Sort((x, y) => -String.Compare(x.MakerName, y.MakerName, true));
							break;
						case OutputItems.MakerRuby:
							Founds.Sort((x, y) => -String.Compare(x.MakerRuby, y.MakerRuby, true));
							break;
						default:
							Debug.Assert(false, "DataGridViewList_ColumnHeaderMouseClick() bad specified target item: " + _runtimeOutputItems[dataGridSortingEventArgs.Column.DisplayIndex].ToString());
							break;
					}
				}

				// 結果の表示
				List<TFound> tmp = Founds;
				Founds = new();
				Founds = tmp;
				SelectedFound = prevSelectedTFound;

				// 並び替えグリフの表示
				dataGridSortingEventArgs.Column.SortDirection = newDirection;

				dataGridSortingEventArgs.Handled = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ヘッダークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 編集ボタンの制御
		private ViewModelCommand? _buttonEditMusicInfoClickedCommand;

		public ViewModelCommand ButtonEditMusicInfoClickedCommand
		{
			get
			{
				if (_buttonEditMusicInfoClickedCommand == null)
				{
					_buttonEditMusicInfoClickedCommand = new ViewModelCommand(ButtonEditMusicInfoClicked, CanButtonEditMusicInfoClicked);
				}
				return _buttonEditMusicInfoClickedCommand;
			}
		}

		public Boolean CanButtonEditMusicInfoClicked()
		{
			return SelectedFound != null;
		}

		public void ButtonEditMusicInfoClicked()
		{
			try
			{
				EditMusicInfo();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー設定ボタンの制御
		private ViewModelCommand? _buttonFolderSettingsClickedCommand;

		public ViewModelCommand ButtonFolderSettingsClickedCommand
		{
			get
			{
				if (_buttonFolderSettingsClickedCommand == null)
				{
					_buttonFolderSettingsClickedCommand = new ViewModelCommand(ButtonFolderSettingsClicked, CanButtonFolderSettingsClicked);
				}
				return _buttonFolderSettingsClickedCommand;
			}
		}

		public Boolean CanButtonFolderSettingsClicked()
		{
			return SelectedFound != null;
		}

		public void ButtonFolderSettingsClicked()
		{
			try
			{
				if (SelectedFound == null)
				{
					return;
				}
				CloseFindKeywordWindowIfNeeded();

				// 設定ファイルがあるフォルダー（設定ファイルが無い場合はファイルのフォルダー）
				String? folder = Path.GetDirectoryName(SelectedFound.Path);
				if (String.IsNullOrEmpty(folder))
				{
					return;
				}
				String? settingsFolder = YlCommon.FindSettingsFolder(folder);
				if (String.IsNullOrEmpty(settingsFolder))
				{
					settingsFolder = folder;
				}

				// ViewModel 経由でフォルダー設定ウィンドウを開く
				using FolderSettingsWindowViewModel folderSettingsWindowViewModel = new(settingsFolder);
				Messenger.Raise(new TransitionMessage(folderSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_FOLDER_SETTINGS_WINDOW));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region エクスプローラーボタンの制御
		private ViewModelCommand? buttonExplorerClickedCommand;

		public ViewModelCommand ButtonExplorerClickedCommand
		{
			get
			{
				if (buttonExplorerClickedCommand == null)
				{
					buttonExplorerClickedCommand = new ViewModelCommand(ButtonExplorerClicked, CanButtonExplorerClicked);
				}
				return buttonExplorerClickedCommand;
			}
		}

		public Boolean CanButtonExplorerClicked()
		{
			return SelectedFound != null;
		}

		public void ButtonExplorerClicked()
		{
			try
			{
				if (SelectedFound == null)
				{
					return;
				}
				CloseFindKeywordWindowIfNeeded();

				YlCommon.OpenExplorer(SelectedFound.Path);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "エクスプローラーボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region キーワード検索ボタンの制御
		private ViewModelCommand? _buttonFindKeywordClickedCommand;

		public ViewModelCommand ButtonFindKeywordClickedCommand
		{
			get
			{
				if (_buttonFindKeywordClickedCommand == null)
				{
					_buttonFindKeywordClickedCommand = new ViewModelCommand(ButtonFindClicked);
				}
				return _buttonFindKeywordClickedCommand;
			}
		}

		public void ButtonFindClicked()
		{
			try
			{
				ShowFindKeywordWindow();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "キーワード検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 空きセル・入力済みセル検索ボタンの制御
		private ListenerCommand<String>? _buttonFindCellClickedCommand;

		public ListenerCommand<String> ButtonFindCellClickedCommand
		{
			get
			{
				if (_buttonFindCellClickedCommand == null)
				{
					_buttonFindCellClickedCommand = new ListenerCommand<String>(ButtonFindEmptyCellClicked);
				}
				return _buttonFindCellClickedCommand;
			}
		}

		public void ButtonFindEmptyCellClicked(String parameter)
		{
			try
			{
				FindEmptyOrNonEmptyCell(String.IsNullOrEmpty(parameter));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "セル検索ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// キーワード検索が要求された
		// --------------------------------------------------------------------
		public void FindKeywordRequested()
		{
			try
			{
				FindKeyword();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "検索時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				// タイトルバー
				Title = "ゆかり検索対象ファイル一覧";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
#if TEST
				Title = "［テスト］" + Title;
#endif

				// 出力項目
				OutputSettings outputSettings = new();
				outputSettings.Load();
				_runtimeOutputItems = outputSettings.RuntimeOutputItems();

				// カラム作成
				foreach (OutputItems outputItem in _runtimeOutputItems)
				{
					DataGridTextColumn column = new();
					column.Binding = new Binding(outputItem.ToString());
					if (outputItem == OutputItems.SmartTrack)
					{
						column.Header = "On/Off";
					}
					else
					{
						column.Header = YlConstants.OUTPUT_ITEM_NAMES[(Int32)outputItem];
					}
					Columns.Add(column);
				}

				// データベース読み込み
				// キャッシュも利用できるよう、メモリではなくディスクの方を使用する
				using ListContextInDisk listContextInDisk = new();
				listContextInDisk.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				Founds = listContextInDisk.Founds.ToList();

				// カーソルを左上にする（変更検知のため一旦ダミーを設定する）
				CurrentCellLocation = new Point(1, 0);
				CurrentCellLocation = new Point(0, 0);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "環境設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 出力項目
		List<OutputItems> _runtimeOutputItems = new();

		// 検索ウィンドウのビューモデル
		FindKeywordWindowViewModel? _findKeywordWindowViewModel;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// SmartTrackOnVocal / SmartTrackOffVocal を数値化
		// --------------------------------------------------------------------
		private static Int32 SmartTrackToInt32(TFound found)
		{
			return (found.SmartTrackOnVocal ? 2 : 0) + (found.SmartTrackOffVocal ? 1 : 0);
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// セルに表示されている値
		// x, y の範囲チェックはしない（呼びだし元でチェック済みである必要がある）
		// --------------------------------------------------------------------
		private String? CellValue(Int32 x, Int32 y)
		{
			PropertyInfo? propertyInfo = typeof(TFound).GetProperty(_runtimeOutputItems[x].ToString());
			Object? val = propertyInfo?.GetValue(Founds[y]);
			return val?.ToString();
		}

		// --------------------------------------------------------------------
		// 必要に応じて検索ウィンドウを閉じる
		// --------------------------------------------------------------------
		private void CloseFindKeywordWindowIfNeeded()
		{
			if (_findKeywordWindowViewModel != null && !_findKeywordWindowViewModel.IsClosed)
			{
				_findKeywordWindowViewModel.Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
		}

		// --------------------------------------------------------------------
		// 指定された TFound に対して編集ウィンドウを開く
		// --------------------------------------------------------------------
		private void EditMusicInfo()
		{
			if (SelectedFound == null)
			{
				return;
			}
			CloseFindKeywordWindowIfNeeded();

			String path = SelectedFound.Path;

			// キャッシュデータを基にしている場合があるため、ファイルが既に存在しない場合がある
			if (!File.Exists(path))
			{
				throw new Exception("ファイルが存在しません。\n前回起動以降にファイルが削除・リネームされた可能性があります。\n" + path + "\nフォルダー整理完了後にファイル一覧ウィンドウを開き直してください。\n");
			}

			// ファイル命名規則とフォルダー固定値を適用
			Dictionary<String, String?> dicByFile = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(path);

			// 楽曲名が取得できていない場合は編集不可
			if (String.IsNullOrEmpty(dicByFile[YlConstants.RULE_VAR_TITLE]))
			{
				throw new Exception("ファイル名から楽曲名を取得できていないため、編集できません。\nファイル命名規則を確認して下さい。");
			}

			// ViewModel 経由で名称の編集ウィンドウを開く
			using EditMusicInfoWindowViewModel editMusicInfoWindowViewModel = new(path);
			Messenger.Raise(new TransitionMessage(editMusicInfoWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_MUSIC_INFO_WINDOW));
		}

		// --------------------------------------------------------------------
		// 未登録または登録済みの項目を検索して選択
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void FindEmptyOrNonEmptyCell(Boolean findEmpty)
		{
			if (CurrentCellLocation.Y < 0 || CurrentCellLocation.Y >= Founds.Count
					|| CurrentCellLocation.X < 0 || CurrentCellLocation.X >= _runtimeOutputItems.Count)
			{
				throw new Exception("セルを選択して下さい。");
			}

			for (Int32 i = (Int32)CurrentCellLocation.Y + 1; i < Founds.Count; i++)
			{
				if (String.IsNullOrEmpty(CellValue((Int32)CurrentCellLocation.X, i)) == findEmpty)
				{
					// 発見
					CurrentCellLocation = new Point(CurrentCellLocation.X, i);
					return;
				}
			}

			throw new Exception("選択されたセルより下には、" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)_runtimeOutputItems[(Int32)CurrentCellLocation.X]] + "が空欄"
					+ (findEmpty ? "の" : "ではない") + "セルはありません。");
		}

		// --------------------------------------------------------------------
		// キーワード検索ウィンドウの情報を元に検索
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void FindKeyword()
		{
			if (_findKeywordWindowViewModel == null)
			{
				throw new Exception("内部エラー：FindKeyword()");
			}
			String? keyword = _findKeywordWindowViewModel.Keyword?.Trim();
			if (String.IsNullOrEmpty(keyword))
			{
				throw new Exception("キーワードが指定されていません。");
			}

			Int32 beginRowIndex = (Int32)CurrentCellLocation.Y;
			Int32 direction = _findKeywordWindowViewModel.Direction;
			Debug.Assert(direction == 1 || direction == -1, "FindKeyword() direction not set");
			if (direction == 1)
			{
				if (beginRowIndex < 0)
				{
					beginRowIndex = 0;
				}
			}
			else
			{
				if (beginRowIndex < 0)
				{
					beginRowIndex = Founds.Count - 1;
				}
			}

			for (Int32 i = beginRowIndex; direction == 1 ? i < Founds.Count : i >= 0; i += direction)
			{
				Int32 beginColumnIndex;
				if (i == beginRowIndex)
				{
					beginColumnIndex = (Int32)CurrentCellLocation.X + direction;
				}
				else
				{
					if (direction == 1)
					{
						beginColumnIndex = 0;
					}
					else
					{
						beginColumnIndex = _runtimeOutputItems.Count - 1;
					}
				}

				for (Int32 j = beginColumnIndex; direction == 1 ? j < _runtimeOutputItems.Count : j >= 0; j += direction)
				{
					if (_findKeywordWindowViewModel.WholeMatch)
					{
						if (String.Compare(CellValue(j, i), keyword, !_findKeywordWindowViewModel.CaseSensitive) == 0)
						{
							// 発見
							CurrentCellLocation = new Point(j, i);
							return;
						}
					}
					else
					{
						if (!String.IsNullOrEmpty(CellValue(j, i))
								&& CellValue(j, i)?.IndexOf(keyword,
								_findKeywordWindowViewModel.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0)
						{
							// 発見
							CurrentCellLocation = new Point(j, i);
							return;
						}
					}
				}
			}

			throw new Exception("キーワード「" + keyword + "」は\n見つかりませんでした。");
		}

		// --------------------------------------------------------------------
		// 検索ウィンドウを表示する
		// --------------------------------------------------------------------
		private void ShowFindKeywordWindow()
		{
			if (_findKeywordWindowViewModel == null)
			{
				// 新規作成
				_findKeywordWindowViewModel = new(this);
				CompositeDisposable.Add(_findKeywordWindowViewModel);
				Messenger.Raise(new TransitionMessage(_findKeywordWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_FIND_KEYWORD_WINDOW));
			}
			else if (_findKeywordWindowViewModel.IsClosed)
			{
				// 閉じられたウィンドウからプロパティーを引き継ぐ
				FindKeywordWindowViewModel old = _findKeywordWindowViewModel;
				_findKeywordWindowViewModel = new(this);
				_findKeywordWindowViewModel.CopyFrom(old);
				CompositeDisposable.Remove(old);
				old.Dispose();
				CompositeDisposable.Add(_findKeywordWindowViewModel);
				Messenger.Raise(new TransitionMessage(_findKeywordWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_FIND_KEYWORD_WINDOW));
			}

			// ウィンドウを前面に出すなど
			_findKeywordWindowViewModel.Messenger.Raise(new InteractionMessage(YlConstants.MESSAGE_KEY_WINDOW_ACTIVATE));
		}
	}
}
