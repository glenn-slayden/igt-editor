using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;

using alib.Character;
using alib.String;
using alib.Enumerable;
using alib.Reflection;

namespace alib.Wpf.pathref
{
	using String = System.String;
	using Array = System.Array;

	public class PathRefConverter : IValueConverter
	{
		public readonly static PathRefConverter Instance = new PathRefConverter();

		public Object Convert(Object obj, System.Type targetType, Object parameter, CultureInfo culture)
		{
			String s_field = parameter as String;
			if (s_field == null)
				return "Converter parameter must be a String which specifies the field or void method name.";

			return _pathref.ReflectOnPath(obj, s_field);

			//var fi = obj.GetType().GetField(s_field, (System.Reflection.BindingFlags)0x34);
			//if (fi == null)
			//	return String.Format("Could not find a field '{0}' in '{1}'", s_field, obj.GetType().Name);
			//return fi.GetValue(obj);
		}

		public object ConvertBack(Object value, System.Type targetType, Object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[UsableDuringInitialization(true)]
	public abstract class _pathref : MarkupExtension
	{
		static readonly Object UnsetValue;
		static _pathref() { UnsetValue = new Object(); }

		protected _pathref(Object item, String s_ref)
		{
			this.Arg0 = item;
			this.Arg1 = s_ref;
			this.result = UnsetValue;
		}
		protected _pathref(Object item)
		{
			this.Arg0 = item;
			this.result = UnsetValue;
		}
		protected _pathref()
		{
			this.result = UnsetValue;
		}

		Object Arg0 { get; set; }
		String Arg1 { get; set; }

		public Object Item
		{
			get { return this.Arg0; }
			set
			{
				if (value == Arg0)
					return;
				if (result != UnsetValue)
					throw new ArgumentException("Item");
				this.Arg0 = value;
			}
		}

		public String Path
		{
			get { return this.Arg1; }
			set
			{
				if (value == Arg1)
					return;
				if (result != UnsetValue)
					throw new ArgumentException("Path");
				this.Arg1 = value.Trim();
			}
		}

		protected Object result;
		public override Object ProvideValue(IServiceProvider sp)
		{
			Object cur;
			if ((cur = Arg0) == null)
				throw new ArgumentNullException("Item");

			if (result == UnsetValue)
			{
				cur = PathRef.ResolveMarkupExtension(cur, sp);

				if (String.IsNullOrEmpty(Arg1))
					return cur;

				var pth = PreparePath(this.Arg1);
				//if (this is PropertyRef)
				//{
				//	result = ReflectOnPath(cur, pth.Take(pth.Length-1).ToArray());
				//}
				//else
				{
					result = ReflectOnPath(cur, pth);
				}
			}
			return result;
		}

		static String[] PreparePath(String path)
		{
			return path.Replace("[", ".[").Split(Charset.dot, StringSplitOptions.RemoveEmptyEntries);
		}

		static Object ReflectOnPath(Object _cur, String[] walk)
		{
			Object cur = _cur;
			PropertyInfo pi;
			MethodInfo mi;
			FieldInfo fi;

			for (int i = 0; i < walk.Length; i++)
			{
				var step = walk[i];
				if (TryWalkList(ref cur, step))
					continue;

				var T = cur.GetType();

				Object _tmp;
				if ((pi = T.GetProperty(step)) != null)
				{
					if ((_tmp = pi.GetValue(cur, null)) != null)
					{
						cur = _tmp;
						continue;
					}
					if (i == walk.Length - 1)
						return pi.SetMethod.CreateDelegate(typeof(Action<>).MakeGenericType(pi.PropertyType), cur);
				}

				if ((fi = T.GetField(step, (System.Reflection.BindingFlags)0x34)) != null)
				{
					cur = fi.GetValue(cur);
					continue;
				}

				if ((mi = T.GetMethod(step)) != null)
				{
					if (mi.GetParameters().Length > 0)
						throw new Exception("cannot call a method from a markup path.");
					cur = mi.Invoke(cur, null);
				}
				else
					throw new Exception(String.Format("Invalid path '{0}' for object /{1}/",
													walk.StringJoin(" . "),
													T.ToString()));
			}
			return cur;
		}

		static bool TryWalkList(ref Object cur, String step)
		{
			int ix;
			String ss;
			if ((ss = step.ExtractFirstBracket()) == null)
				return cur is IList && step == "Items";

			if (!int.TryParse(ss, out ix))
			{
				var ns = cur as INameScope;
				if (ns != null)
					return cur != (cur = (ns.FindName(ss) ?? cur));

				return false;
			}

			System.Type tx;
			if (cur is Array)
				cur = ((Array)cur).GetValue(ix);
			else if (cur is IList)
				cur = ((IList)cur)[ix];
			else if ((tx = cur.GetType().FindGenericInterface(typeof(IList<>))) != null)
			{
				cur = typeof(IList<>)
					.MakeGenericType(tx.GetGenericArguments()[0])
					.GetMethod("get_Item", (BindingFlags)0x14)
					.Invoke(cur, new Object[] { ix });
			}
			else
				return false;

			return true;
		}

		public static Object ReflectOnPath(Object _cur, String path)
		{
			return ReflectOnPath(_cur, PreparePath(path));
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[ContentProperty("Item")]
	public sealed class PathRef : _pathref
	{
		public static Object ResolveMarkupExtension(Object cur, IServiceProvider sp = null)
		{
			MarkupExtension me;
			while ((me = cur as PathRef) != null)
				cur = me.ProvideValue(null);

			while ((me = cur as MarkupExtension) != null)
			{

				if (cur == (cur = me.ProvideValue(sp)))
					break;
			}
			return cur;
		}

		public PathRef(Object item, String s_ref)
			: base(item, s_ref)
		{
		}
		public PathRef(Object item)
			: base(item)
		{
		}
		public PathRef()
		{
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[ContentProperty("Target")]
	public sealed class PropertyRef : _pathref
	{
		public PropertyRef(Object item, String s_ref)
			: base(item, s_ref)
		{
		}
		public PropertyRef()
			: base()
		{
		}

		public override Object ProvideValue(IServiceProvider sp)
		{
			var o = base.ProvideValue(sp);

			MethodInfo mi;
			Delegate del;
			IEnumerable<Object> iev;

			if ((del = is_setter(o)) != null)
			{
				del.Method.Invoke(del.Target, new Object[] { target });
				return null;
			}

			if ((mi = o.GetType().GetMethod("AddRange")) != null && (iev = target as IEnumerable<Object>) != null)
			{
				mi.Invoke(o, new Object[] { iev });
				return null;
			}

			throw new Exception();
		}

		Delegate is_setter(Object _tmp)
		{
			MethodInfo mi;
			ParameterInfo[] rgpi;
			Delegate del;

			return (del = _tmp as Delegate) != null &&
					(rgpi = (mi = del.Method).GetParameters()) != null &&
					mi.ReturnType == typeof(void) &&
					rgpi.Length == 1 &&
					rgpi[0].ParameterType.IsAssignableFrom(target.GetType()) ?
					del : null;
		}

		Object target;
		public Object Target
		{
			get { return target ?? (target = base.ProvideValue(null)); }
			set { target = value; }
		}
	};
}