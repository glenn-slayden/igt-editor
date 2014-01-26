using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Xml;
using System.Xaml;
using System.Threading.Tasks;
using System.Windows;

namespace xigt2
{
	public partial class App : Application
	{
		static App()
		{
			ctx = new XamlSchemaContext(new XamlSchemaContextSettings
			{
				SupportMarkupExtensionsWithDuplicateArity = true
			});

			settings = Settings.Load();
		}

		public App()
		{
			this.Exit += app_exit;
		}

		void app_exit(object sender, ExitEventArgs e)
		{
			settings.Save();
		}

		public static readonly XamlSchemaContext ctx;


		public static Settings settings;
	};
}
