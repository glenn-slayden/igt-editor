using System;
using System.Collections.Generic;
using System.Diagnostics;
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

using alib.Debugging;
using alib.Enumerable;
using alib.Wpf;

namespace xie
{
	public partial class tier_ui_base : Border
	{
		/// using DataContext instead because it has magical automatic re-binding
		public tier_base Tier { get { return (tier_base)DataContext; } }

		static tier_ui_base()
		{
		}

		public TiersControl TiersControl
		{
			get { return this.FindAncestor<TiersControl>(); }
		}

		public tier_ui_content UiContent
		{
			get { return (tier_ui_content)((Grid)Child).Children.FirstOfType<tier_ui_content>(); }
		}

		public tier_ui_base()
		{
			InitializeComponent();

			//DataContextChanged += (o, e) => attach_tierindex_binding();
		}

		//void attach_tierindex_binding()
		//{
		//	var tier = (tier_base)DataContext;
		//	if (tier != null)
		//	{
		//		var bb = BindingOperations.SetBinding(tb_tier_index, TextBlock.TextProperty, new Binding
		//		{
		//			Source = tier.TiersHost,
		//			Path = new PropertyPath(dps.TiersProperty),
		//			Mode = BindingMode.OneWay,
		//			Converter = IndexOfConverter.Instance,
		//			ConverterParameter = tier,
		//		});

		//		tier.TiersHost.Tiers.CollectionChanged += (o, e) =>
		//		{
		//			if (bb.Status != BindingStatus.Detached)
		//				bb.UpdateTarget();
		//		};
		//	}
		//}

		protected override void OnContextMenuOpening(ContextMenuEventArgs e)
		{
			base.OnContextMenuOpening(e);

			if (e.Handled)
				return;

			if (e.Source != e.OriginalSource)
			{
				this.ContextMenu = null;
				return;
			}

			//e.Handled = true;

			MenuItem mi;
			var stier = this.DataContext as parts_tier_base;
			if (stier != null)
			{
				var cm = this.ContextMenu;
				if (cm != null)
				{
					var q = cm.Items.OfType<MenuItem>().FirstOrDefault(_mi => (String)_mi.Header == "Align with tier");
					if (q != null)
						cm.Items.Remove(q);
				}

				var host = stier.TiersHost;

				var hst = new HashSet<ITier>(host.Tiers);
				ITier pt;
				while ((pt = host as ITier) != null)
					host = pt.TiersHost;
				hst.UnionWith(host.Tiers);

				MenuItem m = null;
				foreach (var t in hst)
				{
					if (t == stier)
						continue;
					var stier2 = t as parts_tier_base;
					if (stier2 == null)
						continue;

					if (m == null)
						m = new MenuItem();

					mi = new MenuItem
					{
						Header = String.Format("{0} {1}", t.TierType, t.OuterIndex)
					};
					mi.Click += (o, ee) =>
					{
						var at = new AlignmentTier(stier.Parts, p =>
							{
								return new AlignPart { Source = p };
							}, p =>
							{
								throw new Exception("");
							})
						{
							AlignWith = stier2,
							TierType = "Align",
						};
						//at.AddParts(stier, stier2);
						stier.TiersHost.Add(at);
					};
					m.Items.Add(mi);
				}
				if (m != null)
				{
					m.Header = "Align with tier";
					if (cm == null)
						this.ContextMenu = cm = new ContextMenu();
					cm.Items.Insert(0, m);
				}
			}

			var tier1 = this.DataContext as TextTier;
			if (tier1 != null)
			{
				var cm = this.ContextMenu;
				if (cm != null)
				{
					var q = cm.Items.OfType<MenuItem>().FirstOrDefault(_mi => ((String)_mi.Header).StartsWith("Join with tier"));
					if (q != null)
						cm.Items.Remove(q);
				}

				var host = tier1.TiersHost;
				List<MenuItem> m = null;
				foreach (var t in host.Tiers)
				{
					if (t == tier1)
						continue;
					var tier2 = t as TextTier;
					if (tier2 == null)
						continue;

					if (m == null)
						m = new List<MenuItem>();

					mi = new MenuItem
					{
						Header = tier2.TierType //String.Format("{0} {1}", tier2.TierType, tier2.OuterIndex)
					};
					mi.Click += (o, ee) =>
					{
						var stt = (tier1.TierType ?? tier1.GetHashCode().ToString("X")) + ";" + (tier2.TierType ?? tier2.GetHashCode().ToString("X"));
						var mst = new CompoundTextTier
						{
							TierType = stt
						};

						mst.Lines.Add(tier1);
						mst.Lines.Add(tier2);
						host.Add(mst);
					};
					m.Add(mi);
				}
				if (m != null)
				{
					MenuItem mm;
					if (m.Count == 1)
					{
						mm = m[0];
						mm.Header = "Join with tier " + (String)mm.Header;
					}
					else
					{
						mm = new MenuItem
						{
							Header = "Join with tier",
							ItemsSource = m,
						};
					}
					if (cm == null)
						this.ContextMenu = cm = new ContextMenu();
					cm.Items.Insert(0, mm);
				}
			}

			var tierb = this.DataContext as tier_base;
			if (tierb != null)
			{
				var cm = this.ContextMenu;
				if (cm != null)
				{
					var q = cm.Items.OfType<MenuItem>().FirstOrDefault(_mi => (String)_mi.Header == "Promote");
					if (q != null)
						cm.Items.Remove(q);
				}

				var thh = tierb.TiersHost as ITier;
				if (thh != null)
				{
					if (cm == null)
						this.ContextMenu = cm = new ContextMenu();
					mi = new MenuItem { Header = "Promote" };
					mi.Click += (o, ee) =>
					{
						tier_base.MoveTier(tierb.TiersHost, tierb.OuterIndex, thh.TiersHost, thh.TiersHost.Count);
					};
					cm.Items.Insert(0, mi);
				}
			}
		}

		private void delete_tier(object sender, RoutedEventArgs e)
		{
			var tier = (tier_base)DataContext;
			var th = tier.TiersHost;
			var tiers = th.Tiers;

			tiers.Remove(tier);

			IHostedItem hi;
			if (tiers.Count == 0 && (hi = th as IHostedItem) != null)
			{
				Debug.Print("Removing {0} from {1}", hi.GetType().Name, hi.Host.GetType().Name);
				hi.Host.GetList().Remove(hi);
			}
		}
	};

	public class TextUiTier : tier_ui_base
	{
		public SegTier ActiveSegmentation
		{
			get { return (SegTier)GetValue(ActiveSegmentationProperty); }
			set { SetValue(ActiveSegmentationProperty, value); }
		}

		public static readonly DependencyProperty ActiveSegmentationProperty =
			DependencyProperty.Register("ActiveSegmentation", typeof(SegTier), typeof(TextUiTier), new PropertyMetadata(null));
	};
}
