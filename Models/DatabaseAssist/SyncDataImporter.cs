// ============================================================================
// 
// 同期データを楽曲情報データベースに保存
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
using System.Threading.Tasks;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels;

namespace YukaLister.Models.DatabaseAssist
{
	public class SyncDataImporter : IDisposable
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public SyncDataImporter(MainWindowViewModel mainWindowViewModel, String extractFolder)
		{
			// 最初にログの設定
			YlCommon.SetLogWriterSyncDetail(_logWriterSyncDetail);

			// 初期化
			_mainWindowViewModel = mainWindowViewModel;
			Debug.Assert(extractFolder[^0] == '\\', "SyncDataImporter() bad extractFolder");
			_extractFolder = extractFolder;
			_musicInfoContext = MusicInfoContext.CreateContext(out _properties,
					out _songs, out _people, out _tieUps, out _categories,
					out _tieUpGroups, out _makers, out _tags,
					out _songAliases, out _personAliases, out _tieUpAliases,
					out _categoryAliases, out _tieUpGroupAliases, out _makerAliases,
					out _artistSequences, out _lyristSequences, out _composerSequences, out _arrangerSequences,
					out _tieUpGroupSequences, out _tagSequences);

			// 情報解析
			_syncInfos = AnalyzeSyncInfo();
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// IDisposable.Dispose()
		// --------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
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
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected virtual void Dispose(Boolean isDisposing)
		{
			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
				_musicInfoContext.Dispose();
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ファイル名
		private const String FILE_NAME_SYNC_INFO = "SyncInfo" + Common.FILE_EXT_TXT;

		// FILE_NAME_SYNC_INFO の中のパラメーター
		private const String SYNC_INFO_PARAM_DATE = "Date";

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// データベースプロパティーテーブル
		private readonly DbSet<TProperty> _properties;

		// --------------------------------------------------------------------
		// マスターテーブル
		// --------------------------------------------------------------------

		// 楽曲マスターテーブル
		private readonly DbSet<TSong> _songs;

		// 人物マスターテーブル
		private readonly DbSet<TPerson> _people;

		// タイアップマスターテーブル
		private readonly DbSet<TTieUp> _tieUps;

		// カテゴリーマスターテーブル
		private readonly DbSet<TCategory> _categories;

		// タイアップグループマスターテーブル
		private readonly DbSet<TTieUpGroup> _tieUpGroups;

		// 制作会社マスターテーブル
		private readonly DbSet<TMaker> _makers;

		// タグマスターテーブル
		private readonly DbSet<TTag> _tags;

		// --------------------------------------------------------------------
		// 別名テーブル
		// --------------------------------------------------------------------

		// 楽曲別名テーブル
		private readonly DbSet<TSongAlias> _songAliases;

		// 人物別名テーブル
		private readonly DbSet<TPersonAlias> _personAliases;

		// タイアップ別名テーブル
		private readonly DbSet<TTieUpAlias> _tieUpAliases;

		// カテゴリー別名テーブル
		private readonly DbSet<TCategoryAlias> _categoryAliases;

		// タイアップグループ別名テーブル
		private readonly DbSet<TTieUpGroupAlias> _tieUpGroupAliases;

		// 制作会社別名テーブル
		private readonly DbSet<TMakerAlias> _makerAliases;

		// --------------------------------------------------------------------
		// 紐付テーブル
		// --------------------------------------------------------------------

		// 歌手紐付テーブル
		private readonly DbSet<TArtistSequence> _artistSequences;

		// 作詞者紐付テーブル
		private readonly DbSet<TLyristSequence> _lyristSequences;

		// 作曲者紐付テーブル
		private readonly DbSet<TComposerSequence> _composerSequences;

		// 編曲者紐付テーブル
		private readonly DbSet<TArrangerSequence> _arrangerSequences;

		// タイアップグループ紐付テーブル
		private readonly DbSet<TTieUpGroupSequence> _tieUpGroupSequences;

		// タグ紐付テーブル
		private readonly DbSet<TTagSequence> _tagSequences;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// メインウィンドウ
		private MainWindowViewModel _mainWindowViewModel;

		// ダウンロード済の zip ファイルを解凍したフォルダー（末尾は '\\'）
		private String _extractFolder;

		// 楽曲情報データベースのコンテキスト
		private readonly MusicInfoContext _musicInfoContext;

		// 同期情報
		Dictionary<String, String> _syncInfos;

		// 詳細ログ（同期専用）
		private LogWriter _logWriterSyncDetail = new(YlConstants.APP_ID + YlConstants.SYNC_DETAIL_ID);

		// Dispose フラグ
		private Boolean _isDisposed;

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
				syncInfos[line[0..pos]] = line.Substring(pos + 1);
			}
			return syncInfos;
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
			List<Dictionary<String, String>> syncData = new List<Dictionary<String, String>>();
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

#if false
			// インポート
			String tableName = Path.GetFileNameWithoutExtension(csvPath);
			switch (tableName)
			{
				case TSong.TABLE_NAME_SONG:
					numTotalImports += ImportSyncDataTSong(oSyncInfos, syncData);
					break;
				case TPerson.TABLE_NAME_PERSON:
					numTotalImports += ImportSyncDataTPerson(oSyncInfos, syncData);
					break;
				case TTieUp.TABLE_NAME_TIE_UP:
					numTotalImports += ImportSyncDataTTieUp(oSyncInfos, syncData);
					break;
				case TTieUpGroup.TABLE_NAME_TIE_UP_GROUP:
					numTotalImports += ImportSyncDataTTieUpGroup(oSyncInfos, syncData);
					break;
				case TMaker.TABLE_NAME_MAKER:
					numTotalImports += ImportTMaker(syncData);
					break;
				case TSongAlias.TABLE_NAME_SONG_ALIAS:
					numTotalImports += ImportSyncDataTSongAlias(oSyncInfos, syncData);
					break;
				case TTieUpAlias.TABLE_NAME_TIE_UP_ALIAS:
					numTotalImports += ImportSyncDataTTieUpAlias(oSyncInfos, syncData);
					break;
				case TArtistSequence.TABLE_NAME_ARTIST_SEQUENCE:
					numTotalImports += ImportSyncDataTArtistSequence(oSyncInfos, syncData);
					break;
				case TLyristSequence.TABLE_NAME_LYRIST_SEQUENCE:
					numTotalImports += ImportSyncDataTLyristSequence(oSyncInfos, syncData);
					break;
				case TComposerSequence.TABLE_NAME_COMPOSER_SEQUENCE:
					numTotalImports += ImportSyncDataTComposerSequence(oSyncInfos, syncData);
					break;
				case TArrangerSequence.TABLE_NAME_ARRANGER_SEQUENCE:
					numTotalImports += ImportSyncDataTArrangerSequence(oSyncInfos, syncData);
					break;
				case TTieUpGroupSequence.TABLE_NAME_TIE_UP_GROUP_SEQUENCE:
					numTotalImports += ImportSyncDataTTieUpGroupSequence(oSyncInfos, syncData);
					break;
				default:
					_logWriterSyncDetail.LogMessage(TraceEventType.Error, "ダウンロード：未対応のテーブルデータがありました：" + tableName);
					break;
			}

			_logWriterSyncDetail.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, tableName + "：ダウンロード " + numTotalDownloads.ToString("#,0") + " 件、うちインポート "
					+ numTotalImports.ToString("#,0") + " 件");
#endif
		}

