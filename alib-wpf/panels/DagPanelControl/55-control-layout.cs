using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

using alib.dg;
using alib.Math;
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
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void reset_vertex_locations()
		{
			foreach (layout_vertex_base v in g_cur.Verticies)
				v.Location = util.point_NaN;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void layout_verticies(LevelInfo[] levels)
		{
			reset_vertex_locations();

			int i, j;
			LevelInfo L;
			LevelInfo.RuntimeLayoutData rtd;

			this.max_width = Double.NegativeInfinity;

			for (i = 0; i < levels.Length; i++)
			{
				math.Maximize(ref max_width, levels[i].RuntimeData.TotalWidth);
			}

#if report
			Debug.Print("");
			for (i = 0; i < levels.Length; i++)
			{
				L = levels[i];
				rtd = L.RuntimeData;
				Debug.Print("L{0,-2}  c:{1,-2}  cp:{2,2}  cnp:{3,-2}  elem_tot:{4,7:N2}  pad_tot:{5,7:N2}  w_tot:{6,7:N2}{7} extra:{8,7:N2}  [ {9} {{ {10} }} {11} ]",
						L.i_level,
						L.Count,
						rtd.ProxyCount,
						rtd.NonProxyCount,
						rtd.ElementWidthTotal,
						rtd.HorizontalPadTotal,
						rtd.TotalWidth,
						rtd.TotalWidth == max_width ? "*" : " ",
						max_width - rtd.TotalWidth,
						rtd.DesiredPadding.First().ToString("0.##"),
						rtd.DesiredPadding.TrimEnds().Select(pp => pp.ToString("0.##")).StringJoin(" "),
						rtd.DesiredPadding.Last().ToString("0.##"));
			}
#endif

			Double extra, expand, x, y = 0;

			for (i = 0; i < levels.Length; i++)
			{
				L = levels[i];
				rtd = L.RuntimeData;

				if (i > 0)
					y += swap(VertexPadding).Top;

				var rgdp = rtd.DesiredPadding;

				var pad_plus_outer = rgdp.Sum();
				extra = max_width - (L.RuntimeData.ElementWidthTotal + pad_plus_outer);

				if (extra < math.ε)
				{
					rgdp[0] = rgdp[rgdp.Length - 1] = 0.0;
					pad_plus_outer = rgdp.Sum();
					extra = max_width - (L.RuntimeData.ElementWidthTotal + pad_plus_outer);
				}

				if (extra < math.ε)
					expand = 1.0;
				else if (pad_plus_outer.IsZero())
				{
					var q = extra / rgdp.Length;
					for (j = 0; j < rgdp.Length; j++)
						rgdp[j] = q;
					expand = Double.NaN;
				}
				else
				{
					expand = 1.0 + extra / pad_plus_outer;
					for (j = 0; j < rgdp.Length; j++)
						rgdp[j] *= expand;
				}

				x = rgdp[0];
				for (j = 0; ; )
				{
					var v = L[j];
					var yy = y;

					var sz = swap(v.Size);

					if (ContentAlignment == ContentAlignment.Center || v is LayoutProxyVertex)
						yy += (swap(rtd.ElementMax).Height - sz.Height) / 2;
					else if (ContentAlignment == ContentAlignment.TowardsLeaf)
						yy += swap(rtd.ElementMax).Height - sz.Height;

					v.Location = swap(new Point(x, yy));

					r_all.Union(v.LayoutRect);

					x += sz.Width;

					if (++j == L.Count)
						break;

					x += rgdp[j];
				}

#if report
				Debug.Print("L{0,-2}  {1,7:N2}  ({2,7:N2})  extra:{3,7:N2}  expand:{4,7:N2}  [ {5} {{ {6} }} {7} ]",
					i,
					x,
					x + rgdp[j],
					extra,
					expand,
					rgdp.First().ToString("0.##"),
					rgdp.TrimEnds().Select(pp => pp.ToString("0.##")).StringJoin(" "),
					rgdp.Last().ToString("0.##"));
#endif
				y += swap(rtd.ElementMax).Height;
			}

			//Debug.Print("{0} {1}", max_width, r_all.Width);
		}

		Double max_width;
#if false
		Double advise_below(layout_vertex_base v)
		{
			Rect rb;

			var rgb = v.Below;
			if (rgb.Length == 0)
				return Double.NaN;

			if (rgb.Length == 1)
				return (rb = rgb[0].LayoutRect).Location.IsFinite() ? swap(rb).HCenter() : Double.NaN;

			for (int i = 0; i < rgb.Length; i++)
			{
				if (!(rb = rgb[0].LayoutRect).Location.IsFinite())
					return Double.NaN;
				
			}
			//return rgb.Select(vb =>
			//	{
			//		Rect _rb;

			//	})
			//	.Average();

			//layout_vertex_base[] rgn;
			//if (x + sz.Width < L.RuntimeData.TotalWidth && ((rgn = v.Above).Length) > 0)
			//{
			//	var aa = v.Above.Select(va => swap(va.LayoutRect).HCenter()).Average();
			//	if (aa > x)
			//		x = aa - sz.Width / 2;
			//}
			throw new NotImplementedException();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void layout_vertex(ref double x, double y, layout_vertex_base v)
		{
			var L = levels[v.Row];

			var sz = swap(v.Size);

#if false
			layout_vertex_base[] rgn;
			if (x + sz.Width < L.RuntimeData.TotalWidth && ((rgn = v.Above).Length) > 0)
			{
				if (rgn.Length == 1)
				{
				}
				else
				{
					var aa = rgn.Select(va => swap(va.LayoutRect).HCenter()).Average();
					if (aa > x)
						x = aa - sz.Width / 2;
				}
			}
#endif

			switch (ContentAlignment)
			{
				case ContentAlignment.Center:
					y += (_ElementMax(v.Row).Height - sz.Height) / 2;
					break;
				case ContentAlignment.TowardsLeaf:
					y += _ElementMax(v.Row).Height - sz.Height;
					break;
			}

			v.Location = swap(new Point(x, y));

			r_all.Union(v.LayoutRect);

			x += sz.Width + _VertexRightPad(v);
		}
#endif

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void layout_edges()
		{
			this.tfp = null;

			foreach (LayoutEdgeEx e in g_cur.Edges)
				if (e.el != null)
					layout_edge(e, e.el);
		}

		TypefacePlus tfp;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void layout_edge(LayoutEdgeEx e, UIElement uie)
		{
			Size sz = util.zero_size;
			String txt = null;
			Point pt;
			Rect r;
			Points pts;	/// can't use Rect because it normalizes its coordinates to always have a positive size
			Double a = Double.NaN;

			CompoundLayoutEdgeEx cle;
			LayoutMultiEdgeEx mle;
			FrameworkElement fe;

			e.geom = null;

			switch (EdgeContentMode)
			{
				default:
				case EdgeContentMode.Element:
					sz = e.Size;
					break;

				case EdgeContentMode.Index:
					txt = "E" + e.Index;
					break;

				case EdgeContentMode.Text:
					txt = e.TextLabel;
					break;

				case EdgeContentMode.None:
					break;
			}

			if (!String.IsNullOrWhiteSpace(txt))
			{
				if (tfp == null)
					tfp = TypefacePlus.Create(this, EdgeTextStyle);
				var ft = tfp.FormattedText(txt);
				(e.geom = ft.BuildGeometry(util.coord_origin)).Transform = null;
				sz = (r = e.geom.Bounds).Size;

				e.AddTransform(new TranslateTransform(-r.Left, -r.Top));
			}

			if (sz.IsZero())
			{
				e.geom = null;
				uie.Visibility = Visibility.Hidden;
				if ((fe = uie as FrameworkElement) != null)
					fe.LayoutTransform = null;
				return;
			}

			uie.Visibility = e.geom == null ? Visibility.Visible : Visibility.Hidden;

			if ((cle = e as CompoundLayoutEdgeEx) != null)
			{
				var rg = cle.proxies;
				var vc = rg[rg.Length / 2];

				if (vc.Above.Length != 1 || vc.Below.Length != 1)
					throw new Exception();

				var va = vc.Above[0];

				if (!(vc is LayoutProxyVertex))
					throw new Exception();

				if ((rg.Length & 1) == 1)
				{
					pts = default(Points);
					a = label_on_proxy_vertex(va, vc.Below[0], pt = vc.LayoutRect.Center());
					goto precomputed;
				}
				pts = new Points(va.LayoutRect.Center(), vc.LayoutRect.Center());

			}
			else
			{
				pts = LogicalOrientation == Orientation.Vertical ?
					new Points(e.From.LayoutRect.BottomCenter(), e.To.LayoutRect.TopCenter()) :
					new Points(e.From.LayoutRect.RightCenter(), e.To.LayoutRect.LeftCenter());

				if ((mle = e as LayoutMultiEdgeEx) != null)
				{
					pt = mle.multi_mid_adjust(pts.P1, pts.P2);
					goto precomputed;
				}
			}

			if (double.IsNaN(pts.P1.X) || double.IsNaN(pts.P1.Y))
				throw new Exception();
			if (double.IsNaN(pts.P2.X) || double.IsNaN(pts.P2.Y))
				throw new Exception();


			pt = pts.Rect.Center();
		precomputed:

			if (double.IsNaN(pt.X) || double.IsNaN(pt.Y))
				throw new Exception();

			r = pt.MakeCenteredRect(sz);
			e.Location = r.TopLeft;

			if (EdgeContentOrientation == EdgeContentOrientation.Rotated)
			{
				if (Double.IsNaN(a))
				{
					a = util.Atan2(pts) * math.PiRad;
					if (Double.IsNaN(a))
						throw new Exception();
				}
				else if (Double.IsNaN(a))
					throw new Exception();

				if (LayoutDirection >= LayoutDirection.RightToLeft)
					a = -a;
				if (a < -90)
					a += 180;
				else if (a >= 90)
					a -= 180;

				if (e.geom != null)
				{
					e.AddTransform(new RotateTransform(a, sz.Width / 2, sz.Height / 2));

					r = e.geom.Bounds;
					r.Offset(e.Location.X, e.Location.Y);
				}
				else if ((fe = uie as FrameworkElement) != null)
				{
					fe.LayoutTransform = new RotateTransform(a, pt.X, pt.Y);
				}
			}
			else
			{
				if (e.geom != null)
					e.AddTransform(Transform.Identity);
				else if ((fe = uie as FrameworkElement) != null)
					fe.LayoutTransform = null;
			}

			r_all.Union(r);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		Double label_on_proxy_vertex(layout_vertex_base va, layout_vertex_base vb, Point pt)
		{
			var pt_a = va is LayoutProxyVertex ? va.LayoutRect.Center() :
				LogicalOrientation == Orientation.Vertical ?
					va.LayoutRect.BottomCenter() : va.LayoutRect.RightCenter();

			var pt_b = vb is LayoutProxyVertex ? vb.LayoutRect.Center() :
				LogicalOrientation == Orientation.Vertical ?
					vb.LayoutRect.TopCenter() : vb.LayoutRect.LeftCenter();

			var a1 = Math.Atan2(pt_a.Y - pt.Y, pt_a.X - pt.X) * math.PiRad;
			var a2 = Math.Atan2(pt.Y - pt_b.Y, pt.X - pt_b.X) * math.PiRad;

			return (a1 + a2) / 2;
		}
	};
}
