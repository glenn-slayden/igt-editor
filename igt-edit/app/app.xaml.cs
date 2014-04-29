using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using System.Xaml;
using System.Threading.Tasks;
using System.Windows;

namespace xie
{
	public partial class App : Application
	{
		public static readonly XamlSchemaContext xsch;

		static App()
		{
			xsch = new XamlSchemaContext(new XamlSchemaContextSettings
			{
				SupportMarkupExtensionsWithDuplicateArity = true,
			});

			settings = Settings.Load();
		}

		public static Settings settings;

		public static void ResetSettings()
		{
			settings = settings.Reset();
		}

		public static bool FindConfigFile(ref String filename)
		{
			String fn1, fn2;

			var dir1 = Environment.CurrentDirectory;
			if (File.Exists(fn1 = Path.Combine(dir1, filename)))
			{
				filename = fn1;
				return true;
			}

			var dir2 = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (File.Exists(fn2 = Path.Combine(dir2, filename)))
			{
				filename = fn2;
				return true;
			}

			filename = fn1;
			return false;
		}


		public static T Load<T>(String fn)
			where T : class
		{
			T ret;
			using (var sr = new StreamReader(fn))
			using (var xr = new XamlXmlReader(sr, xsch))
			using (var xw = new XamlObjectWriter(xsch))
			{
				XamlServices.Transform(xr, xw);

				ret = (T)xw.Result;
			}
			return ret;
		}

		public App()
		{
			this.Exit += app_exit;
		}

		void app_exit(object sender, ExitEventArgs e)
		{
			settings.Save();
		}
	};
}
