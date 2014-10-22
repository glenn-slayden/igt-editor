using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using alib.Debugging;
using alib.Reflection;

#pragma warning disable 618

namespace alib
{
	using SysString = System.String;

	static class _identity<T>
	{
		static _identity() { func = _func; }
		static T _func(T t) { return t; }

		public static readonly Func<T, T> func;
	};

	public sealed class ReferenceEquality<T> : IEqualityComparer<T>
		where T : class
	{
		public static readonly IEqualityComparer<T> Comparer;

		static ReferenceEquality() { Comparer = new ReferenceEquality<T>(); }

		ReferenceEquality() { }

		public bool Equals(T x, T y) { return (Object)x == (Object)y; }

		public int GetHashCode(T obj) { return RuntimeHelpers.GetHashCode(obj); }
	};

	public static class _misc_ext
	{
		[DebuggerHidden]
		public static int ObjectHashCode(this Object obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
#if false
		public static R Throw<E, R>(SysString fmt = null, params Object[] args)
			where E : Exception
		{
			if (fmt == null)
				fmt = "";
			else if (args != null && args.Length > 0)
				fmt = SysString.Format(fmt, args);
			throw typeof(E)._ctor<E>(new Object[] { fmt });
		}
#endif
		public static void Swap<T>(ref T a, ref T b)
		{
			T _tmp = b;
			b = a;
			a = _tmp;
		}

		public static T _<T>(this Exception ex) { throw ex; }
		public static T _<T>(this Exception ex, T _dummy) { throw ex; }
	};

	public sealed class TVoid { TVoid() { } };

	public sealed class MiscException : Exception
	{
		public MiscException(SysString fmt, params Object[] args)
			: base(SysString.Format(fmt, args))
		{
		}
	};

	public sealed class ConsoleBreakKeyException : Exception
	{
	}

	public class ArgumentTypeException : Exception
	{
	};

	public class NotInitializedException : Exception
	{
		public NotInitializedException(SysString fmt, params Object[] args)
			: base(SysString.Format(fmt, args))
		{
		}
	};

	public class ResultPendingException : Exception
	{
	};

	public class NotExpectedException : Exception
	{
		public NotExpectedException()
		{
		}
		public NotExpectedException(SysString fmt, params Object[] args)
			: base(SysString.Format(fmt, args))
		{
		}
	};

	public class NotTestedException : Exception
	{
		public NotTestedException()
		{
		}
		public NotTestedException(SysString fmt, params Object[] args)
			: base(SysString.Format(fmt, args))
		{
		}
	};

	public class DuplicateKeyException : ArgumentException
	{
		public DuplicateKeyException()
			: base("Duplicate dictionary key.")
		{
		}
	};

	public class _indent : IDisposable
	{
		public static int indent = 0;
		public _indent(SysString fmt, params Object[] args)
		{
			System.Diagnostics.Debug.Print(new SysString(' ', indent * 4) + fmt, args);
			indent++;
		}
		public void Dispose() { if (indent > 0) indent--; }
	}

	public static class must
	{
		static must()
		{
			reimplement = new InvalidOperationException("Derived class must re-implement the interface");
		}
		public static readonly InvalidOperationException reimplement;
	};
	public static class cannot
	{
		static cannot()
		{
			modify = new InvalidOperationException("Cannot modify this collection");
			happen = new NotExpectedException("Should be impossible");
		}
		public static readonly InvalidOperationException modify;
		public static readonly NotExpectedException happen;
	};
	public static class bad
	{
		static bad()
		{
			index = new IndexOutOfRangeException();
			arg = new ArgumentOutOfRangeException();
		}
		public static readonly IndexOutOfRangeException index;
		public static readonly ArgumentOutOfRangeException arg;
	};
	public static class not
	{
		static not()
		{
			impl = new NotImplementedException();
			valid = new InvalidOperationException();
			expected = new NotExpectedException();
			tested = new NotTestedException();
			threadsafe = new InvalidOperationException();
			found = new KeyNotFoundException();
		}
		public static readonly NotImplementedException impl;
		public static readonly InvalidOperationException valid;
		public static readonly NotExpectedException expected;
		public static readonly NotTestedException tested;
		public static readonly InvalidOperationException threadsafe;
		public static readonly KeyNotFoundException found;
	};
	public static class fix
	{
		static fix()
		{
			me = new Exception("fix me");
		}
		public static readonly Exception me;
	};
	public static class dont
	{
		static dont()
		{
			use = not.valid;
		}
		public static readonly InvalidOperationException use;
	};

	public static class no
	{
		public static void Action() { }
		public static void Action<T>(T t) { }
	};

	public static class Upcast
	{
		/// <summary>
		/// Clone all fields from an instance of base class TSrc into derived class TDst
		/// </summary>
		public static void Clone<TSrc, TDst>(TSrc source, TDst target)
			where TDst : TSrc
		{
			var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
			foreach (FieldInfo fis in typeof(TSrc).GetFields(bf))
				fis.SetValue(target, fis.GetValue(source));
		}

