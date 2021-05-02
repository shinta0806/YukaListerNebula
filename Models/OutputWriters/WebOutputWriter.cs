// ============================================================================
// 
// HTML / PHP リスト出力用基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 出力フロー
// 1. 新しいリストをテンポラリーフォルダーに作成
// 2. インデックス系を「更新中」の表示にする
// 3. インデックス系以外の古いリストを削除
// 4. インデックス系以外の新しいリストと出力フォルダーに移動
// 5. インデックス系を移動
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shinta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.OutputWriters
{
	public abstract class WebOutputWriter : OutputWriter
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public WebOutputWriter(String listExt)
		{
			_listExt = listExt;

			// テーブル項目名（原則 YlCommon.OUTPUT_ITEM_NAMES だが一部見やすいよう変更）
			_thNames = new List<String>(YlConstants.OUTPUT_ITEM_NAMES);
			_thNames[(Int32)OutputItems.Worker] = "制作";
			_thNames[(Int32)OutputItems.SmartTrack] = "On</th><th>Off";
			_thNames[(Int32)OutputItems.FileSize] = "サイズ";
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// リスト出力設定ウィンドウの ViewModel を生成
		// --------------------------------------------------------------------
		public override OutputSettingsWindowViewModel CreateOutputSettingsWindowViewModel()
		{
			return new WebOutputSettingsWindowViewModel();
		}
#endif

		// --------------------------------------------------------------------
		// リスト出力
		// --------------------------------------------------------------------
		public override void Output()
		{
			PrepareOutput();

			// 内容の生成
			// 生成の順番は GroupNaviCore() と合わせる
			GenerateNew();
			GenerateCategoryAndHeads();
			GenerateTieUpGroupHeadAndTieUpGroups();
			GenerateYearsAndSeasons();
			GeneratePeriodAndHeads();
			GenerateArtistAndHeads();
			GenerateComposerAndHeads();
			GenerateTagHeadAndTags();

			// 内容の調整
			Debug.Assert(_topPage != null, "WebOutputWriter.Output() _topPage is null");
			AdjustList(_topPage);

			// 一時フォルダーへの出力
			OutputList(_topPage!);

			// インデックス系を「更新中」表示にする
			OutputNoticeIndexes();

			// 古いファイルを削除
			DeleteOldListContents();

			// 出力先フォルダーへの出力
			OutputCss();
			OutputJs();

			// その他のファイルの出力
			OutputMisc();

			// 一時フォルダーから移動
			MoveList();
		}

		// ====================================================================
		// protected 定数
		// ====================================================================

		// HTML テンプレートに記載されている変数
		protected const String HTML_VAR_ADDITIONAL_NAVI = "<!-- $AdditionalNavi$ -->";
		protected const String HTML_VAR_GENERATOR = "<!-- $Generator$ -->";

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// リストの拡張子（ピリオド含む）
		protected String _listExt;

		// トップページ
		protected WebPageInfoTree _topPage = new();

		// 追加説明
		protected String? _additionalDescription;

		// 追加 HTML ヘッダー
		protected String? _additionalHeader;

		// 追加階層ナビゲーション
		protected String? _additionalNavi;

		// トップページからリストをリンクする際の引数
		protected String? _listLinkArg;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 曲情報を文字列に追加する際のテーブル内容を追加
		// --------------------------------------------------------------------
		protected virtual void AppendSongInfoAddTd(StringBuilder stringBuilder, OutputItems chapterItem, TFound found)
		{
			foreach (OutputItems outputItem in _runtimeOutputItems)
			{
				if (outputItem == chapterItem)
				{
					continue;
				}

				switch (outputItem)
				{
					case OutputItems.Path:
						stringBuilder.Append("<td class=\"small\">" + FileNameDescription(found.Path) + "</td>");
						break;
					case OutputItems.FileName:
						stringBuilder.Append("<td class=\"small\">" + FileNameDescription(Path.GetFileName(found.Path)) + "</td>");
						break;
					case OutputItems.Head:
						stringBuilder.Append("<td>" + found.Head + "</td>");
						break;
					case OutputItems.Worker:
						stringBuilder.Append("<td>" + found.Worker + "</td>");
						break;
					case OutputItems.Track:
						stringBuilder.Append("<td>" + found.Track + "</td>");
						break;
					case OutputItems.SmartTrack:
						stringBuilder.Append("<td>" + (found.SmartTrackOnVocal ? YlConstants.SMART_TRACK_VALID_MARK : null) + "</td>");
						stringBuilder.Append("<td>" + (found.SmartTrackOffVocal ? YlConstants.SMART_TRACK_VALID_MARK : null) + "</td>");
						break;
					case OutputItems.Comment:
						stringBuilder.Append("<td class=\"small\">" + found.Comment + "</td>");
						break;
					case OutputItems.LastWriteTime:
						stringBuilder.Append("<td class=\"small\">" + JulianDay.ModifiedJulianDateToDateTime(found.LastWriteTime).
								ToString(YlConstants.DATE_FORMAT + " " + YlConstants.TIME_FORMAT) + "</td>");
						break;
					case OutputItems.FileSize:
						stringBuilder.Append("<td class=\"small\">" + (found.FileSize / (1024 * 1024)).ToString("#,0") + " MB</td>");
						break;
					case OutputItems.SongName:
						stringBuilder.Append("<td>" + found.SongName + "</td>");
						break;
					case OutputItems.SongRuby:
						stringBuilder.Append("<td>" + found.SongRuby + "</td>");
						break;
					case OutputItems.SongOpEd:
						stringBuilder.Append("<td>" + found.SongOpEd + "</td>");
						break;
					case OutputItems.SongReleaseDate:
						if (found.SongReleaseDate <= YlConstants.INVALID_MJD)
						{
							stringBuilder.Append("<td></td>");
						}
						else
						{
							stringBuilder.Append("<td class=\"small\">" + JulianDay.ModifiedJulianDateToDateTime(found.SongReleaseDate).ToString(YlConstants.DATE_FORMAT) + "</td>");
						}
						break;
					case OutputItems.ArtistName:
						stringBuilder.Append("<td>" + found.ArtistName + "</td>");
						break;
					case OutputItems.ArtistRuby:
						stringBuilder.Append("<td>" + found.ArtistRuby + "</td>");
						break;
					case OutputItems.LyristName:
						stringBuilder.Append("<td>" + found.LyristName + "</td>");
						break;
					case OutputItems.LyristRuby:
						stringBuilder.Append("<td>" + found.LyristRuby + "</td>");
						break;
					case OutputItems.ComposerName:
						stringBuilder.Append("<td>" + found.ComposerName + "</td>");
						break;
					case OutputItems.ComposerRuby:
						stringBuilder.Append("<td>" + found.ComposerRuby + "</td>");
						break;
					case OutputItems.ArrangerName:
						stringBuilder.Append("<td>" + found.ArrangerName + "</td>");
						break;
					case OutputItems.ArrangerRuby:
						stringBuilder.Append("<td>" + found.ArrangerRuby + "</td>");
						break;
					case OutputItems.TieUpName:
						stringBuilder.Append("<td>" + found.TieUpName + "</td>");
						break;
					case OutputItems.TieUpRuby:
						stringBuilder.Append("<td>" + found.TieUpRuby + "</td>");
						break;
					case OutputItems.TieUpAgeLimit:
						stringBuilder.Append("<td>" + found.TieUpAgeLimit + "</td>");
						break;
					case OutputItems.Category:
						stringBuilder.Append("<td>" + found.Category + "</td>");
						break;
					case OutputItems.TieUpGroupName:
						stringBuilder.Append("<td>" + found.TieUpGroupName + "</td>");
						break;
					case OutputItems.TieUpGroupRuby:
						stringBuilder.Append("<td>" + found.TieUpGroupRuby + "</td>");
						break;
					case OutputItems.MakerName:
						stringBuilder.Append("<td>" + found.MakerName + "</td>");
						break;
					case OutputItems.MakerRuby:
						stringBuilder.Append("<td>" + found.MakerRuby + "</td>");
						break;
					default:
						Debug.Assert(false, "AppendSongInfo() bad outputItem");
						break;
				}
			}
		}

		// --------------------------------------------------------------------
		// 章を開始する際のテーブル見出しを追加
		// --------------------------------------------------------------------
		protected virtual void BeginChapterAddTh(StringBuilder stringBuilder, OutputItems chapterItem)
		{
			foreach (OutputItems outputItem in _runtimeOutputItems)
			{
				if (outputItem == chapterItem)
				{
					continue;
				}

				stringBuilder.Append("<th>" + _thNames[(Int32)outputItem] + "</th>");
			}
		}

		// --------------------------------------------------------------------
		// その他のファイルの削除
		// --------------------------------------------------------------------
		protected virtual void DeleteMisc()
		{
		}

		// --------------------------------------------------------------------
		// リストに出力するファイル名の表現
		// --------------------------------------------------------------------
		protected abstract String? FileNameDescription(String? fileName);

		// --------------------------------------------------------------------
		// その他のファイルの出力
		// --------------------------------------------------------------------
		protected virtual void OutputMisc()
		{
		}

		// --------------------------------------------------------------------
		// コンストラクターでは行えない準備などを実施
		// --------------------------------------------------------------------
		protected override void PrepareOutput()
		{
			base.PrepareOutput();

			// ページ構造の基本をクリア＆生成
			_topPage = new();
			_topPage.Name = "曲一覧";
			_topPage.FileName = IndexFileName(false, KIND_FILE_NAME_CATEGORY);

			WebPageInfoTree general = new();
			general.Name = ZoneName(false);
			_topPage.AddChild(general);

			WebPageInfoTree adult = new();
			adult.Name = ZoneName(true);
			_topPage.AddChild(adult);

			// 一時フォルダー
			_tempFolderPath = YlCommon.TempPath() + "\\";
			Directory.CreateDirectory(_tempFolderPath);
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// リストファイル名の先頭文字列（カテゴリーインデックス以外）
		private const String FILE_NAME_PREFIX = "List";

		// リストの種類に応じたファイル名
		private const String KIND_FILE_NAME_ARTIST = "Artist";
		private const String KIND_FILE_NAME_CATEGORY = "Category";
		private const String KIND_FILE_NAME_COMPOSER = "Composer";
		private const String KIND_FILE_NAME_NEW = "New";
		private const String KIND_FILE_NAME_PERIOD = "Period";
		private const String KIND_FILE_NAME_SEASON = "Season";
		private const String KIND_FILE_NAME_TAG = "Tag";
		private const String KIND_FILE_NAME_TIE_UP_GROUP = "Series";

		// HTML テンプレートに記載されている変数
		private const String HTML_VAR_ADDITIONAL_DESCRIPTION = "<!-- $AdditionalDescription$ -->";
		private const String HTML_VAR_ADDITIONAL_HEADER = "<!-- $AdditionalHeader$ -->";
		private const String HTML_VAR_CATEGORY = "<!-- $Category$ -->";
		private const String HTML_VAR_CATEGORY_INDEX = "<!-- $CategoryIndex$ -->";
		private const String HTML_VAR_CHAPTER_NAME = "<!-- $ChapterName$ -->";
		private const String HTML_VAR_CLASS_OF_KANA = "<!-- $ClassOfKana$ -->";
		private const String HTML_VAR_CLASS_OF_MISC = "<!-- $ClassOfMisc$ -->";
		private const String HTML_VAR_DIRECTORY = "<!-- $Directory$ -->";
		private const String HTML_VAR_GENERATE_DATE = "<!-- $GenerateDate$ -->";
		private const String HTML_VAR_GROUP_NAVI = "<!-- $GroupNavi$ -->";
		private const String HTML_VAR_INDICES = "<!-- $Indices$ -->";
		private const String HTML_VAR_NEIGHBOR = "<!-- $Neighbor$ -->";
		private const String HTML_VAR_NEW = "<!-- $New$ -->";
		private const String HTML_VAR_NUM_SONGS = "<!-- $NumSongs$ -->";
		private const String HTML_VAR_PAGES = "<!-- $Pages$ -->";
		private const String HTML_VAR_PROGRAMS = "<!-- $Programs$ -->";
		private const String HTML_VAR_TITLE = "<!-- $Title$ -->";

		// テーブル非表示
		private const String CLASS_NAME_INVISIBLE = "class=\"invisible\"";

		// 期別リストの年数
		private const Int32 SEASON_YEARS = 5;

		// 文字列を HEX に変換する際の最大長
		// C:\Users\ユーザー名\AppData\Local\Temp\YukaLister\PID..\2_22\List_Artist_GroupName_Hex1_Hex2.html
		// Hex1 / Hex2 は MAX_HEX_SOURCE_LENGTH の 2 倍の長さになる
		// 長くなるのは Hex1 か Hex2 のどちらかという前提で、パスの長さが 256 を超えない程度の指定にする
		private const Int32 MAX_HEX_SOURCE_LENGTH = 70;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// テーブルに表示する項目名
		private List<String> _thNames;

		// リストを一時的に出力するフォルダー（末尾 '\\'）
		private String? _tempFolderPath;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リストの内容を調整する
		// --------------------------------------------------------------------
		private void AdjustList(WebPageInfoTree pageInfoTree)
		{
			// HTML テンプレートの内容にどのページでも使われる変数を適用する
			ReplaceListContent(pageInfoTree, HTML_VAR_ADDITIONAL_HEADER, _additionalHeader);
			ReplaceListContent(pageInfoTree, HTML_VAR_ADDITIONAL_NAVI, _additionalNavi);
			if (OutputSettings != null)
			{
				ReplaceListContent(pageInfoTree, HTML_VAR_GROUP_NAVI, GroupNavi(((WebOutputSettings)OutputSettings).EnableNew));
			}
			ReplaceListContent(pageInfoTree, HTML_VAR_GENERATOR, YlConstants.APP_NAME_J + "  " + YlConstants.APP_VER);
			ReplaceListContent(pageInfoTree, HTML_VAR_GENERATE_DATE, DateTime.Now.ToString(YlConstants.DATE_FORMAT));

			// その他の調整
			AdjustListMisc(pageInfoTree);
		}

		// --------------------------------------------------------------------
		// その他の調整
		// --------------------------------------------------------------------
		private void AdjustListMisc(WebPageInfoTree pageInfoTree)
		{
			// pageInfoTree を調整
			if (!String.IsNullOrEmpty(pageInfoTree.Content))
			{
				pageInfoTree.Content = pageInfoTree.Content!.Replace(HTML_VAR_TITLE, pageInfoTree.DirectoryText());
				pageInfoTree.Content = pageInfoTree.Content.Replace(HTML_VAR_DIRECTORY, pageInfoTree.DirectoryLink(_listLinkArg));
				pageInfoTree.Content = pageInfoTree.Content.Replace(HTML_VAR_NUM_SONGS, pageInfoTree.NumTotalSongs.ToString("#,0"));

				// 隣のページ
				if (pageInfoTree.Parent != null && pageInfoTree.Parent.Children.Count > 1)
				{
					List<WebPageInfoTree> children = pageInfoTree.Parent.Children;
					Int32 index = children.IndexOf(pageInfoTree);
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append("<table class=\"centering\"><tr>");
					if (index > 0)
					{
						stringBuilder.Append("<td class=\"exist\"><a href=\"" + children[index - 1].FileName + _listLinkArg + "\">　&lt;&lt;　"
								+ children[index - 1].Name + "　</a></td>");
					}
					stringBuilder.Append("<td>　" + pageInfoTree.Parent.Name + "　" + pageInfoTree.Name + "　</td>");
					if (index < children.Count - 1)
					{
						stringBuilder.Append("<td class=\"exist\"><a href=\"" + children[index + 1].FileName + _listLinkArg + "\">　"
								+ children[index + 1].Name + "　&gt;&gt;　</a></td>");
					}
					stringBuilder.Append("</tr></table>\n");
					pageInfoTree.Content = pageInfoTree.Content.Replace(HTML_VAR_NEIGHBOR, stringBuilder.ToString());
				}
			}

			// 子ページを調整
			for (Int32 i = 0; i < pageInfoTree.Children.Count; i++)
			{
				AdjustListMisc(pageInfoTree.Children[i]);
			}
		}

		// --------------------------------------------------------------------
		// 曲情報を文字列に追加する
		// --------------------------------------------------------------------
		private void AppendSongInfo(StringBuilder stringBuilder, OutputItems chapterItem, Int32 songsIndex, TFound found)
		{
			stringBuilder.Append("  <tr class=\"");
			if (songsIndex % 2 == 0)
			{
				stringBuilder.Append("even");
			}
			else
			{
				stringBuilder.Append("odd");
			}
			stringBuilder.Append("\">\n    ");
			AppendSongInfoAddTd(stringBuilder, chapterItem, found);
			stringBuilder.Append("\n  </tr>\n");
		}

		// --------------------------------------------------------------------
		// 章を開始する
		// --------------------------------------------------------------------
		private void BeginChapter(StringBuilder stringBuilder, OutputItems chapterItem, Int32 chapterIndex, Int32 numChapters, List<TFound> founds)
		{
			// 章名挿入
			stringBuilder.Append("<input type=\"checkbox\" id=\"label" + chapterIndex + "\" class=\"accparent\"");

			// 章数が 1、かつ、タイアップ名 == 頭文字、の場合（ボカロ等）は、リストが最初から開いた状態にする
			if (numChapters == 1 && founds[0].TieUpName == founds[0].Head)
			{
				stringBuilder.Append(" checked=\"checked\"");
			}
			stringBuilder.Append(">\n");
			stringBuilder.Append("<label for=\"label" + chapterIndex + "\">" + ChapterValue(chapterItem, founds[0]) + "　（"
					+ founds.Count.ToString("#,0") + " 曲）");
			TTieUpGroup? tieUpGroup = DbCommon.SelectMasterByName(_tieUpGroupsInMemory, founds[0].TieUpGroupName);
			if (chapterItem == OutputItems.TieUpName && tieUpGroup != null)
			{
				// 章の区切りがタイアップ名の場合、シリーズがあるなら記載する
				stringBuilder.Append("　<a class=\"series\" href=\"");
				stringBuilder.Append(OutputFileName(founds[0].TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z, KIND_FILE_NAME_TIE_UP_GROUP,
						TieUpGroupHead(tieUpGroup), tieUpGroup.Name + YlConstants.TIE_UP_GROUP_SUFFIX) + _listLinkArg);
				stringBuilder.Append("\">" + tieUpGroup.Name + YlConstants.TIE_UP_GROUP_SUFFIX + "</a>");
			}
			stringBuilder.Append("</label>\n");
			stringBuilder.Append("<div class=\"accchild\">\n");

			// テーブルを開く
			stringBuilder.Append("<table>\n");
			stringBuilder.Append("  <tr>\n    ");
			BeginChapterAddTh(stringBuilder, chapterItem);
			stringBuilder.Append("\n  </tr>\n");
		}

		// --------------------------------------------------------------------
		// 章名として使用する値を返す
		// --------------------------------------------------------------------
		private String ChapterValue(OutputItems chapterItem, TFound found)
		{
			switch (chapterItem)
			{
				case OutputItems.ArtistName:
					return found.ArtistName ?? String.Empty;
				case OutputItems.ComposerName:
					return found.ComposerName ?? String.Empty;
				case OutputItems.TieUpName:
					return found.TieUpName ?? String.Empty;
				default:
					Debug.Assert(false, "ChapterValue() bad chapter item: " + chapterItem.ToString());
					return "Bad";
			}
		}

		// --------------------------------------------------------------------
		// 古いリストを削除（インデックス以外）
		// --------------------------------------------------------------------
		private void DeleteOldListContents()
		{
			DeleteOldListContentsCore(KIND_FILE_NAME_NEW);
			DeleteOldListContentsCore(KIND_FILE_NAME_CATEGORY);
			DeleteOldListContentsCore(KIND_FILE_NAME_TIE_UP_GROUP);
			DeleteOldListContentsCore(KIND_FILE_NAME_PERIOD);
			DeleteOldListContentsCore(KIND_FILE_NAME_SEASON);
			DeleteOldListContentsCore(KIND_FILE_NAME_ARTIST);
			DeleteOldListContentsCore(KIND_FILE_NAME_COMPOSER);
			DeleteOldListContentsCore(KIND_FILE_NAME_TAG);
			DeleteMisc();
		}

		// --------------------------------------------------------------------
		// 古いリストを削除
		// --------------------------------------------------------------------
		private void DeleteOldListContentsCore(String kindFileName)
		{
			Debug.Assert(!String.IsNullOrEmpty(FolderPath), "DeleteOldListContentsCore() FolderPath is null");
			String[] listPathes = Directory.GetFiles(FolderPath, FILE_NAME_PREFIX + "_" + kindFileName + "_*" + _listExt);

			foreach (String path in listPathes)
			{
				try
				{
					File.Delete(path);
				}
				catch (Exception)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "古いリストファイル " + Path.GetFileName(path) + " を削除できませんでした。");
				}
			}
		}

		// --------------------------------------------------------------------
		// 章を終了する
		// --------------------------------------------------------------------
		private void EndChapter(StringBuilder stringBuilder)
		{
			stringBuilder.Append("</table>\n");
			stringBuilder.Append("</div>\n");
		}

		// --------------------------------------------------------------------
		// グループ＝歌手別、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateArtistAndHeads()
		{
			ZonePage(false).AddChild(GenerateArtistAndHeadsCore(false));
			ZonePage(true).AddChild(GenerateArtistAndHeadsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝歌手別、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateArtistAndHeadsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "歌手別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_ARTIST);

			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// TFound と歌手を紐付ける
			List<QrFoundAndPerson> queryResult = GetQrFoundAndPersons(_artistSequencesInMemory, isAdult);
			QrFoundAndPerson? prevRecord = null;
			String? prevPersonHead = null;

			foreach (QrFoundAndPerson record in queryResult)
			{
				String personHead = PersonHead(record.Person);

				if (prevRecord != null && prevPersonHead != null
						&& (personHead != prevPersonHead || record.Person.Ruby != prevRecord.Person.Ruby || record.Person.Name != prevRecord.Person.Name))
				{
					// 頭文字またはページが新しくなったので 1 ページ分出力
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
							KIND_FILE_NAME_ARTIST, prevPersonHead, prevRecord.Person.Name ?? String.Empty, OutputItems.TieUpName);
					prevRecord = null;
				}

				if (prevRecord == null
						|| prevRecord != null && record.Found.TieUpName != prevRecord.Found.TieUpName)
				{
					// タイアップ名が新しくなった
					tieUpNamesAndTFounds[record.Found.TieUpName!] = new();
				}

				// 曲情報追加
				tieUpNamesAndTFounds[record.Found.TieUpName!].Add(record.Found);

				// ループ処理
				prevRecord = record;
				prevPersonHead = personHead;
			}

			if (prevRecord != null && prevPersonHead != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
						KIND_FILE_NAME_ARTIST, prevPersonHead, prevRecord.Person.Name ?? String.Empty, OutputItems.TieUpName);
			}

			// インデックス
			GenerateFreestyleIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_ARTIST, "五十音");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// グループ＝カテゴリー、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateCategoryAndHeads()
		{
			ZonePage(false).AddChild(GenerateCategoryAndHeadsCore(false));
			ZonePage(true).AddChild(GenerateCategoryAndHeadsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝カテゴリー、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateCategoryAndHeadsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "カテゴリー別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_CATEGORY);

			IQueryable<TFound> queryResult = _founds.Where(x => x.TieUpName != null && (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z))
					.OrderBy(x => x.Category).ThenBy(x => x.Head).ThenBy(x => x.TieUpRuby).ThenBy(x => x.TieUpName).ThenBy(x => x.SongRuby).ThenBy(x => x.SongName);
			GenerateCategoryAndHeadsCore(pageInfoTree, queryResult, isAdult, KIND_FILE_NAME_CATEGORY);

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// グループ＝カテゴリー、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateCategoryAndHeadsCore(WebPageInfoTree pageInfoTree, IQueryable<TFound> queryResult, Boolean isAdult, String kindFileName)
		{
			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			TFound? prevFound = null;

			foreach (TFound found in queryResult)
			{
				if (prevFound != null
						&& (found.Category != prevFound.Category || found.Head != prevFound.Head))
				{
					// カテゴリーまたはページが新しくなったので 1 ページ分出力
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
							kindFileName, prevFound.Category, prevFound.Head ?? String.Empty, OutputItems.TieUpName);
				}

				if (!String.IsNullOrEmpty(found.TieUpName))
				{
					if (tieUpNamesAndTFounds.Count == 0 || prevFound != null && found.TieUpName != prevFound.TieUpName)
					{
						// タイアップ名が新しくなった
						tieUpNamesAndTFounds[found.TieUpName] = new();
					}

					// 曲情報追加
					tieUpNamesAndTFounds[found.TieUpName].Add(found);
				}

				// ループ処理
				prevFound = found;
			}

			if (prevFound != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
						kindFileName, prevFound.Category, prevFound.Head ?? String.Empty, OutputItems.TieUpName);
			}

			// インデックス
			GenerateIndexPageContent(pageInfoTree, isAdult, kindFileName, "カテゴリー名");
		}

		// --------------------------------------------------------------------
		// グループ＝作曲者別、ページ＝頭文字、章＝作曲者名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateComposerAndHeads()
		{
			ZonePage(false).AddChild(GenerateComposerAndHeadsCore(false));
			ZonePage(true).AddChild(GenerateComposerAndHeadsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝作曲者別、ページ＝頭文字、章＝タイアップ名、でファイル出力
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateComposerAndHeadsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "作曲者別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_COMPOSER);

			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// TFound と作曲者を紐付ける
			List<QrFoundAndPerson> queryResult = GetQrFoundAndPersons(_composerSequencesInMemory, isAdult);
			QrFoundAndPerson? prevRecord = null;
			String? prevPersonHead = null;

			foreach (QrFoundAndPerson record in queryResult)
			{
				String personHead = PersonHead(record.Person);

				if (prevRecord != null && prevPersonHead != null
						&& (personHead != prevPersonHead || record.Person.Ruby != prevRecord.Person.Ruby || record.Person.Name != prevRecord.Person.Name))
				{
					// 頭文字またはページが新しくなったので 1 ページ分出力
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
							KIND_FILE_NAME_COMPOSER, prevPersonHead, prevRecord.Person.Name ?? String.Empty, OutputItems.TieUpName);
					prevRecord = null;
				}

				if (prevRecord == null
						|| prevRecord != null && record.Found.TieUpName != prevRecord.Found.TieUpName)
				{
					// 番組名が新しくなった
					tieUpNamesAndTFounds[record.Found.TieUpName!] = new List<TFound>();
				}

				// 曲情報追加
				tieUpNamesAndTFounds[record.Found.TieUpName!].Add(record.Found);

				// ループ処理
				prevRecord = record;
				prevPersonHead = personHead;
			}

			if (prevRecord != null && prevPersonHead != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
						KIND_FILE_NAME_COMPOSER, prevPersonHead, prevRecord.Person.Name ?? String.Empty, OutputItems.TieUpName);
			}

			// インデックス
			GenerateFreestyleIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_COMPOSER, "五十音");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// インデックスページ（ページは任意の文字列ごと）の内容を生成
		// --------------------------------------------------------------------
		private void GenerateFreestyleIndexPageContent(WebPageInfoTree indexPage, Boolean isAdult, String kindFileName, String chapterName)
		{
			Int32 groupIndex = 0;
			StringBuilder stringBuilder = new();
			WebPageInfoTree? miscGroup = null;

			// その他以外
			for (Int32 i = 0; i < indexPage.Children.Count; i++)
			{
				if (indexPage.Children[i].Name == YlConstants.GROUP_MISC)
				{
					miscGroup = indexPage.Children[i];
					continue;
				}
				GenerateFreestyleIndexPageContentOneGroup(stringBuilder, indexPage.Children[i], groupIndex, isAdult, kindFileName);
				groupIndex++;
			}

			// その他
			if (miscGroup != null)
			{
				GenerateFreestyleIndexPageContentOneGroup(stringBuilder, miscGroup, groupIndex, isAdult, kindFileName);
			}

			// インデックスページ
			String topTemplate = LoadTemplate("HtmlIndex");
			topTemplate = topTemplate.Replace(HTML_VAR_CHAPTER_NAME, chapterName);
			topTemplate = topTemplate.Replace(HTML_VAR_INDICES, stringBuilder.ToString());
			indexPage.Content = topTemplate;
		}

		// --------------------------------------------------------------------
		// インデックスページ（ページは任意の文字列ごと）の 1 グループ分の内容を生成
		// --------------------------------------------------------------------
		private void GenerateFreestyleIndexPageContentOneGroup(StringBuilder stringBuilder, WebPageInfoTree group, Int32 groupIndex, Boolean isAdult, String kindFileName)
		{
			StringBuilder stringBuilderPages = new();
			Int32 numSongs = 0;
			WebPageInfoTree? miscGroup = null;

			String oneTemplate = LoadTemplate("HtmlFreestyleIndexOneGroup");
			oneTemplate = oneTemplate.Replace(HTML_VAR_CATEGORY, group.Name);

			// その他以外
			foreach (WebPageInfoTree pageInfoTree in group.Children)
			{
				if (pageInfoTree.Name == YlConstants.GROUP_MISC)
				{
					miscGroup = pageInfoTree;
					continue;
				}

				stringBuilderPages.Append("<tr><td class=\"exist\"><a href=\"" + OutputFileName(isAdult, kindFileName, group.Name, pageInfoTree.Name) + _listLinkArg + "\">"
						+ pageInfoTree.Name + " （" + pageInfoTree.NumSongs.ToString("#,0") + " 曲）</a></td></tr>");
				numSongs += pageInfoTree.NumSongs;
			}

			// その他
			if (miscGroup != null)
			{
				WebPageInfoTree pageInfoTree = miscGroup;
				stringBuilderPages.Append("<tr><td class=\"exist\"><a href=\"" + OutputFileName(isAdult, kindFileName, group.Name, pageInfoTree.Name) + _listLinkArg + "\">"
						+ pageInfoTree.Name + " （" + pageInfoTree.NumSongs.ToString("#,0") + " 曲）</a></td></tr>");
				numSongs += pageInfoTree.NumSongs;
			}

			oneTemplate = oneTemplate.Replace(HTML_VAR_PAGES, stringBuilderPages.ToString());
			oneTemplate = oneTemplate.Replace(HTML_VAR_CATEGORY_INDEX, groupIndex.ToString());
			oneTemplate = oneTemplate.Replace(HTML_VAR_NUM_SONGS, "（" + numSongs.ToString("#,0") + " 曲）");
			stringBuilder.Append(oneTemplate);
		}

		// --------------------------------------------------------------------
		// インデックスページ（ページは頭文字ごと）の内容を生成
		// --------------------------------------------------------------------
		private void GenerateIndexPageContent(WebPageInfoTree indexPage, Boolean isAdult, String kindFileName, String chapterName)
		{
			Int32 groupIndex = 0;
			StringBuilder stringBuilder = new();
			WebPageInfoTree? miscGroup = null;

			// その他以外
			for (Int32 i = 0; i < indexPage.Children.Count; i++)
			{
				if (indexPage.Children[i].Name == YlConstants.GROUP_MISC)
				{
					miscGroup = indexPage.Children[i];
					continue;
				}
				GenerateIndexPageContentOneGroup(stringBuilder, indexPage.Children[i], groupIndex, isAdult, kindFileName);
				groupIndex++;
			}

			// その他
			if (miscGroup != null)
			{
				GenerateIndexPageContentOneGroup(stringBuilder, miscGroup, groupIndex, isAdult, kindFileName);
			}

			// インデックスページ
			String topTemplate = LoadTemplate("HtmlIndex");
			topTemplate = topTemplate.Replace(HTML_VAR_CHAPTER_NAME, chapterName);
			topTemplate = topTemplate.Replace(HTML_VAR_INDICES, stringBuilder.ToString());
			indexPage.Content = topTemplate;
		}

		// --------------------------------------------------------------------
		// インデックスページ（ページは頭文字ごと）の 1 グループ分の内容を生成
		// --------------------------------------------------------------------
		private void GenerateIndexPageContentOneGroup(StringBuilder stringBuilder, WebPageInfoTree group, Int32 groupIndex, Boolean isAdult, String kindFileName)
		{
			Boolean hasKana = false;
			Boolean hasMisc = false;
			Int32 numSongs = 0;

			String oneTemplate = LoadTemplate("HtmlIndexOneGroup");
			oneTemplate = oneTemplate.Replace(HTML_VAR_CATEGORY, group.Name);

			foreach (WebPageInfoTree pageInfoTree in group.Children)
			{
				oneTemplate = oneTemplate.Replace("<td>" + pageInfoTree.Name + "</td>", "<td class=\"exist\"><a href=\""
						+ OutputFileName(isAdult, kindFileName, group.Name, pageInfoTree.Name) + _listLinkArg + "\">" + pageInfoTree.Name + "</a></td>");
				numSongs += pageInfoTree.NumSongs;

				if (pageInfoTree.Name == YlConstants.HEAD_MISC)
				{
					hasMisc = true;
				}
				else
				{
					hasKana = true;
				}
			}

			oneTemplate = oneTemplate.Replace(HTML_VAR_CATEGORY_INDEX, groupIndex.ToString());
			oneTemplate = oneTemplate.Replace(HTML_VAR_NUM_SONGS, "（" + numSongs.ToString("#,0") + " 曲）");
			oneTemplate = oneTemplate.Replace(HTML_VAR_CLASS_OF_KANA, hasKana ? null : CLASS_NAME_INVISIBLE);
			oneTemplate = oneTemplate.Replace(HTML_VAR_CLASS_OF_MISC, hasMisc ? null : CLASS_NAME_INVISIBLE);
			stringBuilder.Append(oneTemplate);
		}

		// --------------------------------------------------------------------
		// グループ＝新着、ページ＝カテゴリー、章＝番組名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateNew()
		{
			if (OutputSettings == null || !((WebOutputSettings)OutputSettings).EnableNew)
			{
				return;
			}

			ZonePage(false).AddChild(GenerateNewCore(false));
			ZonePage(true).AddChild(GenerateNewCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝新着、ページ＝カテゴリー、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateNewCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "新着";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_NEW);

			// 番組名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// 新着とする日付
			Int32 deltaDate = 0;
			deltaDate = -((WebOutputSettings)OutputSettings).NewDays;
			Double newDate = JulianDay.DateTimeToModifiedJulianDate(DateTime.Now.AddDays(deltaDate));

			IQueryable<TFound> queryResult = _founds.Where(x => x.TieUpName != null && x.LastWriteTime >= newDate && (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z)).
					OrderBy(x => x.Category).ThenBy(x => x.Head).ThenBy(x => x.TieUpRuby).ThenBy(x => x.TieUpName).ThenBy(x => x.SongRuby).ThenBy(x => x.SongName);
			TFound? prevTFound = null;
			String? prevCategory = null;

			foreach (TFound found in queryResult)
			{
				if (prevTFound != null && prevCategory != null
						&& found.Category != prevCategory)
				{
					// カテゴリーが新しくなったので 1 ページ分出力
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult, KIND_FILE_NAME_NEW, "新着",
							String.IsNullOrEmpty(prevCategory) ? YlConstants.GROUP_MISC : prevCategory, OutputItems.TieUpName);
				}

				if (prevTFound == null
						|| prevTFound != null && found.TieUpName != prevTFound.TieUpName)
				{
					// 番組名が新しくなった
					tieUpNamesAndTFounds[found.TieUpName!] = new();
				}

				// 曲情報追加
				tieUpNamesAndTFounds[found.TieUpName!].Add(found);

				// ループ処理
				prevTFound = found;
				prevCategory = found.Category;
			}

			if (prevTFound != null && prevCategory != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult, KIND_FILE_NAME_NEW, "新着",
						String.IsNullOrEmpty(prevCategory) ? YlConstants.GROUP_MISC : prevCategory, OutputItems.TieUpName);
			}

			// インデックス
			GenerateFreestyleIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_NEW, "新着");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// リストの 1 ページ分を生成
		// ＜引数＞ chaptersAndTFounds: 章（橙色の区切り）ごとの楽曲群
		// --------------------------------------------------------------------
		private void GenerateOneList(WebPageInfoTree parent, Dictionary<String, List<TFound>> chaptersAndTFounds,
				Boolean isAdult, String kindFileName, String? groupName, String pageName, OutputItems chapterItem)
		{
			// null を調整
			if (String.IsNullOrEmpty(groupName))
			{
				groupName = YlConstants.GROUP_MISC;
			}

			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = pageName;
			pageInfoTree.FileName = OutputFileName(isAdult, kindFileName, groupName, pageName);

			String template = LoadTemplate("HtmlList");

			// リスト本体部分
			StringBuilder stringBuilder = new();
			Int32 numPageSongs = 0;
			Int32 chapterIndex = 0;
			try
			{
				foreach (KeyValuePair<String, List<TFound>> chapterAndTFounds in chaptersAndTFounds)
				{
					List<TFound> list = chapterAndTFounds.Value;
					BeginChapter(stringBuilder, chapterItem, chapterIndex, chaptersAndTFounds.Count, list);
					for (Int32 i = 0; i < list.Count; i++)
					{
						AppendSongInfo(stringBuilder, chapterItem, i, list[i]);
					}
					EndChapter(stringBuilder);

					numPageSongs += list.Count;
					chapterIndex++;
				}
			}
			catch (Exception excep)
			{
				// ToDo: METEOR チケット #190 暫定対応（エラー発生位置が分かったら正式対応する）
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "GenerateOneList() リスト本体部分 Exception: " + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "GenerateOneList() oIsAdult: " + isAdult.ToString() + ", oKindFileName: " + kindFileName
						+ ", oGroupName: " + groupName + ", oPageName: " + pageName);
			}

			// テンプレート適用
			try
			{
				template = template.Replace(HTML_VAR_ADDITIONAL_DESCRIPTION, _additionalDescription);
				template = template.Replace(HTML_VAR_CHAPTER_NAME, YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem]);
				template = template.Replace(HTML_VAR_PROGRAMS, stringBuilder.ToString());
			}
			catch (Exception excep)
			{
				// ToDo: METEOR チケット #190 暫定対応（エラー発生位置が分かったら正式対応する）
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "GenerateOneList() テンプレート適用部分 Exception: " + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "GenerateOneList() oIsAdult: " + isAdult.ToString() + ", oKindFileName: " + kindFileName
						+ ", oGroupName: " + groupName + ", oPageName: " + pageName);
			}

			pageInfoTree.Content = template;
			pageInfoTree.NumSongs = numPageSongs;

			// 出力済みの内容をクリア
			chaptersAndTFounds.Clear();

			// oParent 配下のどこにぶら下げるかを検索
			WebPageInfoTree? group = null;
			for (Int32 i = 0; i < parent.Children.Count; i++)
			{
				if (parent.Children[i].Name == groupName)
				{
					group = parent.Children[i];
					break;
				}
			}
			if (group == null)
			{
				// parent 配下に groupName のページを新規作成
				group = new WebPageInfoTree();
				group.Name = groupName;
				parent.AddChild(group);
			}

			// group にぶら下げる
			group.AddChild(pageInfoTree);
		}

		// --------------------------------------------------------------------
		// グループ＝年代、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private void GeneratePeriodAndHeads()
		{
			ZonePage(false).AddChild(GeneratePeriodAndHeadsCore(false));
			ZonePage(true).AddChild(GeneratePeriodAndHeadsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝年代、ページ＝頭文字、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private WebPageInfoTree GeneratePeriodAndHeadsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "年代別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_PERIOD);

			// 番組名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// 年月日設定
			Int32 sinceYear = DateTime.UtcNow.Year;
			sinceYear -= sinceYear % 10;

			while (sinceYear > YlConstants.INVALID_YEAR)
			{
				Int32 untilYear = sinceYear + 10;

				IQueryable<TFound> queryResult = _founds.Where(x => x.TieUpName != null
						&& JulianDay.DateTimeToModifiedJulianDate(new DateTime(sinceYear, 1, 1)) <= x.SongReleaseDate
						&& x.SongReleaseDate < JulianDay.DateTimeToModifiedJulianDate(new DateTime(untilYear, 1, 1))
						&& (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z)).
						OrderBy(x => x.Head).ThenBy(x => x.TieUpRuby).ThenBy(x => x.TieUpName).ThenBy(x => x.SongRuby).ThenBy(x => x.SongName);
				TFound? prevTFound = null;

				foreach (TFound found in queryResult)
				{
					if (prevTFound != null
							&& found.Head != prevTFound.Head)
					{
						// 頭文字が新しくなったので 1 ページ分出力
						GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
								KIND_FILE_NAME_PERIOD, sinceYear.ToString() + " 年代", prevTFound.Head ?? String.Empty, OutputItems.TieUpName);
					}

					if (prevTFound == null
							|| prevTFound != null && found.TieUpName != prevTFound.TieUpName)
					{
						// 番組名が新しくなった
						tieUpNamesAndTFounds[found.TieUpName!] = new();
					}

					// 曲情報追加
					tieUpNamesAndTFounds[found.TieUpName!].Add(found);

					// ループ処理
					prevTFound = found;
				}

				if (prevTFound != null)
				{
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
							KIND_FILE_NAME_PERIOD, sinceYear.ToString() + " 年代", prevTFound.Head ?? String.Empty, OutputItems.TieUpName);
				}

				sinceYear -= 10;
			}

			// インデックス
			GenerateIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_PERIOD, "年代");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// グループ＝タグ名の頭文字、ページ＝タグ名、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateTagHeadAndTags()
		{
			ZonePage(false).AddChild(GenerateTagHeadAndTagsCore(false));
			ZonePage(true).AddChild(GenerateTagHeadAndTagsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝タグ名の頭文字、ページ＝タグ名、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateTagHeadAndTagsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "タグ別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_TAG);

			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// ToDo: JOIN したほうが速いかもしれない
			IQueryable<TFound> founds = _founds.Where(x => x.TieUpName != null && x.SongId != null
					&& (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z));
			List<QrFoundAndTag> queryResult = new(founds.Count());
			foreach (TFound found in founds)
			{
				List<TTag> tags = DbCommon.SelectSequencedTagsBySongId(_tagSequencesInMemory, _tagsInMemory, found.SongId!);
				foreach (TTag tag in tags)
				{
					queryResult.Add(new(found, tag));
				}
			}
			queryResult = queryResult.OrderBy(x => x.Tag.Ruby).ThenBy(x => x.Tag.Name).ThenBy(x => x.Found.Head).ThenBy(x => x.Found.TieUpRuby).
					ThenBy(x => x.Found.TieUpName).ThenBy(x => x.Found.SongRuby).ThenBy(x => x.Found.SongName).ToList();
			QrFoundAndTag? prevRecord = null;
			String? prevTagHead = null;

			foreach (QrFoundAndTag record in queryResult)
			{
				String tagHead = TagHead(record.Tag);

				if (prevRecord != null && prevTagHead != null
						&& (tagHead != prevTagHead || record.Tag.Ruby != prevRecord.Tag.Ruby || record.Tag.Name != prevRecord.Tag.Name))
				{
					// 頭文字またはページが新しくなったので 1 ページ分出力
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
							KIND_FILE_NAME_TAG, prevTagHead, prevRecord.Tag.Name ?? String.Empty, OutputItems.TieUpName);
					prevRecord = null;
				}

				if (prevRecord == null
						|| prevRecord != null && record.Found.TieUpName != prevRecord.Found.TieUpName)
				{
					// タイアップ名が新しくなった
					tieUpNamesAndTFounds[record.Found.TieUpName!] = new();
				}

				// 曲情報追加
				tieUpNamesAndTFounds[record.Found.TieUpName!].Add(record.Found);

				// ループ処理
				prevRecord = record;
				prevTagHead = tagHead;
			}

			if (prevRecord != null && prevTagHead != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
						KIND_FILE_NAME_TAG, prevTagHead, prevRecord.Tag.Name ?? String.Empty, OutputItems.TieUpName);
			}

			// インデックス
			GenerateFreestyleIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_TAG, "五十音");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// グループ＝タイアップグループ名の頭文字、ページ＝タイアップグループ名、章＝タイアップ名、でページ内容生成
		// --------------------------------------------------------------------
		private void GenerateTieUpGroupHeadAndTieUpGroups()
		{
			ZonePage(false).AddChild(GenerateTieUpGroupHeadAndTieUpGroupsCore(false));
			ZonePage(true).AddChild(GenerateTieUpGroupHeadAndTieUpGroupsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝タイアップグループ名の頭文字、ページ＝タイアップグループ名、章＝タイアップ名、でページ内容生成
		// ToDo: GenerateTagHeadAndTagsCore() などと統合できるのではないか
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateTieUpGroupHeadAndTieUpGroupsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "シリーズ別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_TIE_UP_GROUP);

			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// ToDo: JOIN したほうが速いかもしれない
			IQueryable<TFound> founds = _founds.Where(x => x.TieUpName != null && x.SongId != null
					&& (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z));
			List<QrFoundAndTieUpGroup> queryResult = new(founds.Count());
			foreach (TFound found in founds)
			{
				List<TTieUpGroup> tieUpGroups = DbCommon.SelectSequencedTieUpGroupsByTieUpId(_tieUpGroupSequencesInMemory, _tieUpGroupsInMemory, found.SongId!);
				foreach (TTieUpGroup tieUpGroup in tieUpGroups)
				{
					queryResult.Add(new(found, tieUpGroup));
				}
			}
			queryResult = queryResult.OrderBy(x => x.TieUpGroup.Ruby).ThenBy(x => x.TieUpGroup.Name).ThenBy(x => x.Found.Head).ThenBy(x => x.Found.TieUpRuby).
					ThenBy(x => x.Found.TieUpName).ThenBy(x => x.Found.SongRuby).ThenBy(x => x.Found.SongName).ToList();
			QrFoundAndTieUpGroup? prevRecord = null;
			String? prevTieUpGroupHead = null;

			foreach (QrFoundAndTieUpGroup record in queryResult)
			{
				String tieUpGroupHead = TieUpGroupHead(record.TieUpGroup);

				if (prevRecord != null && prevTieUpGroupHead != null
						&& (tieUpGroupHead != prevTieUpGroupHead || record.TieUpGroup.Ruby != prevRecord.TieUpGroup.Ruby || record.TieUpGroup.Name != prevRecord.TieUpGroup.Name))
				{
					// 頭文字またはページが新しくなったので 1 ページ分出力
					GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
							KIND_FILE_NAME_TIE_UP_GROUP, prevTieUpGroupHead, prevRecord.TieUpGroup.Name + YlConstants.TIE_UP_GROUP_SUFFIX, OutputItems.TieUpName);
				}

				if (prevRecord == null
						|| prevRecord != null && record.Found.TieUpName != prevRecord.Found.TieUpName)
				{
					// 番組名が新しくなった
					tieUpNamesAndTFounds[record.Found.TieUpName!] = new List<TFound>();
				}

				// 曲情報追加
				tieUpNamesAndTFounds[record.Found.TieUpName!].Add(record.Found);

				// ループ処理
				prevRecord = record;
				prevTieUpGroupHead = tieUpGroupHead;
			}

			if (prevRecord != null && prevTieUpGroupHead != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult,
						KIND_FILE_NAME_TIE_UP_GROUP, prevTieUpGroupHead, prevRecord.TieUpGroup.Name + YlConstants.TIE_UP_GROUP_SUFFIX, OutputItems.TieUpName);
			}

			// インデックス
			GenerateFreestyleIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_TIE_UP_GROUP, "五十音");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// グループ＝年、ページ＝季節、章＝番組名、でページ内容生成
		// 直近 SEASON_YEARS 年分のみ
		// --------------------------------------------------------------------
		private void GenerateYearsAndSeasons()
		{
			ZonePage(false).AddChild(GenerateYearsAndSeasonsCore(false));
			ZonePage(true).AddChild(GenerateYearsAndSeasonsCore(true));
		}

		// --------------------------------------------------------------------
		// グループ＝年、ページ＝季節、章＝番組名、でページ内容生成
		// 直近 SEASON_YEARS 年分のみ
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateYearsAndSeasonsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = "期別";
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_SEASON);

			// 年月日設定
			Int32 sinceYear = DateTime.UtcNow.Year;
			Int32 untilYear = sinceYear - SEASON_YEARS;

			while (sinceYear > untilYear)
			{
				GenerateYearsAndSeasonsOneSeason(pageInfoTree, isAdult, sinceYear, 1, sinceYear, 4, "1 月～3 月：冬");
				GenerateYearsAndSeasonsOneSeason(pageInfoTree, isAdult, sinceYear, 4, sinceYear, 7, "4 月～6 月：春");
				GenerateYearsAndSeasonsOneSeason(pageInfoTree, isAdult, sinceYear, 7, sinceYear, 10, "7 月～9 月：夏");
				GenerateYearsAndSeasonsOneSeason(pageInfoTree, isAdult, sinceYear, 10, sinceYear + 1, 1, "10 月～12 月：秋");
				sinceYear--;
			}

			// インデックス
			GenerateFreestyleIndexPageContent(pageInfoTree, isAdult, KIND_FILE_NAME_SEASON, "年");

			return pageInfoTree;
		}

		// --------------------------------------------------------------------
		// 1 期分をページ内容生成
		// --------------------------------------------------------------------
		private void GenerateYearsAndSeasonsOneSeason(WebPageInfoTree pageInfoTree, Boolean isAdult,
				Int32 sinceYear, Int32 sinceMonth, Int32 untilYear, Int32 untilMonth, String seasonName)
		{
			// 番組名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			IQueryable<TFound> queryResult = _founds.Where(x => x.TieUpName != null
					&& JulianDay.DateTimeToModifiedJulianDate(new DateTime(sinceYear, sinceMonth, 1)) <= x.SongReleaseDate
					&& x.SongReleaseDate < JulianDay.DateTimeToModifiedJulianDate(new DateTime(untilYear, untilMonth, 1))
					&& (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z)).
					OrderBy(x => x.Head).ThenBy(x => x.TieUpRuby).ThenBy(x => x.TieUpName).ThenBy(x => x.SongRuby).ThenBy(x => x.SongName);
			TFound? prevTFound = null;

			foreach (TFound found in queryResult)
			{
				Debug.Assert(found.TieUpName != null, "GenerateYearsAndSeasonsOneSeason() tie up name is null");
				if (prevTFound == null
						|| prevTFound != null && found.TieUpName != prevTFound.TieUpName)
				{
					// 番組名が新しくなった
					tieUpNamesAndTFounds[found.TieUpName!] = new List<TFound>();
				}

				// 曲情報追加
				tieUpNamesAndTFounds[found.TieUpName!].Add(found);

				// ループ処理
				prevTFound = found;
			}

			if (prevTFound != null)
			{
				GenerateOneList(pageInfoTree, tieUpNamesAndTFounds, isAdult, KIND_FILE_NAME_SEASON, sinceYear.ToString() + " 年",
						seasonName, OutputItems.TieUpName);
			}
		}

		// --------------------------------------------------------------------
		// TFound と人物（歌手、作曲者）を紐付けたリストを取得
		// --------------------------------------------------------------------
		private List<QrFoundAndPerson> GetQrFoundAndPersons<T>(DbSet<T> records, Boolean isAdult) where T : class, IRcSequence
		{
			// ToDo: JOIN したほうが速いかもしれない
			IQueryable<TFound> founds = _founds.Where(x => x.TieUpName != null && x.SongId != null
					&& (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z));
			List<QrFoundAndPerson> queryResult = new(founds.Count());
			foreach (TFound found in founds)
			{
				List<TPerson> people = DbCommon.SelectSequencedPeopleBySongId(records, _peopleInMemory, found.SongId!);
				foreach (TPerson person in people)
				{
					queryResult.Add(new(found, person));
				}
			}
			return queryResult.OrderBy(x => x.Person.Ruby).ThenBy(x => x.Person.Name).ThenBy(x => x.Found.Head).ThenBy(x => x.Found.TieUpRuby).
					ThenBy(x => x.Found.TieUpName).ThenBy(x => x.Found.SongRuby).ThenBy(x => x.Found.SongName).ToList();
		}

		// --------------------------------------------------------------------
		// グループナビ文字列を生成
		// --------------------------------------------------------------------
		private String GroupNavi(Boolean newExists)
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append("<table>\n");
			GroupNaviCore(stringBuilder, false, newExists);
			GroupNaviCore(stringBuilder, true, newExists);
			stringBuilder.Append("</table>\n");
			return stringBuilder.ToString();
		}

		// --------------------------------------------------------------------
		// グループナビ文字列を生成
		// ナビの順番は Output() と合わせる
		// --------------------------------------------------------------------
		private void GroupNaviCore(StringBuilder stringBuilder, Boolean isAdult, Boolean newExists)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>　" + ZoneName(isAdult) + "　</td>");

			// 新着を最優先
			if (newExists)
			{
				stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_NEW) + _listLinkArg + "\">　新着　</a></td>");
			}

			// 全曲を網羅するカテゴリーと、関連するシリーズは新着に次ぐ優先
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_CATEGORY) + _listLinkArg + "\">　カテゴリー別　</a></td>");
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_TIE_UP_GROUP) + _listLinkArg + "\">　シリーズ別　</a></td>");

			// 利用頻度が高い期別と、関連する年代別
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_SEASON) + _listLinkArg + "\">　期別　</a></td>");
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_PERIOD) + _listLinkArg + "\">　年代別　</a></td>");

			// 人別はさほど優先度が高くない
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_ARTIST) + _listLinkArg + "\">　歌手別　</a></td>");
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_COMPOSER) + _listLinkArg + "\">　作曲者別　</a></td>");

			// PC ごとに異なるタグ別は優先度低
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + IndexFileName(isAdult, KIND_FILE_NAME_TAG) + _listLinkArg + "\">　タグ別　</a></td>");
			stringBuilder.Append("</tr>\n");
		}

		// --------------------------------------------------------------------
		// インデックスファイル名
		// --------------------------------------------------------------------
		private String IndexFileName(Boolean isAdult, String kindFileName)
		{
			if (kindFileName == KIND_FILE_NAME_CATEGORY)
			{
				return Path.GetFileNameWithoutExtension(TopFileName) + (isAdult ? "_" + YlConstants.AGE_LIMIT_CERO_Z.ToString() : null) + Path.GetExtension(TopFileName);
			}
			else
			{
				return FILE_NAME_PREFIX + "_index_" + kindFileName + (isAdult ? "_" + YlConstants.AGE_LIMIT_CERO_Z.ToString() : null) + _listExt;
			}
		}

		// --------------------------------------------------------------------
		// 一時フォルダーからリストを移動
		// --------------------------------------------------------------------
		private void MoveList()
		{
			MoveListContentsCore(KIND_FILE_NAME_NEW);
			MoveListIndexCore(false, KIND_FILE_NAME_NEW);
			MoveListIndexCore(true, KIND_FILE_NAME_NEW);

			MoveListContentsCore(KIND_FILE_NAME_CATEGORY);
			MoveListIndexCore(false, KIND_FILE_NAME_CATEGORY);
			MoveListIndexCore(true, KIND_FILE_NAME_CATEGORY);

			MoveListContentsCore(KIND_FILE_NAME_TIE_UP_GROUP);
			MoveListIndexCore(false, KIND_FILE_NAME_TIE_UP_GROUP);
			MoveListIndexCore(true, KIND_FILE_NAME_TIE_UP_GROUP);

			MoveListContentsCore(KIND_FILE_NAME_PERIOD);
			MoveListIndexCore(false, KIND_FILE_NAME_PERIOD);
			MoveListIndexCore(true, KIND_FILE_NAME_PERIOD);

			MoveListContentsCore(KIND_FILE_NAME_SEASON);
			MoveListIndexCore(false, KIND_FILE_NAME_SEASON);
			MoveListIndexCore(true, KIND_FILE_NAME_SEASON);

			MoveListContentsCore(KIND_FILE_NAME_ARTIST);
			MoveListIndexCore(false, KIND_FILE_NAME_ARTIST);
			MoveListIndexCore(true, KIND_FILE_NAME_ARTIST);

			MoveListContentsCore(KIND_FILE_NAME_COMPOSER);
			MoveListIndexCore(false, KIND_FILE_NAME_COMPOSER);
			MoveListIndexCore(true, KIND_FILE_NAME_COMPOSER);

			MoveListContentsCore(KIND_FILE_NAME_TAG);
			MoveListIndexCore(false, KIND_FILE_NAME_TAG);
			MoveListIndexCore(true, KIND_FILE_NAME_TAG);
		}

		// --------------------------------------------------------------------
		// 一時フォルダーからリスト（インデックス以外）を移動
		// --------------------------------------------------------------------
		private void MoveListContentsCore(String kindFileName)
		{
			Debug.Assert(!String.IsNullOrEmpty(_tempFolderPath), "MoveListContentsCore() _tempFolderPath is null");
			String[] listPathes = Directory.GetFiles(_tempFolderPath, FILE_NAME_PREFIX + "_" + kindFileName + "_*" + _listExt);

			foreach (String path in listPathes)
			{
				try
				{
					File.Move(path, FolderPath + Path.GetFileName(path));
				}
				catch (Exception)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "リストファイル " + Path.GetFileName(path) + " を移動できませんでした。");
				}
			}
		}

		// --------------------------------------------------------------------
		// 一時フォルダーからリスト（インデックス）を移動
		// --------------------------------------------------------------------
		private void MoveListIndexCore(Boolean isAdult, String kindFileName)
		{
			String indexFileName = IndexFileName(isAdult, kindFileName);
			try
			{
				// File.Move() には上書きフラグが無いので File.Copy() を使う
				File.Copy(_tempFolderPath + indexFileName, FolderPath + indexFileName, true);
				File.Delete(_tempFolderPath + indexFileName);
			}
			catch (Exception)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "リストファイル " + Path.GetFileName(indexFileName) + " を移動できませんでした。");
			}
		}

		// --------------------------------------------------------------------
		// CSS を出力
		// HtmlCss テンプレートは WebServer でも使用するので内容を変更せずに出力する前提
		// --------------------------------------------------------------------
		private void OutputCss()
		{
			String template = LoadTemplate("HtmlCss");
			File.WriteAllText(FolderPath + "List.css", template, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// リストファイル名
		// --------------------------------------------------------------------
		private String OutputFileName(Boolean isAdult, String kindFileName, String groupName, String? pageName)
		{
			return FILE_NAME_PREFIX + "_" + kindFileName + "_" + (isAdult ? YlConstants.AGE_LIMIT_CERO_Z.ToString() + "_" : null)
					+ StringToHex(groupName) + (String.IsNullOrEmpty(pageName) ? null : "_" + StringToHex(pageName)) + _listExt;
		}

		// --------------------------------------------------------------------
		// JS を出力
		// --------------------------------------------------------------------
		private void OutputJs()
		{
			String template = LoadTemplate("HtmlJs");
			File.WriteAllText(FolderPath + "List.js", template, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// リストを一時フォルダーに出力
		// --------------------------------------------------------------------
		private void OutputList(WebPageInfoTree pageInfoTree)
		{
			// pageInfoTree の内容を出力
			if (!String.IsNullOrEmpty(pageInfoTree.FileName))
			{
				try
				{
					File.WriteAllText(_tempFolderPath + pageInfoTree.FileName, pageInfoTree.Content, Encoding.UTF8);
				}
				catch (Exception)
				{
					// ファイル名が長すぎる場合はエラーとなる
				}
			}

			// 子ページの内容を出力
			for (Int32 i = 0; i < pageInfoTree.Children.Count; i++)
			{
				OutputList(pageInfoTree.Children[i]);
			}
		}

		// --------------------------------------------------------------------
		// 更新中を表示する出力
		// --------------------------------------------------------------------
		private void OutputNoticeIndexes()
		{
			// 内容
			String topTemplate = LoadTemplate("HtmlIndexNotice");

			// 新着（実際には新着が無い場合でも、更新中リストとしては 404 にならないように新着も出力しておく）
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_NEW), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_NEW), topTemplate, Encoding.UTF8);

			// インデックス系
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_CATEGORY), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_CATEGORY), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_TIE_UP_GROUP), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_TIE_UP_GROUP), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_PERIOD), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_PERIOD), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_SEASON), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_SEASON), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_ARTIST), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_ARTIST), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(false, KIND_FILE_NAME_COMPOSER), topTemplate, Encoding.UTF8);
			File.WriteAllText(FolderPath + IndexFileName(true, KIND_FILE_NAME_COMPOSER), topTemplate, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// 人物の頭文字
		// --------------------------------------------------------------------
		private String PersonHead(TPerson person)
		{
			// 人物データベースにルビが無い場合に名前から頭文字を取るようにすると、「その他」とひらがなが入り乱れてしまうため、
			// ルビが無い場合は常に「その他」を返すようにする
			return !String.IsNullOrEmpty(person.Ruby) ? YlCommon.Head(person.Ruby) : YlConstants.HEAD_MISC;
		}

		// --------------------------------------------------------------------
		// ページ内容を置換
		// --------------------------------------------------------------------
		private void ReplaceListContent(WebPageInfoTree pageInfoTree, String oldStr, String? newStr)
		{
			// oPageInfoTree の内容を置換
			if (!String.IsNullOrEmpty(pageInfoTree.Content))
			{
				pageInfoTree.Content = pageInfoTree.Content?.Replace(oldStr, newStr);
			}

			// 子ページの内容を置換
			for (Int32 i = 0; i < pageInfoTree.Children.Count; i++)
			{
				ReplaceListContent(pageInfoTree.Children[i], oldStr, newStr);
			}
		}

		// --------------------------------------------------------------------
		// 文字を UTF-16 HEX に変換
		// --------------------------------------------------------------------
		private String StringToHex(String str)
		{
			Byte[] byteData = Encoding.Unicode.GetBytes(str);
			return BitConverter.ToString(byteData, 0, Math.Min(byteData.Length, MAX_HEX_SOURCE_LENGTH)).Replace("-", String.Empty).ToLower();
		}

		// --------------------------------------------------------------------
		// タグの頭文字
		// --------------------------------------------------------------------
		private String TagHead(TTag tag)
		{
			return !String.IsNullOrEmpty(tag.Ruby) ? YlCommon.Head(tag.Ruby) : YlCommon.Head(tag.Name);
		}

		// --------------------------------------------------------------------
		// タイアップグループ名の頭文字
		// --------------------------------------------------------------------
		private String TieUpGroupHead(TTieUpGroup tieUpGroup)
		{
			return !String.IsNullOrEmpty(tieUpGroup.Ruby) ? YlCommon.Head(tieUpGroup.Ruby) : YlCommon.Head(tieUpGroup.Name);
		}

		// --------------------------------------------------------------------
		// ゾーニング名称
		// --------------------------------------------------------------------
		private String ZoneName(Boolean isAdult)
		{
			return isAdult ? "アダルト " : "一般 ";
		}

		// --------------------------------------------------------------------
		// ゾーニングされたページ
		// --------------------------------------------------------------------
		private WebPageInfoTree ZonePage(Boolean isAdult)
		{
			Debug.Assert(_topPage != null, "ZonePage() mTopPage is null");
			return isAdult ? _topPage.Children[1] : _topPage.Children[0];
		}
	}
}
