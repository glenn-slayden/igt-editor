using System;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

using alib.Enumerable;
using alib.Collections;

namespace alib.Wpf
{
	using Math = System.Math;
	using String = System.String;
	using math = alib.Math.math;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class TreeLayoutPanel : Panel
	{
		const FrameworkPropertyMetadataOptions fpmo =
						FrameworkPropertyMetadataOptions.AffectsParentMeasure |
						FrameworkPropertyMetadataOptions.AffectsParentArrange;

		static TreeLayoutPanel FindPanel(FrameworkElement el) { return el.FindAncestor<TreeLayoutPanel>(); }

		///////////////////////////////////////////////////////
		/// 
		public static readonly DependencyProperty TreeParentProperty =
			DependencyProperty.RegisterAttached("TreeParent", typeof(FrameworkElement), typeof(TreeLayoutPanel),
				new FrameworkPropertyMetadata(default(FrameworkElement), fpmo));
		public static FrameworkElement GetTreeParent(FrameworkElement e)
		{
			return (FrameworkElement)e.GetValue(TreeParentProperty);
		}
		public static void SetTreeParent(FrameworkElement e, FrameworkElement par)
		{
			e.SetValue(TreeParentProperty, par);
		}
		/// 
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		/// 
		public static readonly DependencyProperty LinkTextProperty =
			DependencyProperty.RegisterAttached("LinkText", typeof(String), typeof(TreeLayoutPanel),
				new FrameworkPropertyMetadata(default(FrameworkElement), fpmo));
		public static String GetLinkText(DependencyObject obj)
		{
			return (String)obj.GetValue(LinkTextProperty);
		}
		public static void SetLinkText(DependencyObject obj, String value)
		{
			obj.SetValue(LinkTextProperty, value);
		}
		/// 
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		/// 
		public static readonly DependencyProperty NodeLinkProperty =
			DependencyProperty.RegisterAttached("NodeLink", typeof(bool), typeof(TreeLayoutPanel),
				new FrameworkPropertyMetadata(true, fpmo));

		public static bool GetNodeLink(FrameworkElement e)
		{
			return (bool)e.GetValue(NodeLinkProperty);
		}
		public static void SetNodeLink(FrameworkElement e, bool f)
		{
			e.SetValue(NodeLinkProperty, f);
		}
		/// 
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		/// 
		public static readonly DependencyProperty IsSelectedNodeProperty =
			DependencyProperty.RegisterAttached("IsSelectedNode", typeof(bool), typeof(TreeLayoutPanel),
				new FrameworkPropertyMetadata(false, fpmo, (o, e) =>
					{
						((FrameworkElement)o).Effect = (bool)e.NewValue ? eff : null;
					}));
		public static bool GetIsSelectedNode(FrameworkElement e)
		{
			return (bool)e.GetValue(IsSelectedNodeProperty);
		}
		public static void SetIsSelectedNode(FrameworkElement e, bool f)
		{
			e.SetValue(IsSelectedNodeProperty, f);
		}
		/// 
		///////////////////////////////////////////////////////


		public static DependencyProperty PaddingProperty { get { return Control.PaddingProperty; } }
		public Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}

		public static DependencyProperty BorderThicknessProperty { get { return Border.BorderThicknessProperty; } }
		public Thickness BorderThickness
		{
			get { return (Thickness)GetValue(BorderThicknessProperty); }
			set { SetValue(BorderThicknessProperty, value); }
		}

		public static DependencyProperty BorderBrushProperty { get { return Border.BorderBrushProperty; } }
		public Brush BorderBrush
		{
			get { return (Brush)GetValue(BorderBrushProperty); }
			set { SetValue(BorderBrushProperty, value); }
		}

