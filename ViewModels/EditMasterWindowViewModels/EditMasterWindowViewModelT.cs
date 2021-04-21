// ============================================================================
// 
// 楽曲情報データベースマスター詳細編集ウィンドウの ViewModel 基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet.Messaging.Windows;

using Microsoft.EntityFrameworkCore;

using Shinta;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public class EditMasterWindowViewModel<T> : EditMasterWindowViewModel where T : class, IRcMaster, new()
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditMasterWindowViewModel(MusicInfoContext musicInfoContext, DbSet<T> records)
				: base(YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()], musicInfoContext)
		{
			_records = records;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 選択可能な Master 群
		// Masters を設定する際は SetMasters() を使うこと
		public ObservableCollection<T> Masters { get; } = new();

		// 選択された Master
		private T? _selectedMaster;
		public T? SelectedMaster
		{
			get => _selectedMaster;
			set
			{
				if (RaisePropertyChangedIfSet(ref _selectedMaster, value))
				{
					SelectedMasterChanged();
				}
			}
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// 初期表示する Master
		public T? DefaultMaster { get; set; }

		// OK ボタンが押された時に選択されていたマスター
		public T? OkSelectedMaster { get; private set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー：OK ボタンがクリックされた
		// --------------------------------------------------------------------
		public override void ButtonOKClicked()
		{
			try
			{
				// Enter キーでボタンが押された場合はテキストボックスからフォーカスが移らずプロパティーが更新されないため強制フォーカス
				IsButtonOkFocused = true;

				CheckInput();

				// データベースをバックアップ
				MusicInfoContext.BackupDatabase();

				T master = new();
				PropertiesToRecord(master);
				Save(master);
				OkSelectedMaster = master;
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (OperationCanceledException excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "OK ボタンクリック時中止");
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + excep.Message);
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
				// デフォルト ID を選択
				if (DefaultMaster != null)
				{
					SelectedMaster = DefaultMaster;
				}
				else
				{
					SelectedMaster = Masters[0];
				}

			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "楽曲情報データベースマスター詳細編集ウィンドウ <T> 初期化時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// Masters の要素を設定
		// --------------------------------------------------------------------
		public void SetMasters(List<T> masters)
		{
			Masters.Clear();
			foreach (T master in masters)
			{
				if (String.IsNullOrEmpty(master.Id))
				{
					master.Id = NewIdForDisplay();
				}
				Masters.Add(master);
			}
		}

		// ====================================================================
		// protected メンバー変数
		// ====================================================================

		// 検索対象データベースレコード
		protected DbSet<T> _records;

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// テーブルに新規レコードを追加
		// --------------------------------------------------------------------
		protected void AddNewRecord(T newRecord)
		{
			YlCommon.InputIdPrefixIfNeededWithInvoke(this);
			newRecord.Id = YukaListerModel.Instance.EnvModel.YlSettings.PrepareLastId(_records);
			_records.Add(newRecord);
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS,
					YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "テーブル新規登録：" + newRecord.Id + " / " + newRecord.Name);
		}

		// --------------------------------------------------------------------
		// 入力値を確認する
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		protected virtual void CheckInput()
		{
			String? normalizedName = YlCommon.NormalizeDbString(Name);
			String? normalizedRuby = YlCommon.NormalizeDbRubyForMusicInfo(Ruby);
			String? normalizedKeyword = YlCommon.NormalizeDbString(Keyword);

			// 名前が入力されているか
			if (String.IsNullOrEmpty(normalizedName))
			{
				throw new Exception(_caption + "名を入力して下さい。");
			}

			// 同名の既存レコード数をカウント
			(List<T> dups, Int32 numDups) = GetSameNameRecords(normalizedName);

			// 同名が既に登録されている場合
			if (numDups > 0)
			{
				if (String.IsNullOrEmpty(normalizedKeyword))
				{
					// キーワードがなければ同名の登録は禁止
					throw new Exception(_caption + "「" + normalizedName + "」は既に登録されています。\n検索ワードを入力して識別できるようにしてください。");
				}
				else
				{
					// キーワードが同じものがあると登録は禁止
					foreach (T dup in dups)
					{
						if (dup.Id != SelectedMaster?.Id && dup.Keyword == normalizedKeyword)
						{
							throw new Exception("登録しようとしている" + _caption + "「" + normalizedName + "」は既に登録されており、検索ワードも同じです。\n"
									+ _caption + " ID を切り替えて登録済みの" + _caption + "を選択してください。\n"
									+ "同名の別" + _caption + "を登録しようとしている場合は、検索ワードを見分けが付くようにして下さい。");
						}
					}
				}
			}

			// フリガナとして使えない文字がある場合は警告
			WarnRubyDeletedIfNeeded(Ruby, normalizedRuby);
		}

		// --------------------------------------------------------------------
		// 同名の既存レコード数をカウント
		// ＜返値＞ numDups は、現在選択されているマスターは除いた数
		// --------------------------------------------------------------------
		protected (List<T> dups, Int32 numDups) GetSameNameRecords(String normalizedName)
		{
			// レコード一覧
			List<T> dups = DbCommon.SelectMastersByName(_records, normalizedName);

			// カウント
			Int32 numDups = 0;
			foreach (T dup in dups)
			{
				if (dup.Id != SelectedMaster?.Id)
				{
					numDups++;
				}
			}

			return (dups, numDups);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー：Name が変更された
		// --------------------------------------------------------------------
		protected override void NameChanged()
		{
			base.NameChanged();

			try
			{
				if (SelectedMaster == null)
				{
					return;
				}
				String? normalizedName = YlCommon.NormalizeDbString(Name);
				if (String.IsNullOrEmpty(normalizedName) || normalizedName == SelectedMaster.Name)
				{
					return;
				}

				// 編集中の ID 以外で同名があるか検索
				List<T> dups = DbCommon.SelectMastersByName(_records, normalizedName);
				Int32 numDups = 0;
				foreach (T dup in dups)
				{
					if (dup.Id != SelectedMaster.Id)
					{
						numDups++;
					}
				}

				// 確認
				if (String.IsNullOrEmpty(SelectedMaster.Name))
				{
					if (numDups > 0)
					{
						// 空の名前から変更しようとしている場合は、同名がある場合のみ警告
						YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Warning, "登録しようとしている" + _caption + "名「" + normalizedName
								+ "」は既にデータベースに登録されています。\n" + _caption + "名は同じでも" + _caption + "自体が異なる場合は、このまま作業を続行して下さい。\n"
								+ "それ以外の場合は、重複登録を避けるために、" + _caption + " ID コンボボックスから既存の" + _caption + "情報を選択して下さい。");
					}
				}
				else
				{
					String? add = null;
					if (numDups > 0)
					{
						add = "\n\n【注意】\n変更後の名前は既にデータベースに登録されています。";
					}
					if (MessageBox.Show(_caption + "名を「" + SelectedMaster.Name + "」から「" + normalizedName + "」に変更しますか？" + add, "確認",
							MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
					{
						Name = SelectedMaster.Name;
						return;
					}
				}

				// 同名のレコードが編集対象になっていない場合は追加する
				foreach (T dup in dups)
				{
					if (Masters.FirstOrDefault(x => x.Id == dup.Id) != null)
					{
						continue;
					}

					Masters.Add(dup);
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "Name 変更時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// プロパティーの内容を Master に格納
		// --------------------------------------------------------------------
		protected virtual void PropertiesToRecord(T master)
		{
			if (SelectedMaster == null)
			{
				throw new Exception("ID が選択されていません。");
			}

			// IRcBase
			master.Id = SelectedMaster.Id;
			master.Import = false;
			master.Invalid = false;
			master.UpdateTime = YlConstants.INVALID_MJD;
			master.Dirty = true;

			// IRcMaster
			master.Name = YlCommon.NormalizeDbString(Name);
			master.Ruby = YlCommon.NormalizeDbRubyForMusicInfo(Ruby);
			master.RubyForSearch = YlCommon.NormalizeDbRubyForSearch(Ruby);
			master.Keyword = YlCommon.NormalizeDbString(Keyword);
		}

		// --------------------------------------------------------------------
		// Master の内容をプロパティーに反映
		// --------------------------------------------------------------------
		protected virtual void RecordToProperties(T master)
		{
			Ruby = master.Ruby;
			Name = master.Name;
			Keyword = master.Keyword;
		}

		// --------------------------------------------------------------------
		// レコード保存
		// --------------------------------------------------------------------
		protected virtual void Save(T master)
		{
			if (master.Id == NewIdForDisplay())
			{
				// 新規登録
				AddNewRecord(master);
			}
			else
			{
				T? existRecord = DbCommon.SelectBaseById(_records, master.Id, true);
				if (existRecord == null)
				{
					throw new Exception("更新対象の" + YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "レコードが見つかりません：" + master.Id);
				}
				if (DbCommon.IsRcMasterUpdated(existRecord, master))
				{
					// 更新（既存のレコードが無効化されている場合は有効化も行う）
					UpdateExistRecord(existRecord, master);
				}
			}

			_musicInfoContext.SaveChanges();
		}

		// --------------------------------------------------------------------
		// レコードを更新（既存のレコードが無効化されている場合は有効化も行う前提）
		// --------------------------------------------------------------------
		protected void UpdateExistRecord(T existRecord, T newRecord)
		{
			Debug.Assert(!newRecord.Invalid, "UpdateExistRecord() invalid");

			newRecord.UpdateTime = existRecord.UpdateTime;
			Common.ShallowCopy(newRecord, existRecord);
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS,
					YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "テーブル更新：" + newRecord.Id + " / " + newRecord.Name);
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// ====================================================================
		// private メンバー関数
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// 選択された Master
		// --------------------------------------------------------------------
		private T SelectedMaster()
		{
			foreach (T master in Masters)
			{
				if (master.Id == SelectedId || String.IsNullOrEmpty(master.Id) && SelectedId == NewIdForDisplay())
				{
					return master;
				}
			}

			throw new Exception("Master が選択されていません。");
		}
#endif

		// --------------------------------------------------------------------
		// イベントハンドラー：SelectedMaster が変更された
		// --------------------------------------------------------------------
		private void SelectedMasterChanged()
		{
			try
			{
				if (SelectedMaster == null)
				{
					return;
				}

				RecordToProperties(SelectedMaster);
				UpdateIdInfo();
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "SelectedMaster 変更時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// IdInfo を更新
		// --------------------------------------------------------------------
		private void UpdateIdInfo()
		{
			if (Masters.Count <= 1)
			{
				IdInfo = null;
			}
			else if (Masters.Count == 2)
			{
				if (SelectedMaster?.Id == NewIdForDisplay())
				{
					IdInfo = "（同名の登録が既にあります）";
				}
				else
				{
					IdInfo = null;
				}
			}
			else
			{
				IdInfo = "（同名の登録が複数あります）";
			}
		}

		// --------------------------------------------------------------------
		// ルビの一部が削除されたら警告
		// ＜例外＞ OperationCanceledException
		// --------------------------------------------------------------------
		private void WarnRubyDeletedIfNeeded(String? originalRuby, String? normalizedRuby)
		{
			if (!String.IsNullOrEmpty(originalRuby)
					&& (String.IsNullOrEmpty(normalizedRuby) || originalRuby.Length != normalizedRuby.Length))
			{
				if (MessageBox.Show("フリガナはカタカナのみ登録可能のため、カタカナ以外は削除されます。\n"
						+ originalRuby + " → " + normalizedRuby + "\nよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					throw new OperationCanceledException();
				}
			}
		}

	}
}
