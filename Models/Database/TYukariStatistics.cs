// ============================================================================
// 
// ゆかり統計テーブル
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using YukaLister.Models.Database.Masters;
using YukaLister.Models.SharedMisc;

namespace YukaLister.Models.Database
{
	[Table(TABLE_NAME_YUKARI_STATISTICS)]
	public class TYukariStatistics : IRcBase
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// IRcBase
		// --------------------------------------------------------------------

		// ゆかり統計 ID
		[Key]
		[Column(FIELD_NAME_YUKARI_STATISTICS_ID)]
		public String Id { get; set; } = String.Empty;

		// インポートフラグ
		[Column(FIELD_NAME_YUKARI_STATISTICS_IMPORT)]
		public Boolean Import { get; set; }

		// 無効フラグ
		[Column(FIELD_NAME_YUKARI_STATISTICS_INVALID)]
		public Boolean Invalid { get; set; }

		// 更新日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_YUKARI_STATISTICS_UPDATE_TIME)]
		public Double UpdateTime { get; set; }

		// Dirty フラグ
		[Column(FIELD_NAME_YUKARI_STATISTICS_DIRTY)]
		public Boolean Dirty { get; set; }

		// --------------------------------------------------------------------
		// TYukariStatistics 独自
		// --------------------------------------------------------------------

		// request.db フルパス
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_DATABASE_PATH)]
		public String RequestDatabasePath { get; set; } = String.Empty;

		// 推定予約日時 UTC（修正ユリウス日）
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_TIME)]
		public Double RequestTime { get; set; }

		// 属性確認済
		[Column(FIELD_NAME_YUKARI_STATISTICS_ATTRIBUTES_DONE)]
		public Boolean AttributesDone { get; set; }

		// ルーム名
		[Column(FIELD_NAME_YUKARI_STATISTICS_ROOM_NAME)]
		public String? RoomName { get; set; }

#if false
		// ID 接頭辞
		// ゆかり統計 ID から取得することもできるが、別列にしておくほうが既存存在判定が楽 → と思ったが不要そう
		[Column(FIELD_NAME_YUKARI_STATISTICS_ID_PREFIX)]
		public String IdPrefix { get; set; } = String.Empty;
#endif

#if false
		// 同期アカウント名（サーバー側で書き込む）
		[Column(FIELD_NAME_YUKARI_STATISTICS_SYNC_ACCOUNT_NAME)]
		public String? SyncAccountName { get; set; }
