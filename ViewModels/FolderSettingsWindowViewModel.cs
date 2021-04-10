// ============================================================================
// 
// フォルダー設定ウィンドウの ViewModel
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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using YukaLister.Models;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels
{
	public class FolderSettingsWindowViewModel : YlViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public FolderSettingsWindowViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// タグボタンのコンテキストメニュー
		public List<MenuItem> ContextMenuButtonVarItems { get; set; } = new();

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			try
			{
				// タイトルバー
				Title = "フォルダー設定";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// タグボタンのコンテキストメニュー
				List<String> labels = CreateRuleVarLabels();
				foreach (String label in labels)
				{
					// オンボーカル・オフボーカル・タグは除外
					if (label.IndexOf(YlConstants.RULE_VAR_ON_VOCAL, StringComparison.OrdinalIgnoreCase) < 0
							&& label.IndexOf(YlConstants.RULE_VAR_OFF_VOCAL, StringComparison.OrdinalIgnoreCase) < 0
							&& label.IndexOf(YlConstants.RULE_VAR_TAG, StringComparison.OrdinalIgnoreCase) < 0)
					{
						AddContextMenuItemToButtonVar(label);
					}
				}

#if false
				// カテゴリー一覧
				using (MusicInfoDatabaseInDisk aMusicInfoDatabaseInDisk = new MusicInfoDatabaseInDisk(Environment!))
				{
					mCachedCategoryNames = YlCommon.SelectCategoryNames(aMusicInfoDatabaseInDisk.Connection);
				}

				// 固定値項目（カテゴリー一覧設定後に行う）
				FolderNameRuleNames = new List<String>();
				foreach (String aLabel in labels)
				{
					// * は除外
					if (aLabel.IndexOf(YlConstants.RULE_VAR_ANY) < 0)
					{
						FolderNameRuleNames.Add(aLabel);
					}
				}
				SelectedFolderNameRuleName = FolderNameRuleNames[0];

				// リスナーに通知
				RaisePropertyChanged(nameof(PathExLen));
				RaisePropertyChanged(nameof(ContextMenuButtonVarItems));
				RaisePropertyChanged(nameof(FolderNameRuleNames));
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー設定ウィンドウ初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// --------------------------------------------------------------------
		// ButtonVar のコンテキストメニューにアイテムを追加
		// --------------------------------------------------------------------
		private void AddContextMenuItemToButtonVar(String label)
		{
			if (ContextMenuButtonVarItems != null)
			{
				YlCommon.AddContextMenuItem(ContextMenuButtonVarItems, label, ContextMenuButtonVarItem_Click);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void ContextMenuButtonVarItem_Click(Object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				MenuItem item = (MenuItem)sender;
				String? key = FindRuleVarName((String)item.Header);
				if (String.IsNullOrEmpty(key))
				{
					return;
				}
				String wrappedVarName = WrapVarName(key);

#if false
				// カーソル位置に挿入
				Int32 aSelectionStartBak = FileNameRuleSelectionStart;
				if (String.IsNullOrEmpty(FileNameRule))
				{
					FileNameRule = wrappedVarName;
				}
				else
				{
					FileNameRule = FileNameRule!.Substring(0, FileNameRuleSelectionStart) + wrappedVarName
							+ FileNameRule.Substring(FileNameRuleSelectionStart + FileNameRuleSelectionLength);
				}

				// タグボタンにフォーカスが移っているので戻す
				IsFileNameRuleFocused = true;

				// カーソル位置変更
				FileNameRuleSelectionStart = aSelectionStartBak + wrappedVarName.Length;
				FileNameRuleSelectionLength = 0;
#endif
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "変数メニュークリック時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// ファイル命名規則の変数の表示用文字列を生成
		// --------------------------------------------------------------------
		private List<String> CreateRuleVarLabels()
		{
			List<String> labels = new();
			TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
			Dictionary<String, String> varMap = YlCommon.CreateRuleDictionaryWithDescription();
			foreach (KeyValuePair<String, String> kvp in varMap)
			{
				String key;
				if (kvp.Key == YlConstants.RULE_VAR_ANY)
				{
					key = kvp.Key;
				}
				else
				{
					key = YlConstants.RULE_VAR_BEGIN + textInfo.ToTitleCase(kvp.Key) + YlConstants.RULE_VAR_END;
				}
				labels.Add(key + "（" + kvp.Value + "）");
			}
			return labels;
		}

		// --------------------------------------------------------------------
		// 文字列の中に含まれている命名規則の変数名を返す
		// 文字列の中には <Name> 形式で変数名を含んでいる必要がある
		// 返す変数名には <> は含まない
		// --------------------------------------------------------------------
		private String? FindRuleVarName(String str)
		{
			Dictionary<String, String?> varMap = YlCommon.CreateRuleDictionary();
			foreach (String kvp in varMap.Keys)
			{
				if (str.Contains(YlConstants.RULE_VAR_BEGIN + kvp + YlConstants.RULE_VAR_END, StringComparison.CurrentCultureIgnoreCase))
				{
					return kvp;
				}
			}
			if (str.Contains(YlConstants.RULE_VAR_ANY))
			{
				return YlConstants.RULE_VAR_ANY;
			}
			return null;
		}

		// --------------------------------------------------------------------
		// 変数名を <> で囲む
		// --------------------------------------------------------------------
		private String WrapVarName(String varName)
		{
			if (varName == YlConstants.RULE_VAR_ANY)
			{
				return YlConstants.RULE_VAR_ANY;
			}
			else
			{
				TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
				return YlConstants.RULE_VAR_BEGIN + textInfo.ToTitleCase(varName) + YlConstants.RULE_VAR_END;
			}
		}

	}
}
