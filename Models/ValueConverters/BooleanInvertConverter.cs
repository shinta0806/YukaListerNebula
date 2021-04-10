// ============================================================================
// 
// Boolean を反転するコンバーター
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

#nullable enable

namespace YukaLister.Models.ValueConverters
{
	class BooleanInvertConverter : IValueConverter
	{
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンバート
		// --------------------------------------------------------------------
		public Object Convert(Object oValue, Type oTargetType, Object oParameter, CultureInfo oCulture)
		{
			if(!(oValue is Boolean))
			{
				throw new ArgumentException("Boolean 型ではありません。");
			}

			return !(Boolean)oValue;
		}

		// --------------------------------------------------------------------
		// 逆コンバート
		// --------------------------------------------------------------------
		public Object ConvertBack(Object oValue, Type oTargetType, Object oParameter, CultureInfo oCulture)
		{
			throw new NotImplementedException();
		}
	}
	// class BooleanInvertConverter ___END___

}
// namespace YukaLister.Models.ValueConverters ___END___
