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

using alib.Enumerable;
using alib.Debugging;
using alib.Wpf;

namespace xigt2
{
	public class MinWidthIfBlank : IValueConverter
	{
		public static readonly IValueConverter Instance = new MinWidthIfBlank();

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return String.IsNullOrWhiteSpace(value as String) ? 60 : 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};


	public class IsReadOnlyToBrushConverter : IValueConverter
	{
		public static readonly IValueConverter Instance = new IsReadOnlyToBrushConverter();

		public object Convert(Object value, Type targetType, object param, System.Globalization.CultureInfo culture)
		{
			var is_read_only = (bool)value;

			Color c;
			String sc;

			if ((sc = param as String) != null)
				c = sc.ToColor();
			else if (param is Color)
				c = (Color)param;
			else if (param is HSL)
				c = ((HSL)param).ToRGB();
			else
				throw new InvalidOperationException();

			if (!is_read_only)
			{
				var hsl = new HSL(c);
				hsl.S *= .90;
				hsl.L *= 1.08;
				c = hsl.ToRGB();
			}
			return new SolidColorBrush(c);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};

}