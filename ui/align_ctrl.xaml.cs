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

using alib.Wpf;

namespace xigt2
{
	public partial class align_ctrl : DockPanel
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
	};
}
