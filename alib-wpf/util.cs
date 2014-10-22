using System;
using System.Windows;
using System.Linq;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace alib.Wpf
{
	using SP = SystemParameters;
	using String = System.String;

	public static class _interop
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Win32Point
		{
			public Int32 X;
			public Int32 Y;
		};

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll")]
		public static extern uint GetDoubleClickTime();

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(ref Win32Point pt);
	};

	public static partial class util
	{
		static util()
		{
			BooleanToVisibilityConverterInst = new BooleanToVisibilityConverter();

			xlang_en = XmlLanguage.GetLanguage("en");
			xlang_th = XmlLanguage.GetLanguage("th");

			(EmptyDrawing = new GeometryDrawing(null, null, RectangleGeometry.Empty)).Freeze();

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
				Scale = 1.37,
			});

			ff_arial = new FontFamily("Arial");
			ff_arial_ums = new FontFamily("Arial Unicode MS");
			ff_tahoma = new FontFamily("Tahoma");
			ff_cambria = new FontFamily("Cambria");
			ff_calibri = new FontFamily("Calibri");
			ff_consolas = new FontFamily("Consolas");
			ff_lucida_console = new FontFamily("Lucida Console");

			tf_calibri = new Typeface("Calibri");

			LightGrayPen = new Pen(Brushes.LightGray, 1);
			DarkGrayPen = new Pen(Brushes.DarkGray, 1);

			DoubleClickTime = TimeSpan.FromMilliseconds(_interop.GetDoubleClickTime());
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

		public static Window GetActiveWindow()
		{
			IntPtr active = _interop.GetActiveWindow();
			Window w;

			var e = Application.Current.Windows.GetEnumerator();
			while (e.MoveNext())
				if ((w = e.Current as Window) != null && new WindowInteropHelper(w).Handle == active)
					return w;
			return null;
		}


		public static readonly TimeSpan DoubleClickTime;

		public static readonly Pen LightGrayPen;
		public static readonly Pen DarkGrayPen;

		public static Brush gethatchbrush()
		{
			return new DrawingBrush
			{
				Stretch = Stretch.None,
				ViewportUnits = BrushMappingMode.Absolute,
				Viewport = new Rect(0, 0, 10, 10),
				TileMode = TileMode.Tile,
				Drawing = new GeometryDrawing
				{
					Pen = LightGrayPen,
					Geometry = new LineGeometry
					{
						StartPoint = new Point(-1, -1),
						EndPoint = new Point(11, 11),
					}
				},
			};
		}
	};
}