#endif

		// --------------------------------------------------------------------
		// request.db 由来
		// カラム名はゆかり側が変更された際に影響されないよう独自にする
		// --------------------------------------------------------------------

		// 予約 ID（request.db の ID）
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_ID)]
		public Int32 RequestId { get; set; }

		// 予約動画フルパス
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_MOVIE_PATH)]
		public String RequestMoviePath { get; set; } = String.Empty;

		// 予約者
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_SINGER)]
		public String? RequestSinger { get; set; }

		// 予約コメント
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_COMMENT)]
		public String? RequestComment { get; set; }

		// 予約順
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_ORDER)]
		public Int32 RequestOrder { get; set; }

		// キー
		[Column(FIELD_NAME_YUKARI_STATISTICS_REQUEST_KEY_CHANGE)]
		public Int32 RequestKeyChange { get; set; }

		// --------------------------------------------------------------------
		// TFound 由来
		// --------------------------------------------------------------------

		// カラオケ動画制作者
		[Column(FIELD_NAME_YUKARI_STATISTICS_WORKER)]
		public String? Worker { get; set; }

		// --------------------------------------------------------------------
		// TSong + TTieUp 由来
		// --------------------------------------------------------------------

		// リリース日（修正ユリウス日）
		[Column(FIELD_NAME_YUKARI_STATISTICS_SONG_RELEASE_DATE)]
		public Double SongReleaseDate { get; set; }

		// カテゴリー名
		[Column(FIELD_NAME_YUKARI_STATISTICS_CATEGORY_NAME)]
		public String? CategoryName { get; set; }

		// --------------------------------------------------------------------
		// TTieUp 由来
		// --------------------------------------------------------------------

		// タイアップ名
		[Column(FIELD_NAME_YUKARI_STATISTICS_TIE_UP_NAME)]
		public String? TieUpName { get; set; }

		// 年齢制限（○歳以上対象）
		[Column(FIELD_NAME_YUKARI_STATISTICS_AGE_LIMIT)]
		public Int32 TieUpAgeLimit { get; set; } = YlConstants.AGE_LIMIT_DEFAULT;

		// --------------------------------------------------------------------
		// TMaker 由来
		// --------------------------------------------------------------------

		// 制作会社名
		[Column(FIELD_NAME_YUKARI_STATISTICS_MAKER_NAME)]
		public String? MakerName { get; set; }

		// --------------------------------------------------------------------
		// TTieUpGroup 由来
		// --------------------------------------------------------------------

		// タイアップグループ名
		[Column(FIELD_NAME_YUKARI_STATISTICS_TIE_UP_GROUP_NAME)]
		public String? TieUpGroupName { get; set; }

		// --------------------------------------------------------------------
		// TSong 由来
		// --------------------------------------------------------------------

		// 楽曲名
		[Column(FIELD_NAME_YUKARI_STATISTICS_SONG_NAME)]
		public String? SongName { get; set; }

		// 摘要
		[Column(FIELD_NAME_YUKARI_STATISTICS_SONG_OP_ED)]
		public String? SongOpEd { get; set; }

		// --------------------------------------------------------------------
		// TPerson 由来
		// --------------------------------------------------------------------

		// 歌手名
		[Column(FIELD_NAME_YUKARI_STATISTICS_ARTIST_NAME)]
		public String? ArtistName { get; set; }

		// 作詞者名
		[Column(FIELD_NAME_YUKARI_STATISTICS_LYRIST_NAME)]
		public String? LyristName { get; set; }

		// 作曲者名
		[Column(FIELD_NAME_YUKARI_STATISTICS_COMPOSER_NAME)]
		public String? ComposerName { get; set; }

		// 編曲者名
		[Column(FIELD_NAME_YUKARI_STATISTICS_ARRANGER_NAME)]
		public String? ArrangerName { get; set; }

		// ====================================================================
		// public 定数
		// ====================================================================

		public const String TABLE_NAME_YUKARI_STATISTICS = "t_yukari_statistics";
		public const String FIELD_PREFIX_YUKARI_STATISTICS = "yukari_statistics_";

		// 同期対象なのですべてのフィールドに FIELD_PREFIX_YUKARI_STATISTICS を付け、サーバーのフィールド名に合わせる（TFound のフィールド名を流用しない）
		public const String FIELD_NAME_YUKARI_STATISTICS_ID = FIELD_PREFIX_YUKARI_STATISTICS + YlConstants.FIELD_SUFFIX_ID;
		public const String FIELD_NAME_YUKARI_STATISTICS_IMPORT = FIELD_PREFIX_YUKARI_STATISTICS + YlConstants.FIELD_SUFFIX_IMPORT;
		public const String FIELD_NAME_YUKARI_STATISTICS_INVALID = FIELD_PREFIX_YUKARI_STATISTICS + YlConstants.FIELD_SUFFIX_INVALID;
		public const String FIELD_NAME_YUKARI_STATISTICS_UPDATE_TIME = FIELD_PREFIX_YUKARI_STATISTICS + YlConstants.FIELD_SUFFIX_UPDATE_TIME;
		public const String FIELD_NAME_YUKARI_STATISTICS_DIRTY = FIELD_PREFIX_YUKARI_STATISTICS + YlConstants.FIELD_SUFFIX_DIRTY;
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_DATABASE_PATH = FIELD_PREFIX_YUKARI_STATISTICS + "request_database_path";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_TIME = FIELD_PREFIX_YUKARI_STATISTICS + "request_time";
		public const String FIELD_NAME_YUKARI_STATISTICS_ATTRIBUTES_DONE = FIELD_PREFIX_YUKARI_STATISTICS + "attributes_done";
		public const String FIELD_NAME_YUKARI_STATISTICS_ROOM_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "room_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_ID = FIELD_PREFIX_YUKARI_STATISTICS + "request_id";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_MOVIE_PATH = FIELD_PREFIX_YUKARI_STATISTICS + "request_movie_path";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_SINGER = FIELD_PREFIX_YUKARI_STATISTICS + "request_singer";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_COMMENT = FIELD_PREFIX_YUKARI_STATISTICS + "request_comment";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_ORDER = FIELD_PREFIX_YUKARI_STATISTICS + "request_order";
		public const String FIELD_NAME_YUKARI_STATISTICS_REQUEST_KEY_CHANGE = FIELD_PREFIX_YUKARI_STATISTICS + "request_key_change";
		public const String FIELD_NAME_YUKARI_STATISTICS_WORKER = FIELD_PREFIX_YUKARI_STATISTICS + "worker";
		public const String FIELD_NAME_YUKARI_STATISTICS_SONG_RELEASE_DATE = FIELD_PREFIX_YUKARI_STATISTICS + "release_date";
		public const String FIELD_NAME_YUKARI_STATISTICS_CATEGORY_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "category_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_TIE_UP_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "tie_up_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_AGE_LIMIT = FIELD_PREFIX_YUKARI_STATISTICS + "age_limit";
		public const String FIELD_NAME_YUKARI_STATISTICS_MAKER_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "maker_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_TIE_UP_GROUP_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "tie_up_group_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_SONG_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "song_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_SONG_OP_ED = FIELD_PREFIX_YUKARI_STATISTICS + "op_ed";
		public const String FIELD_NAME_YUKARI_STATISTICS_ARTIST_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "artist_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_LYRIST_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "lyrist_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_COMPOSER_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "composer_name";
		public const String FIELD_NAME_YUKARI_STATISTICS_ARRANGER_NAME = FIELD_PREFIX_YUKARI_STATISTICS + "arranger_name";
	}
}
