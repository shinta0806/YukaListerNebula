// ============================================================================
// 
// ゆかりすたー NEBULA の基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Messaging.IO;

using Shinta.ViewModels;

using System;
using System.IO;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	internal class YlViewModel : BasicWindowViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public YlViewModel(Boolean useLogWriter = true)
				: base(useLogWriter ? YlModel.Instance.EnvModel.LogWriter : null)
		{
		}

		// ====================================================================
		// public 関数
		// ====================================================================

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
	}
}