		public static DependencyProperty CornerRadiusProperty { get { return Border.CornerRadiusProperty; } }
		public CornerRadius CornerRadius
		{
			get { return (CornerRadius)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// add owner dependency properties
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static System.Windows.Media.Effects.DropShadowEffect eff;

		static TreeLayoutPanel()
		{
			eff = new System.Windows.Media.Effects.DropShadowEffect();
			//eff.BlurRadius = 5;
			//eff.ShadowDepth = 3;
			//eff.Color = Colors.LightGray;
			eff.BlurRadius = 12;
			eff.ShadowDepth = 0;
			eff.Color = Colors.Red;

			PaddingProperty.AddOwner(typeof(TreeLayoutPanel));
			BorderThicknessProperty.AddOwner(typeof(TreeLayoutPanel));
			BorderBrushProperty.AddOwner(typeof(TreeLayoutPanel));
			CornerRadiusProperty.AddOwner(typeof(TreeLayoutPanel));

			Control.VerticalContentAlignmentProperty.AddOwner(typeof(TreeLayoutPanel),
					new FrameworkPropertyMetadata(
						VerticalAlignment.Top,
						FrameworkPropertyMetadataOptions.AffectsMeasure |
						FrameworkPropertyMetadataOptions.AffectsArrange |
						FrameworkPropertyMetadataOptions.AffectsParentMeasure |
						FrameworkPropertyMetadataOptions.AffectsParentArrange |
						FrameworkPropertyMetadataOptions.AffectsRender |
						0,
						null,
						(e, o) =>
						{
							VerticalAlignment va = (VerticalAlignment)o;
							return va == VerticalAlignment.Stretch ? VerticalAlignment.Center : va;
						},
						true
					));

			Shape.StrokeProperty.AddOwner(typeof(TreeLayoutPanel),
					new FrameworkPropertyMetadata(
						Brushes.Black,
						0,
						(e, o) =>
						{
							TreeLayoutPanel p = (TreeLayoutPanel)e;
							p.InvalidateVisual();

						},
						null,
						false));

			Shape.StrokeThicknessProperty.AddOwner(typeof(TreeLayoutPanel),
					new FrameworkPropertyMetadata(
						1.0,
						0,
						(e, o) =>
						{
							TreeLayoutPanel p = (TreeLayoutPanel)e;
							p.InvalidateVisual();

						},
						null,
						false));
		}

		public VerticalAlignment VerticalContentAlignment
		{
			get { return (VerticalAlignment)GetValue(Control.VerticalContentAlignmentProperty); }
			set { SetValue(Control.VerticalContentAlignmentProperty, value); }
		}

		public Brush Stroke
		{
			get { return (Brush)GetValue(Shape.StrokeProperty); }
			set { SetValue(Shape.StrokeProperty, value); }
		}

		public Double StrokeThickness
		{
			get { return (Double)GetValue(Shape.StrokeThicknessProperty); }
			set { SetValue(Shape.StrokeThicknessProperty, value); }
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// introduced dependency properties
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static readonly DependencyProperty VerticalBufferProperty =
			DependencyProperty.Register("VerticalBuffer", typeof(Double), typeof(TreeLayoutPanel), new FrameworkPropertyMetadata(30.0, fpmo));

		public Double VerticalBuffer
		{
			get { return (Double)GetValue(VerticalBufferProperty); }
			set { SetValue(VerticalBufferProperty, value); }
		}

		public static readonly DependencyProperty HorizontalBufferSubtreeProperty =
			DependencyProperty.Register("HorizontalBufferSubtree", typeof(Double), typeof(TreeLayoutPanel), new FrameworkPropertyMetadata(10.0, fpmo));

		public Double HorizontalBufferSubtree
		{
			get { return (Double)GetValue(HorizontalBufferSubtreeProperty); }
			set { SetValue(HorizontalBufferSubtreeProperty, value); }
		}

		public static readonly DependencyProperty HorizontalBufferProperty =
			DependencyProperty.Register("HorizontalBuffer", typeof(Double), typeof(TreeLayoutPanel), new FrameworkPropertyMetadata(10.0, fpmo));

		public Double HorizontalBuffer
		{
			get { return (Double)GetValue(HorizontalBufferProperty); }
			set { SetValue(HorizontalBufferProperty, value); }
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		Dictionary<FrameworkElement, NodeLayoutInfo> nli_dict;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override Size MeasureOverride(Size availableSize)
		{
			if (InternalChildren.Count == 0)
				return new Size(100, 100);

			nli_dict = new Dictionary<FrameworkElement, NodeLayoutInfo>();

			var roots = new List<FrameworkElement>();

			foreach (FrameworkElement child in InternalChildren)
			{
				nli_dict.Add(child, new NodeLayoutInfo(this, child));

				if (!(child.GetValue(TreeParentProperty) is FrameworkElement))
					roots.Add(child);
			}

			if (roots.Count == 0)
				return new Size(100, 100);

			foreach (var kvp in nli_dict)
			{
				NodeLayoutInfo nli_parent;
				var fe_child = kvp.Key;
				var tpar = fe_child.GetValue(TreeParentProperty) as FrameworkElement;
				if (tpar != null && nli_dict.TryGetValue(tpar, out nli_parent))
					(kvp.Value.nli_parent = nli_parent).Add(fe_child);
			}

			Double x = 0.0;
			Double y_max = 0.0;
			foreach (var root in roots)
			{
				if (x > 0.0)
					x += 10.0;

				var layer_heights = new List<Double>();
				var nli = nli_dict[root];
				nli.CalculateLayout(layer_heights, 0);
				var sz2 = nli.DetermineFinalPositions(layer_heights, 0, 0, x + nli.pxLeftPosRelativeToBoundingBox);
				math.Maximize(ref y_max, sz2.Height);
				x += sz2.Width;

				layer_heights = null;
			}
			var sz = new Size(x, y_max);

			sz.Width += Padding.Left + Padding.Right;
			sz.Height += Padding.Top + Padding.Bottom;
			return sz;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override Size ArrangeOverride(Size sz)
		{
			if (nli_dict != null)
			{
				foreach (var nli in nli_dict.Values)
				{
					var r_child = nli.r_final;

					r_child.Offset(Padding.Left, Padding.Top);

					nli.fe.Arrange(r_child);
				}
			}
			return sz;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override void OnRender(DrawingContext dc)
		{
			Rect r_render;
			if (nli_dict == null || (r_render = new Rect(util.coord_origin, RenderSize)).IsZeroSize())
				return;

			bool f_pad = DrawBackground(dc, r_render);

			NodeLayoutInfo nli;
			String text;
			Pen pen = new Pen(Stroke, StrokeThickness);

			foreach (var kvp in nli_dict)
			{
				if (GetNodeLink(kvp.Key))
				{
					foreach (var child in (nli = kvp.Value))
						if (GetNodeLink(child))
						{
							Point pt1 = nli.r_final.BottomCenter();
							Point pt2 = nli_dict[child].r_final.TopCenter();

							dc.DrawLine(pen, pt1, pt2);

							if (!String.IsNullOrWhiteSpace(text = child.Tag as String ?? GetLinkText(child)))
							{
								var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, util.tf_calibri, 14, Brushes.Black);
								var r = new Rotation(pt1, pt2);
#if false
								dc.DrawText(ft, ft.get_text_origin(r));
#else
								RectangleGeometry rect_geo;
								var geo = ft.RotateTextGeometry(r, out rect_geo);
								dc.DrawGeometry(Brushes.White, null, rect_geo);
								dc.DrawGeometry(Brushes.Black, null, geo);
#endif
							}
						}
				}

				//if (GetIsSelectedNode(cur))
				//    dc.DrawRectangle(null, new Pen(Brushes.Black, 4), nli.FinalRect(this, cur));
			}

			if (f_pad)
				dc.Pop();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		bool DrawBackground(DrawingContext dc, Rect r_render)
		{
			Brush bb, fill;
			Pen pen = null;
			Double d;
			if ((bb = BorderBrush) != null && bb != Brushes.Transparent && !alib.Math.math.IsZero(d = BorderThickness.Left))
				pen = new Pen(bb, d);

			if (((fill = Background) == null || fill == Brushes.Transparent) && pen == null)
				return false;

			if (alib.Math.math.IsZero(d = CornerRadius.TopLeft))
				dc.DrawRectangle(fill, pen, r_render);
			else
				dc.DrawRoundedRectangle(fill, pen, r_render, d, d);

			if (Padding.Left == 0 && Padding.Top == 0)
				return false;

			dc.PushTransform(new TranslateTransform(Padding.Left, Padding.Top));
			return true;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			var htr = base.HitTestCore(hitTestParameters);
			if (htr == null)
				htr = new PointHitTestResult(this, hitTestParameters.HitPoint);
			return htr;
		}
	};
}
