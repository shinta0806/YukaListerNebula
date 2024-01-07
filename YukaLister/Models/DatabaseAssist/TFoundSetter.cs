// ============================================================================
// 
// TFound の項目を埋める
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

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseAssist
{
	internal class TFoundSetter : IDisposable
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public TFoundSetter(ListContextInMemory listContextInMemory)
		{
			Debug.Assert(listContextInMemory.ChangeTracker.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll, "TFoundSetter() bad QueryTrackingBehavior");
			_listContextInMemory = listContextInMemory;

			// MusicInfoContext は検索専用なので NoTracking にする
			_musicInfoContext = new MusicInfoContextDefault();
			_musicInfoContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

			_categoryNames = DbCommon.SelectCategoryNames(_musicInfoContext.Categories);

			// スマートトラック判定用単語
			_offVocalWords = YlConstants.SMART_TRACK_SEPARATOR + String.Join(YlConstants.SMART_TRACK_SEPARATOR, YlModel.Instance.EnvModel.YlSettings.OffVocalWords) + YlConstants.SMART_TRACK_SEPARATOR;
			_bothVocalWords = YlConstants.SMART_TRACK_SEPARATOR + String.Join(YlConstants.SMART_TRACK_SEPARATOR, YlModel.Instance.EnvModel.YlSettings.BothVocalWords) + YlConstants.SMART_TRACK_SEPARATOR;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 楽曲マスターテーブル
		public DbSet<TSong> Songs
		{
			get => _musicInfoContext.Songs;
		}

		// タイアップマスターテーブル
		public DbSet<TTieUp> TieUps
		{
			get => _musicInfoContext.TieUps;
		}

		// ====================================================================
		// public 関数
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
		// dicByFile に合致する楽曲群を、楽曲情報データベースから検索
		// 楽曲名、タイアップ名、年齢制限、カテゴリー、歌手名で絞り込むが、複数候補となることがあり得る
		// --------------------------------------------------------------------
		public List<TSong> FindSongsByMusicInfoDatabase(Dictionary<String, String?> dicByFile)
		{
#if DEBUGz
			if (dicByFile[YlConstants.RULE_VAR_TITLE] == "バイバイ")
			{
			}
#endif
			// 楽曲名で検索
			List<TSong> songs = DbCommon.SelectMastersByName(_musicInfoContext.Songs, dicByFile[YlConstants.RULE_VAR_TITLE]);
			if (YlModel.Instance.EnvModel.YlSettings.ApplyMusicInfoIntelligently)
			{
				songs = RefineSongIntelligently(songs, dicByFile);
			}

			// タイアップで絞り込み
			if (songs.Count > 1)
			{
				Dictionary<TSong, TTieUp> songsAndTieUps = SongsAndTieUps(songs);

				// タイアップ名で絞り込み
				if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_PROGRAM] != null)
				{
					List<TSong> songsWithTieUpName = new();
					foreach (KeyValuePair<TSong, TTieUp> kvp in songsAndTieUps)
					{
						if (kvp.Value.Name == dicByFile[YlConstants.RULE_VAR_PROGRAM])
						{
							songsWithTieUpName.Add(kvp.Key);
						}
					}
					if (songsWithTieUpName.Any())
					{
						songs = songsWithTieUpName;
					}
				}

				// タイアップのカテゴリーで絞り込み
				if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_CATEGORY] != null)
				{
					List<TSong> songsWithTieUpCategory = new();
					foreach (KeyValuePair<TSong, TTieUp> kvp in songsAndTieUps)
					{
						TCategory? category = DbCommon.SelectBaseById(_musicInfoContext.Categories, kvp.Value.CategoryId);
						if (category != null && category.Name == dicByFile[YlConstants.RULE_VAR_CATEGORY])
						{
							songsWithTieUpCategory.Add(kvp.Key);
						}
					}
					if (songsWithTieUpCategory.Any())
					{
						songs = songsWithTieUpCategory;
					}
				}
			}

			// 楽曲のカテゴリーで絞り込み
			// タイアップの年齢制限より先に絞り込む（一般アニメと VOCALOID の 2 つがある場合、年齢制限を先にするとタイアップの付いていない VOCALOID が絞り込みから外れてしまう）
			if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_CATEGORY] != null)
			{
				List<TSong> songsWithCategory = new();
				foreach (TSong song in songs)
				{
					TCategory? category = DbCommon.SelectBaseById(_musicInfoContext.Categories, song.CategoryId);
					if (category != null && category.Name == dicByFile[YlConstants.RULE_VAR_CATEGORY])
					{
						songsWithCategory.Add(song);
					}
				}
				if (songsWithCategory.Any())
				{
					songs = songsWithCategory;
				}
			}

			// 歌手名で絞り込み
			if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_ARTIST] != null)
			{
				List<TSong> songsWithArtist = new();
				foreach (TSong song in songs)
				{
					(String? artistNames, _) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.ArtistSequences, _musicInfoContext.People, song.Id).ToList<IRcMaster>());
					if (!String.IsNullOrEmpty(artistNames) && artistNames == dicByFile[YlConstants.RULE_VAR_ARTIST])
					{
						songsWithArtist.Add(song);
					}
				}
				if (songsWithArtist.Any())
				{
					songs = songsWithArtist;
				}
			}

			// タイアップの年齢制限で絞り込み
			if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_AGE_LIMIT] != null)
			{
				Int32 dicAgeLimt = Common.StringToInt32(dicByFile[YlConstants.RULE_VAR_AGE_LIMIT]);
				List<TSong> songsWithAgeLimit = new();
				Dictionary<TSong, TTieUp> songsAndTieUps = SongsAndTieUps(songs);
				foreach (KeyValuePair<TSong, TTieUp> kvp in songsAndTieUps)
				{
					if (0 <= kvp.Value.AgeLimit && kvp.Value.AgeLimit < YlConstants.AGE_LIMIT_CERO_Z && 0 <= dicAgeLimt && dicAgeLimt < YlConstants.AGE_LIMIT_CERO_Z
							|| kvp.Value.AgeLimit == YlConstants.AGE_LIMIT_CERO_Z && dicAgeLimt == YlConstants.AGE_LIMIT_CERO_Z)
					{
						songsWithAgeLimit.Add(kvp.Key);
					}
				}
				if (songsWithAgeLimit.Any())
				{
					songs = songsWithAgeLimit;
				}
			}

			// 作曲者名で絞り込み
			if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_COMMENT] != null)
			{
				List<TSong> songsWithComposer = new();
				foreach (TSong song in songs)
				{
					(String? composerNames, _) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.ComposerSequences, _musicInfoContext.People, song.Id).ToList<IRcMaster>());
					if (!String.IsNullOrEmpty(composerNames) && dicByFile[YlConstants.RULE_VAR_COMMENT]!.Contains(composerNames))
					{
						songsWithComposer.Add(song);
					}
				}
				if (songsWithComposer.Any())
				{
					songs = songsWithComposer;
				}
			}

			return songs;
		}

		// --------------------------------------------------------------------
		// ファイル名とファイル命名規則・フォルダー固定値がマッチするか確認し、マッチしたマップを返す（ルビは検索用に正規化）
		// ＜引数＞ fileNameBody: 拡張子無し
		// --------------------------------------------------------------------
		public Dictionary<String, String?> MatchFileNameRulesAndFolderRuleForSearch(String fileNameBody, FolderSettingsInMemory folderSettingsInMemory)
		{
			// ファイル名・フォルダー固定値と合致する命名規則を探す
			Dictionary<String, String?> dicByFile = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(fileNameBody, folderSettingsInMemory);
			dicByFile[YlConstants.RULE_VAR_PROGRAM] = ProgramOrigin(dicByFile[YlConstants.RULE_VAR_PROGRAM]);
			dicByFile[YlConstants.RULE_VAR_TITLE] = SongOrigin(dicByFile[YlConstants.RULE_VAR_TITLE]);
			if (dicByFile[YlConstants.RULE_VAR_CATEGORY] != null)
			{
				if (!_categoryNames.Contains(dicByFile[YlConstants.RULE_VAR_CATEGORY]!))
				{
					dicByFile[YlConstants.RULE_VAR_CATEGORY] = null;
				}
			}
			return dicByFile;
		}

		// --------------------------------------------------------------------
		// 別名から元のタイアップ名を取得
		// --------------------------------------------------------------------
		public virtual String? ProgramOrigin(String? alias)
		{
			if (String.IsNullOrEmpty(alias))
			{
				return null;
			}

			// タイアップマスターテーブルに登録済みの名前の場合は、別名解決しない
			if (DbCommon.SelectMasterByName(_musicInfoContext.TieUps, alias) != null)
			{
				return alias;
			}

			TTieUpAlias? tieUpAlias = DbCommon.SelectAliasByAlias(_musicInfoContext.TieUpAliases, alias);
			if (tieUpAlias != null)
			{
				TTieUp? tieUp = DbCommon.SelectBaseById(_musicInfoContext.TieUps, tieUpAlias.OriginalId);
				if (tieUp != null)
				{
					// 元のタイアップ名
					return tieUp.Name;
				}
			}

			// 別名が見つからない場合はそのまま返す
			return alias;
		}

		// --------------------------------------------------------------------
		// 検出ファイルレコードの値を、フォルダー設定や楽曲情報データベースから検索して設定する
		// record.Path は事前に設定されている必要がある
		// --------------------------------------------------------------------
		public void SetTFoundValues(TFound record, FolderSettingsInMemory folderSettingsInMemory)
		{
			Dictionary<String, String?> dicByFile = MatchFileNameRulesAndFolderRuleForSearch(Path.GetFileNameWithoutExtension(record.Path), folderSettingsInMemory);

			// 楽曲情報データベースを適用
			SetTFoundValuesByMusicInfoDatabase(record, dicByFile);

			// 楽曲情報データベースに無かった項目をファイル名・フォルダー固定値から取得
			record.Category ??= dicByFile[YlConstants.RULE_VAR_CATEGORY];
			record.TieUpName ??= dicByFile[YlConstants.RULE_VAR_PROGRAM];
			record.TieUpAgeLimit = record.TieUpAgeLimit == 0 ? Common.StringToInt32(dicByFile[YlConstants.RULE_VAR_AGE_LIMIT]) : record.TieUpAgeLimit;
			record.SongOpEd ??= dicByFile[YlConstants.RULE_VAR_OP_ED];
			if (record.SongName == null)
			{
				record.SongName = dicByFile[YlConstants.RULE_VAR_TITLE] ?? Path.GetFileNameWithoutExtension(record.Path);
			}

			// SongId が無い場合は楽曲名を採用（フォルダー設定の歌手名やタグを紐付できるように）
			if (String.IsNullOrEmpty(record.SongId))
			{
				record.SongId = YlConstants.TEMP_ID_PREFIX + record.SongName;
			}

			SetTFoundArtistByDic(record, dicByFile);
			record.SongRuby ??= dicByFile[YlConstants.RULE_VAR_TITLE_RUBY];
			record.Worker ??= dicByFile[YlConstants.RULE_VAR_WORKER];
			record.Track ??= dicByFile[YlConstants.RULE_VAR_TRACK];
			record.SmartTrackOnVocal = !record.SmartTrackOnVocal ? dicByFile[YlConstants.RULE_VAR_ON_VOCAL] != null : record.SmartTrackOnVocal;
			record.SmartTrackOffVocal = !record.SmartTrackOffVocal ? dicByFile[YlConstants.RULE_VAR_OFF_VOCAL] != null : record.SmartTrackOffVocal;

			// コメントについては、楽曲情報データベースのコメントがある場合でも dicByFile のコメントも付与する
			// 楽曲情報データベースのコメントは Web リストに出力しないので、dicByFile のコメントを前に付与する
			if (dicByFile[YlConstants.RULE_VAR_COMMENT] != null)
			{
				record.Comment = dicByFile[YlConstants.RULE_VAR_COMMENT] + record.Comment;
			}

			// トラック情報からスマートトラック解析
			(Boolean hasOn, Boolean hasOff) = AnalyzeSmartTrack(record.Track);
			record.SmartTrackOnVocal |= hasOn;
			record.SmartTrackOffVocal |= hasOff;

			// ルビが無い場合は漢字を採用
			if (String.IsNullOrEmpty(record.TieUpRuby))
			{
				record.TieUpRuby = record.TieUpName;
			}
			if (String.IsNullOrEmpty(record.SongRuby))
			{
				record.SongRuby = record.SongName;
			}

			// 頭文字
			if (!String.IsNullOrEmpty(record.TieUpRuby))
			{
				record.Head = YlCommon.Head(record.TieUpRuby);
			}
			else
			{
				record.Head = YlCommon.Head(record.SongRuby);
			}

			// 番組名が無い場合は頭文字を採用（ボカロ曲等のリスト化用）
			if (String.IsNullOrEmpty(record.TieUpName))
			{
				record.TieUpName = record.Head;
			}
		}

		// --------------------------------------------------------------------
		// 別名から元の楽曲名を取得
		// --------------------------------------------------------------------
		public virtual String? SongOrigin(String? alias)
		{
			if (String.IsNullOrEmpty(alias))
			{
				return null;
			}

			// 楽曲マスターテーブルに登録済みの名前の場合は、別名解決しない
			if (DbCommon.SelectMasterByName(_musicInfoContext.Songs, alias) != null)
			{
				return alias;
			}

			TSongAlias? songAlias = DbCommon.SelectAliasByAlias(_musicInfoContext.SongAliases, alias);
			if (songAlias != null)
			{
				TSong? song = DbCommon.SelectBaseById(_musicInfoContext.Songs, songAlias.OriginalId);
				if (song != null)
				{
					// 元の楽曲名
					return song.Name;
				}
			}

			// 別名が見つからない場合はそのまま返す
			return alias;
		}

		// ====================================================================
		// protected 関数
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
		// private 変数
		// ====================================================================

		// 楽曲情報データベースのコンテキスト
		private readonly MusicInfoContextDefault _musicInfoContext;

		// リストデータベース（作業用：インメモリ）のコンテキスト
		private readonly ListContextInMemory _listContextInMemory;

		// カテゴリー名正規化用
		private readonly List<String> _categoryNames;

		// オフボーカルと見なす単語
		private String _offVocalWords;

		// オンボーカル・オフボーカル両方と見なす単語
		private String _bothVocalWords;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 環境設定を考慮した検索用フリガナ
		// --------------------------------------------------------------------
		private String? AdditionalRubyForSearch(IRcMaster master)
		{
			if (!YlModel.Instance.EnvModel.YlSettings.OutputAdditionalYukariRuby)
			{
				// 追加しない設定なら RubyForSearch をそのまま返す
				return master.RubyForSearch;
			}

			// 元のフリガナが空なら RubyForSearch をそのまま返す
			if (String.IsNullOrEmpty(master.Ruby))
			{
				return master.RubyForSearch;
			}

			// 元のフリガナに長音がないなら RubyForSearch をそのまま返す
			if (!master.Ruby.Contains('ー', StringComparison.Ordinal))
			{
				return master.RubyForSearch;
			}

			// RubyForSearch（長音なし）に長音ありバージョンを追加して返す
			return master.RubyForSearch + ',' + YlCommon.NormalizeDbRubyForSearch(master.Ruby, false).normalizedRuby;
		}

		// --------------------------------------------------------------------
		// トラック情報からオンボーカル・オフボーカルがあるか解析する
		// --------------------------------------------------------------------
		private (Boolean hasOn, Boolean hasOff) AnalyzeSmartTrack(String? trackString)
		{
			Boolean hasOn = false;
			Boolean hasOff = false;

			if (!String.IsNullOrEmpty(trackString))
			{
				String[] tracks = trackString.Split(new Char[] { '-', '_', '+', ',', '.', ' ', (Char)0x2010 }, StringSplitOptions.RemoveEmptyEntries);
				for (Int32 i = 0; i < tracks.Length; i++)
				{
					if (_bothVocalWords.Contains("|" + tracks[i] + "|", StringComparison.OrdinalIgnoreCase))
					{
						// オンオフ両方を意味する単語の場合
						hasOn = true;
						hasOff = true;
					}
					else
					{
						if (_offVocalWords.Contains("|" + tracks[i] + "|", StringComparison.OrdinalIgnoreCase))
						{
							// オフを意味する単語の場合
							hasOff = true;
						}
						else
						{
							// それ以外の単語はオン扱い
							hasOn = true;
						}
					}
				}
			}
			return (hasOn, hasOff);
		}

		// --------------------------------------------------------------------
		// 複数の IRcMaster の名前とルビをカンマで結合
		// --------------------------------------------------------------------
		private static (String? names, String? rubies) ConcatMasterNamesAndRubies(List<IRcMaster> masters)
		{
			String names = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], masters.Select(x => x.Name));
			String rubies = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], masters.Select(x => x.RubyForSearch));
			return (String.IsNullOrEmpty(names) ? null : names, String.IsNullOrEmpty(rubies) ? null : rubies);
		}

		// --------------------------------------------------------------------
		// dicByFile に合致するタイアップを、楽曲情報データベースから検索
		// --------------------------------------------------------------------
		private TTieUp? FindTieUpByMusicInfoDatabase(Dictionary<String, String?> dicByFile)
		{
			if (dicByFile[YlConstants.RULE_VAR_PROGRAM] == null)
			{
				return null;
			}

			List<TTieUp> tieUps = DbCommon.SelectMastersByName(_musicInfoContext.TieUps, dicByFile[YlConstants.RULE_VAR_PROGRAM]);

			// カテゴリーで絞り込み
			if (tieUps.Count > 1 && dicByFile[YlConstants.RULE_VAR_CATEGORY] != null)
			{
				List<TTieUp> tieUpsWithCategory = new();
				foreach (TTieUp tieUp in tieUps)
				{
					TCategory? category = DbCommon.SelectBaseById(_musicInfoContext.Categories, tieUp.CategoryId);
					if (category != null && category.Name == dicByFile[YlConstants.RULE_VAR_CATEGORY])
					{
						tieUpsWithCategory.Add(tieUp);
					}
				}
				if (tieUpsWithCategory.Any())
				{
					tieUps = tieUpsWithCategory;
				}
			}

			// 年齢制限で絞り込み
			if (tieUps.Count > 1 && dicByFile[YlConstants.RULE_VAR_AGE_LIMIT] != null)
			{
				Int32 dicAgeLimt = Common.StringToInt32(dicByFile[YlConstants.RULE_VAR_AGE_LIMIT]);
				List<TTieUp> tieUpsWithAgeLimit = new();
				foreach (TTieUp tieUp in tieUps)
				{
					if (0 <= tieUp.AgeLimit && tieUp.AgeLimit < YlConstants.AGE_LIMIT_CERO_Z && 0 <= dicAgeLimt && dicAgeLimt < YlConstants.AGE_LIMIT_CERO_Z
							|| tieUp.AgeLimit == YlConstants.AGE_LIMIT_CERO_Z && dicAgeLimt == YlConstants.AGE_LIMIT_CERO_Z)
					{
						tieUpsWithAgeLimit.Add(tieUp);
					}
				}
				if (tieUpsWithAgeLimit.Any())
				{
					tieUps = tieUpsWithAgeLimit;
				}
			}

			return tieUps.FirstOrDefault();
		}

		// --------------------------------------------------------------------
		// 検索ワードと検索ワードフリガナをコメント用に整形
		// --------------------------------------------------------------------
		private static String? KeywordToComment(IRcMaster master)
		{
			if (!YlModel.Instance.EnvModel.YlSettings.OutputAdditionalYukariAssist)
			{
				return null;
			}

			String? comment = null;
			if (!String.IsNullOrEmpty(master.Keyword))
			{
				comment += master.Keyword + YlConstants.VAR_VALUE_DELIMITER;
			}
			if (!String.IsNullOrEmpty(master.KeywordRubyForSearch))
			{
				comment += master.KeywordRubyForSearch + YlConstants.VAR_VALUE_DELIMITER;
			}
			if (!String.IsNullOrEmpty(comment))
			{
				comment = YlConstants.WEB_LIST_IGNORE_COMMENT_DELIMITER + comment;
			}
			return comment;
		}

		// --------------------------------------------------------------------
		// 適合割合が高いかどうか
		// --------------------------------------------------------------------
		private Boolean MatchIntelligently(String dicByFileName, String musicInfoName)
		{
			// 適合割合チェック
			Int32 numIncludes = 0;
			foreach (Char chr in dicByFileName)
			{
				if (musicInfoName.Contains(chr))
				{
					numIncludes++;
				}
			}

			return (Double)numIncludes / dicByFileName.Length >= YlModel.Instance.EnvModel.YlSettings.IntelligentThreshold / 10.0;
		}

		// --------------------------------------------------------------------
		// 楽曲名だけではなく、タイアップ名と歌手名もある程度合致したもののみを候補とする
		// --------------------------------------------------------------------
		private List<TSong> RefineSongIntelligently(List<TSong> songs, Dictionary<String, String?> dicByFile)
		{
			List<TSong> refineSongs = new();
			foreach (TSong song in songs)
			{
				// タイアップ名の適合割合をチェック
				if (dicByFile[YlConstants.RULE_VAR_PROGRAM] != null)
				{
					TTieUp? tieUpOfSong = DbCommon.SelectBaseById(_musicInfoContext.TieUps, song.TieUpId);
					if (tieUpOfSong == null || tieUpOfSong.Name == null)
					{
						// タイアップを持たない楽曲は除外
						continue;
					}
					if (!MatchIntelligently(dicByFile[YlConstants.RULE_VAR_PROGRAM]!, tieUpOfSong.Name))
					{
						// タイアップ名の適合割合が低い楽曲は除外
						continue;
					}
				}

				// 歌手名の適合割合をチェック
				if (dicByFile[YlConstants.RULE_VAR_ARTIST] != null)
				{
					(String? artistNames, _) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.ArtistSequences, _musicInfoContext.People, song.Id)
							.ToList<IRcMaster>());
					if (String.IsNullOrEmpty(artistNames))
					{
						// 歌手を持たない楽曲は除外
						continue;
					}
					if (!MatchIntelligently(dicByFile[YlConstants.RULE_VAR_ARTIST]!, artistNames))
					{
						// 歌手名の適合割合が低い楽曲は除外
						continue;
					}
				}

				// 候補とする
				refineSongs.Add(song);
			}
			return refineSongs;
		}

		// --------------------------------------------------------------------
		// （dicByFile から取得した）人物情報をゆかり用リストデータベースに登録
		// --------------------------------------------------------------------
		private void RegisterPerson<T>(DbSet<T> listSequences, TFound found, TPerson person) where T : class, IRcSequence, new()
		{
			if (String.IsNullOrEmpty(found.SongId))
			{
				return;
			}

			// 人物は人物テーブルに登録済みか？
			TPerson? registeredPerson;
			if (!String.IsNullOrEmpty(person.Id))
			{
				// 登録済み
				registeredPerson = person;
			}
			else
			{
				// 人物テーブルにフォルダー設定の人物情報と同名の人物があるか？
				registeredPerson = DbCommon.SelectMasterByName(_listContextInMemory.People, person.Name);
				if (registeredPerson == null)
				{
					// ID で再検索
					String personId = YlConstants.TEMP_ID_PREFIX + person.Name;
					registeredPerson = DbCommon.SelectBaseById(_listContextInMemory.People, personId);

					if (registeredPerson == null)
					{
						// 同名も同 ID もないので作成
						registeredPerson = new()
						{
							// IRcBase
							Id = personId,
							Import = false,
							Invalid = false,
							UpdateTime = YlConstants.INVALID_MJD,
							Dirty = true,

							// IRcMaster
							Name = person.Name,
							Ruby = null,
							Keyword = null,
						};
						_listContextInMemory.People.Add(registeredPerson);
					}
				}
			}

			// TXxxSequence にフォルダー設定のタグ情報が無ければ保存
			T? registeredSequence = listSequences.FirstOrDefault(x => x.Id == found.SongId && x.LinkId == registeredPerson.Id);
			if (registeredSequence == null)
			{
				IQueryable<Int32> sequenceResults = listSequences.Where(x => x.Id == found.SongId).Select(x => x.Sequence);
				Int32 seqMax = sequenceResults.Any() ? sequenceResults.Max() : -1;
				registeredSequence = new()
				{
					// IDbBase
					Id = found.SongId,
					Import = false,
					Invalid = false,
					UpdateTime = YlConstants.INVALID_MJD,
					Dirty = true,

					// IDbSequence
					Sequence = seqMax + 1,
					LinkId = registeredPerson.Id,
				};
				//Debug.WriteLine("RegisterPerson() id: " + registeredSequence.Id + ", seq: " + registeredSequence.Sequence);
				listSequences.Add(registeredSequence);
			}

			// 直後から検索できるように直ちにコミット
			_listContextInMemory.SaveChanges();
		}

		// --------------------------------------------------------------------
		// 歌手情報を dicByFile から設定
		// --------------------------------------------------------------------
		private void SetTFoundArtistByDic(TFound record, Dictionary<String, String?> dicByFile)
		{
			String? dicArtist = dicByFile[YlConstants.RULE_VAR_ARTIST];

			if (record.ArtistName == null && dicArtist != null)
			{
				// ファイル名から歌手名を取得できている場合は、楽曲情報データベースからフリガナを探す
				List<TPerson> artists;
				artists = DbCommon.SelectMastersByName(_musicInfoContext.People, dicArtist);
				if (artists.Any())
				{
					// 歌手名が楽曲情報データベースに登録されていた場合はその情報を使う
					record.ArtistName = artists[0].Name;
					record.ArtistRuby = artists[0].Ruby;
					RegisterPerson(_listContextInMemory.ArtistSequences, record, artists[0]);
				}
				else
				{
					// 歌手名そのままでは楽曲情報データベースに登録されていない場合
					if (dicArtist.Contains(YlConstants.VAR_VALUE_DELIMITER))
					{
						// 区切り文字で区切られた複数の歌手名が記載されている場合は分解して解析する
						String[] artistNames = dicArtist.Split(YlConstants.VAR_VALUE_DELIMITER[0]);
						foreach (String artistName in artistNames)
						{
							TPerson? artistsTmp = DbCommon.SelectMasterByName(_listContextInMemory.People, artistName);
							if (artistsTmp != null)
							{
								// 区切られた歌手名が楽曲情報データベースに存在する
								artists.Add(artistsTmp);
							}
							else
							{
								// 区切られた歌手名が楽曲情報データベースに存在しないので仮の人物を作成
								TPerson artistTmp = new()
								{
									Name = artistName,
								};
								artists.Add(artistTmp);
							}
						}
						(record.ArtistName, record.ArtistRuby) = ConcatMasterNamesAndRubies(artists.ToList<IRcMaster>());
						for (Int32 i = 0; i < artists.Count; i++)
						{
							RegisterPerson(_listContextInMemory.ArtistSequences, record, artists[i]);
						}
					}
					else
					{
						// 楽曲情報データベースに登録されていないので漢字のみ格納
						record.ArtistName = dicArtist;
						TPerson artistTmp = new()
						{
							Name = dicArtist,
						};
						RegisterPerson(_listContextInMemory.ArtistSequences, record, artistTmp);
					}
				}
			}
		}

		// --------------------------------------------------------------------
		// 検出ファイルレコードの値を、楽曲情報データベースから検索して設定する
		// ファイル名を元に検索し、結果が複数ある場合は他の情報も照らし合わせて最も近い物を設定する
		// --------------------------------------------------------------------
		private void SetTFoundValuesByMusicInfoDatabase(TFound record, Dictionary<String, String?> dicByFile)
		{
			if (dicByFile[YlConstants.RULE_VAR_TITLE] == null)
			{
				return;
			}

			// 楽曲名、タイアップ名等で絞り込んだ結果を取得
			List<TSong> songs = FindSongsByMusicInfoDatabase(dicByFile);

			TSong? selectedSong = null;
			TTieUp? selectedTieUp = null;
			if (songs.Any())
			{
				// これ以上は絞り込めないので、先頭の楽曲を選択する
				selectedSong = songs[0];

				// 楽曲情報データベース内に曲情報がある場合は、曲に紐付くタイアップを得る
				selectedTieUp = DbCommon.SelectBaseById(_musicInfoContext.TieUps, selectedSong.TieUpId);
			}
			if (selectedTieUp == null)
			{
				// 曲に紐付くタイアップが無い場合は、ファイル名からタイアップを取得
				selectedTieUp = FindTieUpByMusicInfoDatabase(dicByFile);
			}
			if (selectedSong == null && selectedTieUp == null)
			{
				// 曲情報もタイアップ情報も無い場合は諦める
				return;
			}

			if (selectedTieUp != null)
			{
				TCategory? categoryOfTieUp = DbCommon.SelectBaseById(_musicInfoContext.Categories, selectedTieUp.CategoryId);
				if (categoryOfTieUp != null)
				{
					// TCategory 由来項目の設定
					record.Category = categoryOfTieUp.Name;
				}

				TMaker? makerOfTieUp = DbCommon.SelectBaseById(_musicInfoContext.Makers, selectedTieUp.MakerId);
				if (makerOfTieUp != null)
				{
					// TMaker 由来項目の設定
					record.MakerName = makerOfTieUp.Name;
					record.MakerRuby = AdditionalRubyForSearch(makerOfTieUp);
				}

				List<TTieUpGroup> tieUpGroups = DbCommon.SelectSequencedTieUpGroupsByTieUpId(_musicInfoContext.TieUpGroupSequences, _musicInfoContext.TieUpGroups, selectedTieUp.Id);
				if (tieUpGroups.Any())
				{
					// TTieUpGroup 由来項目の設定
					record.TieUpGroupName = tieUpGroups[0].Name;
					record.TieUpGroupRuby = AdditionalRubyForSearch(tieUpGroups[0]);
					foreach(TTieUpGroup group in tieUpGroups)
					{
						record.Comment += KeywordToComment(group);
					}
				}

				// TieUp 由来項目の設定
				record.TieUpId = selectedTieUp.Id;
				record.TieUpName = selectedTieUp.Name;
				record.TieUpRuby = AdditionalRubyForSearch(selectedTieUp);
				record.TieUpAgeLimit = selectedTieUp.AgeLimit;
				record.SongReleaseDate = selectedTieUp.ReleaseDate;
				record.Comment += KeywordToComment(selectedTieUp);
			}

			if (selectedSong == null)
			{
				return;
			}

			// 人物系
			(record.ArtistName, record.ArtistRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.ArtistSequences, _musicInfoContext.People, selectedSong.Id).ToList<IRcMaster>());
			(record.LyristName, record.LyristRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.LyristSequences, _musicInfoContext.People, selectedSong.Id).ToList<IRcMaster>());
			(record.ComposerName, record.ComposerRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.ComposerSequences, _musicInfoContext.People, selectedSong.Id).ToList<IRcMaster>());
			(record.ArrangerName, record.ArrangerRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_musicInfoContext.ArrangerSequences, _musicInfoContext.People, selectedSong.Id).ToList<IRcMaster>());

			// TSong 由来項目の設定
			record.SongId = selectedSong.Id;
			record.SongName = selectedSong.Name;
			record.SongRuby = AdditionalRubyForSearch(selectedSong);
			record.SongOpEd = selectedSong.OpEd;
			if (record.SongReleaseDate <= YlConstants.INVALID_MJD && selectedSong.ReleaseDate > YlConstants.INVALID_MJD)
			{
				record.SongReleaseDate = selectedSong.ReleaseDate;
			}
			if (String.IsNullOrEmpty(record.Category))
			{
				TCategory? categoryOfSong = DbCommon.SelectBaseById(_musicInfoContext.Categories, selectedSong.CategoryId);
				if (categoryOfSong != null)
				{
					record.Category = categoryOfSong.Name;
				}
			}
			record.Comment += KeywordToComment(selectedSong);

			// タグ
			(record.TagName, record.TagRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedTagsBySongId(_musicInfoContext.TagSequences, _musicInfoContext.Tags, selectedSong.Id).ToList<IRcMaster>());
		}

		// --------------------------------------------------------------------
		// 楽曲と紐付くタイアップ
		// --------------------------------------------------------------------
		private Dictionary<TSong, TTieUp> SongsAndTieUps(List<TSong> songs)
		{
			Dictionary<TSong, TTieUp> songsAndTieUps = new();
			foreach (TSong song in songs)
			{
				TTieUp? tieUpOfSong = DbCommon.SelectBaseById(_musicInfoContext.TieUps, song.TieUpId);
				if (tieUpOfSong != null)
				{
					songsAndTieUps[song] = tieUpOfSong;
				}
			}
			return songsAndTieUps;
		}
	}
}
