// ============================================================================
// 
// ゆかりすたー情報ファイルから楽曲情報データベースへインポート
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
using System.Threading;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseAssist
{
	internal class Importer
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public Importer(String importSrcPath, Boolean importTag, Boolean importSameName, Action<String>? descriptionSetter, CancellationToken cancellationToken)
		{
			if (Path.IsPathRooted(importSrcPath))
			{
				_importSrcPath = importSrcPath;
			}
			else
			{
				_importSrcPath = Path.GetFullPath(importSrcPath, YlModel.Instance.EnvModel.ExeFullFolder);
			}
			_importTag = importTag;
			_importSameName = importSameName;
			if (descriptionSetter != null)
			{
				_descriptionSetter = descriptionSetter;
			}
			_cancellationToken = cancellationToken;
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 楽曲情報データベースをインポート
		// --------------------------------------------------------------------
		public void Import()
		{
			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インポートしています：" + _importSrcPath);

			String file;
			if (Path.GetExtension(_importSrcPath).ToLower() == YlConstants.FILE_EXT_YL_EXPORT_ARCHIVE)
			{
				file = Extract();
			}
			else
			{
				file = _importSrcPath;
			}

			using MusicInfoContextExport musicInfoContextExport = new(file);
			musicInfoContextExport.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			using MusicInfoContextDefault musicInfoContextDefault = new();

			// 有効なマスターテーブルをインポート（カテゴリー以外）
			ImportMasterTable(musicInfoContextExport.Songs, musicInfoContextDefault.Songs, musicInfoContextDefault);
			ImportMasterTable(musicInfoContextExport.People, musicInfoContextDefault.People, musicInfoContextDefault);
			ImportMasterTable(musicInfoContextExport.TieUps, musicInfoContextDefault.TieUps, musicInfoContextDefault);
			ImportMasterTable(musicInfoContextExport.TieUpGroups, musicInfoContextDefault.TieUpGroups, musicInfoContextDefault);
			ImportMasterTable(musicInfoContextExport.Makers, musicInfoContextDefault.Makers, musicInfoContextDefault);
			if (_importTag)
			{
				ImportMasterTable<TTag>(musicInfoContextExport.Tags, musicInfoContextDefault.Tags, musicInfoContextDefault);
			}

			// 有効な別名テーブルをインポート
			ImportAliasTable(musicInfoContextExport.SongAliases, musicInfoContextDefault.SongAliases, musicInfoContextDefault);
			ImportAliasTable(musicInfoContextExport.TieUpAliases, musicInfoContextDefault.TieUpAliases, musicInfoContextDefault);

			// 有効な紐付テーブルをインポート
			ImportSequenceTable(musicInfoContextExport.ArtistSequences, musicInfoContextDefault.ArtistSequences, musicInfoContextDefault);
			ImportSequenceTable(musicInfoContextExport.LyristSequences, musicInfoContextDefault.LyristSequences, musicInfoContextDefault);
			ImportSequenceTable(musicInfoContextExport.ComposerSequences, musicInfoContextDefault.ComposerSequences, musicInfoContextDefault);
			ImportSequenceTable(musicInfoContextExport.ArrangerSequences, musicInfoContextDefault.ArrangerSequences, musicInfoContextDefault);
			ImportSequenceTable(musicInfoContextExport.TieUpGroupSequences, musicInfoContextDefault.TieUpGroupSequences, musicInfoContextDefault);
			if (_importTag)
			{
				ImportSequenceTable(musicInfoContextExport.TagSequences, musicInfoContextDefault.TagSequences, musicInfoContextDefault);
			}

			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インポートが完了しました。");
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// インポート元
		private readonly String _importSrcPath;

		// タグ情報をインポートする
		private readonly Boolean _importTag;

		// 同名の情報も極力インポートする
		private readonly Boolean _importSameName;

		// 中断制御
		private readonly CancellationToken _cancellationToken;

		// 説明プロパティーのセッター
		private readonly Action<String> _descriptionSetter = delegate { };

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// インポート元を解凍
		// --------------------------------------------------------------------
		private String Extract()
		{
			String tempFolder = Common.TempPath() + "\\";
			Directory.CreateDirectory(tempFolder);
			ZipFile.ExtractToDirectory(_importSrcPath, tempFolder);
			String[] files = Directory.GetFiles(tempFolder, "*", SearchOption.AllDirectories);
			if (files.Length == 0)
			{
				throw new Exception("ゆかりすたー情報ファイルにインポートできるデータが存在しません。");
			}
			return files[0];
		}

		// --------------------------------------------------------------------
		// 別名テーブルをインポート
		// --------------------------------------------------------------------
		private void ImportAliasTable<T>(DbSet<T> recordsExport, DbSet<T> recordsDefault, MusicInfoContextDefault musicInfoContextDefault) where T : class, IRcAlias
		{
			_descriptionSetter(YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "情報をインポート中...");

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

			_cancellationToken.ThrowIfCancellationRequested();

			musicInfoContextDefault.SaveChanges();
		}

		// --------------------------------------------------------------------
		// マスターテーブルをインポート
		// --------------------------------------------------------------------
		private void ImportMasterTable<T>(DbSet<T> recordsExport, DbSet<T> recordsDefault, MusicInfoContextDefault musicInfoContextDefault) where T : class, IRcMaster
		{
			_descriptionSetter(YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "情報をインポート中...");

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

			_cancellationToken.ThrowIfCancellationRequested();

			musicInfoContextDefault.SaveChanges();
		}

		// --------------------------------------------------------------------
		// 紐付テーブルをインポート
		// --------------------------------------------------------------------
		private void ImportSequenceTable<T>(DbSet<T> recordsExport, DbSet<T> recordsDefault, MusicInfoContextDefault musicInfoContextDefault) where T : class, IRcSequence
		{
			_descriptionSetter(YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "情報をインポート中...");

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

			_cancellationToken.ThrowIfCancellationRequested();

			musicInfoContextDefault.SaveChanges();
		}
	}
}
