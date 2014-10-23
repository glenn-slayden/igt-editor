using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Windows.Shapes;

using alib;
using alib.Wpf;
using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	public partial class ui_part_base : Border
	{
		public static readonly DependencyProperty HighlightProperty =
				DependencyProperty.Register("Highlight", typeof(bool), typeof(ui_part_base),
				new FrameworkPropertyMetadata(default(bool), (o, e) =>
				{
					var uip = (ui_part_base)o;
					if ((bool)e.NewValue)
					{
						uip.BorderBrush = Brushes.DarkOrange;
						uip.Background = SolidColorBrushCache.Get(Color.FromArgb(0x80, 255, 255, 255));
					}
					else
					{
						uip.BorderBrush = Brushes.Transparent;
						uip.Background = Brushes.Transparent;
					}
				}));

		public ui_part_base()
		{
			InitializeComponent();

			DataContextChanged += (o, e) =>
			{
				var tdp = e.NewValue as temp_drag_part;
				if (tdp != null)
				{
					((ui_part_base)o).Highlight = true;
					//if (tdp.SourcePart is IEditText)
					//	w_part_controls.w_btn_edit_part.Visibility = Visibility.Collapsed;
				}

				//if (e.NewValue.GetType() == typeof(SegPart))
				//	Nop.X();

				//var cp = e.NewValue as CopyPart;
				//if (cp != null)
				//{
				//	var q = cp.SourcePart;

				//	Nop.X();

				//}

				//Debug.Print("{0:X8} {1} {2}", o.GetHashCode(), o.GetType().Name, e.NewValue.GetType().Name);
			};
		}
#if false
		static void uipb_DataContextChanged(Object o, DependencyPropertyChangedEventArgs e)
		{
			var tdp = e.NewValue as temp_drag_part;
			if (tdp != null)
			{
				((ui_part_base)o).Highlight = true;
				//if (tdp.SourcePart is IEditText)
				//	w_part_controls.w_btn_edit_part.Visibility = Visibility.Collapsed;
			}
		}

		static void w_text_PreviewTextInput(Object sender, TextCompositionEventArgs e)
		{
			var tb = (TextBox)sender;
			var uip = (ui_part_base)tb.Parent;
			tb.PreviewTextInput -= w_text_PreviewTextInput;

			var st = uip.SegTier as parts_tier_base;
			if (st != null)
				st.Promote((IPart)uip.DataContext);
		}
#endif

		public new FrameworkElement Parent { [DebuggerStepThrough] get { return (FrameworkElement)base.Parent; } }

		public bool Highlight
		{
			get { return (bool)GetValue(HighlightProperty); }
			set { SetValue(HighlightProperty, value); }
		}


		public Part SourcePart
		{
			//[DebuggerStepThrough]
			get
			{
#if false
				//var p1 = (Part)DataContext;

				if (this.Parent != null)
				{
					return (Part)((FrameworkElement)this.Parent).DataContext;
					//var p2 = ((FrameworkElement)this.Parent).DataContext;
					//if (p1 != p2)
					//	Nop.X();
				}
#endif
				return (Part)DataContext;
			}
		}

		//public Part CurrentPart
		//{
		//	[DebuggerStepThrough]
		//	get
		//	{
		//		var par = this.Parent;
		//		return (Part)(par == null ? DataContext : par.DataContext);
		//	}
		//}

		public IParts PartsHost
		{
			get
			{
				//if (VirtualPartsHost != null)
				//	Nop.X();

				return SourcePart.PartsHost;
			}
		}

		//public IParts VirtualPartsHost
		//{
		//	get
		//	{
		//		var vph = TemplatedParent.FindAncestor<Panel>().DataContext;
		//		return vph == Part.PartsHost ? null : (IParts)vph;
		//	}
		//}


		public int Index { get { return PartsHost.IndexOf(SourcePart); } }

		public TiersControl TiersControl
		{
			get { return this.FindAncestor<TiersControl>(); }
		}

		public ItemContainerGenerator ItemContainerGenerator
		{
			get
			{
				var cp = TiersControl.ItemContainerGenerator.ContainerFromItem(this.PartsHost);
				if (cp == null)
					return null;
				var stic = cp.EnumerateVisualChildren().OfType<ItemsControl>().Where(ic => ic.ItemsSource is SegTier).ToArray();
				if (stic.Length == 0)
					return null;
				if (stic.Length > 1)
					throw new Exception();
				var icg = stic[0].ItemContainerGenerator;
				return icg;
			}
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters htp)
		{
			var htr = base.HitTestCore(htp);
			if (htr == null)
				htr = new PointHitTestResult(this, htp.HitPoint);
			return htr;
		}

		public PartLocationRef PartLocationRef { get { return new PartLocationRef(this.PartsHost, this.Index); } }

		temp_drag_part tdp;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			PartLocationRef __this, __drop;
			Part p = this.SourcePart;

			//FrameworkElement fe_hide = this.Parent ?? this;
			//FrameworkElement fe_hide = this;

			Nop.X();

			try
			{
				__this = this.PartLocationRef;

				Debug.Assert(__this.SourcePart == p && !(p is temp_drag_part));

				//fe_hide.Visibility = Visibility.Collapsed;
				//Visibility = Visibility.Collapsed;
				p.IsVisible = false;

				try
				{
					this.tdp = new temp_drag_part(this);

					var re = dd_loop();
					if (re == DragDropEffects.None)
						return;

					__drop = tdp.PartTierAssignment;
				}
				finally
				{
					tdp.Remove();
					tdp = null;
				}

				if (__drop.Equals(__this))
					return;

				Debug.Print("move {0} '{1}' {2} -> {3}", p.GetType().Name, p.ToString(), __this, __drop);

				if (__this.host == __drop.host)
				{
					__this.host.Move(__this.Index, __drop.Index);
				}
				else
				{
					__this.host.RemoveAt(__this.Index);
					__drop.host.Insert(__drop.Index, p);
				}
			}
			finally
			{
				//fe_hide.Visibility = Visibility.Visible;
				//Visibility = Visibility.Visible;
				p.IsVisible = true;

				e.Handled = true;
			}
		}

		DragDropEffects dd_loop()
		{
			//var c = this.Cursor;
			//this.Cursor = Cursors.No;

			GiveFeedbackEventHandler _gf_func = (o, e) =>
			{
				((ui_part_base)o).change();

				e.UseDefaultCursors = true;
				e.Handled = true;
			};

			GiveFeedback += _gf_func;

			var re = DragDrop.DoDragDrop(this, SourcePart, DragDropEffects.Link);

			GiveFeedback -= _gf_func;

			//this.Cursor = c;

			return re;
		}

