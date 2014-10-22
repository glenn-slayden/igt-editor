using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace alib.Wpf
{
	public class DragItemPanel : Canvas
	{
		public static readonly DependencyProperty CanBeDraggedProperty;
		public static readonly DependencyProperty EnableDraggingProperty;
		public static readonly DependencyProperty LimitToBoundsProperty;

		static DragItemPanel()
		{
			CanBeDraggedProperty = DependencyProperty.RegisterAttached(
				"CanBeDragged", typeof(bool), typeof(DragItemPanel), new UIPropertyMetadata(true));

			EnableDraggingProperty = DependencyProperty.Register(
				"EnableDragging", typeof(bool), typeof(DragItemPanel), new UIPropertyMetadata(true));

			LimitToBoundsProperty = DependencyProperty.Register(
				"LimitToBounds", typeof(bool), typeof(DragItemPanel), new UIPropertyMetadata(true));
		}

		public static void SetCanBeDragged(UIElement child, bool value)
		{
			if (child != null)
				child.SetValue(CanBeDraggedProperty, value);
		}
		public static bool GetCanBeDragged(UIElement child)
		{
			return child != null && (bool)child.GetValue(CanBeDraggedProperty);
		}

		public bool EnableDragging
		{
			get { return (bool)base.GetValue(EnableDraggingProperty); }
			set { base.SetValue(EnableDraggingProperty, value); }
		}

		public bool LimitToBounds
		{
			get { return (bool)GetValue(LimitToBoundsProperty); }
			set { SetValue(LimitToBoundsProperty, value); }
		}

		UIElement cur_drag;
		Point pt_drag_start, pt_offs_old;
		bool f_dragging;

		public UIElement CurrentDragElement { get { return cur_drag; } }

		void SetCurrentDragElement(UIElement child)
		{
			if (cur_drag != null)
			{
				cur_drag.ReleaseMouseCapture();
				cur_drag = null;
			}

			if (EnableDragging && DragItemPanel.GetCanBeDragged(child))
			{
				cur_drag = child;
				cur_drag.CaptureMouse();
			}
		}

		public UIElement FindCanvasChild(DependencyObject o)
		{
			UIElement elem = null;
			while (o != null && ((elem = o as UIElement) == null || !Children.Contains(elem)))
				o = o.GetVisualParent() ?? o.GetLogicalParent();
			return elem;
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			f_dragging = false;
			cur_drag = null;

			if (e.Source is UIElement && ((UIElement)e.Source).Focusable)
				return;

			pt_drag_start = e.GetPosition(this);

			SetCurrentDragElement(FindCanvasChild(e.Source as DependencyObject));
			if (cur_drag != null)
			{
				pt_offs_old = new Point(Canvas.GetLeft(cur_drag), Canvas.GetTop(cur_drag));
				e.Handled = true;
				f_dragging = true;
			}
		}

		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseUp(e);
			SetCurrentDragElement(null);
		}

		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);

			if (cur_drag == null || !f_dragging)
				return;

			Point new_offs = pt_offs_old + (e.GetPosition(this) - pt_drag_start);

			if (LimitToBounds)
			{
				Rect elemRect = new Rect(new_offs, cur_drag.RenderSize);

				bool leftAlign = elemRect.Left < 0;
				bool rightAlign = elemRect.Right > ActualWidth;

				if (leftAlign)
					new_offs.X = 0;
				else if (rightAlign)
					new_offs.X = ActualWidth - elemRect.Width;

				bool topAlign = elemRect.Top < 0;
				bool bottomAlign = elemRect.Bottom > ActualHeight;

				if (topAlign)
					new_offs.Y = 0;
				else if (bottomAlign)
					new_offs.Y = ActualHeight - elemRect.Height;
			}

			Canvas.SetLeft(cur_drag, new_offs.X);
			Canvas.SetTop(cur_drag, new_offs.Y);
#if false
			System.Diagnostics.Debug.WriteLine("{0},{1}", new_offs.X, new_offs.Y);
#endif
		}

		protected override Size MeasureOverride(Size constraint)
		{
			Size sz = new Size();
			foreach (UIElement uie in Children)
			{
				if (!uie.IsMeasureValid)
					uie.Measure(constraint);
				Size ds = uie.DesiredSize;
				if (ds.Width != 0)
				{
					double x = VisualTreeHelper.GetOffset(uie).X + ds.Width;
					if (x > sz.Width)
						sz.Width = x;
				}
				if (ds.Height != 0)
				{
					double y = VisualTreeHelper.GetOffset(uie).Y + ds.Height;
					if (y > sz.Height)
						sz.Height = y;
				}
			}
			return sz;
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			Size sz = base.ArrangeOverride(arrangeSize);
			foreach (UIElement uie in Children)
			{
				Size ds = uie.DesiredSize;
				if (ds.Width != 0)
				{
					Canvas.SetLeft(uie, VisualTreeHelper.GetOffset(uie).X);
					Canvas.SetRight(uie, double.NaN);
				}
				if (ds.Height != 0)
				{
					Canvas.SetTop(uie, VisualTreeHelper.GetOffset(uie).Y);
					Canvas.SetBottom(uie, double.NaN);
				}
			}
			return sz;
		}

		public void BringToFront(UIElement child) { UpdateZOrder(child, true); }
		public void SendToBack(UIElement child) { UpdateZOrder(child, false); }

		void UpdateZOrder(UIElement child, bool bringToFront)
		{
			if (child == null)
				throw new ArgumentNullException("child");
			if (!base.Children.Contains(child))
				throw new ArgumentException("Must be a child element of the Canvas.", "child");

			// Determine the Z-Index for the target UIElement.
			int elementNewZIndex = -1;
			if (bringToFront)
			{
				foreach (UIElement elem in base.Children)
					if (elem.Visibility != Visibility.Collapsed)
						elementNewZIndex++;
			}
			else
				elementNewZIndex = 0;

			// Determine if the other UIElements' Z-Index should be raised or lowered by one. 
			int offset = (elementNewZIndex == 0) ? 1 : -1;

			int elementCurrentZIndex = Canvas.GetZIndex(child);

			foreach (UIElement childElement in base.Children)
			{
				if (childElement == child)
					Canvas.SetZIndex(child, elementNewZIndex);
				else
				{
					int zIndex = Canvas.GetZIndex(childElement);

					// Only modify the z-index of an element if it is in between the target element's old and new z-index.
					if (bringToFront && elementCurrentZIndex < zIndex || !bringToFront && zIndex < elementCurrentZIndex)
					{
						Canvas.SetZIndex(childElement, zIndex + offset);
					}
				}
			}
		}
	};
}
