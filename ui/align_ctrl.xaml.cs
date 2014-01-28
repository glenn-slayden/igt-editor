using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using alib.Wpf;
using alib.Enumerable;
using alib.Debugging;

namespace xigt2
{
	public partial class align_ctrl : StackPanel
	{
		public align_ctrl()
		{
			InitializeComponent();
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			var r = new Rect(base.RenderSize);

			dc.DrawLine(new Pen(Brushes.Black, 2), r.TopLeft, r.BottomRight);
		}

		private void ToggleButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in w_parts.Items)
			{
				var cp = (ContentPresenter)w_parts.ItemContainerGenerator.ContainerFromItem(item);
				var but_item = VisualTreeHelper.GetChild(cp, 0) as ToggleButton;

				if (but_item == null || but_item == sender)
					continue;
				but_item.IsChecked = false;
			}
		}

		IPart get_selected_part()
		{
			foreach (var item in w_parts.Items)
			{
				var cp = (ContentPresenter)w_parts.ItemContainerGenerator.ContainerFromItem(item);
				var but_item = VisualTreeHelper.GetChild(cp, 0) as ToggleButton;

				if (but_item.IsChecked == true)
					return (IPart)cp.Content;
			}
			return null;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var ip = get_selected_part();
		}
	};
}
