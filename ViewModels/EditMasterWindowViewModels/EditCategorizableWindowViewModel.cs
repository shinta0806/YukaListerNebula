// ============================================================================
// 
// 楽曲情報データベースカテゴリー持ち詳細編集ウィンドウの ViewModel 基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;

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
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditCategorizableWindowViewModel<T> : EditMasterWindowViewModel<T> where T : class, IRcCategorizable, new()
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditCategorizableWindowViewModel(MusicInfoContext musicInfoContext, DbSet<T> records)
				: base(musicInfoContext, records)
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// カテゴリーあり
		private Boolean _hasCategory;
		public Boolean HasCategory
		{
			get => _hasCategory;
			set
			{
				if (RaisePropertyChangedIfSet(ref _hasCategory, value))
				{
					ButtonSelectCategoryClickedCommand.RaiseCanExecuteChanged();
					if (!_hasCategory)
					{
						_categoryId = null;
						CategoryName = null;
					}
					HasCategoryChanged();
				}
			}
		}

		// カテゴリー選択ボタンのコンテキストメニュー
		public List<MenuItem> ContextMenuButtonSelectCategoryItems { get; set; } = new();

		// カテゴリー名
		private String? _categoryName;
		public String? CategoryName
		{
			get => _categoryName;
			set => RaisePropertyChangedIfSet(ref _categoryName, value);
		}

		// リリース年
		private String? _releaseYear;
		public String? ReleaseYear
		{
			get => _releaseYear;
			set => RaisePropertyChangedIfSet(ref _releaseYear, value);
		}

		// リリース月
		private String? _releaseMonth;
		public String? ReleaseMonth
		{
			get => _releaseMonth;
			set => RaisePropertyChangedIfSet(ref _releaseMonth, value);
		}

		// リリース日
		private String? _releaseDay;
		public String? ReleaseDay
		{
			get => _releaseDay;
			set => RaisePropertyChangedIfSet(ref _releaseDay, value);
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region カテゴリー選択ボタンの制御
		private ViewModelCommand? _buttonSelectCategoryClickedCommand;

		public ViewModelCommand ButtonSelectCategoryClickedCommand
		{
			get
			{
				if (_buttonSelectCategoryClickedCommand == null)
				{
					_buttonSelectCategoryClickedCommand = new ViewModelCommand(ButtonSelectCategoryClicked, CanButtonSelectCategoryClicked);
				}
				return _buttonSelectCategoryClickedCommand;
			}
		}

		public Boolean CanButtonSelectCategoryClicked()
		{
			return HasCategory;
		}

		public void ButtonSelectCategoryClicked()
		{
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
				// カテゴリー選択ボタンのコンテキストメニュー
				YlCommon.SetContextMenuItemCategories(ContextMenuButtonSelectCategoryItems, ContextMenuButtonSelectCategoryItem_Click);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "カテゴリー持ち詳細編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// カテゴリー ID
		protected String? _categoryId;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力値を確認する
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		protected override void CheckInput()
		{
			base.CheckInput();

			// チェックされているのに指定されていない項目を確認
			if (HasCategory && String.IsNullOrEmpty(_categoryId))
			{
				throw new Exception("カテゴリーが「あり」になっていますが指定されていません。");
			}
		}

		// --------------------------------------------------------------------
		// 複数の IRcMaster の ID と名前をカンマで結合
		// --------------------------------------------------------------------
		protected (Boolean has, String? ids, String? names) ConcatMasterIdsAndNames<U>(List<U> masters) where U : IRcMaster
		{
			String ids = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], masters.Select(x => x.Id));
			String names = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], masters.Select(x => x.Name));
			return (!String.IsNullOrEmpty(ids), String.IsNullOrEmpty(ids) ? null : ids, String.IsNullOrEmpty(names) ? null : names);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：HasCategory が変更された
		// --------------------------------------------------------------------
		protected virtual void HasCategoryChanged()
		{
		}

		// --------------------------------------------------------------------
		// プロパティーの内容を Master に格納
		// --------------------------------------------------------------------
		protected override void PropertiesToRecord(T master)
		{
			base.PropertiesToRecord(master);

			// IRcCategorizable
			master.CategoryId = _categoryId;
			master.ReleaseDate = YlCommon.StringsToMjd("リリース日", ReleaseYear, ReleaseMonth, ReleaseDay);
		}

		// --------------------------------------------------------------------
		// Master の内容をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void RecordToProperties(T master)
		{
			base.RecordToProperties(master);

			// カテゴリー関係
			if (String.IsNullOrEmpty(master.CategoryId))
			{
				HasCategory = false;
			}
			else
			{
				HasCategory = true;

				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TCategory> categories);
				TCategory? category = DbCommon.SelectBaseById(categories, master.CategoryId);
				if (category != null)
				{
					_categoryId = category.Id;
					CategoryName = category.Name;
				}
				else
				{
					_categoryId = null;
					CategoryName = null;
				}
			}

			// リリース日
			(ReleaseYear, ReleaseMonth, ReleaseDay) = YlCommon.MjdToStrings(master.ReleaseDate);
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void ContextMenuButtonSelectCategoryItem_Click(Object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				MenuItem item = (MenuItem)sender;
				MusicInfoContext.GetDbSet(_musicInfoContext, out DbSet<TCategory> categories);
				TCategory? category = DbCommon.SelectMasterByName(categories, (String)item.Header);
				if (category != null)
				{
					_categoryId = category.Id;
					CategoryName = category.Name;
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "カテゴリー選択メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
	}
}
