using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

using alib.Math;

namespace alib.Wpf
{
	using Math = System.Math;
	using String = System.String;

	public static partial class util
	{
		public static readonly Size infinite_size;

		public static readonly Size zero_size;

		public static readonly Point coord_origin;

		public static readonly Point point_NaN;

		public static readonly Rect zero_rect;

		/// <summary>
		/// Use this (instead of Rect.IsEmpty) when you want to detect rectangles which have width
		/// and/or height of zero. Also returns true for Rect.Empty, so that return values from (e.g.) 
		/// VisualTreeHelper functions and other WPF places are handled correctly
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZeroSize(this Rect r)
		{
			return IsZero(r.Size);
		}

		/// <summary>
		/// Use this (instead of Size.IsEmpty) when you want to detect sizes which have width
		/// and/or height of zero. To facilitate use with Rect.Empty, any negative values are considered
		/// zero (Rect is supposedly designed so that only Rect.Empty can obtain that state)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero(this Size sz)
		{
			return sz.Width < math.ε || sz.Height < math.ε;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero(this CornerRadius cr)
		{
			return cr.TopLeft < math.ε && cr.TopRight < math.ε && cr.BottomRight < math.ε && cr.BottomLeft < math.ε;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero(this Thickness th)
		{
			return th.Left < math.ε && th.Top < math.ε && th.Right < math.ε && th.Bottom < math.ε;
		}

		public static void Maximize(ref Thickness th, Thickness other)
		{
			if (other.Left > th.Left)
				th.Left = other.Left;
			if (other.Top > th.Top)
				th.Top = other.Top;
			if (other.Right > th.Right)
				th.Right = other.Right;
			if (other.Bottom > th.Bottom)
				th.Bottom = other.Bottom;
		}
		public static Size OuterSize(this Thickness th)
		{
			return new Size(th.Left + th.Right, th.Top + th.Bottom);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFinite(this Point pt)
		{
			return math.IsFinite(pt.X) && math.IsFinite(pt.Y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFinite(this Vector vv)
		{
			return math.IsFinite(vv.X) && math.IsFinite(vv.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInfinite(this Rect r)
		{
			return r.Size == infinite_size;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect Inflate(this Rect r, Thickness th)
		{
			return new Rect(
				r.X - th.Left,
				r.Y - th.Top,
				r.Width + th.Left + th.Right,
				r.Height + th.Top + th.Bottom);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect Deflate(this Rect r, Thickness th)
		{
			return new Rect(
				r.X + th.Left,
				r.Y + th.Top,
				r.Width - (th.Left + th.Right),
				r.Height - (th.Top + th.Bottom));
		}

		public static Rect Divide(this Rect r, Double d) { return new Rect(r.X / d, r.Y / d, r.Width / d, r.Height / d); }

		public static Size Divide(this Size sz, Double d) { return new Size(sz.Width / d, sz.Height / d); }

		public static Size Divide(this Size sz1, Size sz2) { return new Size(sz1.Width / sz2.Width, sz1.Height / sz2.Height); }

		public static Size Multiply(this Size sz, Double d) { return new Size(sz.Width * d, sz.Height * d); }

		public static Size Multiply(this Size sz1, Size sz2) { return new Size(sz1.Width * sz2.Width, sz1.Height * sz2.Height); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Maximize(ref Size sz1, Size sz2)
		{
			if (sz2.Width > sz1.Width)
				sz1.Width = sz2.Width;
			if (sz2.Height > sz1.Height)
				sz1.Height = sz2.Height;
		}

		public static Point Midpoint(Point p1, Point p2)
		{
			Double d;
			return new Point((d = (p2.X - p1.X) / 2.0) < 0 ? p2.X - d : p1.X + d,
							 (d = (p2.Y - p1.Y) / 2.0) < 0 ? p2.Y - d : p1.Y + d);
		}

		public static Point Midpoint(this LineGeometry lg)
		{
			return Midpoint(lg.StartPoint, lg.EndPoint);
		}

		public static bool PointInTriangle(Point p, Point p0, Point p1, Point p2)
		{
			var s = p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y;
			var t = p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y;

			if ((s < 0) != (t < 0))
				return false;

			var A = -p1.Y * p2.X + p0.Y * (p2.X - p1.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y;
			if (A < 0.0)
			{
				s = -s;
				t = -t;
				A = -A;
			}
			return s > 0 && t > 0 && (s + t) < A;
		}

		public static Double Atan2(Point p1, Point p2)
		{
			return Math.Atan2(p1.Y - p2.Y, p1.X - p2.X);
		}
		public static Double Atan2(Points pts)
		{
			return Math.Atan2(pts.P2.Y - pts.P1.Y, pts.P2.X - pts.P1.X);
		}
		public static Double Atan2(Rect r)
		{
			return Math.Atan2(r.Height, r.Width);
		}
		public static Double Atan2(Point p)
		{
			return Math.Atan2(p.Y, p.X);
		}

		public static Double Angle(this LineGeometry lg)
		{
			Double angle = Atan2(lg.StartPoint, lg.EndPoint) * math.PiRad;

			if (angle < -90)
				angle += 180;
			else if (angle > 90)
				angle -= 180;

			return angle;
		}

		public static String show(this Rect r)
		{
			return String.Format("[{0,8:###.0}, {1,-8:###.0} - {2,8:###.0} x {3,-8:###.0}]", r.Left, r.Top, r.Width, r.Height);
		}

		//public static Point OriginSkew(this Geometry geo, Pen pen)
		//{
		//	var pt = geo.GetRenderBounds(pen).TopLeft;
		//	pt.X = -pt.X;
		//	pt.Y = -pt.Y;
		//	return pt;
		//}

		//public static Vector RenderOriginOffset(this Geometry geo, Pen pen)
		//{
		//	return geo.GetRenderBounds(pen).TopLeft - Util.coord_origin;
		//}

		public static Vector RenderOriginOffset(this Geometry geo, Pen pen)
		{
			return geo.Bounds.TopLeft - geo.GetRenderBounds(pen).TopLeft;
		}

		public static void TranslateToRenderOrigin(this Geometry geo, Pen pen)
		{
			var v = RenderOriginOffset(geo, pen);
			geo.Transform = new TranslateTransform { X = v.X, Y = v.Y, };
		}

		public static Rect GetRect(this Window w)
		{
			return new Rect(w.Left, w.Top, w.Width, w.Height);
		}
		public static void SetRect(this Window w, Rect r)
		{
			w.Left = r.Left;
			w.Top = r.Top;
			w.Width = r.Width;
			w.Height = r.Height;
		}

		public static Point Origin(this TranslateTransform tt)
		{
			return tt == null ? util.coord_origin : new Point(tt.X, tt.Y);
		}
		public static void SetOrigin(this TranslateTransform tt, Point pt)
		{
			tt.X = pt.X;
			tt.Y = pt.Y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point RectOriginForCenter(this Point center, Size size)
		{
			center.X -= size.Width / 2.0;
			center.Y -= size.Height / 2.0;
			return center;
		}
		public static Rect MakeCenteredRect(this Point center, Size size)
		{
			return new Rect(
				center.X - size.Width / 2.0,
				center.Y - size.Height / 2.0,
				size.Width,
				size.Height);
		}
		public static Rect MakeCenteredRect(this Point center, Double xy_offset)
		{
			return new Rect(center.X - xy_offset, center.Y - xy_offset, xy_offset += xy_offset, xy_offset);
		}

		public static Double HCenter(this Rect r)
		{
			return r.Left + r.Width / 2;
		}
		public static Double VCenter(this Rect r)
		{
			return r.Top + r.Height / 2;
		}
		public static Point Center(this Rect r)
		{
			return new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
		}
		public static Point LeftCenter(this Rect r)
		{
			return new Point(r.Left, r.Top + r.Height / 2);
		}
		public static Point TopCenter(this Rect r)
		{
			return new Point(r.Left + r.Width / 2, r.Top);
		}
		public static Point RightCenter(this Rect r)
		{
			return new Point(r.Right, r.Top + r.Height / 2);
		}
		public static Point BottomCenter(this Rect r)
		{
			return new Point(r.Left + r.Width / 2, r.Bottom);
		}

		public static Point Offset(this DrawingGroup dg) { return dg.Bounds.TopLeft; }

		public static Point Center(this DrawingGroup dg) { return dg.Bounds.Center(); }

		public static Point LeftCenter(this DrawingGroup dg) { return dg.Bounds.LeftCenter(); }

		public static Point TopCenter(this DrawingGroup dg) { return dg.Bounds.TopCenter(); }

		public static Point RightCenter(this DrawingGroup dg) { return dg.Bounds.RightCenter(); }

		public static Point BottomCenter(this DrawingGroup dg) { return dg.Bounds.BottomCenter(); }

		public static IEnumerable<Point> Interpolate(this IEnumerable<Point> points)
		{
			var ee = points.GetEnumerator();
			if (ee.MoveNext())
			{
				Point pt_prv;
				yield return pt_prv = ee.Current;

				while (ee.MoveNext())
				{
					yield return util.Midpoint(pt_prv, pt_prv = ee.Current);
					yield return pt_prv;
				}
			}
		}

		public static Point Center(this RotateTransform xform) { return new Point(xform.CenterX, xform.CenterY); }

		public static void SetCenter(this RotateTransform xform, Point pt)
		{
			xform.CenterX = pt.X;
			xform.CenterY = pt.Y;
		}

		public static Vector Offset(this TranslateTransform xform) { return new Vector(xform.X, xform.Y); }

		public static Point get_text_origin(String text, Size sz, Double adj, Rotation r)
		{
			Double x_adj = 0;
			Double y_adj = 0;
			if (text.Length <= 2)
			{
				if (r.Angle < -45)
				{
					r.Angle += 90;
					x_adj = adj;
					y_adj = sz.Height / 2;
				}
				else if (r.Angle > 45)
				{
					r.Angle -= 90;
					x_adj = -adj;
					y_adj = sz.Height / 2;
				}
			}

			var text_origin = new Point(r.Center.X - sz.Width / 2, r.Center.Y - sz.Height / 2);

			if (x_adj != 0)
				text_origin.X += x_adj;
			if (y_adj != 0)
				text_origin.Y += y_adj;

			return text_origin;
		}

		public static Point get_text_origin(this FormattedText ft, Rotation r)
		{
			return get_text_origin(ft.Text, new Size(ft.Width, ft.Height), 8, r);
		}

		public static Geometry RotateTextGeometry(this FormattedText ft, Rotation r, out RectangleGeometry rect_geo)
		{
			var pt = ft.get_text_origin(r);
			var txt_geo = ft.BuildGeometry(pt);

			var xform = r.RotateTransform;

			var rect = txt_geo.Bounds;
			if (rect.IsEmpty)
				rect = new Rect(pt, util.zero_size);
			rect.Inflate(3, 3);
			rect_geo = new RectangleGeometry(rect) { Transform = xform };

			txt_geo.Transform = xform;

#if line_with_arrow
			var stroke_adj = new Point(StrokeThickness * Math.Sin(a_rad), StrokeThickness * Math.Cos(a_rad));

			var pfs = new PathFigure(pt1, new PathSegment[]
			{
				new PolyLineSegment
				{
					IsStroked = false,
					Points = 
					{
						new Point(pt1.X - stroke_adj.X, pt1.Y + stroke_adj.Y),
						pt2,
						new Point(pt2.X + stroke_adj.X, pt2.Y - stroke_adj.Y),
					}
				},
			}, true);

			//geo = new GeometryGroup();
			////geo.Children.Add(pg);
			////geo.Children.Add(new PathGeometry(new PathFigure[] { pfs }));
			////geo.Children.Add(new PathGeometry(new PathFigure[] { CalculateArrow(endPoint, startPoint) }));
			//geo.Children.Add(new CombinedGeometry(
			//    GeometryCombineMode.Union,
			//    new PathGeometry(new PathFigure[] { pfs }),
			//    new PathGeometry(new PathFigure[] { CalculateArrow(endPoint, startPoint) })));
			//geo.Children.Add(txt_geo);
#endif

			return txt_geo;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public struct Points
	{
		public Points(Point P1, Point P2)
		{
			this.P1 = P1;
			this.P2 = P2;
		}
		public Point P1, P2;
		public Point Midpoint { get { return util.Midpoint(P1, P2); } }
		public void Swap()
		{
			var _tmp = this.P1;
			this.P1 = P2;
			this.P2 = _tmp;
		}
		public Rect Rect { get { return new Rect(P1, P2); } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public struct Rotation
	{
		public Rotation(Point center, Double Angle)
		{
			this.Center = center;
			this.Angle = Angle;
		}
		public Rotation(Point pt1, Point pt2)
		{
			this.Center = util.Midpoint(pt1, pt2);
			this.Angle = util.Atan2(pt2, pt1) * math.PiRad;

			if (Angle < -90)
				Angle += 180;
			else if (Angle > 90)
				Angle -= 180;
		}
		public Rotation(Points pts)
			: this(pts.P1, pts.P2)
		{
		}
		public Rotation(LineGeometry lg)
			: this(lg.StartPoint, lg.EndPoint)
		{
		}

		public Point Center;
		public Double Angle;

		public void OffsetCenter(Double dX, Double dY)
		{
			Center.X += dX;
			Center.Y += dY;
		}

		public RotateTransform RotateTransform { get { return new RotateTransform(Angle, Center.X, Center.Y); } }
	};
}
