using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using alib.Enumerable;
using alib.Debugging;
using alib.Wpf;

namespace xie
{
	public class DepItemsControl : ItemsControl
	{
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			var fe = (FrameworkElement)element;
			var dp = (DepPart)item;

			fe.SetBinding(TreeLayoutPanel.LinkTextProperty, new Binding
			{
				Source = dp,
				Path = new PropertyPath(DepPart.DependencyTypeProperty),
			});

			if (dp.Head != null)
			{
				var fe_head = ItemContainerGenerator.ContainerFromItem(dp.Head);
				if (fe_head != null)
					fe.SetValue(TreeLayoutPanel.TreeParentProperty, fe_head);
			}
			else
			{
				fe.ClearValue(TreeLayoutPanel.TreeParentProperty);
			}

			foreach (var dp_dep in dp.Children())
			{
				var fe_dep = ItemContainerGenerator.ContainerFromItem(dp_dep);
				if (fe_dep != null)
					fe_dep.SetValue(TreeLayoutPanel.TreeParentProperty, fe);
			}
		}

		public TreeLayoutPanel ItemsHost
		{
			get
			{
				return (TreeLayoutPanel)
							this.GetType()
							.GetProperty("ItemsHost", BindingFlags.Instance | BindingFlags.NonPublic)
							.GetValue(this);
			}
		}
	};

	public class DepNode : TextBlock
	{
		public DepPart Part { get { return (DepPart)DataContext; } }

		public DependenciesTier Tier { get { return (DependenciesTier)Part.PartsHost; } }

		public DepItemsControl ic { get { return (DepItemsControl)this.Tag; } }

		public FrameworkElement fe
		{
			get { return (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(this.Part); }
		}

		public TreeLayoutPanel tlp { get { return (TreeLayoutPanel)ic.ItemsHost; } }

		protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
		{
			var cm = new ContextMenu();
			TextBlock.SetTextAlignment(cm, TextAlignment.Left);

			cm.Add(new MenuItem
			{
				IsEnabled = false,
				Header = "Select dependency head",
				Background = Brushes.Silver,
				Foreground = Brushes.White,
			});

			cm.Add<MenuItem>(new MenuItem
			{
				Header = "(none)",
				IsChecked = Part.Head == null,

			}).Click += (o, e1) =>
			{
				Part.Head = null;

				fe.ClearValue(TreeLayoutPanel.TreeParentProperty);

				tlp.InvalidateVisual();
			};

			foreach (var dp_head in Part.AvailableHeads)
			{
				cm.Add<MenuItem>(new MenuItem
				{
					Header = dp_head.Text,
					IsChecked = Part.Head == dp_head,

				}).Click += (o, e2) =>
				{
					Part.Head = dp_head;

					var fe_head = (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(dp_head);

					fe.SetValue(TreeLayoutPanel.TreeParentProperty, fe_head);

					tlp.InvalidateVisual();
				};
			}

			if (Part.Head != null)
			{
				cm.Add(new Separator());

				MenuItem mnu;
				String fn = "tags-dep.xaml";
				if (!App.FindConfigFile(ref fn))
					mnu = new MenuItem();
				else
					mnu = App.Load<MenuItem>(fn);

				cm.Add<MenuItem>(mnu).Click += (o, e3) =>
				{
					String s_dep_type = ((MenuItem)e3.OriginalSource).DataContext as String;
					if (s_dep_type == null)
						return;

					Part.DependencyType = s_dep_type;

					tlp.InvalidateVisual();
				};
			}
			//cm.Add(new MenuItem
			//{
			//	IsEnabled = false,
			//	Header = "Select dependency type",
			//	Background = Brushes.Silver,
			//	Foreground = Brushes.White,
			//});

			this.ContextMenu = cm;

			base.OnMouseRightButtonDown(e);
		}
	};

	//public class DepToggleButton : ToggleButton
	//{
	//	public DepPart Part { get { return (DepPart)DataContext; } }

	//	public DependenciesTier Tier { get { return (DependenciesTier)Part.PartsHost; } }

	//	protected override void OnClick()
	//	{
	//		base.OnClick();

	//		var mnu = (ContextMenu)this.Resources["ctx_menu"];

	//		Nop.X();
	//	}
	//}
}
