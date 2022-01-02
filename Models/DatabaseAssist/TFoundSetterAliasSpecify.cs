// ============================================================================
// 
// TFound の項目を埋めるが、エイリアスは固定
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 名称の編集ウィンドウで使用するためのクラス
// ----------------------------------------------------------------------------

using System;

using YukaLister.Models.DatabaseContexts;

namespace YukaLister.Models.DatabaseAssist
{
	public class TFoundSetterAliasSpecify : TFoundSetter
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public TFoundSetterAliasSpecify(ListContextInMemory listContextInMemory, String? specifiedProgramOrigin, String? specifiedSongOrigin)
				: base(listContextInMemory)
		{
			_specifiedProgramOrigin = specifiedProgramOrigin;
			_specifiedSongOrigin = specifiedSongOrigin;
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 指定された元のタイアップ名を返す
		// --------------------------------------------------------------------
		public override String? ProgramOrigin(String? alias)
		{
			if (!String.IsNullOrEmpty(_specifiedProgramOrigin))
			{
				return _specifiedProgramOrigin;
			}

			return alias;
		}

		// --------------------------------------------------------------------
		// 指定された元の楽曲名を返す
		// --------------------------------------------------------------------
		public override String? SongOrigin(String? alias)
		{
			if (!String.IsNullOrEmpty(_specifiedSongOrigin))
			{
				return _specifiedSongOrigin;
			}

			return alias;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// 元のタイアップ名
		private readonly String? _specifiedProgramOrigin;

		// 元の楽曲名
		private readonly String? _specifiedSongOrigin;
	}
}
