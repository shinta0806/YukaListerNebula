// ============================================================================
// 
// ダウンロード済の同期データ（1 日分のデータ）を楽曲情報データベースに保存
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.SyncClient
{
	public class SyncDataImporter : SyncDataIo
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public SyncDataImporter(MainWindowViewModel mainWindowViewModel, String extractFolder)
		{
			// 初期化
			_mainWindowViewModel = mainWindowViewModel;
			Debug.Assert(extractFolder[^1] == '\\', "SyncDataImporter() bad extractFolder");
			_extractFolder = extractFolder;

			// 情報解析
			_syncInfos = AnalyzeSyncInfo();
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 同期情報に含まれている日付
		// --------------------------------------------------------------------
		public DateTime Date()
		{
			return DateTime.ParseExact(_syncInfos[SYNC_INFO_PARAM_DATE], YlConstants.SYNC_URL_DATE_FORMAT, null);
		}

		// --------------------------------------------------------------------
		// 同期データのインポート
		// --------------------------------------------------------------------
		public void Import(ref Int32 numTotalDownloads, ref Int32 numTotalImports)
		{
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "インポート中... " + _syncInfos[SYNC_INFO_PARAM_DATE]);

			String[] csvs = Directory.GetFiles(_extractFolder, "*" + Common.FILE_EXT_CSV);
			foreach (String csv in csvs)
			{
				Import(csv, ref numTotalDownloads, ref numTotalImports);
				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ファイル名
		private const String FILE_NAME_SYNC_INFO = "SyncInfo" + Common.FILE_EXT_TXT;

		// FILE_NAME_SYNC_INFO の中のパラメーター
		private const String SYNC_INFO_PARAM_DATE = "Date";

		// インポートの進捗を表示する間引き
		private const Int32 IMPORT_PROGRESS_BLOCK = 1000;

		// ====================================================================
		// private 変数
		// ====================================================================

		// メインウィンドウ
		private readonly MainWindowViewModel _mainWindowViewModel;

		// ダウンロード済の zip ファイルを解凍したフォルダー（末尾は '\\'）
		private readonly String _extractFolder;

		// 同期情報
		private readonly Dictionary<String, String> _syncInfos;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// SyncInfo.txt の内容を返す
		// --------------------------------------------------------------------
		private Dictionary<String, String> AnalyzeSyncInfo()
		{
			Dictionary<String, String> syncInfos = new();
			String[] syncInfoLines = File.ReadAllLines(_extractFolder + FILE_NAME_SYNC_INFO, Encoding.UTF8);
			foreach (String line in syncInfoLines)
			{
				Int32 pos = line.IndexOf('=');
				if (pos < 0)
				{
					continue;
				}
				syncInfos[line[0..pos]] = line[(pos + 1)..];
			}
			return syncInfos;
		}

		// --------------------------------------------------------------------
		// カウントを増やし、進捗状況をメインウィンドウステータスバーに表示
		// --------------------------------------------------------------------
		private void DisplayMusicInfoStatusIfNeeded(Int32 tableIndex, ref Int32 numChecks)
		{
			DisplayStatusIfNeededCore(YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[tableIndex], ref numChecks);
		}

		// --------------------------------------------------------------------
		// カウントを増やし、進捗状況をメインウィンドウステータスバーに表示
		// --------------------------------------------------------------------
		private void DisplayStatusIfNeededCore(String tableNameLabel, ref Int32 numChecks)
		{
			numChecks++;
			if (numChecks % IMPORT_PROGRESS_BLOCK == 0)
			{
				_mainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データをダウンロード中...（" + _syncInfos[SYNC_INFO_PARAM_DATE] + "）："
						+ tableNameLabel + "確認 " + numChecks.ToString("#,0") + " 件");
			}
		}

		// --------------------------------------------------------------------
		// カウントを増やし、進捗状況をメインウィンドウステータスバーに表示
		// --------------------------------------------------------------------
		private void DisplayYukariStatisticsStatusIfNeeded(ref Int32 numChecks)
		{
			DisplayStatusIfNeededCore("ゆかり統計", ref numChecks);
		}

		// --------------------------------------------------------------------
		// 同期データのインポート（1 ファイル分）
		// 常にローカルデータよりサーバーデータを優先する
		// --------------------------------------------------------------------
		private void Import(String csvPath, ref Int32 numTotalDownloads, ref Int32 numTotalImports)
		{
			// CSV ロード
			List<List<String>> csvContents = CsvManager.LoadCsv(csvPath, Encoding.UTF8);
			if (csvContents.Count < 1)
			{
				return;
			}

			// サーバーの仕様変更によりカラム順序が変わっても対応できるよう、Dictionary にする
			List<Dictionary<String, String>> syncData = new();
			for (Int32 i = 1; i < csvContents.Count; i++)
			{
				Dictionary<String, String> record = new();
				for (Int32 j = 0; j < csvContents[0].Count; j++)
				{
					record[csvContents[0][j]] = csvContents[i][j];
				}
				syncData.Add(record);
			}
			numTotalDownloads += syncData.Count;
			_mainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データをダウンロード中...（" + _syncInfos[SYNC_INFO_PARAM_DATE] + "）：合計 "
					+ numTotalDownloads.ToString("#,0") + " 件");

			// インポート
			String tableName = Path.GetFileNameWithoutExtension(csvPath);
			switch (tableName)
			{
				case TSong.TABLE_NAME_SONG:
					numTotalImports += ImportMaster(_musicInfoContext.Songs, TSong.FIELD_PREFIX_SONG, syncData);
					break;
				case TPerson.TABLE_NAME_PERSON:
					numTotalImports += ImportMaster(_musicInfoContext.People, TPerson.FIELD_PREFIX_PERSON, syncData);
					break;
				case TTieUp.TABLE_NAME_TIE_UP:
					numTotalImports += ImportMaster(_musicInfoContext.TieUps, TTieUp.FIELD_PREFIX_TIE_UP, syncData);
					break;
				case TTieUpGroup.TABLE_NAME_TIE_UP_GROUP:
					numTotalImports += ImportMaster(_musicInfoContext.TieUpGroups, TTieUpGroup.FIELD_PREFIX_TIE_UP_GROUP, syncData);
					break;
				case TMaker.TABLE_NAME_MAKER:
					numTotalImports += ImportMaster(_musicInfoContext.Makers, TMaker.FIELD_PREFIX_MAKER, syncData);
					break;
				case TSongAlias.TABLE_NAME_SONG_ALIAS:
					numTotalImports += ImportAlias(_musicInfoContext.SongAliases, TSongAlias.FIELD_PREFIX_SONG_ALIAS, syncData);
					break;
				case TTieUpAlias.TABLE_NAME_TIE_UP_ALIAS:
					numTotalImports += ImportAlias(_musicInfoContext.TieUpAliases, TTieUpAlias.FIELD_PREFIX_TIE_UP_ALIAS, syncData);
					break;
				case TArtistSequence.TABLE_NAME_ARTIST_SEQUENCE:
					numTotalImports += ImportSequence(_musicInfoContext.ArtistSequences, TArtistSequence.FIELD_PREFIX_ARTIST_SEQUENCE, syncData);
					break;
				case TLyristSequence.TABLE_NAME_LYRIST_SEQUENCE:
					numTotalImports += ImportSequence(_musicInfoContext.LyristSequences, TLyristSequence.FIELD_PREFIX_LYRIST_SEQUENCE, syncData);
					break;
				case TComposerSequence.TABLE_NAME_COMPOSER_SEQUENCE:
					numTotalImports += ImportSequence(_musicInfoContext.ComposerSequences, TComposerSequence.FIELD_PREFIX_COMPOSER_SEQUENCE, syncData);
					break;
				case TArrangerSequence.TABLE_NAME_ARRANGER_SEQUENCE:
					numTotalImports += ImportSequence(_musicInfoContext.ArrangerSequences, TArrangerSequence.FIELD_PREFIX_ARRANGER_SEQUENCE, syncData);
					break;
				case TTieUpGroupSequence.TABLE_NAME_TIE_UP_GROUP_SEQUENCE:
					numTotalImports += ImportSequence(_musicInfoContext.TieUpGroupSequences, TTieUpGroupSequence.FIELD_PREFIX_TIE_UP_GROUP_SEQUENCE, syncData);
					break;
				case TYukariStatistics.TABLE_NAME_YUKARI_STATISTICS:
					numTotalImports += ImportYukariStatistics(syncData);
					break;
				default:
					_logWriterSyncDetail.LogMessage(TraceEventType.Error, "ダウンロード：未対応のテーブルデータがありました：" + tableName);
					break;
			}

			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, tableName + "：ダウンロード " + numTotalDownloads.ToString("#,0") + " 件、うちインポート "
					+ numTotalImports.ToString("#,0") + " 件");
		}

		// --------------------------------------------------------------------
		// IRcAlias インポート
		// --------------------------------------------------------------------
		private Int32 ImportAlias<T>(DbSet<T> records, String fieldPrefix, List<Dictionary<String, String>> syncData) where T : class, IRcAlias, new()
		{
			Int32 numImports = 0;
			Int32 numChecks = 0;
			Int32 tableIndex = DbCommon.MusicInfoTableIndex<T>();
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[tableIndex] + "インポート中...");

			foreach (Dictionary<String, String> oneData in syncData)
			{
				T newRecord = new();
				try
				{
					SetAliasBySyncData(newRecord, fieldPrefix, oneData);
				}
				catch (Exception excep)
				{
					_logWriterSyncDetail.LogMessage(TraceEventType.Error, "別名レコード設定時エラー：" + excep.Message);
					continue;
				}
#if DEBUGz
				Debug.WriteLine("ImportAlias() " + newRecord.GetType().Name + ", " + newRecord.Alias);
#endif
				UpdateAliasDatabaseIfNeeded(records, newRecord, ref numImports);
				DisplayMusicInfoStatusIfNeeded(tableIndex, ref numChecks);

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			_musicInfoContext.SaveChanges();

			return numImports;
		}

		// --------------------------------------------------------------------
		// IRcMaster インポート
		// --------------------------------------------------------------------
		private Int32 ImportMaster<T>(DbSet<T> records, String fieldPrefix, List<Dictionary<String, String>> syncData) where T : class, IRcMaster, new()
		{
			Int32 numImports = 0;
			Int32 numChecks = 0;
			Int32 tableIndex = DbCommon.MusicInfoTableIndex<T>();
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[tableIndex] + "インポート中...");

			foreach (Dictionary<String, String> oneData in syncData)
			{
				T newRecord = new();
				try
				{
					SetMasterBySyncData(newRecord, fieldPrefix, oneData);
				}
				catch (Exception excep)
				{
					_logWriterSyncDetail.LogMessage(TraceEventType.Error, "マスターレコード設定時エラー：" + excep.Message);
					continue;
				}
				UpdateBaseDatabaseIfNeeded(records, newRecord, ref numImports);
				DisplayMusicInfoStatusIfNeeded(tableIndex, ref numChecks);

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			_musicInfoContext.SaveChanges();

			return numImports;
		}

		// --------------------------------------------------------------------
		// IRcSequence インポート
		// --------------------------------------------------------------------
		private Int32 ImportSequence<T>(DbSet<T> records, String fieldPrefix, List<Dictionary<String, String>> syncData) where T : class, IRcSequence, new()
		{
			Int32 numImports = 0;
			Int32 numChecks = 0;
			Int32 tableIndex = DbCommon.MusicInfoTableIndex<T>();
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[tableIndex] + "インポート中...");

			foreach (Dictionary<String, String> oneData in syncData)
			{
				T newRecord = new();
				try
				{
					SetSequenceBySyncData(newRecord, fieldPrefix, oneData);
				}
				catch (Exception excep)
				{
					_logWriterSyncDetail.LogMessage(TraceEventType.Error, "紐付レコード設定時エラー：" + excep.Message);
					continue;
				}
				UpdateSequenceDatabaseIfNeeded(records, newRecord, ref numImports);
				DisplayMusicInfoStatusIfNeeded(tableIndex, ref numChecks);

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			_musicInfoContext.SaveChanges();

			return numImports;
		}

		// --------------------------------------------------------------------
		// TYukariStatistics インポート
		// --------------------------------------------------------------------
		private Int32 ImportYukariStatistics(List<Dictionary<String, String>> syncData)
		{
			Int32 numImports = 0;
			Int32 numChecks = 0;
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり統計インポート中...");

			foreach (Dictionary<String, String> oneData in syncData)
			{
				TYukariStatistics newRecord = new();
				try
				{
					SetYukariStatisticsBySyncData(newRecord, oneData);
				}
				catch (Exception excep)
				{
					_logWriterSyncDetail.LogMessage(TraceEventType.Error, "ゆかり統計レコード設定時エラー：" + excep.Message);
					continue;
				}
				UpdateBaseDatabaseIfNeeded(_yukariStatisticsContext.YukariStatistics, newRecord, ref numImports);
				DisplayYukariStatisticsStatusIfNeeded(ref numChecks);

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			_yukariStatisticsContext.SaveChanges();

			return numImports;
		}

		// --------------------------------------------------------------------
		// 同期データから IRcAlias を設定（下位の IRcBase も設定）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SetAliasBySyncData(IRcAlias alias, String fieldPrefix, Dictionary<String, String> syncOneData)
		{
			SetBaseBySyncData(alias, fieldPrefix, syncOneData);

			String? al = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_ALIAS]);
			if (String.IsNullOrEmpty(al))
			{
				throw new Exception("同期データの別名が空です：" + alias.Id);
			}
			alias.Alias = al;
			String? originalId = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_ORIGINAL_ID]);
			if (String.IsNullOrEmpty(originalId))
			{
				throw new Exception("同期データの元の ID が空です：" + alias.Id);
			}
			alias.OriginalId = originalId;
		}

		// --------------------------------------------------------------------
		// 同期データから IRcBase を設定
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SetBaseBySyncData(IRcBase bas, String fieldPrefix, Dictionary<String, String> syncOneData)
		{
			String? id = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_ID]);
			if (String.IsNullOrEmpty(id))
			{
				throw new Exception("同期データの ID が空です");
			}
			bas.Id = id;
			bas.Import = SyncDataToBoolean(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_IMPORT]);
			bas.Invalid = SyncDataToBoolean(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_INVALID]);
			bas.UpdateTime = SyncDataToDouble(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_UPDATE_TIME]);
			bas.Dirty = false;
		}

		// --------------------------------------------------------------------
		// 同期データから IRcCategorizable を設定（下位は設定しない）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SetCategorizableBySyncData(IRcCategorizable categorizable, String fieldPrefix, Dictionary<String, String> syncOneData)
		{
			categorizable.CategoryId = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_CATEGORY_ID]);
			categorizable.ReleaseDate = SyncDataToDouble(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_RELEASE_DATE]);
		}

		// --------------------------------------------------------------------
		// 同期データから IRcMaster を設定（下位の IRcBase も設定）
		// IRcMaster より上位の TSong, TTieUp にも対応
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SetMasterBySyncData(IRcMaster master, String fieldPrefix, Dictionary<String, String> syncOneData)
		{
			SetBaseBySyncData(master, fieldPrefix, syncOneData);

			master.Name = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_NAME]);
			(master.Ruby, _, _) = YlCommon.NormalizeDbRubyForMusicInfo(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_RUBY]);
			(master.RubyForSearch, _, _) = YlCommon.NormalizeDbRubyForSearch(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_RUBY_FOR_SEARCH]);
			master.Keyword = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_KEYWORD]);
			(master.KeywordRubyForSearch, _, _) = YlCommon.NormalizeDbRubyForSearch(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH]);

			if (master is TSong song)
			{
				SetCategorizableBySyncData(song, fieldPrefix, syncOneData);
				song.TieUpId = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_TIE_UP_ID]);
				song.OpEd = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_OP_ED]);
			}
			else if (master is TTieUp tieUp)
			{
				SetCategorizableBySyncData(tieUp, fieldPrefix, syncOneData);
				tieUp.MakerId = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_MAKER_ID]);
				tieUp.AgeLimit = SyncDataToInt32(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_AGE_LIMIT]);
			}
		}

		// --------------------------------------------------------------------
		// 同期データから IRcSequence を設定（下位の IRcBase も設定）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SetSequenceBySyncData(IRcSequence sequence, String fieldPrefix, Dictionary<String, String> syncOneData)
		{
			SetBaseBySyncData(sequence, fieldPrefix, syncOneData);

			sequence.Sequence = SyncDataToInt32(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_SEQUENCE]);
			String? linkId = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_LINK_ID]);
			if (String.IsNullOrEmpty(linkId))
			{
				throw new Exception("同期データのリンク ID が空です：" + sequence.Id);
			}
			sequence.LinkId = linkId;
		}

		// --------------------------------------------------------------------
		// 同期データから TYukariStatistics を設定（下位の IRcBase も設定）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static void SetYukariStatisticsBySyncData(TYukariStatistics yukariStatistics, Dictionary<String, String> syncOneData)
		{
			SetBaseBySyncData(yukariStatistics, TYukariStatistics.FIELD_PREFIX_YUKARI_STATISTICS, syncOneData);

			yukariStatistics.RequestDatabasePath = syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_DATABASE_PATH];
			yukariStatistics.RequestTime = SyncDataToDouble(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_TIME]);
			yukariStatistics.AttributesDone = SyncDataToBoolean(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_ATTRIBUTES_DONE]);
			yukariStatistics.RoomName = syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_ROOM_NAME];
			yukariStatistics.RequestId = SyncDataToInt32(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_ID]);
			yukariStatistics.RequestMoviePath = syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_MOVIE_PATH];
			yukariStatistics.RequestSinger = syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_SINGER];
			yukariStatistics.RequestComment = syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_COMMENT];
			yukariStatistics.RequestOrder = SyncDataToInt32(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_ORDER]);
			yukariStatistics.RequestKeyChange = SyncDataToInt32(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_REQUEST_KEY_CHANGE]);
			yukariStatistics.Worker = syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_WORKER];
			yukariStatistics.SongReleaseDate = SyncDataToDouble(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_SONG_RELEASE_DATE]);
			yukariStatistics.CategoryName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_CATEGORY_NAME]);
			yukariStatistics.TieUpName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_TIE_UP_NAME]);
			yukariStatistics.TieUpAgeLimit = SyncDataToInt32(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_AGE_LIMIT]);
			yukariStatistics.MakerName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_MAKER_NAME]);
			yukariStatistics.TieUpGroupName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_TIE_UP_GROUP_NAME]);
			yukariStatistics.SongName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_SONG_NAME]);
			yukariStatistics.SongOpEd = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_SONG_OP_ED]);
			yukariStatistics.ArtistName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_ARTIST_NAME]);
			yukariStatistics.LyristName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_LYRIST_NAME]);
			yukariStatistics.ComposerName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_COMPOSER_NAME]);
			yukariStatistics.ArrangerName = YlCommon.NormalizeDbString(syncOneData[TYukariStatistics.FIELD_NAME_YUKARI_STATISTICS_ARRANGER_NAME]);
		}

		// --------------------------------------------------------------------
		// 文字列で受信した同期データを Boolean に変換
		// --------------------------------------------------------------------
		private static Boolean SyncDataToBoolean(String str)
		{
			if (String.IsNullOrEmpty(str))
			{
				return false;
			}

			return str[0] != '0';
		}

		// --------------------------------------------------------------------
		// 文字列で受信した同期データを Double に変換
		// --------------------------------------------------------------------
		private static Double SyncDataToDouble(String str)
		{
			_ = Double.TryParse(str, out Double doub);
			return doub;
		}

		// --------------------------------------------------------------------
		// 文字列で受信した同期データを Int32 に変換
		// --------------------------------------------------------------------
		private static Int32 SyncDataToInt32(String str)
		{
			_ = Int32.TryParse(str, out Int32 int32);
			return int32;
		}

		// --------------------------------------------------------------------
		// 同期データで楽曲情報データベースを更新
		// IRcAlias 用
		// --------------------------------------------------------------------
		private void UpdateAliasDatabaseIfNeeded<T>(DbSet<T> records, T newRecord, ref Int32 numImports) where T : class, IRcAlias
		{
			// 原因不明だが newRecord.Alias が既存のものと重複しているケースが発生した
			// EditMusicInfoWindowViewModel.Save() では無効データも含めて重複チェックをしているのだが……
			// あまりきれいな方法ではないが、ここでも重複チェックを行い、重複している場合は強制的にはじくようにする
			T? existRecord = DbCommon.SelectAliasByAlias(records, newRecord.Alias, true);
			if (existRecord != null && existRecord.Id != newRecord.Id)
			{
				_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "新規登録不可（別名重複）：" + newRecord.Id + " / " + newRecord.Alias);
			}
			else
			{
				UpdateBaseDatabaseIfNeeded(records, newRecord, ref numImports);
			}
		}

		// --------------------------------------------------------------------
		// 同期データで楽曲情報データベースを更新
		// IRcMaster, IRcAlias 用
		// --------------------------------------------------------------------
		private void UpdateBaseDatabaseIfNeeded<T>(DbSet<T> records, T newRecord, ref Int32 numImports) where T : class, IRcBase
		{
			String? logName;
			if (newRecord is IRcMaster newMaster)
			{
				logName = newMaster.Name;
			}
			else if (newRecord is IRcAlias newAlias)
			{
				logName = newAlias.Alias;
			}
			else
			{
				logName = newRecord.Id;
			}

			// ID が既にテーブル内にあるか確認
			T? existRecord = DbCommon.SelectBaseById(records, newRecord.Id, true);
			if (existRecord == null)
			{
				if (newRecord.Invalid)
				{
					// 無効データを新規登録する意味は無いのでスキップ
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "新規登録不要：" + newRecord.Id + " / " + logName);
				}
				else
				{
					// 新規登録
					records.Add(newRecord);
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "新規登録：" + newRecord.Id + " / " + logName);
					numImports++;
				}
			}
			else
			{
				if (existRecord.UpdateTime == newRecord.UpdateTime)
				{
					// 更新日時がサーバーと同じ場合は同期に支障ないので更新しない（ローカルで編集中の場合はローカルの編集が生きることになる）
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新不要：" + newRecord.Id + " / " + logName);
				}
				else
				{
					// 更新日時がサーバーと異なる場合はそのままではアップロードできないのでサーバーデータで上書きする
					Common.ShallowCopyFields(newRecord, existRecord);
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新：" + newRecord.Id + " / " + logName);
					numImports++;
				}
			}
		}

		// --------------------------------------------------------------------
		// 同期データで楽曲情報データベースを更新
		// IRcSequence 用
		// --------------------------------------------------------------------
		private void UpdateSequenceDatabaseIfNeeded<T>(DbSet<T> records, T newRecord, ref Int32 numImports) where T : class, IRcSequence
		{
			// ID・連番 が既にテーブル内にあるか確認
			T? existRecord = DbCommon.SelectSequencesById(records, newRecord.Id, true).FirstOrDefault(x => x.Sequence == newRecord.Sequence);
			if (existRecord == null)
			{
				// 新規登録（IRcSequence は紐付きの増減で無効データが生成され、ローカルデータと競合する可能性があるため、無効データも含めて登録する）
				records.Add(newRecord);
				_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "新規登録：" + newRecord.Id + " / " + newRecord.Sequence);
				numImports++;
			}
			else
			{
				if (existRecord.UpdateTime == newRecord.UpdateTime)
				{
					// 更新日時がサーバーと同じ場合は同期に支障ないので更新しない（ローカルで編集中の場合はローカルの編集が生きることになる）
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新不要：" + newRecord.Id + " / " + newRecord.Sequence);
				}
				else
				{
					// 更新日時がサーバーと異なる場合はそのままではアップロードできないのでサーバーデータで上書きする
					Common.ShallowCopyFields(newRecord, existRecord);
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新：" + newRecord.Id + " / " + newRecord.Sequence);
					numImports++;
				}
			}
		}
	}
}
