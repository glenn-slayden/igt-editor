using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Interop;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Threading;
using System.Reflection;

using alib;
using alib.Enumerable;

namespace alib.Wpf
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static partial class util
	{
		public static void BeginInvoke<T>(this Dispatcher d, Action<T> a, T t)
		{
			var h = new _h<T> { a = a, t = t };
			d.BeginInvoke((Action)h.go, null);
		}
		struct _h<T>
		{
			public T t;
			public Action<T> a;
			public void go() { a(t); }
		};

		public static IList<DependencyProperty> GetAttachedProperties(DependencyObject obj)
		{
			var result = new List<DependencyProperty>();
			foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) }))
			{
				var dpd = DependencyPropertyDescriptor.FromProperty(pd);
				if (dpd != null)
					result.Add(dpd.DependencyProperty);
			}
			return result;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// http://thejoyofcode.com/Generating_images_using_WPF_on_the_Server.aspx
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class BackgroundStaDispatcher
	{
		private Dispatcher _dispatcher;
		static int id = 0;

		public BackgroundStaDispatcher(Action<Exception> exception_handler)
		{
			AutoResetEvent are = new AutoResetEvent(false);

			Thread thread = new Thread(() =>
			{
				_dispatcher = Dispatcher.CurrentDispatcher;
				_dispatcher.UnhandledException += (o, e) =>
				{
					exception_handler(e.Exception);
					if (!Debugger.IsAttached)
					{
						e.Handled = true;
					}
				};
				are.Set();
				Dispatcher.Run();
			});

			thread.Name = string.Format("BackgroundStaDispatcher({0})", (id++).ToString());
			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();

			are.WaitOne();
		}

		public void Invoke(Action action)
		{
			_dispatcher.Invoke(action);
		}
	};
}
