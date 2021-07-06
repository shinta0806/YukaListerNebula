// ============================================================================
// 
// SyncDataImporter / SyncDataExporter の基底クラス
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
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.SyncClient
{
	public class SyncDataIo : IDisposable
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public SyncDataIo()
		{
			// 最初にログの設定
			YlCommon.SetLogWriterSyncDetail(_logWriterSyncDetail);

			_musicInfoContext = MusicInfoContextDefault.CreateContext(out _MusicInfoProperties,
					out _songs, out _people, out _tieUps, out _categories,
					out _tieUpGroups, out _makers, out _tags,
					out _songAliases, out _personAliases, out _tieUpAliases,
					out _categoryAliases, out _tieUpGroupAliases, out _makerAliases,
					out _artistSequences, out _lyristSequences, out _composerSequences, out _arrangerSequences,
					out _tieUpGroupSequences, out _tagSequences);

			_yukariStatisticsContext = YukariStatisticsContext.CreateContext(out _YukariStatisticsProperties, out _yukariStatistics);
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
		// protected メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースプロパティーテーブル
		// --------------------------------------------------------------------

		// 楽曲情報データベースプロパティーテーブル
		protected readonly DbSet<TProperty> _MusicInfoProperties;

		// ゆかり統計データベースプロパティーテーブル
		protected readonly DbSet<TProperty> _YukariStatisticsProperties;

		// --------------------------------------------------------------------
		// マスターテーブル
		// --------------------------------------------------------------------

		// 楽曲マスターテーブル
		protected readonly DbSet<TSong> _songs;

		// 人物マスターテーブル
		protected readonly DbSet<TPerson> _people;

		// タイアップマスターテーブル
		protected readonly DbSet<TTieUp> _tieUps;

		// カテゴリーマスターテーブル
		protected readonly DbSet<TCategory> _categories;

		// タイアップグループマスターテーブル
		protected readonly DbSet<TTieUpGroup> _tieUpGroups;

		// 制作会社マスターテーブル
		protected readonly DbSet<TMaker> _makers;

		// タグマスターテーブル
		protected readonly DbSet<TTag> _tags;

		// --------------------------------------------------------------------
		// 別名テーブル
		// --------------------------------------------------------------------

		// 楽曲別名テーブル
		protected readonly DbSet<TSongAlias> _songAliases;

		// 人物別名テーブル
		protected readonly DbSet<TPersonAlias> _personAliases;

		// タイアップ別名テーブル
		protected readonly DbSet<TTieUpAlias> _tieUpAliases;

		// カテゴリー別名テーブル
		protected readonly DbSet<TCategoryAlias> _categoryAliases;

		// タイアップグループ別名テーブル
		protected readonly DbSet<TTieUpGroupAlias> _tieUpGroupAliases;

		// 制作会社別名テーブル
		protected readonly DbSet<TMakerAlias> _makerAliases;

		// --------------------------------------------------------------------
		// 紐付テーブル
		// --------------------------------------------------------------------

		// 歌手紐付テーブル
		protected readonly DbSet<TArtistSequence> _artistSequences;

		// 作詞者紐付テーブル
		protected readonly DbSet<TLyristSequence> _lyristSequences;

		// 作曲者紐付テーブル
		protected readonly DbSet<TComposerSequence> _composerSequences;

		// 編曲者紐付テーブル
		protected readonly DbSet<TArrangerSequence> _arrangerSequences;

		// タイアップグループ紐付テーブル
		protected readonly DbSet<TTieUpGroupSequence> _tieUpGroupSequences;

		// タグ紐付テーブル
		protected readonly DbSet<TTagSequence> _tagSequences;

		// --------------------------------------------------------------------
		// ゆかり統計テーブル
		// --------------------------------------------------------------------

		// ゆかり統計テーブル
		protected readonly DbSet<TYukariStatistics> _yukariStatistics;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// 楽曲情報データベースのコンテキスト
		protected readonly MusicInfoContextDefault _musicInfoContext;

		// ゆかり統計データベースのコンテキスト
		protected readonly YukariStatisticsContext _yukariStatisticsContext;

		// 詳細ログ（同期専用）
		protected LogWriter _logWriterSyncDetail = new(YlConstants.APP_ID + YlConstants.SYNC_DETAIL_ID);

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

		// Dispose フラグ
		private Boolean _isDisposed;
	}
}
