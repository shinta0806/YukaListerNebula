// ============================================================================
// 
// データベース共通で使用する関数
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

#if MOCHIKARA_PRODUCER_DB
using MochikaraProducer.Models.Database.DatabaseContexts;
#endif
#if MOCHIKARA_PRODUCER || MOCHIKARA_PRODUCER_DB
using MochikaraProducer.Models.SharedMisc;
#endif

#if MOCHIKARA_PRODUCER || MOCHIKARA_PRODUCER_DB
using MochikaraProducer.Models.MpModels;
#endif

#if MOCHIKARA_PRODUCER_DB || MOCHIKARA_PRODUCER_DB
using MochikaraProducerDb.Models.SharedMisc;
#endif

using Shinta;

#if YUKALISTER
using System.Reflection;
#endif

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseAssist;

internal class DbCommon
{
	// ====================================================================
	// public 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// データベースファイルのバックアップを作成
	// --------------------------------------------------------------------
	public static void BackupDatabase(String srcPath)
	{
		String? srcFolder = Path.GetDirectoryName(srcPath);
		if (String.IsNullOrEmpty(srcFolder))
		{
			return;
		}
		if (!File.Exists(srcPath))
		{
			return;
		}

		String fileNameForLog = Path.GetFileNameWithoutExtension(srcPath);
		try
		{
			// バックアップ先の決定（既に存在する場合はバックアップをスキップ：1 日 1 回まで）
			FileInfo srcFileInfo = new(srcPath);
			String backupPath = YukaListerDatabaseFullFolder() + Path.GetFileNameWithoutExtension(srcPath) + "_(bak)_" + srcFileInfo.LastWriteTime.ToString("yyyy_MM_dd") + Common.FILE_EXT_BAK;
			if (File.Exists(backupPath))
			{
				return;
			}

			// バックアップ
			File.Copy(srcPath, backupPath);
			Log.Information("データベース " + fileNameForLog + " のバックアップ作成：" + backupPath);

			// 溢れたバックアップを削除
			List<FileInfo> backupFileInfos = [];
			String[] backupFiles = Directory.GetFiles(srcFolder, Path.GetFileNameWithoutExtension(srcPath) + "_(bak)_*" + Common.FILE_EXT_BAK);
			foreach (String backupFile in backupFiles)
			{
				backupFileInfos.Add(new FileInfo(backupFile));
			}
			backupFileInfos.Sort((a, b) => -a.LastWriteTime.CompareTo(b.LastWriteTime));
			for (Int32 i = backupFileInfos.Count - 1; i >= NUM_DB_BACKUP_GENERATIONS; i--)
			{
				File.Delete(backupFileInfos[i].FullName);
				Log.Information("データベース " + fileNameForLog + " のバックアップ削除：" + backupFileInfos[i].FullName);
			}
		}
		catch (Exception excep)
		{
			// スプラッシュウィンドウに隠れる恐れがあるため表示はしない
			SerilogUtils.LogException("データベース " + fileNameForLog + " バックアップ時エラー", excep);
		}
	}

