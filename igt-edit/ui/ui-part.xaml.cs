using System;
using System.Diagnostics;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xaml;


using alib.Wpf;
using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	using XamlReader = System.Windows.Markup.XamlReader;

	public partial class ui_part : ContentControl
	{
		static ArrayList arr_pos_tags;

		static ui_part()
		{
			arr_pos_tags = App.Load<ArrayList>(App.FindConfigFile("tags-pos.xaml"));
		}

		public ui_part()
		{
			InitializeComponent();
		}

		private void ComboBox_Loaded(object sender, RoutedEventArgs e)
		{
			((ComboBox)sender).ItemsSource = arr_pos_tags;
		}
	};
}
