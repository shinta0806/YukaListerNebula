// ============================================================================
// 
// 楽曲情報データベースマスター検索ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

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
				: base(itemName ?? YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()])
		{
			_records = records;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 検索結果
		private List<T> _founds = new();
		public List<T> Founds
		{
			get => _founds;
			set => RaisePropertyChangedIfSet(ref _founds, value);
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

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー：検索ボタンがクリックされた
		// --------------------------------------------------------------------
		public override async void ButtonSearchClicked()
		{
			try
			{
				String? normalizedKeyword = YlCommon.NormalizeDbString(Keyword);
				if (String.IsNullOrEmpty(normalizedKeyword))
				{
					return;
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

		// --------------------------------------------------------------------
		// イベントハンドラー：選択ボタンがクリックされた
		// --------------------------------------------------------------------
		public override void ButtonSelectClicked()
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

		// --------------------------------------------------------------------
		// イベントハンドラー：選択ボタンが有効かどうかの判定
		// --------------------------------------------------------------------
		public override Boolean CanButtonSelectClicked()
		{
			return SelectedFound != null;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：DataGrid がダブルクリックされた
		// --------------------------------------------------------------------
		public override void DataGridDoubleClicked()
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
		// 検索
		// --------------------------------------------------------------------
		private async Task<List<T>> SearchAsync(String normalizedKeyword)
		{
			List<T>? results = null;
			await Task.Run(() =>
			{
				String? ruby = YlCommon.NormalizeDbRubyForSearch(normalizedKeyword);
				Boolean isKeywordRuby = !String.IsNullOrEmpty(ruby) && ruby.Length == normalizedKeyword.Length;
				results = _records.Where(x =>
						(
							// EF Core では String.Contains() が StringComparison.OrdinalIgnoreCase 付きで動作しないため、EF.Functions.Like() を使う
							EF.Functions.Like(x.Name, $"%{normalizedKeyword}%")
							|| EF.Functions.Like(x.Keyword, $"%{normalizedKeyword}%")
							// すべてフリガナとして使える文字が入力された場合は、フリガナでも検索
							|| isKeywordRuby && EF.Functions.Like(x.RubyForSearch, $"%{ruby}%")
							|| isKeywordRuby && EF.Functions.Like(x.Keyword, $"%{ruby}%")
							|| isKeywordRuby && EF.Functions.Like(x.KeywordRubyForSearch, $"%{ruby}%")
						)
						&& !x.Invalid
				).ToList();
				results.Sort(SearchResultComparison);
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

			// 先頭を選択
			SelectedFound = Founds[0];
		}
	}
}
