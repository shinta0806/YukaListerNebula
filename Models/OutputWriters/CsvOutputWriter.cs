// ============================================================================
// 
// CSV リスト出力クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using YukaLister.Models.Database;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.OutputWriters
{
	public class CsvOutputWriter : OutputWriter
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public CsvOutputWriter()
		{
			// プロパティー
			FormatName = "CSV";
			TopFileName = "List.csv";
			//OutputSettings = new CsvOutputSettings();
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リスト出力
		// --------------------------------------------------------------------
		public override void Output()
		{
			StringBuilder stringBuilder = new();
			PrepareOutput();

			// ヘッダー
			stringBuilder.Append("No.");
			foreach (OutputItems outputItem in _runtimeOutputItems)
			{
				switch (outputItem)
				{
					case OutputItems.SmartTrack:
						stringBuilder.Append(",On,Off");
						break;
					case OutputItems.LastWriteTime:
						stringBuilder.Append(",最終更新日,最終更新時刻");
						break;
					default:
						stringBuilder.Append("," + YlConstants.OUTPUT_ITEM_NAMES[(Int32)outputItem]);
						break;
				}
			}
			stringBuilder.Append('\n');

			IQueryable<TFound> queryResult = _listContextInMemory.Founds.OrderBy(x => x.Category).ThenBy(x => x.Head).ThenBy(x => x.TieUpRuby).ThenBy(x => x.TieUpName).ThenBy(x => x.SongRuby).ThenBy(x => x.SongName);

			// コンテンツ
			Int32 index = 1;
			foreach (TFound found in queryResult)
			{
				stringBuilder.Append(index);
				foreach (OutputItems outputItem in _runtimeOutputItems)
				{
					switch (outputItem)
					{
						case OutputItems.Path:
							stringBuilder.Append(",\"" + found.Path + "\"");
							break;
						case OutputItems.FileName:
							stringBuilder.Append(",\"" + Path.GetFileName(found.Path) + "\"");
							break;
						case OutputItems.Head:
							stringBuilder.Append(",\"" + found.Head + "\"");
							break;
						case OutputItems.Worker:
							stringBuilder.Append(",\"" + found.Worker + "\"");
							break;
						case OutputItems.Track:
							stringBuilder.Append(",\"" + found.Track + "\"");
							break;
						case OutputItems.SmartTrack:
							stringBuilder.Append(",\"" + (found.SmartTrackOnVocal ? YlConstants.SMART_TRACK_VALID_MARK : null) + "\"");
							stringBuilder.Append(",\"" + (found.SmartTrackOffVocal ? YlConstants.SMART_TRACK_VALID_MARK : null) + "\"");
							break;
						case OutputItems.Comment:
							stringBuilder.Append(",\"" + found.Comment + "\"");
							break;
						case OutputItems.LastWriteTime:
							stringBuilder.Append("," + JulianDay.ModifiedJulianDateToDateTime(found.LastWriteTime).ToString(YlConstants.DATE_FORMAT));
							stringBuilder.Append("," + JulianDay.ModifiedJulianDateToDateTime(found.LastWriteTime).ToString(YlConstants.TIME_FORMAT));
							break;
						case OutputItems.FileSize:
							stringBuilder.Append("," + found.FileSize);
							break;
						case OutputItems.SongName:
							stringBuilder.Append(",\"" + found.SongName + "\"");
							break;
						case OutputItems.SongRuby:
							stringBuilder.Append(",\"" + found.SongRuby + "\"");
							break;
						case OutputItems.SongOpEd:
							stringBuilder.Append(",\"" + found.SongOpEd + "\"");
							break;
						case OutputItems.SongReleaseDate:
							if (found.SongReleaseDate <= YlConstants.INVALID_MJD)
							{
								stringBuilder.Append(',');
							}
							else
							{
								stringBuilder.Append("," + JulianDay.ModifiedJulianDateToDateTime(found.SongReleaseDate).ToString(YlConstants.DATE_FORMAT));
							}
							break;
						case OutputItems.ArtistName:
							stringBuilder.Append(",\"" + found.ArtistName + "\"");
							break;
						case OutputItems.ArtistRuby:
							stringBuilder.Append(",\"" + found.ArtistRuby + "\"");
							break;
						case OutputItems.LyristName:
							stringBuilder.Append(",\"" + found.LyristName + "\"");
							break;
						case OutputItems.LyristRuby:
							stringBuilder.Append(",\"" + found.LyristRuby + "\"");
							break;
						case OutputItems.ComposerName:
							stringBuilder.Append(",\"" + found.ComposerName + "\"");
							break;
						case OutputItems.ComposerRuby:
							stringBuilder.Append(",\"" + found.ComposerRuby + "\"");
							break;
						case OutputItems.ArrangerName:
							stringBuilder.Append(",\"" + found.ArrangerName + "\"");
							break;
						case OutputItems.ArrangerRuby:
							stringBuilder.Append(",\"" + found.ArrangerRuby + "\"");
							break;
						case OutputItems.TieUpName:
							stringBuilder.Append(",\"" + found.TieUpName + "\"");
							break;
						case OutputItems.TieUpRuby:
							stringBuilder.Append(",\"" + found.TieUpRuby + "\"");
							break;
						case OutputItems.TieUpAgeLimit:
							stringBuilder.Append(",\"" + found.TieUpAgeLimit + "\"");
							break;
						case OutputItems.Category:
							stringBuilder.Append(",\"" + found.Category + "\"");
							break;
						case OutputItems.TieUpGroupName:
							stringBuilder.Append(",\"" + found.TieUpGroupName + "\"");
							break;
						case OutputItems.TieUpGroupRuby:
							stringBuilder.Append(",\"" + found.TieUpGroupRuby + "\"");
							break;
						case OutputItems.MakerName:
							stringBuilder.Append(",\"" + found.MakerName + "\"");
							break;
						case OutputItems.MakerRuby:
							stringBuilder.Append(",\"" + found.MakerRuby + "\"");
							break;
						default:
							Debug.Assert(false, "Output() bad aOutputItem");
							break;
					}

				}
				stringBuilder.Append('\n');

				index++;

			}
			File.WriteAllText(_folderPath + TopFileName, stringBuilder.ToString(), Encoding.UTF8);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 出力設定を生成
		// --------------------------------------------------------------------
		protected override void GenerateOutputSettingsCore()
		{
			OutputSettings = new CsvOutputSettings();
		}

		// --------------------------------------------------------------------
		// コンストラクターでは行えない準備などを実施
		// --------------------------------------------------------------------
		protected override void PrepareOutput()
		{
			base.PrepareOutput();

			// 出力先フォルダー
			SetFolderPathByYlSettings();
		}
	}
}
