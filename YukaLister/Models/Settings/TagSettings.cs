﻿// ============================================================================
// 
// タグ設定を管理
// 
// ============================================================================

// ----------------------------------------------------------------------------
// シリアライズされるため public class である必要がある
// ----------------------------------------------------------------------------

using Shinta;
using Shinta.Wpf;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace YukaLister.Models.Settings
{
	public class TagSettings : SerializableSettings
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// EnvironmentModel 構築時に呼びだされるため、LogWriter は指定できない
		// --------------------------------------------------------------------
		public TagSettings()
				: base(null)
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// フォルダーごとのタグ設定
		// キーは、ドライブレターを除き '\\' から始まるフォルダー名
		[XmlIgnore]
		public ConcurrentDictionary<String, String> FolderTags { get; set; } = new();

		// 保存専用（プログラム中では使用しないこと）
		public List<SerializableKeyValuePair<String, String>> FolderTagsSave { get; set; } = new();


		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		public override String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(TagSettings) + Common.FILE_EXT_CONFIG;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存前の調整
		// --------------------------------------------------------------------
		protected override void AdjustBeforeSave()
		{
			FolderTagsSave.Clear();
			foreach (KeyValuePair<String, String> kvp in FolderTags)
			{
				FolderTagsSave.Add(new SerializableKeyValuePair<String, String>(kvp.Key, kvp.Value));
			}
		}

		// --------------------------------------------------------------------
		// 読み込み後の調整
		// --------------------------------------------------------------------
		protected override void AdjustAfterLoad()
		{
			FolderTags.Clear();
			foreach (SerializableKeyValuePair<String, String> kvp in FolderTagsSave)
			{
				FolderTags[kvp.Key] = kvp.Value;
			}
		}
	}
}
