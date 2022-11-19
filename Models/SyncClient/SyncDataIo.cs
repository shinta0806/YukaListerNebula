// ============================================================================
// 
// SyncDataImporter / SyncDataExporter の基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using Shinta.Wpf;

using System;

using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.SyncClient
{
	internal class SyncDataIo : IDisposable
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public SyncDataIo()
		{
			// 最初にログの設定
			YlCommon.SetLogWriterSyncDetail(_logWriterSyncDetail);

			_musicInfoContext = new();
			_yukariStatisticsContext = new();
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

		// ====================================================================
		// protected 変数
		// ====================================================================

		// 楽曲情報データベースのコンテキスト
		protected readonly MusicInfoContextDefault _musicInfoContext;

		// ゆかり統計データベースのコンテキスト
		protected readonly YukariStatisticsContext _yukariStatisticsContext;

		// 詳細ログ（同期専用）
		protected LogWriter _logWriterSyncDetail = new(YlConstants.APP_ID + YlConstants.SYNC_DETAIL_ID);

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

		// Dispose フラグ
		private Boolean _isDisposed;
	}
}
