// ============================================================================
// 
// リストデータベース（ゆかり用：ディスク）のコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

#if MOCHIKARA_PRODUCER || MOCHIKARA_PRODUCER_DB
using MochikaraProducer.Models.SharedMisc;
#endif
#if MOCHIKARA_PRODUCER_DB
using MochikaraProducerDb.Models.SharedMisc;
#endif
using Shinta;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.DatabaseContexts;

internal partial class ListContextInDisk : ListContext
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	// --------------------------------------------------------------------
	// メインコンストラクター
	// --------------------------------------------------------------------
	public ListContextInDisk()
			: base("ゆかり用リスト")
	{
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// データベースのフルパス
	// --------------------------------------------------------------------
	public override String DatabasePath()
	{
#if YUKALISTER
		return DbCommon.ListDatabasePath(YlModel.Instance.EnvModel.YlSettings);
#endif
#if MOCHIKARA_PRODUCER
		// デバッグ用
		return CommonWindows.SettingsFolder() + MpConstants.FOLDER_NAME_DATABASE + "List" + Common.FILE_EXT_SQLITE3;
#endif
#if MOCHIKARA_PRODUCER_DB
		// デバッグ用
		return MpdCommon.SettingsFolder() + MpConstants.FOLDER_NAME_DATABASE + "List" + Common.FILE_EXT_SQLITE3;
#endif
	}
}
