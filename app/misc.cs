using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using alib.Enumerable;
using alib.Debugging;
using alib.Wpf;

namespace xigt2
{
	static class __util
	{
		public static RenderTargetBitmap GetImage(UIElement fe, Brush background = null, Size sz = default(Size), int dpi = 144)
		{
			if (sz.Width < alib.Math.math.ε || sz.Height < alib.Math.math.ε)
			{
				fe.Measure(util.infinite_size);
				sz = fe.DesiredSize; //VisualTreeHelper.GetContentBounds(fe).Size; //
			}

			DrawingVisual dv = new DrawingVisual();
			RenderOptions.SetEdgeMode(dv, EdgeMode.Aliased);

			using (DrawingContext ctx = dv.RenderOpen())
			{
				Rect r = new Rect(0, 0, sz.Width, sz.Height);
				if (background != null)
					ctx.DrawRectangle(background, null, r);

				VisualBrush br = new VisualBrush(fe);
				br.AutoLayoutContent = true;
				ctx.DrawRectangle(br, null, r);
			}

			Double f = dpi / 96.0;

			RenderTargetBitmap bitmap = new RenderTargetBitmap(
				(int)(sz.Width * f) + 1,
				(int)(sz.Height * f) + 1,
				dpi,
				dpi,
				PixelFormats.Pbgra32);
			bitmap.Render(dv);
			return bitmap;
		}
	};
}