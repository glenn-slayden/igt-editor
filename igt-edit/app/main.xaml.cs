using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
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

namespace xie
{
	using Path = System.IO.Path;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class main : Window
	{
		public main()
		{
			this.WindowState = App.settings.WindowMaximized ? WindowState.Maximized : WindowState.Normal;

			InitializeComponent();

			Loaded += OnLoaded;
		}


		void OnLoaded(Object o, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;

			String[] _tmp;
			if (App.settings.ReloadLastSession && (_tmp = App.settings.SessionFiles) != null)
				foo(_tmp);

			var ic = new xxx().bar(@"D:\github\igt-editor\odin-2.0-splits\by-lang\tha.xml");
			//new xxx().bar(@"D:\github\igt-editor\odin-2.0-splits\full\odin.xml");

			ccc.Add(ic);
		}


		void foo(String[] _tmp)
		{
			try
			{
				Mouse.SetCursor(Cursors.Wait);

				open_files(_tmp);
			}
			catch (ErrorActionException mbe)
			{
				mbe.show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Mouse.SetCursor(Cursors.Arrow);
			}
		}

		bool f_nosave;
		protected override void OnClosing(CancelEventArgs e)
		{
			if (!f_nosave && App.settings.SaveOnExit)
			{
				save_all_files();
			}

			App.settings.WindowMaximized = this.WindowState == WindowState.Maximized;

			App.settings.SessionFiles =
							w_corpora
							.Select(c => c.Filename)
							.Where(fn => fn != null)
							.Select(fn => Path.GetFullPath(fn))
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

		private void fm_Click(Object sender, RoutedEventArgs e)
		{
			var mi = sender as MenuItem;
			var item = mi.DataContext as IgtCorpus;
			if (item == null)
			{
				w_opened.SetCurrentValue(TextBlock.TextProperty, "No item selected.");
				return;
			}
			String filename = item.Filename;
			int ix;

			switch (mi.Name)
			{
				case "fm_save":
					item.Save();
					w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("saved changes to file '{0}'.", filename));
					break;
				case "fm_save_close":
					item.Save();
					if ((ix = ccc.IndexOf(item)) != -1)
					{
						ccc.RemoveAt(ix);
						while (ix >= ccc.Count)
							ix--;
						w_corpora.SelectedIndex = ix;
					}
					w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("saved changes and closed file '{0}'.", filename));
					break;
				case "fm_revert":
					{
						var _new = open_file_inner(filename);
						if ((ix = ccc.IndexOf(item)) != -1)
							ccc[ix] = _new;
						w_corpora.SelectedIndex = ix;
						w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("reverted file '{0}'.", filename));
					}
					break;
				case "fm_close":
					if ((ix = ccc.IndexOf(item)) != -1)
					{
						ccc.RemoveAt(ix);
						while (ix >= ccc.Count)
							ix--;
						w_corpora.SelectedIndex = ix;
					}
					break;
				default:
					throw new Exception();
			}
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class CorporaListView : ListView, IReadOnlyList<IgtCorpus>
	{
		CorpusIgtsListView w_items { get { return ((main)App.Current.MainWindow).w_items; } }

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

				if (w_items.SelectedIndex != (ix = App.settings.SelectedIgtIndex) && (ix < w_items.Count))
					w_items.SelectedIndex = ix;

#if false
				var igt = w_items.SelectedItem;
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
				w_items.Focus();
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
		CorporaListView w_corpora { get { return ((main)App.Current.MainWindow).w_corpora; } }

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
				w_corpora.Focus();
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
