using System;
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
	public partial class TiersControl : ItemsControl
	{
		public TiersControl()
		{
			InitializeComponent();
		}

		public StackPanel Panel { get { return (StackPanel)GetTemplateChild("w_panel"); } }

		private void menu_tok_source_tier(object sender, RoutedEventArgs e)
		{
			var mi = (MenuItem)sender;
			var cm = (ContextMenu)mi.Parent;
			var uib = (tier_ui_base)cm.PlacementTarget;
			var src_tier = (TextTier)uib.Tier;

			src_tier.Segment();
		}

		private void menu_hide_tier(object sender, RoutedEventArgs e)
		{
			var mi = (MenuItem)sender;
			var cm = (ContextMenu)mi.Parent;
			var uib = (tier_ui_base)cm.PlacementTarget;
			var tier = (tier_base)uib.Tier;
			//if (tier == ActiveSegmentation)
			//	ActiveSegmentation = null;

			tier.SetValue(dps.IsVisibleProperty, false);
		}

		private void menu_pos_tag_tier(object sender, RoutedEventArgs e)
		{
			var mi = (MenuItem)sender;
			var cm = (ContextMenu)mi.Parent;
			var uib = (tier_ui_base)cm.PlacementTarget;
			var src_tier = (IPartsTier)uib.Tier;
			var igti = src_tier.TiersHost;

#if true
			var pos_tier = new PosTagTier(src_tier.Parts, t => new TagPart { Source = t }, u =>
			{
				throw new Exception("slave tier should not add new parts");
			})
			{
				TierType = "POS",
			};
#else
			var pos_tier = new PosTagTier();

			foreach (var p in src_tier.Parts)
			{
				var pospart = new TagPart
				{
					Source = p,
				};
				pos_tier.Add(pospart);
			}
#endif
			igti.Add(pos_tier);
		}

		private void menu_dependencies_tier(object sender, RoutedEventArgs e)
		{
			var mi = (MenuItem)sender;
			var cm = (ContextMenu)mi.Parent;
			var uib = (tier_ui_base)cm.PlacementTarget;
			var src_tier = (IPartsTier)uib.Tier;
			var igti = src_tier.TiersHost;

			var uic = uib.UiContent;

			var dep_tier = new DependenciesTier(src_tier.Parts, p => new DepPart { Source = p }, u =>
			{
				throw new Exception("slave tier (Dep) should not add new parts");
			})
			{
				TierType = "Dep"
			};
			igti.Add(dep_tier);
#if false
			UpdateLayout();

			var pc = Panel.Children;
			var contp = (ContentPresenter)pc[pc.Count - 1];
			var tlp = contp.EnumerateVisualChildren().FirstOfType<TreeLayoutPanel>();
			tlp.Children.Clear();

			tree_ui_part x1 = null, x2 = null;
			foreach (var p in src_tier.Parts)
			{
				var dep = new CopyPart
				{
					//PartsHost = p.PartsHost,
					Source = p,
				};
				dep_tier.Add(dep);

				var tup = new tree_ui_part(dep);
				if (x1 == null)
					x1 = tup;
				else if (x2 == null)
					x2 = tup;
				else
				{
					TreeLayoutPanel.SetTreeParent(x1, tup);
					TreeLayoutPanel.SetTreeParent(x2, tup);
					x1 = tup;
					x2 = null;
				}
				tlp.Children.Add(tup);
			}
#endif
		}
	};

	public class tree_ui_part : AttachmentHandles
	{
		public tree_ui_part(String text)
		{
			this.text = text;
			this.Content = new Border
			{
				Margin = new Thickness(3, 0, 3, 0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(2),
				Padding = new Thickness(3, 5, 3, 5),
				Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xC7)),
				Child = new TextBlock
				{
					Text = text,
				},
			};
		}
		public tree_ui_part(CopyPart dep)
			: this(dep.Text)
		{
			this.edge = dep;
		}
		public String text;
		public CopyPart edge;
	};
}
