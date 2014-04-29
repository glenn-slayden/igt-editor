using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace alib.Wpf
{
	using String = System.String;

	public static partial class util
	{
		public static String FindText(this UIElement el)
		{
			UIElement prv;
			do
			{
				prv = el;

				var b = el as Border;
				if (b != null)
					el = b.Child;

				var c = el as ContentControl;
				if (c != null && c.Content is UIElement)
					el = (UIElement)c.Content;

				if (el == null)
					return null;

				String s;
				if ((s = (String)el.GetValue(TextBlock.TextProperty)) != null)
					return s;
				if ((s = (String)el.GetValue(TextBox.TextProperty)) != null)
					return s;
			}
			while (prv != el);
			return null;
		}

		public static UIElement StripFrames(this UIElement _el)
		{
			var el = _el;
			while (el != null)
			{
				if (el is ContentControl)
					el = ((ContentControl)el).Content as FrameworkElement;
				else if (el is Border)
					el = ((Border)el).Child as FrameworkElement;
				else
					break;
			}
			return el ?? _el;
		}

		public static Window FindWindow(this DependencyObject o) { return FindAncestor<Window>(o); }

		public static T FindAncestor<T>(this DependencyObject o)
			where T : DependencyObject
		{
			if (o == null)
				return null;
			return o as T ?? find_ancestor<T>(o, new HashSet<DependencyObject>());
		}

		static T find_ancestor<T>(DependencyObject o, HashSet<DependencyObject> hs)
			where T : DependencyObject
		{
			var t = o as T;
			if (t == null && hs.Add(o))
			{
				FrameworkElement fe;
				DependencyObject par = null;

				if ((fe = o as FrameworkElement) != null && (par = VisualTreeHelper.GetParent(fe)) != null && (t = find_ancestor<T>(par, hs)) != null)
					goto ok;

				if ((par = LogicalTreeHelper.GetParent(o)) != null && (t = find_ancestor<T>(par, hs)) != null)
					goto ok;

				if (o is Visual && (par = VisualTreeHelper.GetParent(o)) != null && (t = find_ancestor<T>(par, hs)) != null)
					goto ok;
			}
		ok:
			return t;
		}

		public static FrameworkElement FindFrameworkElement(this DependencyObject o)
		{
			var fe = default(FrameworkElement);
			if (o != null)
				if ((fe = o as FrameworkElement) == null)
					if (!(o is Visual || o is System.Windows.Media.Media3D.Visual3D) || (fe = VisualTreeHelper.GetParent(o) as FrameworkElement) == null)
						fe = LogicalTreeHelper.GetParent(o) as FrameworkElement;
			return fe;
		}

		public static IEnumerable<FrameworkElement> AllDescendants(this FrameworkElement fe)
		{
			ContentControl cc;
			Panel pn;
			ItemsControl ic;
			Decorator dc;
			FrameworkElement depx;

			if ((cc = fe as ContentControl) != null)
			{
				if ((depx = cc.Content as FrameworkElement) != null)
					foreach (var depo in AllDescendantsInclusive(depx))
						yield return depo;
			}
			else if ((dc = fe as Decorator) != null)
			{
				if ((depx = dc.Child as FrameworkElement) != null)
					foreach (var depo in AllDescendantsInclusive(depx))
						yield return depo;
			}
			else if ((pn = fe as Panel) != null)
			{
				foreach (var pc in pn.Children.OfType<FrameworkElement>())
					foreach (var depo in AllDescendantsInclusive(pc))
						yield return depo;
			}
			else if ((ic = fe as ItemsControl) != null)
			{
				foreach (var pc in ic.Items.OfType<FrameworkElement>())
					foreach (var depo in AllDescendantsInclusive(pc))
						yield return depo;
			}
		}

		public static IEnumerable<FrameworkElement> AllDescendantsInclusive(this FrameworkElement fe)
		{
			foreach (var child in AllDescendants(fe))
				yield return child;
			yield return fe;
		}

		public static IEnumerable<DependencyObject> EnumerateVisualChildren(this DependencyObject o)
		{
			int c = VisualTreeHelper.GetChildrenCount(o);
			for (int i = 0; i < c; i++)
			{
				var dd = VisualTreeHelper.GetChild(o, i);
				if (dd != null)
				{
					foreach (var cch in dd.EnumerateVisualChildren())
						yield return cch;
				}
			}
			yield return o;
		}

		static IEnumerable<DependencyObject> ImmediateChildren(this DependencyObject o)
		{
			int c = VisualTreeHelper.GetChildrenCount(o);
			for (int i = 0; i < c; i++)
			{
				var dd = VisualTreeHelper.GetChild(o, i);
				if (dd != null)
					yield return dd;
			}
		}
	};
}
