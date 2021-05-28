// ============================================================================
// 
// FolderTreeControl を DataGrid に表示するためのコントロール
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using YukaLister.Views.CustomControls;

namespace YukaLister.Views.DataGridControls
{
	public class DataGridFtcColumn : DataGridBoundColumn
	{
		// ====================================================================
		// static コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// static コンストラクター
		// --------------------------------------------------------------------
		static DataGridFtcColumn()
		{
			ElementStyleProperty.OverrideMetadata(typeof(DataGridFtcColumn), new FrameworkPropertyMetadata(DefaultElementStyle));
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ElementStyle（編集モードではない状態のスタイル）のデフォルト値
		private static Style? _defaultElementStyle;
		public static Style DefaultElementStyle
		{
			get
			{
				if (_defaultElementStyle == null)
				{
					Style style = new(typeof(FolderTreeControl));
					style.Seal();
					_defaultElementStyle = style;
				}

				return _defaultElementStyle;
			}
		}

		// ====================================================================
		// protected メンバー関数（DataGridColumn 実装）
		// ====================================================================

		// --------------------------------------------------------------------
		// 編集モードでデータを表示する要素を生成
		// --------------------------------------------------------------------
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			throw new NotImplementedException();
		}

		// --------------------------------------------------------------------
		// 編集モードではない状態でデータを表示する要素を生成
		// cell: 要素を格納するセル, dataItem: 行データ
		// --------------------------------------------------------------------
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			FolderTreeControl ftc = new();
			ftc.Style = DefaultElementStyle;
			ApplyBinding(ftc, FolderTreeControl.TargetFolderInfoProperty);
			return ftc;
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 要素にデータをバインディング
		// --------------------------------------------------------------------
		private void ApplyBinding(DependencyObject target, DependencyProperty property)
		{
			BindingBase binding = Binding;
			if (binding != null)
			{
				BindingOperations.SetBinding(target, property, binding);
			}
			else
			{
				BindingOperations.ClearBinding(target, property);
			}
		}
	}
}
