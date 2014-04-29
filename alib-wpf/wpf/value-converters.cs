using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Markup;

namespace alib.Wpf
{
	using String = System.String;
	using Array = System.Array;

	public static partial class util
	{
		public static readonly BooleanToVisibilityConverter BooleanToVisibilityConverterInst;
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class TwoWayValueConverter<T1, T2, P> : IValueConverter
	{
		void ex(Type T, bool from)
		{
			var msg = String.Format("{0} cannot convert {1} type {2}",
						this.GetType().Name,
						from ? "from" : "to",
						T.Name);
			throw new InvalidOperationException();
		}

		public Object Convert(Object value, Type type, Object parameter, CultureInfo culture)
		{
			if (!(value is T1))
				ex(value.GetType(), true);

			if (!type.IsAssignableFrom(typeof(T2)))
				ex(type, false);

			return Convert((T1)value, ConvertParam(parameter));
		}

		public Object ConvertBack(Object value, Type type, Object parameter, CultureInfo culture)
		{
			if (!(value is T2))
				ex(value.GetType(), true);
			if (!type.IsAssignableFrom(typeof(T1)))
				ex(type, false);

			return Convert((T2)value, ConvertParam(parameter));
		}

		public abstract P ConvertParam(Object op);
		public abstract T2 Convert(T1 value, P parameter);
		public abstract T1 Convert(T2 value, P parameter);
	};
	public abstract class TwoWayValueConverter<T1, T2> : TwoWayValueConverter<T1, T2, Object>
	{
		public sealed override Object ConvertParam(Object op) { return op; }
	};

	public abstract class OneWayValueConverter<T1, T2, P> : TwoWayValueConverter<T1, T2, P>
	{
		public sealed override T1 Convert(T2 value, P parameter) { throw not.valid; }
	};
	public abstract class OneWayValueConverter<T1, T2> : OneWayValueConverter<T1, T2, Object>
	{
		public sealed override Object ConvertParam(Object op) { return op; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class IndexOfConverter : IValueConverter
	{
		public static readonly IndexOfConverter Instance;
		static IndexOfConverter() { Instance = new IndexOfConverter(); }
		IndexOfConverter() { }

		public Object Convert(Object seq, Type target_type, Object value, CultureInfo culture)
		{
			if (seq != null)
			{
				Array A;
				if ((A = seq as Array) != null)
					return Array.IndexOf(A, value);

				IList L;
				if ((L = seq as IList) != null)
					return L.IndexOf(value);

				IEnumerable E;
				if ((E = seq as IEnumerable) != null)
				{
					var e = E.GetEnumerator();
					for (int ix = 0; e.MoveNext(); ix++)
						if (e.Current.Equals(value))
							return ix;
					return -1;
				}
			}
			return value;
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class IsTypeOfConverter : IValueConverter
	{
		public static readonly IsTypeOfConverter Instance;
		static IsTypeOfConverter() { Instance = new IsTypeOfConverter(); }
		IsTypeOfConverter() { }

		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			if (value == null)
				return Visibility.Visible;
			return ((Type)parameter).IsAssignableFrom(value.GetType());
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class VisibileIsTypeConverter : IValueConverter
	{
		public static readonly VisibileIsTypeConverter Instance;
		static VisibileIsTypeConverter() { Instance = new VisibileIsTypeConverter(); }
		VisibileIsTypeConverter() { }

		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			if (value == null)
				return Visibility.Visible;
			return ((Type)parameter).IsAssignableFrom(value.GetType()) ? Visibility.Visible : Visibility.Collapsed;
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class CollapseIsTypeConverter : IValueConverter
	{
		public static readonly CollapseIsTypeConverter Instance;
		static CollapseIsTypeConverter() { Instance = new CollapseIsTypeConverter(); }
		CollapseIsTypeConverter() { }

		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			if (value == null)
				return Visibility.Visible;
			return ((Type)parameter).IsAssignableFrom(value.GetType()) ? Visibility.Collapsed : Visibility.Visible;
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class BooleanOrConverter : IMultiValueConverter
	{
		public static readonly BooleanOrConverter Instance;
		static BooleanOrConverter() { Instance = new BooleanOrConverter(); }
		BooleanOrConverter() { }

		public Object Convert(Object[] values, Type targetType, Object parameter, CultureInfo culture)
		{
			for (int i = 0; i < values.Length; i++)
				if ((Boolean)values[i])
					return true;
			return false;
		}

		public Object[] ConvertBack(Object value, Type[] targetTypes, Object parameter, CultureInfo culture)
		{
			throw new InvalidOperationException();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class SolidBrushConverter : IValueConverter
	{
		public static readonly IValueConverter Instance = new SolidBrushConverter();

		public Object Convert(Object param, Type targetType, Object value, CultureInfo culture)
		{
			Control ctrl;
			Shape sh;
			if ((ctrl = param as Control) != null)
				param = ctrl.Background as SolidColorBrush ?? ctrl.Foreground as SolidColorBrush;
			else if ((sh = param as Shape) != null)
				param = sh.Fill;

			SolidColorBrush br;
			if ((br = param as SolidColorBrush) != null)
				return br;

			Color c;
			String sc;

			if (param is Color)
				c = (Color)param;
			else if ((sc = param as String) != null)
				c = sc.ToColor();
			else if (param is HSL)
				c = ((HSL)param).ToRGB();
			else
				throw new InvalidOperationException();

			return SolidColorBrushCache.Get(c);
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[ContentProperty("ColorSource")]
	public class SolidColorBrushExtension : MarkupExtension
	{
		public SolidColorBrushExtension()
		{
		}
		public SolidColorBrushExtension(Object param)
		{
			this.param = param;
		}
		Object param;
		public Object ColorSource
		{
			get { return param; }
			set { param = value; }
		}
		public override Object ProvideValue(IServiceProvider sp)
		{
			if (param is MarkupExtension)
			{
				IProvideValueTarget pv;
				DependencyObject dobj;
				DependencyProperty dp;

				return (pv = sp as IProvideValueTarget) == null ||
					   (dobj = pv.TargetObject as DependencyObject) == null ||
					   (dp = pv.TargetProperty as DependencyProperty) == null ?
							null :
							dobj.GetValue((DependencyProperty)pv.TargetProperty);
			}

			Control ctrl;
			Shape sh;
			if ((ctrl = param as Control) != null)
				param = ctrl.Background as SolidColorBrush ?? ctrl.Foreground as SolidColorBrush;
			else if ((sh = param as Shape) != null)
				param = sh.Fill;

			var brush = param as SolidColorBrush;
			if (brush == null)
			{
				Color c;
				String sc;

				if (param is Color)
					c = (Color)param;
				if ((sc = param as String) != null)
					c = sc.ToColor();
				else if (param is HSL)
					c = ((HSL)param).ToRGB();
				else
				{
					return default(Brush);
					//throw new InvalidOperationException();
				}

				brush = new SolidColorBrush(c);
			}
			return brush;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// (not actually a converter)
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class BooleanSelector : DependencyObject
	{
		public static DependencyProperty IsSelectedProperty { get { return Selector.IsSelectedProperty; } }

		static BooleanSelector()
		{
			IsSelectedProperty.AddOwner(typeof(BooleanSelector));
		}

		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}
	};
}
