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
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.DatabaseAssist
{
	public class TFoundSetter : IDisposable
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public TFoundSetter(ListContextInMemory listContextInMemory, DbSet<TFound> listFounds,
				DbSet<TPerson> listPeople, DbSet<TArtistSequence> listArtistSequences, DbSet<TComposerSequence> listComposerSequences,
				DbSet<TTieUpGroup> listTieUpGroups, DbSet<TTieUpGroupSequence> listTieUpGroupSequences,
				DbSet<TTag> listTags, DbSet<TTagSequence> listTagSequences)
		{
			_listContextInMemory = listContextInMemory;
			_ = listFounds;
			_listPeople = listPeople;
			_listArtistSequences = listArtistSequences;
			_ = listComposerSequences;
			_ = listTieUpGroups;
			_ = listTieUpGroupSequences;
			_ = listTags;
			_ = listTagSequences;
			_musicInfoContext = MusicInfoContext.CreateContext(out _,
					out _songs, out _people, out _tieUps, out _categories,
					out _tieUpGroups, out _makers, out _tags,
					out _songAliases, out _, out _tieUpAliases,
					out _, out _, out _,
					out _artistSequences, out _lyristSequences, out _composerSequences, out _arrangerSequences,
					out _tieUpGroupSequences, out _tagSequences);
			_categoryNames = DbCommon.SelectCategoryNames(_categories);
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 楽曲マスターテーブル
		public DbSet<TSong> Songs
		{
			get => _songs;
		}

		// タイアップマスターテーブル
		public DbSet<TTieUp> TieUps
		{
			get => _tieUps;
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
		// dicByFile に合致する楽曲群を、楽曲情報データベースから検索
		// 楽曲名、タイアップ名、カテゴリー、歌手名で絞り込むが、複数候補となることがあり得る
		// --------------------------------------------------------------------
		public List<TSong> FindSongsByMusicInfoDatabase(Dictionary<String, String?> dicByFile)
		{
			// 楽曲名で検索
			List<TSong> songs = DbCommon.SelectMastersByName(_songs, dicByFile[YlConstants.RULE_VAR_TITLE]);

			// タイアップ名で絞り込み
			if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_PROGRAM] != null)
			{
				List<TSong> songsWithTieUp = new();
				foreach (TSong song in songs)
				{
					TTieUp? tieUp = DbCommon.SelectBaseById(_tieUps, song.TieUpId);
					if (tieUp != null && tieUp.Name == dicByFile[YlConstants.RULE_VAR_PROGRAM])
					{
						songsWithTieUp.Add(song);
					}
				}
				if (songsWithTieUp.Any())
				{
					songs = songsWithTieUp;
				}
			}

			// カテゴリーで絞り込み
			if (songs.Count > 1 && dicByFile[YlConstants.RULE_VAR_CATEGORY] != null)
			{
				List<TSong> songsWithCategory = new();
				foreach (TSong song in songs)
				{
					TCategory? category = DbCommon.SelectBaseById(_categories, song.CategoryId);
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
					(String? artistNames, _) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_artistSequences, _people, song.Id).ToList<IRcMaster>());
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
			if (DbCommon.SelectMasterByName(_tieUps, alias) != null)
			{
				return alias;
			}

			// ToDo: METEOR 時代は SQL で高速化していた
			TTieUpAlias? tieUpAlias = DbCommon.SelectAliasByAlias(_tieUpAliases, alias);
			if (tieUpAlias != null)
			{
				TTieUp? tieUp = DbCommon.SelectBaseById(_tieUps, tieUpAlias.OriginalId);
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
		// oRecord.Path は事前に設定されている必要がある
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
			if (dicByFile[YlConstants.RULE_VAR_COMMENT] != null)
			{
				record.Comment += dicByFile[YlConstants.RULE_VAR_COMMENT];
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
			if (DbCommon.SelectMasterByName(_songs, alias) != null)
			{
				return alias;
			}

			// ToDo: METEOR 時代は SQL で高速化していた
			TSongAlias? songAlias = DbCommon.SelectAliasByAlias(_songAliases, alias);
			if (songAlias != null)
			{
				TSong? song = DbCommon.SelectBaseById(_songs, songAlias.OriginalId);
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
		// private メンバー定数
		// ====================================================================

		// スマートトラック判定用の単語（小文字表記、両端を | で括る）
		private const String OFF_VOCAL_WORDS = "|cho|cut|dam|guide|guidevocal|inst|joy|off|offcho|offvocal|offのみ|vc|オフ|オフボ|オフボーカル|ボイキャン|ボーカルキャンセル|配信|";
		private const String BOTH_VOCAL_WORDS = "|2tr|2ch|onoff|offon|";

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// データベースプロパティーテーブル
		//private readonly DbSet<TProperty> _properties;

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
		//private readonly DbSet<TPersonAlias> _personAliases;

		// タイアップ別名テーブル
		private readonly DbSet<TTieUpAlias> _tieUpAliases;

		// カテゴリー別名テーブル
		//private readonly DbSet<TCategoryAlias> _categoryAliases;

		// タイアップグループ別名テーブル
		//private readonly DbSet<TTieUpGroupAlias> _tieUpGroupAliases;

		// 制作会社別名テーブル
		//private readonly DbSet<TMakerAlias> _makerAliases;

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
		// リストデータベース：検出ファイルリストテーブル
		// --------------------------------------------------------------------

		// 検出ファイルリストテーブル
		//private readonly DbSet<TFound> _listFounds;

		// 人物マスターテーブル
		private readonly DbSet<TPerson> _listPeople;

		// 歌手紐付テーブル
		private readonly DbSet<TArtistSequence> _listArtistSequences;

		// 作曲者紐付テーブル
		//private readonly DbSet<TComposerSequence> _listComposerSequences;

		// タイアップグループマスターテーブル
		//private readonly DbSet<TTieUpGroup> _listTieUpGroups;

		// タイアップグループ紐付テーブル
		//private readonly DbSet<TTieUpGroupSequence> _listTieUpGroupSequences;

		// タグマスターテーブル
		//private readonly DbSet<TTag> _listTags;

		// タグ紐付テーブル
		//private readonly DbSet<TTagSequence> _listTagSequences;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// 楽曲情報データベースのコンテキスト
		private readonly MusicInfoContext _musicInfoContext;

		// リストデータベース（作業用：インメモリ）のコンテキスト
		private readonly ListContextInMemory _listContextInMemory;

		// カテゴリー名正規化用
		private readonly List<String> _categoryNames;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// トラック情報からオンボーカル・オフボーカルがあるか解析する
		// --------------------------------------------------------------------
		private static (Boolean hasOn, Boolean hasOff) AnalyzeSmartTrack(String? trackString)
		{
			Boolean hasOn = false;
			Boolean hasOff = false;

			if (!String.IsNullOrEmpty(trackString))
			{
				String[] tracks = trackString.Split(new Char[] { '-', '_', '+', ',', '.', ' ', (Char)0x2010 }, StringSplitOptions.RemoveEmptyEntries);
				for (Int32 i = 0; i < tracks.Length; i++)
				{
					if (BOTH_VOCAL_WORDS.Contains("|" + tracks[i] + "|", StringComparison.OrdinalIgnoreCase))
					{
						// オンオフ両方を意味する単語の場合
						hasOn = true;
						hasOff = true;
					}
					else
					{
						if (OFF_VOCAL_WORDS.Contains("|" + tracks[i] + "|", StringComparison.OrdinalIgnoreCase))
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
		// 検索ワードと検索ワードフリガナをコメント用に整形
		// --------------------------------------------------------------------
		private static String? KeywordToComment(IRcMaster master)
		{
			String? comment = null;
			if (!String.IsNullOrEmpty(master.Keyword))
			{
				comment += master.Keyword + YlConstants.VAR_VALUE_DELIMITER;
			}
			if (!String.IsNullOrEmpty(master.KeywordRubyForSearch))
			{
				comment += master.KeywordRubyForSearch + YlConstants.VAR_VALUE_DELIMITER;
			}
			return comment;
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

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
				registeredPerson = DbCommon.SelectMasterByName(_listPeople, person.Name);
				if (registeredPerson == null)
				{
					// ID で再検索
					String personId = YlConstants.TEMP_ID_PREFIX + person.Name;
					registeredPerson = DbCommon.SelectBaseById(_listPeople, personId);

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
						_listPeople.Add(registeredPerson);
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
				artists = DbCommon.SelectMastersByName(_people, dicArtist);
				if (artists.Any())
				{
					// 歌手名が楽曲情報データベースに登録されていた場合はその情報を使う
					record.ArtistName = artists[0].Name;
					record.ArtistRuby = artists[0].Ruby;
					RegisterPerson(_listArtistSequences, record, artists[0]);
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
							TPerson? artistsTmp = DbCommon.SelectMasterByName(_listPeople, artistName);
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
							RegisterPerson(_listArtistSequences, record, artists[i]);
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
						RegisterPerson(_listArtistSequences, record, artistTmp);
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
				selectedTieUp = DbCommon.SelectBaseById(_tieUps, selectedSong.TieUpId);
			}
			if (selectedTieUp == null)
			{
				// 曲に紐付くタイアップが無い場合は、ファイル名からタイアップを取得
				if (dicByFile[YlConstants.RULE_VAR_PROGRAM] != null)
				{
					selectedTieUp = DbCommon.SelectMasterByName(_tieUps, dicByFile[YlConstants.RULE_VAR_PROGRAM]);
				}
			}
			if (selectedSong == null && selectedTieUp == null)
			{
				// 曲情報もタイアップ情報も無い場合は諦める
				return;
			}

			if (selectedTieUp != null)
			{
				TCategory? categoryOfTieUp = DbCommon.SelectBaseById(_categories, selectedTieUp.CategoryId);
				if (categoryOfTieUp != null)
				{
					// TCategory 由来項目の設定
					record.Category = categoryOfTieUp.Name;
				}

				TMaker? makerOfTieUp = DbCommon.SelectBaseById(_makers, selectedTieUp.MakerId);
				if (makerOfTieUp != null)
				{
					// TMaker 由来項目の設定
					record.MakerName = makerOfTieUp.Name;
					record.MakerRuby = makerOfTieUp.RubyForSearch;
				}

				List<TTieUpGroup> tieUpGroups = DbCommon.SelectSequencedTieUpGroupsByTieUpId(_tieUpGroupSequences, _tieUpGroups, selectedTieUp.Id);
				if (tieUpGroups.Any())
				{
					// TTieUpGroup 由来項目の設定
					record.TieUpGroupName = tieUpGroups[0].Name;
					record.TieUpGroupRuby = tieUpGroups[0].RubyForSearch;
				}

				// TieUp 由来項目の設定
				record.TieUpId = selectedTieUp.Id;
				record.TieUpName = selectedTieUp.Name;
				record.TieUpRuby = selectedTieUp.RubyForSearch;
				record.TieUpAgeLimit = selectedTieUp.AgeLimit;
				record.SongReleaseDate = selectedTieUp.ReleaseDate;
				record.Comment += KeywordToComment(selectedTieUp);
			}

			if (selectedSong == null)
			{
				return;
			}

			// 人物系
			(record.ArtistName, record.ArtistRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_artistSequences, _people, selectedSong.Id).ToList<IRcMaster>());
			(record.LyristName, record.LyristRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_lyristSequences, _people, selectedSong.Id).ToList<IRcMaster>());
			(record.ComposerName, record.ComposerRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_composerSequences, _people, selectedSong.Id).ToList<IRcMaster>());
			(record.ArrangerName, record.ArrangerRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedPeopleBySongId(_arrangerSequences, _people, selectedSong.Id).ToList<IRcMaster>());

			// TSong 由来項目の設定
			record.SongId = selectedSong.Id;
			record.SongName = selectedSong.Name;
			record.SongRuby = selectedSong.RubyForSearch;
			record.SongOpEd = selectedSong.OpEd;
			if (record.SongReleaseDate <= YlConstants.INVALID_MJD && selectedSong.ReleaseDate > YlConstants.INVALID_MJD)
			{
				record.SongReleaseDate = selectedSong.ReleaseDate;
			}
			if (String.IsNullOrEmpty(record.Category))
			{
				TCategory? categoryOfSong = DbCommon.SelectBaseById(_categories, selectedSong.CategoryId);
				if (categoryOfSong != null)
				{
					record.Category = categoryOfSong.Name;
				}
			}
			record.Comment += KeywordToComment(selectedSong);

			// タグ
			(record.TagName, record.TagRuby) = ConcatMasterNamesAndRubies(DbCommon.SelectSequencedTagsBySongId(_tagSequences, _tags, selectedSong.Id).ToList<IRcMaster>());
		}
	}
}
