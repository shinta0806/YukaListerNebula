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
		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			if (value is FolderSettingsStatus folderSettingsStatus)
			{
				return folderSettingsStatus switch
				{
					FolderSettingsStatus.None => "このフォルダーの設定がありません。",
					FolderSettingsStatus.Set => "このフォルダーは設定済みです。",
					FolderSettingsStatus.Inherit => "親フォルダーの設定を参照しています（設定変更すると親フォルダーとは別の設定になります）。",
					_ => String.Empty,
				};
			}
			else
			{
				throw new ArgumentException("FolderSettingsStatus 型ではありません。");
			}
		}

		// --------------------------------------------------------------------
		// 逆コンバート
		// --------------------------------------------------------------------
		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
