// ============================================================================
// 
// 楽曲情報データベース（通常用）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;

using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.DatabaseContexts
{
	public class MusicInfoContextDefault : MusicInfoContext
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public MusicInfoContextDefault()
				: base("楽曲情報")
		{
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public override void CreateDatabase()
		{
			BackupDatabase();
			base.CreateDatabase();
			InsertCategoryDefaultRecords();
		}

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.YukaListerDatabaseFullFolder() + FILE_NAME_MUSIC_INFO_DATABASE;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// マスターテーブル
			modelBuilder.Entity<TSong>().HasIndex(x => x.Name);
			modelBuilder.Entity<TSong>().HasIndex(x => x.CategoryId);
			modelBuilder.Entity<TSong>().HasIndex(x => x.OpEd);

			modelBuilder.Entity<TPerson>().HasIndex(x => x.Name);

			modelBuilder.Entity<TTieUp>().HasIndex(x => x.Name);
			modelBuilder.Entity<TTieUp>().HasIndex(x => x.CategoryId);

			modelBuilder.Entity<TCategory>().HasIndex(x => x.Name);

			modelBuilder.Entity<TTieUpGroup>().HasIndex(x => x.Name);

			modelBuilder.Entity<TMaker>().HasIndex(x => x.Name);

			modelBuilder.Entity<TTag>().HasIndex(x => x.Name);

			// 別名テーブル
			modelBuilder.Entity<TSongAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TPersonAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TTieUpAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TCategoryAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TTieUpGroupAlias>().HasIndex(x => x.Alias).IsUnique();

			modelBuilder.Entity<TMakerAlias>().HasIndex(x => x.Alias).IsUnique();

			// 紐付テーブル
			modelBuilder.Entity<TArtistSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TLyristSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TComposerSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TArrangerSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TTieUpGroupSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TTagSequence>().HasKey(x => new { x.Id, x.Sequence });
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// データベースファイル名
		private const String FILE_NAME_MUSIC_INFO_DATABASE = "NebulaMusicInfo" + Common.FILE_EXT_SQLITE3;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// カテゴリーテーブルのレコードを作成
		// --------------------------------------------------------------------
		private static TCategory CreateCategoryRecord(Int32 idNumber, String name, String? ruby = null)
		{
			String? normalizedName = YlCommon.NormalizeDbString(name);
			(String? normalizedRubyForMusicInfo, _, _) = YlCommon.NormalizeDbRubyForMusicInfo(ruby);
			if (String.IsNullOrEmpty(normalizedRubyForMusicInfo))
			{
				normalizedRubyForMusicInfo = normalizedName;
			}
			(String? normalizedRubyForSearch, _, _) = YlCommon.NormalizeDbRubyForSearch(normalizedRubyForMusicInfo);

			return new TCategory()
			{
				// IRcBase
				Id = YlConstants.MUSIC_INFO_SYSTEM_ID_PREFIX + YlConstants.MUSIC_INFO_ID_SECOND_PREFIXES[(Int32)MusicInfoTables.TCategory] + idNumber.ToString("D3"),
				Import = false,
				Invalid = false,
				UpdateTime = YlConstants.INVALID_MJD,
				Dirty = true,

				// IRcMaster
				Name = normalizedName,
				Ruby = normalizedRubyForMusicInfo,
				RubyForSearch = normalizedRubyForSearch,
				Keyword = null,
				KeywordRubyForSearch = null,
			};
		}

		// --------------------------------------------------------------------
		// カテゴリーマスターテーブルの既定レコードを挿入
		// ニコニコ動画のカテゴリータグおよび anison.info のカテゴリーから主要な物を抽出
		// --------------------------------------------------------------------
		private void InsertCategoryDefaultRecords()
		{
			if (Categories == null)
			{
				throw new Exception("カテゴリーを作成できません。");
			}

			// 主にタイアップ用
			Categories.Add(CreateCategoryRecord(1, "アニメ"));
			Categories.Add(CreateCategoryRecord(2, "イベント/舞台/公演", "イベントブタイコウエン"));
			Categories.Add(CreateCategoryRecord(3, "ゲーム"));
			Categories.Add(CreateCategoryRecord(4, "時代劇", "ジダイゲキ"));
			Categories.Add(CreateCategoryRecord(5, "特撮", "トクサツ"));
			Categories.Add(CreateCategoryRecord(6, "ドラマ"));
			Categories.Add(CreateCategoryRecord(7, "ラジオ"));

			// 主にタイアップの無い楽曲用
			Categories.Add(CreateCategoryRecord(101, YlConstants.CATEGORY_NAME_VOCALOID, "ボーカロイド"));
			// 102 は欠番（旧：歌ってみた）
			Categories.Add(CreateCategoryRecord(103, YlConstants.CATEGORY_NAME_GENERAL, "イッパン"));

			SaveChanges();
		}
	}
}
