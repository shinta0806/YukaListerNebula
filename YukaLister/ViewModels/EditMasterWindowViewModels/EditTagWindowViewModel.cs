﻿// ============================================================================
// 
// タグ詳細情報の編集ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ビューは EditMasterWindow を使う。
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using YukaLister.Models.Database.Masters;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	internal class EditTagWindowViewModel : EditMasterWindowViewModel<TTag>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EditTagWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<TTag> records)
				: base(musicInfoContext, records)
		{
		}

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
				// ヘルプ
				HelpCommandParameter = "TagjouhounoShinkitourokutoHenkou";

				// ヒント
				NameHint = "一個分のタグ名のみを入力して下さい（複数タグをまとめないで下さい）。";
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タグ詳細情報編集ウィンドウ初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力値を確認する
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		protected override void CheckProperties()
		{
			// タグの場合、名前の重複は無条件で NG のため、基底より先にチェック
			String? normalizedName = YlCommon.NormalizeDbString(Name);
			if (!String.IsNullOrEmpty(normalizedName))
			{
				(_, Int32 numDups) = GetSameNameRecordsCaseInsensitive(normalizedName);
				if (numDups > 0)
				{
					throw new Exception(_caption + "「" + normalizedName + "」は既に登録されています。\n同じ名前の" + _caption + "は登録できません。");
				}
			}

			// 基底
			base.CheckProperties();
		}
	}
}
