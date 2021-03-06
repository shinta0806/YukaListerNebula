// ============================================================================
// 
// ゆかりすたー NEBULA の基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// スプラッシュウィンドウ ViewModel 以外のすべてのウィンドウの ViewModel に適用する
// ----------------------------------------------------------------------------

using Livet;
using Livet.Messaging.IO;

using Shinta;
using System;
using System.IO;
using System.Windows.Input;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class YlViewModel : ViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public YlViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ウィンドウタイトル（デフォルトが null だと実行時にエラーが発生するので Empty にしておく）
		private String _title = String.Empty;
		public String Title
		{
			get => _title;
			set => RaisePropertyChangedIfSet(ref _title, value);
		}

		// カーソル
		private Cursor? _cursor;
		public Cursor? Cursor
		{
			get => _cursor;
			set => RaisePropertyChangedIfSet(ref _cursor, value);
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// OK ボタン・削除ボタン等、キャンセル以外のボタンが押されて閉じられた
		public Boolean IsOk { get; protected set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public virtual void Initialize()
		{
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 初期化中...");
		}

		// --------------------------------------------------------------------
		// 開くダイアログを表示し、ファイルパスを取得
		// --------------------------------------------------------------------
		public String? PathByOpeningDialog(String title, String filter, String? path)
		{
			OpeningFileSelectionMessage message = new(YlConstants.MESSAGE_KEY_OPEN_OPEN_FILE_DIALOG);
			message.Title = title;
			message.Filter = filter + "|すべてのファイル|*.*";
			if (!String.IsNullOrEmpty(path))
			{
				message.InitialDirectory = Path.GetDirectoryName(path);
				message.FileName = Path.GetFileName(path);
			}
			Messenger.Raise(message);
			if (message.Response == null)
			{
				return null;
			}

			return message.Response[0];
		}

		// --------------------------------------------------------------------
		// 保存ダイアログを表示し、ファイルパスを取得
		// --------------------------------------------------------------------
		public String? PathBySavingDialog(String title, String filter, String? path)
		{
			SavingFileSelectionMessage message = new(YlConstants.MESSAGE_KEY_OPEN_SAVE_FILE_DIALOG);
			message.Title = title;
			message.Filter = filter + "|すべてのファイル|*.*";
			if (!String.IsNullOrEmpty(path))
			{
				message.InitialDirectory = Path.GetDirectoryName(path);
				message.FileName = Path.GetFileName(path);
			}
			Messenger.Raise(message);
			if (message.Response == null)
			{
				return null;
			}

			return message.Response[0];
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 破棄中...");
		}
	}
}
