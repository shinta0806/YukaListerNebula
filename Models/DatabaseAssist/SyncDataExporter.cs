// ============================================================================
// 
// 楽曲情報データベースから同期データ（アップロード用）を生成
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shinta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.DatabaseAssist
{
	public class SyncDataExporter : SyncDataIo
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public SyncDataExporter()
		{
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 同期データ生成
		// --------------------------------------------------------------------
		public (List<String> csvHead, List<List<String>> csvContents) Export(MusicInfoTables tableIndex)
		{
			return tableIndex switch
			{
				MusicInfoTables.TSong => ExportMaster(_songs, TSong.FIELD_PREFIX_SONG),
				MusicInfoTables.TPerson => ExportMaster(_people, TPerson.FIELD_PREFIX_PERSON),
				MusicInfoTables.TTieUp => ExportMaster(_tieUps, TTieUp.FIELD_PREFIX_TIE_UP),
				MusicInfoTables.TTieUpGroup => ExportMaster(_tieUpGroups, TTieUpGroup.FIELD_PREFIX_TIE_UP_GROUP),
				MusicInfoTables.TMaker => ExportMaster(_makers, TMaker.FIELD_PREFIX_MAKER),
				MusicInfoTables.TSongAlias => ExportAlias(_songAliases, TSongAlias.FIELD_PREFIX_SONG_ALIAS),
				MusicInfoTables.TTieUpAlias => ExportAlias(_tieUpAliases, TTieUpAlias.FIELD_PREFIX_TIE_UP_ALIAS),
				MusicInfoTables.TArtistSequence => ExportSequence(_artistSequences, TArtistSequence.FIELD_PREFIX_ARTIST_SEQUENCE),
				MusicInfoTables.TLyristSequence => ExportSequence(_lyristSequences, TLyristSequence.FIELD_PREFIX_LYRIST_SEQUENCE),
				MusicInfoTables.TComposerSequence => ExportSequence(_composerSequences, TComposerSequence.FIELD_PREFIX_COMPOSER_SEQUENCE),
				MusicInfoTables.TArrangerSequence => ExportSequence(_arrangerSequences, TArrangerSequence.FIELD_PREFIX_ARRANGER_SEQUENCE),
				MusicInfoTables.TTieUpGroupSequence => ExportSequence(_tieUpGroupSequences, TTieUpGroupSequence.FIELD_PREFIX_TIE_UP_GROUP_SEQUENCE),
				_ => NotExport(tableIndex),
			};
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// Boolean を文字列で送信する同期データに変換
		// --------------------------------------------------------------------
		private String BooleanToSyncData(Boolean boolean)
		{
			if (boolean)
			{
				return "1";
			}
			else
			{
				return "0";
			}
		}


		// --------------------------------------------------------------------
		// IRcAlias エクスポート
		// --------------------------------------------------------------------
		private (List<String> csvHead, List<List<String>> csvContents) ExportAlias<T>(DbSet<T> records, String fieldPrefix) where T : class, IRcAlias
		{
			List<String> csvHead = new();
			List<List<String>> csvContents = new();

			// ヘッダー
			SetAliasCsvHead(csvHead, fieldPrefix);

			// レコード群
			IQueryable<T> dirties = records.Where(x => x.Dirty);
			foreach (T dirtyRecord in dirties)
			{
				List<String> csvRecord = new();
				SetAliasCsvRecord(dirtyRecord, csvRecord);
				csvContents.Add(csvRecord);
			}

			return (csvHead, csvContents);
		}

		// --------------------------------------------------------------------
		// IRcMaster エクスポート
		// --------------------------------------------------------------------
		private (List<String> csvHead, List<List<String>> csvContents) ExportMaster<T>(DbSet<T> records, String fieldPrefix) where T : class, IRcMaster
		{
			List<String> csvHead = new();
			List<List<String>> csvContents = new();

			// ヘッダー
			SetMasterCsvHead<T>(csvHead, fieldPrefix);

			// レコード群
			IQueryable<T> dirties = records.Where(x => x.Dirty);
			foreach (T dirtyRecord in dirties)
			{
				List<String> csvRecord = new();
				SetMasterCsvRecord(dirtyRecord, csvRecord);
				csvContents.Add(csvRecord);
			}

			return (csvHead, csvContents);
		}

		// --------------------------------------------------------------------
		// IRcSequence エクスポート
		// --------------------------------------------------------------------
		private (List<String> csvHead, List<List<String>> csvContents) ExportSequence<T>(DbSet<T> records, String fieldPrefix) where T : class, IRcSequence
		{
			List<String> csvHead = new();
			List<List<String>> csvContents = new();

			// ヘッダー
			SetSequenceCsvHead(csvHead, fieldPrefix);

			// レコード群
			IQueryable<T> dirties = records.Where(x => x.Dirty);
			foreach (T dirtyRecord in dirties)
			{
				List<String> csvRecord = new();
				SetSequenceCsvRecord(dirtyRecord, csvRecord);
				csvContents.Add(csvRecord);
			}

			return (csvHead, csvContents);
		}

		// --------------------------------------------------------------------
		// エクスポート対象外
		// --------------------------------------------------------------------
		private (List<String> csvHead, List<List<String>> csvContents) NotExport(MusicInfoTables tableIndex)
		{
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "エクスポート対象外テーブル：" + YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[(Int32)tableIndex]);
			return (new List<String>(), new List<List<String>>());
		}

		// --------------------------------------------------------------------
		// IRcAlias 用に csvHead を設定（下位の IRcBase も設定）
		// --------------------------------------------------------------------
		private void SetAliasCsvHead(List<String> csvHead, String fieldPrefix)
		{
			SetBaseCsvHead(csvHead, fieldPrefix);

			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_ALIAS);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_ORIGINAL_ID);
		}

		// --------------------------------------------------------------------
		// IRcAlias を csvRecord に設定（下位の IRcBase も設定）
		// --------------------------------------------------------------------
		private void SetAliasCsvRecord(IRcAlias alias, List<String> csvRecord)
		{
			SetBaseCsvRecord(alias, csvRecord);

			csvRecord.Add(alias.Alias);
			csvRecord.Add(alias.OriginalId);
		}

		// --------------------------------------------------------------------
		// IRcBase 用に csvHead を設定（Dirty を除く）
		// --------------------------------------------------------------------
		private void SetBaseCsvHead(List<String> csvHead, String fieldPrefix)
		{
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_ID);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_IMPORT);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_INVALID);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_UPDATE_TIME);
		}

		// --------------------------------------------------------------------
		// IRcBase を csvRecord に設定（Dirty を除く）
		// --------------------------------------------------------------------
		private void SetBaseCsvRecord(IRcBase bas, List<String> csvRecord)
		{
			csvRecord.Add(bas.Id);
			csvRecord.Add(BooleanToSyncData(bas.Import));
			csvRecord.Add(BooleanToSyncData(bas.Invalid));
			csvRecord.Add(bas.UpdateTime.ToString());
		}

		// --------------------------------------------------------------------
		// IRcCategorizable 用に csvHead を設定（下位は設定しない）
		// --------------------------------------------------------------------
		private void SetCategorizableCsvHead(List<String> csvHead, String fieldPrefix)
		{
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_CATEGORY_ID);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_RELEASE_DATE);
		}

		// --------------------------------------------------------------------
		// IRcCategorizable を csvRecord に設定（下位は設定しない）
		// --------------------------------------------------------------------
		private void SetCategorizableCsvRecord(IRcCategorizable categorizable, List<String> csvRecord)
		{
			csvRecord.Add(categorizable.CategoryId ?? String.Empty);
			csvRecord.Add(categorizable.ReleaseDate.ToString());
		}

		// --------------------------------------------------------------------
		// IRcMaster 用に csvHead を設定（下位の IRcBase も設定）
		// IRcMaster より上位の TSong, TTieUp にも対応
		// --------------------------------------------------------------------
		private void SetMasterCsvHead<T>(List<String> csvHead, String fieldPrefix) where T : IRcBase
		{
			SetBaseCsvHead(csvHead, fieldPrefix);

			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_NAME);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_RUBY);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_RUBY_FOR_SEARCH);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_KEYWORD);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH);

			if (typeof(T) == typeof(TSong))
			{
				SetCategorizableCsvHead(csvHead, fieldPrefix);
				csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_TIE_UP_ID);
				csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_OP_ED);
			}
			else if (typeof(T) == typeof(TTieUp))
			{
				SetCategorizableCsvHead(csvHead, fieldPrefix);
				csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_MAKER_ID);
				csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_AGE_LIMIT);
			}
		}

		// --------------------------------------------------------------------
		// IRcMaster を csvRecord に設定（下位の IRcBase も設定）
		// IRcMaster より上位の TSong, TTieUp にも対応
		// --------------------------------------------------------------------
		private void SetMasterCsvRecord(IRcMaster master, List<String> csvRecord)
		{
			SetBaseCsvRecord(master, csvRecord);

			csvRecord.Add(master.Name ?? String.Empty);
			csvRecord.Add(master.Ruby ?? String.Empty);
			csvRecord.Add(master.RubyForSearch ?? String.Empty);
			csvRecord.Add(master.Keyword ?? String.Empty);
			csvRecord.Add(master.KeywordRubyForSearch ?? String.Empty);

			if (master is TSong song)
			{
				SetCategorizableCsvRecord(song, csvRecord);
				csvRecord.Add(song.TieUpId ?? String.Empty);
				csvRecord.Add(song.OpEd ?? String.Empty);
			}
			else if (master is TTieUp tieUp)
			{
				SetCategorizableCsvRecord(tieUp, csvRecord);
				csvRecord.Add(tieUp.MakerId ?? String.Empty);
				csvRecord.Add(tieUp.AgeLimit.ToString());
			}
		}

		// --------------------------------------------------------------------
		// IRcSequence 用に csvHead を設定（下位の IRcBase も設定）
		// --------------------------------------------------------------------
		private void SetSequenceCsvHead(List<String> csvHead, String fieldPrefix)
		{
			SetBaseCsvHead(csvHead, fieldPrefix);

			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_SEQUENCE);
			csvHead.Add(fieldPrefix + YlConstants.FIELD_SUFFIX_LINK_ID);
		}

		// --------------------------------------------------------------------
		// IRcSequence を csvRecord に設定（下位の IRcBase も設定）
		// --------------------------------------------------------------------
		private void SetSequenceCsvRecord(IRcSequence sequence, List<String> csvRecord)
		{
			SetBaseCsvRecord(sequence, csvRecord);

			csvRecord.Add(sequence.Sequence.ToString());
			csvRecord.Add(sequence.LinkId);
		}
	}
}
