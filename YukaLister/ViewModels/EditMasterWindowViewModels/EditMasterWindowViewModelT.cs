﻿// ============================================================================
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
using System.Threading.Tasks;
using System.Windows;

using YukaLister.Models.Database;
using YukaLister.Models.DatabaseAssist;
using YukaLister.Models.DatabaseContexts;
using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.ViewModels.EditMasterWindowViewModels
{
	internal abstract class EditMasterWindowViewModel<T> : EditMasterWindowViewModel where T : class, IRcMaster, new()
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public EditMasterWindowViewModel(MusicInfoContextDefault musicInfoContext, DbSet<T> records)
		{
			Debug.Assert(musicInfoContext.ChangeTracker.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll, "EditMasterWindowViewModel() bad QueryTrackingBehavior");
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

		// ヘルプ引数
		private String? _helpCommandParameter;
		public String? HelpCommandParameter
		{
			get => _helpCommandParameter;
			set => RaisePropertyChangedIfSet(ref _helpCommandParameter, value);
		}

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

		#region ヘルプリンクの制御
		public static ListenerCommand<String>? HelpClickedCommand
		{
			get => YlModel.Instance.EnvModel.HelpClickedCommand;
		}
		#endregion

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
					if (MessageBox.Show("この" + _caption + "「" + Name + "」を削除しますか？\n\n【注意】削除すると復活できません。",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						return;
					}
				}

				// データベースをバックアップ
				_musicInfoContext.BackupDatabase();

				// 無効化
				T master = new();
				PropertiesToRecord(master);
				Invalidate(master);
				Result = MessageBoxResult.OK;
				Messenger.Raise(new WindowActionMessage(Common.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "削除ボタンクリック時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

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
				// タイトルバー
				Title = _caption + "詳細情報の編集";

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

				_initialized = true;
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "楽曲情報データベースマスター詳細編集ウィンドウ <T> 初期化時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
		// protected 変数
		// ====================================================================

		// 編集対象の名称
		protected String _caption;

		// 楽曲情報データベースのコンテキスト（外部から指定されたもの）
		protected MusicInfoContextDefault _musicInfoContext;

		// 検索対象データベースレコード
		protected DbSet<T> _records;

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// テーブルに新規レコードを追加
		// --------------------------------------------------------------------
		protected async Task AddNewRecord(T newRecord)
		{
			await YlCommon.InputIdPrefixIfNeededWithInvoke(this);
			newRecord.Id = YlModel.Instance.EnvModel.YlSettings.PrepareLastId(_records);
			_records.Add(newRecord);
			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS,
					YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "テーブル新規登録：" + newRecord.Id + " / " + newRecord.Name);
		}

		// --------------------------------------------------------------------
		// 入力値を確認する
		// ＜例外＞ Exception, OperationCanceledException
		// --------------------------------------------------------------------
		protected override void CheckProperties()
		{
			base.CheckProperties();

			if (SelectedMaster == null)
			{
				throw new Exception("内部エラー：登録対象が指定されていません。");
			}

			String? normalizedName = YlCommon.NormalizeDbString(Name);
			(String? normalizedRuby, Boolean allRuby, _) = YlCommon.NormalizeDbRubyForMusicInfo(Ruby);
			String? normalizedKeyword = YlCommon.NormalizeDbString(Keyword);

			// 名前が入力されているか
			if (String.IsNullOrEmpty(normalizedName))
			{
				throw new Exception(_caption + "名を入力して下さい。");
			}

			// 同名の既存レコード数をカウント
			(List<T> dups, Int32 numDups) = GetSameNameRecordsCaseInsensitive(normalizedName);

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
					if (dup.Id != SelectedMaster.Id && dup.Keyword == normalizedKeyword)
					{
						throw new Exception("登録しようとしている" + _caption + "「" + normalizedName + "」は既に登録されており、検索ワードも同じです。\n"
								+ _caption + " ID を切り替えて登録済みの" + _caption + "を選択してください。\n"
								+ "同名の別" + _caption + "を登録しようとしている場合は、検索ワードを見分けが付くようにして下さい。");
					}
				}

				// 新規 ID の場合は確認
				// ID 切替で新規を選んだ場合は、今までに警告が表示されていないため、この確認は必要
				if (SelectedMaster.Id == NewIdForDisplay())
				{
					if (MessageBox.Show("新規登録しようとしている" + _caption + "「" + normalizedName + "」と同名の" + _caption + "は既に登録されています。\n同名の" + _caption + "を追加で新規登録しますか？",
							"確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					{
						throw new OperationCanceledException();
					}
				}
			}

			// フリガナとして使えない文字がある場合は警告
			WarnRubyDeletedIfNeeded(allRuby, Ruby, normalizedRuby);

			// 想定しているマスターの型と異なる場合はエラー（本来あってはならないが念のため）
			Int32 tableIndex = DbCommon.MusicInfoTableIndex<T>();
			if (SelectedMaster.Id != NewIdForDisplay() && !SelectedMaster.Id.Contains(YlConstants.MUSIC_INFO_ID_SECOND_PREFIXES[tableIndex])
					&& !(tableIndex == (Int32)MusicInfoTables.TTieUp && SelectedMaster.Id.Contains("_P_")))
			{
				throw new Exception("内部エラー：登録対象の型が異なっています。");
			}
		}

		// --------------------------------------------------------------------
		// 同名（大文字小文字違いを含む）の既存レコード数をカウント
		// ＜返値＞ numDups は、現在選択されているマスターは除いた数
		// --------------------------------------------------------------------
		protected (List<T> dups, Int32 numDups) GetSameNameRecordsCaseInsensitive(String normalizedName)
		{
			// レコード一覧
			List<T> dups = DbCommon.SelectMastersByNameCaseInsensitive(_records, normalizedName);

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
			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS,
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
			(master.Ruby, _, _) = YlCommon.NormalizeDbRubyForMusicInfo(Ruby);
			(master.RubyForSearch, _, _) = YlCommon.NormalizeDbRubyForSearch(Ruby);

			// 検索ワードはカンマごとに正規化する
			Debug.Assert(master.Keyword == null, "PropertiesToRecord() master.Keyword already set");
			if (!String.IsNullOrEmpty(Keyword))
			{
				String[] keywords = Keyword.Split(YlConstants.VAR_VALUE_DELIMITER[0], StringSplitOptions.RemoveEmptyEntries);
				master.Keyword = String.Join(YlConstants.VAR_VALUE_DELIMITER[0], keywords.Select(x => YlCommon.NormalizeDbString(x)));
				master.KeywordRubyForSearch = YlCommon.KeywordRubyForSearch(master.Keyword);
			}
		}

		// --------------------------------------------------------------------
		// Master の内容をプロパティーに反映
		// --------------------------------------------------------------------
		protected virtual void RecordToProperties(T master)
		{
			Name = master.Name;
			Ruby = master.Ruby;
			if (String.IsNullOrEmpty(Ruby))
			{
				SetRubyFromName();
			}
			Keyword = master.Keyword;
		}

		// --------------------------------------------------------------------
		// レコード保存
		// --------------------------------------------------------------------
		protected virtual async Task SaveRecord(T master)
		{
			if (master.Id == NewIdForDisplay())
			{
				// 新規登録
				await AddNewRecord(master);
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
		// 設定を保存
		// --------------------------------------------------------------------
		protected override async void SaveSettings()
		{
			base.SaveSettings();

			try
			{
				// データベースをバックアップ
				_musicInfoContext.BackupDatabase();

				// 保存
				T master = new();
				PropertiesToRecord(master);
				await SaveRecord(master);
				OkSelectedMaster = master;
			}
			catch (Exception ex)
			{
				DbCommon.LogDatabaseExceptionIfCan(ex);
				throw;
			}
		}

		// --------------------------------------------------------------------
		// レコードを更新（既存のレコードが無効化されている場合は有効化も行う前提）
		// --------------------------------------------------------------------
		protected void UpdateExistRecord(T existRecord, T newRecord)
		{
			Debug.Assert(!newRecord.Invalid, "UpdateExistRecord() invalid");

			newRecord.UpdateTime = existRecord.UpdateTime;
			Common.ShallowCopyFields(newRecord, existRecord);
			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS,
					YlConstants.MUSIC_INFO_TABLE_NAME_LABELS[DbCommon.MusicInfoTableIndex<T>()] + "テーブル更新：" + newRecord.Id + " / " + newRecord.Name);
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// 初期化が完了した
		private Boolean _initialized;

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// イベントハンドラー：Name が変更された
		// ID 切り替えでは Name は変わらない前提（変わるのであればフリガナ更新はまずい）
		// --------------------------------------------------------------------
		private void NameChanged()
		{
			try
			{
				if (!_initialized)
				{
					return;
				}
				if (SelectedMaster == null)
				{
					return;
				}
				String? normalizedName = YlCommon.NormalizeDbString(Name);
				if (String.IsNullOrEmpty(normalizedName) || normalizedName == SelectedMaster.Name)
				{
					return;
				}

				// 編集中の ID 以外で同名（大文字小文字違い含む）があるか検索
				(List<T> dups, Int32 numDups) = GetSameNameRecordsCaseInsensitive(normalizedName);

				// 確認
				if (String.IsNullOrEmpty(SelectedMaster.Name))
				{
					if (numDups > 0)
					{
						// 空の名前から変更しようとしている場合は、同名がある場合のみ警告
						_logWriter?.ShowLogMessage(TraceEventType.Warning, "登録しようとしている" + _caption + "名「" + normalizedName
								+ "」は既にデータベースに登録されています。\n" + _caption + "名は同じでも" + _caption + "自体が異なる場合は、このまま作業を続行して下さい。\n"
								+ "それ以外の場合は、重複登録を避けるために、" + _caption + " ID コンボボックスから既存の" + _caption + "情報を選択して下さい。");
					}
				}
				else
				{
					String? add = null;
					if (numDups > 0)
					{
						add = "【注意】\n変更後の" + _caption + "名「" + normalizedName + "」は既にデータベースに登録されています。\n\n";
					}
					if (MessageBox.Show(add + _caption + "名を「" + SelectedMaster.Name + "」から「" + normalizedName + "」に変更しますか？", "確認",
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

				// フリガナ更新
				SetRubyFromName();
			}
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "Name 変更時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
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
				_logWriter?.ShowLogMessage(TraceEventType.Error, "SelectedMaster 変更時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}


		// --------------------------------------------------------------------
		// 名前からフリガナを取得して設定
		// --------------------------------------------------------------------
		private void SetRubyFromName()
		{
			using RubyReconverter rubyReconverter = new();
			(String? autoRuby, _, Boolean headRuby) = YlCommon.NormalizeDbRubyForMusicInfo(rubyReconverter.Reconvert(YlCommon.NormalizeDbString(Name)));
			if (!String.IsNullOrEmpty(autoRuby) && headRuby)
			{
				// 先頭がフリガナの場合のみ採用（先頭がフリガナでなかった場合、ユーザーが見逃してフリガナとして保存されてしまった場合、頭文字に影響が出るため）
				Ruby = autoRuby;
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
		private static void WarnRubyDeletedIfNeeded(Boolean allRuby, String? originalRuby, String? normalizedRuby)
		{
			if (!String.IsNullOrEmpty(originalRuby) && !allRuby)
			{
				if (MessageBox.Show("フリガナはカタカナのみ登録可能のため、カタカナ以外は削除されます。\n"
						+ originalRuby + " →\n" + normalizedRuby + "\nよろしいですか？", "確認",
						MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					throw new OperationCanceledException();
				}
			}
		}
	}
}