	// --------------------------------------------------------------------
	// データベース接続
	// --------------------------------------------------------------------
	public static SqliteConnection Connect(String path)
	{
		SqliteConnectionStringBuilder stringBuilder = new()
		{
			DataSource = path,
		};
		return new SqliteConnection(stringBuilder.ToString());
	}

#if YUKALISTER
	// --------------------------------------------------------------------
	// TFound → TYukariStatistics へコピー（TFound の属性情報がある場合のみ）
	// --------------------------------------------------------------------
	public static void CopyFoundToYukariStatisticsIfAttributesPrepared(TFound found, TYukariStatistics yukariStatistics)
	{
		if (found.FileSize < 0)
		{
			Debug.WriteLine("CopyFoundToYukariStatisticsIfAttributesPrepared() 属性確認しようとしたがまだ整理されていない " + yukariStatistics.RequestMoviePath);
			return;
		}

		yukariStatistics.Dirty |= !yukariStatistics.AttributesDone;
		yukariStatistics.AttributesDone = true;

		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.Worker), yukariStatistics, nameof(yukariStatistics.Worker));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.SongReleaseDate), yukariStatistics, nameof(yukariStatistics.SongReleaseDate));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.Category), yukariStatistics, nameof(yukariStatistics.CategoryName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.TieUpName), yukariStatistics, nameof(yukariStatistics.TieUpName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.TieUpAgeLimit), yukariStatistics, nameof(yukariStatistics.TieUpAgeLimit));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.MakerName), yukariStatistics, nameof(yukariStatistics.MakerName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.TieUpGroupName), yukariStatistics, nameof(yukariStatistics.TieUpGroupName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.SongName), yukariStatistics, nameof(yukariStatistics.SongName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.SongOpEd), yukariStatistics, nameof(yukariStatistics.SongOpEd));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.ArtistName), yukariStatistics, nameof(yukariStatistics.ArtistName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.LyristName), yukariStatistics, nameof(yukariStatistics.LyristName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.ComposerName), yukariStatistics, nameof(yukariStatistics.ComposerName));
		CopyFoundToYukariStatisticsIfUpdated(found, nameof(found.ArrangerName), yukariStatistics, nameof(yukariStatistics.ArrangerName));

		Debug.WriteLine("CopyFoundToYukariStatisticsIfAttributesPrepared() 属性確認実施 " + yukariStatistics.RequestMoviePath);
	}

	// --------------------------------------------------------------------
	// TFound → TYukariStatistics へプロパティーを 1 つコピー（TFound と TYukariStatistics が異なる場合のみ）
	// --------------------------------------------------------------------
	public static void CopyFoundToYukariStatisticsIfUpdated(TFound found, String foundPropertyName, TYukariStatistics yukariStatistics, String statisticsPropertyName)
	{
		Type foundType = typeof(TFound);
		PropertyInfo? foundPropertyInfo = foundType.GetProperty(foundPropertyName);
		Type statisticsType = typeof(TYukariStatistics);
		PropertyInfo? statisticsPropertyInfo = statisticsType.GetProperty(statisticsPropertyName);
		Debug.Assert(foundPropertyInfo != null && statisticsPropertyInfo != null, "CopyFoundToYukariStatisticsIfUpdated() bad propertyName");

		Object? foundValue = foundPropertyInfo.GetValue(found);
		Object? statisticsValue = statisticsPropertyInfo.GetValue(yukariStatistics);

		if (foundPropertyInfo.PropertyType == typeof(String))
		{
			// String の場合は null と String.Empty を同値扱い
			if (String.IsNullOrEmpty((String?)foundValue) && String.IsNullOrEmpty((String?)statisticsValue))
			{
				return;
			}
		}
		if (foundValue == null && statisticsValue == null || foundValue?.Equals(statisticsValue) == true)
		{
			return;
		}

		// 異なるのでコピー
		Debug.WriteLine("CopyFoundToYukariStatisticsIfUpdated() copy " + yukariStatistics.Id + ", " + foundPropertyInfo.Name + ": " + foundValue);
		statisticsPropertyInfo.SetValue(yukariStatistics, foundValue);
		yukariStatistics.Dirty = true;
	}
#endif

	/// <summary>
	/// NoTracking な ListContextInMemory を作成
	/// </summary>
	/// <returns>呼出元で Dispose() する必要あり</returns>
	public static ListContextInMemory CreateNoTrackingListContextInMemory()
	{
		ListContextInMemory listContextInMemory = new();
		listContextInMemory.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
		return listContextInMemory;
	}

#if MOCHIKARA_PRODUCER_DB
	/// <summary>
	/// NoTracking な OrganizedContextInMemory を作成
	/// </summary>
	/// <returns>呼出元で Dispose() する必要あり</returns>
	public static OrganizedContextInMemory CreateNoTrackingOrganizedContextInMemory()
	{
		OrganizedContextInMemory organizedContextInMemory = new();
		organizedContextInMemory.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
		return organizedContextInMemory;
	}

	/// <summary>
	/// NoTracking な RequestListContext を作成
	/// </summary>
	/// <returns>呼出元で Dispose() する必要あり</returns>
	public static RequestListContext CreateNoTrackingRequestListContext()
	{
		RequestListContext requestListContext = new();
		requestListContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
		return requestListContext;
	}
#endif

	// --------------------------------------------------------------------
	// 標準的な計算方法で算出される DisplayCategoryName
	// --------------------------------------------------------------------
	public static String? DisplayCategoryNameByDefaultAlgorithm(String? displayCategoryName, String? categoryId)
	{
		if (displayCategoryName == null && categoryId != null)
		{
			using MusicInfoContextDefault musicInfoContextDefault = new();
			displayCategoryName = DbCommon.SelectBaseById(musicInfoContextDefault.Categories, categoryId)?.Name;
		}
		return displayCategoryName;
	}

	// --------------------------------------------------------------------
	// 標準的な計算方法で算出される DisplayName
	// --------------------------------------------------------------------
	public static String? DisplayNameByDefaultAlgorithm<T>(T master) where T : IRcMaster
	{
		if (master.AvoidSameName)
		{
			return master.Name + "（" + (String.IsNullOrEmpty(master.Keyword) ? "キーワード無し" : master.Keyword) + "）";
		}
		else
		{
			return master.Name;
		}
	}

	// --------------------------------------------------------------------
	// 標準的な計算方法で算出される DisplayReleaseDate
	// --------------------------------------------------------------------
	public static String? DisplayReleaseDateByDefaultAlgorithm<T>(T categorizable) where T : IRcCategorizable
	{
		if (categorizable.ReleaseDate <= YlConstants.INVALID_MJD)
		{
			return null;
		}
		return JulianDay.ModifiedJulianDateToDateTime(categorizable.ReleaseDate).ToString(YlConstants.DATE_FORMAT);
	}

	// --------------------------------------------------------------------
	// 有効なレコードのみに絞り込む
	// --------------------------------------------------------------------
	public static List<T> ExceptInvalid<T>(DbSet<T> records, List<String> validAndInvalidItemIds) where T : class, IRcBase
	{
		List<T> valids = [];
		foreach (String id in validAndInvalidItemIds)
		{
			T? item = SelectBaseById(records, id);
			if (item != null)
			{
				valids.Add(item);
			}
		}
		return valids;
	}

	// --------------------------------------------------------------------
	// 指定の参照項目を持つ紐付テーブルを無効化
	// SaveChanges() は呼び出し元で実施する必要がある
	// --------------------------------------------------------------------
	public static void InvalidateSequenceByLinkId<T>(DbSet<T> records, String linkId) where T : class, IRcSequence
	{
		List<T> validSequences = [.. records.Where(x => x.LinkId == linkId && !x.Invalid)];
		for (Int32 i = 0; i < validSequences.Count; i++)
		{
			validSequences[i].Invalid = true;
			validSequences[i].Dirty = true;
			Log.Information(typeof(T).Name + " 紐付テーブル無効化：" + validSequences[i].Id + " / " + i.ToString());
		}
	}

