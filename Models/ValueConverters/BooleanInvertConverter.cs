// ============================================================================
// 
// Boolean を反転するコンバーター
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;

namespace YukaLister.Models.ValueConverters
{
	class BooleanInvertConverter : IValueConverter
	{
		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンバート
		// --------------------------------------------------------------------
		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			if (value is Boolean booleanValue)
			{
				return !booleanValue;
			}
			else
			{
				throw new ArgumentException("Boolean 型ではありません。");
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
