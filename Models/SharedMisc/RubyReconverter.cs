// ============================================================================
// 
// 漢字からフリガナを得る
// 
// ============================================================================

// ----------------------------------------------------------------------------
// Input Method Editor Reference
// https://docs.microsoft.com/en-us/previous-versions/office/developer/office-2007/ee828920(v=office.12)?redirectedfrom=MSDN
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YukaLister.Models.SharedMisc
{
	public class RubyReconverter : IDisposable
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public RubyReconverter()
		{
			Type? type = Type.GetTypeFromProgID("MSIME.Japan");
			if (type != null)
			{
				_ime = Activator.CreateInstance(type) as IFELanguage2;
				if (_ime != null)
				{
					if (_ime.Open() != 0)
					{
						_ime.Close();
						_ime = null;
					}
				}
				else
				{
					Debug.WriteLine("RubyReconverter() _ime is null");
				}
			}
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// IDisposable.Dispose()
		// --------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// --------------------------------------------------------------------
		// 漢字をひらがなに変換
		// --------------------------------------------------------------------
		public String? Reconvert(String? kanji)
		{
			if (_ime == null || String.IsNullOrEmpty(kanji))
			{
				return null;
			}

			if (_ime.GetPhonetic(kanji, 1, -1, out String hiragana) != 0)
			{
				return null;
			}

			//Debug.WriteLine("Reconvert() " + hiragana);
			return hiragana;
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected virtual void Dispose(Boolean isDisposing)
		{
			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
				_ime?.Close();
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
		}


		// ====================================================================
		// private 変数
		// ====================================================================

		// IME
		private readonly IFELanguage2? _ime;

		// Dispose フラグ
		private Boolean _isDisposed;
	}

	// ====================================================================
	// IFE Language 2 Interface
	// ====================================================================

	[ComImport]
	[Guid("21164102-C24A-11d1-851A-00C04FCC6B14")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFELanguage2
	{
		// --------------------------------------------------------------------
		// IFE Language 1
		// --------------------------------------------------------------------

		// 初期化
		Int32 Open();

		// 後始末
		Int32 Close();

		// --------------------------------------------------------------------
		// IFE Language 2
		// --------------------------------------------------------------------

		// モーフ解析
		Int32 GetMorphResult(UInt32 dwRequest, UInt32 dwCMode, Int32 cwchInput, [MarshalAs(UnmanagedType.LPWStr)] String pwchInput, IntPtr pfCInfo, out Object ppResult);

		// 変換モード
		Int32 GetConversionModeCaps(ref uint pdwCaps);

		// 漢字→ひらがな
		Int32 GetPhonetic([MarshalAs(UnmanagedType.BStr)] String str, Int32 start, Int32 length, [MarshalAs(UnmanagedType.BStr)] out String result);

		// ひらがな→漢字
		Int32 GetConversion([MarshalAs(UnmanagedType.BStr)] String str, Int32 start, Int32 length, [MarshalAs(UnmanagedType.BStr)] out String result);
	}
}
