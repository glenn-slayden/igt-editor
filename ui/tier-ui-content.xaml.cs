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

using alib.Enumerable;
using alib.Debugging;
using alib.Wpf;

namespace xigt2
{
	public abstract class cmd_base : ICommand
	{
		protected cmd_base() { }

		public abstract void Execute(object parameter);
		public bool CanExecute(object parameter) { return true; }
		public event EventHandler CanExecuteChanged;
	};

	public class Cmd_RemovePart : cmd_base
	{
		public static readonly ICommand Instance = new Cmd_RemovePart();
		public override void Execute(Object parameter)
		{
			var uipc = (ui_part_controls)parameter;
			var uip = (ui_part_base)((Grid)uipc.Parent).Parent;
			var part = (IPart)uip.DataContext;
			uip.SegTier.Remove(part);
		}
	};

	public class Cmd_AddTextPart : cmd_base
	{
		public static readonly ICommand Instance = new Cmd_AddTextPart();
		public override void Execute(Object parameter)
		{
			var tier = (IPartsTier)parameter;

			tier.Add(new TextPart());
		}
	};

	public class Cmd_AddGroupPart : cmd_base
	{
		public static readonly ICommand Instance = new Cmd_AddGroupPart();
		public override void Execute(Object parameter)
		{
			var tier = (IPartsTier)parameter;

			var gp = new GroupPart();

			if (tier.Count > 0)
			{
				gp.Add(tier.Parts[0]);
				if (tier.Count > 1)
				{
					gp.Add(tier.Parts[1]);
					if (tier.Count > 2)
						gp.Add(tier.Parts[2]);
				}
			}
			tier.Add(gp);
		}
	};

	public class Cmd_PromotePart : cmd_base
	{
		public static readonly ICommand Instance = new Cmd_PromotePart();
		public override void Execute(Object parameter)
		{
			var uipc = (ui_part_controls)parameter;
			var uip = (ui_part_base)((Grid)uipc.Parent).Parent;
			var part = (IPart)uip.DataContext;

			var st = uip.SegTier as parts_tier_base;
			if (st != null)
				st.Promote(part);
		}
	};


	public class Cmd_MergePart : cmd_base
	{
		public static readonly ICommand Instance = new Cmd_MergePart();
		public override void Execute(Object parameter)
		{
			var uipc = (ui_part_controls)parameter;
			var uip = (ui_part_base)((Grid)uipc.Parent).Parent;
			var part = (IPart)uip.DataContext;

			var gp = part as MergePart;
			if (gp != null)
			{
				var st = uip.SegTier as parts_tier_base;
				if (st != null)
					st.Merge(part);
			}
			else
			{
				var st = uip.SegTier as parts_tier_base;
				if (st != null)
					st.Merge(part);
			}
		}
	};

	public partial class tier_ui_content : ContentControl
	{
		public tier_ui_content()
		{
			InitializeComponent();
		}

		public tier_base Tier { get { return (tier_base)Content; } }

		public tier_ui_base TierUib { get { return (tier_ui_base)((Grid)this.Parent).Parent; } }

		public TiersControl TiersControl { get { return TierUib.FindAncestor<TiersControl>(); } }

		public FrameworkElement GetTemplateItem(String s_name) { return (FrameworkElement)GetTemplateChild(s_name); }

		private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			var tb = (TextBox)sender;
			var tb_info = (TextBlock)((Grid)tb.Parent).Children[1];
			tb_info.Text = String.Format("<{0} - {1}> ({2})", tb.SelectionStart, tb.SelectionStart + tb.SelectionLength, tb.SelectionLength);
		}

		private void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			var tb = (TextBox)sender;
			if (tb.IsMouseCaptured)
			{
				String sel;
				int i_from, i_to;

				var tier = (TextTier)this.Tier;
				var igti = tier.TiersHost;

				while ((sel = tb.SelectedText).Length > 0 && Char.IsWhiteSpace(sel[sel.Length - 1]))
					tb.SelectionLength--;

				while (true)
				{
					i_from = tb.SelectionStart;
					i_to = i_from + tb.SelectionLength;
					if (i_from == i_to || !Char.IsWhiteSpace(tb.SelectedText[0]))
						break;
					tb.SelectionStart++;
					tb.SelectionLength--;
				}
				if (i_from != i_to)
				{
					var seg = TiersControl.ActiveSegmentation;
					if (seg == null)
					{
						TiersControl.ActiveSegmentation = seg = new SegTier { TierType = "Seg" };
						igti.Add(seg);
					}

					seg.Add(new SegPart
					{
						//PartsHost = seg,
						SourceTier = tier,
						FromChar = i_from,
						ToChar = i_to,
					});
				}
			}