#if false
		static void uipb_GiveFeedback(Object o, GiveFeedbackEventArgs e)
		{
			((ui_part_base)o).change();

			e.UseDefaultCursors = true;
			e.Handled = true;
		}
#endif

		void change()
		{
			ui_part_base uip_tgt = null;
			var panel = TiersControl.Panel;

			VisualTreeHelper.HitTest(panel,
				obj => HitTestFilterBehavior.Continue,
				htr =>
				{
					if ((uip_tgt = htr.VisualHit as ui_part_base) != null)
						return HitTestResultBehavior.Stop;
					return HitTestResultBehavior.Continue;
				},
				new PointHitTestParameters(util.GetCorrectMousePosition(panel)));

			if (uip_tgt != null && uip_tgt != this && !(uip_tgt.SourcePart is temp_drag_part))
				tdp.ChangeUiPart(uip_tgt);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public struct PartLocationRef : IEquatable<PartLocationRef>
	{
		public PartLocationRef(IParts host, int ix)
		{
			this.host = host;
			this.Index = ix;
		}
		public IParts host;
		public int Index;

		public IPart SourcePart { get { return host.Parts[Index]; } }

		public bool Equals(PartLocationRef other)
		{
			return this.host == other.host && this.Index == other.Index;
		}
		public override int GetHashCode()
		{
			return host.GetHashCode() ^ Index;
		}
		public override bool Equals(object obj)
		{
			return obj is PartLocationRef && Equals((PartLocationRef)obj);
		}
		public override String ToString()
		{
			if (host == null)
				return "null";
			return String.Format("{0}.{1} ({2})", host.OuterIndex(), Index, SourcePart.GetType().Name);
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class temp_drag_part : CopyPart
	{
		temp_drag_part(IPart part)
		{
			base.SourcePart = part;
		}
		public temp_drag_part(ui_part_base uip_drag)
			: this(uip_drag.SourcePart)
		{
			this.tier_cur = this.tier_original = uip_drag.PartsHost;

			tier_cur.Insert(uip_drag.Index + 1, this);
		}

		public IParts tier_original;
		public IParts tier_cur;

		public PartLocationRef PartTierAssignment { get { return new PartLocationRef(tier_cur, AdjustedIndex); } }

		public void Remove() { tier_cur.Remove(this); }

		public int AdjustedIndex
		{
			get
			{
				var ix = tier_cur.IndexOf(this);
				AdjustIndex(tier_cur, ref ix);
				return ix;
			}
		}

		/// the UI for the original part (stored in 'source' of our base class) is WPF-hidden, so don't exclude
		/// it from the adjacency calculation
		public void AdjustIndex(IParts tier, ref int ix)
		{
			int ix_original;
			if (tier == tier_original && (ix_original = tier_original.IndexOf(base.SourcePart)) != -1 && ix > ix_original)
				ix--;
		}

		bool anti_ocillation(ui_part_base uip_tgt, int ix_drag, int ix_tgt)
		{
			var tier = uip_tgt.PartsHost;
			AdjustIndex(tier, ref ix_drag);
			AdjustIndex(tier, ref ix_tgt);

			int d = ix_tgt - ix_drag;
			if (d == -1 || d == 1)
			{
				var icg = uip_tgt.ItemContainerGenerator;
				if (icg == null)
					return false;
				var cp = icg.ContainerFromItem(this) as ContentPresenter;
				if (cp == null)
					return false;

				var drg_width = cp.ActualWidth;
				var tgt_excess = uip_tgt.ActualWidth - drg_width;

				if (tgt_excess > 0)
				{
					var x_offs = util.GetCorrectMousePosition(uip_tgt).X;
					if (d == -1)
					{
						if (x_offs > drg_width)
							return true;
					}
					else
					{
						if (x_offs < tgt_excess)
							return true;
					}
				}
			}
			return false;
		}

		public void ChangeUiPart(ui_part_base uip_tgt)
		{
			var tgt_tier = uip_tgt.PartsHost;
			var ix_tgt = uip_tgt.Index;
			var ic_cur = tier_cur.IndexOf(this);

			if (tier_cur != tgt_tier)
			{
				tier_cur.RemoveAt(ic_cur);
				(tier_cur = tgt_tier).Insert(ix_tgt, this);
			}
			else if (ic_cur != ix_tgt && !anti_ocillation(uip_tgt, ic_cur, ix_tgt))
			{
				tier_cur.Move(ic_cur, ix_tgt);
			}
		}

		public override String ToString()
		{
			return String.Format("{0}.{1}", tier_cur.OuterIndex(), AdjustedIndex);
		}
	};
}
