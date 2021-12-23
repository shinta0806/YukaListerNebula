// ============================================================================
// 
// インポートウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.ImportExportWindowViewModels
{
	public class ImportWindowViewModel : ImportExportWindowViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
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
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// インポート処理
		// --------------------------------------------------------------------
		protected override Task ImportExportByWorker(Object? _)
		{
			// 楽曲情報データベースバックアップ
			using MusicInfoContextDefault musicInfoContextDefault = MusicInfoContextDefault.CreateContext(out DbSet<TProperty> _);
			musicInfoContextDefault.BackupDatabase();

			// インポートタスクを実行
#if false
			Action<String>? descriptionSetter = null;
			MethodInfo? methodInfo = typeof(ImportWindowViewModel).GetProperty(nameof(Description))?.GetSetMethod();
			if (methodInfo != null)
			{
				descriptionSetter = (Action<String>?)Delegate.CreateDelegate(typeof(Action<String>), this, methodInfo, false);
			}
			Importer importer = new(_importYukaListerPath, _importTag, _importSameName, _abortCancellationTokenSource.Token, descriptionSetter);
			importer.Import();
#endif
			Importer importer = new(_importYukaListerPath, _importTag, _importSameName, _abortCancellationTokenSource.Token, (x) => Description = x);
			importer.Import();

			return Task.CompletedTask;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// インポート元
		private readonly String _importYukaListerPath;

		// タグ情報をインポートする
		private readonly Boolean _importTag;

		// 同名の情報も極力インポートする
		private readonly Boolean _importSameName;

		// ====================================================================
		// private メンバー関数
		// ====================================================================


	}
}