			var tb_info = (TextBlock)((Grid)tb.Parent).Children[1];
			tb_info.Text = String.Empty;
		}

		private void btn_set_align(object sender, RoutedEventArgs e)
		{
			var align_tier = (AlignmentTier)this.Tier;
			var align_part = (AlignPart)((FrameworkElement)e.Source).DataContext;
			var src_part = align_part.Source;

		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			String s_dep_type = ((MenuItem)e.OriginalSource).DataContext as String;
			if (s_dep_type == null)
				return;

			var dep = ((MenuItem)sender).FindAncestor<ContextMenu>().DataContext as DepPart;
			if (dep == null)
				return;

			var tree = this.EnumerateVisualChildren().FirstOfType<TreeLayoutPanel>();

			dep.DependencyType = s_dep_type;

			tree.InvalidateVisual();
		}

		//private void MenuItem_Click(object sender, RoutedEventArgs e)
		//{

		//	var mi = (MenuItem)e.Source;
		//	var dp_head = (DepPart)mi.CommandParameter;
		//	var dp_dep = (DepPart)mi.DataContext;
		//	dp_dep.Head = dp_head;

		//	var ic = this.EnumerateVisualChildren().FirstOfType<ItemsControl>();
		//	var fe_head = (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(dp_head);
		//	var fe_dep = (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(dp_dep);

		//	TreeLayoutPanel.SetTreeParent(fe_dep, fe_head);
		//}

		//private void btn_dependency(Object sender, RoutedEventArgs e)
		//{
		//	var dep_part = (DepPart)((FrameworkElement)e.Source).DataContext;


		//	Nop.X();
		//}
#if false
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			Debug.WriteLine("c");

			base.OnPreviewMouseLeftButtonDown(e);

			if (e.Handled)
				throw new Exception();

			Debug.Print("d");

			e.Handled = true;
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters htp)
		{
			var htr = base.HitTestCore(htp);
			if (htr == null)
			{
				htr = new PointHitTestResult(this, htp.HitPoint);
			}
			else
			{
				Debug.WriteLine("hh");
			}
			return htr;
		}
#endif
	};

	public class DepItemsControl : ItemsControl
	{
#if false
		protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
		{
			Debug.Print("1");
			base.OnTemplateChanged(oldTemplate, newTemplate);
		}

		protected override void OnItemsPanelChanged(ItemsPanelTemplate oldItemsPanel, ItemsPanelTemplate newItemsPanel)
		{
			Debug.Print("2");
			base.OnItemsPanelChanged(oldItemsPanel, newItemsPanel);
		}

		protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue)
		{
			Debug.Print("3");
			base.OnItemsSourceChanged(oldValue, newValue);
		}

		protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
		{
			Debug.Print("4");
			base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);
		}
#endif
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

	};

	public class DepNode : TextBlock
	{
		public DepPart Part { get { return (DepPart)DataContext; } }

		public DependenciesTier Tier { get { return (DependenciesTier)Part.PartsHost; } }

		public tier_ui_content tui { get { return TemplatedParent.FindAncestor<tier_ui_content>(); } }

		public ItemsControl ic { get { return tui.EnumerateVisualChildren().FirstOfType<ItemsControl>(); } }

		public TreeLayoutPanel tlp { get { return tui.EnumerateVisualChildren().FirstOfType<TreeLayoutPanel>(); } }

		public FrameworkElement fe
		{
			get
			{
				return (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(this.Part);
			}
		}

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

			}).Click += (o, ee) =>
			{
				Part.Head = null;

				//var ic = tui.EnumerateVisualChildren().FirstOfType<ItemsControl>();
				//var fe_head = (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(dp_head);
				//var fe_dep = (FrameworkElement)ic.ItemContainerGenerator.ContainerFromItem(dp_dep);

				fe.ClearValue(TreeLayoutPanel.TreeParentProperty);
			};

			foreach (var dp_head in Part.AvailableHeads)
			{
				cm.Add<MenuItem>(new MenuItem
				{
					Header = dp_head.Text,
					IsChecked = Part.Head == dp_head,

				}).Click += (o, ee) =>
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
				var xx = (MenuItem)this.Resources["dep_types_menu"];
				if (xx.Parent != null)
					((ItemsControl)xx.Parent).Items.Remove(xx);
				cm.Add(xx);
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
