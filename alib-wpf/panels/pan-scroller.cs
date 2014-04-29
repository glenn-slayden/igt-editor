using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using alib.Debugging;

namespace alib.Wpf
{
	using Math = System.Math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Scroll viewer which allows mouse-drag panning with inertial flicking
	/// </summary>
	/// <remarks>
	/// Mouse information reporting in WPF is notoriously unreliable and requires specific fragile hacks. Another problem
	/// is that we want the playback timer to be independent of the complexity of client MouseMove activies, which can hog the
	/// dispatcher thread. To help with this, MouseMove events are not forwarded to the content element during animated drift.
	/// </remarks>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class PanScroller : ScrollViewer
	{
		const int msPerFrame = 20;					/// resolution of animation timer
		const Double GestureAmplification = 1.4;	/// initial drift velocity factor relative to sampled velocity
		const Double GestureMin = 0.18;				/// max recent anchor-dragging vector length which inhibits flinging
		const Double HiggsField = 0.95;				/// molasses factor per frame, 1.0 == none
		const Double StopVelocity = 2;				/// done when velocity falls below
		const Double BumperTransfer = 0.5;			/// energy transfer to other axis when hitting bounds

		public PanScroller()
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			IsHitTestVisible = true;

			this.buf = new Point[8];
			this.i_buf = 0;

			timer = new DispatcherTimer();
			timer.Interval = new TimeSpan(0, 0, 0, 0, msPerFrame);
			timer.Tick += new EventHandler(timer_tick);
		}

		public Point ScrollPos
		{
			get { return new Point(HorizontalOffset, VerticalOffset); }
			set
			{
				ScrollToHorizontalOffset(value.X);
				ScrollToVerticalOffset(value.Y);
			}
		}

		protected override void OnScrollChanged(ScrollChangedEventArgs e)
		{
			base.OnScrollChanged(e);

			if (e.ExtentWidthChange + e.ViewportWidthChange + e.ExtentHeightChange + e.ViewportHeightChange != 0)
				update_metrics();
		}

		void update_metrics()
		{
			client_size = new Size(Math.Max(ExtentWidth - ViewportWidth, 0), Math.Max(ExtentHeight - ViewportHeight, 0));

			f_can_pan = ComputedHorizontalScrollBarVisibility == Visibility.Visible ||
				   ComputedVerticalScrollBarVisibility == Visibility.Visible;

			this.Cursor = f_can_pan ? Cursors.Hand : Cursors.Arrow;
		}

		static bool ClosePoint(Point pt1, Point pt2)
		{
			return Math.Abs(pt1.X - pt2.X) <= 3 && Math.Abs(pt2.Y - pt2.Y) <= 3;
		}

		Size client_size;						/// contentful space
		bool f_can_pan;							/// true if either scrollbar is present

		DispatcherTimer timer;					/// animation timer
		Vector velocity;						/// current drift speed and direction

		Point pt_start, drag_anchor;			/// drag start
		Point[] buf;							/// ring buffer for the last 8 mouse points
		int i_buf;
		long t_mousedown, t_mousemove;

		Point get_record_mouse_position()
		{
			var pt = util.GetCorrectMousePosition(this);
			if (i_buf == 0 || !ClosePoint(buf[(i_buf - 1) & 7], pt))
			{
				buf[7 & i_buf++] = pt;
				t_mousemove = DateTime.Now.Ticks;
			}
			return pt;
		}

		void timer_tick(Object sender, EventArgs e)
		{
			Point pt = ScrollPos + velocity;
			if (pt.X <= 0 || pt.X >= client_size.Width)
			{
				velocity.Y += velocity.X * BumperTransfer;
				velocity.X = 0;
			}
			if (pt.Y <= 0 || pt.Y >= client_size.Height)
			{
				velocity.X += velocity.Y * BumperTransfer;
				velocity.Y = 0;
			}

			if (velocity.Length < StopVelocity)
			{
				timer.Stop();
			}
			else
			{
				this.ScrollPos = pt;
				this.velocity *= HiggsField;
			}
		}

		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			if (IsMouseCaptured)
				this.ScrollPos = drag_anchor + (pt_start - get_record_mouse_position());
			else if (!timer.IsEnabled)
				return;			/// ok to propagate the event

			e.Handled = true;	/// do not propagate mousemove to content during drifting or dragging
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			timer.Stop();

			if (!f_can_pan)
				return;

			this.drag_anchor = this.ScrollPos;
			this.Cursor = Cursors.ScrollAll;
			this.i_buf = 0;
			this.pt_start = get_record_mouse_position();

			this.CaptureMouse();

			this.t_mousedown = DateTime.Now.Ticks;

			e.Handled = true;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (this.IsMouseCaptured)
			{
				int c;
				long ticks = DateTime.Now.Ticks - t_mousemove;
				if (ticks == 0)
				{
					//Debug.Print("ticks==0");
				}
				else if ((c = Math.Min(i_buf, 8)) < 2)	/// need at least two time-distinct samples to project drift.
				{
					//Debug.Print("c=={0}",c);
				}
				else
				{
					int i = i_buf - c;	/// start offset in ring buf
					int j = i + c - 1;	/// end offset in ring buf

					var e0 = buf[i & 7];
					var e1 = buf[j & 7];
					Vector vv = e1 - buf[(j - 1) & 7];

					var t = TimeSpan.FromTicks(t_mousemove - t_mousedown).TotalMilliseconds / msPerFrame;
					if (t <= 0)
					{
						//Debug.Print("t: {0} < 0", t);
					}
					else if ((vv / TimeSpan.FromTicks(ticks).TotalMilliseconds).Length < GestureMin)
					{
						//Debug.Print("{0} < GestureMin", (vv / TimeSpan.FromTicks(ticks).TotalMilliseconds).Length);
					}
					else
					{
						//Debug.Print("c:{0} i:{1} j:{2}  ms:{3}  t:{4}  vv.Len:{5}", c, i, j, ms, t, (vv / TimeSpan.FromTicks(ticks).TotalMilliseconds).Length);

						this.velocity = (e0 - e1) / (t / GestureAmplification);

						timer.Start();	/// do this prior to releasing capture: prevents stray mousemove to content
					}
				}

				this.ReleaseMouseCapture();
				this.Cursor = f_can_pan ? Cursors.Hand : Cursors.Arrow;
				e.Handled = true;
			}
		}
	};
}
