using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace alib.Wpf
{
	using Binding = System.Windows.Data.Binding;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class ScaleSlider : Slider
	{
		public ScaleSlider()
		{
			this.Minimum = 0.5;
			this.Value = 1;
			this.Maximum = 1.8;
			this.SmallChange = 0.05;
			this.LargeChange = 0.4;
			this.Width = 80;
			this.HorizontalAlignment = HorizontalAlignment.Left;
			this.VerticalAlignment = VerticalAlignment.Top;
			this.Margin = new Thickness(10, 10, 0, 0);
			Grid.SetZIndex(this, 10);

			bnd = new System.Windows.Data.Binding
			{
				Source = this,
				Path = new PropertyPath(Slider.ValueProperty)
			};

			this.st = new ScaleTransform();
			st.SetValue(TagProperty, this);

			BindingOperations.SetBinding(st, ScaleTransform.ScaleXProperty, bnd);
			BindingOperations.SetBinding(st, ScaleTransform.ScaleYProperty, bnd);
		}

		public readonly ScaleTransform st;
		public readonly Binding bnd;

		ScrollViewer sv;
		FrameworkElement fe;
		Window wnd;
		public void AttachTransform(FrameworkElement fe, ScrollViewer sv)
		{
			this.sv = sv;
			if ((this.fe = fe) != null)
				(fe.LayoutTransform = st).Changed += st_Changed;
		}
		static void st_Changed(Object o, EventArgs e)
		{
			((ScaleSlider)((ScaleTransform)o).GetValue(TagProperty)).auto_resize();
		}
		void auto_resize()
		{
			if (fe == null)
				return;
			if ((wnd ?? (wnd = this.FindWindow())) == null || wnd.SizeToContent == SizeToContent.Manual)
				st.Changed -= st_Changed;
			else
			{
				if (sv != null)
					sv.HorizontalScrollBarVisibility = sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

				wnd.ClearValue(Window.SizeToContentProperty);
				fe.ClearValue(WidthProperty);
				fe.ClearValue(HeightProperty);
				wnd.SizeToContent = SizeToContent.WidthAndHeight;
			}
		}
		protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
		{
			base.OnThumbDragCompleted(e);
			if (sv != null)
				sv.HorizontalScrollBarVisibility = sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class RoundedRectangle : Border
	{
		static RoundedRectangle()
		{
			Selector.IsSelectedProperty.AddOwner(typeof(RoundedRectangle));
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			var p = e.Property;
			if (p == Selector.IsSelectedProperty)
			{
#if ADORNER
				//Debug.WriteLine("{0} {1}", this.GetType(), e.NewValue);
				var al = AdornerLayer.GetAdornerLayer(this);
				var rg = al.GetAdorners(this);
				if (rg != null)
					foreach (var a in rg.OfType<SimpleCircleAdorner>())
						al.Remove(a);
#endif
				if ((bool)e.NewValue)
				{
#if ADORNER
					al.Add(new SimpleCircleAdorner(this));
#else
					BorderThickness = new Thickness(3);
#endif
				}
#if !ADORNER
				else
				{
					BorderThickness = new Thickness(1);
				}
#endif
			}
		}

		protected static TextBlock _text_child(String txt)
		{
			var tb = new TextBlock
			{
				Foreground = Brushes.Black,
				Padding = new Thickness(1.5),
				TextWrapping = TextWrapping.NoWrap,
				VerticalAlignment = VerticalAlignment.Center,
			};
			if (txt != null)
				tb.Text = txt;
			return tb;
		}

		public RoundedRectangle()
		{
			Margin = new Thickness(2.5);
			CornerRadius = new CornerRadius(2);
			BorderThickness = new Thickness(1);
			BorderBrush = Brushes.Gray;
			VerticalAlignment = VerticalAlignment.Top;
		}
		public RoundedRectangle(Brush br)
			: this()
		{
			if (br != null)
				Background = br;
		}
		public RoundedRectangle(Brush br, UIElement uel)
			: this(br)
		{
			if (uel != null)
				Child = uel;
		}
		public RoundedRectangle(Brush br, String txt)
			: this(br, _text_child(txt))
		{
		}
		public RoundedRectangle(String txt)
			: this(default(Brush), txt)
		{
		}

		public bool IsSelected
		{
			get { return (bool)GetValue(Selector.IsSelectedProperty); }
			set { SetValue(Selector.IsSelectedProperty, value); }
		}

		public String Text
		{
			get { return ((TextBlock)Child).Text; }
			set { ((TextBlock)Child).Text = value; }
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class DenotationOf : Decorator
	{
		const double brk_wide = 20;
		const double brk_high = 4;
		const double brk_tail = 9.5;
		const double brk_gap = 4;

		static DenotationOf()
		{
			DoubleBracketProperty = DependencyProperty.Register("DoubleBracket", typeof(bool), typeof(DenotationOf), new UIPropertyMetadata(true));

			Shape.StrokeProperty.AddOwner(typeof(DenotationOf),
					new FrameworkPropertyMetadata(
						Brushes.Black,
						0,
						(e, o) =>
						{
							((DenotationOf)e).InvalidateVisual();
						},
						null,
						false));

			Shape.StrokeThicknessProperty.AddOwner(typeof(DenotationOf),
					new FrameworkPropertyMetadata(
						2.0,
						0,
						(e, o) =>
						{
							((DenotationOf)e).InvalidateVisual();
						},
						null,
						false));
		}

		public static DependencyProperty DoubleBracketProperty;

		public bool DoubleBracket
		{
			get { return (bool)GetValue(DoubleBracketProperty); }
			set { SetValue(DoubleBracketProperty, value); }
		}

		public Brush Stroke
		{
			get { return (Brush)GetValue(Shape.StrokeProperty); }
			set { SetValue(Shape.StrokeProperty, value); }
		}

		public Double StrokeThickness
		{
			get { return (Double)GetValue(Shape.StrokeThicknessProperty); }
			set { SetValue(Shape.StrokeThicknessProperty, value); }
		}

		protected override Size MeasureOverride(Size constraint)
		{
			base.Child.Measure(constraint);
			var sz = base.Child.DesiredSize;
			sz.Width += brk_wide;
			sz.Height += brk_high;
			return sz;
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			Rect r = new Rect(base.Child.DesiredSize);
			r.Offset(brk_wide / 2, brk_high / 2);
			base.Child.Arrange(r);
			return new Size(r.Width + brk_wide, r.Height + brk_high);
		}

		protected override void OnRender(DrawingContext dc)
		{
			Size sz = base.Child.DesiredSize;

			var r = new Rect(0, 0, sz.Width + brk_wide, sz.Height + brk_high);

			Pen p = new Pen(this.Stroke, this.StrokeThickness);

			r.Inflate(-p.Thickness / 2, -p.Thickness / 2);

			dc.DrawLine(p, new Point(r.Left + brk_tail, r.Top), new Point(r.Left, r.Top));
			dc.DrawLine(p, new Point(r.Left, r.Top), new Point(r.Left, r.Bottom));
			if (DoubleBracket)
				dc.DrawLine(p, new Point(r.Left + brk_gap, r.Top), new Point(r.Left + brk_gap, r.Bottom));
			dc.DrawLine(p, new Point(r.Left, r.Bottom), new Point(r.Left + brk_tail, r.Bottom));

			dc.DrawLine(p, new Point(r.Right - brk_tail, r.Top), new Point(r.Right, r.Top));
			dc.DrawLine(p, new Point(r.Right, r.Top), new Point(r.Right, r.Bottom));
			if (DoubleBracket)
				dc.DrawLine(p, new Point(r.Right - brk_gap, r.Top), new Point(r.Right - brk_gap, r.Bottom));
			dc.DrawLine(p, new Point(r.Right, r.Bottom), new Point(r.Right - brk_tail, r.Bottom));
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class DialogBox : Window
	{
		public DialogBox(Window owner, String title)
		{
			this.Title = title;
			this.WindowStyle = WindowStyle.ToolWindow;
			this.ShowInTaskbar = false;
			this.Owner = owner;
			this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			this.SizeToContent = SizeToContent.WidthAndHeight;
			this.ResizeMode = ResizeMode.NoResize;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class DummyWindow : Window
	{
		public static readonly DummyWindow Instance;
		static DummyWindow() { Instance = new DummyWindow(); }
		DummyWindow() { }
	};
}