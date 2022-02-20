// ============================================================================
// 
// タブアイテムの基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta.ViewModels;

using System;
using System.IO;

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
			String dropped = String.Empty;
			String unknown = String.Empty;
			Boolean isFile = false;

			foreach (String path in pathes)
			{
				if (!File.Exists(path))
				{
					continue;
				}

				if (Array.IndexOf(exts, Path.GetExtension(path).ToLower()) >= 0)
				{
					dropped = path;
					break;
				}
				else
				{
					isFile = true;
					unknown = Path.GetFileName(path) + "\n";
				}
			}

			if (String.IsNullOrEmpty(dropped))
			{
				if (isFile)
				{
					throw new Exception("ドロップされたファイルの種類を自動判定できませんでした。\n参照ボタンでファイルを指定して下さい。\n" + unknown + "\n対応している形式：" + String.Join(" ", exts));
				}
				else
				{
					throw new Exception("ファイルをドロップしてください。");
				}
			}

			return dropped;
		}

		// --------------------------------------------------------------------
		// ドロップされたフォルダー名を 1 つ取得
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected static String DroppedFolder(String[] pathes)
		{
			String? folderPath = null;
			foreach (String path in pathes)
			{
				if (Directory.Exists(path))
				{
					// フォルダーがドロップされた場合は、そのフォルダーを使用することで確定する
					folderPath = path;
					break;
				}
				if (File.Exists(path))
				{
					// ファイルがドロップされた場合は、そのファイルを含むフォルダーを使用（フォルダーが指定されればフォルダー優先のため、ループは継続）
					folderPath = Path.GetDirectoryName(path);
				}
			}
			if (String.IsNullOrEmpty(folderPath))
			{
				throw new Exception("ドロップされたフォルダーを取得できませんでした。\n参照ボタンでフォルダーを指定して下さい。");
			}

			return folderPath;
		}
	}
}
