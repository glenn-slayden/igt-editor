using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using alib;
using alib.Debugging;
using alib.Enumerable;
using alib.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace xigt2
{
	using Path = System.IO.Path;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class __main : Window
	{
		public __main()
		{
			InitializeComponent();

			this.WindowState = App.settings.WindowMaximized ? WindowState.Maximized : WindowState.Normal;

			w_items.mw = w_corpora.mw = this;

			Loaded += OnLoaded;
		}

		void OnLoaded(Object o, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;

			String[] _tmp;
			if (App.settings.ReloadLastSession && (_tmp = App.settings.SessionFiles) != null)
			{
				foreach (var f in _tmp)
				{
					cmd_OpenXamlIgtFile(f);
				}
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (App.settings.SaveOnExit && App.settings.LastDirectory != null && System.IO.Directory.Exists(App.settings.LastDirectory))
			{
				cmd_SaveAll(App.settings.LastDirectory);
			}

			App.settings.WindowMaximized = this.WindowState == WindowState.Maximized;

			App.settings.SessionFiles =
							w_corpora
							.Select(c => Path.GetFullPath(c.Filename))
							.ToArray();

			var _tmp = w_corpora.SelectedItem;
			if (_tmp != null)
			{
				App.settings.SelectedCorpusFilename = _tmp.Filename;
				App.settings.SelectedIgtIndex = w_items.SelectedIndex;
			}
			else
			{
				App.settings.SelectedCorpusFilename = null;
				App.settings.SelectedIgtIndex = -1;
			}

			base.OnClosing(e);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == TextBlock.FontFamilyProperty)
			{
				var d = ((FontFamily)e.NewValue).FamilyNames.Values;
				foreach (var mi in mi_font_families.Items.OfType<MenuItem>())
				{
					mi.IsChecked = d.Contains((String)mi.Header, StringComparer.InvariantCultureIgnoreCase);
				}
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (!e.Handled && e.Key == Key.Escape && App.settings.EscapeKeyExit)
			{
				Close();
				e.Handled = true;
			}
		}

		private void Font_Click(Object sender, RoutedEventArgs e)
		{
			this.FontFamily = new FontFamily((String)((MenuItem)sender).Header);
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class CorporaListView : ListView, IReadOnlyList<IgtCorpus>
	{
		public __main mw;

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			if (Count <= 0)
				return;

			int ix;
			var fn = App.settings.SelectedCorpusFilename;

			if (fn != null && (ix = this.IndexOfFirst(c => String.Equals(c.Filename, fn, StringComparison.OrdinalIgnoreCase))) != -1)
			{
				SelectedIndex = ix;
				App.settings.SelectedCorpusFilename = null;
				Focus();

				if (mw.w_items.SelectedIndex != (ix = App.settings.SelectedIgtIndex) && (ix < mw.w_items.Count))
					mw.w_items.SelectedIndex = ix;

#if false
				var igt = mw.w_items.SelectedItem;
				var nt = new TierGroupTier
				{
					TiersHost = igt,
					Tiers = 
					{
						new TextTier { Text = "mary had a little lamb" },
						new TextTier { Text = "whose fleece was white as snow" },
						new TextTier { Text = "And everywhere that Mary went" },
						new TextTier { Text = "the lamb was sure to go." },
					}
				};
				igt.Add(nt);
#endif
			}
			else if (SelectedIndex < 0)
				SelectedIndex = 0;
		}

		public new IgtCorpus SelectedItem
		{
			get { return (IgtCorpus)base.SelectedItem; }
			set
			{
				int ix = base.Items.IndexOf(value);
				if (ix != -1)
					base.SelectedIndex = ix;
			}
		}

		public int Count { get { return base.Items.Count; } }

		public IgtCorpus this[int index] { get { return (IgtCorpus)base.Items[index]; } }

		IEnumerator<IgtCorpus> IEnumerable<IgtCorpus>.GetEnumerator()
		{
			return ((IEnumerable)base.Items).Cast<IgtCorpus>().GetEnumerator();
		}
		public IEnumerator GetEnumerator() { return ((IEnumerable)base.Items).GetEnumerator(); }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!e.Handled && e.Key == Key.Right)
			{
				mw.w_items.Focus();
				e.Handled = true;
			}
		}
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			int ix;
			if (!e.Handled && (ix = SelectedIndex) != -1)
			{
				UIElement el;

				if ((el = (UIElement)ItemContainerGenerator.ContainerFromIndex(ix)) == null)
				{
					UpdateLayout();
					if ((el = (UIElement)ItemContainerGenerator.ContainerFromIndex(ix)) == null)
						return;
				}
				el.Focus();
				e.Handled = true;
			}
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class CorpusIgtsListView : ListView, IReadOnlyList<Igt>
	{
		public __main mw;

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			if (Count > 0 && SelectedIndex < 0)
				SelectedIndex = 0;
		}

		public new Igt SelectedItem { get { return (Igt)base.SelectedItem; } }

		public int Count { get { return base.Items.Count; } }

		public Igt this[int index] { get { return (Igt)base.Items[index]; } }

		IEnumerator<Igt> IEnumerable<Igt>.GetEnumerator()
		{
			return ((IEnumerable)base.Items).Cast<Igt>().GetEnumerator();
		}
		public IEnumerator GetEnumerator() { return ((IEnumerable)base.Items).GetEnumerator(); }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!e.Handled && e.Key == Key.Left)
			{
				mw.w_corpora.Focus();
				e.Handled = true;
			}
		}
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			int ix;
			if (!e.Handled && (ix = SelectedIndex) != -1)
			{
				UIElement el;

				if ((el = (UIElement)ItemContainerGenerator.ContainerFromIndex(ix)) == null)
				{
					UpdateLayout();
					if ((el = (UIElement)ItemContainerGenerator.ContainerFromIndex(ix)) == null)
						return;
				}
				el.Focus();
				e.Handled = true;
			}
		}
	};
}
