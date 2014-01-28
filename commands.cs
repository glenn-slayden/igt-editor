#define test_data

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xaml;
using System.Xml;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using Microsoft.WindowsAPICodePack.Dialogs;


using alib;
using alib.Wpf;
using alib.Enumerable;
using alib.Debugging;

namespace xigt2
{
	using Path = System.IO.Path;

	public partial class __main : Window
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private void Menu_OpenXamlIgtDir(Object sender, RoutedEventArgs e)
		{
			var dlg = new CommonOpenFileDialog
			{
				Title = "Select xaml-igt directory",
				InitialDirectory = Path.GetDirectoryName(App.settings.LastDirectory),
				DefaultFileName = Path.GetFileName(App.settings.LastDirectory),
				IsFolderPicker = true,
			};

			if (dlg.ShowDialog(this) == CommonFileDialogResult.Ok)
			{
				var fn = dlg.FileName;
				App.settings.LastDirectory = fn;

				cmd_OpenXamlIgtDir(fn);
			}
		}

		private void Menu_OpenXamlIgtFile(object _, RoutedEventArgs __)
		{
			OpenFileDialog dlg = new OpenFileDialog
			{
				Title = "Open xaml-igt file...",
				DefaultExt = ".xml",
				Filter = "Xaml Interlinear Glossed Text (*.xml)|*.xml|All files (*.*)|*.*",
				InitialDirectory = App.settings.LastDirectory,
				AddExtension = true,
				CheckFileExists = true,
			};

			if (dlg.ShowDialog(this).Value)
			{
				var fn = dlg.FileName;
				//App.settings.LastFilename = fn;
				App.settings.LastDirectory = Path.GetDirectoryName(fn);
				cmd_OpenXamlIgtFile(fn);
			}
		}

		//private void Menu_SaveFile(Object sender, RoutedEventArgs e)
		//{
		//	//cmd_SaveFile();
		//}

		private void Menu_SaveAll(Object sender, RoutedEventArgs e)
		{
			var dlg = new CommonOpenFileDialog
			{
				Title = "Set directory for output files",
				InitialDirectory = Path.GetDirectoryName(App.settings.LastDirectory),
				DefaultFileName = Path.GetFileName(App.settings.LastDirectory),
				IsFolderPicker = true,
			};

			if (dlg.ShowDialog(this) == CommonFileDialogResult.Ok)
			{
				var fn = dlg.FileName;
				App.settings.LastDirectory = fn;
				int c_saved = cmd_SaveAll(fn);

				var msg = String.Format("{0} items saved to '{1}'", c_saved, fn);
				MessageBox.Show(this, msg, "Saved items", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void Menu_ResetSettings(Object sender, RoutedEventArgs e)
		{
			Settings.Reset();
		}

		private void Menu_CloseAll(Object sender, RoutedEventArgs e)
		{
			cmd_CloseAll();
		}

		private void Menu_Exit(Object sender, RoutedEventArgs e)
		{
			Close();
		}
		private void Menu_Exit_nosave(Object sender, RoutedEventArgs e)
		{
			f_nosave = true;
			Close();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		IgtCorpora _ensure_data_context()
		{
			var _tmp = (IgtCorpora)this.DataContext;
			if (_tmp == null)
				this.DataContext = _tmp = new IgtCorpora();
			return _tmp;
		}

		void _clear_data_context()
		{
			this.DataContext = null;
			w_opened.Content = null;
		}


		bool _load_xaml_file(String fn)
		{
			fn = Path.GetFullPath(fn);

			var ccc = _ensure_data_context();
			if (ccc.ContainsFile(fn))
				return false;

			var corpus = IgtCorpus.LoadXaml(fn);

			corpus.Filename = fn;

			ccc.Add(corpus);

			if (w_corpora.SelectedIndex == -1)
				w_corpora.SelectedIndex = 0;

			return true;
		}

		public void cmd_OpenXamlIgtDir(String dirname)
		{
			dirname = Path.GetFullPath(dirname);
			if (!Directory.Exists(dirname))
			{
				var msg = String.Format("The directory:\r\r{0}\r\rwas not found.", dirname);
				MessageBox.Show(this, msg, "Directory not found", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Mouse.SetCursor(Cursors.Wait);

			foreach (var filename in Directory.GetFiles(dirname, "*.xml"))
				_load_xaml_file(filename);

			Mouse.SetCursor(Cursors.Arrow);

			w_opened.Content = dirname;
		}

		public void cmd_OpenXamlIgtFile(String filename)
		{
			if (!File.Exists(filename))
			{
				var msg = String.Format("The file:\r\r{0}\r\rwas not found.", filename);
				MessageBox.Show(this, msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Mouse.SetCursor(Cursors.Wait);

			var b = _load_xaml_file(filename);

			Mouse.SetCursor(Cursors.Arrow);

			if (!b)
			{
				var msg = String.Format("A file named '{0}' is already open.", Path.GetFileNameWithoutExtension(filename));
				MessageBox.Show(this, msg, "File already open", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			w_opened.Content = filename;
		}

		private bool cmd_SaveFile(IgtCorpus corp, String xigtdir)
		{
			if (!Directory.Exists(xigtdir))
			{
				Directory.CreateDirectory(xigtdir);
				if (!Directory.Exists(xigtdir))
				{
					var msg = String.Format("The directory:\r\r{0}\r\rcould not be found or created.", xigtdir);
					MessageBox.Show(this, msg, "Directory not found", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}
			}

			corp.Save(xigtdir);

			return true;
		}

		private int cmd_SaveAll(String xigtdir)
		{
			Mouse.SetCursor(Cursors.Wait);

			var ccc = _ensure_data_context();

			int c_saved = 0;
			foreach (IgtCorpus c in ccc)
			{
				if (cmd_SaveFile(c, xigtdir))
					c_saved++;
			}

			Mouse.SetCursor(Cursors.Arrow);
			return c_saved;
		}

		public void cmd_CloseAll()
		{
			_clear_data_context();
		}
	};
}