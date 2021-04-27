// ============================================================================
// 
// ゆかりすたー NEBULA の設定を管理
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
using System.Windows;
using YukaLister.Models.Database;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SerializableSettings
{
	public class YlSettings : SerializableSettings
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定
		// --------------------------------------------------------------------

		// ゆかり設定ファイルのパス（相対または絶対）
		public String YukariConfigPathSeed { get; set; } = @"..\" + YlConstants.FILE_NAME_YUKARI_CONFIG;

		// リムーバブルメディア接続時、前回のフォルダーを自動的に追加する
		public Boolean AddFolderOnDeviceArrived { get; set; } = true;

		// ゆかりでのプレビューを可能にするか
		public Boolean ProvideYukariPreview { get; set; } = true;

		// ゆかり用のサーバーポート
		public Int32 WebServerPort { get; set; } = DEFAULT_WEB_SERVER_PORT;

		// ID の接頭辞
		public String? IdPrefix { get; set; }

		// --------------------------------------------------------------------
		// リスト対象
		// --------------------------------------------------------------------

		// リスト化対象ファイルの拡張子
		public List<String> TargetExts { get; set; } = new();

		// --------------------------------------------------------------------
		// リスト出力
		// --------------------------------------------------------------------

		// リスト出力前に確認
		public Boolean ConfirmOutputYukariList { get; set; }

		// リスト出力先フォルダー
		public String? ListOutputFolder { get; set; }

		// --------------------------------------------------------------------
		// メンテナンス
		// --------------------------------------------------------------------

		// 新着情報を確認するかどうか
		public Boolean CheckRss { get; set; } = true;

		// 楽曲情報データベースを同期するかどうか
		public Boolean SyncMusicInfoDb { get; set; }

		// 楽曲情報データベース同期サーバーアドレス
		public String? SyncServer { get; set; } = "http://";

		// 楽曲情報データベース同期サーバーアカウント名
		public String? SyncAccount { get; set; }

		// 楽曲情報データベース同期サーバーパスワード
		public String? SyncPassword { get; set; }

		// --------------------------------------------------------------------
		// 終了時の状態（ゆかりすたー専用）
		// --------------------------------------------------------------------

		// 前回発行した ID（次回はインクリメントした番号で発行する）
		public List<Int32> LastIdNumbers { get; set; } = new();

		// --------------------------------------------------------------------
		// 終了時の状態（一般）
		// --------------------------------------------------------------------

		// 前回起動時の世代
		public String PrevLaunchGeneration { get; set; } = String.Empty;

		// 前回起動時のバージョン
		public String PrevLaunchVer { get; set; } = String.Empty;

		// 前回起動時のパス
		public String PrevLaunchPath { get; set; } = String.Empty;

		// ウィンドウ位置
		public Rect DesktopBounds { get; set; }

		// RSS 確認日
		public DateTime RssCheckDate { get; set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ゆかり設定ファイルが正しく指定されているかどうか
		// --------------------------------------------------------------------
		public Boolean IsYukariConfigPathValid()
		{
			return File.Exists(YukariConfigPath());
		}

		// --------------------------------------------------------------------
		// 前回使った ID 文字列
		// --------------------------------------------------------------------
		public String LastId(Int32 tableIndex)
		{
			Debug.Assert(!String.IsNullOrEmpty(IdPrefix), "LastId() empty IdPrefix");
			return IdPrefix + YlConstants.MUSIC_INFO_ID_SECOND_PREFIXES[tableIndex] + LastIdNumbers[tableIndex].ToString();
		}

		// --------------------------------------------------------------------
		// LastIdNumbers をこれから使う ID 番号に設定
		// ＜返値＞ これから使う ID 文字列
		// --------------------------------------------------------------------
		public String PrepareLastId<T>(DbSet<T> records) where T : class, IRcBase
		{
			Int32 tableIndex = DbCommon.MusicInfoTableIndex<T>();
			for (; ; )
			{
				LastIdNumbers[tableIndex]++;
				if (DbCommon.SelectBaseById(records, LastId(tableIndex)) == null)
				{
					return LastId(tableIndex);
				}
			}
		}

		// --------------------------------------------------------------------
		// ゆかり設定ファイルのフルパス
		// --------------------------------------------------------------------
		public String YukariConfigPath()
		{
			if (Path.IsPathRooted(YukariConfigPathSeed))
			{
				return YukariConfigPathSeed;
			}
			else
			{
				return Common.MakeAbsolutePath(YukaListerModel.Instance.EnvModel.ExeFullFolder, YukariConfigPathSeed);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定を調整
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected override void AdjustAfterLoad()
		{
			if (TargetExts.Count == 0)
			{
				// 動画の拡張子をアルファベット順に追加（比較的メジャーで現在もサポートが行われている形式のみ）
				TargetExts.Add(Common.FILE_EXT_AVI);
				TargetExts.Add(Common.FILE_EXT_MKV);
				TargetExts.Add(Common.FILE_EXT_MOV);
				TargetExts.Add(Common.FILE_EXT_MP4);
				TargetExts.Add(Common.FILE_EXT_MPG);
				TargetExts.Add(Common.FILE_EXT_WMV);
			}
			if (LastIdNumbers.Count < (Int32)MusicInfoTables.__End__)
			{
				LastIdNumbers.Clear();
				for (Int32 i = 0; i < (Int32)MusicInfoTables.__End__; i++)
				{
					LastIdNumbers.Add(0);
				}
			}
#if false
			YukaListerSettings.AnalyzeYukariEasyAuthConfig(this);
#endif
		}

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected override String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(YlSettings) + Common.FILE_EXT_CONFIG;
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// ゆかり用のサーバーポート
		private const Int32 DEFAULT_WEB_SERVER_PORT = 13582;
	}
}
