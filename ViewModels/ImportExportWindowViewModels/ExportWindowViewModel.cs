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

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

namespace YukaLister.ViewModels.ImportExportWindowViewModels
{
	public class ExportWindowViewModel : ImportExportWindowViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
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
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// エクスポート処理
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		protected override Task ImportExportByWorker(Object? _)
		{
			String tempExportPath = YlCommon.TempPath();
			MusicInfoContextDefault musicInfoContextDefault = MusicInfoContextDefault.CreateContext(out DbSet<TProperty> propertiesDefault,
					out DbSet<TSong> songsDefault, out DbSet<TPerson> peopleDefault, out DbSet<TTieUp> tieUpsDefault, out DbSet<TCategory> categoriesDefault,
					out DbSet<TTieUpGroup> tieUpGroupsDefault, out DbSet<TMaker> makersDefault, out DbSet<TTag> tagsDefault,
					out DbSet<TSongAlias> songAliasesDefault, out DbSet<TPersonAlias> personAliasesDefault, out DbSet<TTieUpAlias> tieUpAliasesDefault,
					out DbSet<TCategoryAlias> categoryAliasesDefault, out DbSet<TTieUpGroupAlias> tieUpGroupAliasesDefault, out DbSet<TMakerAlias> makerAliasesDefault,
					out DbSet<TArtistSequence> artistSequencesDefault, out DbSet<TLyristSequence> lyristSequencesDefault, out DbSet<TComposerSequence> composerSequencesDefault, out DbSet<TArrangerSequence> arrangerSequencesDefault,
					out DbSet<TTieUpGroupSequence> tieUpGroupSequencesDefault, out DbSet<TTagSequence> tagSequencesDefault);
			musicInfoContextDefault.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			MusicInfoContextExport musicInfoContextExport = MusicInfoContextExport.CreateContext(tempExportPath, out DbSet<TProperty> propertiesExport,
					out DbSet<TSong> songsExport, out DbSet<TPerson> peopleExport, out DbSet<TTieUp> tieUpsExport, out DbSet<TCategory> categoriesExport,
					out DbSet<TTieUpGroup> tieUpGroupsExport, out DbSet<TMaker> makersExport, out DbSet<TTag> tagsExport,
					out DbSet<TSongAlias> songAliasesExport, out DbSet<TPersonAlias> personAliasesExport, out DbSet<TTieUpAlias> tieUpAliasesExport,
					out DbSet<TCategoryAlias> categoryAliasesExport, out DbSet<TTieUpGroupAlias> tieUpGroupAliasesExport, out DbSet<TMakerAlias> makerAliasesExport,
					out DbSet<TArtistSequence> artistSequencesExport, out DbSet<TLyristSequence> lyristSequencesExport, out DbSet<TComposerSequence> composerSequencesExport, out DbSet<TArrangerSequence> arrangerSequencesExport,
					out DbSet<TTieUpGroupSequence> tieUpGroupSequencesExport, out DbSet<TTagSequence> tagSequencesExport);
			musicInfoContextExport.CreateDatabase();

			// コピー
			// peopleInMemory.AddRange(peopleInMusicInfo) のように DbSet 全体を追加すると、アプリ終了時にタスクが終了しないため、Where を挟む
			Description = "エクスポートしています...";
			songsExport.AddRange(songsDefault.Where(x => true));
			peopleExport.AddRange(peopleDefault.Where(x => true));
			tieUpsExport.AddRange(tieUpsDefault.Where(x => true));
			tieUpGroupsExport.AddRange(tieUpGroupsDefault.Where(x => true));
			makersExport.AddRange(makersDefault.Where(x => true));
			songAliasesExport.AddRange(songAliasesDefault.Where(x => true));
			tieUpAliasesExport.AddRange(tieUpAliasesDefault.Where(x => true));
			artistSequencesExport.AddRange(artistSequencesDefault.Where(x => true));
			lyristSequencesExport.AddRange(lyristSequencesDefault.Where(x => true));
			composerSequencesExport.AddRange(composerSequencesDefault.Where(x => true));
			arrangerSequencesExport.AddRange(arrangerSequencesDefault.Where(x => true));
			tieUpGroupSequencesExport.AddRange(tieUpGroupSequencesDefault.Where(x => true));
			tagsExport.AddRange(tagsDefault.Where(x => true));
			tagSequencesExport.AddRange(tagSequencesDefault.Where(x => true));
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
		// private メンバー変数
		// ====================================================================

		// エクスポート先
		private readonly String _exportYukaListerPath;
	}
}
