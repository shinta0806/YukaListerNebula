// ============================================================================
// 
// リスト出力用基底クラス
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

using YukaLister.Models.Database;
using YukaLister.Models.Database.Aliases;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.OutputSettingsWindowViewModels;

namespace YukaLister.Models.OutputWriters
{
	public abstract class OutputWriter : IDisposable
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		protected OutputWriter()
		{
			_listContextInMemory = ListContextInMemory.CreateContext(out _founds,
					out _peopleInMemory, out _artistSequencesInMemory, out _composerSequencesInMemory,
					out _tieUpGroupsInMemory, out _tieUpGroupSequencesInMemory,
					out _tagsInMemory, out _tagSequencesInMemory);
			_musicInfoContext = MusicInfoContext.CreateContext(out _,
					out _songsInMusicInfo, out _, out _tieUpsInMusicInfo, out _categoriesInMusicInfo,
					out _, out _makersInMusicInfo, out _,
					out _songAliasesInMusicInfo, out _personAliasesInMusicInfo, out _tieUpAliasesInMusicInfo,
					out _categoryAliasesInMusicInfo, out _tieUpGroupAliasesInMusicInfo, out _makerAliasesInMusicInfo,
					out _, out _lyristSequencesInMusicInfo, out _, out _arrangerSequencesInMusicInfo,
					out _, out _);
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 出力形式（表示用）
		public String FormatName { get; protected set; } = String.Empty;

		// 出力先インデックスファイル名（パス無し）
		public String TopFileName { get; protected set; } = String.Empty;

		// 出力設定
		public OutputSettings OutputSettings { get; protected set; } = new();

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
		// リスト出力設定ウィンドウの ViewModel を生成
		// --------------------------------------------------------------------
		public virtual OutputSettingsWindowViewModel CreateOutputSettingsWindowViewModel()
		{
			return new OutputSettingsWindowViewModel(this);
		}

		// --------------------------------------------------------------------
		// リスト出力
		// --------------------------------------------------------------------
		public abstract void Output();

		// ====================================================================
		// protected 定数
		// ====================================================================

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// マスターテーブル
		// --------------------------------------------------------------------

		// 楽曲マスターテーブル
		protected readonly DbSet<TSong> _songsInMusicInfo;

		// 人物マスターテーブル
		protected readonly DbSet<TPerson> _peopleInMemory;

		// タイアップマスターテーブル
		protected readonly DbSet<TTieUp> _tieUpsInMusicInfo;

		// カテゴリーマスターテーブル
		protected readonly DbSet<TCategory> _categoriesInMusicInfo;

		// タイアップグループマスターテーブル
		protected readonly DbSet<TTieUpGroup> _tieUpGroupsInMemory;

		// 制作会社マスターテーブル
		protected readonly DbSet<TMaker> _makersInMusicInfo;

		// タグマスターテーブル
		protected readonly DbSet<TTag> _tagsInMemory;

		// --------------------------------------------------------------------
		// 別名テーブル
		// --------------------------------------------------------------------

		// 楽曲別名テーブル
		protected readonly DbSet<TSongAlias> _songAliasesInMusicInfo;

		// 人物別名テーブル
		protected readonly DbSet<TPersonAlias> _personAliasesInMusicInfo;

		// タイアップ別名テーブル
		protected readonly DbSet<TTieUpAlias> _tieUpAliasesInMusicInfo;

		// カテゴリー別名テーブル
		protected readonly DbSet<TCategoryAlias> _categoryAliasesInMusicInfo;

		// タイアップグループ別名テーブル
		protected readonly DbSet<TTieUpGroupAlias> _tieUpGroupAliasesInMusicInfo;

		// 制作会社別名テーブル
		protected readonly DbSet<TMakerAlias> _makerAliasesInMusicInfo;

		// --------------------------------------------------------------------
		// 紐付テーブル
		// --------------------------------------------------------------------

		// 歌手紐付テーブル
		protected readonly DbSet<TArtistSequence> _artistSequencesInMemory;

		// 作詞者紐付テーブル
		protected readonly DbSet<TLyristSequence> _lyristSequencesInMusicInfo;

		// 作曲者紐付テーブル
		protected readonly DbSet<TComposerSequence> _composerSequencesInMemory;

		// 編曲者紐付テーブル
		protected readonly DbSet<TArrangerSequence> _arrangerSequencesInMusicInfo;

		// タイアップグループ紐付テーブル
		protected readonly DbSet<TTieUpGroupSequence> _tieUpGroupSequencesInMemory;

		// タグ紐付テーブル
		protected readonly DbSet<TTagSequence> _tagSequencesInMemory;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// リストデータベース（作業用：インメモリ）のコンテキスト
		protected readonly ListContextInMemory _listContextInMemory;

		// 楽曲情報データベースのコンテキスト
		protected readonly MusicInfoContext _musicInfoContext;

		// 検出ファイルリストテーブル
		protected readonly DbSet<TFound> _founds;

		// 実際の出力項目
		protected List<OutputItems> _runtimeOutputItems = new();

		// 出力先フォルダー（末尾 '\\' 付き）
		protected String? _folderPath;

		// ====================================================================
		// protected static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// テンプレート読み込み
		// --------------------------------------------------------------------
		protected static String LoadTemplate(String fileNameBody)
		{
			return File.ReadAllText(YukaListerModel.Instance.EnvModel.ExeFullFolder + YlConstants.FOLDER_NAME_TEMPLATES
					+ fileNameBody + Common.FILE_EXT_TPL);
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
				_listContextInMemory.Dispose();
				_musicInfoContext.Dispose();
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}

		// --------------------------------------------------------------------
		// コンストラクターでは行えない準備などを実施
		// --------------------------------------------------------------------
		protected virtual void PrepareOutput()
		{
			// 設定読み込み
			OutputSettings.Load();

			// OutputSettings.OutputAllItems に基づく設定（コンストラクターでは OutputSettings がロードされていない）
			_runtimeOutputItems = OutputSettings.RuntimeOutputItems();
		}

		// --------------------------------------------------------------------
		// 出力先フォルダーを環境設定のものにする
		// --------------------------------------------------------------------
		protected void SetFolderPathByYlSettings()
		{
			Debug.Assert(!String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder), "SetFolderPathByYlSettings() bad output folder");
			if (!Directory.Exists(YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder))
			{
				Directory.CreateDirectory(YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder);
			}
			_folderPath = YukaListerModel.Instance.EnvModel.YlSettings.ListOutputFolder;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// Dispose フラグ
		private Boolean _isDisposed;
	}
}
