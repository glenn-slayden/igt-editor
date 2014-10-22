using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using alib.Debugging;
using alib.Math;
using alib.String;

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

		static String[] _HASHCHAR = { "", "#", "##", "###", "####", "#####", "######", "#######", "########", "#########" };

		public static String dFmt(this Double d, int N = 2)
		{
			return Double.IsPositiveInfinity(d) ? "∞" : Double.IsNegativeInfinity(d) ? "-∞" : d.ToString("0." + _HASHCHAR[N]);
		}
		public static String ptFmt(this Point pt, int N = 2)
		{
			return dFmt(pt.X, N) + "," + dFmt(pt.Y, N);
		}
		public static String szFmt(this Size sz, int N = 2)
		{
			return sz.IsEmpty ? "empty" : dFmt(sz.Width, N) + "×" + dFmt(sz.Height, N);
		}
		public static String rFmt(this Rect r)
		{
			return "{ " + ptFmt(r.Location) + "-" + szFmt(r.Size) + "}";
		}

		public static void ClearAllDrawings(this DrawingGroup dxg)
		{
			if (dxg != null)
			{
				foreach (Drawing drawing in dxg.Children)
				{
					if (drawing is DrawingGroup)
						ClearAllDrawings((DrawingGroup)drawing);
				}
				dxg.Children.Clear();
			}
		}

		public static void DrawAllDrawings(this DrawingContext dc, Visual viz)
		{
			var uie = viz as UIElement;
			var x = uie != null ? uie.RenderTransform : null;
			if (x == Transform.Identity)
				x = null;

			var v = VisualTreeHelper.GetOffset(viz);

			var t = v.X.IsZero() && v.Y.IsZero() ? null : new TranslateTransform(v.X, v.Y);

			if (t != null)
				dc.PushTransform(t);
			if (x != null)
				dc.PushTransform(x);

			dc.DrawDrawing(VisualTreeHelper.GetDrawing(viz));

			foreach (var cdv in viz.ImmediateChildren<Visual>())
				DrawAllDrawings(dc, cdv);

			if (x != null)
				dc.Pop();
			if (t != null)
				dc.Pop();
		}

		public static IEnumerable<String> WalkReport(this Drawing d, int level = 0, int term = 0)
		{
			String s;
			int i, cc;
			DrawingGroup d1;
			GlyphRunDrawing d2;
			GeometryDrawing d3;

			cc = (d1 = d as DrawingGroup) != null ? d1.Children.Count : 0;

			var r = d.Bounds;
#if false
			s = r.IsEmpty ? 
					"(empty)".PadLeft(29) :
					_string_ext.LeftRight(
							"(" + r.Left.ToString("0.#") + ", " + r.Top.ToString("0.#") + ")",
							"(" + r.Width.ToString("0.#") + "×" + r.Height.ToString("0.#") + ")", 
							29);
#else
			s = _string_ext.LeftRight(ptFmt(r.Location), szFmt(r.Size, 1), 29);
#endif

			{
				var xs = new String('│', level);
				if (cc > 0)
					xs += "┌─";
				else if (term > 0)
					xs = xs.Remove(xs.Length - term) + new String('└', term) + "─";		// "└" + new String('┴', term - 1)
				else
					xs += " ";
				s += " " + xs.PadRight(45, xs[xs.Length - 1]);
			}

			if (d == EmptyDrawing)
			{
				s += "Empty";
				yield return s;
				yield break;
			}

			s += " " + d.GetType().Name.Replace("Drawing", "");

			if (d1 != null)
			{
				s += "-" + cc;
				var x = d1.Transform;
				if (x == null)
					s += " x:null";
				else
				{
					s += " x:" + x.GetType().Name.Replace("Transform", "");
					var xx = x as TranslateTransform;
					if (xx != null)
					{
						s += " " + xx.X.dFmt() + "," + xx.Y.dFmt();
					}
					else
					{
						throw new NotImplementedException();
					}
				}
			}
			else if ((d2 = d as GlyphRunDrawing) != null)
			{
				var rgch = d2.GlyphRun.Characters;
				s += " \"" + new String(rgch as char[] ?? rgch.ToArray()) + "\"";
			}
			else if ((d3 = d as GeometryDrawing) != null)
			{
				var g = d3.Geometry;
				if (g == null)
					s += " (null)";
				else
					s += " (" + g.GetType().Name.Replace("Geometry", "") + ")";
			}
			else
			{
				throw new NotImplementedException();
			}

			yield return s;

			for (i = 0; i < cc; )
				foreach (var ss in WalkReport(d1.Children[i], level + 1, ++i == cc ? term + 1 : 0))
					yield return ss;
		}

#if false
		public static IEnumerable<String> WalkReport(this Visual viz, int level = 0)
		{
			yield return new String(' ', level * 2) + viz.GetType().Name;

			level++;

			var v = VisualTreeHelper.GetOffset(viz);

			var d = VisualTreeHelper.GetDrawing(viz);
			if (d != null)
			{
				foreach (var s in WalkReport(d, level))
					yield return s;
			}

			var rg = viz.ImmediateChildren<Visual>();
			for (int i = 0; i < rg.Length; i++)
			{
				foreach (var s in WalkReport(rg[i], level))
					yield return s;
			}
		}
		public static void DrawAllDrawings(this DrawingContext dc, Visual viz)
		{
			DrawingGroup grp;

			if ((grp = VisualTreeHelper.GetDrawing(viz)) != null)
			{
				draw_all_drawings_B(dc, grp);

				foreach (var child in ImmediateChildren<Visual>(viz))
					draw_all_drawings_A(dc, child);
			}
			else
			{
				var v = VisualTreeHelper.GetOffset(viz);
				dc.PushTransform(new TranslateTransform(v.X, v.Y));

				foreach (var child in ImmediateChildren<Visual>(viz))
					draw_all_drawings_A(dc, child);

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

				foreach (var child in ImmediateChildren<Visual>(viz))
					draw_all_drawings_A(dc, child);

				if (xform != null)
					dc.Pop();

				dc.Pop();
			}
			else
			{
				dc.PushTransform(new TranslateTransform(v.X, v.Y));

				foreach (var vch in ImmediateChildren<Visual>(viz))
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