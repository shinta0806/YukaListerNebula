// ============================================================================
// 
// 楽曲情報データベースのコンテキスト基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;

using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;

namespace YukaLister.Models.DatabaseContexts
{
	public abstract class MusicInfoContext : YukaListerContext
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public MusicInfoContext(String databaseName)
				: base(databaseName)
		{
			Debug.Assert(Songs != null, "Songs table not init");
			Debug.Assert(People != null, "People table not init");
			Debug.Assert(TieUps != null, "TieUps table not init");
			Debug.Assert(Categories != null, "Categories table not init");
			Debug.Assert(TieUpGroups != null, "TieUpGroups table not init");
			Debug.Assert(Makers != null, "Makers table not init");
			Debug.Assert(Tags != null, "Tags table not init");
			Debug.Assert(SongAliases != null, "SongAliases table not init");
			Debug.Assert(PersonAliases != null, "PersonAliases table not init");
			Debug.Assert(TieUpAliases != null, "TieUpAliases table not init");
			Debug.Assert(CategoryAliases != null, "CategoryAliases table not init");
			Debug.Assert(TieUpGroupAliases != null, "TieUpGroupAliases table not init");
			Debug.Assert(MakerAliases != null, "MakerAliases table not init");
			Debug.Assert(ArtistSequences != null, "ArtistSequences table not init");
			Debug.Assert(LyristSequences != null, "LyristSequences table not init");
			Debug.Assert(ComposerSequences != null, "ComposerSequences table not init");
			Debug.Assert(ArrangerSequences != null, "ArrangerSequences table not init");
			Debug.Assert(TieUpGroupSequences != null, "TieUpGroupSequences table not init");
			Debug.Assert(TagSequences != null, "TagSequences table not init");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// マスターテーブル
		// --------------------------------------------------------------------

		// 楽曲マスターテーブル
		public DbSet<TSong> Songs { get; set; }

		// 人物マスターテーブル
		public DbSet<TPerson> People { get; set; }

		// タイアップマスターテーブル
		public DbSet<TTieUp> TieUps { get; set; }

		// カテゴリーマスターテーブル
		public DbSet<TCategory> Categories { get; set; }

		// タイアップグループマスターテーブル
		public DbSet<TTieUpGroup> TieUpGroups { get; set; }

		// 制作会社マスターテーブル
		public DbSet<TMaker> Makers { get; set; }

		// タグマスターテーブル
		public DbSet<TTag> Tags { get; set; }

		// --------------------------------------------------------------------
		// 別名テーブル
		// --------------------------------------------------------------------

		// 楽曲別名テーブル
		public DbSet<TSongAlias> SongAliases { get; set; }

		// 人物別名テーブル
		public DbSet<TPersonAlias> PersonAliases { get; set; }

		// タイアップ別名テーブル
		public DbSet<TTieUpAlias> TieUpAliases { get; set; }

		// カテゴリー別名テーブル
		public DbSet<TCategoryAlias> CategoryAliases { get; set; }

		// タイアップグループ別名テーブル
		public DbSet<TTieUpGroupAlias> TieUpGroupAliases { get; set; }

		// 制作会社別名テーブル
		public DbSet<TMakerAlias> MakerAliases { get; set; }

		// --------------------------------------------------------------------
		// 紐付テーブル
		// --------------------------------------------------------------------

		// 歌手紐付テーブル
		public DbSet<TArtistSequence> ArtistSequences { get; set; }

		// 作詞者紐付テーブル
		public DbSet<TLyristSequence> LyristSequences { get; set; }

		// 作曲者紐付テーブル
		public DbSet<TComposerSequence> ComposerSequences { get; set; }

		// 編曲者紐付テーブル
		public DbSet<TArrangerSequence> ArrangerSequences { get; set; }

		// タイアップグループ紐付テーブル
		public DbSet<TTieUpGroupSequence> TieUpGroupSequences { get; set; }

		// タグ紐付テーブル
		public DbSet<TTagSequence> TagSequences { get; set; }
	}
}
