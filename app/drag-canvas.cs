using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using alib.Enumerable;
using alib.Debugging;
using alib.Wpf;

namespace xigt2
{
	public class DragCanvas : Grid
	{
		class drag_params
		{
			public drag_params(DragCanvas mg)
			{
				this.mg = mg;
			}

			readonly DragCanvas mg;
			tier_ui_base tub;
			BindingBase bb;
			public Rectangle tui;
			Point pp_offs;

			public tier_ui_base get_tier_ui(Point pt)
			{
				tier_ui_base _cur = null;

				if (pt.Y - pp_offs.Y <= 0)
				{
					_cur = get_tub_for_ix(0);
				}
				else
				{
					VisualTreeHelper.HitTest(mg,
						obj => { return HitTestFilterBehavior.Continue; },
						htr =>
						{
							if ((_cur = htr.VisualHit as tier_ui_base) != null)
								return HitTestResultBehavior.Stop;
							return HitTestResultBehavior.Continue;
						},
						new PointHitTestParameters(pt));

					if (_cur == null)
						_cur = get_tub_for_ix(int.MaxValue);
				}
				return _cur;
			}

			tier_ui_base get_tub_for_ix(int ix)
			{
				var icg = tub.TiersControl.ItemContainerGenerator;
				if (ix < 0)
					ix = 0;
				else if (ix >= icg.Items.Count)
					ix = icg.Items.Count - 1;
				var cz = (ContentPresenter)icg.ContainerFromIndex(ix);
				return cz.EnumerateVisualChildren().FirstOfType<tier_ui_base>();
			}

			public bool Capture(Point pt)
			{
				this.tub = get_tier_ui(pt);
				if (tub == null /*|| tub.TiersControl.Items.Count == 1*/)
					return false;

				pt.X = 0.0;
				pp_offs = mg.TransformToDescendant(tub).Transform(pt);
				pp_offs.X += tub.Margin.Left;
				pp_offs.Y += tub.Margin.Top;

				var sz = new Size(tub.ActualWidth, tub.ActualHeight);

				this.tui = new Rectangle
				{
					Fill = new ImageBrush
					{
						ImageSource = __util.GetImage(tub, null, sz),
						Stretch = Stretch.None
					},
					Width = sz.Width,
					Height = sz.Height,
					Margin = tub.Margin,
				};

				this.bb = BindingOperations.GetBinding(tub, VisibilityProperty);
				BindingOperations.ClearBinding(tub, VisibilityProperty);
				tub.Visibility = Visibility.Hidden;

				mg.Children.Add(tui);

				return tui.CaptureMouse();
			}

			public void MouseMove(Point pt)
			{
				pt.X = -pp_offs.X;
				pt.Y -= pp_offs.Y;
				tui.Arrange(new Rect(pt, tui.DesiredSize));
			}

			public void Release(Point pt)
			{
				var _new = get_tier_ui(pt);

				tui.ReleaseMouseCapture();
				mg.Children.Remove(tui);

				if (_new != null)
				{
					tier_base.MoveTier(tub.Tier, _new.Tier);
				}
				
				tub.Visibility = Visibility.Visible;
				BindingOperations.SetBinding(tub, VisibilityProperty, bb);
				bb = null;
			}

			//public static void MoveTier(tier_base told, tier_base tnew)
			//{
			//	if (told != tnew)
			//	{
			//		ITiers h0, h1;
			//		if ((h0 = told.TiersHost) != (h1 = tnew.TiersHost))
			//		{
			//			h0.RemoveAt(told.OuterIndex);
			//			(told.TiersHost = h1).Insert(tnew.OuterIndex, told);

			//			IHostedItem hi;
			//			if (h0.Count == 0 && (hi = h0 as IHostedItem) != null)
			//			{
			//				Debug.Print("drag removing {0} from {1}", hi.GetType().Name, hi.Host.GetType().Name);
			//				hi.Host.GetList().Remove(hi);
			//			}
			//		}
			//		else
			//		{
			//			h0.Tiers.Move(told.OuterIndex, tnew.OuterIndex);
			//		}
			//	}
			//}
		};

		drag_params cur_drag;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);

			if (!e.Handled)
			{
				if (cur_drag != null)
					throw new Exception();

				var _tmp = new drag_params(this);
				if (_tmp.Capture(e.GetPosition(this)))
				{
					cur_drag = _tmp;
					e.Handled = true;
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (cur_drag != null)
			{
				cur_drag.MouseMove(e.GetPosition(this));
				e.Handled = true;
			}
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);

			if (cur_drag != null)
			{
				cur_drag.Release(e.GetPosition(this));
				cur_drag = null;
				e.Handled = true;
			}
		}
	};
}