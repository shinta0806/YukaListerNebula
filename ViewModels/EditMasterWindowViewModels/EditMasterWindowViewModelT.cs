// ============================================================================
// 
// 楽曲情報データベースマスター詳細編集ウィンドウの ViewModel 基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// プログラム中では本クラスではなく派生クラスを使うこと。
// ----------------------------------------------------------------------------

using Livet.Commands;
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
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	public abstract class EditMasterWindowViewModel<T> : EditMasterWindowViewModel where T : class, IRcMaster, new()
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public EditMasterWindowViewModel(MusicInfoContext musicInfoContext, DbSet<T> records)
		{
			_caption = YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()];
			_musicInfoContext = musicInfoContext;
			_records = records;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ID キャプション
		private String? _idCaption;
		public String? IdCaption
		{
			get => _idCaption;
			set => RaisePropertyChangedIfSet(ref _idCaption, value);
		}

		// ID の補足情報
		private String? _idInfo;
		public String? IdInfo
		{
			get => _idInfo;
			set => RaisePropertyChangedIfSet(ref _idInfo, value);
		}

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

		// フリガナ
		private String? _ruby;
		public String? Ruby
		{
			get => _ruby;
			set => RaisePropertyChangedIfSet(ref _ruby, value);
		}

		// 名前キャプション
		private String? _nameCaption;
		public String? NameCaption
		{
			get => _nameCaption;
			set => RaisePropertyChangedIfSet(ref _nameCaption, value);
		}

		// 名前
		private String? _name;
		public String? Name
		{
			get => _name;
			set
			{
				if (RaisePropertyChangedIfSet(ref _name, value))
				{
					NameChanged();
				}
			}
		}

		// 名前のヒント
		private String? _nameHint;
		public String? NameHint
		{
			get => _nameHint;
			set => RaisePropertyChangedIfSet(ref _nameHint, value);
		}

		// 検索ワード
		private String? _keyword;
		public String? Keyword
		{
			get => _keyword;
			set => RaisePropertyChangedIfSet(ref _keyword, value);
		}

		// 検索ワードのヒント
		private String? _keywordHint;
		public String? KeywordHint
		{
			get => _keywordHint;
			set => RaisePropertyChangedIfSet(ref _keywordHint, value);
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
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// 初期表示する Master の Id
		// T 型にするとインスタンス違いでコンボボックスに反映されない事故が起こるので Id にする
		public String? DefaultMasterId { get; set; }

		// OK ボタンが押された時に選択されていたマスター
		public T? OkSelectedMaster { get; private set; }

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region 削除ボタンの制御
		private ViewModelCommand? _buttonDeleteClickedCommand;

		public ViewModelCommand ButtonDeleteClickedCommand
		{
			get
			{
				if (_buttonDeleteClickedCommand == null)
				{
					_buttonDeleteClickedCommand = new ViewModelCommand(ButtonDeleteClicked);
				}
				return _buttonDeleteClickedCommand;
			}
		}

		public void ButtonDeleteClicked()
		{
			try
			{
				if (SelectedMaster == null)
				{
					return;
				}

				if (SelectedMaster.Id == NewIdForDisplay())
				{
					throw new Exception("新規" + _caption + "はまだ登録されていないので、削除の必要はありません。\nキャンセルボタンをクリックして編集をキャンセルしてください。");
				}

				T? sameNameNoSyncMaster = DbCommon.SelectMastersByName(_records, SelectedMaster.Name).FirstOrDefault(x => x.UpdateTime <= YlConstants.INVALID_MJD);
				if (SelectedMaster.UpdateTime > YlConstants.INVALID_MJD && sameNameNoSyncMaster != null)
				{
					// 削除しようとしているレコードが同期されたデータで、同名の同期されていないデータがある場合は警告
					if (MessageBox.Show("この" + _caption + "は同期されたデータです。\n同名の同期されていないデータ（ID: " + sameNameNoSyncMaster.Id + "）存在しています。\n"
							+ "名前が重複している" + _caption + "を整理しようとしている場合、同期されていないデータから削除することを検討してください。\n"
							+ "このまま同期されたデータを削除してしまってよろしいですか？\n（同期されていないデータから削除する場合は「いいえ」をクリックしてください）",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						return;
					}
				}
				else
				{
					// それ以外の場合は通常の確認
					if (MessageBox.Show("この" + _caption + "を削除しますか？",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						return;
					}
				}

				// データベースをバックアップ
				MusicInfoContext.BackupDatabase();

				// 無効化
				T master = new();
				PropertiesToRecord(master);
				Invalidate(master);
				IsOk = true;
				Messenger.Raise(new WindowActionMessage(YlConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "削除ボタンクリック時エラー：\n" + excep.Message);
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

				CheckInput();

				// データベースをバックアップ
				MusicInfoContext.BackupDatabase();

				// 保存
				T master = new();
				PropertiesToRecord(master);
				Save(master);
				IsOk = true;
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
		#endregion

		// ====================================================================
		// public メンバー関数
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
				Title = _caption + "詳細情報の編集";
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// キャプション
				IdCaption = _caption + " ID (_I)：";
				NameCaption = _caption + "名 (_N)：";

				// デフォルト ID を選択
				if (!String.IsNullOrEmpty(DefaultMasterId))
				{
					T? defaultMaster = Masters.SingleOrDefault(x => x.Id == DefaultMasterId);
					if (defaultMaster != null)
					{
						SelectedMaster = defaultMaster;
					}
					else
					{
						SelectedMaster = Masters[0];
					}
				}
				else
				{
					if (Masters.Count > 1)
					{
						SelectedMaster = Masters[1];
					}
					else
					{
						SelectedMaster = Masters[0];
					}
				}

				// ヒント
				KeywordHint = "キーワード、コメント、略称など。複数入力する際は、半角カンマ「 , 」で区切って下さい。";
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

		// 編集対象の名称
		protected String _caption;

		// 楽曲情報データベースのコンテキスト
		protected MusicInfoContext _musicInfoContext;

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

				// 新規 ID の場合は確認
				// ID 切替で新規を選んだ場合は、今までに警告が表示されていないため、この確認は必要
				if (SelectedMaster?.Id == NewIdForDisplay())
				{
					if (MessageBox.Show("新規登録しようとしている" + _caption + "「" + normalizedName + "」と同名の" + _caption + "は既に登録されています。\n同名の" + _caption + "を追加で新規登録しますか？",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						throw new OperationCanceledException();
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
		// レコード無効化
		// --------------------------------------------------------------------
		protected virtual void Invalidate(T master)
		{
			Debug.Assert(master.Id != NewIdForDisplay(), "Invalidate() invalidating new item");
			T? existRecord = DbCommon.SelectBaseById(_records, master.Id, true);
			if (existRecord == null)
			{
				throw new Exception("削除対象の" + YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "レコードが見つかりません：" + master.Id);
			}

			// レコード自体を削除するのではなく、無効フラグを立てる
			existRecord.Invalid = true;
			existRecord.Dirty = true;
			_musicInfoContext.SaveChanges();
			YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS,
					YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "テーブル無効化：" + existRecord.Id + " / " + existRecord.Name);
		}

		// --------------------------------------------------------------------
		// 表示用新規 ID
		// --------------------------------------------------------------------
		protected String NewIdForDisplay()
		{
			return "（新規" + _caption + "）";
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
			master.KeywordRubyForSearch = YlCommon.KeywordRubyForSearch(Keyword);
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
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー：Name が変更された
		// --------------------------------------------------------------------
		private void NameChanged()
		{
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
