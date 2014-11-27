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

		public TiersControl TiersControl { get { return this.FindAncestor<TiersControl>(); } }

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

		ITiers TiersHost { get { return this.Tier.TiersHost; } }


		protected override void OnContextMenuOpening(ContextMenuEventArgs e)
		{
			//base.OnContextMenuOpening(e);

			ContextMenu cm = null;
			MenuItem mi;

			foreach (var cmd in this.Tier.GetCommands())
			{
				if (cm == null)
					cm = new ContextMenu();

				cm.Items.Add(mi = new MenuItem
				{
					Header = cmd.CommandText,
				});
				mi.Click += cmd.Handler;
			}

#if false
			if (e.Handled)
				return;

			if (e.Source != e.OriginalSource)
			{
				this.ContextMenu = null;
				return;
			}

			//e.Handled = true;

			var tier = this.Tier;

			contextmenu_parts_tier(tier as parts_tier_base);

			contextmenu_text_tier(tier as TextTier);

			contextmenu_tier_base(tier as tier_base);
#endif
			this.ContextMenu = cm;
		}

#if false
		void contextmenu_parts_tier(parts_tier_base stier)
		{
			if (stier == null)
				return;

			var cm = this.ContextMenu;
			if (cm != null)
			{
				var q = cm.Items.OfType<MenuItem>().FirstOrDefault(_mi => (String)_mi.Header == "Align with tier");
				if (q != null)
					cm.Items.Remove(q);
			}

			var host = TiersHost;

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

				var mi = new MenuItem
				{
					Header = String.Format("{0} {1}", t.TierType, t.OuterIndex)
				};
				mi.Click += (o, ee) =>
				{
					var at = new AlignmentTier(stier.Parts)
					//var at = new AlignmentTier(stier.Parts, p =>
					//{
					//	return new AlignPart { Source = p };
					//}, p =>
					//{
					//	throw new Exception("");
					//})
					{
						AlignWith = stier2,
						TierType = "Align",
					};
					//at.AddParts(stier, stier2);
					TiersHost.Add(at);
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

		void contextmenu_text_tier(TextTier tier1)
		{
			if (tier1 == null)
				return;

			var cm = this.ContextMenu;
			if (cm != null)
			{
				var q = cm.Items.OfType<MenuItem>().FirstOrDefault(_mi => ((String)_mi.Header).StartsWith("Join with tier"));
				if (q != null)
					cm.Items.Remove(q);
			}

			var host = TiersHost;
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

				var mi = new MenuItem
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

		void contextmenu_tier_base(tier_base tierb)
		{
			if (tierb == null)
				return;

			var cm = this.ContextMenu;
			if (cm != null)
			{
				var q = cm.Items.OfType<MenuItem>().FirstOrDefault(_mi => (String)_mi.Header == "Promote");
				if (q != null)
					cm.Items.Remove(q);
			}

			var thh = TiersHost as ITier;
			if (thh != null)
			{
				var mi = new MenuItem { Header = "Promote" };
				mi.Click += (o, ee) =>
				{
					tier_base.MoveTier(TiersHost, tierb.OuterIndex, thh.TiersHost, thh.TiersHost.Count);
				};
				if (cm == null)
					this.ContextMenu = cm = new ContextMenu();

				cm.Items.Insert(0, mi);
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
#endif

		private void delete_tier(object sender, RoutedEventArgs e)
		{
			new cmd_delete_tier((ITier)DataContext).Execute();
		}



		//private void btn_set_align(object sender, RoutedEventArgs e)
		//{
		//	var align_tier = (AlignmentTier)this.Tier;
		//	var align_part = (AlignPart)((FrameworkElement)e.Source).DataContext;
		//	var src_part = align_part.Source;
		//}


		public SegTier ActiveSegmentation
		{
			get { return (SegTier)GetValue(ActiveSegmentationProperty); }
			set { SetValue(ActiveSegmentationProperty, value); }
		}

		public static readonly DependencyProperty ActiveSegmentationProperty =
			DependencyProperty.Register("ActiveSegmentation", typeof(SegTier), typeof(tier_ui_base), new PropertyMetadata(null));


		private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			var tb = (TextBox)sender;
			var tb_info = (TextBlock)((Grid)tb.Parent).Children[3];
			
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
					SegTier seg = ActiveSegmentation;
					if (seg == null || seg.Host == null)
					{
						ActiveSegmentation = seg = new SegTier { TierType = "Seg" };
						igti.Add(seg);
					}
					seg.IsVisible = true;

					seg.Add(new SegPart
					{
						//PartsHost = seg,
						SourceTier = tier,
						FromChar = i_from,
						ToChar = i_to,
					});
				}
			}

			var tb_info = (TextBlock)((Grid)tb.Parent).Children[3];
			tb_info.Text = String.Empty;
		}
	};
}
