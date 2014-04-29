using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using alib.Debugging;

namespace alib.Wpf
{
	using String = System.String;

	public static partial class util
	{
		public static readonly Drawing EmptyDrawing;

		public static _dc_pushsafe_xform PushSafe(this DrawingContext dc, Transform xform)
		{
			return new _dc_pushsafe_xform(dc, xform);
		}
		public struct _dc_pushsafe_xform : IDisposable
		{
			public _dc_pushsafe_xform(DrawingContext dc, Transform xform)
			{
				if (xform != null && xform != Transform.Identity)
					(this.dc = dc).PushTransform(this.xform = xform);
				else
					this = default(_dc_pushsafe_xform);
			}
			DrawingContext dc;
			Transform xform;
			public void Dispose()
			{
				if (xform != null)
					dc.Pop();
			}
		};

		public static _dc_pushsafe_clip PushSafe(this DrawingContext dc, Geometry clip)
		{
			return new _dc_pushsafe_clip(dc, clip);
		}
		public struct _dc_pushsafe_clip : IDisposable
		{
			public _dc_pushsafe_clip(DrawingContext dc, Geometry clip)
			{
				if (clip != null && !clip.IsEmpty())
					(this.dc = dc).PushClip(this.clip = clip);
				else
					this = default(_dc_pushsafe_clip);
			}
			DrawingContext dc;
			Geometry clip;
			public void Dispose()
			{
				if (clip != null)
					dc.Pop();
			}
		}

		public static String dFmt(this Double d)
		{
			return Double.IsPositiveInfinity(d) ? "∞" : Double.IsNegativeInfinity(d) ? "-∞" : d.ToString("0.##");
		}
		public static String ptFmt(this Point pt)
		{
			return dFmt(pt.X) + "," + dFmt(pt.Y);
		}
		public static String ptFmt(this Vector pt)
		{
			return dFmt(pt.X) + "," + dFmt(pt.Y);
		}
		public static String szFmt(this Size sz)
		{
			return dFmt(sz.Width) + "×" + dFmt(sz.Height);
		}
		public static String rFmt(this Rect r)
		{
			return "{ " + ptFmt(r.Location) + "-" + szFmt(r.Size) + "}";
		}

		public static void ClearAllDrawings(this DrawingGroup dg)
		{
			if (dg != null)
			{
				foreach (Drawing drawing in dg.Children)
				{
					if (drawing is DrawingGroup)
						ClearAllDrawings((DrawingGroup)drawing);
				}
				dg.Children.Clear();
			}
		}

#if true
		public static void DrawAllDrawings(this DrawingContext dc, Visual viz)
		{
			var v = VisualTreeHelper.GetOffset(viz);
			var d = VisualTreeHelper.GetDrawing(viz);

			dc.PushTransform(new TranslateTransform(v.X, v.Y));
			dc.DrawDrawing(d);

			foreach (var cdv in viz.ImmediateChildren().OfType<Visual>())
			{
				DrawAllDrawings(dc, cdv);
			}
			dc.Pop();
		}
#else
		public static void DrawAllDrawings(this DrawingContext dc, Visual viz)
		{
			DrawingGroup grp;

			if ((grp = VisualTreeHelper.GetDrawing(viz)) != null)
			{
				draw_all_drawings_B(dc, grp);

				foreach (Visual vch in ImmediateChildren(viz))
					draw_all_drawings_A(dc, vch);
			}
			else
			{
				var v = VisualTreeHelper.GetOffset(viz);
				dc.PushTransform(new TranslateTransform(v.X, v.Y));

				foreach (Visual vch in ImmediateChildren(viz))
					draw_all_drawings_A(dc, vch);

				dc.Pop();
			}
		}

		static void draw_all_drawings_A(DrawingContext dc, Visual viz)
		{
			var v = VisualTreeHelper.GetOffset(viz);

			DrawingGroup grp;

			if ((grp = VisualTreeHelper.GetDrawing(viz)) != null)
			{
				var xform = VisualTreeHelper.GetTransform(viz);

				dc.PushTransform(new TranslateTransform(v.X, v.Y));

				if (xform != null)
					dc.PushTransform(xform);

				draw_all_drawings_B(dc, grp);

				foreach (Visual vch in ImmediateChildren(viz))
					draw_all_drawings_A(dc, vch);

				if (xform != null)
					dc.Pop();

				dc.Pop();
			}
			else
			{
				dc.PushTransform(new TranslateTransform(v.X, v.Y));

				foreach (Visual vch in ImmediateChildren(viz))
					draw_all_drawings_A(dc, vch);

				dc.Pop();
			}
		}

		static void draw_all_drawings_B(DrawingContext dc, Drawing d1)
		{
			DrawingGroup grp;

			if ((grp = d1 as DrawingGroup) != null)
				foreach (var d2 in grp.Children)
					draw_all_drawings_B(dc, d2);
			else
				dc.DrawDrawing(d1);
		}
#endif
#if false
					//var r1 = VisualTreeHelper.GetContentBounds(viz);
			//var r2 = VisualTreeHelper.GetDescendantBounds(viz);
			//Debug.Print("{0,4}  {1,-22}  {2,-14}  {3,-14}  {4,-14}  {5,-14}  {6}",
			//	ind,
			//	viz.GetType().Name,
			//	ptFmt(VisualTreeHelper.GetOffset(viz)),
			//	ptFmt(r1.Location),
			//	ptFmt(r2.Location),
			//	szFmt(r1.Size),
			//	szFmt(r2.Size));




