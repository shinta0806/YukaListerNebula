// ============================================================================
// 
// テーブルのインターフェースクラス群
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

namespace YukaLister.Models.Database;

// ====================================================================
// 基礎テーブルのレコードインターフェース
// ====================================================================

internal interface IRcBase
{
	// ID
	String Id { get; set; }

	// インポートフラグ
	Boolean Import { get; set; }

	// 無効フラグ
	Boolean Invalid { get; set; }

	// 更新日時 UTC（修正ユリウス日）
	Double UpdateTime { get; set; }

	// Dirty フラグ
	Boolean Dirty { get; set; }
}

// ====================================================================
// マスターテーブルのレコードインターフェース
// ====================================================================

internal interface IRcMaster : IRcBase
{
	// 名
	String? Name { get; set; }

	// フリガナ
	String? Ruby { get; set; }

	// フリガナ（検索用）
	String? RubyForSearch { get; set; }

	// 検索ワード
	String? Keyword { get; set; }

	// 検索ワードフリガナ（検索用）
	// カンマ区切りされた検索ワードの各要素のうち、フリガナとして使用可能かつフリガナと異なる表記のもののみをカンマ区切りで連結して格納
	String? KeywordRubyForSearch { get; set; }

	// --------------------------------------------------------------------
	// 以下はデータベースに保存しない
	// --------------------------------------------------------------------

	// 同名の区別が付くように表示する
	Boolean AvoidSameName { get; set; }

	// 表示名
	String? DisplayName { get; }
}

// ====================================================================
// カテゴリー持ちテーブルのレコードインターフェース
// ====================================================================

internal interface IRcCategorizable : IRcMaster
{
	// カテゴリー ID ＜参照項目＞
	String? CategoryId { get; set; }

	// リリース日（修正ユリウス日）
	Double ReleaseDate { get; set; }

	// --------------------------------------------------------------------
	// 以下はデータベースに保存しない
	// --------------------------------------------------------------------

	// 表示カテゴリー名（マスター一覧ウィンドウ用）
	String? DisplayCategoryName { get; }

	// 表示リリース日（マスター一覧ウィンドウ用）
	String? DisplayReleaseDate { get; }
}

// ====================================================================
// 別名テーブルのレコードインターフェース
// ====================================================================

internal interface IRcAlias : IRcBase
{
	// 別名
	String Alias { get; set; }

	// 元の ID ＜参照項目＞
	String OriginalId { get; set; }
}

// ====================================================================
// 紐付テーブルのレコードインターフェース
// ====================================================================

internal interface IRcSequence : IRcBase
{
	// 連番
	Int32 Sequence { get; set; }

	// ID ＜参照項目＞
	String LinkId { get; set; }
}
