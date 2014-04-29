using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

using alib.Math;

namespace alib.Wpf
{
	using String = System.String;

	public static partial class util
	{
		public static Color ToColor(this String sc)
		{
			if (sc.Length > 0 && sc[0] == '#')
				sc = sc.Substring(1);
			var ul = ulong.Parse(sc, NumberStyles.HexNumber);
			if (sc.Length <= 6)
				ul |= 0xFF000000;
			return new Color
			{
				B = (byte)ul,
				G = (byte)(ul >>= 8),
				R = (byte)(ul >>= 8),
				A = (byte)(ul >>= 8),
			};
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class SolidColorBrushCache
	{
		static SolidColorBrushCache()
		{
			_brush_cache = new[]
			{
				Brushes.White,
				Brushes.Black,
				Brushes.Crimson,
				Brushes.MediumBlue,
				Brushes.AliceBlue,
			};
		}
		static SolidColorBrush[] _brush_cache;

		public static SolidColorBrush Get(byte r, byte g, byte b)
		{
			return Get(Color.FromRgb(r, g, b));
		}
		public static SolidColorBrush Get(ulong rgb)
		{
			return Get((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		}

		public static SolidColorBrush Get(Color c)
		{
			SolidColorBrush br;
			var _tmp = _brush_cache;
			for (int i = 0; i < _tmp.Length; i++)
				if ((br = _tmp[i]).Color.Equals(c))
					goto got;
			(br = new SolidColorBrush(c)).Freeze();
			Interlocked.CompareExchange(ref _brush_cache, alib.Array.arr.Append(_tmp, br), _tmp);
		got:
			return br;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public struct HSL
	{
		public HSL(Color rgb)
			: this()
		{
			this.A = (double)rgb.A / 255;
			var r = (double)rgb.R / 255;
			var g = (double)rgb.G / 255;
			var b = (double)rgb.B / 255;

			var min = math.Min(r, g, b);
			var max = math.Max(r, g, b);
			var delta = max - min;

			if (delta.IsZero())
			{
				this.H = 0;
				this.S = 0;
				this.L = max;
				return;
			}

			this.L = (min + max) / 2;

			if (this.L < 0.5)
				this.S = delta / (max + min);
			else
				this.S = delta / (2.0 - max - min);

			if (r == max)
				this.H = (g - b) / delta;
			if (g == max)
				this.H = 2.0 + (b - r) / delta;
			if (b == max)
				this.H = 4.0 + (r - g) / delta;
			this.H *= 60;
			if (this.H < 0)
				this.H += 360;
		}

		public double H, S, L, A;

		public Color ToRGB()
		{
			if (this.S == 0)
				return new Color
				{
					R = (byte)(this.L * 255),
					G = (byte)(this.L * 255),
					B = (byte)(this.L * 255),
					A = (byte)(this.A * 255),
				};

			double t1;
			if (this.L < 0.5)
				t1 = this.L * (1.0 + this.S);
			else
				t1 = this.L + this.S - (this.L * this.S);

			var t2 = 2.0 * this.L - t1;

			var h = this.H / 360;

			var tR = h + (1.0 / 3.0);
			var r = SetColor(t1, t2, tR);

			var tG = h;
			var g = SetColor(t1, t2, tG);

			var tB = h - (1.0 / 3.0);
			var b = SetColor(t1, t2, tB);

			return new Color
			{
				R = (byte)(r * 255),
				G = (byte)(g * 255),
				B = (byte)(b * 255),
				A = (byte)(this.A * 255),
			};
		}

		static double SetColor(double t1, double t2, double t3)
		{
			if (t3 < 0)
				t3 += 1.0;
			if (t3 > 1)
				t3 -= 1.0;

			double color;
			if (6.0 * t3 < 1)
				color = t2 + (t1 - t2) * 6.0 * t3;
			else if (2.0 * t3 < 1)
				color = t1;
			else if (3.0 * t3 < 2)
				color = t2 + (t1 - t2) * ((2.0 / 3.0) - t3) * 6.0;
			else
				color = t2;
			return color;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class AdjustColor : MarkupExtension
	{
		public AdjustColor(Control fe, Double adj_H, Double adj_S, Double adj_L)
			: this(fe.Background as SolidColorBrush, adj_H, adj_S, adj_L)
		{
		}
		public AdjustColor(SolidColorBrush br, Double adj_H, Double adj_S, Double adj_L)
			: this(br == null ? default(Color) : br.Color, adj_H, adj_S, adj_L)
		{
		}
		public AdjustColor(Color c, Double adj_H, Double adj_S, Double adj_L)
		{
			var hsl = new HSL(c);
			hsl.H *= adj_H;
			hsl.S *= adj_S;
			hsl.L *= adj_L;
			this.c_adjusted = hsl.ToRGB();
		}
		readonly Color c_adjusted;
		public override Object ProvideValue(IServiceProvider serviceProvider)
		{
			return c_adjusted;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class AdjustColorConverter : IValueConverter
	{
		public static readonly AdjustColorConverter Instance;
		static AdjustColorConverter() { Instance = new AdjustColorConverter(); }
		AdjustColorConverter() { }

		public Object Convert(Object c, Type target_type, Object parameter, CultureInfo culture)
		{
			var _hsl = (Double[])parameter;
			var hsl = new HSL((Color)c);
			hsl.H *= _hsl[0];
			hsl.S *= _hsl[1];
			hsl.L *= _hsl[2];
			return hsl.ToRGB();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};
}
