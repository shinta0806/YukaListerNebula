// ============================================================================
// 
// ネビュラコア：検索データ作成担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.YukaListerCores
{
	internal class Sifolin : YlCore
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public Sifolin()
		{
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（検索データ作成）のメインルーチン
		// --------------------------------------------------------------------
		protected override Task CoreMainAsync()
		{
			while (true)
			{
				MainEvent.WaitOne();
				Int32 startTick = Environment.TickCount;

				try
				{
					YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
					if (YlModel.Instance.ProjModel.UndoneTargetFolderInfo() == null)
					{
						continue;
					}

					YlModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Sifolin] = YukaListerStatus.Running;
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " アクティブ化。");
					if (YlModel.Instance.EnvModel.YlSettings.ApplyMusicInfoIntelligently)
					{
						YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "楽曲情報データベースが不十分な場合の誤適用を軽減する設定。");
					}

					MusicInfoDatabaseToMemory();

					_prevFolderTaskDetail = FolderTaskDetail.Done;
					while (true)
					{
						TargetFolderInfo? targetFolderInfo;

						// 削除
						targetFolderInfo = YlModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.Remove);
						if (targetFolderInfo != null)
						{
							Remove(targetFolderInfo);
							continue;
						}

						// キャッシュ活用（全体の動作状況がエラーではない場合のみ）
						if (YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error)
						{
							targetFolderInfo = YlModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.CacheToDisk);
							if (targetFolderInfo != null)
							{
								CacheToDisk(targetFolderInfo);
								continue;
							}
						}

						// すべてのフォルダーのキャッシュ活用が終わったら Yurelin をアクティブ化する
						// 起動直後に前回のゆかり予約を解析することを想定している
						if (_prevFolderTaskDetail == FolderTaskDetail.CacheToDisk)
						{
							Debug.WriteLine("Sifolin.CoreMain() キャッシュ活用後の Yurelin アクティブ化");
							YlCommon.ActivateYurelinIfNeeded();
						}

						// サブフォルダー検索
						targetFolderInfo = YlModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.FindSubFolders);
						if (targetFolderInfo != null)
						{
							FindSubFolders(targetFolderInfo);
							continue;
						}

						// ファイル名追加
						targetFolderInfo = YlModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.AddFileNames);
						if (targetFolderInfo != null)
						{
							AddFileNames(targetFolderInfo);
							continue;
						}

						// ファイル情報追加
						targetFolderInfo = YlModel.Instance.ProjModel.FindTargetFolderInfo(FolderTaskDetail.AddInfos);
						if (targetFolderInfo != null)
						{
							AddInfos(targetFolderInfo);
							continue;
						}

						// メモリー DB → ディスク DB（全体の動作状況がエラーではない場合のみ）
						if (_needsMemoryDbToDiskDb && YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error)
						{
							MemoryToDisk();
							_needsMemoryDbToDiskDb = false;
							continue;
						}

						// メモリー DB → キャッシュ DB
						if (_needsMemoryDbToCacheDb)
						{
							MemoryToCache();
							_needsMemoryDbToCacheDb = false;
							continue;
						}

						// Kamlin アクティブ化
						YlCommon.ActivateKamlinIfNeeded();

						// Yurelin アクティブ化
						YlCommon.ActivateYurelinIfNeeded();

						// やることが無くなったのでループを抜けて待機へ向かう
						break;
					}
				}
				catch (OperationCanceledException)
				{
					return Task.CompletedTask;
				}
				catch (Exception ex)
				{
					YlModel.Instance.EnvModel.NebulaCoreErrors.Enqueue(GetType().Name + " ループ稼働時エラー：\n" + ex.Message);
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
				}

				// 念のため最後に表示を更新
				YlModel.Instance.EnvModel.YukaListerPartsStatus[(Int32)YukaListerPartsStatusIndex.Sifolin] = YukaListerStatus.Ready;
				YlModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = true;

				TimeSpan timeSpan = new(YlCommon.MiliToHNano(Environment.TickCount - startTick));
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " スリープ化：アクティブ時間：" + timeSpan.ToString(@"hh\:mm\:ss"));
			}
		}

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// メモリー DB → ディスク DB 更新フラグ
		private Boolean _needsMemoryDbToDiskDb;

		// メモリー DB → キャッシュ DB 更新フラグ
		private Boolean _needsMemoryDbToCacheDb;

		// 直前のフォルダータスク詳細
		private FolderTaskDetail _prevFolderTaskDetail;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルにファイル名を追加
		// --------------------------------------------------------------------
		private void AddFileNames(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			AddFileNamesCore(targetFolderInfo);

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.AddFileNames, FolderTaskDetail.AddInfos);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
			_prevFolderTaskDetail = FolderTaskDetail.AddFileNames;
		}

		// --------------------------------------------------------------------
		// 指定フォルダ内のファイルを検索してゆかり用データベースに追加
		// ユニーク ID、フルパス、フォルダーのみ記入する
		// ファイルは再帰検索しない
		// --------------------------------------------------------------------
		private void AddFileNamesCore(TargetFolderInfo targetFolderInfo)
		{
			// フォルダー除外設定を読み込む
			if (YlCommon.DetectFolderExcludeSettingsStatus(targetFolderInfo.TargetPath) == FolderExcludeSettingsStatus.True)
			{
				return;
			}

			// Uid
			using ListContextInMemory listContextInMemory = new();
			Int64 uid = listContextInMemory.Founds.Any() ? listContextInMemory.Founds.Max(x => x.Uid) + 1 : 1;

			// キャッシュが使われていない場合はディスク DB の Uid とも重複しないようにする（全体の動作状況がエラーではない場合のみ）
			if (YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error && !targetFolderInfo.IsCacheUsed)
			{
				using ListContextInDisk listContextInDisk = new();
				if (listContextInDisk.Founds.Any())
				{
					uid = Math.Max(uid, listContextInDisk.Founds.Max(x => x.Uid) + 1);
				}
			}

			// 検索
			String[] allPathes;
			try
			{
				allPathes = Directory.GetFiles(targetFolderInfo.TargetPath);
			}
			catch (Exception)
			{
				return;
			}

			// 追加準備
			List<TFound> addRecords = new();
			addRecords.Capacity = allPathes.Length;
			foreach (String path in allPathes)
			{
				if (!YlModel.Instance.EnvModel.YlSettings.TargetExts.Contains(Path.GetExtension(path).ToLower()))
				{
					continue;
				}

				TFound record = new();
				record.Uid = uid;
				record.Path = path;
				record.Folder = Path.GetDirectoryName(path) ?? String.Empty;
				record.ParentFolder = targetFolderInfo.ParentPath;

				// 楽曲名とファイルサイズが両方とも初期値だと、ゆかりが検索結果をまとめてしまうため、ダミーのファイルサイズを入れる
				// （文字列である楽曲名を入れると処理が遅くなるので処理が遅くなりにくい数字のファイルサイズをユニークにする）
				record.FileSize = -uid;

				addRecords.Add(record);
				uid++;
			}

			// メモリー DB に追加
			listContextInMemory.Founds.AddRange(addRecords);
			listContextInMemory.SaveChanges();
			_needsMemoryDbToDiskDb = true;
			_needsMemoryDbToCacheDb = true;

			// キャッシュが使われていない場合はディスク DB にも追加（全体の動作状況がエラーではない場合のみ）
			if (YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error && !targetFolderInfo.IsCacheUsed)
			{
				using ListContextInDisk listContextInDisk = new();
				listContextInDisk.Founds.AddRange(addRecords);
				listContextInDisk.SaveChanges();
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり用リストデータベースにファイル名を追加しました。" + targetFolderInfo.TargetPath);
			}
		}

		// --------------------------------------------------------------------
		// フォルダー設定で指定されているタグを TFound とゆかり用リストデータベースに付与する
		// --------------------------------------------------------------------
		private static void AddFolderTagsInfo(TargetFolderInfo targetFolderInfo, IQueryable<TFound> records, DbSet<TTag> tags, DbSet<TTagSequence> tagSequences)
		{
			try
			{
				String tagKey = YlCommon.WithoutDriveLetter(targetFolderInfo.TargetPath);
				if (!YlModel.Instance.EnvModel.TagSettings.FolderTags.ContainsKey(tagKey))
				{
					return;
				}

				// TTag にフォルダー設定のタグ情報と同名のタグがあるか？
				String tagValue = YlModel.Instance.EnvModel.TagSettings.FolderTags[tagKey];
				TTag? tagRecord = DbCommon.SelectMasterByName(tags, tagValue);
				if (tagRecord == null)
				{
					// 同名のタグが無いので、tagKey を Id とするタグがまだ存在しなければ作成
					String tagId = YlConstants.TEMP_ID_PREFIX + tagKey;
					tagRecord = DbCommon.SelectBaseById(tags, tagId);
					if (tagRecord == null)
					{
						tagRecord = new()
						{
							// IRcBase
							Id = tagId,
							Import = false,
							Invalid = false,
							UpdateTime = YlConstants.INVALID_MJD,
							Dirty = true,

							// IRcMaster
							Name = tagValue,
							Ruby = null,
							Keyword = null,
						};
						tags.Add(tagRecord);
					}
				}

				Dictionary<String, Boolean> addedIds = new();
				foreach (TFound record in records)
				{
					// TFound にタグ情報を追加
					// 楽曲情報データベースで付与されたものと同じ場合は重複連結となるが、ゆかりが検索するためのものなので問題ない
					record.TagName += "," + tagRecord.Name;
					if (!String.IsNullOrEmpty(tagRecord.Ruby))
					{
						record.TagRuby += "," + tagRecord.Ruby;
					}

					// ゆかり用リストデータベースの TTagSequence にタグ情報を追加
					// 1 つのフォルダー内に同じ曲が複数個存在する場合があるので、既に作業済みの曲はスキップ
					if (String.IsNullOrEmpty(record.SongId) || addedIds.ContainsKey(record.SongId))
					{
						continue;
					}

					// TTagSequence にフォルダー設定のタグ情報が無ければ保存
					List<TTag> songTags = DbCommon.SelectSequencedTagsBySongId(tagSequences, tags, record.SongId);
					if (songTags.FirstOrDefault(x => x.Name == tagRecord.Name) == null)
					{
						IQueryable<Int32> sequenceResults = tagSequences.Where(x => x.Id == record.SongId).Select(x => x.Sequence);
						Int32 seqMax = sequenceResults.Any() ? sequenceResults.Max() : -1;
						TTagSequence tagSequenceRecord = new()
						{
							// IDbBase
							Id = record.SongId,
							Import = false,
							Invalid = false,
							UpdateTime = YlConstants.INVALID_MJD,
							Dirty = true,

							// IDbSequence
							Sequence = seqMax + 1,
							LinkId = tagRecord.Id,
						};
						tagSequences.Add(tagSequenceRecord);
						addedIds[record.SongId] = true;
					}
				}
			}
			catch (Exception ex)
			{
				YlModel.Instance.EnvModel.NebulaCoreErrors.Enqueue("フォルダー設定タグ付与時エラー：\n" + ex.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルに属性を追加
		// --------------------------------------------------------------------
		private void AddInfos(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);
			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "属性確認中... " + targetFolderInfo.TargetPath);

			// 作業
			AddInfosCore(targetFolderInfo);
#if DEBUGz
			Thread.Sleep(500);
#endif

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.AddInfos, FolderTaskDetail.Done);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.DoneInMemory);
			_prevFolderTaskDetail = FolderTaskDetail.AddInfos;
		}

		// --------------------------------------------------------------------
		// 検出ファイルリストテーブルにファイル情報を追加
		// AddFileNames() で追加されない情報をすべて付与する
		// ファイルは再帰検索しない
		// --------------------------------------------------------------------
		private static void AddInfosCore(TargetFolderInfo targetFolderInfo)
		{
			// フォルダー設定を読み込む
			FolderSettingsInDisk folderSettingsInDisk = YlCommon.LoadFolderSettings(targetFolderInfo.TargetPath);
			FolderSettingsInMemory folderSettingsInMemory = YlCommon.CreateFolderSettingsInMemory(folderSettingsInDisk);

			using ListContextInMemory listContextInMemory = new();
			using TFoundSetter foundSetter = new(listContextInMemory);

			// 指定フォルダーの全レコード
			IQueryable<TFound> targetRecords = listContextInMemory.Founds.Where(x => x.Folder == targetFolderInfo.TargetPath);

			// 情報付与
			foreach (TFound record in targetRecords)
			{
				FileInfo fileInfo = new(record.Path);
				record.LastWriteTime = JulianDay.DateTimeToModifiedJulianDate(fileInfo.LastWriteTime);
				record.FileSize = fileInfo.Length;
				foundSetter.SetTFoundValues(record, folderSettingsInMemory);

				YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			}
			AddFolderTagsInfo(targetFolderInfo, targetRecords, listContextInMemory.Tags, listContextInMemory.TagSequences);

			// コミット
			listContextInMemory.SaveChanges();
		}

		// --------------------------------------------------------------------
		// キャッシュ DB からディスク DB へコピー
		// --------------------------------------------------------------------
		private void CacheToDisk(TargetFolderInfo targetFolderInfo)
		{
			Debug.Assert(targetFolderInfo.IsParent, "CacheToDisk() not parent");

			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			CacheToDiskCore(targetFolderInfo);
#if TEST
			YlModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "CacheToDisk() キャッシュ追加：" + targetFolderInfo.TargetPath);
