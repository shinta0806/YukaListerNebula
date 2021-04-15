﻿// ============================================================================
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

		// ID の接頭辞
		public String? IdPrefix { get; set; }

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
#if false
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

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected override String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(YlSettings) + Common.FILE_EXT_CONFIG;
		}
	}
}
