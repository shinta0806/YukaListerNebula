// ============================================================================
// 
// 楽曲情報データベースマスター検索ウィンドウの ViewModel（Visual Studio 用）
// 
// ============================================================================

// ----------------------------------------------------------------------------
// Visual Studio の XAML エディタは ViewModel がジェネリックだと表示できないため、
// 非ジェネリックの本クラスを作成。
// プログラム中では本クラスではなく SearchMasterWindowViewModel<T> を使うこと。
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// IRcMaster に無関係の変数は極力 SearchMasterWindowViewModel で宣言する。
// IRcMaster の派生で外部との通信をするための変数は派生型の方が便利なので SearchMasterWindowViewModel<T> で宣言する。
// ----------------------------------------------------------------------------

using Livet.Commands;

using System;

using YukaLister.Models.SharedMisc;

namespace YukaLister.ViewModels.SearchMasterWindowViewModels
{
	public class SearchMasterWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public SearchMasterWindowViewModel(String itemName)
		{
			_itemName = itemName;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public SearchMasterWindowViewModel()
		{
			_itemName = String.Empty;
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

		// 検索結果は SearchMasterWindowViewModel<T> で宣言

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

		// 選択された検索結果は SearchMasterWindowViewModel<T> で宣言

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

		public virtual void ButtonSearchClicked()
		{
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

		public virtual void DataGridDoubleClicked()
		{
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

		public virtual Boolean CanButtonSelectClicked()
		{
			return false;
		}

		public virtual void ButtonSelectClicked()
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
		}

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// 検索項目名
		protected String _itemName;

		// 検索中
		protected Boolean _isSearching;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// LabelFounds を空にする
		// --------------------------------------------------------------------
		protected void ClearLabelFounds()
		{
			// null にするとラベルの高さが変わってしまうため Empty にする
			FoundsDescription = String.Empty;
		}
	}
}
