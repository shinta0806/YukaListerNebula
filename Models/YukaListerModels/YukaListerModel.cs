﻿// ============================================================================
// 
// ゆかりすたー NEBULA のロジック本体
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet;

namespace YukaLister.Models.YukaListerModels
{
	public class YukaListerModel : NotificationObject
	{
		// ====================================================================
		// static public プロパティー
		// ====================================================================

		// 唯一のインスタンス
		public static YukaListerModel Instance { get; } = new();

		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public YukaListerModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 環境設定
		public EnvironmentModel EnvModel { get; } = new();

		// プロジェクト
		public ProjectModel ProjModel { get; } = new();
	}
}
