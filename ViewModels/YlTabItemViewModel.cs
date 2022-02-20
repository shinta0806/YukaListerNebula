// ============================================================================
// 
// タブアイテムの基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;
using Shinta.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YukaLister.Models.Settings;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels
{
	internal class YlTabItemViewModel : TabItemViewModel<YlSettings>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlTabItemViewModel(YlSettingsWindowViewModel ylSettingsWindowViewModel)
				: base(ylSettingsWindowViewModel, YlModel.Instance.EnvModel.LogWriter)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public YlTabItemViewModel()
		{
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ドロップされたファイル名を 1 つ取得
		// ＜引数＞ exts: 小文字前提
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected static String DroppedFile(String[] pathes, String[] exts)
		{
			List<String> files = Common.SelectFiles(pathes, exts);
			if (!files.Any())
			{
				throw new Exception("ドロップされたファイルの種類を自動判定できませんでした。\n参照ボタンでファイルを指定して下さい。\n\n対応している形式：" + String.Join(" ", exts));
			}
			return files[0];
		}

		// --------------------------------------------------------------------
		// ドロップされたフォルダー名を 1 つ取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected static String DroppedFolder(String[] pathes)
		{
			String? folderPath = Common.SelectFolder(pathes);
			if (String.IsNullOrEmpty(folderPath))
			{
				throw new Exception("ドロップされたフォルダーを取得できませんでした。\n参照ボタンでフォルダーを指定して下さい。");
			}
			return folderPath;
		}
	}
}
