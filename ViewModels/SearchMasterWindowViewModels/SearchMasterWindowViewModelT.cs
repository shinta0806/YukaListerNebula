// ============================================================================
// 
// 楽曲情報データベースマスター検索ウィンドウの ViewModel
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.SearchMasterWindowViewModels
{
	public class SearchMasterWindowViewModel<T> : SearchMasterWindowViewModel where T : class, IRcMaster
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public SearchMasterWindowViewModel(DbSet<T> records, String? itemName = null)
		{
			_records = records;
			_itemName = itemName ?? YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()];
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 説明
		private String _description = String.Empty;
		public String Description
		{
			get => _description;
			set => RaisePropertyChangedIfSet(ref _description, value);
		}

		// 入力されたキーワード
		private String? _keyword;
		public String? Keyword
		{
			get => _keyword;
			set
			{
				if (RaisePropertyChangedIfSet(ref _keyword, value))
				{
					ButtonSearchClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 選択状態で入力されているキーワード
		private String? _selectedKeyword;
		public String? SelectedKeyword
		{
			get => _selectedKeyword;
			set => RaisePropertyChangedIfSet(ref _selectedKeyword, value);
		}

		// キーワードフォーカス
		private Boolean _isKeywordFocused;
		public Boolean IsKeywordFocused
		{
			get => _isKeywordFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isKeywordFocused = value;
				RaisePropertyChanged(nameof(IsKeywordFocused));
			}
		}

		// 検索結果の説明
		private String _foundsDescription = String.Empty;
		public String FoundsDescription
		{
			get => _foundsDescription;
			set => RaisePropertyChangedIfSet(ref _foundsDescription, value);
		}

		// 検索結果
		private List<T> _founds = new();
		public List<T> Founds
		{
			get => _founds;
			set => RaisePropertyChangedIfSet(ref _founds, value);
		}

		// 検索結果フォーカス
		private Boolean _areFoundsFocused;
		public Boolean AreFoundsFocused
		{
			get => _areFoundsFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_areFoundsFocused = value;
				RaisePropertyChanged(nameof(AreFoundsFocused));
			}
		}

		// 選択された検索結果
		private T? _selectedFound;
		public T? SelectedFound
		{
			get => _selectedFound;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedFound, value))
				{
					ButtonSelectClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// 選択ボタンで選択されたマスター
		public T? OkSelectedMaster { get; private set; }

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 検索ボタンの制御
		private ViewModelCommand? _buttonSearchClickedCommand;

		public ViewModelCommand ButtonSearchClickedCommand
		{
			get
			{
				if (_buttonSearchClickedCommand == null)
				{
					_buttonSearchClickedCommand = new ViewModelCommand(ButtonSearchClicked, CanButtonSearchClicked);
				}
				return _buttonSearchClickedCommand;
			}
		}

		public Boolean CanButtonSearchClicked()
		{
			return !_isSearching && !String.IsNullOrEmpty(YlCommon.NormalizeDbString(Keyword));
		}

		public async void ButtonSearchClicked()
		{
			try
			{
				String? normalizedKeyword = YlCommon.NormalizeDbString(Keyword);
				if (String.IsNullOrEmpty(normalizedKeyword))
				{
					throw new Exception("キーワードを入力してください。");
				}

				_isSearching = true;
				ButtonSearchClickedCommand.RaiseCanExecuteChanged();
				Cursor = Cursors.Wait;
				Founds = new();
				ClearLabelFounds();

				// 検索
				Founds = await SearchAsync(normalizedKeyword);
				if (Founds.Count == 0)
				{
					throw new Exception("「" + normalizedKeyword + "」を含む" + _itemName + "はありません。");
				}
				FoundsDescription = Founds.Count.ToString("#,0") + " 個の結果が見つかりました。";
				AreFoundsFocused = true;

				// リストボックスの選択
				SetSelectedFound(normalizedKeyword);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "検索時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				Cursor = null;
				_isSearching = false;
				ButtonSearchClickedCommand.RaiseCanExecuteChanged();
			}
		}
		#endregion

		#region DataGrid ダブルクリックの制御
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
				Select();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "DataGrid ダブルクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 選択ボタンの制御
		private ViewModelCommand? mButtonSelectClickedCommand;

		public ViewModelCommand ButtonSelectClickedCommand
		{
			get
			{
				if (mButtonSelectClickedCommand == null)
				{
					mButtonSelectClickedCommand = new ViewModelCommand(ButtonSelectClicked, CanButtonSelectClicked);
				}
				return mButtonSelectClickedCommand;
			}
		}

		public Boolean CanButtonSelectClicked()
		{
			return SelectedFound != null;
		}

		public void ButtonSelectClicked()
		{
			try
			{
				Select();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "選択ボタンクリック時エラー：\n" + excep.Message);
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
				Title = _itemName + "を検索";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
				// 説明
				Description = _itemName + "を、既に登録されている情報から検索します。";

				// フォーカス
				IsKeywordFocused = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲情報データベースマスター検索ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 検索対象データベースレコード
		private DbSet<T> _records;

		// 検索項目名
		private String _itemName;

		// 検索中
		private Boolean _isSearching;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 検索結果ソート用関数（大文字小文字を区別しない名前順）
		// --------------------------------------------------------------------
		private static Int32 SearchResultComparison(T lhs, T rhs)
		{
			if (String.IsNullOrEmpty(lhs.Name))
			{
				// 左側が空
				if (String.IsNullOrEmpty(rhs.Name))
				{
					return 0;
				}
				else
				{
					return 1;
				}
			}
			else
			{
				// 左側が空ではない
				if (String.IsNullOrEmpty(rhs.Name))
				{
					return -1;
				}
				else
				{
					return String.Compare(lhs.Name, rhs.Name, true);
				}
			}
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// LabelFounds を空にする
		// --------------------------------------------------------------------
		private void ClearLabelFounds()
		{
			// null にするとラベルの高さが変わってしまうため Empty にする
			FoundsDescription = String.Empty;
		}

		// --------------------------------------------------------------------
		// 検索
		// --------------------------------------------------------------------
		private async Task<List<T>> SearchAsync(String normalizedKeyword)
		{
			List<T>? results = null;
			await Task.Run(() =>
			{
				results = _records.AsNoTracking().Where(x => !x.Invalid).ToList();

				// スペース等区切りで AND 検索
				String[] split = normalizedKeyword.Split(new Char[] { ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_', '{', '|', '}', '～' },
						StringSplitOptions.RemoveEmptyEntries);
				if (split.Length == 0)
				{
					// 区切り文字のみの場合はスペースのみ区切りで AND 検索
					split = normalizedKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				}
				foreach (String oneWord in split)
				{
					String? oneRuby = YlCommon.NormalizeDbRubyForSearch(oneWord);
					Boolean isOneWordRuby = !String.IsNullOrEmpty(oneRuby) && oneRuby.Length == oneWord.Length;

					// EF Core レコード直接の場合は、String.Contains() が StringComparison.OrdinalIgnoreCase 付きで動作しないため、EF.Functions.Like() を使う
					// ここでは一度 List に変換しているので String.Contains() を使う
					results = results.Where(x =>
							(x.Name?.Contains(oneWord, StringComparison.OrdinalIgnoreCase) ?? false)
							|| (x.Keyword?.Contains(oneWord, StringComparison.OrdinalIgnoreCase) ?? false)
							// すべてフリガナとして使える文字が入力された場合は、フリガナでも検索
							|| isOneWordRuby && (x.RubyForSearch?.Contains(oneRuby!, StringComparison.OrdinalIgnoreCase) ?? false)
							|| isOneWordRuby && (x.Keyword?.Contains(oneRuby!, StringComparison.OrdinalIgnoreCase) ?? false)
							|| isOneWordRuby && (x.KeywordRubyForSearch?.Contains(oneRuby!, StringComparison.OrdinalIgnoreCase) ?? false)
					).ToList();
				}

				// ソート
				results.Sort(SearchResultComparison);

				// 同名検出
				// ソート済みのため簡単に検出できるため、DbCommon.SetAvoidSameName() よりも高速と思う
				for (Int32 i = 1; i < results.Count; i++)
				{
					if (results[i].Name == results[i - 1].Name)
					{
						results[i].AvoidSameName = true;
						results[i - 1].AvoidSameName = true;
					}
				}
#if DEBUGz
				Thread.Sleep(2000);
#endif
			});
			if (results == null)
			{
				results = new();
			}
			return results;
		}

		// --------------------------------------------------------------------
		// 選択中のアイテムで決定
		// --------------------------------------------------------------------
		private void Select()
		{
			if (SelectedFound == null)
			{
				return;
			}

			IsOk = true;
			OkSelectedMaster = SelectedFound;
			Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
		}

		// --------------------------------------------------------------------
		// 検索結果の中から最も適切なものを選択
		// --------------------------------------------------------------------
		private void SetSelectedFound(String normalizedKeyword)
		{
			if (Founds.Count == 0)
			{
				return;
			}

			// 選択（完全一致）
			SelectedFound = Founds.FirstOrDefault(x => x.Name == normalizedKeyword);
			if (SelectedFound != null)
			{
				return;
			}

			// 選択（大文字小文字を区別しない）
			SelectedFound = Founds.FirstOrDefault(x => String.Compare(x.Name, normalizedKeyword, true) == 0);
			if (SelectedFound != null)
			{
				return;
			}

			// ルビ
			String? rubyForSearch = YlCommon.NormalizeDbRubyForSearch(normalizedKeyword);
			if (!String.IsNullOrEmpty(rubyForSearch) && rubyForSearch.Length == normalizedKeyword.Length)
			{
				SelectedFound = Founds.FirstOrDefault(x => x.RubyForSearch == rubyForSearch);
				if (SelectedFound != null)
				{
					return;
				}
			}

			// 先頭を選択
			SelectedFound = Founds[0];
		}
	}
}
