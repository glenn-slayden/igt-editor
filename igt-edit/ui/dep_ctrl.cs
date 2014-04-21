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
	public class dep_ctrl : Panel
	{
		public dep_ctrl()
		{
		}

		protected override Size MeasureOverride(Size sz)
		{
			double x = 0.0;

			Rect r_all = util.zero_rect;

			foreach (UIElement el in InternalChildren)
			{
				if (!el.IsMeasureValid)
					el.Measure(sz);

				if (x > 0.0)
					x += 10.0;

				var r = new Rect(new Point(x, 0.0), el.DesiredSize);
				r_all.Union(r);

				x += r.Width;
			}
			return r_all.Size;
		}

		protected override Size ArrangeOverride(Size sz)
		{
			double x = 0.0;
			//double y = 0.0;
			foreach (UIElement el in InternalChildren)
			{
				if (x > 0.0)
					x += 10.0;

				var r = new Rect(new Point(x, 0.0), el.DesiredSize);
				el.Arrange(r);

				x += r.Width;
			}

			return sz;
		}

		//protected override void OnRender(DrawingContext dc)
		//{
		//	base.OnRender(dc);
		//}
	};
}
