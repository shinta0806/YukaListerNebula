// ============================================================================
// 
// Web 出力で使用するクエリー結果保存用レコード群
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;

namespace YukaLister.Models.OutputWriters
{
	// 検出ファイルとそれに紐付けられた人物
	internal record QrFoundAndPerson(TFound Found, TPerson Person);

	// 検出ファイルとそれに紐付けられたタイアップグループ
	internal record QrFoundAndTieUpGroup(TFound Found, TTieUpGroup TieUpGroup);

	// 検出ファイルとそれに紐付けられたタグ
	internal record QrFoundAndTag(TFound Found, TTag Tag);
}
