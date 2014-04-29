using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace alib.Wpf
{
	using SP = SystemParameters;

	public static partial class util
	{
		static util()
		{
			BooleanToVisibilityConverterInst = new BooleanToVisibilityConverter();

			xlang_en = XmlLanguage.GetLanguage("en");
			xlang_th = XmlLanguage.GetLanguage("th");

			var d = EmptyDrawing = new DrawingGroup();
			d.ClearValue(DrawingGroup.ChildrenProperty);
			d.Freeze();

			zero_size = new Size(0, 0);
			infinite_size = new Size(double.PositiveInfinity, double.PositiveInfinity);
			coord_origin = new Point(0, 0);
			point_NaN = new Point(Double.NaN, Double.NaN);
			zero_rect = new Rect(coord_origin, zero_size);

			ff_segoe = new FontFamily();
			ff_segoe.FamilyNames[XmlLanguage.GetLanguage("en-US")] = "agree font";
			ff_segoe.FamilyMaps.Add(new FontFamilyMap
			{
				Target = "Segoe UI",
				Language = xlang_en,
				Scale = 1.0,
			});
			ff_segoe.FamilyMaps.Add(new FontFamilyMap
			{
				Target = "Tahoma",
				Language = xlang_th,
				Scale = 1.27,
			});

			ff_arial = new FontFamily("Arial");
			ff_arial_ums = new FontFamily("Arial Unicode MS");
			ff_tahoma = new FontFamily("Tahoma");
			ff_cambria = new FontFamily("Cambria");
			ff_calibri = new FontFamily("Calibri");
			ff_consolas = new FontFamily("Consolas");

			tf_calibri = new Typeface("Calibri");
		}

		public static int GetScreenConfigHash()
		{
			return
				(int)SP.PrimaryScreenWidth ^
				((int)SP.PrimaryScreenHeight << 8) ^
				((int)SP.VirtualScreenWidth << 16) ^
				((int)SP.VirtualScreenHeight << 24) ^
				((int)SP.VirtualScreenLeft << 11) ^
				((int)SP.VirtualScreenTop << 21);
		}
	};
}