		/// <summary>
		/// Create a by-value copy of the shallow (non-nested) fields of 'source'. Naturally, 'source'
		/// is a reference type, since this is already the behavior of value types.
		/// Create a new instance of a derived class, cloning all fields from type TSrc
		/// </summary>
		public static TDst Clone<TSrc, TDst>(this TSrc source)
			where TDst : TSrc, new()
		{
			TDst target = new TDst();
			Clone(source, target);
			return target;
		}

		/// <summary>
		/// Clone all fields from an instance of source object into object target
		/// </summary>
		public static void Clone<TSrc>(TSrc source, TSrc target)
		{
			var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
			foreach (FieldInfo fis in typeof(TSrc).GetFields(bf))
				fis.SetValue(target, fis.GetValue(source));
		}
	};

	public static class NearestConsoleColor
	{
#if false
		static NearestConsoleColor()
		{
			var a_system_drawing = Assembly.LoadWithPartialName("System.Drawing");
			var Tc = a_system_drawing.GetType("System.Drawing.Color");
			var mi = Tc.GetMethod("FromName", (BindingFlags)0x18);
			var mi2 = Tc.GetMethod("get_Value", (BindingFlags)0x24);
			System.Array rg = System.Enum.GetValues(typeof(ConsoleColor));
			rgccinf = new ce[rg.Length];
			int i = 0;
			foreach (ConsoleColor cc in rg)
			{
				var n = System.Enum.GetName(typeof(ConsoleColor), cc);
				var c = mi.Invoke(null, new Object[] { (n == "DarkYellow" ? "Orange" : n) });
				rgccinf[i++] = new ce(cc, (int)(long)mi2.Invoke(c, new Object[] { }));
			}
			foreach (var ci in rgccinf)
			{
				System.Diagnostics.Debug.Print(" new ce({0},0x{1:X2},0x{2:X2},0x{3:X2}), ", (int)ci.cc, ci.R, ci.G, ci.B);
			}
		}
#endif
		static ce[] rgccinf =
		{
			 new ce(0,0x00,0x00,0x00),
			 new ce(1,0x00,0x00,0x8B),
			 new ce(2,0x00,0x64,0x00),
			 new ce(3,0x00,0x8B,0x8B),
			 new ce(4,0x8B,0x00,0x00),
			 new ce(5,0x8B,0x00,0x8B),
			 new ce(6,0xFF,0xA5,0x00),
			 new ce(7,0x80,0x80,0x80),
			 new ce(8,0xA9,0xA9,0xA9),
			 new ce(9,0x00,0x00,0xFF),
			 new ce(10,0x00,0x80,0x00),
			 new ce(11,0x00,0xFF,0xFF),
			 new ce(12,0xFF,0x00,0x00),
			 new ce(13,0xFF,0x00,0xFF),
			 new ce(14,0xFF,0xFF,0x00),
			 new ce(15,0xFF,0xFF,0xFF),
		};

		struct ce
		{
#if false
			public ce(ConsoleColor cc, int rgb)
			{
				this.cc = cc;
				this.R = (byte)(rgb >> 16);
				this.G = (byte)(rgb >> 8);
				this.B = (byte)rgb;
			}
#endif
			public ce(int cc, byte r, byte g, byte b)
			{
				this.cc = (ConsoleColor)cc;
				this.R = r;
				this.G = g;
				this.B = b;
			}
			public ConsoleColor cc;
			public byte R;
			public byte G;
			public byte B;
		}

		public static ConsoleColor ClosestConsoleColor(byte r, byte g, byte b)
		{
			ConsoleColor ret = 0;
			Double delta = Double.MaxValue;
			foreach (var ci in rgccinf)
			{
				var t = System.Math.Pow(ci.R - r, 2.0) + System.Math.Pow(ci.G - g, 2.0) + System.Math.Pow(ci.B - b, 2.0);
				if (alib.Math.math.IsZero(t))
					return ci.cc;
				if (t < delta)
				{
					delta = t;
					ret = ci.cc;
				}
			}
			return ret;
		}
	}


	public static class Formatter
	{
		public static SysString ReplaceAngleBrackets(this SysString s)
		{
			Char[] rgch = null;
			for (int i = 0; i < s.Length; i++)
			{
				Char repl;
				if (s[i] == '<')
					repl = '〈';
				else if (s[i] == '>')	//‹›  〈〉
					repl = '〉';
				else
					continue;
				if (rgch == null)
					rgch = s.ToCharArray();
				rgch[i] = repl;
			}
			if (rgch != null)
				s = new SysString(rgch);
			return s;
		}


		public static IFormatProvider Null = new _null_format();
		class _null_format : IFormatProvider, ICustomFormatter
		{
			public object GetFormat(Type service)
			{
				return service == typeof(ICustomFormatter) ? this : null;
			}

			public SysString Format(SysString format, Object arg, IFormatProvider provider)
			{
				if (format.Length > 1 && format[0] == '/')
				{
					if (arg == null)
						return "";
					return format.Substring(1) + ": " + arg.ToString();
				}
				IFormattable formattable = arg as IFormattable;
				return formattable != null ? formattable.ToString(format, provider) : arg.ToString();
			}
		};
	};


	public static class Enum
	{
		public static T ParseEnum<T>(this SysString s) where T : struct
		{
			T t;
			return System.Enum.TryParse<T>(s, true, out t) ? t : default(T);
		}
	};
}
