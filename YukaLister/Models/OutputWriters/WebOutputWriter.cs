﻿// ============================================================================
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
using System.Security.Cryptography;
using System.Text;

using YukaLister.Models.Database;
using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.OutputSettingsWindowViewModels;

namespace YukaLister.Models.OutputWriters
{
	internal abstract class WebOutputWriter : OutputWriter
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public WebOutputWriter(String listExt)
		{
			_listExt = listExt;
			_md5 = MD5.Create();

			// テーブル項目名（原則 YlCommon.OUTPUT_ITEM_NAMES だが一部見やすいよう変更）
			_thNames = new(YlConstants.OUTPUT_ITEM_NAMES);
			_thNames[(Int32)OutputItems.Worker] = "制作";
			_thNames[(Int32)OutputItems.SmartTrack] = "On</th><th>Off";
			_thNames[(Int32)OutputItems.FileSize] = "サイズ";

			Debug.WriteLine("WebOutputWriter() HashSize: " + _md5.HashSize);
			Debug.Assert(KIND_FILE_NAMES.Length == (Int32)GroupNaviItems.__End__, "WebOutputWriter() bad KIND_FILE_NAMES.Length");
			Debug.Assert(GROUP_NAMES.Length == (Int32)GroupNaviItems.__End__, "WebOutputWriter() bad GROUP_NAMES.Length");
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リスト出力設定ウィンドウの ViewModel を生成
		// --------------------------------------------------------------------
		public override OutputSettingsWindowViewModel CreateOutputSettingsWindowViewModel()
		{
			return new WebOutputSettingsWindowViewModel(this);
		}

		// --------------------------------------------------------------------
		// リスト出力
		// --------------------------------------------------------------------
		public override void Output()
		{
			PrepareOutput();

			// 内容の生成
			// 生成の順番は GroupNaviSequence と合わせる
			WebOutputSettings webOutputSettings = (WebOutputSettings)OutputSettings;
			for (Int32 i = 0; i < webOutputSettings.GroupNaviSequence.Count; i++)
			{
				switch (webOutputSettings.GroupNaviSequence[i])
				{
					case GroupNaviItems.New:
						GenerateNew();
						break;
					case GroupNaviItems.Category:
						GenerateCategoryAndHeads();
						break;
					case GroupNaviItems.TieUpGroup:
						GenerateTieUpGroupHeadAndTieUpGroups();
						break;
					case GroupNaviItems.SeasonAndPeriod:
						GenerateYearsAndSeasons();
						GeneratePeriodAndHeads();
						break;
					case GroupNaviItems.ArtistAndComposer:
						GenerateArtistAndHeads();
						GenerateComposerAndHeads();
						break;
					case GroupNaviItems.Tag:
						GenerateTagHeadAndTags();
						break;
					default:
						Debug.Assert(false, "Output() bad GroupNaviSequence");
						break;
				}
			}

			// 内容の調整
			AdjustList(_topPage);

			// 一時フォルダーへの出力
			OutputList(_topPage);

			// インデックス系を「更新中」表示にする
			OutputNoticeIndexes();

			// 古いファイルを削除
			DeleteOldListContents();

			// 出力先フォルダーへの出力
			OutputCss();
			OutputJs();
			OutputImage();

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
		// protected 変数
		// ====================================================================

		// リストの拡張子（ピリオド含む）
		protected String _listExt;

		// トップページ
		protected WebPageInfoTree _topPage = new();

		// 追加説明（派生クラスごとに設定）
		protected String? _additionalDescription;

		// 追加 HTML ヘッダー
		protected String? _additionalHeader;

		// 追加階層ナビゲーション
		protected String? _additionalNavi;

		// トップページからリストをリンクする際の引数
		protected String? _listLinkArg;

		// ====================================================================
		// protected 関数
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
						String? comment = null;
						if (!String.IsNullOrEmpty(found.Comment))
						{
							Int32 ignorePos = found.Comment.IndexOf(YlConstants.WEB_LIST_IGNORE_COMMENT_DELIMITER);
							if (ignorePos >= 0)
							{
								comment = found.Comment[0..ignorePos];
							}
							else
							{
								comment = found.Comment;
							}
						}
						stringBuilder.Append("<td class=\"small\">" + comment + "</td>");
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
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			if (_isDisposed)
			{
				return;
			}

			// マネージドリソース解放
			if (isDisposing)
			{
				_md5.Dispose();
			}

			// アンマネージドリソース解放
			// 今のところ無し
			// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

			// 解放完了
			_isDisposed = true;
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
			_tempFolderPath = Common.TempPath() + "\\";
			Directory.CreateDirectory(_tempFolderPath);
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// リストファイル名の先頭文字列（カテゴリーインデックス以外）
		private const String FILE_NAME_PREFIX = "List";

		// Fantial ロゴ画像
		private const String FILE_NAME_BODY_FANTIA_LOGO = "FantiaLogo";

		// リストの種類に応じたファイル名
		private const String KIND_FILE_NAME_ARTIST = "Artist";
		private const String KIND_FILE_NAME_CATEGORY = "Category";
		private const String KIND_FILE_NAME_COMPOSER = "Composer";
		private const String KIND_FILE_NAME_NEW = "New";
		private const String KIND_FILE_NAME_PERIOD = "Period";
		private const String KIND_FILE_NAME_SEASON = "Season";
		private const String KIND_FILE_NAME_TAG = "Tag";
		private const String KIND_FILE_NAME_TIE_UP_GROUP = "Series";

		// リストの種類に応じたファイル名（GroupNaviItems 順、結合アイテムを除く）
		private static readonly String[] KIND_FILE_NAMES =
		{
			KIND_FILE_NAME_NEW, KIND_FILE_NAME_CATEGORY, KIND_FILE_NAME_TIE_UP_GROUP, String.Empty, String.Empty, KIND_FILE_NAME_TAG,
		};

		// リストの種類の名前（GroupNaviItems 順、結合アイテムを除く）
		private static readonly String[] GROUP_NAMES =
		{
			YlConstants.GROUP_NAME_NEW,
			YlConstants.GROUP_NAME_CATEGORY,
			YlConstants.GROUP_NAME_TIE_UP_GROUP,
			String.Empty,
			String.Empty,
			YlConstants.GROUP_NAME_TAG,
		};

		// HTML テンプレートに記載されている変数
		private const String HTML_VAR_ADDITIONAL_DESCRIPTION = "<!-- $AdditionalDescription$ -->";
		private const String HTML_VAR_ADDITIONAL_HEADER = "<!-- $AdditionalHeader$ -->";
		private const String HTML_VAR_ADDITIONAL_NOTICE = "<!-- $AdditionalNotice$ -->";
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
		//private const String HTML_VAR_NEW = "<!-- $New$ -->";
		private const String HTML_VAR_NUM_SONGS = "<!-- $NumSongs$ -->";
		private const String HTML_VAR_PAGES = "<!-- $Pages$ -->";
		private const String HTML_VAR_PROGRAMS = "<!-- $Programs$ -->";
		private const String HTML_VAR_SUPPORT = "<!-- $Support$ -->";
		private const String HTML_VAR_TITLE = "<!-- $Title$ -->";

		// テーブル非表示
		private const String CLASS_NAME_INVISIBLE = "class=\"invisible\"";

		// 期別リストの年数
		private const Int32 SEASON_YEARS = 5;

		// MD5 文字列長
		private const Int32 MD5_STRING_LENGTH = 32;

		// 開発者支援サイトリンク
		private const String SUPPORT_LINK = "<a href=\"" + YlConstants.URL_FANTIA + "\" target=\"_blank\"><img src=\""
				+ FILE_NAME_BODY_FANTIA_LOGO + Common.FILE_EXT_PNG + "\" height=\"20\">開発者支援サイト</a>";

		// ====================================================================
		// private 変数
		// ====================================================================

		// テーブルに表示する項目名
		private readonly List<String> _thNames;

		// カテゴリーの順番
		private readonly Dictionary<String, Int32> _categoryOrders = new();

		// リストを一時的に出力するフォルダー（末尾 '\\'）
		private String? _tempFolderPath;

		// MD5 生成
		private readonly MD5 _md5;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private 関数
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
			ReplaceListContent(pageInfoTree, HTML_VAR_SUPPORT, SUPPORT_LINK);

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
				pageInfoTree.Content = pageInfoTree.Content.Replace(HTML_VAR_TITLE, pageInfoTree.DirectoryText());
				pageInfoTree.Content = pageInfoTree.Content.Replace(HTML_VAR_DIRECTORY, pageInfoTree.DirectoryLink(_listLinkArg));
				pageInfoTree.Content = pageInfoTree.Content.Replace(HTML_VAR_NUM_SONGS, pageInfoTree.NumTotalSongs.ToString("#,0"));

				// 隣のページ
				if (pageInfoTree.Parent != null && pageInfoTree.Parent.Children.Count > 1)
				{
					List<WebPageInfoTree> children = pageInfoTree.Parent.Children;
					Int32 index = children.IndexOf(pageInfoTree);
					StringBuilder stringBuilder = new();
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
		private void AppendSongInfo(StringBuilder stringBuilder, OutputItems chapterItem, TFound found)
		{
			stringBuilder.Append("  <tr>\n    ");
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
			if (chapterItem == OutputItems.TieUpName && founds[0].TieUpId != null)
			{
				// 章の区切りがタイアップ名の場合、シリーズがあるなら記載する
				List<TTieUpGroup> tieUpGroups = DbCommon.SelectSequencedTieUpGroupsByTieUpId(_tieUpGroupSequencesInMemory, _tieUpGroupsInMemory, founds[0].TieUpId!);
				foreach (TTieUpGroup tieUpGroup in tieUpGroups)
				{
					// 「ひらがな以外の頭文字を「その他」として出力する」がオフの場合は記載されないこともある
					if (((WebOutputSettings)OutputSettings).OutputHeadMisc || tieUpGroup.Ruby != null)
					{
						stringBuilder.Append("　<a class=\"series\" href=\"");
						stringBuilder.Append(OutputFileName(founds[0].TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z, KIND_FILE_NAME_TIE_UP_GROUP,
								TieUpGroupHead(tieUpGroup), tieUpGroup.Name + YlConstants.TIE_UP_GROUP_SUFFIX) + _listLinkArg);
						stringBuilder.Append("\">" + tieUpGroup.Name + YlConstants.TIE_UP_GROUP_SUFFIX + "</a>");
					}
				}
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
		private static String ChapterValue(OutputItems chapterItem, TFound found)
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
			Debug.Assert(!String.IsNullOrEmpty(_folderPath), "DeleteOldListContentsCore() FolderPath is null");
			String[] listPathes = Directory.GetFiles(_folderPath, FILE_NAME_PREFIX + "_" + kindFileName + "_*" + _listExt);

			foreach (String path in listPathes)
			{
				try
				{
					File.Delete(path);
				}
				catch (Exception)
				{
					YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "古いリストファイル " + Path.GetFileName(path) + " を削除できませんでした。");
				}
			}
		}

		// --------------------------------------------------------------------
		// 章を終了する
		// --------------------------------------------------------------------
		private static void EndChapter(StringBuilder stringBuilder)
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
			pageInfoTree.Name = YlConstants.GROUP_NAME_ARTIST;
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
			pageInfoTree.Name = YlConstants.GROUP_NAME_CATEGORY;
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_CATEGORY);

			IQueryable<TFound> queryResult = _listContextInMemory.Founds.Where(x => x.TieUpName != null && (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z))
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

			// カテゴリーソート
			SortCategory(pageInfoTree);

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
			pageInfoTree.Name = YlConstants.GROUP_NAME_COMPOSER;
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
				if (indexPage.Children[i].Name == YlConstants.HEAD_MISC)
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
			pageInfoTree.Name = YlConstants.GROUP_NAME_NEW;
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_NEW);

			// 番組名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// 新着とする日付
			Int32 deltaDate = 0;
			deltaDate = -((WebOutputSettings)OutputSettings).NewDays;
			Double newDate = JulianDay.DateTimeToModifiedJulianDate(DateTime.Now.AddDays(deltaDate));

			IQueryable<TFound> queryResult = _listContextInMemory.Founds.Where(x => x.TieUpName != null && x.LastWriteTime >= newDate && (isAdult ? x.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z)).
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
					prevTFound = null;
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
						AppendSongInfo(stringBuilder, chapterItem, list[i]);
					}
					EndChapter(stringBuilder);

