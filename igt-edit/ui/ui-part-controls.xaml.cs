using System;
using System.Diagnostics;
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

namespace xie
{
	public partial class ui_part_controls : StackPanel
	{
		public ui_part_controls()
		{
			InitializeComponent();
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters htp)
		{
			var htr = base.HitTestCore(htp);
			if (htr == null)
				htr = new PointHitTestResult(this, htp.HitPoint);
			return htr;
		}
	};

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
}
