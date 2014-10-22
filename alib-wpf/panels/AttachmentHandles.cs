#define POINT

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace alib.Wpf
{
	using String = System.String;

	public class AttachmentHandles : ContentControl
	{
		public static readonly DependencyProperty HostParentProperty;

		public static bool GetHostParent(DependencyObject obj)
		{
			return (bool)obj.GetValue(HostParentProperty);
		}
		public static void SetHostParent(DependencyObject obj, bool value)
		{
			obj.SetValue(HostParentProperty, value);
		}

#if POINT
		public static readonly DependencyProperty TopHandleProperty;
		public static readonly DependencyProperty LeftHandleProperty;
		public static readonly DependencyProperty RightHandleProperty;
		public static readonly DependencyProperty BottomHandleProperty;
		public static readonly DependencyProperty InnerLeftHandleProperty;
		public static readonly DependencyProperty InnerCenterHandleProperty;
		public static readonly DependencyProperty InnerRightHandleProperty;
#else
		public static readonly DependencyProperty TopXHandleProperty;
		public static readonly DependencyProperty TopYHandleProperty;
		public static readonly DependencyProperty LeftXHandleProperty;
		public static readonly DependencyProperty LeftYHandleProperty;
		public static readonly DependencyProperty RightXHandleProperty;
		public static readonly DependencyProperty RightYHandleProperty;
		public static readonly DependencyProperty BottomXHandleProperty;
		public static readonly DependencyProperty BottomYHandleProperty;
#endif
		static AttachmentHandles()
		{
			HostParentProperty = DependencyProperty.RegisterAttached("HostParent", typeof(bool), typeof(AttachmentHandles));
#if POINT
			TopHandleProperty = DependencyProperty.Register("TopHandle", typeof(Point), typeof(AttachmentHandles));
			LeftHandleProperty = DependencyProperty.Register("LeftHandle", typeof(Point), typeof(AttachmentHandles));
			RightHandleProperty = DependencyProperty.Register("RightHandle", typeof(Point), typeof(AttachmentHandles));
			BottomHandleProperty = DependencyProperty.Register("BottomHandle", typeof(Point), typeof(AttachmentHandles));
			InnerLeftHandleProperty = DependencyProperty.Register("InnerLeftHandle", typeof(Point), typeof(AttachmentHandles));
			InnerCenterHandleProperty = DependencyProperty.Register("InnerCenterHandle", typeof(Point), typeof(AttachmentHandles));
			InnerRightHandleProperty = DependencyProperty.Register("InnerRightHandle", typeof(Point), typeof(AttachmentHandles));
#else
			TopXHandleProperty = DependencyProperty.Register("TopXHandle", typeof(Double), typeof(AttachmentHandles));
			TopYHandleProperty = DependencyProperty.Register("TopYHandle", typeof(Double), typeof(AttachmentHandles));
			LeftXHandleProperty = DependencyProperty.Register("LeftXHandle", typeof(Double), typeof(AttachmentHandles));
			LeftYHandleProperty = DependencyProperty.Register("LeftYHandle", typeof(Double), typeof(AttachmentHandles));
			RightXHandleProperty = DependencyProperty.Register("RightXHandle", typeof(Double), typeof(AttachmentHandles));
			RightYHandleProperty = DependencyProperty.Register("RightYHandle", typeof(Double), typeof(AttachmentHandles));
			BottomXHandleProperty = DependencyProperty.Register("BottomXHandle", typeof(Double), typeof(AttachmentHandles));
			BottomYHandleProperty = DependencyProperty.Register("BottomYHandle", typeof(Double), typeof(AttachmentHandles));
#endif
		}
#if POINT
		public Point TopHandle
		{
			get { return (Point)GetValue(TopHandleProperty); }
			set { SetValue(TopHandleProperty, value); }
		}
		public Point LeftHandle
		{
			get { return (Point)GetValue(LeftHandleProperty); }
			set { SetValue(LeftHandleProperty, value); }
		}
		public Point RightHandle
		{
			get { return (Point)GetValue(RightHandleProperty); }
			set { SetValue(RightHandleProperty, value); }
		}
		public Point BottomHandle
		{
			get { return (Point)GetValue(BottomHandleProperty); }
			set { SetValue(BottomHandleProperty, value); }
		}
		public Point InnerLeftHandle
		{
			get { return (Point)GetValue(InnerLeftHandleProperty); }
			set { SetValue(InnerLeftHandleProperty, value); }
		}
		public Point InnerCenterHandle
		{
			get { return (Point)GetValue(InnerCenterHandleProperty); }
			set { SetValue(InnerCenterHandleProperty, value); }
		}
		public Point InnerRightHandle
		{
			get { return (Point)GetValue(InnerRightHandleProperty); }
			set { SetValue(InnerRightHandleProperty, value); }
		}
#else

		public Double TopXHandle
		{
			get { return (Double)GetValue(TopXHandleProperty); }
			set { SetValue(TopXHandleProperty, value); }
		}
		public Double TopYHandle
		{
			get { return (Double)GetValue(TopYHandleProperty); }
			set { SetValue(TopYHandleProperty, value); }
		}
		public Double LeftXHandle
		{
			get { return (Double)GetValue(LeftXHandleProperty); }
			set { SetValue(LeftXHandleProperty, value); }
		}
		public Double LeftYHandle
		{
			get { return (Double)GetValue(LeftYHandleProperty); }
			set { SetValue(LeftYHandleProperty, value); }
		}
		public Double RightXHandle
		{
			get { return (Double)GetValue(RightXHandleProperty); }
			set { SetValue(RightXHandleProperty, value); }
		}
		public Double RightYHandle
		{
			get { return (Double)GetValue(RightYHandleProperty); }
			set { SetValue(RightYHandleProperty, value); }
		}
		public Double BottomXHandle
		{
			get { return (Double)GetValue(BottomXHandleProperty); }
			set { SetValue(BottomXHandleProperty, value); }
		}
		public Double BottomYHandle
		{
			get { return (Double)GetValue(BottomYHandleProperty); }
			set { SetValue(BottomYHandleProperty, value); }
		}
#endif
		public AttachmentHandles()
		{
			//if (DesignerProperties.GetIsInDesignMode(this))
			//    return;
			LayoutUpdated += new EventHandler(AttachmentHandles_LayoutUpdated);
		}

		//protected override void OnChildDesiredSizeChanged(UIElement child)
		//{
		//    base.OnChildDesiredSizeChanged(child);
		//    AttachmentHandles_LayoutUpdated(null, null);
		//}

		FrameworkElement FindParent(FrameworkElement el)
		{
			while ((el = el.Parent as FrameworkElement) != null && !((bool)el.GetValue(HostParentProperty)))
				;
			return el;
		}

		void AttachmentHandles_LayoutUpdated(Object sender, EventArgs e)
		{
			FrameworkElement par = FindParent(this);
			if (par == null)
				return;
			Rect r = VisualTreeHelper.GetDescendantBounds(this);
			if (r.IsZeroSize())
				return;
			Point pt = TransformToAncestor(par).Transform(default(Point));
			r.Offset(pt.X, pt.Y);

			//Debug.Print("{0} {1} {2:N2} {3:N2} {4:N2} {5:N2}", 
			//    this.GetType().Name, 
			//    par.GetType().Name,
			//    r.Left, r.Top, r.Width, r.Height);

			Double half_width = r.Width / 2;
			Double half_height = r.Height / 2;
			Double v_center = r.Top + half_height;
			Double h_center = r.Left + half_width;
#if POINT
			TopHandle = new Point(h_center, r.Top);
			LeftHandle = new Point(r.Left, v_center);
			RightHandle = new Point(r.Right, v_center);
			BottomHandle = new Point(h_center, r.Bottom);

			InnerLeftHandle = new Point(r.Left + r.Width / 7, v_center);
			InnerCenterHandle = new Point(h_center, v_center);
			InnerRightHandle = new Point(r.Right - r.Width / 7, v_center);
#else
			//TopXHandle = new Point(r.Width / 2, 0);
			//LeftXHandle = new Point(0, r.Height / 2);
			//RightXHandle = new Point(r.Width, r.Height / 2);
			//BottomXHandle = new Point(r.Width / 2, r.Height);
#endif
		}

		public static Shape ConnectInnerLefts(AttachmentHandles h1, AttachmentHandles h2, String text = null)
		{
			if (text == null)
			{
				Line l = new Line
				{
					Stroke = Brushes.Gray,
					//StrokeThickness = 2.5,
					StrokeThickness = 1,
					//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 });
				};
				Panel.SetZIndex(l, -1);
				BindingOperations.SetBinding(l, Line.X1Property, new Binding
				{
					Source = h2,
					Path = new PropertyPath("InnerLeftHandle.X"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.Y1Property, new Binding
				{
					Source = h2,
					Path = new PropertyPath("InnerLeftHandle.Y"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.X2Property, new Binding
				{
					Source = h1,
					Path = new PropertyPath("InnerLeftHandle.X"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.Y2Property, new Binding
				{
					Source = h1,
					Path = new PropertyPath("InnerLeftHandle.Y"),
					Mode = BindingMode.OneWay,
				});
				return l;
			}

			LineWithText L = new LineWithText(text);
			Panel.SetZIndex(L, -1);
			//Stroke = Brushes.Gray;
			//StrokeThickness = 2.5;
			L.StrokeThickness = 1;
			//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 });

			BindingOperations.SetBinding(L, LineWithText.X1Property, new Binding
			{
				Source = h2,
				Path = new PropertyPath("InnerLeftHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText.Y1Property, new Binding
			{
				Source = h2,
				Path = new PropertyPath("InnerLeftHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText.X2Property, new Binding
			{
				Source = h1,
				Path = new PropertyPath("InnerLeftHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText.Y2Property, new Binding
			{
				Source = h1,
				Path = new PropertyPath("InnerLeftHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			return L;
		}

		public static FrameworkElement ConnectInnerRights(AttachmentHandles h1, AttachmentHandles h2, String text = null)
		{
			if (text == null)
			{
				Line l = new Line
				{
					Stroke = Brushes.Black,
					//StrokeThickness = 2.5;
					StrokeThickness = 1,
					StrokeDashArray = new DoubleCollection(new Double[] { 4, 4 }),
				};
				Panel.SetZIndex(l, -1);

				BindingOperations.SetBinding(l, Line.X1Property, new Binding
				{
					Source = h2,
					Path = new PropertyPath("InnerRightHandle.X"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.Y1Property, new Binding
				{
					Source = h2,
					Path = new PropertyPath("InnerRightHandle.Y"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.X2Property, new Binding
				{
					Source = h1,
					Path = new PropertyPath("InnerRightHandle.X"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.Y2Property, new Binding
				{
					Source = h1,
					Path = new PropertyPath("InnerRightHandle.Y"),
					Mode = BindingMode.OneWay,
				});
				return l;
			}

			LineWithText2 L = new LineWithText2(text);
			Panel.SetZIndex(L, -1);
			//Stroke = Brushes.Gray;
			//StrokeThickness = 2.5;
			//StrokeThickness = 1;
			//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 });

			BindingOperations.SetBinding(L, LineWithText2.X1Property, new Binding
			{
				Source = h2,
				Path = new PropertyPath("InnerRightHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText2.Y1Property, new Binding
			{
				Source = h2,
				Path = new PropertyPath("InnerRightHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText2.X2Property, new Binding
			{
				Source = h1,
				Path = new PropertyPath("InnerRightHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText2.Y2Property, new Binding
			{
				Source = h1,
				Path = new PropertyPath("InnerRightHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			return L;

		}

		public static Shape ConnectInnerLeftRight(AttachmentHandles left, AttachmentHandles right, String text = null)
		{
			if (text == null)
			{
				Line l = new Line
				{
					Stroke = Brushes.Gray,
					//StrokeThickness = 2.5,
					StrokeThickness = 1,
					//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 }),
				};
				Panel.SetZIndex(l, -1);

				BindingOperations.SetBinding(l, Line.X1Property, new Binding
				{
					Source = left,
					Path = new PropertyPath("InnerLeftHandle.X"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.Y1Property, new Binding
				{
					Source = left,
					Path = new PropertyPath("InnerLeftHandle.Y"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.X2Property, new Binding
				{
					Source = right,
					Path = new PropertyPath("InnerRightHandle.X"),
					Mode = BindingMode.OneWay,
				});
				BindingOperations.SetBinding(l, Line.Y2Property, new Binding
				{
					Source = right,
					Path = new PropertyPath("InnerRightHandle.Y"),
					Mode = BindingMode.OneWay,
				});
				return l;
			}

			LineWithText L = new LineWithText(text);
			Panel.SetZIndex(L, -1);
			//Stroke = Brushes.Gray;
			//StrokeThickness = 2.5;
			L.StrokeThickness = 1;
			L.StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 });

			BindingOperations.SetBinding(L, LineWithText.X1Property, new Binding
			{
				Source = left,
				Path = new PropertyPath("InnerLeftHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText.Y1Property, new Binding
			{
				Source = left,
				Path = new PropertyPath("InnerLeftHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText.X2Property, new Binding
			{
				Source = right,
				Path = new PropertyPath("InnerRightHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(L, LineWithText.Y2Property, new Binding
			{
				Source = right,
				Path = new PropertyPath("InnerRightHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			return L;
		}

		public static Shape ConnectInnerCenters(AttachmentHandles from, AttachmentHandles to)
		{
			var l = new ArrowLine
			{
				Stroke = Brushes.Crimson,
				//StrokeThickness = 2.5,
				StrokeThickness = 1,
				ArrowEnds = ArrowEnds.End,
				//IsArrowClosed = true,
				Fill = Brushes.Crimson,
				//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 });
			};
			Panel.SetZIndex(l, -1);

			BindingOperations.SetBinding(l, ArrowLine.X1Property, new Binding
			{
				Source = from,
				Path = new PropertyPath("InnerCenterHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.Y1Property, new Binding
			{
				Source = from,
				Path = new PropertyPath("InnerCenterHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			String shx, shy;
			if (System.Math.Abs(from.InnerCenterHandle.X - to.InnerCenterHandle.X) > System.Math.Abs(from.InnerCenterHandle.Y - to.InnerCenterHandle.Y))
			{
				if (from.InnerCenterHandle.X > to.InnerCenterHandle.X)
					shx = "RightHandle.X";
				else
					shx = "LeftHandle.X";
				shy = "InnerCenterHandle.Y";
			}
			else
			{
				shx = "InnerCenterHandle.X";
				if (from.InnerCenterHandle.Y < to.InnerCenterHandle.Y)
					shy = "TopHandle.Y";
				else
					shy = "BottomHandle.Y";
			}
			BindingOperations.SetBinding(l, ArrowLine.X2Property, new Binding
			{
				Source = to,
				Path = new PropertyPath(shx),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.Y2Property, new Binding
			{
				Source = to,
				Path = new PropertyPath(shy),
				Mode = BindingMode.OneWay,
			});
			return l;
		}

		public static LineWithText ConnectVerticalWithText(AttachmentHandles upper, AttachmentHandles lower, String text)
		{
			LineWithText l = new LineWithText(text)
			{
				//Stroke = Brushes.Gray,
				//StrokeThickness = 2.5,
				StrokeThickness = 1,
				//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 }),
			};
			Panel.SetZIndex(l, -1);

			Rect r_upper = VisualTreeHelper.GetDescendantBounds(upper);
			Rect r_lower = VisualTreeHelper.GetDescendantBounds(lower);

			if (r_upper.Bottom > r_lower.Top)
			{
				var tmp = upper;
				upper = lower;
				lower = tmp;
			}

			BindingOperations.SetBinding(l, LineWithText.X1Property, new Binding
			{
				Source = lower,
				Path = new PropertyPath("TopHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, LineWithText.Y1Property, new Binding
			{
				Source = lower,
				Path = new PropertyPath("TopHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, LineWithText.X2Property, new Binding
			{
				Source = upper,
				Path = new PropertyPath("BottomHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, LineWithText.Y2Property, new Binding
			{
				Source = upper,
				Path = new PropertyPath("BottomHandle.Y"),
				Mode = BindingMode.OneWay,
			});

			return l;
		}

		public static Line ConnectVertical(AttachmentHandles upper, AttachmentHandles lower)
		{
			var l = new Line
			{
				Stroke = Brushes.Gray,
				//StrokeThickness = 2.5,
				StrokeThickness = 1,
				//StrokeDashArray = new DoubleCollection(new Double[] { 2, 2 }),
			};
			Panel.SetZIndex(l, -1);

			Rect r_upper = VisualTreeHelper.GetDescendantBounds(upper);
			Rect r_lower = VisualTreeHelper.GetDescendantBounds(lower);

			if (r_upper.Bottom > r_lower.Top)
			{
				var tmp = upper;
				upper = lower;
				lower = tmp;
			}

			BindingOperations.SetBinding(l, Line.X1Property, new Binding
			{
				Source = lower,
				Path = new PropertyPath("TopHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, Line.Y1Property, new Binding
			{
				Source = lower,
				Path = new PropertyPath("TopHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, Line.X2Property, new Binding
			{
				Source = upper,
				Path = new PropertyPath("BottomHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, Line.Y2Property, new Binding
			{
				Source = upper,
				Path = new PropertyPath("BottomHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			return l;
		}

		public static ArrowLine ConnectVerticalArrow(AttachmentHandles from, AttachmentHandles to)
		{
			var l = new ArrowLine
			{
				Stroke = Brushes.DimGray,
				StrokeThickness = 2.5,
				ArrowEnds = ArrowEnds.Start,
			};
			Panel.SetZIndex(l, -1);

			Rect r_upper = VisualTreeHelper.GetDescendantBounds(from);
			Rect r_lower = VisualTreeHelper.GetDescendantBounds(to);

			if (r_upper.Bottom > r_lower.Top)
			{
				var tmp = from;
				from = to;
				to = tmp;
			}

			BindingOperations.SetBinding(l, ArrowLine.X1Property, new Binding
			{
				Source = to,
				Path = new PropertyPath("TopHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.Y1Property, new Binding
			{
				Source = to,
				Path = new PropertyPath("TopHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.X2Property, new Binding
			{
				Source = from,
				Path = new PropertyPath("BottomHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.Y2Property, new Binding
			{
				Source = from,
				Path = new PropertyPath("BottomHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			return l;
		}

		public static ArrowLine ConnectHorizontalArrow(AttachmentHandles from, AttachmentHandles to)
		{
			var l = new ArrowLine
			{
				Stroke = Brushes.DimGray,
				StrokeThickness = 2.5,
				ArrowEnds = ArrowEnds.Start,
			};
			Panel.SetZIndex(l, -1);

			Rect r_left = VisualTreeHelper.GetDescendantBounds(from);
			Rect r_right = VisualTreeHelper.GetDescendantBounds(to);

			if (r_left.Right > r_right.Left)
			{
				var tmp = from;
				from = to;
				to = tmp;
			}

			BindingOperations.SetBinding(l, ArrowLine.X1Property, new Binding
			{
				Source = to,
				Path = new PropertyPath("LeftHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.Y1Property, new Binding
			{
				Source = to,
				Path = new PropertyPath("LeftHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.X2Property, new Binding
			{
				Source = from,
				Path = new PropertyPath("RightHandle.X"),
				Mode = BindingMode.OneWay,
			});
			BindingOperations.SetBinding(l, ArrowLine.Y2Property, new Binding
			{
				Source = from,
				Path = new PropertyPath("RightHandle.Y"),
				Mode = BindingMode.OneWay,
			});
			return l;
		}
	};
}
