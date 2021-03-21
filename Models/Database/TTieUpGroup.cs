// ============================================================================
// 
// タイアップグループマスターテーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukaLister.Models.Database
{
	[Table(TABLE_NAME_TIE_UP_GROUP)]
	public class TTieUpGroup : IRcMaster
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// タイアップグループ ID
		[Key]
		[Column(FIELD_NAME_TIE_UP_GROUP_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_TIE_UP_GROUP_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_TIE_UP_GROUP_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// IRcMaster
		// --------------------------------------------------------------------

		// タイアップグループ名
		[Column(FIELD_NAME_TIE_UP_GROUP_NAME)]
		public String? Name { get; set; }

		// タイアップグループフリガナ
		[Column(FIELD_NAME_TIE_UP_GROUP_RUBY)]
		public String? Ruby { get; set; }

		// 検索ワード
		[Column(FIELD_NAME_TIE_UP_GROUP_KEYWORD)]
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

		public const String TABLE_NAME_TIE_UP_GROUP = "t_tie_up_group";
		public const String FIELD_NAME_TIE_UP_GROUP_ID = "tie_up_group_id";
		public const String FIELD_NAME_TIE_UP_GROUP_IMPORT = "tie_up_group_import";
		public const String FIELD_NAME_TIE_UP_GROUP_INVALID = "tie_up_group_invalid";
		public const String FIELD_NAME_TIE_UP_GROUP_UPDATE_TIME = "tie_up_group_update_time";
		public const String FIELD_NAME_TIE_UP_GROUP_DIRTY = "tie_up_group_dirty";
		public const String FIELD_NAME_TIE_UP_GROUP_NAME = "tie_up_group_name";
		public const String FIELD_NAME_TIE_UP_GROUP_RUBY = "tie_up_group_ruby";
		public const String FIELD_NAME_TIE_UP_GROUP_KEYWORD = "tie_up_group_keyword";
	}
}
