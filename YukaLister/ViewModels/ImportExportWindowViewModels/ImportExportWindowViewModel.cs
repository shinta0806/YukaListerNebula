﻿// ============================================================================
// 
// インポートウィンドウ・エクスポートウィンドウの基底 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 本クラスは ImportExportWindow を使わない。
// ImportWindowViewModel などの派生クラスが ImportExportWindow を使う。
// abstract にすると VisualStudio が ImportExportWindow のプレビューを表示しなくなるので通常のクラスにしておく。
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.ImportExportWindowViewModels
{
	internal class ImportExportWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラム中で使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public ImportExportWindowViewModel(String kind)
		{
			_kind = kind;
			CompositeDisposable.Add(_semaphoreSlim);
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public ImportExportWindowViewModel()
		{
			_kind = String.Empty;
			CompositeDisposable.Add(_semaphoreSlim);
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 説明
		private String? _description;
		public String? Description
		{
			get => _description;
			set
			{
				if (RaisePropertyChangedIfSet(ref _description, value))
				{
					if (!String.IsNullOrEmpty(_description))
					{
						_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _description);
					}
				}
			}
		}

		// 進捗
		private String? _progress;
		public String? Progress
		{
			get => _progress;
			set
			{
				if (RaisePropertyChangedIfSet(ref _progress, value))
				{
					if (!String.IsNullOrEmpty(_progress))
					{
						_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _progress);
					}
				}
			}
		}

		// ログ
		public ObservableCollection<String> Logs { get; set; } = new();

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ウィンドウを閉じられるかの制御
		private ListenerCommand<CancelEventArgs>? _windowClosingCommand;

		public ListenerCommand<CancelEventArgs> WindowClosingCommand
		{
			get
			{
				if (_windowClosingCommand == null)
				{
					_windowClosingCommand = new ListenerCommand<CancelEventArgs>(WindowClosing);
				}
				return _windowClosingCommand;
			}
		}

		public void WindowClosing(CancelEventArgs cancelEventArgs)
		{
			try
			{
				if (!CancelImportExportIfNeeded())
				{
					// インポートをキャンセルしなかった場合はクローズをキャンセル
					cancelEventArgs.Cancel = true;
				}
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "クローズ処理時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
		#endregion

		#region 中止ボタンの制御
		private ViewModelCommand? _buttonAbortClickedCommand;

		public ViewModelCommand ButtonAbortClickedCommand
		{
			get
			{
				if (_buttonAbortClickedCommand == null)
				{
					_buttonAbortClickedCommand = new ViewModelCommand(ButtonAbortClicked);
				}
				return _buttonAbortClickedCommand;
			}
		}

		public void ButtonAbortClicked()
		{
			try
			{
				if (CancelImportExportIfNeeded())
				{
					Messenger.Raise(new WindowActionMessage(Common.MESSAGE_KEY_WINDOW_CLOSE));
				}
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "中止ボタンクリック時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override async void Initialize()
		{
			base.Initialize();

			try
			{
				Title = "ゆかりすたー情報ファイルの" + _kind;

				YlModel.Instance.EnvModel.LogWriter.AppendDisplayText = AppendDisplayText;

				await ImportExportAsync();
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "インポートエクスポートウィンドウ初期化時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
			finally
			{
				// 終了確認を出さないようにする
				_abortCancellationTokenSource.Cancel();

				YlModel.Instance.EnvModel.LogWriter.AppendDisplayText = null;
				if (!_isDisposed)
				{
					Close();
				}
			}
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// タスク中止用
		protected CancellationTokenSource _abortCancellationTokenSource = new();

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			if (!_isDisposed)
			{
				try
				{
					// タスク実行中は待機（セマフォが破棄されないようにするため）
					while (_semaphoreSlim.CurrentCount == 0)
					{
						Thread.Sleep(Common.GENERAL_SLEEP_TIME);
					}

					_isDisposed = true;
				}
				catch (Exception ex)
				{
					_logWriter?.ShowLogMessage(TraceEventType.Error, "インポートエクスポートウィンドウ破棄時エラー：\n" + ex.Message);
					_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
				}
			}

			base.Dispose(isDisposing);
		}

		// --------------------------------------------------------------------
		// インポート・エクスポート処理
		// ワーカースレッドで実行される前提
		// --------------------------------------------------------------------
		protected virtual Task ImportExportByWorker(Object? _)
		{
			return Task.CompletedTask;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// インポートまたはエクスポート
		private readonly String _kind;

		// タスクが多重起動されるのを抑止する
		private readonly SemaphoreSlim _semaphoreSlim = new(1);

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ログ文字列に追加
		// --------------------------------------------------------------------
		private void AppendDisplayText(String text)
		{
			Logs.Add(text);
		}

		// --------------------------------------------------------------------
		// インポート・エクスポートをキャンセル
		// ＜返値＞ true: キャンセルした（または既にされている）, false: キャンセルしなかった
		// --------------------------------------------------------------------
		private Boolean CancelImportExportIfNeeded()
		{
			if (_abortCancellationTokenSource.IsCancellationRequested)
			{
				// 既にキャンセル処理中
				return true;
			}

			if (MessageBox.Show(_kind + "を中止してよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.No)
			{
				// 新たにキャンセル
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, _kind + "を中止しています...");
				_abortCancellationTokenSource.Cancel();
				return true;
			}

			// キャンセルしない
			return false;
		}

		// --------------------------------------------------------------------
		// インポート・エクスポート処理
		// --------------------------------------------------------------------
		private async Task ImportExportAsync()
		{
			try
			{
				await YlCommon.LaunchTaskAsync<Object?>(_semaphoreSlim, ImportExportByWorker, null, _kind);
				_abortCancellationTokenSource.Token.ThrowIfCancellationRequested();
				_logWriter?.ShowLogMessage(TraceEventType.Information, _kind + "完了。");
			}
			catch (OperationCanceledException)
			{
				_logWriter?.LogMessage(TraceEventType.Information, _kind + "を中止しました。");
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, _kind + "時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
	}
}
