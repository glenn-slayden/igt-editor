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

namespace xie
{
	using Path = System.IO.Path;

	public partial class main : Window
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private void Menu_OpenXamlIgtDir(Object sender, RoutedEventArgs _)
		{
			var dir = App.settings.LastDirectory;
			if (!Directory.Exists(dir))
				dir = Environment.CurrentDirectory;

			var dlg = new CommonOpenFileDialog
			{
				Title = "Select xaml-igt directory",
				InitialDirectory = dir,
				IsFolderPicker = true,
				EnsureFileExists = true,
			};

			var e = Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories).GetEnumerator();
			while (e.MoveNext())
				if (!(dlg.DefaultFileName = Path.GetFileName(Path.GetDirectoryName(e.Current))).StartsWith("."))
					goto ok;
			dlg.DefaultFileName = null;
		ok:

			if (dlg.ShowDialog(this) == CommonFileDialogResult.Ok)
			{
				dir = dlg.FileName;

				cmd_OpenXamlIgtDir(dir);

				App.settings.LastDirectory = dir;
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

		private void Menu_SaveAllFiles(Object sender, RoutedEventArgs e)
		{
			int c_saved = save_all_files();

			var msg = String.Format("Saved {0} XAML-IGT files", c_saved);
			MessageBox.Show(this, msg, "Saved XAML-IGT files", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void Menu_SaveAllFilesTo(Object sender, RoutedEventArgs e)
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

				if (retarget_all_files_to(fn))
				{
					int c_saved = save_all_files();

					var msg = String.Format("{0} XAML-IGT files saved to '{1}'", c_saved, fn);
					MessageBox.Show(this, msg, "Saved XAML-IGT files", MessageBoxButton.OK, MessageBoxImage.Information);
				}
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
			w_opened.SetCurrentValue(TextBlock.TextProperty, null);
		}


		bool _load_xaml_file(String fn)
		{
			fn = Path.GetFullPath(fn);

			var ccc = _ensure_data_context();
			if (ccc.ContainsFile(fn))
				return false;

			var corpus = IgtCorpus.LoadXaml(fn);

			ccc.Add(corpus);

			if (w_corpora.SelectedIndex == -1)
				w_corpora.SelectedIndex = 0;

			return true;
		}

		public bool cmd_OpenXamlIgtDir(String dirname)
		{
			dirname = Path.GetFullPath(dirname);
			if (!Directory.Exists(dirname))
			{
				var msg = String.Format("The directory:\r\r{0}\r\rwas not found.", dirname);
				MessageBox.Show(this, msg, "Directory not found", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			Mouse.SetCursor(Cursors.Wait);

			String s_existing = null;

			var files = Directory.GetFiles(dirname, "*.xml");
			foreach (var filename in files)
			{
				if (!_load_xaml_file(filename))
				{
					s_existing = filename;
					break;
				}
			}

			Mouse.SetCursor(Cursors.Arrow);

			if (s_existing != null)
			{
				var msg = String.Format("A file named '{0}' is already open.", Path.GetFileNameWithoutExtension(s_existing));
				MessageBox.Show(this, msg, "File already open", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("opened {0} files from '{1}'.", files.Length, dirname));
			return true;
		}

		public bool cmd_OpenXamlIgtFile(String filename)
		{
			if (!File.Exists(filename))
			{
				var msg = String.Format("The file:\r\r{0}\r\rwas not found.", filename);
				MessageBox.Show(this, msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			Mouse.SetCursor(Cursors.Wait);

			var b = _load_xaml_file(filename);

			Mouse.SetCursor(Cursors.Arrow);

			if (!b)
			{
				var msg = String.Format("A file named '{0}' is already open.", Path.GetFileNameWithoutExtension(filename));
				MessageBox.Show(this, msg, "File already open", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("opened '{0}'.", filename));
			return true;
		}

		private int save_all_files()
		{
			var ccc = _ensure_data_context();

			int c_saved = 0;
			foreach (IgtCorpus c in ccc)
			{
				Exception ex = c.Save();

				if (ex == null)
					c_saved++;
				else
				{
					throw ex;
				}
			}

			Mouse.SetCursor(Cursors.Arrow);
			return c_saved;
		}

		private bool retarget_all_files_to(String xigtdir)
		{
			if (!Directory.Exists(xigtdir))
			{
				Directory.CreateDirectory(xigtdir);
				if (!Directory.Exists(xigtdir))
				{
					var msg = String.Format("The directory:\r\r{0}\r\rcould not be found or created.", xigtdir);
					MessageBox.Show(this, msg, "Cannot create directory", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}
			}

			Mouse.SetCursor(Cursors.Wait);
			var ccc = _ensure_data_context();

			foreach (IgtCorpus c in ccc)
				c.ChangeTargetDirectory(xigtdir);

			Mouse.SetCursor(Cursors.Arrow);
			return true;
		}

		public void cmd_CloseAll()
		{
			_clear_data_context();
		}
	};
}