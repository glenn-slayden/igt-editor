using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace alib.Wpf
{
	using String = System.String;

	public static partial class util
	{
		[DebuggerDisplay("{ToString(),nq}")]
		public struct TimestampedMousePosition : IEquatable<TimestampedMousePosition>
		{
			public Point pt;
			public long ticks;
			public override bool Equals(Object obj)
			{
				return obj is TimestampedMousePosition && ((TimestampedMousePosition)obj).Equals(this);
			}
			public bool Equals(TimestampedMousePosition other)
			{
				return pt == other.pt && ticks == other.ticks;
			}
			public override int GetHashCode()
			{
				return pt.GetHashCode() ^ (int)ticks ^ (int)(ticks >> 32);
			}
			public override String ToString()
			{
				return String.Format("{0,9} [{1}, {2}]", ticks, pt.X, pt.Y);
			}
		};

		public static TimestampedMousePosition GetCorrectTimestampedMousePosition(Visual viz)
		{
			var w32Mouse = new _interop.Win32Point();
			_interop.GetCursorPos(ref w32Mouse);
			return new TimestampedMousePosition
			{
				pt = viz.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y)),
				ticks = DateTime.Now.Ticks
			};
		}

		public static Point GetCorrectMousePosition(Visual viz)
		{
			if (PresentationSource.FromVisual(viz) == null)
				throw new Exception("GetCorrectMousePosition: visual detached");
			var pt32 = new _interop.Win32Point();
			_interop.GetCursorPos(ref pt32);
			return viz.PointFromScreen(new Point(pt32.X, pt32.Y));
		}

		public static T AddWithEvent<T>(this ItemCollection item_coll, T t)
		{
			item_coll.Add(t);
			return t;
		}

		public static T Add<T>(this ItemsControl items_ctrl, T t)
		{
			return AddWithEvent(items_ctrl.Items, t);
		}

		public static void CommandAlwaysEnabled(Object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		public static ContextMenu ContextMenu(this DependencyObject dobj)
		{
			return (ContextMenu)dobj.GetValue(FrameworkElement.ContextMenuProperty);
		}

		public static void Blur(this IInputElement inp)
		{
#if true
			UIElement uie;
			DependencyObject cur;
			if ((cur = inp as DependencyObject) != null)
				while ((cur = cur.GetVisualParent()) != null)
					if ((uie = cur as UIElement) != null && uie.Focusable)
					{
						uie.Focus();
						break;
					}
#else
			var fe = inp as FrameworkElement;
			if (fe == null)
				return;

			var scope = FocusManager.GetFocusScope(fe);
			while ((fe = fe.Parent as FrameworkElement) != null)
			{
				if ((inp = fe as IInputElement) != null && inp.Focusable)
				{
					FocusManager.SetFocusedElement(scope, inp);
					break;
				}
			}
#endif
		}
	};

#if false
	public class CommandInput : TextBox
	{
		List<String> history = new List<String>();
		int i_history = 0;

		public CommandInput()
		{
			PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Up && i_history > 0)
				{
					Text = history[--i_history];
					CursorEnd();
				}
				else if (e.Key == Key.Down && i_history < history.Count - 1)
				{
					Text = history[++i_history];
					CursorEnd();
				}
				else
					return;
				e.Handled = true;
			};
			KeyDown += (s, e) =>
			{
				if (e.Key == Key.Escape)
					Text = String.Empty;

				if (e.Key == Key.Enter)
				{
					String cmd = Text;
					Clear();
					if (!String.IsNullOrWhiteSpace(cmd))
					{
						UpdateHistory(cmd);
						var _tmp = CommandEvent;
						if (_tmp != null)
							_tmp(cmd);
					}
				}
				else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
					CommandEvent("cls");
			};
		}

		void UpdateHistory(String cmd)
		{
			int c = history.Count;
			if (c == 0 || cmd != history[c - 1])
			{
				history.Add(cmd);
				i_history = ++c;
			}
		}

		void CursorEnd() { Select(Text.Length, 0); }

		public delegate void CommandHandler(String s);

		public event CommandHandler CommandEvent;
	};
#endif
}
