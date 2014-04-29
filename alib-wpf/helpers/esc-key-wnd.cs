using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace alib.Wpf
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class EscapeKeyWindow : Window
	{
		public static EscapeKeyWindow Create(Window owner, Object content)
		{
			var w = new EscapeKeyWindow(owner)
			{
				SizeToContent = SizeToContent.WidthAndHeight,
				ShowActivated = true,
				Focusable = true,
				Content = content,
			};
			w.Show();
			return w;
		}

		public EscapeKeyWindow(Window owner)
		{
			//if (owner == null)
			//	throw new Exception();
			base.Owner = owner;
		}

		protected override void OnContentChanged(Object _old, Object _new)
		{
			base.OnContentChanged(_old, _new);

			if (Owner != null)
				FocusManager.SetFocusedElement(base.Owner, (FrameworkElement)_new);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			EscapeWindowCloseHandler(this, e);
		}

		/// <summary>
		/// the first param must be a Window but doesn't have to be 'EscapeKeyWindow'
		/// the signature allows is for direct use in event handler
		/// </summary>
		public static void EscapeWindowCloseHandler(Object window, KeyEventArgs e)
		{
			Window w, owner, ww;
			if (!e.Handled && e.Key == Key.Escape)
			{
				w = (Window)window;
				w.Close();

				if ((owner = w.Owner) == null)
					goto ok2;

				var foc = FocusManager.GetFocusedElement(owner) as FrameworkElement;

				if (foc == null)
				{
					//20131222
					//Debug.Print("N");
				}

				/// basically, if the FocusManager has a usable choice (it almost never does)
				/// then we will honor it.

				if (foc == null || (ww = foc as Window) == null || !ww.IsVisible)
				{
					var wc = owner.OwnedWindows;

					for (int c = wc.Count; --c >= 0; )
					{
						ww = wc[c];

						if (ww == owner)
						{
							Debug.Print("D");
						}
						if (ww == w)
						{
							Debug.Print("E");
						}
						if (ww == w.Content)
						{
							Debug.Print("F");
						}

						if (ww.IsVisible)
						{
							//20131222
							//Debug.Print("S");
							goto ok1;
						}
						else
						{
							Debug.Print("X");
						}
					}
					/// nothing available. use the owner
					//20131222
					//Debug.Print("R");
					ww = owner;
				}
				else
				{
					Debug.Print("Q");
				}
			ok1:
				ww.Activate();
			ok2:
				e.Handled = true;
			}
		}
	};
}




