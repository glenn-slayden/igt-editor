using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace alib.Wpf
{
	using Math = System.Math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class lwt_base : FrameworkElement
	{
		public static DependencyProperty X1Property { get { return Line.X1Property; } }
		public static DependencyProperty Y1Property { get { return Line.Y1Property; } }
		public static DependencyProperty X2Property { get { return Line.X2Property; } }
		public static DependencyProperty Y2Property { get { return Line.Y2Property; } }

		static lwt_base()
		{
			X1Property.AddOwner(typeof(lwt_base), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
			Y1Property.AddOwner(typeof(lwt_base), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
			X2Property.AddOwner(typeof(lwt_base), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
			Y2Property.AddOwner(typeof(lwt_base), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
		}

		public double X1
		{
			get { return (double)GetValue(X1Property); }
			set { SetValue(X1Property, value); }
		}

		public double Y1
		{
			get { return (double)GetValue(Y1Property); }
			set { SetValue(Y1Property, value); }
		}

		public double X2
		{
			get { return (double)GetValue(X2Property); }
			set { SetValue(X2Property, value); }
		}

		public double Y2
		{
			get { return (double)GetValue(Y2Property); }
			set { SetValue(Y2Property, value); }
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class LineWithText2 : lwt_base
	{
		public LineWithText2(String text)
		{
			this.tb = new TextBlock();
			tb.Text = text;
			tb.FontSize = 16;
			tb.FontFamily = new FontFamily("Cambria");
			tb.HorizontalAlignment = HorizontalAlignment.Left;
			tb.VerticalAlignment = VerticalAlignment.Top;
			AddVisualChild(tb);

			this.L = new Line();
			L.Stroke = Brushes.Black;
			L.StrokeThickness = 1;
			L.StrokeDashArray = new DoubleCollection(new Double[] { 4, 4 });
			AddVisualChild(L);
		}

		Line L;
		TextBlock tb;

		protected override Size MeasureOverride(Size availableSize)
		{
			var pt1 = new Point(X1, Y1);
			var pt2 = new Point(X2, Y2);

			//pt1 = TransformToAncestor((Visual)Parent).Transform(pt1);
			//pt2 = TransformToAncestor((Visual)Parent).Transform(pt2);

			L.X1 = pt1.X;
			L.Y1 = pt1.Y;
			L.X2 = pt2.X;
			L.Y2 = pt2.Y;
			if (!L.IsMeasureValid)
				L.Measure(availableSize);
			
			//if (!tb.IsMeasureValid)
			//	tb.Measure(availableSize);

			return L.DesiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			L.Arrange(new Rect(finalSize));

			//Point midPoint = util.Midpoint(pt1, pt2);

			var text_origin = util.get_text_origin(tb.Text, tb.DesiredSize, 10, new Rotation(new Point(X1, Y1), new Point(X2, Y2)));

			text_origin = TransformToAncestor((Visual)Parent).Transform(text_origin);

			tb.Arrange(new Rect(text_origin, tb.DesiredSize));

			return L.DesiredSize;
		}

		protected override int VisualChildrenCount { get { return 2; } }

		protected override Visual GetVisualChild(int index)
		{
			return index == 0 ? (Visual)this.tb : (Visual)this.L;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class LineWithText : Shape
	{
		const double ArrowLength = 12;
		const double ArrowAngle = 45;
		const bool IsArrowClosed = false;

		public static DependencyProperty X1Property { get { return Line.X1Property; } }
		public static DependencyProperty Y1Property { get { return Line.Y1Property; } }
		public static DependencyProperty X2Property { get { return Line.X2Property; } }
		public static DependencyProperty Y2Property { get { return Line.Y2Property; } }

		static LineWithText()
		{
			X1Property.AddOwner(typeof(LineWithText), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, (o, e) => ((LineWithText)o).geo = null));
			Y1Property.AddOwner(typeof(LineWithText), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, (o, e) => ((LineWithText)o).geo = null));
			X2Property.AddOwner(typeof(LineWithText), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, (o, e) => ((LineWithText)o).geo = null));
			Y2Property.AddOwner(typeof(LineWithText), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, (o, e) => ((LineWithText)o).geo = null));
		}

		public double X1
		{
			get { return (double)GetValue(X1Property); }
			set { SetValue(X1Property, value); }
		}

		public double X2
		{
			get { return (double)GetValue(X2Property); }
			set { SetValue(X2Property, value); }
		}

		public double Y1
		{
			get { return (double)GetValue(Y1Property); }
			set { SetValue(Y1Property, value); }
		}

		public double Y2
		{
			get { return (double)GetValue(Y2Property); }
			set { SetValue(Y2Property, value); }
		}

		public LineWithText(String text)
		{
			this.text = text;
			this.tf = new Typeface(
				new FontFamily("Cambria"),//(FontFamily)GetValue(TextBlock.FontFamilyProperty),
				(FontStyle)GetValue(TextBlock.FontStyleProperty),
				FontWeights.Normal,//	(FontWeight)GetValue(TextBlock.FontWeightProperty),
				(FontStretch)GetValue(TextBlock.FontStretchProperty));

			//this.Fill = Brushes.Black;
			//this.Fill = Brushes.Gray;
			//this.Stroke = Brushes.Black;
			this.VisualTextHintingMode = TextHintingMode.Fixed;
			this.VisualTextRenderingMode = TextRenderingMode.Grayscale;
			this.Fill = Brushes.Black;

			//this.Stroke = Brushes.Gray;
			//this.StrokeThickness = 1;
		}

		GeometryGroup geo;
		String text;
		Typeface tf;

		protected override Geometry DefiningGeometry
		{
			get
			{
				if (geo == null)
					geo = geo_all();
				return geo;
			}
		}

		public static PathFigure ArrowheadFigure(Point pt1, Point pt2)
		{
			PathFigure pathfig = new PathFigure();
			PolyLineSegment polyseg = new PolyLineSegment();
			pathfig.Segments.Add(polyseg);

			System.Windows.Media.Matrix matx = new System.Windows.Media.Matrix();
			Vector vect = pt1 - pt2;
			vect.Normalize();
			vect *= ArrowLength;

			polyseg.Points.Clear();
			matx.Rotate(ArrowAngle / 2);
			pathfig.StartPoint = pt2 + vect * matx;
			polyseg.Points.Add(pt2);

			matx.Rotate(-ArrowAngle);
			polyseg.Points.Add(pt2 + vect * matx);
			pathfig.IsClosed = IsArrowClosed;

			return pathfig;
		}

		PathFigure LineSegFigure(Point pt1, Point pt2, Double text_height, out Double angle, out Vector adj)
		{
			var r = new Rotation(pt1, pt2);

			adj = new Vector(0, 0);
			angle = r.Angle;

			if (text.Length <= 2)
			{
				if (angle < -45)
				{
					angle += 90;
					adj.X = 8;
					adj.Y = text_height / 2;
				}
				else if (angle > 45)
				{
					angle -= 90;
					adj.X = -8;
					adj.Y = text_height / 2;
				}
			}

			var th = StrokeThickness;
			var a_rad = util.Atan2(pt1, pt2);

			return new PathFigure(pt1, new PathSegment[]
			{
				new PolyLineSegment
				{
					IsStroked = false,
					Points =
					{
						new Point(pt1.X - th * Math.Sin(a_rad), pt1.Y + th * Math.Cos(a_rad)),
						pt2,
						new Point(pt2.X + th * Math.Sin(a_rad), pt2.Y - th * Math.Cos(a_rad)),
					}
				},
			}, true);
		}

		Geometry txt_geom(FormattedText ft, Point pt1, Point pt2, Double angle, Vector adj)
		{
			var mid = util.Midpoint(pt1, pt2);

			var text_origin = new Point(mid.X - ft.Width / 2, mid.Y - ft.Height) + adj;

			var txt_geo = ft.BuildGeometry(text_origin);

			txt_geo.Transform = new RotateTransform(angle, mid.X, mid.Y);

			return txt_geo;
		}

		GeometryGroup geo_all()
		{
			var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 16, Brushes.Black);

			Double angle;
			Vector adj;
			var pt1 = new Point(X1, Y1);
			var pt2 = new Point(X2, Y2);

			return new GeometryGroup
			{
				Children =
				{
					new PathGeometry
					{
						Figures = 
						{
							LineSegFigure(pt1, pt2, ft.Height, out angle, out adj),
							ArrowheadFigure(pt2, pt1)
						}
					},
					txt_geom(ft, pt1, pt2, angle, adj)
				}
			};
		}
	};
}