#if YUKALISTER
	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（IRcAlias）
	// --------------------------------------------------------------------
	public static Boolean IsRcAliasUpdated(IRcAlias existRecord, IRcAlias newRecord)
	{
		Boolean? isRcBaseUpdated = IsRcBaseUpdatedCore(existRecord, newRecord);
		if (isRcBaseUpdated != null)
		{
			return isRcBaseUpdated.Value;
		}

		return existRecord.Alias != newRecord.Alias
				|| existRecord.OriginalId != newRecord.OriginalId;
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（IRcMaster）
	// --------------------------------------------------------------------
	public static Boolean IsRcMasterUpdated(IRcMaster existRecord, IRcMaster newRecord)
	{
		return IsRcMasterUpdatedCore(existRecord, newRecord) ?? false;
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（IRcSequence）
	// --------------------------------------------------------------------
	public static Boolean IsRcSequenceUpdated(IRcSequence existRecord, IRcSequence newRecord)
	{
		Boolean? isRcBaseUpdated = IsRcBaseUpdatedCore(existRecord, newRecord);
		if (isRcBaseUpdated != null)
		{
			return isRcBaseUpdated.Value;
		}

		return existRecord.LinkId != newRecord.LinkId;
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（TSong）
	// --------------------------------------------------------------------
	public static Boolean IsRcSongUpdated(TSong existRecord, TSong newRecord)
	{
		Boolean? isRcCategorizableUpdated = IsRcCategorizableUpdatedCore(existRecord, newRecord);
		if (isRcCategorizableUpdated != null)
		{
			return isRcCategorizableUpdated.Value;
		}

		return existRecord.TieUpId != newRecord.TieUpId
				|| existRecord.OpEd != newRecord.OpEd;
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（TTieUp）
	// --------------------------------------------------------------------
	public static Boolean IsRcTieUpUpdated(TTieUp existRecord, TTieUp newRecord)
	{
		Boolean? isRcCategorizableUpdated = IsRcCategorizableUpdatedCore(existRecord, newRecord);
		if (isRcCategorizableUpdated != null)
		{
			return isRcCategorizableUpdated.Value;
		}

		return existRecord.MakerId != newRecord.MakerId
				|| existRecord.AgeLimit != newRecord.AgeLimit;
	}

	// --------------------------------------------------------------------
	// リストデータベース（ゆかり用：ディスク）のフルパス
	// --------------------------------------------------------------------
	public static String ListDatabasePath(YlSettings ylSettings)
	{
		return YukariDatabaseFullFolder(ylSettings) + FILE_NAME_LIST_DATABASE_IN_DISK;
	}
#endif

	// --------------------------------------------------------------------
	// 例外がデータベース系の場合に詳細をログする
	// --------------------------------------------------------------------
	public static void LogDatabaseExceptionIfCan(Exception excep)
	{
		if (excep is DbUpdateException dbUpdateExcep)
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append("DbUpdateException Entries\n");
			foreach (EntityEntry entry in dbUpdateExcep.Entries)
			{
				stringBuilder.Append("Name: " + entry.Entity.GetType().Name + ", State: " + entry.State);
				if (entry.Entity is IRcBase rcBase)
				{
					stringBuilder.Append(", ID: " + rcBase.Id);
				}
				stringBuilder.Append('\n');
			}
			stringBuilder.Append("Inner Message: " + dbUpdateExcep.InnerException?.Message);
			Log.Error(stringBuilder.ToString());
		}
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースマスター詳細編集ウィンドウ用の編集対象マスター群
	// 同名の既存マスター＋新規マスター
	// --------------------------------------------------------------------
	public static List<T> MastersForEdit<T>(DbSet<T> masters, String? masterName) where T : class, IRcMaster, new()
	{
		List<T> sameNameMasters = SelectMastersByName(masters, masterName);

		// 新規作成用を追加
		T newRecord = new()
		{
			// IRcBase
			Id = String.Empty,
			Import = false,
			Invalid = false,
			UpdateTime = YlConstants.INVALID_MJD,
			Dirty = true,

			// IRcMaster
			Name = masterName,
			Ruby = null,
			Keyword = null,
		};
		sameNameMasters.Insert(0, newRecord);

		return sameNameMasters;
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースのテーブル番号
	// データベース自体に付与されている番号ではなく、内部での各種定数利用用
	// --------------------------------------------------------------------
	public static Int32 MusicInfoTableIndex<T>() where T : class, IRcBase
	{
		// case に typeof(hoge) が使えないので switch は使えない
		if (typeof(T) == typeof(TSong))
		{
			return (Int32)MusicInfoTables.TSong;
		}
		else if (typeof(T) == typeof(TPerson))
		{
			return (Int32)MusicInfoTables.TPerson;
		}
		else if (typeof(T) == typeof(TTieUp))
		{
			return (Int32)MusicInfoTables.TTieUp;
		}
		else if (typeof(T) == typeof(TCategory))
		{
			return (Int32)MusicInfoTables.TCategory;
		}
		else if (typeof(T) == typeof(TTieUpGroup))
		{
			return (Int32)MusicInfoTables.TTieUpGroup;
		}
		else if (typeof(T) == typeof(TMaker))
		{
			return (Int32)MusicInfoTables.TMaker;
		}
		else if (typeof(T) == typeof(TTag))
		{
			return (Int32)MusicInfoTables.TTag;
		}
		else if (typeof(T) == typeof(TSongAlias))
		{
			return (Int32)MusicInfoTables.TSongAlias;
		}
		else if (typeof(T) == typeof(TPersonAlias))
		{
			return (Int32)MusicInfoTables.TPersonAlias;
		}
		else if (typeof(T) == typeof(TTieUpAlias))
		{
			return (Int32)MusicInfoTables.TTieUpAlias;
		}
		else if (typeof(T) == typeof(TCategoryAlias))
		{
			return (Int32)MusicInfoTables.TCategoryAlias;
		}
		else if (typeof(T) == typeof(TTieUpGroupAlias))
		{
			return (Int32)MusicInfoTables.TTieUpGroupAlias;
		}
		else if (typeof(T) == typeof(TMakerAlias))
		{
			return (Int32)MusicInfoTables.TMakerAlias;
		}
		else if (typeof(T) == typeof(TArtistSequence))
		{
			return (Int32)MusicInfoTables.TArtistSequence;
		}
		else if (typeof(T) == typeof(TLyristSequence))
		{
			return (Int32)MusicInfoTables.TLyristSequence;
		}
		else if (typeof(T) == typeof(TComposerSequence))
		{
			return (Int32)MusicInfoTables.TComposerSequence;
		}
		else if (typeof(T) == typeof(TArrangerSequence))
		{
			return (Int32)MusicInfoTables.TArrangerSequence;
		}
		else if (typeof(T) == typeof(TTieUpGroupSequence))
		{
			return (Int32)MusicInfoTables.TTieUpGroupSequence;
		}
		else if (typeof(T) == typeof(TTagSequence))
		{
			return (Int32)MusicInfoTables.TTagSequence;
		}
		else
		{
			return -1;
		}
	}

	// --------------------------------------------------------------------
	// データベースファイルを準備
	// --------------------------------------------------------------------
	public static void PrepareDatabases()
	{
		try
		{
			// メモリ DB は常に作成
			// using しない
			ListContextInMemory listContextInMemory = new();
			listContextInMemory.CreateDatabase();
#if YUKALISTER
			YlModel.Instance.EnvModel.ListContextInMemory = listContextInMemory;
#endif
#if MOCHIKARA_PRODUCER_DB
			MpModel.Instance.EnvModel.ListContextInMemory = listContextInMemory;

			OrganizedContextInMemory organizedContextInMemory = new();
			organizedContextInMemory.CreateDatabase();
			MpModel.Instance.EnvModel.OrganizedContextInMemory = organizedContextInMemory;
#endif

			// 楽曲情報データベース等が存在しない場合は作成
			Directory.CreateDirectory(YukaListerDatabaseFullFolder());

			using MusicInfoContextDefault musicInfoContextDefault = new();
#if MOCHIKARA_PRODUCER_DB
			CopyMusicInfoDatabase(musicInfoContextDefault);
#endif
			musicInfoContextDefault.CreateDatabaseIfNeeded();

#if YUKALISTER
			using YukariStatisticsContext yukariStatisticsContext = new();
			yukariStatisticsContext.CreateDatabaseIfNeeded();

			// ゆかり用データベース等は、ゆかり設定ファイルが正しく指定されている場合のみ
			// スプラッシュウィンドウからも呼ばれるため YukaListerWholeStatus（その時点では未初期化）は使えない
			if (!YlModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				return;
			}

			Directory.CreateDirectory(YukariDatabaseFullFolder(YlModel.Instance.EnvModel.YlSettings));

			// 存在しない場合は作成
			using ReportContext reportContext = new();
			reportContext.CreateDatabaseIfNeeded();

			using ThumbContext thumbContext = new();
			thumbContext.CreateDatabaseIfNeeded();

			// 常に作成（クリア）
			using ListContextInDisk listContextInDisk = new();
			listContextInDisk.CreateDatabase();
#endif

#if MOCHIKARA_PRODUCER_DB
			// 予約一覧
			using RequestListContext requestListContext = new();
			requestListContext.CreateDatabaseIfNeeded();
#endif
		}
		catch (Exception excep)
		{
			// スプラッシュウィンドウに隠れる恐れがあるため表示はしない
			SerilogUtils.LogException("データベース準備時エラー", excep);
		}
	}

	// --------------------------------------------------------------------
	// データベースのプロパティーを取得
	// --------------------------------------------------------------------
	public static TProperty Property(DbSet<TProperty> properties)
	{
		try
		{
			return properties.AsNoTracking().First();
		}
		catch (Exception)
		{
			return new TProperty();
		}
	}

#if YUKALISTER
	// --------------------------------------------------------------------
	// 紐付テーブルに新規登録または更新
	// SaveChanges() は呼び出し元で実施する必要がある
	// --------------------------------------------------------------------
	public static void RegisterSequence<T>(DbSet<T> records, String id, List<String> linkIds, Boolean isImport = false) where T : class, IRcSequence, new()
	{
		// 新規レコード
		List<T> newSequences = [];
		for (Int32 i = 0; i < linkIds.Count; i++)
		{
			T newSequence = CreateSequenceRecord<T>(id, i, linkIds[i], isImport);
			newSequences.Add(newSequence);
		}

		// 既存レコード
		List<T> existSequences = SelectSequencesById<T>(records, id, true);

		// 既存レコードがインポートではなく新規レコードがインポートの場合は更新しない
		if (existSequences.Count > 0 && !existSequences[0].Import
				&& newSequences.Count > 0 && newSequences[0].Import)
		{
			return;
		}

		// 既存レコードがある場合は更新
		for (Int32 i = 0; i < Math.Min(newSequences.Count, existSequences.Count); i++)
		{
			if (IsRcSequenceUpdated(existSequences[i], newSequences[i]))
			{
				newSequences[i].UpdateTime = existSequences[i].UpdateTime;
				Common.ShallowCopyFields(newSequences[i], existSequences[i]);
				if (!isImport)
				{
					Log.Information(typeof(T).Name + " 紐付テーブル更新：" + id + " / " + i.ToString());
				}
			}
		}

		// 既存レコードがない部分は新規登録
		for (Int32 i = existSequences.Count; i < newSequences.Count; i++)
		{
			records.Add(newSequences[i]);
			if (!isImport)
			{
				Log.Information(typeof(T).Name + " 紐付テーブル新規登録：" + id + " / " + i.ToString());
			}
		}

		// 既存レコードが余る部分は無効化
		for (Int32 i = newSequences.Count; i < existSequences.Count; i++)
		{
			if (!existSequences[i].Invalid)
			{
				existSequences[i].Invalid = true;
				existSequences[i].Dirty = true;
				if (!isImport)
				{
					Log.Information(typeof(T).Name + " 紐付テーブル無効化：" + id + " / " + i.ToString());
				}
			}
		}
	}

	// --------------------------------------------------------------------
	// リスト問題報告データベースのフルパス
	// --------------------------------------------------------------------
	public static String ReportDatabasePath(YlSettings ylSettings)
	{
		return YukariDatabaseFullFolder(ylSettings) + FILE_NAME_REPORT_DATABASE;
	}
#endif

	// --------------------------------------------------------------------
	// 楽曲情報データベースから別名を検索
	// 見つからない場合は null
	// --------------------------------------------------------------------
	public static T? SelectAliasByAlias<T>(IQueryable<T> records, String? alias, Boolean includesInvalid = false) where T : class, IRcAlias
	{
		if (String.IsNullOrEmpty(alias))
		{
			return null;
		}
		return records.SingleOrDefault(x => x.Alias == alias && (includesInvalid || !x.Invalid));
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースから IRcBase を検索
	// 見つからない場合は null
	// --------------------------------------------------------------------
	public static T? SelectBaseById<T>(IQueryable<T> records, String? id, Boolean includesInvalid = false) where T : class, IRcBase
	{
		if (String.IsNullOrEmpty(id))
		{
			return null;
		}
		return records.SingleOrDefault(x => x.Id == id && (includesInvalid || !x.Invalid));
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースからカテゴリー名を列挙
	// --------------------------------------------------------------------
	public static List<String> SelectCategoryNames(IQueryable<TCategory> categories, Boolean includesInvalid = false)
	{
		return [.. categories.Where(x => (includesInvalid || !x.Invalid) && !String.IsNullOrEmpty(x.Name)).Select(x => x.Name)!];
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースから IRcMaster を 1 つだけ検索
	// --------------------------------------------------------------------
	public static T? SelectMasterByName<T>(IQueryable<T> records, String? name, Boolean includesInvalid = false) where T : class, IRcMaster
	{
		if (String.IsNullOrEmpty(name))
		{
			return null;
		}
		return records.FirstOrDefault(x => x.Name == name && (includesInvalid || !x.Invalid));
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースから IRcMaster をすべて検索
	// --------------------------------------------------------------------
	public static List<T> SelectMastersByName<T>(IQueryable<T> records, String? name, Boolean includesInvalid = false) where T : class, IRcMaster
	{
		if (String.IsNullOrEmpty(name))
		{
			return [];
		}
		return [.. records.Where(x => x.Name == name && (includesInvalid || !x.Invalid))];
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースから IRcMaster をすべて検索（大文字小文字を区別しない）
	// --------------------------------------------------------------------
	public static List<T> SelectMastersByNameCaseInsensitive<T>(IQueryable<T> records, String? name, Boolean includesInvalid = false) where T : class, IRcMaster
	{
		if (String.IsNullOrEmpty(name))
		{
			return [];
		}
		return [.. records.Where(x => x.Name != null && EF.Functions.Like(x.Name, $"{name}") && (includesInvalid || !x.Invalid))];
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースから楽曲に紐付く人物を検索
	// sequenceRecords の型によって歌手、作曲者、作詞者、編曲者のいずれかを検索
	// 現時点では includesInvalid == true の用途が思いつかないため引数として includesInvalid は装備しない
	// 引数として includesInvalid を装備する場合は返値を List<TPerson?> にする必要があると思う
	// --------------------------------------------------------------------
	public static List<TPerson> SelectSequencedPeopleBySongId<T>(IQueryable<T> sequenceRecords, IQueryable<TPerson> personRecords, String songId) where T : class, IRcSequence
	{
		List<T> sequences = SelectSequencesById(sequenceRecords, songId);
		List<TPerson> people = [];

		foreach (T sequence in sequences)
		{
			TPerson? person = SelectBaseById(personRecords, sequence.LinkId);
			if (person != null)
			{
				people.Add(person);
			}
		}

		return people;
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースから楽曲に紐付くタグを検索
	// 現時点では includesInvalid == true の用途が思いつかないため引数として includesInvalid は装備しない
	// 引数として includesInvalid を装備する場合は返値を List<TTag?> にする必要があると思う
	// --------------------------------------------------------------------
	public static List<TTag> SelectSequencedTagsBySongId(IQueryable<TTagSequence> tagSequenceRecords, IQueryable<TTag> tagRecords, String songId)
	{
		List<TTagSequence> sequences = SelectSequencesById(tagSequenceRecords, songId);
		List<TTag> tags = [];

		foreach (TTagSequence sequence in sequences)
		{
			TTag? tag = SelectBaseById(tagRecords, sequence.LinkId);
			if (tag != null)
			{
				tags.Add(tag);
			}
		}

		return tags;
	}

	// --------------------------------------------------------------------
	// 楽曲情報データベースからタイアップに紐付くタイアップグループを検索
	// 現時点では includesInvalid == true の用途が思いつかないため引数として includesInvalid は装備しない
	// 引数として includesInvalid を装備する場合は返値を List<TTieUpGroup?> にする必要があると思う
	// --------------------------------------------------------------------
	public static List<TTieUpGroup> SelectSequencedTieUpGroupsByTieUpId(IQueryable<TTieUpGroupSequence> tieUpGroupSequenceRecords, IQueryable<TTieUpGroup> tieUpGroupRecords, String tieUpId)
	{
		List<TTieUpGroupSequence> sequences = SelectSequencesById(tieUpGroupSequenceRecords, tieUpId);
		List<TTieUpGroup> tieUpGroups = [];

		foreach (TTieUpGroupSequence sequence in sequences)
		{
			TTieUpGroup? tieUpGroup = SelectBaseById(tieUpGroupRecords, sequence.LinkId);
			if (tieUpGroup != null)
			{
				tieUpGroups.Add(tieUpGroup);
			}
		}

		return tieUpGroups;
	}

	// --------------------------------------------------------------------
	// 紐付データベースから紐付を検索
	// 紐付更新時は includesInvalid == true で呼ばれる
	// --------------------------------------------------------------------
	public static List<T> SelectSequencesById<T>(IQueryable<T> records, String id, Boolean includesInvalid = false) where T : class, IRcSequence
	{
		if (String.IsNullOrEmpty(id))
		{
			return [];
		}

		return [.. records.Where(x => x.Id == id && (includesInvalid || !x.Invalid)).OrderBy(x => x.Sequence)];
	}

	// --------------------------------------------------------------------
	// 同名のレコードがあるかどうかによって AvoidSameName を設定する
	// --------------------------------------------------------------------
	public static void SetAvoidSameName<T>(IQueryable<T> records, T master) where T : class, IRcMaster
	{
		master.AvoidSameName = SelectMastersByName(records, master.Name).Count > 1;
	}

#if YUKALISTER
	// --------------------------------------------------------------------
	// サムネイルキャッシュデータベースのフルパス
	// --------------------------------------------------------------------
	public static String ThumbDatabasePath(YlSettings ylSettings)
	{
		return YukariDatabaseFullFolder(ylSettings) + FILE_NAME_THUMB_DATABASE;
	}
#endif

	// --------------------------------------------------------------------
	// データベースのプロパティーを更新（存在しない場合は新規作成）
	// --------------------------------------------------------------------
	public static void UpdateProperty(DbContext context, DbSet<TProperty> properties)
	{
		// 古いプロパティーを削除
		properties.RemoveRange(properties);
		context.SaveChanges();

		// 新しいプロパティーを追加
		TProperty property = new() { AppId = YlConstants.APP_ID, AppVer = YlConstants.APP_GENERATION + "," + YlConstants.APP_VER };
		properties.Add(property);
		context.SaveChanges();
	}

	// --------------------------------------------------------------------
	// データベース中に有効なプロパティー情報が存在するか
	// --------------------------------------------------------------------
	public static Boolean ValidPropertyExists(DbSet<TProperty> properties)
	{
		TProperty property = Property(properties);
		return property.AppId == YlConstants.APP_ID && property.AppVer.Contains(YlConstants.APP_GENERATION);
	}

	// --------------------------------------------------------------------
	// ゆかりすたーデータベースを保存するフォルダーのフルパス（末尾 '\\'）
	// 設定のバックアップのやりやすさを考慮し、Common.UserAppDataFolderPath() の配下ではなく
	// Common.UserAppDataFolderPath() と並列の階層とする
	// --------------------------------------------------------------------
	public static String YukaListerDatabaseFullFolder()
	{
#if YUKALISTER
		return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) + "\\"
				+ Common.FOLDER_NAME_SHINTA + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]) + YlConstants.FOLDER_NAME_DATABASE;
#endif
#if MOCHIKARA_PRODUCER
		return CommonWindows.SettingsFolder() + MpConstants.FOLDER_NAME_DATABASE;
#endif
#if MOCHIKARA_PRODUCER_DB
		return MpdCommon.SettingsFolder() + MpConstants.FOLDER_NAME_DATABASE;
#endif
	}

#if YUKALISTER
	// --------------------------------------------------------------------
	// ゆかり用データベースを保存するフォルダーのフルパス（末尾 '\\'）
	// --------------------------------------------------------------------
	public static String YukariDatabaseFullFolder(YlSettings ylSettings)
	{
		return Path.GetDirectoryName(ylSettings.YukariConfigPath()) + "\\" + YlConstants.FOLDER_NAME_LIST;
	}
#endif

	// ====================================================================
	// private 定数
	// ====================================================================

	// バックアップ世代数
	private const Int32 NUM_DB_BACKUP_GENERATIONS = 31;

	// データベースファイル名
	private const String FILE_NAME_LIST_DATABASE_IN_DISK = "List" + Common.FILE_EXT_SQLITE3;
	private const String FILE_NAME_REPORT_DATABASE = "Report" + Common.FILE_EXT_SQLITE3;
	private const String FILE_NAME_THUMB_DATABASE = "Thumb" + Common.FILE_EXT_SQLITE3;

	// ====================================================================
	// private 関数
	// ====================================================================

#if MOCHIKARA_PRODUCER_DB
	/// <summary>
	/// 環境設定で指定されたフォルダーから楽曲情報データベースをコピーしてくる
	/// </summary>
	private static void CopyMusicInfoDatabase(MusicInfoContextDefault musicInfoContextDefault)
	{
		String srcPath;
		if (MpModel.Instance.EnvModel.MpSettings.MusicInfoDatabaseAuto)
		{
			// ゆかりすたー 4 NEBULA Ver 7.32 時点で
			// C:\Users\ユーザー名\AppData\Local\Packages\22724SHINTA.YukaLister4NEBULA_7y6dzca6yqjvw\LocalCache\Roaming\SHINTA\YukaListerDatabase\NebulaMusicInfo.sqlite3
			String packagePath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(MpdCommon.SettingsFolder()))) ?? String.Empty;
			srcPath = packagePath + "\\" + MpConstants.FOLDER_NAME_YUKALISTER_PACKAGE + MpConstants.FOLDER_NAME_LOCAL_CACHE + MpConstants.FOLDER_NAME_ROAMING
				+ Common.FOLDER_NAME_SHINTA + "YukaLister" + YlConstants.FOLDER_NAME_DATABASE + YlConstants.FILE_NAME_MUSIC_INFO_DATABASE;
#if TEST
			Log.Information("楽曲情報データベースのコピー元パス：" + srcPath);
#endif
			if (!File.Exists(srcPath))
			{
				// 旧方式のパスも試す（環境によってはこちらになることもある模様）
				// 発行・非発行にかかわらず C:\Users\ユーザー名\AppData\Roaming\ 配下になる
				srcPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) + "\\"
					+ Common.FOLDER_NAME_SHINTA + "YukaLister" + YlConstants.FOLDER_NAME_DATABASE + YlConstants.FILE_NAME_MUSIC_INFO_DATABASE;
			}
		}
		else
		{
			srcPath = MpModel.Instance.EnvModel.MpSettings.MusicInfoDatabaseManualPath;
		}
		String destPath = musicInfoContextDefault.DatabasePath();
		try
		{
			File.Copy(srcPath, destPath, true);
			Log.Information("楽曲情報データベースをコピーしました。");
		}
		catch (Exception)
		{
			Log.Error("楽曲情報データベースをコピーできませんでした。");
		}
	}
#endif

	// --------------------------------------------------------------------
	// 紐付テーブルのレコードを作成
	// --------------------------------------------------------------------
	private static T CreateSequenceRecord<T>(String id, Int32 sequence, String linkId, Boolean isImport = false) where T : IRcSequence, new()
	{
		return new T()
		{
			// IRcBase
			Id = id,
			Import = isImport,
			Invalid = false,
			UpdateTime = YlConstants.INVALID_MJD,
			Dirty = true,

			// IRcSequence
			Sequence = sequence,
			LinkId = linkId,
		};
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（IRcBase）
	// より派生型の IsRcXXXUpdated() から呼び出される前提
	// プライマリーキーは比較しない
	// ＜返値＞ true: 更新された, false: 更新されていない, null: より派生型での判断に委ねる
	// --------------------------------------------------------------------
	private static Boolean? IsRcBaseUpdatedCore(IRcBase existRecord, IRcBase newRecord)
	{
		if (!existRecord.Import && newRecord.Import)
		{
			// 既存レコードがゆかりすたー登録で新規レコードがインポートの場合は、ゆかりすたー登録した既存レコードを優先する
			return false;
		}

		if (existRecord.Invalid)
		{
			if (newRecord.Import)
			{
				// 既存レコードが無効の場合は、インポートでは無効解除しない
				return false;
			}

			// 既存レコードが無効の場合は、無効解除されるまでは更新しない、無効解除されたら更新された
			return !newRecord.Invalid;
		}

		// 派生型の内容が更新されたかどうかで判断すべき
		return null;
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（IRcCategorizable）
	// より派生型の IsRcXXXUpdated() から呼び出される前提
	// ＜返値＞ true: 更新された, false: 更新されていない, null: より派生型での判断に委ねる
	// --------------------------------------------------------------------
	private static Boolean? IsRcCategorizableUpdatedCore(IRcCategorizable existRecord, IRcCategorizable newRecord)
	{
		Boolean? isRcMasterUpdated = IsRcMasterUpdatedCore(existRecord, newRecord);
		if (isRcMasterUpdated != null)
		{
			return isRcMasterUpdated.Value;
		}

		// IRcCategorizable の要素が更新されていれば更新されたことが確定
		if (existRecord.CategoryId != newRecord.CategoryId
				|| existRecord.ReleaseDate != newRecord.ReleaseDate)
		{
			return true;
		}

		// 派生型の内容が更新されたかどうかで判断すべき
		return null;
	}

	// --------------------------------------------------------------------
	// レコードの内容が更新されたか（IRcMaster）
	// より派生型の IsRcXXXUpdated() から呼び出される前提
	// ＜返値＞ true: 更新された, false: 更新されていない, null: より派生型での判断に委ねる
	// --------------------------------------------------------------------
	private static Boolean? IsRcMasterUpdatedCore(IRcMaster existRecord, IRcMaster newRecord)
	{
		Boolean? isRcBaseUpdated = IsRcBaseUpdatedCore(existRecord, newRecord);
		if (isRcBaseUpdated != null)
		{
			return isRcBaseUpdated.Value;
		}

		// IRcMaster の要素が更新されていれば更新されたことが確定
		if (existRecord.Name != newRecord.Name
				|| existRecord.Ruby != newRecord.Ruby
				|| existRecord.Keyword != newRecord.Keyword)
		{
			return true;
		}

		// 派生型の内容が更新されたかどうかで判断すべき
		return null;
	}
}
