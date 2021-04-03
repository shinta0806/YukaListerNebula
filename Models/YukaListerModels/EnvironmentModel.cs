// ============================================================================
// 
// 環境設定類を管理する
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet;

using Shinta;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerCores;

namespace YukaLister.Models.YukaListerModels
{
	public class EnvironmentModel : NotificationObject
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EnvironmentModel()
		{
			// 最初にログの設定をする
			SetLogWriter();

			// 環境設定
			// Load() はしない（YukaListerModel.Instance 生成途中で EnvironmentModel が生成され、エラー発生時に YukaListerModel.Instance 経由でのログ記録ができないため）
			YlSettings = new();
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// 環境設定
		public YlSettings YlSettings { get; set; }

		// ログ
		public LogWriter LogWriter { get; } = new(YlConstants.APP_ID);

		// ネビュラコア
		public Sifolin Sifolin { get; } = new();

		// ゆかりすたー NEBULA 全体の動作状況
		private volatile YukaListerStatus _yukaListerStatus = YukaListerStatus.Ready;
		public YukaListerStatus YukaListerStatus
		{
			get => _yukaListerStatus;
			set => _yukaListerStatus = value;
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
					_exeFullFolder = Path.GetDirectoryName(ExeFullPath) + "\\";
				}
				return _exeFullFolder;
			}
		}

		// アプリケーション終了時タスク安全中断用
		public CancellationTokenSource AppCancellationTokenSource { get; } = new();

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア稼働終了
		// --------------------------------------------------------------------
		public async Task QuitAllCoresAsync()
		{
			Debug.Assert(AppCancellationTokenSource.Token.IsCancellationRequested, "QuitAllCores() not cancelled");
			Debug.WriteLine("QuitAllCoresAsync()");
			if (Sifolin.MainTask != null)
			{
				Sifolin.MainEvent.Set();
				await Sifolin.MainTask;
			}
		}

		// --------------------------------------------------------------------
		// ネビュラコア稼働開始
		// --------------------------------------------------------------------
		public void StartAllCores()
		{
			Sifolin.Start();
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// LogWriter の設定
		// --------------------------------------------------------------------
		private void SetLogWriter()
		{
			LogWriter.ApplicationQuitToken = AppCancellationTokenSource.Token;
			LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "起動しました：" + YlConstants.APP_NAME_J + " "
					+ YlConstants.APP_VER + " ====================");
#if DEBUG
			LogWriter.ShowLogMessage(TraceEventType.Verbose, "デバッグモード：" + Common.DEBUG_ENABLED_MARK);
#endif
			LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "プロセス動作モード：" + (Environment.Is64BitProcess ? "64" : "32"));
			LogWriter.ShowLogMessage(TraceEventType.Verbose, "Path: " + ExeFullPath);
		}

	}
}
