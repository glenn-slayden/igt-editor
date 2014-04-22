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

			settings = Load<Settings>(Settings.filename());
		}

		public static String FindConfigFile(String fn)
		{
			String path;

			var dir1 = Environment.CurrentDirectory;
			if (File.Exists(path = Path.Combine(dir1, fn)))
				return path;

			var dir2 = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (File.Exists(path = Path.Combine(dir2, fn)))
				return path;

			return dir1;
		}

		public static T Load<T>(String fn)
	where T : class, new()
		{
			fn = Path.GetFullPath(fn);
			if (!File.Exists(fn))
				return new T();

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

		public static Settings settings;
	};
}
