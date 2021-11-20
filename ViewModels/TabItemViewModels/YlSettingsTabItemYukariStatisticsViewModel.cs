// ============================================================================
// 
// ゆかり統計タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	public class YlSettingsTabItemYukariStatisticsViewModel : TabItemViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemYukariStatisticsViewModel(YlViewModel windowViewModel)
				: base(windowViewModel)
		{
			CompositeDisposable.Add(_semaphoreSlim);
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemYukariStatisticsViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 出力対象期間
		public List<Control> YukariStatisticsPeriodItems { get; set; } = new();

		// 選択された出力対象期間
		private Int32 _selectedYukariStatisticsPeriodIndex = -1;
		public Int32 SelectedYukariStatisticsPeriodIndex
		{
			get => _selectedYukariStatisticsPeriodIndex;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedYukariStatisticsPeriodIndex, value))
				{
					try
					{
						UpdateYukariStatisticsPeriodControls();
					}
					catch (Exception excep)
					{
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかり統計出力対象期間変更時エラー：\n" + excep.Message);
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
					}
				}
			}
		}

		// 出力対象期間指定有効化
		private Boolean _isCustomYukariStatisticsPeriodEnabled;
		public Boolean IsCustomYukariStatisticsPeriodEnabled
		{
			get => _isCustomYukariStatisticsPeriodEnabled;
			set => RaisePropertyChangedIfSet(ref _isCustomYukariStatisticsPeriodEnabled, value);
		}

		// 出力対象期間 From
		private DateTime? _yukariStatisticsPeriodFrom;
		public DateTime? YukariStatisticsPeriodFrom
		{
			get => _yukariStatisticsPeriodFrom;
			set => RaisePropertyChangedIfSet(ref _yukariStatisticsPeriodFrom, value);
		}

		// 出力対象期間 To
		private DateTime? _yukariStatisticsPeriodTo;
		public DateTime? YukariStatisticsPeriodTo
		{
			get => _yukariStatisticsPeriodTo;
			set => RaisePropertyChangedIfSet(ref _yukariStatisticsPeriodTo, value);
		}

		// 属性未確認の予約情報も出力する
		private Boolean _outputAttributesNone;
		public Boolean OutputAttributesNone
		{
			get => _outputAttributesNone;
			set => RaisePropertyChangedIfSet(ref _outputAttributesNone, value);
		}

		// ゆかり統計出力先
		private String? _yukariStatisticsPath;
		public String? YukariStatisticsPath
		{
			get => _yukariStatisticsPath;
			set => RaisePropertyChangedIfSet(ref _yukariStatisticsPath, value);
		}

		// ゆかり統計出力プログレスバー表示
		private Visibility _progressBarOutputYukariStatisticsVisibility;
		public Visibility ProgressBarOutputYukariStatisticsVisibility
		{
			get => _progressBarOutputYukariStatisticsVisibility;
			set
			{
				if (RaisePropertyChangedIfSet(ref _progressBarOutputYukariStatisticsVisibility, value))
				{
					ButtonOutputYukariStatisticsClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ゆかり統計出力先参照ボタンの制御
		private ViewModelCommand? _buttonBrowseYukariStatisticsFileClickedCommand;

		public ViewModelCommand ButtonBrowseYukariStatisticsFileClickedCommand
		{
			get
			{
				if (_buttonBrowseYukariStatisticsFileClickedCommand == null)
				{
					_buttonBrowseYukariStatisticsFileClickedCommand = new ViewModelCommand(ButtonBrowseYukariStatisticsFileClicked);
				}
				return _buttonBrowseYukariStatisticsFileClickedCommand;
			}
		}

		public void ButtonBrowseYukariStatisticsFileClicked()
		{
			try
			{
				String? path = _windowViewModel.PathBySavingDialog("ゆかり統計出力", YlConstants.DIALOG_FILTER_CSV, "YukariStatistics");
				if (path != null)
				{
					YukariStatisticsPath = path;
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "エクスポート先参照ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region ゆかり統計出力ボタンの制御
		private ViewModelCommand? _buttonOutputYukariStatisticsClickedCommand;

		public ViewModelCommand ButtonOutputYukariStatisticsClickedCommand
		{
			get
			{
				if (_buttonOutputYukariStatisticsClickedCommand == null)
				{
					_buttonOutputYukariStatisticsClickedCommand = new ViewModelCommand(ButtonOutputYukariStatisticsClicked, CanButtonOutputYukariStatisticsClicked);
				}
				return _buttonOutputYukariStatisticsClickedCommand;
			}
		}

		public Boolean CanButtonOutputYukariStatisticsClicked()
		{
			return ProgressBarOutputYukariStatisticsVisibility != Visibility.Visible;
		}

		public async void ButtonOutputYukariStatisticsClicked()
		{
			try
			{
				// 確認
				if (String.IsNullOrEmpty(YukariStatisticsPath))
				{
					throw new Exception("ゆかり統計出力先フォルダーを指定してください。");
				}

				// ウィンドウのキャンセルボタンが押された場合でも出力先は確定
				YukaListerModel.Instance.EnvModel.YlSettings.YukariStatisticsPath = YukariStatisticsPath;

				// 出力
				ProgressBarOutputYukariStatisticsVisibility = Visibility.Visible;
				Boolean result = await YlCommon.LaunchTaskAsync<Object?>(_semaphoreSlim, OutputYukariStatisticsByWorker, null, "ゆかり統計出力");
				if (!result)
				{
					return;
				}
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "ゆかり統計出力が完了しました。");

				// 表示
				try
				{
					YlCommon.ShellExecute(YukariStatisticsPath);
				}
				catch (Exception)
				{
					throw new Exception("出力先ファイルを開けませんでした。\n" + YukariStatisticsPath);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかり統計出力ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				ProgressBarOutputYukariStatisticsVisibility = Visibility.Hidden;
			}
		}
		#endregion

		#region すべて削除するボタンの制御
		private ViewModelCommand? _buttonDeleteAllYukariStatisticsClickedCommand;

		public ViewModelCommand ButtonDeleteAllYukariStatisticsClickedCommand
		{
			get
			{
				if (_buttonDeleteAllYukariStatisticsClickedCommand == null)
				{
					_buttonDeleteAllYukariStatisticsClickedCommand = new ViewModelCommand(ButtonDeleteAllYukariStatisticsClicked);
				}
				return _buttonDeleteAllYukariStatisticsClickedCommand;
			}
		}

		public static void ButtonDeleteAllYukariStatisticsClicked()
		{
			try
			{
				if (MessageBox.Show("ゆかり統計をすべて削除します。\n復活できません。\nすべて削除してよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					return;
				}

				using YukariStatisticsContext yukariStatisticsContext = YukariStatisticsContext.CreateContext(out DbSet<TProperty> _);
				yukariStatisticsContext.CreateDatabase();
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Information, "ゆかり統計データベースを削除しました。");
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "ゆかり統計すべて削除ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力された値が適正か確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public override void CheckInput()
		{
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			// ゆかり統計出力対象期間
			foreach (String label in YlConstants.YUKARI_STATISTICS_PERIOD_LABELS)
			{
				YlCommon.AddComboBoxItem(YukariStatisticsPeriodItems, label);
			}
			SelectedYukariStatisticsPeriodIndex = 0;

			// プログレスバー
			ProgressBarOutputYukariStatisticsVisibility = Visibility.Hidden;
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings()
		{
			YukaListerModel.Instance.EnvModel.YlSettings.YukariStatisticsPath = YukariStatisticsPath;
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties()
		{
			YukariStatisticsPath = YukaListerModel.Instance.EnvModel.YlSettings.YukariStatisticsPath;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// タスクが多重起動されるのを抑止する
		private readonly SemaphoreSlim _semaphoreSlim = new(1);

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 今月 1 日
		// --------------------------------------------------------------------
		private static DateTime CurrentMonth()
		{
			return DateTime.Today.AddDays(-(DateTime.Today.Day - 1));
		}

		// --------------------------------------------------------------------
		// 今年の 1 月 1 日
		// --------------------------------------------------------------------
		private static DateTime ThisYear()
		{
			return CurrentMonth().AddMonths(-(CurrentMonth().Month - 1));
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ゆかり統計出力処理
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		private Task OutputYukariStatisticsByWorker(Object? _)
		{
			// ゆかり統計の属性情報を最新化
			UpdateYukariStatistics();
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "予約当時の動画ファイルが現存しているものについて、可能な限り、ゆかり統計の属性情報を最新化しました。");

			// タイトル行
			List<String> titleColumns = new(new String[] { "No", "PC", "予約日", "ルーム名", "カテゴリー", "タイアップ名", "摘要", "年齢制限", "リリース日", "リリース年", "シリーズ", "制作会社名",
					"楽曲名", "歌手名", "作詞者", "作曲者", "編曲者", "ファイル", "動画制作者" });

			// 出力対象期間
			Double periodFrom = YukariStatisticsPeriodFrom == null ? 0.0 : JulianDay.DateTimeToModifiedJulianDate(YukariStatisticsPeriodFrom.Value.ToUniversalTime());
			Double periodTo = JulianDay.DateTimeToModifiedJulianDate((YukariStatisticsPeriodTo == null ? DateTime.Today : YukariStatisticsPeriodTo.Value).AddDays(1).ToUniversalTime());

			// 内容
			using YukariStatisticsContext yukariStatisticsContext = YukariStatisticsContext.CreateContext(out DbSet<TYukariStatistics> yukariStatistics);
			yukariStatisticsContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			List<TYukariStatistics> targetStatistics = yukariStatistics.
					Where(x => periodFrom <= x.RequestTime && x.RequestTime < periodTo && (OutputAttributesNone || x.AttributesDone) && !x.Invalid).ToList();

			if (targetStatistics.Count == 0)
			{
				throw new Exception("対象となる予約がありませんでした。");
			}

			List<List<String>> contents = new(targetStatistics.Count);
			Int32 index = 1;
			foreach (TYukariStatistics yukariStatisticsRecord in targetStatistics)
			{
				List<String> line = new(titleColumns.Count);
				line.Add(index.ToString());
				index++;

				// 会の情報
				line.Add(yukariStatisticsRecord.Id[0..yukariStatisticsRecord.Id.IndexOf('_')]);
				line.Add(JulianDay.ModifiedJulianDateToDateTime(yukariStatisticsRecord.RequestTime).ToLocalTime().ToString(YlConstants.DATE_FORMAT));
				line.Add(yukariStatisticsRecord.RoomName ?? String.Empty);

				// タイアップ情報
				line.Add(yukariStatisticsRecord.CategoryName ?? String.Empty);
				line.Add(yukariStatisticsRecord.TieUpName ?? String.Empty);
				line.Add(yukariStatisticsRecord.SongOpEd ?? String.Empty);
				line.Add((yukariStatisticsRecord.TieUpAgeLimit < 0 ? 0 : yukariStatisticsRecord.TieUpAgeLimit).ToString());
				DateTime releaseDate = JulianDay.ModifiedJulianDateToDateTime(yukariStatisticsRecord.SongReleaseDate);
				line.Add(releaseDate.ToString(YlConstants.DATE_FORMAT));
				line.Add(releaseDate.ToString("yyyy"));
				line.Add(yukariStatisticsRecord.TieUpGroupName ?? String.Empty);

				// 制作会社情報
				line.Add(yukariStatisticsRecord.MakerName ?? String.Empty);

				// 楽曲情報
				line.Add(yukariStatisticsRecord.SongName ?? String.Empty);

				// 人物情報
				line.Add(yukariStatisticsRecord.ArtistName ?? String.Empty);
				line.Add(yukariStatisticsRecord.LyristName ?? String.Empty);
				line.Add(yukariStatisticsRecord.ComposerName ?? String.Empty);
				line.Add(yukariStatisticsRecord.ArrangerName ?? String.Empty);

				// その他
				line.Add(yukariStatisticsRecord.RequestMoviePath);
				line.Add(yukariStatisticsRecord.Worker ?? String.Empty);

				contents.Add(line);
			}

			CsvManager.SaveCsv(YukariStatisticsPath!, contents, "\r\n", Encoding.UTF8, titleColumns);
			return Task.CompletedTask;
		}

		// --------------------------------------------------------------------
		// ゆかり統計の属性情報を最新化
		// --------------------------------------------------------------------
		private static void UpdateYukariStatistics()
		{
			// Yurelin に最新化を依頼
			YukaListerModel.Instance.EnvModel.Yurelin.UpdatePastYukariStatisticsKind = UpdatePastYukariStatisticsKind.All;
			YlCommon.ActivateYurelinIfNeeded();

			// 最新化されるまで待機
			while (YukaListerModel.Instance.EnvModel.Yurelin.UpdatePastYukariStatisticsKind != UpdatePastYukariStatisticsKind.None)
			{
				Thread.Sleep(Common.GENERAL_SLEEP_TIME);
			}
		}

		// --------------------------------------------------------------------
		// ゆかり統計出力対象期間コントロールを更新
		// --------------------------------------------------------------------
		private void UpdateYukariStatisticsPeriodControls()
		{
			YukariStatisticsPeriod yukariStatisticsPeriod = (YukariStatisticsPeriod)SelectedYukariStatisticsPeriodIndex;

			// 出力対象期間
			switch (yukariStatisticsPeriod)
			{
				case YukariStatisticsPeriod.Today:
					YukariStatisticsPeriodFrom = YukariStatisticsPeriodTo = DateTime.Today;
					break;
				case YukariStatisticsPeriod.Yesterday:
					YukariStatisticsPeriodFrom = YukariStatisticsPeriodTo = DateTime.Today.AddDays(-1);
					break;
				case YukariStatisticsPeriod.CurrentMonth:
					YukariStatisticsPeriodFrom = CurrentMonth();
					YukariStatisticsPeriodTo = CurrentMonth().AddMonths(1).AddDays(-1);
					break;
				case YukariStatisticsPeriod.LastMonth:
					YukariStatisticsPeriodFrom = CurrentMonth().AddMonths(-1);
					YukariStatisticsPeriodTo = CurrentMonth().AddDays(-1);
					break;
				case YukariStatisticsPeriod.ThisYear:
					YukariStatisticsPeriodFrom = ThisYear();
					YukariStatisticsPeriodTo = ThisYear().AddYears(1).AddDays(-1);
					break;
				case YukariStatisticsPeriod.LastYear:
					YukariStatisticsPeriodFrom = ThisYear().AddYears(-1);
					YukariStatisticsPeriodTo = ThisYear().AddDays(-1);
					break;
				case YukariStatisticsPeriod.All:
				case YukariStatisticsPeriod.Custom:
					YukariStatisticsPeriodFrom = YukariStatisticsPeriodTo = null;
					break;
			}

			// 期間指定
			IsCustomYukariStatisticsPeriodEnabled = yukariStatisticsPeriod == YukariStatisticsPeriod.Custom;
		}
	}
}
