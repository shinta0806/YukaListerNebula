// ============================================================================
// 
// FolderSettingsStatus を説明文に変換するコンバーター
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

using YukaLister.Models.SharedMisc;

#nullable enable

namespace YukaLister.Models.ValueConverters
{
	class FolderSettingsStatusToDescriptionConverter : IValueConverter
	{
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンバート
		// --------------------------------------------------------------------
		public Object Convert(Object oValue, Type oTargetType, Object oParameter, CultureInfo oCulture)
		{
			if(!(oValue is FolderSettingsStatus))
			{
				throw new ArgumentException("FolderSettingsStatus 型ではありません。");
			}

			switch (oValue)
			{
				case FolderSettingsStatus.None:
					return "このフォルダーの設定がありません。";
				case FolderSettingsStatus.Set:
					return "このフォルダーは設定済みです。";
				case FolderSettingsStatus.Inherit:
					return "親フォルダーの設定を参照しています（設定変更すると親フォルダーとは別の設定になります）。";
				default:
					Debug.Assert(false, "FolderSettingsStatusToDescriptionConverter.Convert() bad FolderSettingsStatus");
					return String.Empty;
			}
		}

		// --------------------------------------------------------------------
		// 逆コンバート
		// --------------------------------------------------------------------
		public Object ConvertBack(Object oValue, Type oTargetType, Object oParameter, CultureInfo oCulture)
		{
			throw new NotImplementedException();
		}
	}
	// class FolderSettingsStatusToDescriptionConverter ___END___

}
// namespace YukaLister.Models.ValueConverters ___END___