#if false
		// --------------------------------------------------------------------
		// TMaker インポート
		// --------------------------------------------------------------------
		private Int32 ImportTMaker(List<Dictionary<String, String>> syncData)
		{
			Int32 aNumImports = 0;
			Int32 aNumCheck = 0;
			_logWriterSyncDetail.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "制作会社マスターインポート中...");

			using (MusicInfoDatabaseInDisk aMusicInfoDbInDisk = new MusicInfoDatabaseInDisk(mEnvironment))
			using (DataContext aContext = new DataContext(aMusicInfoDbInDisk.Connection))
			{
				Table<TMaker> aTableMaker = aContext.GetTable<TMaker>();

				foreach (Dictionary<String, String> aOneData in syncData)
				{
					TMaker aDbNewRecord = new TMaker();

					// IRcBase
					aDbNewRecord.Id = YlCommon.NormalizeDbString(aOneData[TMaker.FIELD_NAME_MAKER_ID]);
					aDbNewRecord.Import = SyncDataToBoolean(aOneData[TMaker.FIELD_NAME_MAKER_IMPORT]);
					aDbNewRecord.Invalid = SyncDataToBoolean(aOneData[TMaker.FIELD_NAME_MAKER_INVALID]);
					aDbNewRecord.UpdateTime = SyncDataToDouble(aOneData[TMaker.FIELD_NAME_MAKER_UPDATE_TIME]);
					aDbNewRecord.Dirty = false;

					// TMaster
					aDbNewRecord.Name = YlCommon.NormalizeDbString(aOneData[TMaker.FIELD_NAME_MAKER_NAME]);
					aDbNewRecord.Ruby = YlCommon.NormalizeDbRuby(aOneData[TMaker.FIELD_NAME_MAKER_RUBY]);
					aDbNewRecord.Keyword = YlCommon.NormalizeDbString(aOneData[TMaker.FIELD_NAME_MAKER_KEYWORD]);

					// メーカー ID が既にテーブル内にあるか確認
					TMaker aDbExistRecord = aTableMaker.SingleOrDefault(x => x.Id == aDbNewRecord.Id);
					if (aDbExistRecord == null)
					{
						if (aDbNewRecord.Invalid)
						{
							// 無効データを新規登録する意味は無いのでスキップ
							mLogWriterSyncDetail.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "新規登録不要：" + aDbNewRecord.Id + " / " + aDbNewRecord.Name);
						}
						else
						{
							// 新規登録
							aTableMaker.InsertOnSubmit(aDbNewRecord);
							mLogWriterSyncDetail.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "新規登録：" + aDbNewRecord.Id + " / " + aDbNewRecord.Name);
							aNumImports++;
						}
					}
					else
					{
						if (aDbExistRecord.UpdateTime == aDbNewRecord.UpdateTime)
						{
							// 更新日時がサーバーと同じ場合は同期に支障ないので更新しない（ローカルで編集中の場合はローカルの編集が生きることになる）
							mLogWriterSyncDetail.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新不要：" + aDbNewRecord.Id + " / " + aDbNewRecord.Name);
						}
						else
						{
							// 更新日時がサーバーと異なる場合はそのままではアップロードできないのでサーバーデータで上書きする
							Common.ShallowCopy(aDbNewRecord, aDbExistRecord);
							mLogWriterSyncDetail.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "更新：" + aDbNewRecord.Id + " / " + aDbNewRecord.Name);
							aNumImports++;
						}
					}

					aNumCheck++;
					if (aNumCheck % IMPORT_PROGRESS_BLOCK == 0)
					{
						mMainWindowViewModel.SetStatusBarMessageWithInvoke(Common.TRACE_EVENT_TYPE_STATUS, "同期データをダウンロード中...（" + oSyncInfos[SYNC_INFO_PARAM_DATE] + "）：制作会社マスター確認 "
								+ aNumCheck.ToString("#,0") + " 件");
					}

					mEnvironment.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
				}

				aContext.SubmitChanges();
			}

			return aNumImports;
		}
#endif

	}
}
