// ============================================================================
// 
// TFound の項目を埋める
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;

namespace YukaLister.Models.Database
{
	public class TFoundSetter : IDisposable
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public TFoundSetter(DbSet<TFound> founds)
		{
			_founds = founds;
			_musicInfoContext = MusicInfoContext.CreateContext(out _properties,
					out _songs, out _people, out _tieUps, out _categories,
					out _tieUpGroups, out _makers, out _tags,
					out _songAliases, out _personAliases, out _tieUpAliases,
					out _categoryAliases, out _tieUpGroupAliases, out _makerAliases,
					out _artistSequences, out _lyristSequences, out _composerSequences, out _arrangerSequences,
					out _tieUpGroupSequences, out _tagSequences);
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
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// データベースプロパティーテーブル
		public DbSet<TProperty> _properties;

		// --------------------------------------------------------------------
		// マスターテーブル
		// --------------------------------------------------------------------

		// 楽曲マスターテーブル
		public DbSet<TSong> _songs;

		// 人物マスターテーブル
		public DbSet<TPerson> _people;

		// タイアップマスターテーブル
		public DbSet<TTieUp> _tieUps;

		// カテゴリーマスターテーブル
		public DbSet<TCategory> _categories;

		// タイアップグループマスターテーブル
		public DbSet<TTieUpGroup> _tieUpGroups;

		// 制作会社マスターテーブル
		public DbSet<TMaker> _makers;

		// タグマスターテーブル
		public DbSet<TTag> _tags;

		// --------------------------------------------------------------------
		// 別名テーブル
		// --------------------------------------------------------------------

		// 楽曲別名テーブル
		public DbSet<TSongAlias> _songAliases;

		// 人物別名テーブル
		public DbSet<TPersonAlias> _personAliases;

		// タイアップ別名テーブル
		public DbSet<TTieUpAlias> _tieUpAliases;

		// カテゴリー別名テーブル
		public DbSet<TCategoryAlias> _categoryAliases;

		// タイアップグループ別名テーブル
		public DbSet<TTieUpGroupAlias> _tieUpGroupAliases;

		// 制作会社別名テーブル
		public DbSet<TMakerAlias> _makerAliases;

		// --------------------------------------------------------------------
		// 紐付テーブル
		// --------------------------------------------------------------------

		// 歌手紐付テーブル
		public DbSet<TArtistSequence> _artistSequences;

		// 作詞者紐付テーブル
		public DbSet<TLyristSequence> _lyristSequences;

		// 作曲者紐付テーブル
		public DbSet<TComposerSequence> _composerSequences;

		// 編曲者紐付テーブル
		public DbSet<TArrangerSequence> _arrangerSequences;

		// タイアップグループ紐付テーブル
		public DbSet<TTieUpGroupSequence> _tieUpGroupSequences;

		// タグ紐付テーブル
		public DbSet<TTagSequence> _tagSequences;

		// --------------------------------------------------------------------
		// リストデータベース：検出ファイルリストテーブル
		// --------------------------------------------------------------------

		// 検出ファイルリストテーブル
		private DbSet<TFound> _founds;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// 楽曲情報データベースのコンテキスト
		private MusicInfoContext _musicInfoContext;

		// Dispose フラグ
		private Boolean _isDisposed;

	}
}
