// ============================================================================
// 
// ドライブ接続時にゆかり検索対象フォルダーに自動的に追加するための情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ゆかりすたー 4 NEBULA：シリアライズされるため public class である必要がある
// もちからプロデューサー：JSON を想定
// ----------------------------------------------------------------------------

using Shinta;
#if YUKALISTER
using Shinta.Wpf;
#endif

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.Settings;
#if YUKALISTER
public class AutoTargetInfo : SerializableSettings
#endif
#if MOCHIKARA_PRODUCER
internal class AutoTargetInfo
#endif
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="driveLetter">"D:" のようにコロンまで</param>
	public AutoTargetInfo(String driveLetter)
#if YUKALISTER
			: base(YlModel.Instance.EnvModel.LogWriter)
#endif
	{
		Debug.Assert(driveLetter.Length == 2, "AutoTargetInfo() bad driveLetter");
		_driveLetter = driveLetter;
	}

#if YUKALISTER
	/// <summary>
	/// シリアライズ用コンストラクター
	/// </summary>
	public AutoTargetInfo()
			: base(YlModel.Instance.EnvModel.LogWriter)
	{
		_driveLetter = String.Empty;
	}
#endif

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 前回接続時に追加されていたフォルダー群（ドライブレターを除き '\\' から始まる）
	/// </summary>
	public List<String> Folders
	{
		get;
		set;
	} = [];

	// ====================================================================
	// public 関数
	// ====================================================================

#if YUKALISTER
	/// <summary>
	/// 保存パス
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public override String SettingsPath()
	{
		if (String.IsNullOrEmpty(_driveLetter))
		{
			throw new Exception("ドライブレターが設定されていません。");
		}
		return YlCommon.YukaListerStatusFolderPath(_driveLetter) + FILE_NAME_AUTO_TARGET_INFO;
	}
#endif

	// ====================================================================
	// protected 関数
	// ====================================================================

#if YUKALISTER
	/// <summary>
	/// 保存前の調整
	/// </summary>
	protected override void AdjustBeforeSave()
	{
		YlCommon.YukaListerStatusFolderPath(_driveLetter, true);
	}
#endif

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// 自動追加情報記録ファイル名
	/// </summary>
	private const String FILE_NAME_AUTO_TARGET_INFO = YlConstants.APP_ID + "AutoTarget" + Common.FILE_EXT_CONFIG;

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// ドライブレター
	/// </summary>
	private readonly String _driveLetter;
}
