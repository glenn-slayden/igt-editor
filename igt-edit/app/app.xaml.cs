using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xaml;
using System.Threading.Tasks;
using System.Windows;

namespace xie
{
	public partial class App : Application
	{
		static App()
		{
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

		public static Settings settings;
	};
}
