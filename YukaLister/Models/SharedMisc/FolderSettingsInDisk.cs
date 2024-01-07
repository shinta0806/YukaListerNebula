﻿// ============================================================================
// 
// フォルダーごとの設定（フォルダ内に保存する用）
// 
// ============================================================================

// ----------------------------------------------------------------------------
// シリアライズされるため public class である必要がある
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace YukaLister.Models.SharedMisc
{
	public class FolderSettingsInDisk
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 基本情報
		// --------------------------------------------------------------------

		// 保存時のアプリケーションの世代
		public String? AppGeneration { get; set; }

		// 保存時のアプリケーションのバージョン
		public String? AppVer { get; set; }

		// --------------------------------------------------------------------
		// 設定
		// --------------------------------------------------------------------

		// ファイル命名規則（アプリ独自ルール表記）
		public List<String> FileNameRules { get; set; } = new();

		// フォルダー命名規則（アプリ独自ルール表記）
		public List<String> FolderNameRules { get; set; } = new();
	}
}