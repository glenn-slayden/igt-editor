using System;
using System.Diagnostics;
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

namespace xie
{
	public partial class align_ctrl : StackPanel
	{
		public align_ctrl()
		{
			InitializeComponent();

			Loaded += (o, e) =>
				{
					var at = (AlignmentTier)DataContext;

					at.Parts.CollectionChanged += (oo, ee) => InvalidateVisual();
				};
		}

		private void ToggleButton_Click(object sender, RoutedEventArgs e)
		{
			clear_toggles((ToggleButton)sender);
		}

		void clear_toggles(ToggleButton except)
		{
			foreach (var item in w_parts.Items)
			{
				var cp = (ContentPresenter)w_parts.ItemContainerGenerator.ContainerFromItem(item);
				var but_item = VisualTreeHelper.GetChild(cp, 0) as ToggleButton;

				if (but_item == except || but_item == null)
					continue;
				but_item.IsChecked = false;
			}
		}

		AlignPart get_selected_part()
		{
			foreach (var item in w_parts.Items)
			{
				var cp = (ContentPresenter)w_parts.ItemContainerGenerator.ContainerFromItem(item);
				var but_item = VisualTreeHelper.GetChild(cp, 0) as ToggleButton;

				if (but_item.IsChecked == true)
					return (AlignPart)cp.Content;
			}
			return null;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var ap1 = get_selected_part();
			if (ap1 == null)
			{
				return;
			}
			var p2 = (IPart)((Button)sender).DataContext;

			clear_toggles(null);

			if (ap1.AlignedParts.Contains(p2))
			{
				ap1.AlignedParts.Remove(p2);
				Debug.Print("Removed {0} to {1}", p2, ap1);
			}
			else
			{
				ap1.AlignedParts.Add(p2);
				Debug.Print("Added {0} to {1}", p2, ap1);
			}
			InvalidateVisual();
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			foreach (AlignPart it1 in w_parts.Items)
			{
				var cp1 = (ContentPresenter)w_parts.ItemContainerGenerator.ContainerFromItem(it1);
				var bi1 = VisualTreeHelper.GetChild(cp1, 0) as ToggleButton;
				var r1 = new Rect(bi1.DesiredSize);
				var pt1 = bi1.TransformToAncestor(this).Transform(r1.BottomCenter());

				foreach (var it2 in it1.AlignedParts)
				{
					var cp2 = (ContentPresenter)w_alignwith.ItemContainerGenerator.ContainerFromItem(it2);
					var bi2 = VisualTreeHelper.GetChild(cp2, 0) as Button;
					var r2 = new Rect(bi2.DesiredSize);
					var pt2 = bi2.TransformToAncestor(this).Transform(r2.TopCenter());

					dc.DrawLine(new Pen(Brushes.Black, 2), pt1, pt2);

				}
			}

		}
	};
}
