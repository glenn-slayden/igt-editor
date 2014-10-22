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

using alib.Graph;
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
		void render_edges(DrawingContext dc)
		{
			bool f_any_proxies = false;
			Point[] points = null;

			foreach (LayoutEdgeEx e in g_cur.Edges)
			{
				f_any_proxies |= e is CompoundLayoutEdgeEx;

				var g = get_edge_geom(e, ref points);

				dc.DrawGeometry(e.LineFill ?? Stroke, e.LineStroke ?? EdgePen, g);

#if false
				foreach (var pt in points.Skip(1).ExceptLast())
					dc.DrawGeometry(
						Brushes.Black,
						null,
						new EllipseGeometry(pt.MakeCenteredRect(StrokeThickness * 2)));
#endif

				if ((e.EdgeDirection & EdgeDirection.Reversed) != 0)
					dc.DrawGeometry(e.LineFill ?? Stroke, e.LineStroke ?? EdgePen, new PathGeometry
					{
						Figures = 
						{ 
							LineWithText.ArrowheadFigure(points[1], 
							points[0])
						}
					});

				if ((e.EdgeDirection & EdgeDirection.Normal) != 0)
					dc.DrawGeometry(e.LineFill ?? Stroke, e.LineStroke ?? EdgePen, new PathGeometry
					{
						Figures =
						{
							LineWithText.ArrowheadFigure(points[points.Length - 2], 
							points[points.Length - 1])
						}
					});
			}

			if (f_any_proxies)
				render_proxy_options(dc, ShowLayoutProxyPoints, ShowLayoutProxyOutlines);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void render_proxy_options(DrawingContext dc, bool f_points, bool f_outline)
		{
			if (!(f_points || f_outline))
				return;

			foreach (var vx in g_cur.Verticies.OfType<LayoutProxyVertex>())
			{
				var r = coord_adjust(vx.LayoutRect);

				if (f_points)
					dc.DrawGeometry(
						Brushes.Black,
						null,
						new EllipseGeometry(r.Center().MakeCenteredRect(StrokeThickness * 2)));

				if (f_outline)
					dc.DrawGeometry(null, _proxy_outline_pen, r.IsZeroSize() ? (Geometry)
									new LineGeometry(r.TopLeft, r.BottomRight) :
									new RectangleGeometry(r));
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		Geometry get_edge_geom(LayoutEdgeEx e, ref Point[] points)
		{
			CompoundLayoutEdgeEx cle;
			LayoutMultiEdgeEx mle;

			if ((cle = e as CompoundLayoutEdgeEx) != null)
			{
				points = cle.Points.ToArray();

				if (SplineTension.IsZero())
					return new PathGeometry
					{
						Figures = 
						{ 
							new PathFigure
							{
								StartPoint = points[0],
								IsFilled = false,
								Segments = { new PolyLineSegment(points.Skip(1), true) }
							}
						}
					};
				return CanonicalSpline.Create(points, SplineTension, null, false, false);
			}

			if (points == null || points.Length != 2)
				points = new Point[2];
			points[0] = from_anchor(e.From.LayoutRect);
			points[1] = to_anchor(e.To.LayoutRect);

			if ((mle = e as LayoutMultiEdgeEx) != null)
			{
				var pt = mle.multi_mid_adjust(points[0], points[1]);
				arr.InsertAt(ref points, 1, pt);
				return CanonicalSpline.Create(points, SplineTension, null, false, false);
			}

			return new LineGeometry { StartPoint = points[0], EndPoint = points[1] };
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		PathGeometry render_edge_labels(DrawingContext dc)
		{
			var tfp = TypefacePlus.Create(this, EdgeTextStyle);
			var clip = new PathGeometry();

			foreach (LayoutEdgeEx e in g_cur.Edges)
			{
				if (e.geom == null)
					continue;

				var r_geo = e.GetRawGeometryBounds();

				var tgc = e.TransformGroup.Children;
				if (tgc.Count == 2)
				{
					r_geo.Location = e.Location;		/// layout location computed by the panel
					var r_adj = coord_adjust(r_geo);	/// final translate vector
					var tt = new TranslateTransform(r_adj.X, r_adj.Y);
					e.AddTransform(tt);
				}

				Debug.Assert(tgc.Count == 3);
				/// [0] : translate the FormattedText to (0,0)
				/// [1] : rotation (or identity)
				/// [2] : actual translation (just added here if not previously added)

				r_geo = new Rect(r_geo.Size);
				r_geo.Inflate(1, 1);
				var cg = new RectangleGeometry(r_geo)
				{
					Transform = new TransformGroup { Children = { tgc[1], tgc[2] } }
				};
				clip.AddGeometry(cg);

				//dc.DrawGeometry(null, new Pen(Brushes.Red, 1), cg);

				dc.DrawGeometry(tfp.Foreground, null, e.geom);
			}
			return clip;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void render_background(DrawingContext dc, Rect r_render)
		{
			Brush fill;
			Double d;
			var pen = BorderPen;

			if (((fill = Background) == null || fill == Brushes.Transparent) && pen == null)
				return;

			if (math.IsZero(d = CornerRadius.TopLeft))
				dc.DrawRectangle(fill, pen, r_render);
			else
				dc.DrawRoundedRectangle(fill, pen, r_render, d, d);
		}
	};
}
