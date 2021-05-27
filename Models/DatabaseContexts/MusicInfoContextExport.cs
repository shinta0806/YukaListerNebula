// ============================================================================
// 
// 楽曲情報データベース（エクスポート用）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class MusicInfoContextExport : MusicInfoContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public MusicInfoContextExport(String databasePath)
		{
			_databasePath = databasePath;
		}

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextExport CreateContext(String databasePath, out DbSet<TProperty> properties,
				out DbSet<TSong> songs, out DbSet<TPerson> people, out DbSet<TTieUp> tieUps, out DbSet<TCategory> categories,
				out DbSet<TTieUpGroup> tieUpGroups, out DbSet<TMaker> makers, out DbSet<TTag> tags,
				out DbSet<TSongAlias> songAliases, out DbSet<TPersonAlias> personAliases, out DbSet<TTieUpAlias> tieUpAliases,
				out DbSet<TCategoryAlias> categoryAliases, out DbSet<TTieUpGroupAlias> tieUpGroupAliases, out DbSet<TMakerAlias> makerAliases,
				out DbSet<TArtistSequence> artistSequences, out DbSet<TLyristSequence> lyristSequences, out DbSet<TComposerSequence> composerSequences, out DbSet<TArrangerSequence> arrangerSequences,
				out DbSet<TTieUpGroupSequence> tieUpGroupSequences, out DbSet<TTagSequence> tagSequences)
		{
			MusicInfoContextExport musicInfoContext = new(databasePath);
			GetDbSet(musicInfoContext, out properties,
					out songs, out people, out tieUps, out categories,
					out tieUpGroups, out makers, out tags,
					out songAliases, out personAliases, out tieUpAliases,
					out categoryAliases, out tieUpGroupAliases, out makerAliases,
					out artistSequences, out lyristSequences, out composerSequences, out arrangerSequences,
					out tieUpGroupSequences, out tagSequences);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static MusicInfoContextExport CreateContext(String databasePath, out DbSet<TProperty> properties)
		{
			MusicInfoContextExport musicInfoContext = new(databasePath);
			GetDbSet(musicInfoContext, out properties);
			return musicInfoContext;
		}

		// --------------------------------------------------------------------
		// データベースファイル生成（既存がある場合はクリア）
		// --------------------------------------------------------------------
		public static void CreateDatabase(String databasePath)
		{
			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "エクスポート用楽曲情報データベース初期化中...");

			// クリア
			using MusicInfoContextExport musicInfoContext = CreateContext(databasePath, out DbSet<TProperty> properties);
			musicInfoContext.Database.EnsureDeleted();

			// 新規作成
			musicInfoContext.Database.EnsureCreated();
			DbCommon.UpdateProperty(musicInfoContext, properties);

			YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "エクスポート用楽曲情報データベースを初期化しました。");
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベース設定
		// --------------------------------------------------------------------
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(DbCommon.Connect(_databasePath));
		}

		// --------------------------------------------------------------------
		// データベースモデル作成
		// --------------------------------------------------------------------
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// インデックスの作成は不要だがキーの設定は必要
			// 紐付テーブル
			modelBuilder.Entity<TArtistSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TLyristSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TComposerSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TArrangerSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TTieUpGroupSequence>().HasKey(x => new { x.Id, x.Sequence });

			modelBuilder.Entity<TTagSequence>().HasKey(x => new { x.Id, x.Sequence });
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// データベースパス
		private readonly String _databasePath;
	}
}
