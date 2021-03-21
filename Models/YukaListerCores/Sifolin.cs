﻿// ============================================================================
// 
// ネビュラコア：検索データ作成担当
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Models.YukaListerCores
{
	public class Sifolin : YukaListerCore
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Sifolin()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================


		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ネビュラコア（検索データ作成）のメインルーチン
		// --------------------------------------------------------------------
		protected override void CoreMain()
		{
			while (true)
			{
				MainEvent.WaitOne();
				Debug.WriteLine("Sifolin.CoreMain() 進行");
				if (YukaListerModel.Instance.EnvModel.AppCancellationTokenSource.Token.IsCancellationRequested)
				{
					YukaListerModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " の稼働を終了します。");
					return;
				}

				// ToDo: 削除系未実装


			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// メモリ DB 更新フラグ
		private Boolean _isMemoryDbDirty;
	}
}
