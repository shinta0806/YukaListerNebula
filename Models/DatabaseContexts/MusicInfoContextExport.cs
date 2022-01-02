// ============================================================================
// 
// 楽曲情報データベース（エクスポート用）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;

using YukaLister.Models.Database.Sequences;

namespace YukaLister.Models.DatabaseContexts
{
	public class MusicInfoContextExport : MusicInfoContext
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public MusicInfoContextExport(String databasePath)
				: base("エクスポート用楽曲情報")
		{
			_databasePath = databasePath;
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return _databasePath;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

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
		// private 変数
		// ====================================================================

		// データベースパス
		private readonly String _databasePath;
	}
}
