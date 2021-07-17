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
using System.Text;
using System.Windows;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
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

		// ゆかり用のさらなる検索支援データを出力するか
		// ゆかり側が対応するまでの暫定オプション
		public Boolean OutputAdditionalYukariAssist { get; set; } = true;

		// サムネイルを作成する動画の位置 [S]
		public Int32 ThumbSeekPos { get; set; } = DEFAULT_THUMB_SEEK_POS;

		// サムネイルのデフォルトの横幅 [px]
		public Int32 ThumbDefaultWidth { get; set; } = DEFAULT_THUMB_WIDTH;

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

		// リスト出力先フォルダー（末尾 '\\'）
		private String? _listOutputFolder;
		public String? ListOutputFolder
		{
			get => _listOutputFolder;
			set
			{
				_listOutputFolder = value;
				if (!String.IsNullOrEmpty(_listOutputFolder))
				{
					if (_listOutputFolder[^1] != '\\')
					{
						_listOutputFolder += '\\';
					}
				}
			}
		}

		// --------------------------------------------------------------------
		// メンテナンス
		// --------------------------------------------------------------------

		// 新着情報を確認するかどうか
		public Boolean CheckRss { get; set; } = true;

		// --------------------------------------------------------------------
		// 楽曲情報データベース
		// --------------------------------------------------------------------

		// 楽曲情報データベースを同期するかどうか
		public Boolean SyncMusicInfoDb { get; set; }

		// 楽曲情報データベース同期サーバーアドレス
		public String? SyncServer { get; set; } = "http://";

		// 楽曲情報データベース同期サーバーアカウント名
		public String? SyncAccount { get; set; }

		// 楽曲情報データベース同期サーバーパスワード
		public String? SyncPassword { get; set; }

		// --------------------------------------------------------------------
		// ゆかり統計
		// --------------------------------------------------------------------

		// ゆかり統計出力先
		public String? YukariStatisticsPath { get; set; }

		// --------------------------------------------------------------------
		// ゆかりの設定
		// --------------------------------------------------------------------

		// DB ファイル名（相対または絶対）
		public String YukariRequestDatabasePathSeed { get; set; } = String.Empty;

		// ルーム名
		public String? YukariRoomName { get; set; }

		// 簡易認証を使用するかどうか
		public Boolean YukariUseEasyAuth { get; set; }

		// 簡易認証キーワード
		public String YukariEasyAuthKeyword { get; set; } = String.Empty;

		// --------------------------------------------------------------------
		// 終了時の状態（ゆかりすたー専用）
		// --------------------------------------------------------------------

		// 前回発行した楽曲情報データベースの ID（次回はインクリメントした番号で発行する）
		public List<Int32> LastIdNumbers { get; set; } = new();

		// 前回楽曲情報データベースを同期ダウンロードした日（修正ユリウス日 UTC）
		public Double LastSyncDownloadDate { get; set; }

		// 前回発行したゆかり統計データベースの ID（次回はインクリメントした番号で発行する）
		public Int32 LastYukariStatisticsIdNumber { get; set; }

		// request.db の全消去を検知した日時（修正ユリウス日 UTC）
		public Double LastYukariRequestClearTime { get; set; }

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
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		public static String YlSettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(YlSettings) + Common.FILE_EXT_CONFIG;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ゆかり設定ファイルを解析してゆかりの設定を取得
		// --------------------------------------------------------------------
		public void AnalyzeYukariConfig()
		{
			try
			{
				if (!IsYukariConfigPathValid())
				{
					throw new Exception("ゆかり設定ファイルが正しく指定されていません。");
				}

				String[] config = File.ReadAllLines(YukariConfigPath(), Encoding.UTF8);

				// DB ファイル名
				YukariRequestDatabasePathSeed = YukariConfigValue(config, YUKARI_CONFIG_KEY_NAME_DB_NAME);

				// ルーム名
				YukariRoomName = YukariConfigValue(config, YUKARI_CONFIG_KEY_NAME_ROOM_NAME);

				// 簡易認証を使用するかどうか
				String useEasyAuth = YukariConfigValue(config, YUKARI_CONFIG_KEY_NAME_USE_EASY_AUTH);
				YukariUseEasyAuth = (useEasyAuth == "1");

				// 簡易認証キーワード
				YukariEasyAuthKeyword = YukariConfigValue(config, YUKARI_CONFIG_KEY_NAME_EASY_AUTH_KEYWORD);
			}
			catch (Exception excep)
			{
				// エラーの場合は情報をクリア
				YukariRequestDatabasePathSeed = YUKARI_CONFIG_DEFAULT_DB_NAME;
				YukariUseEasyAuth = false;
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, excep.Message + "サーバーに簡易認証を適用しません。");
			}
		}

		// --------------------------------------------------------------------
		// RSS の確認が必要かどうか
		// --------------------------------------------------------------------
		public Boolean IsCheckRssNeeded()
		{
			if (!CheckRss)
			{
				return false;
			}
			DateTime emptyDate = new();
			TimeSpan day3 = new(3, 0, 0, 0);
			return RssCheckDate == emptyDate || DateTime.Now.Date - RssCheckDate >= day3;
		}

		// --------------------------------------------------------------------
		// ゆかり設定ファイルが正しく指定されているかどうか
		// --------------------------------------------------------------------
		public Boolean IsYukariConfigPathValid()
		{
			return File.Exists(YukariConfigPath());
		}

		// --------------------------------------------------------------------
		// ゆかり設定ファイルが正しく指定されているかどうか
		// --------------------------------------------------------------------
		public Boolean IsYukariRequestDatabasePathValid()
		{
			return File.Exists(YukariRequestDatabasePath());
		}

		// --------------------------------------------------------------------
		// 前回使った楽曲情報データベース ID 文字列
		// --------------------------------------------------------------------
		public String LastId(Int32 tableIndex)
		{
			Debug.Assert(!String.IsNullOrEmpty(IdPrefix), "LastId() empty IdPrefix");
			return IdPrefix + YlConstants.MUSIC_INFO_ID_SECOND_PREFIXES[tableIndex] + LastIdNumbers[tableIndex].ToString();
		}

		// --------------------------------------------------------------------
		// 前回使ったゆかり統計データベース ID 文字列
		// --------------------------------------------------------------------
		public String LastYukariStatisticsId()
		{
			Debug.Assert(!String.IsNullOrEmpty(IdPrefix), "LastYukariStatisticsId() empty IdPrefix");
			return IdPrefix + YlConstants.YUKARI_STATISTICS_ID_SECOND_PREFIXES + LastYukariStatisticsIdNumber.ToString();
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
		// LastIdNumbers をこれから使う ID 番号に設定
		// ＜返値＞ これから使う ID 文字列
		// --------------------------------------------------------------------
		public String PrepareYukariStatisticsLastId(DbSet<TYukariStatistics> records)
		{
			for (; ; )
			{
				LastYukariStatisticsIdNumber++;
				if (DbCommon.SelectBaseById(records, LastYukariStatisticsId()) == null)
				{
					return LastYukariStatisticsId();
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

		// --------------------------------------------------------------------
		// ゆかり request.db のフルパス
		// --------------------------------------------------------------------
		public String YukariRequestDatabasePath()
		{
			if (String.IsNullOrEmpty(YukariRequestDatabasePathSeed))
			{
				return @"C:\xampp\htdocs\" + YUKARI_CONFIG_DEFAULT_DB_NAME;
			}
			else if (Path.IsPathRooted(YukariRequestDatabasePathSeed))
			{
				return YukariRequestDatabasePathSeed;
			}
			else
			{
				return Common.MakeAbsolutePath(Path.GetDirectoryName(YukariConfigPath()), YukariRequestDatabasePathSeed);
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
			AnalyzeYukariConfig();
		}

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected override String SettingsPath()
		{
			return YlSettingsPath();
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// ゆかり用のサーバーポート
		private const Int32 DEFAULT_WEB_SERVER_PORT = 13582;

		// サムネイルを作成する動画の位置 [S]
		private const Int32 DEFAULT_THUMB_SEEK_POS = 60;

		// サムネイルのデフォルトの横幅 [px]
		private const Int32 DEFAULT_THUMB_WIDTH = 128;

		// ゆかりの config.ini の項目
		private const String YUKARI_CONFIG_KEY_NAME_DB_NAME = "dbname";
		private const String YUKARI_CONFIG_KEY_NAME_ROOM_NAME = "commentroom";
		private const String YUKARI_CONFIG_KEY_NAME_USE_EASY_AUTH = "useeasyauth";
		private const String YUKARI_CONFIG_KEY_NAME_EASY_AUTH_KEYWORD = "useeasyauth_word";

		// デフォルト DB ファイル名
		private const String YUKARI_CONFIG_DEFAULT_DB_NAME = "request.db";

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ゆかり設定を config.ini の内容から取得
		// --------------------------------------------------------------------
		private static String YukariConfigValue(String[] config, String keyName)
		{
			// キーを検索
			Int32 line = -1;
			for (Int32 i = 0; i < config.Length; i++)
			{
				if (config[i].StartsWith(keyName + "="))
				{
					line = i;
					break;
				}
			}
			if (line < 0)
			{
				// キーがない
				return String.Empty;
			}

			// 値を検索
			Int32 pos = config[line].IndexOf('=');
			if (pos == config[line].Length - 1)
			{
				// 値がない
				return String.Empty;
			}

			return config[line][(pos + 1)..];
		}
	}
}
