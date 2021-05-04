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
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.DatabaseAssist
{
	public class SyncDataImporter : SyncDataIo
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
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
		// public メンバー関数
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
		// private メンバー変数
		// ====================================================================

		// メインウィンドウ
		private readonly MainWindowViewModel _mainWindowViewModel;

		// ダウンロード済の zip ファイルを解凍したフォルダー（末尾は '\\'）
		private readonly String _extractFolder;

		// 同期情報
		private readonly Dictionary<String, String> _syncInfos;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

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
			master.Ruby = YlCommon.NormalizeDbRubyForMusicInfo(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_RUBY]);
			master.RubyForSearch = YlCommon.NormalizeDbRubyForSearch(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_RUBY_FOR_SEARCH]);
			master.Keyword = YlCommon.NormalizeDbString(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_KEYWORD]);
			master.KeywordRubyForSearch = YlCommon.NormalizeDbRubyForSearch(syncOneData[fieldPrefix + YlConstants.FIELD_SUFFIX_KEYWORD_RUBY_FOR_SEARCH]);

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

		// ====================================================================
		// private メンバー関数
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
		private void DisplayStatusIfNeeded(Int32 tableIndex, ref Int32 numChecks)
		{
			numChecks++;
			if (numChecks % IMPORT_PROGRESS_BLOCK == 0)
			{
				_mainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データをダウンロード中...（" + _syncInfos[SYNC_INFO_PARAM_DATE] + "）："
						+ YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[tableIndex] + "確認 " + numChecks.ToString("#,0") + " 件");
			}
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
					numTotalImports += ImportMaster(_songs, TSong.FIELD_PREFIX_SONG, syncData);
					break;
				case TPerson.TABLE_NAME_PERSON:
					numTotalImports += ImportMaster(_people, TPerson.FIELD_PREFIX_PERSON, syncData);
					break;
				case TTieUp.TABLE_NAME_TIE_UP:
					numTotalImports += ImportMaster(_tieUps, TTieUp.FIELD_PREFIX_TIE_UP, syncData);
					break;
				case TTieUpGroup.TABLE_NAME_TIE_UP_GROUP:
					numTotalImports += ImportMaster(_tieUpGroups, TTieUpGroup.FIELD_PREFIX_TIE_UP_GROUP, syncData);
					break;
				case TMaker.TABLE_NAME_MAKER:
					numTotalImports += ImportMaster(_makers, TMaker.FIELD_PREFIX_MAKER, syncData);
					break;
				case TSongAlias.TABLE_NAME_SONG_ALIAS:
					numTotalImports += ImportAlias(_songAliases, TSongAlias.FIELD_PREFIX_SONG_ALIAS, syncData);
					break;
				case TTieUpAlias.TABLE_NAME_TIE_UP_ALIAS:
					numTotalImports += ImportAlias(_tieUpAliases, TTieUpAlias.FIELD_PREFIX_TIE_UP_ALIAS, syncData);
					break;
				case TArtistSequence.TABLE_NAME_ARTIST_SEQUENCE:
					numTotalImports += ImportSequence(_artistSequences, TArtistSequence.FIELD_PREFIX_ARTIST_SEQUENCE, syncData);
					break;
				case TLyristSequence.TABLE_NAME_LYRIST_SEQUENCE:
					numTotalImports += ImportSequence(_lyristSequences, TLyristSequence.FIELD_PREFIX_LYRIST_SEQUENCE, syncData);
					break;
				case TComposerSequence.TABLE_NAME_COMPOSER_SEQUENCE:
					numTotalImports += ImportSequence(_composerSequences, TComposerSequence.FIELD_PREFIX_COMPOSER_SEQUENCE, syncData);
					break;
				case TArrangerSequence.TABLE_NAME_ARRANGER_SEQUENCE:
					numTotalImports += ImportSequence(_arrangerSequences, TArrangerSequence.FIELD_PREFIX_ARRANGER_SEQUENCE, syncData);
					break;
				case TTieUpGroupSequence.TABLE_NAME_TIE_UP_GROUP_SEQUENCE:
					numTotalImports += ImportSequence(_tieUpGroupSequences, TTieUpGroupSequence.FIELD_PREFIX_TIE_UP_GROUP_SEQUENCE, syncData);
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
				UpdateBaseDatabaseIfNeeded(records, newRecord, ref numImports);
				DisplayStatusIfNeeded(tableIndex, ref numChecks);

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
				DisplayStatusIfNeeded(tableIndex, ref numChecks);

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
				DisplayStatusIfNeeded(tableIndex, ref numChecks);

				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}

			_musicInfoContext.SaveChanges();

			return numImports;
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
				Debug.Assert(false, "UpdateBaseDatabaseIfNeeded() bad newRecord type");
				logName = null;
			}

			// ID が既にテーブル内にあるか確認
			T? existRecord = DbCommon.SelectBaseById(records, newRecord.Id);
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
					Common.ShallowCopy(newRecord, existRecord);
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
					Common.ShallowCopy(newRecord, existRecord);
					_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新：" + newRecord.Id + " / " + newRecord.Sequence);
					numImports++;
				}
			}
		}
	}
}
