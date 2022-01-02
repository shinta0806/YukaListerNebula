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
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditCategorizableWindowViewModel<T> : EditMasterWindowViewModel<T> where T : class, IRcCategorizable, new()
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EditCategorizableWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<T> records)
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
		public List<Control> ContextMenuButtonSelectCategoryItems { get; set; } = new();

		// カテゴリー名
		private String? _categoryName;
		public String? CategoryName
		{
			get => _categoryName;
			set => RaisePropertyChangedIfSet(ref _categoryName, value);
		}

		// リリース年月日
		private DateTime? _releaseDate;
		public DateTime? ReleaseDate
		{
			get => _releaseDate;
			set
			{
				if (RaisePropertyChangedIfSet(ref _releaseDate, value))
				{
					if (_releaseDate == null)
					{
						DayOfWeek = null;
					}
					else
					{
						DayOfWeek = _releaseDate?.ToString(YlConstants.DAY_OF_WEEK_FORMAT);
					}
				}
			}
		}

		// リリース年月日の曜日
		private String? _dayOfWeek;
		public String? DayOfWeek
		{
			get => _dayOfWeek;
			set => RaisePropertyChangedIfSet(ref _dayOfWeek, value);
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
		// public 関数
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
		// protected 関数
		// ====================================================================

		// カテゴリー ID
		protected String? _categoryId;

		// ====================================================================
		// protected 関数
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
		protected (Boolean has, String? ids, String? displayNames) ConcatMasterIdsAndNames<U>(DbSet<U> records, List<U> targetMasters) where U : class, IRcMaster
		{
			String ids = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], targetMasters.Select(x => x.Id));
			foreach (U master in targetMasters)
			{
				DbCommon.SetAvoidSameName(records, master);
			}
			String displayNames = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], targetMasters.Select(x => x.DisplayName));
			return (!String.IsNullOrEmpty(ids), String.IsNullOrEmpty(ids) ? null : ids, String.IsNullOrEmpty(displayNames) ? null : displayNames);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：HasCategory が変更された
		// --------------------------------------------------------------------
		protected virtual void HasCategoryChanged()
		{
		}

		// --------------------------------------------------------------------
		// カンマ区切りで連結されている ids のうち先頭の id から名前を取得
		// --------------------------------------------------------------------
		protected String? HeadName<U>(DbSet<U> records, String? ids) where U : class, IRcMaster
		{
			if (String.IsNullOrEmpty(ids))
			{
				return null;
			}
			return DbCommon.SelectBaseById(records, ids.Split(YlConstants.VAR_VALUE_DELIMITER[0], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())?.Name;
		}

		// --------------------------------------------------------------------
		// プロパティーの内容を Master に格納
		// --------------------------------------------------------------------
		protected override void PropertiesToRecord(T master)
		{
			base.PropertiesToRecord(master);

			// IRcCategorizable
			master.CategoryId = _categoryId;
			master.ReleaseDate = ReleaseDate == null ? YlConstants.INVALID_MJD : JulianDay.DateTimeToModifiedJulianDate(ReleaseDate.Value);
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

				TCategory? category = DbCommon.SelectBaseById(_musicInfoContext.Categories, master.CategoryId);
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
			ReleaseDate = master.ReleaseDate <= YlConstants.INVALID_MJD ? null : JulianDay.ModifiedJulianDateToDateTime(master.ReleaseDate);
		}

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void ContextMenuButtonSelectCategoryItem_Click(Object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				MenuItem item = (MenuItem)sender;
				TCategory? category = DbCommon.SelectMasterByName(_musicInfoContext.Categories, (String)item.Header);
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
