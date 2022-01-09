// ============================================================================
// 
// インポートウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.IO;
using System.Threading.Tasks;

using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.ImportExportWindowViewModels
{
	public class ImportWindowViewModel : ImportExportWindowViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public ImportWindowViewModel(String importYukaListerPath, Boolean importTag, Boolean importSameName)
				: base("インポート")
		{
			if (Path.IsPathRooted(importYukaListerPath))
			{
				_importYukaListerPath = importYukaListerPath;
			}
			else
			{
				_importYukaListerPath = Common.MakeAbsolutePath(YukaListerModel.Instance.EnvModel.ExeFullFolder, importYukaListerPath);
			}
			_importTag = importTag;
			_importSameName = importSameName;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public ImportWindowViewModel()
		{
			_importYukaListerPath = String.Empty;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// インポート処理
		// --------------------------------------------------------------------
		protected override Task ImportExportByWorker(Object? _)
		{
			// 楽曲情報データベースバックアップ
			using MusicInfoContextDefault musicInfoContextDefault = new();
			musicInfoContextDefault.BackupDatabase();

			// インポートタスクを実行
			Importer importer = new(_importYukaListerPath, _importTag, _importSameName, (x) => Description = x, _abortCancellationTokenSource.Token);
			importer.Import();

			return Task.CompletedTask;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// インポート元
		private readonly String _importYukaListerPath;

		// タグ情報をインポートする
		private readonly Boolean _importTag;

		// 同名の情報も極力インポートする
		private readonly Boolean _importSameName;
	}
}
