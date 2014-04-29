using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;
using System.Windows.Markup;
using System.Windows.Media;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml;
using alib;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;
using alib.Wpf;
using Microsoft.Win32;

namespace xie
{
	using Path = System.IO.Path;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class Settings : DependencyObject
	{
		const String settings_filename = "app-settings.xaml";

		public static Settings Load()
		{
			Settings _new;

			var fn = settings_filename;
			if (App.FindConfigFile(ref fn))
			{
				_new = App.Load<Settings>(fn);
			}
			else
			{
				_new = new Settings();
			}
			_new.filename = fn;
			return _new;
		}

		public Settings()
		{
			this.LastDirectory = Environment.CurrentDirectory;
			this.SaveOnExit = true;
		}

		public String filename;

		public Settings Reset()
		{
			if (File.Exists(filename))
				File.Delete(filename);
			return Load();
		}

		public void Save()
		{
			using (var sw = XmlWriter.Create(filename, new XmlWriterSettings
			{
				Indent = true,
				NewLineOnAttributes = true,
				NamespaceHandling = NamespaceHandling.OmitDuplicates,
				OmitXmlDeclaration = true,
			}))
			using (var xr = new XamlObjectReader(this, App.xsch))
			using (var xw = new XamlXmlWriter(sw, App.xsch))
			{
				XamlServices.Transform(xr, xw);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public String LastDirectory { get; set; }

		public String[] SessionFiles { get; set; }

		public bool EscapeKeyExit
		{
			get { return (bool)GetValue(EscapeKeyExitProperty); }
			set { SetValue(EscapeKeyExitProperty, value); }
		}

		public bool ReloadLastSession
		{
			get { return (bool)GetValue(ReloadLastSessionProperty); }
			set { SetValue(ReloadLastSessionProperty, value); }
		}

		public bool SaveOnExit
		{
			get { return (bool)GetValue(SaveOnExitProperty); }
			set { SetValue(SaveOnExitProperty, value); }
		}

		public static readonly DependencyProperty SaveOnExitProperty =
			DependencyProperty.Register("SaveOnExit", typeof(bool), typeof(Settings), new PropertyMetadata(false));

		public String SelectedCorpusFilename { get; set; }

		public int SelectedIgtIndex { get; set; }

		public static readonly DependencyProperty ReloadLastSessionProperty =
			DependencyProperty.Register("ReloadLastSession", typeof(bool), typeof(Settings), new PropertyMetadata(false));

		public static readonly DependencyProperty EscapeKeyExitProperty =
			DependencyProperty.Register("EscapeKeyExit", typeof(bool), typeof(Settings), new PropertyMetadata(false));


		public bool WindowMaximized
		{
			get { return (bool)GetValue(WindowMaximizedProperty); }
			set { SetValue(WindowMaximizedProperty, value); }
		}

		public static readonly DependencyProperty WindowMaximizedProperty =
			DependencyProperty.Register("WindowMaximized", typeof(bool), typeof(Settings), new PropertyMetadata(false));
	};
}