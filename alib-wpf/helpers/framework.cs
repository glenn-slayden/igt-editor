using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using alib.Collections;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasLocalValue(this DependencyObject o, DependencyProperty dp)
		{
			return o.ReadLocalValue(dp) != DependencyProperty.UnsetValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DependencyObject GetVisualParent(this DependencyObject o)
		{
			return o is Visual || o is Visual3D ? VisualTreeHelper.GetParent(o) : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DependencyObject GetLogicalParent(this DependencyObject o)
		{
			FrameworkElement fe;
			FrameworkContentElement fce;
			return (fe = o as FrameworkElement) != null ? fe.Parent : (fce = o as FrameworkContentElement) != null ? fce.Parent : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Window FindWindow(this DependencyObject o) { return FindAncestor<Window>(o); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool FindAncestor<T>(this DependencyObject o, out T result)
			where T : DependencyObject
		{
			return (result = FindAncestor<T>(o)) != null;
		}

		public static T FindAncestor<T>(this DependencyObject o)
			where T : DependencyObject
		{
			T t;
			if ((t = o as T) != null || o == null)
				return t;
			var par = GetVisualParent(o) ?? GetLogicalParent(o);
			return par != null ? par as T ?? new _finder<T>().find_ancestor(par) : null;
		}

		sealed class _finder<T> : ListHashSet<DependencyObject>
			where T : DependencyObject
		{
			public T find_ancestor(DependencyObject o)
			{
				Debug.Assert(o != null);
				T t;
				return (t = o as T) == null && base.Add(o) ? get_parent(o) : t;
			}

			T get_parent(DependencyObject o)
			{
				T t = null;
				DependencyObject par;
				return (par = GetVisualParent(o)) != null && (t = find_ancestor(par)) == null &&
						(par = GetLogicalParent(o)) != null ? find_ancestor(par) : t;
			}
		};
#if true
		public static IEnumerable<DependencyObject> AllDescendants(this DependencyObject o)
		{
			var hs = new ListHashSet<DependencyObject>();
			hs.UnionWith(LogicalTreeHelper.GetChildren(o).OfType<DependencyObject>());

			if (o is Visual || o is Visual3D)
			{
				int c = VisualTreeHelper.GetChildrenCount(o);
				for (int i = 0; i < c; i++)
					hs.Add(VisualTreeHelper.GetChild(o, i));
			}

			foreach (var oo in hs)
			{
				yield return oo;
				foreach (var ooo in AllDescendants(oo))
					yield return ooo;
			}
		}
#else
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
#endif
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

		static T[] ImmediateChildren<T>(this DependencyObject o)
			where T : DependencyObject
		{
			int i, j;
			var rg = new T[VisualTreeHelper.GetChildrenCount(o)];
			for (i = j = 0; i < rg.Length; i++)
				if ((rg[j] = VisualTreeHelper.GetChild(o, i) as T) != null)
					j++;
			return j < i ? alib.Array.arr.Resize(rg, j) : rg;
		}
	};
}
