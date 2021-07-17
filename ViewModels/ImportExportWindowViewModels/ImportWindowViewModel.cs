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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

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
			_importYukaListerPath = importYukaListerPath;
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
		protected override async Task ImportExportByWorker(Object? _)
		{
			// 楽曲情報データベースバックアップ
			using MusicInfoContextDefault musicInfoContextDefault = MusicInfoContextDefault.CreateContext(out DbSet<TProperty> _);
			musicInfoContextDefault.BackupDatabase();

			// ID 接頭辞
			await YlCommon.InputIdPrefixIfNeededWithInvoke(this);

			// インポートタスクを実行
			ImportYukaLister();
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

		// --------------------------------------------------------------------
		// 別名テーブルをインポート
		// --------------------------------------------------------------------
		private void ImportAliasTable<T>(DbSet<T> recordsExport, DbSet<T> recordsDefault, MusicInfoContextDefault musicInfoContextDefault) where T : class, IRcAlias
		{
			Description = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "情報をインポート中...";

			List<T> resultsExport = recordsExport.Where(x => !x.Invalid).ToList();
			foreach (T resultExport in resultsExport)
			{
				// インポート後に Dirty になるようにフラグをセットしておく
				resultExport.Dirty = true;

				// 同じ Id があるか
				T? sameIdRecord = DbCommon.SelectBaseById(recordsDefault, resultExport.Id, true);
				if (sameIdRecord != null)
				{
					// 同じ Id がある場合、_importSameName なら上書き
					if (_importSameName)
					{
						Common.ShallowCopyFields(resultExport, sameIdRecord);
					}
					continue;
				}

				// 同じ別名があるか
				T? sameAliasRecord = DbCommon.SelectAliasByAlias(recordsDefault, resultExport.Alias, true);
				if (sameAliasRecord != null)
				{
					// 同じ別名がある場合、_importSameName なら上書き
					if (_importSameName)
					{
						Common.ShallowCopyFields(resultExport, sameAliasRecord);

					}
					continue;
				}

				// 新規挿入
				recordsDefault.Add(resultExport);
			}

			_abortCancellationTokenSource.Token.ThrowIfCancellationRequested();

			musicInfoContextDefault.SaveChanges();
		}

		// --------------------------------------------------------------------
		// マスターテーブルをインポート
		// --------------------------------------------------------------------
		private void ImportMasterTable<T>(DbSet<T> recordsExport, DbSet<T> recordsDefault, MusicInfoContextDefault musicInfoContextDefault) where T : class, IRcMaster
		{
			Description = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "情報をインポート中...";

			List<T> resultsExport = recordsExport.Where(x => !x.Invalid).ToList();
			foreach (T resultExport in resultsExport)
			{
				// インポート後に Dirty になるようにフラグをセットしておく
				resultExport.Dirty = true;

				// 同じ Id があるか
				T? sameIdRecord = DbCommon.SelectBaseById(recordsDefault, resultExport.Id, true);
				if (sameIdRecord != null)
				{
					// 同じ Id がある場合、_importSameName なら上書き
					if (_importSameName)
					{
						Common.ShallowCopyFields(resultExport, sameIdRecord);
					}
					continue;
				}

				// 同じ名前があるか
				List<T> sameNameRecords = recordsDefault.Where(x => x.Name == resultExport.Name).ToList();
				if (sameNameRecords.Any())
				{
					if (_importSameName)
					{
						T? sameKeywordRecord = sameNameRecords.FirstOrDefault(x => x.Keyword == null && resultExport.Keyword == null || x.Keyword != null && x.Keyword == resultExport.Keyword);
						if (sameKeywordRecord != null)
						{
							// _importSameName かつ同じキーワードがある場合は上書き
							Common.ShallowCopyFields(resultExport, sameKeywordRecord);
							continue;
						}
						else
						{
							// _importSameName かつ同じキーワードがない場合は、ここでは何もせず新規挿入に進む
						}
					}
					else
					{
						// _importSameName でない場合は何もしない
						continue;
					}
				}

				// 新規挿入
				recordsDefault.Add(resultExport);
			}

			_abortCancellationTokenSource.Token.ThrowIfCancellationRequested();

			musicInfoContextDefault.SaveChanges();
		}

		// --------------------------------------------------------------------
		// 紐付テーブルをインポート
		// --------------------------------------------------------------------
		private void ImportSequenceTable<T>(DbSet<T> recordsExport, DbSet<T> recordsDefault, MusicInfoContextDefault musicInfoContextDefault) where T : class, IRcSequence
		{
			Description = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "情報をインポート中...";

			List<T> resultsExport = recordsExport.Where(x => !x.Invalid).ToList();
			foreach (T resultExport in resultsExport)
			{
				// インポート後に Dirty になるようにフラグをセットしておく
				resultExport.Dirty = true;

				// 同じ Id かつ同じ連番があるか
				T? sameIdRecord = recordsDefault.FirstOrDefault(x => x.Id == resultExport.Id && x.Sequence == resultExport.Sequence);
				if (sameIdRecord != null)
				{
					// 同じ Id かつ同じ連番がある場合は上書き
					Common.ShallowCopyFields(resultExport, sameIdRecord);
					continue;
				}

				// 新規挿入
				recordsDefault.Add(resultExport);
			}

			_abortCancellationTokenSource.Token.ThrowIfCancellationRequested();

			musicInfoContextDefault.SaveChanges();
		}

		// --------------------------------------------------------------------
		// ゆかりすたー情報ファイルをインポート
		// --------------------------------------------------------------------
		private void ImportYukaLister()
		{
			// 解凍
			String tempFolder = YlCommon.TempPath() + "\\";
			Directory.CreateDirectory(tempFolder);
			ZipFile.ExtractToDirectory(_importYukaListerPath, tempFolder);
			String[] files = Directory.GetFiles(tempFolder, "*", SearchOption.AllDirectories);
			if (files.Length == 0)
			{
				throw new Exception("ゆかりすたー情報ファイルにインポートできるデータが存在しません。");
			}
			String file = files[0];

			MusicInfoContextExport musicInfoContextExport = MusicInfoContextExport.CreateContext(file, out _,
					out DbSet<TSong> songsExport, out DbSet<TPerson> peopleExport, out DbSet<TTieUp> tieUpsExport, out _,
					out DbSet<TTieUpGroup> tieUpGroupsExport, out DbSet<TMaker> makersExport, out DbSet<TTag> tagsExport,
					out DbSet<TSongAlias> songAliasesExport, out _, out DbSet<TTieUpAlias> tieUpAliasesExport,
					out _, out _, out _,
					out DbSet<TArtistSequence> artistSequencesExport, out DbSet<TLyristSequence> lyristSequencesExport, out DbSet<TComposerSequence> composerSequencesExport, out DbSet<TArrangerSequence> arrangerSequencesExport,
					out DbSet<TTieUpGroupSequence> tieUpGroupSequencesExport, out DbSet<TTagSequence> tagSequencesExport);
			musicInfoContextExport.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			MusicInfoContextDefault musicInfoContextDefault = MusicInfoContextDefault.CreateContext(out _,
					out DbSet<TSong> songsDefault, out DbSet<TPerson> peopleDefault, out DbSet<TTieUp> tieUpsDefault, out _,
					out DbSet<TTieUpGroup> tieUpGroupsDefault, out DbSet<TMaker> makersDefault, out DbSet<TTag> tagsDefault,
					out DbSet<TSongAlias> songAliasesDefault, out _, out DbSet<TTieUpAlias> tieUpAliasesDefault,
					out _, out _, out _,
					out DbSet<TArtistSequence> artistSequencesDefault, out DbSet<TLyristSequence> lyristSequencesDefault, out DbSet<TComposerSequence> composerSequencesDefault, out DbSet<TArrangerSequence> arrangerSequencesDefault,
					out DbSet<TTieUpGroupSequence> tieUpGroupSequencesDefault, out DbSet<TTagSequence> tagSequencesDefault);

			// 有効なマスターテーブルをインポート（カテゴリー以外）
			ImportMasterTable(songsExport, songsDefault, musicInfoContextDefault);
			ImportMasterTable(peopleExport, peopleDefault, musicInfoContextDefault);
			ImportMasterTable(tieUpsExport, tieUpsDefault, musicInfoContextDefault);
			ImportMasterTable(tieUpGroupsExport, tieUpGroupsDefault, musicInfoContextDefault);
			ImportMasterTable(makersExport, makersDefault, musicInfoContextDefault);
			if (_importTag)
			{
				ImportMasterTable<TTag>(tagsExport, tagsDefault, musicInfoContextDefault);
			}

			// 有効な別名テーブルをインポート
			ImportAliasTable(songAliasesExport, songAliasesDefault, musicInfoContextDefault);
			ImportAliasTable(tieUpAliasesExport, tieUpAliasesDefault, musicInfoContextDefault);

			// 有効な紐付テーブルをインポート
			ImportSequenceTable(artistSequencesExport, artistSequencesDefault, musicInfoContextDefault);
			ImportSequenceTable(lyristSequencesExport, lyristSequencesDefault, musicInfoContextDefault);
			ImportSequenceTable(composerSequencesExport, composerSequencesDefault, musicInfoContextDefault);
			ImportSequenceTable(arrangerSequencesExport, arrangerSequencesDefault, musicInfoContextDefault);
			ImportSequenceTable(tieUpGroupSequencesExport, tieUpGroupSequencesDefault, musicInfoContextDefault);
			if (_importTag)
			{
				ImportSequenceTable(tagSequencesExport, tagSequencesDefault, musicInfoContextDefault);
			}
		}
	}
}
