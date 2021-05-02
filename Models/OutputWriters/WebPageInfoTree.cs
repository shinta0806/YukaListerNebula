// ============================================================================
// 
// Web 出力用のリスト全体の情報をツリー状に管理
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace YukaLister.Models.OutputWriters
{
	public class WebPageInfoTree
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public WebPageInfoTree()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ページ名
		public String Name { get; set; } = String.Empty;

		// ページファイル名
		// null の場合、構造表現専用ページなので、ページ内容はディスクに出力されない
		public String? FileName { get; set; }

		// ページ内容
		public String? Content { get; set; }

		// ページ単体の曲数
		public Int32 NumSongs { get; set; }

		// ページ単体に加え、子ページとその配下を含む曲数
		public Int32 NumTotalSongs
		{
			get
			{
				Int32 numTotalSongs = NumSongs;
				for (Int32 i = 0; i < Children.Count; i++)
				{
					numTotalSongs += Children[i].NumTotalSongs;
				}
				return numTotalSongs;
			}
		}

		// 子ページ
		// 要素の追加は必ず AddChild() で行うこと
		public List<WebPageInfoTree> Children { get; } = new();

		// 親ページ
		public WebPageInfoTree? Parent { get; private set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 子ページの追加
		// --------------------------------------------------------------------
		public void AddChild(WebPageInfoTree page)
		{
			page.Parent = this;
			Children.Add(page);
		}

		// --------------------------------------------------------------------
		// 階層テキストのリンク
		// --------------------------------------------------------------------
		public String DirectoryLink(String? listLinkArg)
		{
			if (String.IsNullOrEmpty(_directoryLinkCache))
			{
				if (Parent != null)
				{
					_directoryLinkCache = Parent.DirectoryLink(listLinkArg) + " &gt; ";
				}
				if (String.IsNullOrEmpty(FileName))
				{
					// 非リンク
					_directoryLinkCache += Name;
				}
				else
				{
					// リンク
					_directoryLinkCache += "<a href=\"" + FileName + listLinkArg + "\">" + Name + "</a>";
				}
			}
			return _directoryLinkCache;
		}

		// --------------------------------------------------------------------
		// 階層テキスト
		// --------------------------------------------------------------------
		public String DirectoryText()
		{
			if (String.IsNullOrEmpty(_directoryTextCache))
			{
				if (Parent != null)
				{
					_directoryTextCache = Parent.DirectoryText() + " &gt; ";
				}
				_directoryTextCache += Name;
			}
			return _directoryTextCache!;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// DirectoryText キャッシュ用
		private String? _directoryTextCache;

		// DirectoryLink キャッシュ用
		private String? _directoryLinkCache;
	}
}
