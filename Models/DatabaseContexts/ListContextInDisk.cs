// ============================================================================
// 
// リストデータベース（ゆかり用：ディスク）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts
{
	public class ListContextInDisk : ListContext
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ListContextInDisk()
				: base("ゆかり用リスト")
		{
		}

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ListContextInDisk CreateContext(out DbSet<TFound> founds)
		{
			ListContextInDisk listContext = new();
			GetDbSet(listContext, out founds);
			return listContext;
		}

		// --------------------------------------------------------------------
		// データベースコンテキスト生成
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static ListContextInDisk CreateContext(out DbSet<TProperty> properties)
		{
			ListContextInDisk listContext = new();
			GetDbSet(listContext, out properties);
			return listContext;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースのフルパス
		// --------------------------------------------------------------------
		public override String DatabasePath()
		{
			return DbCommon.ListDatabasePath(YukaListerModel.Instance.EnvModel.YlSettings);
		}
	}
}
