// ============================================================================
// 
// メインウィンドウの ViewModel 
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Messaging.IO;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class MainWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel(SplashWindowViewModel splashWindowViewModel)
		{
			_splashWindowViewModel = splashWindowViewModel;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel()
		{
			// 警告抑止用にメンバーを null! で初期化
			_splashWindowViewModel = null!;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ウィンドウ左端
		private Double _left;
		public Double Left
		{
			get => _left;
			set => RaisePropertyChangedIfSet(ref _left, value);
		}

		// ウィンドウ上端
		private Double _top;
		public Double Top
		{
			get => _top;
			set => RaisePropertyChangedIfSet(ref _top, value);
		}

		// ウィンドウ幅
		private Double _width;
		public Double Width
		{
			get => _width;
			set => RaisePropertyChangedIfSet(ref _width, value);
		}

		// ウィンドウ高さ
		private Double _height;
		public Double Height
		{
			get => _height;
			set => RaisePropertyChangedIfSet(ref _height, value);
		}

		// ゆかりすたー NEBULA 全体の動作状況
		private String _yukaListerStatusLabel = String.Empty;
		public String YukaListerStatusLabel
		{
			get => _yukaListerStatusLabel;
			set => RaisePropertyChangedIfSet(ref _yukaListerStatusLabel, value);
		}

		// ゆかりすたー NEBULA 全体の動作状況の背景
		private Brush _yukaListerStatusBackground = YlConstants.BRUSH_STATUS_DONE;
		public Brush YukaListerStatusBackground
		{
			get => _yukaListerStatusBackground;
			set => RaisePropertyChangedIfSet(ref _yukaListerStatusBackground, value);
		}

		// 検索可能ファイル数
		private String _numRecordsLabel = String.Empty;
		public String NumRecordsLabel
		{
			get => _numRecordsLabel;
			set => RaisePropertyChangedIfSet(ref _numRecordsLabel, value);
		}

		// ゆかり検索対象フォルダー（表示用）
		private List<TargetFolderInfo>? _targetFolderInfosVisible;
		public List<TargetFolderInfo>? TargetFolderInfosVisible
		{
			get => _targetFolderInfosVisible;
			set => RaisePropertyChangedIfSet(ref _targetFolderInfosVisible, value);
		}

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		public void AddFolderSelected(FolderSelectionMessage folderSelectionMessage)
		{
			try
			{
				YukaListerModel.Instance.ProjModel.AddTargetFolder(folderSelectionMessage.Response);
				UpdateDataGrid();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "追加フォルダー選択時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				// タイトルバー
				Title = YlConstants.APP_NAME_J;
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// イベントハンドラー
				TargetFolderInfo.IsOpenChanged = TargetFolderInfoIsOpenChanged;

				// ステータスバー
				//ClearStatusBarMessage();

				// スプラッシュウィンドウを閉じる
				_splashWindowViewModel.Close();

				// プログラムエラーチェック
				Debug.Assert(YlConstants.FOLDER_SETTINGS_STATUS_TEXTS.Length == (Int32)FolderSettingsStatus.__End__, "MainWindow.Initialize() bad FOLDER_SETTINGS_STATUS_TEXTS length");

				// 環境の変化に対応
				DoVerChangedIfNeeded();
				//LaunchUpdaterIfNeeded();

				// 動作エラーチェック
				UpdateYukaListerStatusError();

				// その他
				UpdateNumRecordsLabel();

#if DEBUGz
				CacheContext.CreateContext("D:", out _);
#endif

#if DEBUGz
				Debug.WriteLine("Exists 1: " + File.Exists(@"D:\TempD\TestYl\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\a.txt"));
				Debug.WriteLine("Exists 2: " + File.Exists(@"D:\TempD\TestYl\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\12345678901234567890123456789\FolderNameLength.txt"));
#endif

#if DEBUGz
				TargetFolderInfo t1 = new("D:\\hoge\\folder", "D:\\hoge\\folder", 0);
				t1.HasChildren = true;
				t1.NumTotalFolders = 2;
				//targetFolderInfo.IsOpen = true;
				TestTargetFolderInfo = t1;

				List<TargetFolderInfo> targetFolderInfos = new();
				targetFolderInfos.Add(t1);
				TargetFolderInfo t2 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブA", 1);
				t2.HasChildren = true;
				t2.NumTotalFolders = 2;
				targetFolderInfos.Add(t2);
				TargetFolderInfo t3 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブA\\さらにサブ", 2);
				targetFolderInfos.Add(t3);
				TargetFolderInfo t4 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブA\\さらにサブ2", 2);
				targetFolderInfos.Add(t4);
				TargetFolderInfo t5 = new("D:\\hoge\\folder", "D:\\hoge\\folder\\サブB", 1);
				t5.HasChildren = true;
				t5.NumTotalFolders = 2;
				targetFolderInfos.Add(t5);
				TargetFolderInfosVisible = targetFolderInfos;
#endif

				// タイマー
				_timerUpdateUi.Interval = TimeSpan.FromSeconds(1.0);
				_timerUpdateUi.Tick += new EventHandler(TimerUpdateUi_Tick);
				_timerUpdateUi.Start();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override async void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_isDisposed)
			{
				return;
			}

			try
			{
				// アプリケーションの終了を通知
				YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Cancel();

				// 終了処理
				await YukaListerModel.Instance.EnvModel.QuitAllCoresAsync();
				SaveExitStatus();
				_isDisposed = true;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ破棄時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// スプラッシュウィンドウ
		private readonly SplashWindowViewModel _splashWindowViewModel;

		// UI 更新用タイマー
		private DispatcherTimer _timerUpdateUi = new();

		// Dispose フラグ
		private Boolean _isDisposed = false;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// バージョン更新時の処理
		// --------------------------------------------------------------------
		private void DoVerChangedIfNeeded()
		{
			// 更新起動時とパス変更時の記録
			// 新規起動時は、両フラグが立つのでダブらないように注意
			String prevLaunchVer = YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer;
			Boolean verChanged = prevLaunchVer != YlConstants.APP_VER || YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchGeneration != YlConstants.APP_GENERATION;
			if (verChanged)
			{
				// ユーザーにメッセージ表示する前にログしておく
				if (String.IsNullOrEmpty(prevLaunchVer))
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "新規起動：" + YlConstants.APP_VER);
				}
				else
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "更新起動：" + prevLaunchVer + "→" + YlConstants.APP_VER);
				}
			}
			String prevLaunchPath = YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchPath;
			Boolean pathChanged = (String.Compare(prevLaunchPath, YukaListerModel.Instance.EnvModel.ExeFullPath, true) != 0);
			if (pathChanged && !String.IsNullOrEmpty(prevLaunchPath))
			{
				YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Information, "パス変更起動：" + prevLaunchPath + "→" + YukaListerModel.Instance.EnvModel.ExeFullPath);
			}

			// 更新起動時とパス変更時の処理
			if (verChanged || pathChanged)
			{
				YlCommon.LogEnvironmentInfo();
			}
			if (verChanged)
			{
				NewVersionLaunched();
			}
		}

		// --------------------------------------------------------------------
		// インストールフォルダーについての警告メッセージ
		// --------------------------------------------------------------------
		private String? InstallWarningMessage()
		{
			if (YukaListerModel.Instance.EnvModel.ExeFullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
					|| YukaListerModel.Instance.EnvModel.ExeFullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)))
			{
				// 自動更新できない
				return YlConstants.APP_NAME_J + " が Program Files フォルダー配下にインストールされているため、正常に動作しません。\n"
						+ "他のフォルダー（例えば C:\\MyApp）配下にインストールしてください。";
			}
			return null;
		}

		// --------------------------------------------------------------------
		// 新バージョンで初回起動された時の処理を行う
		// --------------------------------------------------------------------
		private void NewVersionLaunched()
		{
			String newVerMsg;
			TraceEventType type = TraceEventType.Information;

			// α・β警告、ならびに、更新時のメッセージ（2017/01/09）
			// 新規・更新のご挨拶
			if (String.IsNullOrEmpty(YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer))
			{
				// 新規
				newVerMsg = "【初回起動】\n\n";
				newVerMsg += YlConstants.APP_NAME_J + "をダウンロードしていただき、ありがとうございます。";
			}
			else
			{
				newVerMsg = "【更新起動】\n\n";
				newVerMsg += YlConstants.APP_NAME_J + "を更新していただき、ありがとうございます。\n";
				newVerMsg += "更新内容については［ヘルプ→改訂履歴］メニューをご参照ください。";
			}

			// α・βの注意
			if (YlConstants.APP_VER.Contains("α"))
			{
				newVerMsg += "\n\nこのバージョンは開発途上のアルファバージョンです。\n"
						+ "使用前にヘルプをよく読み、注意してお使い下さい。";
				type = TraceEventType.Warning;
			}
			else if (YlConstants.APP_VER.Contains("β"))
			{
				newVerMsg += "\n\nこのバージョンは開発途上のベータバージョンです。\n"
						+ "使用前にヘルプをよく読み、注意してお使い下さい。";
				type = TraceEventType.Warning;
			}

			// 表示
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(type, newVerMsg);
			SaveExitStatus();

			// Zone ID 削除
			CommonWindows.DeleteZoneID(YukaListerModel.Instance.EnvModel.ExeFullFolder, SearchOption.AllDirectories);

			// パスの注意
			String? installMsg = InstallWarningMessage();
			if (!String.IsNullOrEmpty(installMsg))
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Warning, installMsg);
			}
		}

		// --------------------------------------------------------------------
		// 終了時の状態を保存
		// --------------------------------------------------------------------
		private void SaveExitStatus()
		{
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchPath = YukaListerModel.Instance.EnvModel.ExeFullPath;
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchGeneration = YlConstants.APP_GENERATION;
			YukaListerModel.Instance.EnvModel.YlSettings.PrevLaunchVer = YlConstants.APP_VER;
			YukaListerModel.Instance.EnvModel.YlSettings.DesktopBounds = new Rect(Left, Top, Width, Height);
			YukaListerModel.Instance.EnvModel.YlSettings.Save();
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体の動作状況に応じて背景を設定
		// --------------------------------------------------------------------
		private void SetYukaListerStatusBackground()
		{
			YukaListerStatusBackground = YukaListerModel.Instance.EnvModel.YukaListerStatus switch
			{
				YukaListerStatus.Error => YlConstants.BRUSH_STATUS_ERROR,
				YukaListerStatus.Running => YlConstants.BRUSH_STATUS_RUNNING,
				_ => YlConstants.BRUSH_STATUS_DONE,
			};
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体の動作状況を待機にする
		// --------------------------------------------------------------------
		private void SetYukaListerStatusReady()
		{
			YukaListerModel.Instance.EnvModel.YukaListerStatus = YukaListerStatus.Ready;
			YukaListerStatusLabel = YlConstants.APP_NAME_J + "は正常に動作しています。";
			SetYukaListerStatusBackground();
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：IsOpen が変更された
		// --------------------------------------------------------------------
		private void TargetFolderInfoIsOpenChanged(TargetFolderInfo targetFolderInfo)
		{
			try
			{
				YukaListerModel.Instance.ProjModel.UpdateTargetFolderInfosVisible(targetFolderInfo);
				UpdateDataGrid();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "IsOpen 変更時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：UI 表示を更新
		// --------------------------------------------------------------------
		private void TimerUpdateUi_Tick(Object? sender, EventArgs e)
		{
			try
			{
				_timerUpdateUi.Stop();

				// ゆかりすたー NEBULA 全体の動作状況
				if (YukaListerModel.Instance.EnvModel.YukaListerStatus == YukaListerStatus.Running)
				{
					UpdateNumRecordsLabel();
					UpdateYukaListerStatusRunning();
				}

				// DataGrid
				if (YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged || YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated)
				{
					UpdateDataGrid();
				}

				_timerUpdateUi.Start();
			}
			catch (Exception excep)
			{
				// 定期的にタイマーエラーが表示されることのないよう、エラー発生時はタイマーを再開しない
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "タイマー時エラー：\n" + excep.Message + "\n再起動してください。");
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// DataGrid 表示を更新
		// --------------------------------------------------------------------
		private void UpdateDataGrid()
		{
			// 先に Dirty フラグをクリア（後にすると、更新中に他のスレッドが立てたフラグもクリアしてしまうため）
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridCountChanged = false;
			YukaListerModel.Instance.EnvModel.IsMainWindowDataGridItemUpdated = false;

			// 更新
			// ToDo: IsMainWindowDataGridItemUpdated のみが立っていた場合は効率よい処理方法があるのではないか
			TargetFolderInfosVisible = YukaListerModel.Instance.ProjModel.TargetFolderInfosVisible();
		}

		// --------------------------------------------------------------------
		// 検索可能ファイル数を更新
		// --------------------------------------------------------------------
		private void UpdateNumRecordsLabel()
		{
			using ListContextInDisk listContextInDisk = ListContextInDisk.CreateContext(out DbSet<TFound> founds);
			NumRecordsLabel = founds.Count().ToString("#,0");
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体のエラーチェック
		// --------------------------------------------------------------------
		private void UpdateYukaListerStatusError()
		{
			if (!YukaListerModel.Instance.EnvModel.YlSettings.IsYukariConfigPathValid())
			{
				// ゆかり設定ファイルエラー
				YukaListerModel.Instance.EnvModel.YukaListerStatus = YukaListerStatus.Error;
				YukaListerStatusLabel = "ゆかり設定ファイルが正しく指定されていません。";
				SetYukaListerStatusBackground();
			}
			else
			{
				// 正常
				if (YukaListerModel.Instance.EnvModel.YukaListerStatus != YukaListerStatus.Running)
				{
					SetYukaListerStatusReady();
				}
			}
		}

		// --------------------------------------------------------------------
		// ゆかりすたー NEBULA 全体の動作状況チェック
		// --------------------------------------------------------------------
		private void UpdateYukaListerStatusRunning()
		{
			TargetFolderInfo? targetFolderInfo = YukaListerModel.Instance.ProjModel.RunningTargetFolderInfo();
			if (targetFolderInfo == null)
			{
				// 動作中のタスクが無くなった
				YukaListerModel.Instance.EnvModel.YukaListerStatus = YukaListerStatus.Ready;
				SetYukaListerStatusReady();
			}
			else
			{
				YukaListerStatusLabel = targetFolderInfo.FolderTaskDetail switch
				{
					FolderTaskDetail.CacheToDisk => YlConstants.RUNNING_CACHE_TO_DISK,
					FolderTaskDetail.FindSubFolders => YlConstants.RUNNING_FIND_SUB_FOLDERS,
					FolderTaskDetail.AddFileNames => YlConstants.RUNNING_ADD_FILE_NAMES,
					FolderTaskDetail.AddInfos => YlConstants.RUNNING_ADD_INFOS,
					FolderTaskDetail.Remove => YlConstants.RUNNING_REMOVE,
					_ => String.Empty,
				} + "...\n" + targetFolderInfo.Path;
				SetYukaListerStatusBackground();
			}
		}


	}
}
