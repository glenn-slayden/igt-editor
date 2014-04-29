using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace alib.Wpf
{
	[Flags]
	public enum ArrowEnds
	{
		None = 0,
		Start = 1,
		End = 2,
		Both = 3
	};

	/// <summary>
	///     Provides a base class for ArrowLine and ArrowPolyline.
	///     This class is abstract.
	/// </summary>
	public abstract class ArrowLineBase : Shape
	{
		protected PathGeometry pathgeo;
		protected PathFigure pathfigLine;
		protected PolyLineSegment polysegLine;

		PathFigure pathfigHead1;
		PolyLineSegment polysegHead1;
		PathFigure pathfigHead2;
		PolyLineSegment polysegHead2;

		/// <summary>
		///     Identifies the ArrowAngle dependency property.
		/// </summary>
		public static readonly DependencyProperty ArrowAngleProperty =
			DependencyProperty.Register("ArrowAngle",
				typeof(double), typeof(ArrowLineBase),
				new FrameworkPropertyMetadata(45.0,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the angle between the two sides of the arrowhead.
		/// </summary>
		public double ArrowAngle
		{
			set { SetValue(ArrowAngleProperty, value); }
			get { return (double)GetValue(ArrowAngleProperty); }
		}

		/// <summary>
		///     Identifies the ArrowLength dependency property.
		/// </summary>
		public static readonly DependencyProperty ArrowLengthProperty =
			DependencyProperty.Register("ArrowLength",
				typeof(double), typeof(ArrowLineBase),
				new FrameworkPropertyMetadata(12.0,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the length of the two sides of the arrowhead.
		/// </summary>
		public double ArrowLength
		{
			set { SetValue(ArrowLengthProperty, value); }
			get { return (double)GetValue(ArrowLengthProperty); }
		}

		/// <summary>
		///     Identifies the ArrowEnds dependency property.
		/// </summary>
		public static readonly DependencyProperty ArrowEndsProperty =
			DependencyProperty.Register("ArrowEnds",
				typeof(ArrowEnds), typeof(ArrowLineBase),
				new FrameworkPropertyMetadata(ArrowEnds.End,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the property that determines which ends of the
		///     line have arrows.
		/// </summary>
		public ArrowEnds ArrowEnds
		{
			set { SetValue(ArrowEndsProperty, value); }
			get { return (ArrowEnds)GetValue(ArrowEndsProperty); }
		}

		/// <summary>
		///     Identifies the IsArrowClosed dependency property.
		/// </summary>
		public static readonly DependencyProperty IsArrowClosedProperty =
			DependencyProperty.Register("IsArrowClosed",
				typeof(bool), typeof(ArrowLineBase),
				new FrameworkPropertyMetadata(false,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the property that determines if the arrow head
		///     is closed to resemble a triangle.
		/// </summary>
		public bool IsArrowClosed
		{
			set { SetValue(IsArrowClosedProperty, value); }
			get { return (bool)GetValue(IsArrowClosedProperty); }
		}

		/// <summary>
		///     Initializes a new instance of ArrowLineBase.
		/// </summary>
		public ArrowLineBase()
		{
			pathgeo = new PathGeometry();

			pathfigLine = new PathFigure();
			polysegLine = new PolyLineSegment();
			pathfigLine.Segments.Add(polysegLine);

			pathfigHead1 = new PathFigure();
			polysegHead1 = new PolyLineSegment();
			pathfigHead1.Segments.Add(polysegHead1);

			pathfigHead2 = new PathFigure();
			polysegHead2 = new PolyLineSegment();
			pathfigHead2.Segments.Add(polysegHead2);
		}

		/// <summary>
		///     Gets a value that represents the Geometry of the ArrowLine.
		/// </summary>
		protected override Geometry DefiningGeometry
		{
			get
			{
				int count = polysegLine.Points.Count;

				if (count > 0)
				{
					// Draw the arrow at the start of the line.
					if ((ArrowEnds & ArrowEnds.Start) == ArrowEnds.Start)
					{
						Point pt1 = pathfigLine.StartPoint;
						Point pt2 = polysegLine.Points[0];
						pathgeo.Figures.Add(CalculateArrow(pathfigHead1, pt2, pt1));
					}

					// Draw the arrow at the end of the line.
					if ((ArrowEnds & ArrowEnds.End) == ArrowEnds.End)
					{
						Point pt1 = count == 1 ? pathfigLine.StartPoint :
												 polysegLine.Points[count - 2];
						Point pt2 = polysegLine.Points[count - 1];
						pathgeo.Figures.Add(CalculateArrow(pathfigHead2, pt1, pt2));
					}
				}
				return pathgeo;
			}
		}

		PathFigure CalculateArrow(PathFigure pathfig, Point pt1, Point pt2)
		{
			System.Windows.Media.Matrix matx = new System.Windows.Media.Matrix();
			Vector vect = pt1 - pt2;
			vect.Normalize();
			vect *= ArrowLength;

			PolyLineSegment polyseg = pathfig.Segments[0] as PolyLineSegment;
			polyseg.Points.Clear();
			matx.Rotate(ArrowAngle / 2);
			pathfig.StartPoint = pt2 + vect * matx;
			polyseg.Points.Add(pt2);

			matx.Rotate(-ArrowAngle);
			polyseg.Points.Add(pt2 + vect * matx);
			pathfig.IsClosed = IsArrowClosed;

			return pathfig;
		}
	};


	/// <summary>
	///     Draws a straight line between two points with 
	///     optional arrows on the ends.
	/// </summary>
	public class ArrowLine : ArrowLineBase
	{
		/// <summary>
		///     Identifies the X1 dependency property.
		/// </summary>
		public static readonly DependencyProperty X1Property =
			DependencyProperty.Register("X1",
				typeof(double), typeof(ArrowLine),
				new FrameworkPropertyMetadata(0.0,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the x-coordinate of the ArrowLine start point.
		/// </summary>
		public double X1
		{
			set { SetValue(X1Property, value); }
			get { return (double)GetValue(X1Property); }
		}

		/// <summary>
		///     Identifies the Y1 dependency property.
		/// </summary>
		public static readonly DependencyProperty Y1Property =
			DependencyProperty.Register("Y1",
				typeof(double), typeof(ArrowLine),
				new FrameworkPropertyMetadata(0.0,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the y-coordinate of the ArrowLine start point.
		/// </summary>
		public double Y1
		{
			set { SetValue(Y1Property, value); }
			get { return (double)GetValue(Y1Property); }
		}

		/// <summary>
		///     Identifies the X2 dependency property.
		/// </summary>
		public static readonly DependencyProperty X2Property =
			DependencyProperty.Register("X2",
				typeof(double), typeof(ArrowLine),
				new FrameworkPropertyMetadata(0.0,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the x-coordinate of the ArrowLine end point.
		/// </summary>
		public double X2
		{
			set { SetValue(X2Property, value); }
			get { return (double)GetValue(X2Property); }
		}

		/// <summary>
		///     Identifies the Y2 dependency property.
		/// </summary>
		public static readonly DependencyProperty Y2Property =
			DependencyProperty.Register("Y2",
				typeof(double), typeof(ArrowLine),
				new FrameworkPropertyMetadata(0.0,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets the y-coordinate of the ArrowLine end point.
		/// </summary>
		public double Y2
		{
			set { SetValue(Y2Property, value); }
			get { return (double)GetValue(Y2Property); }
		}

		/// <summary>
		///     Gets a value that represents the Geometry of the ArrowLine.
		/// </summary>
		protected override Geometry DefiningGeometry
		{
			get
			{
				// Clear out the PathGeometry.
				pathgeo.Figures.Clear();

				// Define a single PathFigure with the points.
				pathfigLine.StartPoint = new Point(X1, Y1);
				polysegLine.Points.Clear();
				polysegLine.Points.Add(new Point(X2, Y2));
				pathgeo.Figures.Add(pathfigLine);

				// Call the base property to add arrows on the ends.
				return base.DefiningGeometry;
			}
		}
	};

	/// <summary>
	///     Draws a series of connected straight lines with
	///     optional arrows on the ends.
	/// </summary>
	public class ArrowPolyline : ArrowLineBase
	{
		/// <summary>
		///     Identifies the Points dependency property.
		/// </summary>
		public static readonly DependencyProperty PointsProperty =
			DependencyProperty.Register("Points",
				typeof(PointCollection), typeof(ArrowPolyline),
				new FrameworkPropertyMetadata(null,
						FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		///     Gets or sets a collection that contains the 
		///     vertex points of the ArrowPolyline.
		/// </summary>
		public PointCollection Points
		{
			set { SetValue(PointsProperty, value); }
			get { return (PointCollection)GetValue(PointsProperty); }
		}

		/// <summary>
		///     Initializes a new instance of the ArrowPolyline class. 
		/// </summary>
		public ArrowPolyline()
		{
			Points = new PointCollection();
		}

		/// <summary>
		///     Gets a value that represents the Geometry of the ArrowPolyline.
		/// </summary>
		protected override Geometry DefiningGeometry
		{
			get
			{
				// Clear out the PathGeometry.
				pathgeo.Figures.Clear();

				// Try to avoid unnecessary indexing exceptions.
				if (Points.Count > 0)
				{
					// Define a PathFigure containing the points.
					pathfigLine.StartPoint = Points[0];
					polysegLine.Points.Clear();

					for (int i = 1; i < Points.Count; i++)
						polysegLine.Points.Add(Points[i]);

					pathgeo.Figures.Add(pathfigLine);
				}

				// Call the base property to add arrows on the ends.
				return base.DefiningGeometry;
			}
		}
	};

}
