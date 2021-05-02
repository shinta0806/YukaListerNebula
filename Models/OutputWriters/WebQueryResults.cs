// ============================================================================
// 
// Web 出力で使用するクエリー結果保存用レコード群
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;

namespace YukaLister.Models.OutputWriters
{
	// 検出ファイルとそれに紐付けられた人物
	public record QrFoundAndPerson(TFound Found, TPerson Person);

	// 検出ファイルとそれに紐付けられたタイアップグループ
	public record QrFoundAndTieUpGroup(TFound Found, TTieUpGroup TieUpGroup);

	// 検出ファイルとそれに紐付けられたタグ
	public record QrFoundAndTag(TFound Found, TTag Tag);
}
