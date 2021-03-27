// ============================================================================
// 
// 人物マスターテーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YukaLister.Models.Database.Masters
{
	[Table(TABLE_NAME_PERSON)]
	public class TPerson : IRcMaster
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// 人物 ID
		[Key]
		[Column(FIELD_NAME_PERSON_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_PERSON_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_PERSON_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_PERSON_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_PERSON_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// 人物名
		[Column(FIELD_NAME_PERSON_NAME)]
		public String? Name { get; set; }

		// 人物フリガナ
		[Column(FIELD_NAME_PERSON_RUBY)]
		public String? Ruby { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_PERSON_KEYWORD)]
		public String? Keyword { get; set; }

		// 同名の区別が付くように DisplayName を設定する
		[NotMapped]
		public Boolean AvoidSameName { get; set; }

		// 表示名
		private String? _displayName;
		public String? DisplayName
		{
			get
			{
				if (String.IsNullOrEmpty(_displayName))
				{
					if (AvoidSameName)
					{
						_displayName = Name + "（" + (String.IsNullOrEmpty(Keyword) ? "キーワード無し" : Keyword) + "）";
					}
					else
					{
						_displayName = Name;
					}
				}
				return _displayName;
			}
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_PERSON = "t_person";
		public const String FIELD_NAME_PERSON_ID = "person_id";
		public const String FIELD_NAME_PERSON_IMPORT = "person_import";
		public const String FIELD_NAME_PERSON_INVALID = "person_invalid";
		public const String FIELD_NAME_PERSON_UPDATE_TIME = "person_update_time";
		public const String FIELD_NAME_PERSON_DIRTY = "person_dirty";
		public const String FIELD_NAME_PERSON_NAME = "person_name";
		public const String FIELD_NAME_PERSON_RUBY = "person_ruby";
		public const String FIELD_NAME_PERSON_KEYWORD = "person_keyword";
	}
}