					numPageSongs += list.Count;
					chapterIndex++;
				}
			}
			catch (Exception excep)
			{
				// METEOR チケット #190 エラー捕捉用
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "GenerateOneList() リスト本体部分 Exception: " + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "GenerateOneList() oIsAdult: " + isAdult.ToString() + ", oKindFileName: " + kindFileName
						+ ", oGroupName: " + groupName + ", oPageName: " + pageName);
			}

			// テンプレート適用
			try
			{
				// 各頭文字専用の追加案内
				String? additionalNode = pageName switch
				{
					"う" => "「ヴァ（バ）」「ヴィ（ビ）」「ヴ（ブ）」「ヴェ（ベ）」「ヴォ（ボ）」から始まる" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem]
							+ "一覧については、「は」「ひ」「ふ」「へ」「ほ」をご覧ください。<br>",
					"は" => "「ヴァ（バ）」から始まる" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem] + "一覧もここに掲載されています。<br>",
					"ひ" => "「ヴィ（ビ）」から始まる" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem] + "一覧もここに掲載されています。<br>",
					"ふ" => "「ヴ（ブ）」から始まる" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem] + "一覧もここに掲載されています。<br>",
					"へ" => "「ヴェ（ベ）」から始まる" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem] + "一覧もここに掲載されています。<br>",
					"ほ" => "「ヴォ（ボ）」から始まる" + YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem] + "一覧もここに掲載されています。<br>",
					_ => null,
				};
				if (isAdult && kindFileName == KIND_FILE_NAME_TIE_UP_GROUP)
				{
					additionalNode += "成人向けタイアップと一般タイアップの両方を掲載しています。<br>";
				}
				if (additionalNode != null)
				{
					template = template.Replace(HTML_VAR_ADDITIONAL_NOTICE, additionalNode);
				}

				template = template.Replace(HTML_VAR_ADDITIONAL_DESCRIPTION, _additionalDescription);
				template = template.Replace(HTML_VAR_CHAPTER_NAME, YlConstants.OUTPUT_ITEM_NAMES[(Int32)chapterItem]);
				template = template.Replace(HTML_VAR_PROGRAMS, stringBuilder.ToString());
			}
			catch (Exception excep)
			{
				// METEOR チケット #190 エラー捕捉用
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "GenerateOneList() テンプレート適用部分 Exception: " + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "GenerateOneList() oIsAdult: " + isAdult.ToString() + ", oKindFileName: " + kindFileName
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
				group = new();
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
			pageInfoTree.Name = YlConstants.GROUP_NAME_PERIOD;
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_PERIOD);

			// 番組名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// 年月日設定
			Int32 sinceYear = DateTime.UtcNow.Year;
			sinceYear -= sinceYear % 10;

			while (sinceYear > YlConstants.INVALID_YEAR)
			{
				Int32 untilYear = sinceYear + 10;

				IQueryable<TFound> queryResult = _listContextInMemory.Founds.Where(x => x.TieUpName != null
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
						prevTFound = null;
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
			pageInfoTree.Name = YlConstants.GROUP_NAME_TAG;
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_TAG);

			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			var joined = _listContextInMemory.Founds.Join(_tagSequencesInMemory, f => f.SongId, s => s.Id, (f, s) => new
			{
				found = f,
				sequence = s,
			}).Where(x => !x.sequence.Invalid)
			.Join(_tagsInMemory, m => m.sequence.LinkId, t => t.Id, (m, t) => new
			{
				Found = m.found,
				Tag = t,
			})
			.Where(x => x.Found.TieUpName != null && x.Found.SongId != null
					&& (((WebOutputSettings)OutputSettings).OutputHeadMisc || x.Tag.Ruby != null)
					&& (isAdult ? x.Found.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.Found.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z))
			.OrderBy(x => x.Tag.Ruby).ThenBy(x => x.Tag.Name).ThenBy(x => x.Found.Head).ThenBy(x => x.Found.TieUpRuby).
					ThenBy(x => x.Found.TieUpName).ThenBy(x => x.Found.SongRuby).ThenBy(x => x.Found.SongName).ToList();

			List<QrFoundAndTag> queryResult = new(joined.Count);
			foreach (var join in joined)
			{
				queryResult.Add(new QrFoundAndTag(join.Found, join.Tag));
			}

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
		// ToDo: GenerateTagHeadAndTagsCore() などと統合できるのではないか（Where が TieUpId だから難しい？）
		// --------------------------------------------------------------------
		private WebPageInfoTree GenerateTieUpGroupHeadAndTieUpGroupsCore(Boolean isAdult)
		{
			WebPageInfoTree pageInfoTree = new();
			pageInfoTree.Name = YlConstants.GROUP_NAME_TIE_UP_GROUP;
			pageInfoTree.FileName = IndexFileName(isAdult, KIND_FILE_NAME_TIE_UP_GROUP);

			// タイアップ名とそれに紐付く楽曲群
			Dictionary<String, List<TFound>> tieUpNamesAndTFounds = new();

			// シリーズ別リストに限り、成人向けのリストに一般向けも含む（シリーズに含まれる全てをここを起点に一覧できるようにするため）
			var joined = _listContextInMemory.Founds.Join(_tieUpGroupSequencesInMemory, f => f.TieUpId, s => s.Id, (f, s) => new
			{
				found = f,
				sequence = s,
			}).Where(x => !x.sequence.Invalid)
			.Join(_tieUpGroupsInMemory, m => m.sequence.LinkId, g => g.Id, (m, g) => new
			{
				Found = m.found,
				TieUpGroup = g,
			})
			.Where(x => x.Found.TieUpId != null && x.Found.SongId != null
					&& (((WebOutputSettings)OutputSettings).OutputHeadMisc || x.TieUpGroup.Ruby != null)
					&& (isAdult || x.Found.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z))
					.OrderBy(x => x.TieUpGroup.Ruby).ThenBy(x => x.TieUpGroup.Name).ThenBy(x => x.Found.Head).ThenBy(x => x.Found.TieUpRuby).
					ThenBy(x => x.Found.TieUpName).ThenBy(x => x.Found.SongRuby).ThenBy(x => x.Found.SongName).ToList();

			List<QrFoundAndTieUpGroup> queryResult = new(joined.Count);
			foreach (var join in joined)
			{
				queryResult.Add(new QrFoundAndTieUpGroup(join.Found, join.TieUpGroup));
			}

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
			pageInfoTree.Name = YlConstants.GROUP_NAME_SEASON;
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

			IQueryable<TFound> queryResult = _listContextInMemory.Founds.Where(x => x.TieUpName != null
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
			// 2 つめの JOIN で new QrFoundAndPerson() すると実行時エラーとなる
			// https://docs.microsoft.com/ja-jp/ef/core/querying/client-eval
			// いったん無名を生成してから後で QrFoundAndPerson に格納する
			var joined = _listContextInMemory.Founds.Join(records, f => f.SongId, r => r.Id, (f, r) => new
			{
				found = f,
				sequence = r,
			}).Where(x => !x.sequence.Invalid)
			.Join(_peopleInMemory, m => m.sequence.LinkId, p => p.Id, (m, p) => new
			{
				Found = m.found,
				Person = p,
			})
			.Where(x => x.Found.TieUpName != null && x.Found.SongId != null
					&& (((WebOutputSettings)OutputSettings).OutputHeadMisc || x.Person.Ruby != null)
					&& (isAdult ? x.Found.TieUpAgeLimit >= YlConstants.AGE_LIMIT_CERO_Z : x.Found.TieUpAgeLimit < YlConstants.AGE_LIMIT_CERO_Z))
			.OrderBy(x => x.Person.Ruby).ThenBy(x => x.Person.Name).ThenBy(x => x.Found.Head).ThenBy(x => x.Found.TieUpRuby).
					ThenBy(x => x.Found.TieUpName).ThenBy(x => x.Found.SongRuby).ThenBy(x => x.Found.SongName).ToList();

			List<QrFoundAndPerson> queryResult = new(joined.Count);
			foreach (var join in joined)
			{
				queryResult.Add(new QrFoundAndPerson(join.Found, join.Person));
			}

			return queryResult;
		}

		// --------------------------------------------------------------------
		// グループナビ文字列を生成
		// --------------------------------------------------------------------
		private String GroupNavi(Boolean enableNew)
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append("<table>\n");
			GroupNaviCore(stringBuilder, false, enableNew);
			GroupNaviCore(stringBuilder, true, enableNew);
			stringBuilder.Append("</table>\n");
			return stringBuilder.ToString();
		}

		// --------------------------------------------------------------------
		// グループナビ文字列を追加
		// --------------------------------------------------------------------
		private void GroupNaviAppend(StringBuilder stringBuilder, String indexFileName, String groupName)
		{
			stringBuilder.Append("<td class=\"exist\"><a href=\"" + indexFileName + _listLinkArg + "\">　" + groupName + "　</a></td>");
		}

		// --------------------------------------------------------------------
		// グループナビ文字列を生成
		// ナビの順番は GroupNaviSequence と合わせる
		// --------------------------------------------------------------------
		private void GroupNaviCore(StringBuilder stringBuilder, Boolean isAdult, Boolean enableNew)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>　" + ZoneName(isAdult) + "　</td>");
			WebOutputSettings webOutputSettings = (WebOutputSettings)OutputSettings;

			// 新着は指定されている場合のみ
			if (enableNew)
			{
				Debug.Assert(webOutputSettings.GroupNaviSequence[0] == GroupNaviItems.New, "GroupNaviCore() bad GroupNaviSequence[0]");
				GroupNaviAppend(stringBuilder, IndexFileName(isAdult, KIND_FILE_NAME_NEW), YlConstants.GROUP_NAME_NEW);
			}

			// それ以降を指定の順で
			for (Int32 i = 1; i < webOutputSettings.GroupNaviSequence.Count; i++)
			{
				Int32 groupNavi = (Int32)webOutputSettings.GroupNaviSequence[i];
				switch (groupNavi)
				{
					case (Int32)GroupNaviItems.SeasonAndPeriod:
						GroupNaviAppend(stringBuilder, IndexFileName(isAdult, KIND_FILE_NAME_SEASON), YlConstants.GROUP_NAME_SEASON);
						GroupNaviAppend(stringBuilder, IndexFileName(isAdult, KIND_FILE_NAME_PERIOD), YlConstants.GROUP_NAME_PERIOD);
						break;
					case (Int32)GroupNaviItems.ArtistAndComposer:
						GroupNaviAppend(stringBuilder, IndexFileName(isAdult, KIND_FILE_NAME_ARTIST), YlConstants.GROUP_NAME_ARTIST);
						GroupNaviAppend(stringBuilder, IndexFileName(isAdult, KIND_FILE_NAME_COMPOSER), YlConstants.GROUP_NAME_COMPOSER);
						break;
					default:
						GroupNaviAppend(stringBuilder, IndexFileName(isAdult, KIND_FILE_NAMES[groupNavi]), GROUP_NAMES[groupNavi]);
						break;
				}
			}

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
		// listerdb_config.ini で指定されているカテゴリー順を読み込む
		// --------------------------------------------------------------------
		private void LoadCategoryOrders()
		{
			if (_categoryOrders.Any())
			{
				// 読み込み済の場合はスキップ
				return;
			}

			if (!YlModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				return;
			}
			if (!File.Exists(YlModel.Instance.EnvModel.YlSettings.YukariListerDbConfigPath()))
			{
				return;
			}

			String[] config = File.ReadAllLines(YlModel.Instance.EnvModel.YlSettings.YukariListerDbConfigPath(), Encoding.UTF8);
			for (Int32 line = 0; line < config.Length; line++)
			{
				Int32 eqPos = config[line].IndexOf('=');
				if (eqPos < 0)
				{
					continue;
				}
				_categoryOrders[config[line][(eqPos + 1)..].Trim()] = line;
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
					File.Move(path, _folderPath + Path.GetFileName(path));
				}
				catch (Exception)
				{
					YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "リストファイル " + Path.GetFileName(path) + " を移動できませんでした。");
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
				File.Copy(_tempFolderPath + indexFileName, _folderPath + indexFileName, true);
				File.Delete(_tempFolderPath + indexFileName);
			}
			catch (Exception)
			{
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "リストファイル " + Path.GetFileName(indexFileName) + " を移動できませんでした。");
			}
		}

		// --------------------------------------------------------------------
		// CSS を出力
		// HtmlCss テンプレートは WebServer でも使用するので内容を変更せずに出力する前提
		// --------------------------------------------------------------------
		private void OutputCss()
		{
			String template = LoadTemplate("HtmlCss");
			File.WriteAllText(_folderPath + "List.css", template, Encoding.UTF8);
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
		// 画像を出力
		// --------------------------------------------------------------------
		private void OutputImage()
		{
			File.Copy(YlModel.Instance.EnvModel.ExeFullFolder + YlConstants.FOLDER_NAME_TEMPLATES + FILE_NAME_BODY_FANTIA_LOGO + Common.FILE_EXT_TPL,
					_folderPath + FILE_NAME_BODY_FANTIA_LOGO + Common.FILE_EXT_PNG, true);
		}

		// --------------------------------------------------------------------
		// JS を出力
		// --------------------------------------------------------------------
		private void OutputJs()
		{
			String template = LoadTemplate("HtmlJs");
			File.WriteAllText(_folderPath + "List.js", template, Encoding.UTF8);
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
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_NEW), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_NEW), topTemplate, Encoding.UTF8);

			// インデックス系
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_CATEGORY), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_CATEGORY), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_TIE_UP_GROUP), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_TIE_UP_GROUP), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_PERIOD), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_PERIOD), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_SEASON), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_SEASON), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_ARTIST), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_ARTIST), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(false, KIND_FILE_NAME_COMPOSER), topTemplate, Encoding.UTF8);
			File.WriteAllText(_folderPath + IndexFileName(true, KIND_FILE_NAME_COMPOSER), topTemplate, Encoding.UTF8);
		}

		// --------------------------------------------------------------------
		// 人物の頭文字
		// --------------------------------------------------------------------
		private static String PersonHead(TPerson person)
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
		// listerdb_config.ini に従ってカテゴリーの順番をソート
		// https://github.com/bee7813993/KaraokeRequestorWeb/commit/e7076e87554a9df9496419fbd11719757a329a23
		// --------------------------------------------------------------------
		private void SortCategory(WebPageInfoTree pageInfoTree)
		{
			try
			{
				LoadCategoryOrders();
				if (!_categoryOrders.Any())
				{
					return;
				}
				pageInfoTree.Children.Sort(SortCategoryComparer);
			}
			catch (Exception excep)
			{
				YlModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "カテゴリーソートエラー：\n" + excep.Message);
				YlModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// ソート比較
		// --------------------------------------------------------------------
		private Int32 SortCategoryComparer(WebPageInfoTree lhs, WebPageInfoTree rhs)
		{
			Int32 lhsIndex = -1;
			if (_categoryOrders.ContainsKey(lhs.Name))
			{
				lhsIndex = _categoryOrders[lhs.Name];
			}
			Int32 rhsIndex = -1;
			if (_categoryOrders.ContainsKey(rhs.Name))
			{
				rhsIndex = _categoryOrders[rhs.Name];
			}

			if (lhsIndex >= 0 && rhsIndex >= 0)
			{
				// 両方 listerdb_config.ini で指定されている
				return lhsIndex - rhsIndex;
			}
			if (lhsIndex < 0 && rhsIndex < 0)
			{
				// どちらも listerdb_config.ini で指定されていない
				return String.Compare(lhs.Name, rhs.Name, StringComparison.OrdinalIgnoreCase);
			}
			if (lhsIndex >= 0)
			{
				// lhs のみ listerdb_config.ini で指定されている
				return -1;
			}
			// rhs のみ listerdb_config.ini で指定されている
			return 1;
		}

		// --------------------------------------------------------------------
		// 文字列を MD5_STRING_LENGTH 文字以下の HEX 表記に変換
		// --------------------------------------------------------------------
		private String StringToHex(String str)
		{
			Byte[] byteData;
			if (str.Length * 4 < MD5_STRING_LENGTH)
			{
				// Unicode HEX 表記にすると 1 文字は 4 文字になる。それが MD5_STRING_LENGTH 文字未満であれば、それを返す
				byteData = Encoding.Unicode.GetBytes(str);
			}
			else
			{
				// MD5 ハッシュを返す
				byteData = _md5.ComputeHash(Encoding.Unicode.GetBytes(str));
			}
			return BitConverter.ToString(byteData).Replace("-", String.Empty).ToLower();
		}

		// --------------------------------------------------------------------
		// タグの頭文字
		// --------------------------------------------------------------------
		private static String TagHead(TTag tag)
		{
			return !String.IsNullOrEmpty(tag.Ruby) ? YlCommon.Head(tag.Ruby) : YlCommon.Head(tag.Name);
		}

		// --------------------------------------------------------------------
		// タイアップグループ名の頭文字
		// --------------------------------------------------------------------
		private static String TieUpGroupHead(TTieUpGroup tieUpGroup)
		{
			return !String.IsNullOrEmpty(tieUpGroup.Ruby) ? YlCommon.Head(tieUpGroup.Ruby) : YlCommon.Head(tieUpGroup.Name);
		}

		// --------------------------------------------------------------------
		// ゾーニング名称
		// --------------------------------------------------------------------
		private static String ZoneName(Boolean isAdult)
		{
			return isAdult ? "成人向け " : "一般 ";
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
