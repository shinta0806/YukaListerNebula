// ============================================================================
// 
// 楽曲情報データベースマスター詳細編集ウィンドウの ViewModel 基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 本クラスは EditMasterWindow を使わない。
// EditMakerWindowViewModel などの派生クラスが EditMasterWindow を使う。
// abstract にすると VisualStudio が EditMasterWindow のプレビューを表示しなくなるので通常のクラスにしておく。
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// IRcMaster に無関係の変数は極力 EditMasterWindowViewModel で宣言する。
// IRcMaster の派生で外部との通信をするための変数は派生型の方が便利なので EditMasterWindowViewModel<T> で宣言する。
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using Shinta;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using YukaLister.Models;
using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditMasterWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public EditMasterWindowViewModel(String caption, MusicInfoContext musicInfoContext)
		{
			_caption = caption;
			_musicInfoContext = musicInfoContext;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public EditMasterWindowViewModel()
		{
			_caption = String.Empty;
			_musicInfoContext = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ID キャプション
		private String? _idCaption;
		public String? IdCaption
		{
			get => _idCaption;
			set => RaisePropertyChangedIfSet(ref _idCaption, value);
		}

#if false
		// 選択可能な ID 群
		public ObservableCollection<String> Ids { get; set; } = new();

		// 選択された ID
		private String? _selectedId;
		public String? SelectedId
		{
			get => _selectedId;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedId, value))
				{
					SelectedIdChanged();
				}
			}
		}
#endif

		// ID の補足情報
		private String? _idInfo;
		public String? IdInfo
		{
			get => _idInfo;
			set => RaisePropertyChangedIfSet(ref _idInfo, value);
		}

		// フリガナ
		private String? _ruby;
		public String? Ruby
		{
			get => _ruby;
			set => RaisePropertyChangedIfSet(ref _ruby, value);
		}

		// 名前キャプション
		private String? _nameCaption;
		public String? NameCaption
		{
			get => _nameCaption;
			set => RaisePropertyChangedIfSet(ref _nameCaption, value);
		}

		// 名前
		private String? _name;
		public String? Name
		{
			get => _name;
			set
			{
				if (RaisePropertyChangedIfSet(ref _name, value))
				{
					NameChanged();
				}
			}
		}

		// 名前のヒント
		private String? _nameHint;
		public String? NameHint
		{
			get => _nameHint;
			set => RaisePropertyChangedIfSet(ref _nameHint, value);
		}

		// 検索ワード
		private String? _keyword;
		public String? Keyword
		{
			get => _keyword;
			set => RaisePropertyChangedIfSet(ref _keyword, value);
		}

		// 検索ワードのヒント
		private String? _keywordHint;
		public String? KeywordHint
		{
			get => _keywordHint;
			set => RaisePropertyChangedIfSet(ref _keywordHint, value);
		}

		// OK ボタンフォーカス
		private Boolean _isButtonOkFocused;
		public Boolean IsButtonOkFocused
		{
			get => _isButtonOkFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isButtonOkFocused = value;
				RaisePropertyChanged(nameof(IsButtonOkFocused));
			}
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOkClickedCommand;

		public ViewModelCommand ButtonOkClickedCommand
		{
			get
			{
				if (_buttonOkClickedCommand == null)
				{
					_buttonOkClickedCommand = new ViewModelCommand(ButtonOKClicked);
				}
				return _buttonOkClickedCommand;
			}
		}

		public virtual void ButtonOKClicked()
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
				// タイトルバー
				Title = _caption + "詳細情報の編集";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// キャプション
				IdCaption = _caption + " ID (_I)：";
				NameCaption = _caption + "名 (_N)：";

				// ヒント
				KeywordHint = "キーワード、コメントなど。複数入力する際は、半角カンマ「 , 」で区切って下さい。";
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲情報データベースマスター詳細編集ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// 編集対象の名称
		protected String? _caption;

		// 楽曲情報データベースのコンテキスト
		protected MusicInfoContext _musicInfoContext;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー：Name が変更された
		// --------------------------------------------------------------------
		protected virtual void NameChanged()
		{
		}

		// --------------------------------------------------------------------
		// 表示用新規 ID
		// --------------------------------------------------------------------
		protected String NewIdForDisplay()
		{
			return "（新規" + _caption + "）";
		}

#if false
		// --------------------------------------------------------------------
		// イベントハンドラー：SelectedId が変更された
		// --------------------------------------------------------------------
		protected virtual void SelectedIdChanged()
		{
		}
#endif




	}
}
