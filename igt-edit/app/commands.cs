#define test_data

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
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

	class ErrorActionException : Exception
	{
		public Action show;
	}

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
				InitialDirectory = Path.GetDirectoryName(dir),
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
			App.ResetSettings();
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


		public void cmd_OpenXamlIgtDir(String dirname)
		{
			try
			{
				Mouse.SetCursor(Cursors.Wait);

				open_dir(dirname);
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

		public void cmd_OpenXamlIgtFile(String filename)
		{
			try
			{
				Mouse.SetCursor(Cursors.Wait);

				open_file(filename);
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

		public void cmd_CloseAll()
		{
			ccc.Clear();
			w_opened.SetCurrentValue(TextBlock.TextProperty, null);
		}

		private int save_all_files()
		{
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

			foreach (IgtCorpus c in ccc)
				c.ChangeTargetDirectory(xigtdir);

			Mouse.SetCursor(Cursors.Arrow);
			return true;
		}


		public void open_dir(String dirname)
		{
			dirname = Path.GetFullPath(dirname);

			if (!Directory.Exists(dirname))
				throw new ErrorActionException
				{
					show = () =>
					{
						var msg = String.Format("The directory:\r\r{0}\r\rwas not found.", dirname);
						MessageBox.Show(this, msg, "Directory not found", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				};

			var files = Directory.GetFiles(dirname, "*.xml");

			if (files.Length == 0)
				throw new ErrorActionException
				{
					show = () =>
					{
						MessageBox.Show(this, "There are no files to load", "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				};

			open_files(files);

			w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("opened {0} files from '{1}'.", files.Length, dirname));
		}

		public void open_files(String[] rg)
		{
			foreach (var fn in rg)
			{
				open_file(fn);
			}
		}

		public void open_file(String fn)
		{
			fn = Path.GetFullPath(fn);
			if (ccc.ContainsFile(fn))
				throw new ErrorActionException
				{
					show = () =>
					{
						var msg = String.Format(@"A file named '{0}' is already open.

To prevent this message, close all XIGT-corpus files first.", Path.GetFileNameWithoutExtension(fn));
						MessageBox.Show(this, msg, "File already open", MessageBoxButton.OK, MessageBoxImage.Information);
					}
				};

			var task = Task.Factory.StartNew(() =>
			{
				var xc = open_file_inner(fn);

				ccc.Add(xc);

				if (w_corpora.SelectedIndex == -1)
					w_corpora.SelectedIndex = 0;

				Dispatcher.InvokeAsync(() => w_opened.SetCurrentValue(TextBlock.TextProperty, String.Format("opened '{0}'.", fn)));
			},
			CancellationToken.None,
			TaskCreationOptions.None,
			TaskScheduler.FromCurrentSynchronizationContext());
		}

		public IgtCorpus open_file_inner(String fn)
		{
			fn = Path.GetFullPath(fn);
			if (!File.Exists(fn))
			{
				throw new ErrorActionException
				{
					show = () =>
					{
						var msg = String.Format("The file:\r\r{0}\r\rwas not found.", fn);
						MessageBox.Show(this, msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				};
			}

			return IgtCorpus.LoadXaml(fn);
		}
	};
}