#endif

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.CacheToDisk, FolderTaskDetail.FindSubFolders);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
			_prevFolderTaskDetail = FolderTaskDetail.CacheToDisk;
#if DEBUGz
			Thread.Sleep(60 * 1000);
#endif
		}

		// --------------------------------------------------------------------
		// キャッシュ DB からディスク DB へコピー
		// --------------------------------------------------------------------
		private static void CacheToDiskCore(TargetFolderInfo targetFolderInfo)
		{
			try
			{
				using CacheContext cacheContext = new(YlCommon.DriveLetter(targetFolderInfo.TargetPath));
				cacheContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				// QueryTrackingBehavior.NoTracking（または AsNoTracking()）時、結果の内容を変更して使いたい時は IQueryable<T> で受けてはならない（インスタンスの内容が変更できない）
				// List<T> 等に変換すれば結果のインスタンス内容を変更できる（変更しても SaveChanges() の対象にはならないと思う）
				List<TFound> cacheRecords = cacheContext.Founds.Where(x => x.ParentFolder == targetFolderInfo.TargetPath).ToList();

				if (!cacheRecords.Any())
				{
					// キャッシュが見つからない場合、ドライブレター以外の部分で合致するか再度検索
					// 誤検知しないようコロンも含めて検索するので、YlCommon.WithoutDriveLetter() は使用しない
					String withoutDriveLetterOne = targetFolderInfo.TargetPath[1..];
					cacheRecords = cacheContext.Founds.Where(x => x.ParentFolder.Contains(withoutDriveLetterOne)).ToList();
					if (!cacheRecords.Any())
					{
						YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
								+ "\nキャッシュはありませんでした。");
						return;
					}

					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
							+ "\nキャッシュのドライブレターを変換しています...");
					String drive = YlCommon.DriveLetter(targetFolderInfo.TargetPath);
					foreach (TFound cacheRecord in cacheRecords)
					{
						cacheRecord.Path = drive + YlCommon.WithoutDriveLetter(cacheRecord.Path);
						cacheRecord.Folder = drive + YlCommon.WithoutDriveLetter(cacheRecord.Folder);
						cacheRecord.ParentFolder = drive + YlCommon.WithoutDriveLetter(cacheRecord.ParentFolder);
					}
				}

				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
						+ "\nキャッシュをゆかり用リストデータベースに反映しています...");

				// キャッシュの Uid 初期化
				foreach (TFound cacheRecord in cacheRecords)
				{
					cacheRecord.Uid = 0;
				}

				using ListContextInDisk listContextInDisk = new();
				listContextInDisk.Founds.AddRange(cacheRecords);
				listContextInDisk.SaveChanges();
				targetFolderInfo.IsCacheUsed = true;
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
						+ "\nキャッシュをゆかり用リストデータベースに反映しました。");
			}
			catch (Exception excep)
			{
				// C ドライブ等、ルートに書き込み権限がなくキャッシュデータベースが作れていない場合も例外が発生する
				// C ドライブをゆかり検索対象フォルダーに追加する度にメッセージが表示されるのはナンセンスなので、ログのみとする
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "キャッシュ DB → ディスク DB 時エラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// リスト化対象フォルダーのサブフォルダーを列挙
		// --------------------------------------------------------------------
		private List<TargetFolderInfo> EnumSubFolders(TargetFolderInfo parentFolder)
		{
			YlModel.Instance.EnvModel.AppCancellationTokenSource.Token.ThrowIfCancellationRequested();
			List<TargetFolderInfo> folders = new();
			try
			{
				// SearchOption.AllDirectories 付きで Directory.GetDirectories を呼び出すと、
				// ごみ箱のようにアクセス権限の無いフォルダーの中も列挙しようとして例外が
				// 発生し中断してしまう。
				// 面倒だが 1 フォルダーずつ列挙する
				String[] subFolderPathes = Directory.GetDirectories(parentFolder.TargetPath, "*", SearchOption.TopDirectoryOnly);
				foreach (String subFolderPath in subFolderPathes)
				{
					if (YlCommon.IsIgnoreFolder(subFolderPath))
					{
						continue;
					}

					// サブフォルダー追加
					TargetFolderInfo subFolder = new(parentFolder.ParentPath, subFolderPath, parentFolder.Level + 1);
					subFolder.IsCacheUsed = parentFolder.IsCacheUsed;
					folders.Add(subFolder);

					// サブフォルダーのサブフォルダー
					List<TargetFolderInfo> subSubFolders = EnumSubFolders(subFolder);
					folders.AddRange(subSubFolders);

					// サブフォルダーの情報
					subFolder.HasChildren = subSubFolders.Any();
					subFolder.NumTotalFolders = 1 + subSubFolders.Count;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception)
			{
			}
			return folders;
		}

		// --------------------------------------------------------------------
		// サブフォルダーを検索して追加
		// --------------------------------------------------------------------
		private void FindSubFolders(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			FindSubFoldersCore(targetFolderInfo);

			// 動作状況設定
			targetFolderInfo.SetFolderTaskDetail(FolderTaskDetail.FindSubFolders, FolderTaskDetail.AddFileNames);
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Queued);
			_prevFolderTaskDetail = FolderTaskDetail.FindSubFolders;
		}

		// --------------------------------------------------------------------
		// サブフォルダーを検索して追加
		// --------------------------------------------------------------------
		private void FindSubFoldersCore(TargetFolderInfo targetFolderInfo)
		{
			// 子の検索と重複チェック
			List<TargetFolderInfo> subFolders = EnumSubFolders(targetFolderInfo);
			Boolean childAdded = YlModel.Instance.ProjModel.IsTargetFolderAdded(subFolders);
			if (childAdded)
			{
				// 追加済みの親を削除
				YlModel.Instance.ProjModel.SetFolderTaskDetailOfFolderToRemove(targetFolderInfo.ParentPath);
				YlModel.Instance.EnvModel.NebulaCoreErrors.Enqueue(targetFolderInfo.TargetPath
						+ "\nのサブフォルダーが既に追加されています。\nサブフォルダーを一旦削除してから追加しなおして下さい。");
				return;
			}

			// 子の追加
			YlModel.Instance.ProjModel.AddTargetSubFolders(targetFolderInfo, subFolders);

			// 親設定
			targetFolderInfo.HasChildren = subFolders.Any();
			targetFolderInfo.NumTotalFolders = 1 + subFolders.Count;

			// その他
			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.TargetPath
					+ "\n" + targetFolderInfo.NumTotalFolders + " 個のフォルダーをキューに追加しました。");
		}

		// --------------------------------------------------------------------
		// メモリー DB → キャッシュ DB
		// --------------------------------------------------------------------
		private static void MemoryToCache()
		{
			using ListContextInMemory listContextInMemory = new();
			listContextInMemory.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			IQueryable<String> parentFolders = listContextInMemory.Founds.GroupBy(x => x.ParentFolder).Select(x => x.Key);
			foreach (String parentFolder in parentFolders)
			{
				try
				{
					using CacheContext cacheContext = new(YlCommon.DriveLetter(parentFolder));
					List<TFound> records = listContextInMemory.Founds.Where(x => x.ParentFolder == parentFolder).ToList();
					cacheContext.UpdateCache(records);
				}
				catch (Exception excep)
				{
					// C ドライブ等、ルートに書き込み権限がなくキャッシュデータベースが作れていない場合も例外が発生する
					// C ドライブをゆかり検索対象フォルダーに追加する度にメッセージが表示されるのはナンセンスなので、ログのみとする
					YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "メモリー DB → キャッシュ DB 時エラー：\n" + excep.Message);
					YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			}
		}

		// --------------------------------------------------------------------
		// メモリー DB → ディスク DB
		// --------------------------------------------------------------------
		private static void MemoryToDisk()
		{
			using ListContextInMemory listContextInMemory = new();
			using SqliteConnection? sqliteConnectionInMemory = listContextInMemory.Database.GetDbConnection() as SqliteConnection;
			using ListContextInDisk listContextInDisk = new();
			using SqliteConnection? sqliteConnectionInDisk = listContextInDisk.Database.GetDbConnection() as SqliteConnection;
			if (sqliteConnectionInMemory != null && sqliteConnectionInDisk != null)
			{
				sqliteConnectionInMemory.BackupDatabase(sqliteConnectionInDisk);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "ゆかり用リストデータベースの作成が完了しました。");
				YlModel.Instance.ProjModel.SetAllFolderTaskStatusToDoneInDisk();
			}
		}

		// --------------------------------------------------------------------
		// 楽曲情報データベース → メモリー DB
		// --------------------------------------------------------------------
		private static void MusicInfoDatabaseToMemory()
		{
			using MusicInfoContextDefault musicInfoContext = new();
			musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			using ListContextInMemory listContextInMemory = new();

			// メモリー DB クリア
			listContextInMemory.People.RemoveRange(listContextInMemory.People);
			listContextInMemory.ArtistSequences.RemoveRange(listContextInMemory.ArtistSequences);
			listContextInMemory.ComposerSequences.RemoveRange(listContextInMemory.ComposerSequences);
			listContextInMemory.TieUpGroups.RemoveRange(listContextInMemory.TieUpGroups);
			listContextInMemory.TieUpGroupSequences.RemoveRange(listContextInMemory.TieUpGroupSequences);
			listContextInMemory.Tags.RemoveRange(listContextInMemory.Tags);
			listContextInMemory.TagSequences.RemoveRange(listContextInMemory.TagSequences);
			listContextInMemory.SaveChanges();

			// コピー
			// peopleInMemory.AddRange(peopleInMusicInfo) のように DbSet 全体を追加すると、アプリ終了時にタスクが終了しないため、Where を挟む
			listContextInMemory.People.AddRange(musicInfoContext.People.Where(x => true));
			listContextInMemory.ArtistSequences.AddRange(musicInfoContext.ArtistSequences.Where(x => true));
			listContextInMemory.ComposerSequences.AddRange(musicInfoContext.ComposerSequences.Where(x => true));
			listContextInMemory.TieUpGroups.AddRange(musicInfoContext.TieUpGroups.Where(x => true));
			listContextInMemory.TieUpGroupSequences.AddRange(musicInfoContext.TieUpGroupSequences.Where(x => true));
			listContextInMemory.Tags.AddRange(musicInfoContext.Tags.Where(x => true));
			listContextInMemory.TagSequences.AddRange(musicInfoContext.TagSequences.Where(x => true));
			listContextInMemory.SaveChanges();
		}

		// --------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------
		private void Remove(TargetFolderInfo targetFolderInfo)
		{
			// 動作状況設定
			SetFolderTaskStatus(targetFolderInfo, FolderTaskStatus.Running);

			// 作業
			RemoveCore(targetFolderInfo);

			_prevFolderTaskDetail = FolderTaskDetail.Remove;
			// targetFolderInfo の動作状況設定は削除済みのため不要
		}

		// --------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------
		private static void RemoveCore(TargetFolderInfo targetFolderInfo)
		{
			if (!targetFolderInfo.IsParent)
			{
				// 親が一括削除するので、親でない場合は削除しない
				return;
			}

#if DEBUGz
			Thread.Sleep(3000);
#endif

			// まずディスク DB から削除（全体の動作状況がエラーではない場合のみ）
			if (YlModel.Instance.EnvModel.YukaListerWholeStatus != YukaListerStatus.Error)
			{
				using ListContextInDisk listContextInDisk = new();
				listContextInDisk.Founds.RemoveRange(listContextInDisk.Founds.Where(x => x.ParentFolder == targetFolderInfo.ParentPath));
				listContextInDisk.SaveChanges();
			}

			// メモリ DB から削除
			using ListContextInMemory listContextInMemory = new();
			listContextInMemory.Founds.RemoveRange(listContextInMemory.Founds.Where(x => x.ParentFolder == targetFolderInfo.ParentPath));
			listContextInMemory.SaveChanges();

			// TargetFolderInfo 削除
			YlModel.Instance.ProjModel.RemoveTargetFolders(targetFolderInfo.ParentPath);

			// その他
			YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, targetFolderInfo.ParentPath
					+ "\nとその配下のフォルダーをゆかり検索対象から削除しました。");
		}

		// --------------------------------------------------------------------
		// フォルダーの動作状況を設定
		// --------------------------------------------------------------------
		private static void SetFolderTaskStatus(TargetFolderInfo targetFolderInfo, FolderTaskStatus folderTaskStatus)
		{
			targetFolderInfo.FolderTaskStatus = folderTaskStatus;
			if (targetFolderInfo.Visible)
			{
				YlModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = true;
			}
		}
	}
}
