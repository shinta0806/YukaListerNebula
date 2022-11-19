// ============================================================================
// 
// 環境設定類を管理する
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;

using Shinta;
using Shinta.Wpf;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.Settings;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerCores;

namespace YukaLister.Models.YukaListerModels
{
	internal class EnvironmentModel : NotificationObject
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EnvironmentModel()
		{
			// 最初にログの設定をする
			SetLogWriter();

			// 環境設定の Load() はしない（YlModel.Instance 生成途中で EnvironmentModel が生成され、エラー発生時に YukaListerModel.Instance 経由でのログ記録ができないため）
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// 環境設定
		public YlSettings YlSettings { get; } = new();

		// タグ設定
		public TagSettings TagSettings { get; } = new();

		// ログ
		public LogWriter LogWriter { get; } = new(YlConstants.APP_ID);

		// ネビュラコア：検索データ作成担当
		public Sifolin Sifolin { get; } = new();

		// ネビュラコア：動画リスト作成担当
		public Kamlin Kamlin { get; } = new();

		// ネビュラコア：統計データ作成担当
		public Yurelin Yurelin { get; } = new();

		// ネビュラコア：同期担当
		public Syclin Syclin { get; } = new();

		// ネビュラコア：負荷監視担当
		public Lomolin Lomolin { get; } = new();

		// ゆかりすたー NEBULA パーツごとの動作状況
		private readonly YukaListerStatus[] _yukaListerPartsStatus = new YukaListerStatus[(Int32)YukaListerPartsStatusIndex.__End__];
		public YukaListerStatus[] YukaListerPartsStatus
		{
			get => _yukaListerPartsStatus;
		}

		// ゆかりすたー NEBULA パーツごとの動作メッセージ
		// YukaListerPartsStatusMessage[YukaListerPartsStatusIndex.Sifolin] は使用しない
		private readonly String[] _yukaListerPartsStatusMessage = new String[(Int32)YukaListerPartsStatusIndex.__End__];
		public String[] YukaListerPartsStatusMessage
		{
			get => _yukaListerPartsStatusMessage;
		}

		// ゆかりすたー NEBULA 全体の動作状況
		public YukaListerStatus YukaListerWholeStatus
		{
			get
			{
				if (YukaListerPartsStatus.Contains(YukaListerStatus.Error))
				{
					return YukaListerStatus.Error;
				}
				if (YukaListerPartsStatus.Contains(YukaListerStatus.Running))
				{
					return YukaListerStatus.Running;
				}
				return YukaListerStatus.Ready;
			}
		}

		// メインウィンドウの DataGrid のアイテム数が増減した
		private volatile Boolean _isMainWindowDataGridCountChanged;
		public Boolean IsMainWindowDataGridCountChanged
		{
			get => _isMainWindowDataGridCountChanged;
			set => _isMainWindowDataGridCountChanged = value;
		}

		// メインウィンドウの DataGrid のアイテム数は変わらないがアイテムの内容が更新された
		private volatile Boolean _isMainWindowDataGridItemUpdated;
		public Boolean IsMainWindowDataGridItemUpdated
		{
			get => _isMainWindowDataGridItemUpdated;
			set => _isMainWindowDataGridItemUpdated = value;
		}

		// ネビュラコアからのエラーメッセージ
		public ConcurrentQueue<String> NebulaCoreErrors { get; set; } = new();

		// EXE フルパス
		private String? _exeFullPath;
		public String ExeFullPath
		{
			get
			{
				if (_exeFullPath == null)
				{
					// 単一ファイル時にも内容が格納される GetCommandLineArgs を用いる（Assembly 系の Location は不可）
					_exeFullPath = Environment.GetCommandLineArgs()[0];
					if (Path.GetExtension(_exeFullPath).ToLower() != Common.FILE_EXT_EXE)
					{
						_exeFullPath = Path.ChangeExtension(_exeFullPath, Common.FILE_EXT_EXE);
					}
				}
				return _exeFullPath;
			}
		}

		// EXE があるフォルダーのフルパス（末尾 '\\'）
		private String? _exeFullFolder;
		public String ExeFullFolder
		{
			get
			{
				if (_exeFullFolder == null)
				{
					_exeFullFolder = Path.GetDirectoryName(ExeFullPath) + '\\';
				}
				return _exeFullFolder;
			}
		}

		// アプリケーション終了時タスク安全中断用
		public CancellationTokenSource AppCancellationTokenSource { get; } = new();

		// インメモリデータベースが生存し続けるようにインスタンスを保持
		// マルチスレッドで安全に使用できるよう、本プロパティーは使用せず、新たなコンテキストを作成すること
		private ListContextInMemory? _listContextInMemory;
		public ListContextInMemory ListContextInMemory
		{
			set
			{
				_listContextInMemory?.Dispose();
				_listContextInMemory = value;
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ヘルプリンクの制御
		private ListenerCommand<String>? _helpClickedCommand;

		public ListenerCommand<String> HelpClickedCommand
		{
			get
			{
				if (_helpClickedCommand == null)
				{
					_helpClickedCommand = new ListenerCommand<String>(HelpClicked);
				}
				return _helpClickedCommand;
			}
		}

		public void HelpClicked(String parameter)
		{
			try
			{
				ShowHelp(parameter);
			}
			catch (Exception excep)
			{
				LogWriter.ShowLogMessage(TraceEventType.Error, "ヘルプ表示時エラー：\n" + excep.Message);
				LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア稼働終了
		// --------------------------------------------------------------------
		public async Task QuitAllCoresAsync()
		{
			Debug.Assert(AppCancellationTokenSource.Token.IsCancellationRequested, "QuitAllCores() not cancelled");
			Debug.WriteLine("QuitAllCoresAsync() a");
			List<Task> tasks = new();
			if (Sifolin.MainTask != null)
			{
				Sifolin.MainEvent.Set();
				tasks.Add(Sifolin.MainTask);
			}
			if (Kamlin.MainTask != null)
			{
				Kamlin.MainEvent.Set();
				tasks.Add(Kamlin.MainTask);
			}
			if (Yurelin.MainTask != null)
			{
				Yurelin.MainEvent.Set();
				tasks.Add(Yurelin.MainTask);
			}
			if (Syclin.MainTask != null)
			{
				Syclin.MainEvent.Set();
				tasks.Add(Syclin.MainTask);
			}
			if (Lomolin.MainTask != null)
			{
				tasks.Add(Lomolin.MainTask);
			}
			Debug.WriteLine("QuitAllCoresAsync() b");
			await Task.WhenAll(tasks);
			Debug.WriteLine("QuitAllCoresAsync() c");
		}

		// --------------------------------------------------------------------
		// ネビュラコア稼働開始
		// --------------------------------------------------------------------
		public void StartAllCores()
		{
			Sifolin.Start();
			Kamlin.Start();
			Yurelin.Start();
			Syclin.Start();
			Lomolin.Start();
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// --------------------------------------------------------------------
		// ファイル名
		// --------------------------------------------------------------------
		private const String FILE_NAME_HELP_PREFIX = YlConstants.APP_ID + "Nebula_JPN";

		// --------------------------------------------------------------------
		// フォルダー名
		// --------------------------------------------------------------------
		private const String FOLDER_NAME_HELP_PARTS = "HelpParts\\";

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// LogWriter の設定
		// --------------------------------------------------------------------
		private void SetLogWriter()
		{
			LogWriter.ApplicationQuitToken = AppCancellationTokenSource.Token;
			LogWriter.SimpleTraceListener.MaxSize = 10 * 1024 * 1024;
			LogWriter.SimpleTraceListener.MaxOldGenerations = 5;
			LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "起動しました：" + YlConstants.APP_NAME_J + " "
					+ YlConstants.APP_VER + "  " + YlConstants.APP_DISTRIB + " ====================");
#if DEBUG
			LogWriter.ShowLogMessage(TraceEventType.Verbose, "デバッグモード：" + Common.DEBUG_ENABLED_MARK);
#endif
			LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "プロセス動作モード：" + (Environment.Is64BitProcess ? "64" : "32"));
			LogWriter.ShowLogMessage(TraceEventType.Verbose, "Path: " + ExeFullPath);
		}

		// --------------------------------------------------------------------
		// ヘルプの表示
		// --------------------------------------------------------------------
		private void ShowHelp(String? anchor = null)
		{
			String? helpPath = null;

			try
			{
				// アンカーが指定されている場合は状況依存型ヘルプを表示
				if (!String.IsNullOrEmpty(anchor))
				{
					helpPath = ExeFullFolder + YlConstants.FOLDER_NAME_DOCUMENTS + FOLDER_NAME_HELP_PARTS + FILE_NAME_HELP_PREFIX + "_" + anchor + Common.FILE_EXT_HTML;
					try
					{
						Common.ShellExecute(helpPath);
						return;
					}
					catch (Exception excep)
					{
						LogWriter.ShowLogMessage(TraceEventType.Error, "状況に応じたヘルプを表示できませんでした：\n" + excep.Message + "\n" + helpPath
								+ "\n通常のヘルプを表示します。");
					}
				}

				// アンカーが指定されていない場合・状況依存型ヘルプを表示できなかった場合は通常のヘルプを表示
				helpPath = ExeFullFolder + YlConstants.FOLDER_NAME_DOCUMENTS + FILE_NAME_HELP_PREFIX + Common.FILE_EXT_HTML;
				Common.ShellExecute(helpPath);
			}
			catch (Exception excep)
			{
				LogWriter.ShowLogMessage(TraceEventType.Error, "ヘルプを表示できませんでした。\n" + excep.Message + "\n" + helpPath);
			}
		}
	}
}
