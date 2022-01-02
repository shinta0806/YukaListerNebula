// ============================================================================
// 
// 報告されたリスト問題の管理ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;
using YukaLister.ViewModels.MiscWindowViewModels;

namespace YukaLister.ViewModels.ReportWindowViewModels
{
	public class EditReportWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public EditReportWindowViewModel(TReport report)
		{
			Report = report;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public EditReportWindowViewModel()
		{
			Report = new TReport();
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 報告内容
		public TReport Report { get; set; }

		// フォルダー
		public String? Folder
		{
			get => Path.GetDirectoryName(Report.Path);
		}

		// 報告日時文字列
		public String? RegistTimeString
		{
			get
			{
				DateTime registTime = JulianDay.ModifiedJulianDateToDateTime(Report.RegistTime);
				return TimeZoneInfo.ConvertTimeFromUtc(registTime, TimeZoneInfo.Local).ToString(YlConstants.DATE_FORMAT + YlConstants.DAY_OF_WEEK_FORMAT + " " + YlConstants.TIME_FORMAT);
			}
		}

		// 対応コメント
		private String? _statusComment;
		public String? StatusComment
		{
			get => _statusComment;
			set => RaisePropertyChangedIfSet(ref _statusComment, value);
		}

		// 対応状況群
		private List<String>? _statusStrings;
		public List<String>? StatusStrings
		{
			get => _statusStrings;
			set => RaisePropertyChangedIfSet(ref _statusStrings, value);
		}

		// 選択された対応状況
		private String? _selectedStatusString;
		public String? SelectedStatusString
		{
			get => _selectedStatusString;
			set => RaisePropertyChangedIfSet(ref _selectedStatusString, value);
		}

		// OK ボタンフォーカス
		private Boolean _isButtonOkFocused;
		public Boolean IsButtonOkFocused
		{
			get => _isButtonOkFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isButtonOkFocused = value;
				RaisePropertyChanged(nameof(IsButtonOkFocused));
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 名称の編集ボタンの制御
		private ViewModelCommand? _buttonEditMusicInfoClickedCommand;

		public ViewModelCommand ButtonEditMusicInfoClickedCommand
		{
			get
			{
				if (_buttonEditMusicInfoClickedCommand == null)
				{
					_buttonEditMusicInfoClickedCommand = new ViewModelCommand(ButtonEditMusicInfoClicked);
				}
				return _buttonEditMusicInfoClickedCommand;
			}
		}

		public void ButtonEditMusicInfoClicked()
		{
			try
			{
				String path = Report.Path;
				if (String.IsNullOrEmpty(path) || !File.Exists(path))
				{
					throw new Exception("報告対象のファイルが存在しません。\n" + path);
				}

				// ファイル命名規則とフォルダー固定値を適用
				Dictionary<String, String?> dic = YlCommon.MatchFileNameRulesAndFolderRuleForSearch(path);

				// 楽曲名が取得できていない場合は編集不可
				if (String.IsNullOrEmpty(dic[YlConstants.RULE_VAR_TITLE]))
				{
					throw new Exception("ファイル名から楽曲名を取得できていないため、編集できません。\nファイル命名規則を確認して下さい。");
				}

				// ViewModel 経由で名称の編集ウィンドウを開く
				using EditMusicInfoWindowViewModel editMusicInfoWindowViewModel = new(path);
				Messenger.Raise(new TransitionMessage(editMusicInfoWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_EDIT_MUSIC_INFO_WINDOW));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "編集ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region フォルダー設定ボタンの制御
		private ViewModelCommand? _buttonFolderSettingsClickedCommand;

		public ViewModelCommand ButtonFolderSettingsClickedCommand
		{
			get
			{
				if (_buttonFolderSettingsClickedCommand == null)
				{
					_buttonFolderSettingsClickedCommand = new ViewModelCommand(ButtonFolderSettingsClicked);
				}
				return _buttonFolderSettingsClickedCommand;
			}
		}

		public void ButtonFolderSettingsClicked()
		{
			try
			{
				String path = Report.Path;
				if (String.IsNullOrEmpty(path) || !File.Exists(path))
				{
					throw new Exception("報告対象のファイルが存在しません。\n" + path);
				}

				// 設定ファイルがあるフォルダー（設定ファイルが無い場合はファイルのフォルダー）
				String? folder = Path.GetDirectoryName(path);
				if (String.IsNullOrEmpty(folder))
				{
					return;
				}
				String? settingsFolder = YlCommon.FindSettingsFolder(folder);
				if (String.IsNullOrEmpty(settingsFolder))
				{
					settingsFolder = folder;
				}

				// ViewModel 経由でフォルダー設定ウィンドウを開く
				using FolderSettingsWindowViewModel folderSettingsWindowViewModel = new(settingsFolder);
				Messenger.Raise(new TransitionMessage(folderSettingsWindowViewModel, YlConstants.MESSAGE_KEY_OPEN_FOLDER_SETTINGS_WINDOW));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOkClickedCommand;

		public ViewModelCommand ButtonOkClickedCommand
		{
			get
			{
				if (_buttonOkClickedCommand == null)
				{
					_buttonOkClickedCommand = new ViewModelCommand(ButtonOKClicked);
				}
				return _buttonOkClickedCommand;
			}
		}

		public void ButtonOKClicked()
		{
			try
			{
				// Enter キーでボタンが押された場合はテキストボックスからフォーカスが移らずプロパティーが更新されないため強制フォーカス
				IsButtonOkFocused = true;

				CheckAndSave();
				IsOk = true;

				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				// タイトルバー
				Title = "報告されたリスト問題の管理";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
#if TEST
				Title = "［テスト］" + Title;
#endif

				// 対応状況選択肢
				List<String> statusStrings = new();
				statusStrings.AddRange(YlConstants.REPORT_STATUS_NAMES);
				StatusStrings = statusStrings;

				// 値反映
				ReportToProperties();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "リスト問題報告管理ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------
		private void CheckAndSave()
		{
			using ReportContext reportContext = new();
			TReport? record = DbCommon.SelectBaseById(reportContext.Reports, Report.Id);
			if (record == null)
			{
				throw new Exception("対象の報告が見つかりません：" + Report.Id);
			}
			record.StatusComment = StatusComment;
			record.Status = Array.IndexOf(YlConstants.REPORT_STATUS_NAMES, SelectedStatusString);
			if (record.Status < 0)
			{
				throw new Exception("対応状況を選択してください。");
			}

			// 保存
			reportContext.SaveChanges();
		}

		// --------------------------------------------------------------------
		// Report の内容をプロパティーに反映
		// --------------------------------------------------------------------
		private void ReportToProperties()
		{
			StatusComment = Report.StatusComment;
			SelectedStatusString = Report.StatusName;
		}
	}
}
