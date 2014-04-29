using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace alib.Wpf
{
	public static partial class util
	{
		public static UIElement FirstOrDefault(this UIElementCollection uiecoll, Func<UIElement, bool> selector)
		{
			lock (uiecoll.SyncRoot)
			{
				UIElement el;
				int c = uiecoll.Count;
				for (int i = 0; i < c; i++)
					if (selector(el = uiecoll[i]))
						return el;
				return null;
			}
		}

		public static int IndexOfFirst(this UIElementCollection uiecoll, Func<UIElement, bool> selector)
		{
			lock (uiecoll.SyncRoot)
			{
				int c = uiecoll.Count;
				for (int i = 0; i < c; i++)
					if (selector(uiecoll[i]))
						return i;
				return -1;
			}
		}

		public static void AddOrReplace(this UIElementCollection uiecoll, Func<UIElement, bool> selector, UIElement _new)
		{
			if (_new == null)
				throw new ArgumentNullException();

			lock (uiecoll.SyncRoot)
			{
				int ix = uiecoll.IndexOfFirst(selector);
				if (ix != -1)
					uiecoll[ix] = _new;
				else
					uiecoll.Add(_new);
			}
		}

		public static void AddReplaceRemove(this UIElementCollection uiecoll, Func<UIElement, bool> selector, UIElement _new)
		{
			lock (uiecoll.SyncRoot ?? uiecoll)
			{
				var _old = uiecoll.FirstOrDefault(selector);
				if (_old != null)
				{
					if (_old == _new)
						return;
					uiecoll.Remove(_old);
				}
				if (_new != null)
					uiecoll.Add(_new);
			}
		}

		public static void AddRange<T>(this UIElementCollection uiecoll, IEnumerable<T> ieuie)
			where T : UIElement
		{
			lock (uiecoll.SyncRoot)
			{
				foreach (var uie in ieuie)
					uiecoll.Add(uie);
			}
		}

		public static void DetachItems(this ItemsControl ictrl)
		{
			IList src;
			if ((src = ictrl.ItemsSource as IList) == null)
				return;

			ictrl.ItemsSource = null;

			var rg = new Object[src.Count];
			int i;
			for (i = rg.Length - 1; i >= 0; --i)
			{
				rg[i] = src[i];
				src.RemoveAt(i);
			}

			var tgt = ictrl.Items;

			for (i = 0; i < rg.Length; i++)
				tgt.Add(rg[i]);
		}

		public static ItemsControl ItemsControlHost(this ItemContainerGenerator icg)
		{
			return typeof(ItemContainerGenerator).GetMethod("get_Host", (BindingFlags)0x24).Invoke(icg, null) as ItemsControl;
		}

		/// <summary>
		/// Walks the tree items to find the node corresponding with the given item, then sets it to be selected.
		/// </summary>
		/// <param name="treeView">The tree view to set the selected item on</param>
		/// <param name="item">The item to be selected</param>
		/// <returns><c>true</c> if the item was found and set to be selected</returns>
		public static bool SetSelectedItem(this TreeView treeView, object item)
		{
			return SetSelected(treeView, item);
		}

		static bool SetSelected(ItemsControl parent, object child)
		{
			if (parent == null || child == null)
				return false;

			TreeViewItem childNode = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
			if (childNode != null)
			{
				childNode.Focus();
				return childNode.IsSelected = true;
			}

			if (parent.Items.Count > 0)
			{
				foreach (object childItem in parent.Items)
				{
					ItemsControl childControl = parent.ItemContainerGenerator.ContainerFromItem(childItem) as ItemsControl;
					if (SetSelected(childControl, child))
						return true;
				}
			}
			return false;
		}

	}
}