		public static void DrawAllDrawings(this DrawingContext dc, Visual viz)
		{
			var d = VisualTreeHelper.GetDrawing(viz);
			if (d != null)
			{
				Debug.Print("1a: {0}  {1}", viz.GetType().Name, d.Bounds);

				//dc.PushTransform(new TranslateTransform(cv.Offset.X, cv.Offset.Y));
				dc.PushTransform(new TranslateTransform(d.Bounds.X, d.Bounds.Y));

				dc.DrawDrawing(d);
			}
			else
			{
				Debug.Print("1x: {0}", viz.GetType().Name);
			}

			foreach (var vch in ImmediateChildren(viz))
			{
				if (vch is Visual)
					DrawAllDrawings(dc, (Visual)vch);
				else
					Debug.Print("2x: {0}", vch.GetType().Name);

				//DrawAllDrawings(dc, vch);
			}

			if (d != null)
				dc.Pop();
		}
#endif


	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class DrawingUIElement : FrameworkElement
	{
		static DrawingUIElement() { tt_cache = new TranslateTransform(); }
		static readonly TranslateTransform tt_cache;

		public DrawingUIElement(Drawing drawing)
		{
			//Debug.Assert(drawing.IsFrozen);
			this.drawing = drawing;
		}
		readonly Drawing drawing;

		TranslateTransform get_transform()
		{
			if (Margin.Left == 0 && Margin.Top == 0)
				return null;
			tt_cache.X = Margin.Left;
			tt_cache.Y = Margin.Top;
			return tt_cache;
		}
		protected override Size MeasureOverride(Size _)
		{
			var sz = drawing.Bounds.Size;
			return new Size
			{
				Width = sz.Width + Margin.Left + Margin.Right,
				Height = sz.Height + Margin.Top + Margin.Bottom,
			};
		}
		protected override void OnRender(DrawingContext dc)
		{
			var tt = get_transform();
			if (tt != null)
				dc.PushTransform(tt);
			dc.DrawDrawing(drawing);
			if (tt != null)
				dc.Pop();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class DpDrawingElement : FrameworkElement
	{
		static readonly TranslateTransform tt_cache;
		public static readonly DependencyProperty DrawingProperty;

#if DEBUG
		static Object _cv(DependencyObject sender, Object value)
		{
			Drawing d;
			if ((d = value as Drawing) != null)
				Debug.Assert(d.IsFrozen);
			return d;
		}
#endif
		static void _pc(DependencyObject sender, DependencyPropertyChangedEventArgs e) { ((DpDrawingElement)sender).prop_changed((Drawing)e.NewValue); }

		static DpDrawingElement()
		{
			tt_cache = new TranslateTransform();

			DrawingProperty = DependencyProperty.Register("Drawing", typeof(Drawing), typeof(DpDrawingElement), new FrameworkPropertyMetadata
			{
				Inherits = true,
				DefaultValue = default(Drawing),
				AffectsRender = true,
				PropertyChangedCallback = _pc,
#if DEBUG
				CoerceValueCallback = _cv,
#endif
			});
		}

		public Drawing Drawing
		{
			get { return (Drawing)GetValue(DrawingProperty); }
			set { SetValue(DrawingProperty, value); }
		}
		void prop_changed(Drawing d)
		{
			Nop.X();
		}

		TranslateTransform get_transform()
		{
			if (Margin.Left == 0 && Margin.Top == 0)
				return null;
			tt_cache.X = Margin.Left;
			tt_cache.Y = Margin.Top;
			return tt_cache;
		}
		protected override Size MeasureOverride(Size _)
		{
			Drawing d;
			if ((d = this.Drawing) == null)
				return Size.Empty;

			var sz = d.Bounds.Size;
			return new Size
			{
				Width = sz.Width + Margin.Left + Margin.Right,
				Height = sz.Height + Margin.Top + Margin.Bottom,
			};
		}
		protected override void OnRender(DrawingContext dc)
		{
			Drawing d;
			if ((d = this.Drawing) != null)
			{
				var tt = get_transform();
				if (tt != null)
					dc.PushTransform(tt);
				dc.DrawDrawing(d);
				if (tt != null)
					dc.Pop();
			}
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class DrawingPair : DpDrawingElement
	{
		public static DependencyProperty IsSelectedProperty { get { return Selector.IsSelectedProperty; } }
		static DrawingPair()
		{
			IsSelectedProperty.AddOwner(typeof(DrawingPair), new FrameworkPropertyMetadata(_pc));
		}

		static void _pc(DependencyObject sender, DependencyPropertyChangedEventArgs e) { ((DrawingPair)sender).prop_changed((bool)e.NewValue); }

		void prop_changed(bool f_selected)
		{
			base.Drawing = f_selected ? d2 : d1;
		}

		public DrawingPair(Drawing d1, Drawing d2)
		{
			Debug.Assert(d1.IsFrozen && d2.IsFrozen);
			Debug.Assert(d1.Bounds.Size == d2.Bounds.Size);
			this.d1 = d1;
			this.d2 = d2;
			prop_changed(false);
		}
		readonly Drawing d1, d2;

		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}
	};


}