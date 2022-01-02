// ============================================================================
// 
// エクスポートウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// エクスポートしたデータベースにはインデックスが付与されない。
// そのまま楽曲情報データベースとして使われることは想定しておらず、あくまでもインポートして使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

namespace YukaLister.ViewModels.ImportExportWindowViewModels
{
	public class ExportWindowViewModel : ImportExportWindowViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public ExportWindowViewModel(String exportYukaListerPath)
				: base("エクスポート")
		{
			_exportYukaListerPath = exportYukaListerPath;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public ExportWindowViewModel()
		{
			_exportYukaListerPath = String.Empty;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// エクスポート処理
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		protected override Task ImportExportByWorker(Object? _)
		{
			String tempExportPath = YlCommon.TempPath();
			MusicInfoContextDefault musicInfoContextDefault = new();
			musicInfoContextDefault.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			MusicInfoContextExport musicInfoContextExport = new(tempExportPath);
			musicInfoContextExport.CreateDatabase();

			// コピー
			// peopleInMemory.AddRange(peopleInMusicInfo) のように DbSet 全体を追加すると、アプリ終了時にタスクが終了しないため、Where を挟む
			Description = "エクスポートしています...";
			musicInfoContextExport.Songs.AddRange(musicInfoContextDefault.Songs.Where(x => true));
			musicInfoContextExport.People.AddRange(musicInfoContextDefault.People.Where(x => true));
			musicInfoContextExport.TieUps.AddRange(musicInfoContextDefault.TieUps.Where(x => true));
			musicInfoContextExport.TieUpGroups.AddRange(musicInfoContextDefault.TieUpGroups.Where(x => true));
			musicInfoContextExport.Makers.AddRange(musicInfoContextDefault.Makers.Where(x => true));
			musicInfoContextExport.SongAliases.AddRange(musicInfoContextDefault.SongAliases.Where(x => true));
			musicInfoContextExport.TieUpAliases.AddRange(musicInfoContextDefault.TieUpAliases.Where(x => true));
			musicInfoContextExport.ArtistSequences.AddRange(musicInfoContextDefault.ArtistSequences.Where(x => true));
			musicInfoContextExport.LyristSequences.AddRange(musicInfoContextDefault.LyristSequences.Where(x => true));
			musicInfoContextExport.ComposerSequences.AddRange(musicInfoContextDefault.ComposerSequences.Where(x => true));
			musicInfoContextExport.ArrangerSequences.AddRange(musicInfoContextDefault.ArrangerSequences.Where(x => true));
			musicInfoContextExport.TieUpGroupSequences.AddRange(musicInfoContextDefault.TieUpGroupSequences.Where(x => true));
			musicInfoContextExport.Tags.AddRange(musicInfoContextDefault.Tags.Where(x => true));
			musicInfoContextExport.TagSequences.AddRange(musicInfoContextDefault.TagSequences.Where(x => true));
			musicInfoContextExport.SaveChanges();

			// 古いファイルを削除
			try
			{
				File.Delete(_exportYukaListerPath);
			}
			catch (Exception)
			{
			}

			// 出力
			// データベースファイルをそのまま圧縮しようとするとプロセスが使用中というエラーになることがある（2 回に 1 回くらい）ため、
			// いったんデータベースファイルをコピーしてから圧縮する
			Description = "保存しています...";
			String tempFolder = YlCommon.TempPath();
			Directory.CreateDirectory(tempFolder);
			File.Copy(tempExportPath, tempFolder + "\\" + FILE_NAME_EXPORT_MUSIC_INFO);
			ZipFile.CreateFromDirectory(tempFolder, _exportYukaListerPath, CompressionLevel.Optimal, false);
			_abortCancellationTokenSource.Token.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ファイル名
		private const String FILE_NAME_EXPORT_MUSIC_INFO = "ExportMusicInfo" + Common.FILE_EXT_SQLITE3;

		// ====================================================================
		// private 変数
		// ====================================================================

		// エクスポート先
		private readonly String _exportYukaListerPath;
	}
}
