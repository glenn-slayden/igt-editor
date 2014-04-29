using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using alib.dg;
using alib.Array;
using alib.Debugging;
using alib.Enumerable;
using alib.Collections;

namespace alib.Wpf
{
	using Math = System.Math;
	using math = alib.Math.math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class DagPanelControl
	{
		Size RuntimeSizeWithPadding
		{
			get
			{
				var sz = r_all.Size;
				sz.Width += Padding.Left + Padding.Right;
				sz.Height += Padding.Top + Padding.Bottom;
				return sz;
			}
		}

		//Thickness _Padding { [DebuggerStepThrough] get { return swap(Padding); } }
		//Thickness _EdgePadding { [DebuggerStepThrough] get { return swap(EdgePadding); } }
		//internal Thickness _VertexPadding { [DebuggerStepThrough] get { return swap(VertexPadding); } }
		//Size _ElementMax(int i_level) { return swap(levels[i_level].RuntimeData.ElementMax); }

		//internal Double _VertexRightPad(layout_vertex_base v)
		//{
		//	int col = v.Column + 1;
		//	if (col == levels[v.Row].Count)
		//		return 0;
		//	var v_next = levels[v.Row][col];

		//	/// any adjacency involving a proxy invokes the reduced proxy padding
		//	if (v is LayoutProxyVertex || v_next is LayoutProxyVertex)
		//		return Math.Max(EdgePadding.Right, EdgePadding.Left);
		//	else
		//		return Math.Max(VertexPadding.Right, VertexPadding.Left);
		//}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		internal Point from_anchor(Rect r)
		{
			r = coord_adjust(r);
			switch (LayoutDirection)
			{
				case LayoutDirection.LeftToRight:
					return r.RightCenter();
				case LayoutDirection.TopToBottom:
					return r.BottomCenter();
				case LayoutDirection.RightToLeft:
					return r.LeftCenter();
				case LayoutDirection.BottomToTop:
					return r.TopCenter();
			}
			throw new Exception();
		}

		internal Point to_anchor(Rect r)
		{
			r = coord_adjust(r);
			switch (LayoutDirection)
			{
				case LayoutDirection.LeftToRight:
					return r.LeftCenter();
				case LayoutDirection.TopToBottom:
					return r.TopCenter();
				case LayoutDirection.RightToLeft:
					return r.RightCenter();
				case LayoutDirection.BottomToTop:
					return r.BottomCenter();
			}
			throw new Exception();
		}

		internal Rect coord_adjust(Rect r)
		{
			switch (LayoutDirection)
			{
				default:
					r.Offset(Padding.Left - r_all.X, Padding.Top - r_all.Y);
					break;
				case LayoutDirection.BottomToTop:
					r.X += Padding.Left - r_all.X;
					r.Y = (r_all.Height + Padding.Top) - r.Bottom;
					break;
				case LayoutDirection.RightToLeft:
					r.X = (r_all.Width + Padding.Left) - r.Right;
					r.Y += Padding.Top - r_all.Y;
					break;
			}
			return r;
		}

		public Thickness swap(Thickness th)
		{
			switch (LayoutDirection)
			{
				default:
				case LayoutDirection.TopToBottom:
					return th;
				case LayoutDirection.LeftToRight:
					return new Thickness(th.Top, th.Right, th.Bottom, th.Left);
				case LayoutDirection.BottomToTop:
					return new Thickness(th.Left, th.Bottom, th.Right, th.Top);
				case LayoutDirection.RightToLeft:
					return new Thickness(th.Bottom, th.Left, th.Top, th.Right);
			}
		}
		public Point swap(Double x, Double y)
		{
			return LogicalOrientation == Orientation.Vertical ? new Point(x, y) : new Point(y, x);
		}
		public Size swap(Size sz)
		{
			return LogicalOrientation == Orientation.Vertical ? sz : new Size(sz.Height, sz.Width);
		}
		public Point swap(Point pt)
		{
			return LogicalOrientation == Orientation.Vertical ? pt : new Point(pt.Y, pt.X);
		}
		public Rect swap(Rect r)
		{
			return LogicalOrientation == Orientation.Vertical ? r : new Rect(r.Y, r.X, r.Height, r.Width);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		Pen _brdr_pen;
		Pen BorderPen
		{
			get
			{
				if (_brdr_pen == null)
				{
					var br = this.BorderBrush;
					var th = BorderThickness.Left;
					if (br != null && br != Brushes.Transparent && th > 0)
						_brdr_pen = new Pen(br, th);
				}
				return _brdr_pen;
			}
		}

		Pen _pen;
		Pen EdgePen
		{
			get
			{
				if (_pen == null)
				{
					var br = this.Stroke;
					var th = this.StrokeThickness;
					if (br != null && br != Brushes.Transparent && th > 0)
						_pen = new Pen(br, th);
				}
				return _pen;
			}
		}

		Pen _pwa;
		Pen _work_area_pen
		{
			get
			{
				if (_pwa == null)
					_pwa = new Pen
					{
						Brush = new SolidColorBrush(Color.FromArgb(0x3f, 0, 0, 0)),
						Thickness = 1,
						DashStyle = new DashStyle { Dashes = { 8.0, 8.0 } }
					};
				return _pwa;
			}
		}

		Pen _pop;
		Pen _proxy_outline_pen
		{
			get
			{
				if (_pop == null)
					_pop = new Pen
					{
						Brush = new SolidColorBrush(Color.FromArgb(0x7f, 0, 0, 0)),
						Thickness = 4,
						DashStyle = new DashStyle { Dashes = { 2, 2 } }
					};
				return _pop;
			}
		}

		public static FrameworkElement create_vertex_element(DagPanelControl panel, IVertexEx vx)
		{
			return new Border
			{
				CornerRadius = new CornerRadius(2),
				BorderBrush = Brushes.DarkGray,
				BorderThickness = new Thickness(1),
				Background = SolidColorBrushCache.Get(0xe0f5e9),
				MinWidth = panel.VertexMinWidth,
				MinHeight = panel.VertexMinHeight,
				DataContext = vx,
				Child = new TextBlock
				{
					Margin = new Thickness(2),
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					Text = vx.TextLabel,
				}
			};
		}
		public static FrameworkElement create_edge_element(IEdgeEx ex, UIElement v_from, UIElement v_to)
		{
			var b = new FrameworkElement { DataContext = ex };
			WpfGraphAdapter.SetTextLabel(b, ex.TextLabel);
			WpfGraphAdapter.SetFrom(b, v_from);
			WpfGraphAdapter.SetTo(b, v_to);
			return b;
		}
	};
}
