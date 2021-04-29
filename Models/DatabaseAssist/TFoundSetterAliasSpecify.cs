// ============================================================================
// 
// TFound の項目を埋めるが、エイリアスは固定
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 名称の編集ウィンドウで使用するためのクラス
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using System;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.Database.Sequences;
using YukaLister.Models.DatabaseContexts;

namespace YukaLister.Models.DatabaseAssist
{
	public class TFoundSetterAliasSpecify : TFoundSetter
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public TFoundSetterAliasSpecify(ListContextInMemory listContextInMemory, DbSet<TFound> listFounds,
				DbSet<TPerson> listPeople, DbSet<TArtistSequence> listArtistSequences, DbSet<TComposerSequence> listComposerSequences,
				DbSet<TTag> listTags, DbSet<TTagSequence> listTagSequences,
				String? specifiedProgramOrigin, String? specifiedSongOrigin)
				: base(listContextInMemory, listFounds, listPeople, listArtistSequences, listComposerSequences, listTags, listTagSequences)
		{
			_specifiedProgramOrigin = specifiedProgramOrigin;
			_specifiedSongOrigin = specifiedSongOrigin;
		}

		// ====================================================================
		// public メンバー関数
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
		// private メンバー関数
		// ====================================================================

		// 元のタイアップ名
		private String? _specifiedProgramOrigin;

		// 元の楽曲名
		public String? _specifiedSongOrigin;
	}
}
