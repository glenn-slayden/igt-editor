using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using alib.Enumerable;
using alib.Debugging;
using alib.Wpf;

namespace xigt2
{
	public partial class IgtControl : DockPanel
	{
		public IgtControl()
		{
			InitializeComponent();

			this.HorizontalAlignment = HorizontalAlignment.Stretch;
			this.VerticalAlignment = VerticalAlignment.Stretch;
		}
	};
}
