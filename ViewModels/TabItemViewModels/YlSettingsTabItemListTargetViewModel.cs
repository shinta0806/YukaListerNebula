// ============================================================================
// 
// リスト対象タブアイテムの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using Shinta;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using YukaLister.Models;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.TabItemViewModels
{
	public class YlSettingsTabItemListTargetViewModel : TabItemViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemListTargetViewModel(YlViewModel windowViewModel)
				: base(windowViewModel)
		{
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public YlSettingsTabItemListTargetViewModel()
				: base()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// リスト化対象ファイルの拡張子
		public ObservableCollection<String> TargetExts { get; set; } = new();

		// リストで選択されている拡張子
		private String? _selectedTargetExt;
		public String? SelectedTargetExt
		{
			get => _selectedTargetExt;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedTargetExt, value))
				{
					ButtonRemoveExtClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// 追加したい拡張子
		private String? _addingTargetExt;
		public String? AddingTargetExt
		{
			get => _addingTargetExt;
			set
			{
				if (RaisePropertyChangedIfSet(ref _addingTargetExt, value))
				{
					ButtonAddExtClickedCommand.RaiseCanExecuteChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 追加ボタンの制御
		private ViewModelCommand? _buttonAddExtClickedCommand;

		public ViewModelCommand ButtonAddExtClickedCommand
		{
			get
			{
				if (_buttonAddExtClickedCommand == null)
				{
					_buttonAddExtClickedCommand = new ViewModelCommand(ButtonAddExtClicked, CanButtonAddExtClicked);
				}
				return _buttonAddExtClickedCommand;
			}
		}

		public Boolean CanButtonAddExtClicked()
		{
			return !String.IsNullOrEmpty(AddingTargetExt);
		}

		public void ButtonAddExtClicked()
		{
			try
			{
				String? ext = AddingTargetExt;

				// 入力が空の場合はボタンは押されないはずだが念のため
				if (String.IsNullOrEmpty(ext))
				{
					throw new Exception("拡張子を入力して下さい。");
				}

				// ワイルドカード等を除去
				ext = ext?.Replace("*", "");
				ext = ext?.Replace("?", "");
				ext = ext?.Replace(".", "");

				// 除去で空になっていないか
				if (String.IsNullOrEmpty(ext))
				{
					throw new Exception("有効な拡張子を入力して下さい。");
				}

				// 先頭にピリオド付加
				ext = "." + ext;

				// 小文字化
				ext = ext.ToLower();

				// 重複チェック
				if (TargetExts.Contains(ext))
				{
					throw new Exception("既に追加されています。");
				}

				// 追加
				TargetExts.Add(ext);
				SelectedTargetExt = ext;
				AddingTargetExt = null;
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "追加ボタンクリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region 削除ボタンの制御
		private ViewModelCommand? _buttonRemoveExtClickedCommand;

		public ViewModelCommand ButtonRemoveExtClickedCommand
		{
			get
			{
				if (_buttonRemoveExtClickedCommand == null)
				{
					_buttonRemoveExtClickedCommand = new ViewModelCommand(ButtonRemoveExtClicked, CanButtonRemoveExtClicked);
				}
				return _buttonRemoveExtClickedCommand;
			}
		}

		public Boolean CanButtonRemoveExtClicked()
		{
			return !String.IsNullOrEmpty(SelectedTargetExt);
		}

		public void ButtonRemoveExtClicked()
		{
			try
			{
				// 選択されていない場合はボタンが押されないはずだが念のため
				if (String.IsNullOrEmpty(SelectedTargetExt))
				{
					throw new Exception("削除したい拡張子を選択してください。");
				}

				// 削除
				TargetExts.Remove(SelectedTargetExt!);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "削除ボタンクリック時エラー：\n" + excep.Message);
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
			if (!TargetExts.Any())
			{
				throw new Exception("リスト化対象ファイルの拡張子を指定して下さい。");
			}
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public override void PropertiesToSettings()
		{
			YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.Clear();
			YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.AddRange(TargetExts);
			YukaListerModel.Instance.EnvModel.YlSettings.TargetExts.Sort();
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public override void SettingsToProperties()
		{
			TargetExts.Clear();
			foreach (String ext in YukaListerModel.Instance.EnvModel.YlSettings.TargetExts)
			{
				TargetExts.Add(ext);
			}
		}
	}
}
