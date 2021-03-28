// ============================================================================
// 
// ゆかりすたー NEBULA の設定を管理
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.SharedMisc
{
	public class YlSettings
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定
		// --------------------------------------------------------------------

		// ゆかり設定ファイルのパス（相対または絶対）
		public String YukariConfigPathSeed { get; set; } = @"..\" + YlConstants.FILE_NAME_YUKARI_CONFIG;

		// --------------------------------------------------------------------
		// リスト対象
		// --------------------------------------------------------------------

		// リスト化対象ファイルの拡張子
		public List<String> TargetExts = new();

		// --------------------------------------------------------------------
		// メンテナンス
		// --------------------------------------------------------------------

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
		public static String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(YlSettings) + Common.FILE_EXT_CONFIG;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------
		public void Load()
		{
			try
			{
				YlSettings loaded = Common.Deserialize(SettingsPath(), this);
				Common.ShallowCopy(loaded, this);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "アプリケーション設定読み込み時エラー：\n" + excep.Message, true);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				Adjust();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "アプリケーション設定調整時エラー：\n" + excep.Message, true);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------
		public void Save()
		{
			try
			{
				Common.Serialize(SettingsPath(), this);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "アプリケーション設定保存時エラー：\n" + excep.Message, true);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定を調整
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void Adjust()
		{
			// Load() 時に TargetExts が null となるかもしれないので対応
			if (TargetExts == null)
			{
				TargetExts = new();
			}
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
#if false
			if (YukaListerSettings.LastIdNumbers == null)
			{
				YukaListerSettings.LastIdNumbers = new List<Int32>();
			}
			if (YukaListerSettings.LastIdNumbers.Count < (Int32)MusicInfoDbTables.__End__)
			{
				YukaListerSettings.LastIdNumbers.Clear();
				for (Int32 i = 0; i < (Int32)MusicInfoDbTables.__End__; i++)
				{
					YukaListerSettings.LastIdNumbers.Add(0);
				}
			}
			YukaListerSettings.AnalyzeYukariEasyAuthConfig(this);
#endif
		}
	}
}
