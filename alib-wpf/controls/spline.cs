using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace alib.Wpf
{
	using Math = System.Math;

	// (c) 2009 by Charles Petzold
	public class CanonicalSpline : Shape
	{
		public static PathGeometry Create(IReadOnlyList<Point> pts, Double tension, DoubleCollection tensions,
												 bool isClosed, bool isFilled, Double tolerance = Double.NaN)
		{
			if (Double.IsNaN(tolerance))
				tolerance = Geometry.StandardFlatteningTolerance;

			if (pts == null || pts.Count < 1)
				return null;

			var ps = new PolyLineSegment();
			var pg = new PathGeometry
			{
				Figures = 
				{
					new PathFigure
					{
						IsClosed = isClosed,
						IsFilled = isFilled,
						StartPoint = pts[0],
						Segments = { ps },
					}
				}
			};

			if (pts.Count < 2)
				return pg;

			else if (pts.Count == 2)
			{
				if (!isClosed)
				{
					Segment(ps.Points, pts[0], pts[0], pts[1], pts[1], tension, tension, tolerance);
				}
				else
				{
					Segment(ps.Points, pts[1], pts[0], pts[1], pts[0], tension, tension, tolerance);
					Segment(ps.Points, pts[0], pts[1], pts[0], pts[1], tension, tension, tolerance);
				}
			}
			else
			{
				bool useTensionCollection = tensions != null && tensions.Count > 0;

				for (int i = 0; i < pts.Count; i++)
				{
					var T1 = useTensionCollection ? tensions[i % tensions.Count] : tension;
					var T2 = useTensionCollection ? tensions[(i + 1) % tensions.Count] : tension;

					if (i == 0)
					{
						Segment(ps.Points, isClosed ? pts[pts.Count - 1] : pts[0], pts[0], pts[1], pts[2], T1, T2, tolerance);
					}
					else if (i == pts.Count - 2)
					{
						Segment(ps.Points, pts[i - 1], pts[i], pts[i + 1], isClosed ? pts[0] : pts[i + 1], T1, T2, tolerance);
					}
					else if (i == pts.Count - 1)
					{
						if (isClosed)
							Segment(ps.Points, pts[i - 1], pts[i], pts[0], pts[1], T1, T2, tolerance);
					}
					else
					{
						Segment(ps.Points, pts[i - 1], pts[i], pts[i + 1], pts[i + 2], T1, T2, tolerance);
					}
				}
			}
			return pg;
		}

		static void Segment(PointCollection points, Point pt0, Point pt1, Point pt2, Point pt3, Double T1, Double T2, Double tolerance)
		{
			Double SX1 = T1 * (pt2.X - pt0.X);
			Double SY1 = T1 * (pt2.Y - pt0.Y);
			Double SX2 = T2 * (pt3.X - pt1.X);
			Double SY2 = T2 * (pt3.Y - pt1.Y);

			Double AX = SX1 + SX2 + 2 * pt1.X - 2 * pt2.X;
			Double AY = SY1 + SY2 + 2 * pt1.Y - 2 * pt2.Y;
			Double BX = -2 * SX1 - SX2 - 3 * pt1.X + 3 * pt2.X;
			Double BY = -2 * SY1 - SY2 - 3 * pt1.Y + 3 * pt2.Y;

			Double CX = SX1;
			Double CY = SY1;
			Double DX = pt1.X;
			Double DY = pt1.Y;

			int num = (int)((Math.Abs(pt1.X - pt2.X) + Math.Abs(pt1.Y - pt2.Y)) / tolerance);

			// Notice begins at 1 so excludes the first point (which is just pt1)
			for (int i = 1; i < num; i++)
			{
				Double t = (Double)i / (num - 1);
				Point pt = new Point(AX * t * t * t + BX * t * t + CX * t + DX,
									 AY * t * t * t + BY * t * t + CY * t + DY);
				points.Add(pt);
			}
		}

		public static readonly DependencyProperty PointsProperty =
			DependencyProperty.Register("Points",
				typeof(IReadOnlyList<Point>),
				typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(null, OnMeasurePropertyChanged));

		public static readonly DependencyProperty TensionProperty =
			DependencyProperty.Register("Tension",
				typeof(Double),
				typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(0.5, OnMeasurePropertyChanged));

		public static readonly DependencyProperty TensionsProperty =
			DependencyProperty.Register("Tensions",
				typeof(DoubleCollection),
				typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(null, OnMeasurePropertyChanged));

		public static readonly DependencyProperty IsClosedProperty =
			PathFigure.IsClosedProperty.AddOwner(typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(false, OnMeasurePropertyChanged));

		public static readonly DependencyProperty IsFilledProperty =
			PathFigure.IsFilledProperty.AddOwner(typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(false, OnMeasurePropertyChanged));

		public static readonly DependencyProperty FillRuleProperty =
			Polyline.FillRuleProperty.AddOwner(typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(FillRule.EvenOdd, OnRenderPropertyChanged));

		public static readonly DependencyProperty ToleranceProperty =
			DependencyProperty.Register("Tolerance",
				typeof(Double),
				typeof(CanonicalSpline),
				new FrameworkPropertyMetadata(Geometry.StandardFlatteningTolerance, OnMeasurePropertyChanged));

		public IReadOnlyList<Point> Points
		{
			set { SetValue(PointsProperty, value); }
			get { return (IReadOnlyList<Point>)GetValue(PointsProperty); }
		}

		public Double Tension
		{
			set { SetValue(TensionProperty, value); }
			get { return (Double)GetValue(TensionProperty); }
		}

		public DoubleCollection Tensions
		{
			set { SetValue(TensionsProperty, value); }
			get { return (DoubleCollection)GetValue(TensionsProperty); }
		}

		public bool IsClosed
		{
			set { SetValue(IsClosedProperty, value); }
			get { return (bool)GetValue(IsClosedProperty); }
		}

		public bool IsFilled
		{
			set { SetValue(IsFilledProperty, value); }
			get { return (bool)GetValue(IsFilledProperty); }
		}

		public FillRule FillRule
		{
			set { SetValue(FillRuleProperty, value); }
			get { return (FillRule)GetValue(FillRuleProperty); }
		}

		public Double Tolerance
		{
			set { SetValue(ToleranceProperty, value); }
			get { return (Double)GetValue(ToleranceProperty); }
		}

		PathGeometry pathGeometry;

		protected override Geometry DefiningGeometry { get { return pathGeometry; } }

		static void OnMeasurePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((CanonicalSpline)o).OnMeasurePropertyChanged(e);
		}

		void OnMeasurePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			pathGeometry = Create(Points, Tension, Tensions, IsClosed, IsFilled, Tolerance);
			InvalidateMeasure();
			OnRenderPropertyChanged(args);
		}

		static void OnRenderPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((CanonicalSpline)o).OnRenderPropertyChanged(e);
		}

		void OnRenderPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (pathGeometry != null)
				pathGeometry.FillRule = FillRule;

			InvalidateVisual();
		}
	};
}
