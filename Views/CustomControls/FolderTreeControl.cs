// ============================================================================
// 
// フォルダー名を階層表示するためのコントロール
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using YukaLister.Models.SharedMisc;
using YukaLister.Models.YukaListerModels;

namespace YukaLister.Views.CustomControls
{
	public class FolderTreeControl : Control
	{
		// ====================================================================
		// static コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// static コンストラクター
		// --------------------------------------------------------------------
		static FolderTreeControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(FolderTreeControl), new FrameworkPropertyMetadata(typeof(FolderTreeControl)));
		}

		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public FolderTreeControl()
		{
			try
			{
				// ピクセルぴったり描画
				SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー階層表示コントロール生成時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ゆかり検索対象フォルダーの情報
		public static readonly DependencyProperty TargetFolderInfoProperty
				= DependencyProperty.Register("TargetFolderInfo", typeof(TargetFolderInfo), typeof(FolderTreeControl),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceTargetFolderInfoPropertyChanged));
		public TargetFolderInfo? TargetFolderInfo
		{
			get => (TargetFolderInfo?)GetValue(TargetFolderInfoProperty);
			set => SetValue(TargetFolderInfoProperty, value);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 制限範囲内でのコントロールサイズを返す
		// --------------------------------------------------------------------
		protected override Size MeasureOverride(Size constraint)
		{
			Size measure = new();

			// constraint が許せば、幅はエキスパンダーとある程度のフォルダー名を表示できる分欲しい
			// 実際の配置で幅に余裕がある場合はもっと広い幅で配置される模様
			measure.Width = Math.Min(constraint.Width, DEFAULT_WIDTH);

			// constraint が許せば、高さはフォントサイズ分欲しい
			measure.Height = Math.Min(constraint.Height, EXPANDER_SIZE);

			return measure;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			try
			{
				if (TargetFolderInfo == null || TargetFolderInfo.IsOpen == null)
				{
					return;
				}

				if (ExpanderRect().Contains(e.GetPosition(this)))
				{
					TargetFolderInfo.IsOpen = !TargetFolderInfo.IsOpen;
					InvalidateVisual();
				}
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー階層表示コントロール左ボタン押下時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 描画
		// --------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			try
			{
				// クリア
				drawingContext.DrawRectangle(Background, null, new(0, 0, ActualWidth, ActualHeight));
				if (TargetFolderInfo == null)
				{
					return;
				}

				// エキスパンダー
				Rect expanderRect = ExpanderRect();
				DrawExpander(drawingContext, expanderRect);

				// フォルダー名
				FormattedText text = new(TargetFolderInfo.Path, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, CreateDefaultTypeface(FontFamily),
						FontSize, Foreground, YlConstants.DPI);
				Double x = expanderRect.Right + MARGIN_WIDTH;
				Double y = (ActualHeight - text.Height) / 2;
				drawingContext.DrawText(text, new Point(x, y));
			}
			catch (Exception excep)
			{
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "フォルダー階層表示コントロール描画時エラー：\n" + excep.Message);
				YukaListerModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// エキスパンダーとある程度のフォルダー名を表示できる幅
		private const Double DEFAULT_WIDTH = 200;

		// エキスパンダーの幅・高さ
		private const Double EXPANDER_SIZE = 12;

		// エキスパンダーの線の太さ
		private const Double EXPANDER_THICKNESS = 4;

		// 横方向マージン
		private const Double MARGIN_WIDTH = 5;

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ViewModel 側で DependencyProperty が変更された（TargetFolderInfoProperty）
		// --------------------------------------------------------------------
		private static void SourceTargetFolderInfoPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is FolderTreeControl folderTreeControl)
			{
				folderTreeControl.InvalidateVisual();
			}
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// FontFamily の中でデフォルトの Typeface を取得
		// フォールバックした場合は指定された FontFamily とは異なることに注意
		// --------------------------------------------------------------------
		private Typeface CreateDefaultTypeface(FontFamily fontFamily)
		{
			FamilyTypeface? familyTypeface;

			// 線の太さが標準、かつ、横幅が標準
			familyTypeface = fontFamily.FamilyTypefaces.FirstOrDefault(x => x.Weight == FontWeights.Regular && x.Stretch == FontStretches.Medium);

			if (familyTypeface == null)
			{
				// 見つからない場合は、線の太さが標準なら何でも良いとする
				familyTypeface = fontFamily.FamilyTypefaces.FirstOrDefault(x => x.Weight == FontWeights.Regular);
			}

			if (familyTypeface == null)
			{
				// 見つからない場合は、何でも良いとする
				familyTypeface = fontFamily.FamilyTypefaces.FirstOrDefault();
			}

			if (familyTypeface == null)
			{
				// それでも見つからない場合は、フォールバック
				return new Typeface(String.Empty);
			}

			// 見つかった情報で Typeface 生成
			return new Typeface(fontFamily, familyTypeface.Style, familyTypeface.Weight, familyTypeface.Stretch);
		}

		// --------------------------------------------------------------------
		// エキスパンダーを描画
		// --------------------------------------------------------------------
		private void DrawExpander(DrawingContext drawingContext, Rect expanderRect)
		{
			Debug.Assert(TargetFolderInfo != null, "DrawExpander() bad TargetFolderInfo");
			if (TargetFolderInfo.IsOpen == null)
			{
				return;
			}

			// 背景が完全な透明だとマウスクリックイベントが発生しないため、ほぼ透明で塗りつぶす
			drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), null, expanderRect);

			if (TargetFolderInfo.IsOpen == true)
			{
				// 下向きの「>」
				Pen pen = new(Foreground, EXPANDER_THICKNESS);
				DrawExpanderStroke(drawingContext, expanderRect,
						new Point(expanderRect.Left, expanderRect.Top + expanderRect.Height / 4), new Point(expanderRect.Left + expanderRect.Width / 2, expanderRect.Top + expanderRect.Height * 3 / 4),
						new Point(expanderRect.Right, expanderRect.Top + expanderRect.Height / 4), pen);
			}
			else
			{
				// 可能なら薄めの色にする
				Brush brush;
				if (Foreground is SolidColorBrush solid)
				{
					brush = new SolidColorBrush(Color.FromArgb((Byte)(solid.Color.A / 4), solid.Color.R, solid.Color.G, solid.Color.B));
				}
				else
				{
					brush = Foreground;
				}

				// 右向きの「>」
				Pen pen = new(brush, EXPANDER_THICKNESS);
				DrawExpanderStroke(drawingContext, expanderRect,
						new Point(expanderRect.Left + expanderRect.Width / 4, expanderRect.Top), new Point(expanderRect.Left + expanderRect.Width * 3 / 4, expanderRect.Top + expanderRect.Height / 2),
						new Point(expanderRect.Left + expanderRect.Width / 4, expanderRect.Bottom), pen);
			}
		}

		// --------------------------------------------------------------------
		// エキスパンダーの「>」部分を描画
		// --------------------------------------------------------------------
		private void DrawExpanderStroke(DrawingContext drawingContext, Rect expanderRect, Point p1, Point p2, Point p3, Pen pen)
		{
			StreamGeometry geometry = new();
			using StreamGeometryContext geometryContext = geometry.Open();
			geometryContext.BeginFigure(p1, false, false);
			geometryContext.LineTo(p2, true, true);
			geometryContext.LineTo(p3, true, true);
			geometry.Freeze();
			drawingContext.DrawGeometry(null, pen, geometry);
		}

		// --------------------------------------------------------------------
		// エキスパンダーの枠
		// --------------------------------------------------------------------
		private Rect ExpanderRect()
		{
			Debug.Assert(TargetFolderInfo != null, "ExpanderRect() bad TargetFolderInfo");
			Double left = MARGIN_WIDTH + TargetFolderInfo.Level * EXPANDER_SIZE / 2;
			Double top = (ActualHeight - EXPANDER_SIZE) / 2;
			return new Rect(left, top, EXPANDER_SIZE, EXPANDER_SIZE);
		}
	}
}
