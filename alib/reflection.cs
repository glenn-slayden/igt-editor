using System;
using System.Globalization;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using alib;

namespace alib.Reflection
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class assembly_stats
	{
		public assembly_stats(Assembly asm, bool f_debug)
		{
			this.asm = asm;

			this.s_build_type = f_debug ? "debug" : "release";

			this.assigned_index = -1;

			this.s_name = new AssemblyName(FullName).Name;

			this.dt_build_time = alib.file._file_ext.BuildTime(asm);

			this.s_build_date = String.Format("{0:yyyyMMdd\\-HHmmss}", dt_build_time);

			this.s_info = String.Format("{0} ({1}) {2}", Name, s_build_type, s_build_date);
		}

		readonly public Assembly asm;
		public String FullName { get { return asm.FullName; } }

		readonly String s_build_type;
		public String BuildType { get { return s_build_type; } }

		readonly public String s_name;
		public String Name { get { return s_name; } }

		readonly DateTime dt_build_time;
		public DateTime BuildTime { get { return dt_build_time; } }

		readonly String s_build_date;
		public String BuildDate { get { return s_build_date; } }

		readonly String s_info;
		public String VersionInfo { get { return s_info; } }

		public Uri Uri { get { return new Uri(asm.CodeBase); } }

		public int TypesCount { get; set; }
		public int PropsCount { get; set; }
		public bool f_extension;
		public int assigned_index;
		public Type attached_property_set;
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public struct mini_invoker<U>
	{
		public mini_invoker(MethodInfo mi, Object target)
		{
			this.mi = mi;
			this.target = target;
		}
		public mini_invoker(Delegate del)
			: this(del.Method, del.Target)
		{
		}
		readonly MethodInfo mi;
		readonly Object target;
		public U Invoke(Object obj) { return (U)mi.Invoke(target, 0, null, new[] { obj }, null); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class TypeTuples<T, U>
	{
		public static readonly Type[] Targs;
		static TypeTuples() { Targs = new[] { typeof(T), typeof(U) }; }
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static unsafe class reflection
	{
		public static readonly String[] abG, abD, ab_auto;

		static reflection()
		{
			abG = new[] { "〈", "〉" };
			abD = new[] { "<", ">" };
			ab_auto = alib.Debugging._dbg_util.IsGuiApplication ? abG : abD;
		}

		[DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode, MethodImpl(MethodImplOptions.NoInlining)]
		public static Type CallingType(this StackTrace st)
		{
			return st.GetFrame(1).GetMethod().ReflectedType;
		}

		static MethodInfo _mi_fullname;
		public static String FullName(this MethodBase mb)
		{
			if (_mi_fullname == null)
				_mi_fullname = typeof(MethodBase).GetMethod("get_FullName", (BindingFlags)0x24);
			return (String)_mi_fullname.Invoke(mb, System.Type.EmptyTypes);
		}

		public static String SimpleName(this Assembly a)
		{
			return new AssemblyName(a.FullName).Name;
		}

		[DebuggerStepThrough]
		public static String StripName(this Type t)
		{
			var s = t.Name;
			int ix = s.IndexOf('`');
			if (ix != -1)
				s = s.Remove(ix);
			if (s[s.Length - 1] == '&')
				s = s.Remove(s.Length - 1);
			return s;
		}

		public static bool HasCustomAttribute<T>(this Assembly a)
			where T : Attribute
		{
			return HasCustomAttribute<T>(a.GetCustomAttributesData());
		}
		public static bool HasCustomAttribute<T>(this MemberInfo mi)
			where T : Attribute
		{
			return HasCustomAttribute<T>(mi.GetCustomAttributesData());
		}
		public static CustomAttributeData GetCustomAttributeData<T>(this MemberInfo mi)
			where T : Attribute
		{
			return GetCustomAttributeData<T>(mi.GetCustomAttributesData());
		}

		static bool HasCustomAttribute<T>(this IList<CustomAttributeData> rgcad)
		{
			int c = rgcad.Count;
			for (int i = 0; i < c; i++)
				if (typeof(T).IsAssignableFrom(rgcad[i].AttributeType))
					return true;
			return false;
		}
		static CustomAttributeData GetCustomAttributeData<T>(this IList<CustomAttributeData> rgcad)
		{
			int c = rgcad.Count;
			CustomAttributeData cad;
			for (int i = 0; i < c; i++)
				if (typeof(T).IsAssignableFrom((cad = rgcad[i]).AttributeType))
					return cad;
			return null;
		}

		[DebuggerStepThrough]
		public static String _Name(this Type t, bool f_space_generic_args = true)
		{
			return _Name(t, ab_auto, f_space_generic_args ? ", " : ",");
		}

		[DebuggerStepThrough]
		public static String _Name(this Type t, String[] ab, String gen_sep)
		{
			if (t == null)
				return "";
			if (t == typeof(void))
				return "void";
			if (t.IsPrimitive)
			{
				if (t == typeof(Int32))
					return "int";
				if (t == typeof(Int64))
					return "long";
				if (t == typeof(UInt32))
					return "uint";
				if (t == typeof(UInt64))
					return "ulong";
				if (t == typeof(Boolean))
					return "bool";
			}
			String s = StripName(t);
			if (t.IsGenericType)
				s += ab[0] + String.Join(gen_sep, System.Linq.Enumerable.Select(t.GetGenericArguments(), x => _Name(x, ab, gen_sep))) + ab[1];
			if (!t.IsGenericParameter && t.IsNested)
				s = StripName(t.DeclaringType) + "." + s;
			return s;
		}

		[DebuggerStepThrough]
		public static String _NameD(this Type t)
		{
			return _Name(t, abD, ",");
		}

		public static MethodInfo GetFirstMethod(this Object obj, String s_method, BindingFlags bf = (BindingFlags)0x16, Type[] rgt = null)
		{
			bf |= BindingFlags.DeclaredOnly;
			Type t = obj.GetType();
			while (true)
			{
				MethodInfo mi;
				if (rgt == null)
					mi = t.GetMethod(s_method, bf);
				else
					mi = t.GetMethod(s_method, bf, null, rgt, null);
				if (mi != null)
					return mi;
				if (t == typeof(Object))
					return null;
				t = t.BaseType;
			}
		}

		public static Type[] MakeTypesArray(Object[] args)
		{
			var rgt = args.Length == 0 ? Type.EmptyTypes : new Type[args.Length];
			for (int i = 0; i < rgt.Length; i++)
				rgt[i] = args[i].GetType();
			return rgt;
		}

		public static bool CheckAssignable(Type[] rgt_base, Type[] rgt_derived)
		{
			if (rgt_base.Length != rgt_derived.Length)
				return false;
			for (int i = 0; i < rgt_base.Length; i++)
				if (!rgt_base[i].IsAssignableFrom(rgt_derived[i]))
					return false;
			return true;
		}

		public static bool CheckAssignable(this ParameterInfo[] rgpi, Type[] rgt)
		{
			if (rgpi.Length != rgt.Length)
				return false;
			for (int i = 0; i < rgpi.Length; i++)
				if (!rgpi[i].ParameterType.IsAssignableFrom(rgt[i]))
					return false;
			return true;
		}

		public static bool CheckAssignable(this MethodBase mi, Type[] rgt)
		{
			return CheckAssignable(mi.GetParameters(), rgt);
		}

		public static Type GetParameterType(this MethodBase m, int ix)
		{
			var px = m.GetParameters();
			return ix < px.Length ? px[ix].ParameterType : null;
		}

		public static bool TypeHasDefaultConstructor(this Type t)
		{
			return t.IsPrimitive ||
				t.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, new Type[0], null) != null;
		}

		/// http://msdn.microsoft.com/en-us/library/system.reflection.methodbase.isfinal.aspx
		public static bool CanOverride(this MethodInfo mi)
		{
			return mi != null && mi.IsVirtual && !mi.IsFinal;
		}

		public static bool OverridesMethod<TBase>(this TBase instance, String s_method)
			where TBase : class
		{
			var TDerived = instance.GetType();
			return TDerived == typeof(TBase) || TDerived.GetMethod(s_method, (BindingFlags)0x34).DeclaringType != typeof(TBase);
		}

		public static bool IsGenericInterface(this Type t, Type Topen, Type arg)
		{
			return Topen.MakeGenericType(arg).IsAssignableFrom(t);
		}

		public static Type FindCompatibleInterface(this Type t, Type Topen, Type arg)
		{
			var ti = Topen.MakeGenericType(arg);
			return ti.IsAssignableFrom(t) ? ti : null;
		}

		public static bool IsGenericOf(this Type t, Type Topen)
		{
			Debug.Assert(!t.IsGenericTypeDefinition);
			Type x;
			return t.IsGenericType && (t == (x = t.GetGenericTypeDefinition()) || Topen.IsAssignableFrom(x));
		}

		/// <summary> Warning: randomly selects one of the matching interfaces if there are multiple </summary>
		[DebuggerStepThrough]
		public static Type FindGenericInterface(this Type t, Type Topen)
		{
			//if (!Topen.IsGenericTypeDefinition)
			//    Topen = Topen.GetGenericTypeDefinition();
			Debug.Assert(Topen.IsGenericTypeDefinition);
			String sto;
			if (t.Name == (sto = Topen.Name))
				return t;
			var arr = t.GetInterfaces();
			for (int i = 0; i < arr.Length; i++)
				if (arr[i].Name == sto)
					return arr[i];
			return null;
		}

		public static Type FindGenericBase(this Type t, Type Topen)
		{
			Debug.Assert(Topen.IsGenericTypeDefinition);
			do
				if (t.IsGenericType && t.GetGenericTypeDefinition() == Topen)
					return t;
			while ((t = t.BaseType) != null);
			return null;
		}

		public static bool HasGenericBase(this Type t, Type Topen)
		{
			Debug.Assert(Topen.IsGenericTypeDefinition);
			do
				if (t.IsGenericType && t.GetGenericTypeDefinition() == Topen)
					return true;
			while ((t = t.BaseType) != null);
			return false;
		}

		[DebuggerStepThrough]
		public static MethodInfo FindGenericMethod(this Type t, String name, Type[] type_params)
		{
			var rgt = t.GetMethods();
			for (int i = 0; i < rgt.Length; i++)
			{
				Type[] ga;
				var mi = rgt[i];
				if (mi.Name == name &&
					mi.IsGenericMethodDefinition &&
					(ga = mi.GetGenericArguments()).Length == type_params.Length &&
					(mi = mi.MakeGenericMethod(type_params)) != null)
					return mi;
			}
			return null;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static Type FindElementType(this IEnumerable seq)
		{
			Type Tseq = seq.GetType();
			Type Telem = null;
			IEnumerator en;
			Type[] rgT;

			if (Tseq.IsGenericType && (rgT = Tseq.GetGenericArguments()).Length > 0)
				Telem = rgT[0];

			if (Telem == null && (en = seq.GetEnumerator()).MoveNext() && en.Current != null)
				Telem = en.Current.GetType();

			return Telem;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static T _ctor<T>(this Type t, params Object[] args)
		{
#if DEBUG
			if (args.Length == 0)
				throw new Exception("Function not intended for use with parameterless constructor.");

			if (t.IsAbstract)
				throw new Exception(String.Format("Cannot construct an instance of abstract type or interface '{0}'.", t._Name()));

			if (!typeof(T).IsAssignableFrom(t))
				throw new ArgumentException(String.Format("Incorrect generic function usage: '{0}' would not be convertible to '{1}'", t._Name(), typeof(T)._Name()));
#endif
			ConstructorInfo ci;
			int i, c;
			var rgci = t.GetConstructors();
			if ((c = rgci.Length) == 1)
			{
				ci = rgci[0];
				goto invoke;
			}
			ci = null;
			for (i = 0; i < c; i++)
				if (rgci[i].GetParameters().Length == args.Length)
				{
					if (ci != null)
						goto full;
					ci = rgci[i];
				}
				else
					rgci[i] = null;
			goto invoke;

		full:
			var rgt = MakeTypesArray(args);
			for (i = 0; i < c; i++)
				if ((ci = rgci[i]) != null && CheckAssignable(ci, rgt))
					goto invoke;

#if !DEBUG
			throw new Exception();
#else
			var msg = String.Format("No constructor on type '{0}' takes {1} argument(s) which are compatible with types '{2}'.",
				t._Name(),
				args.Length,
				Enumerable.enum_ext.StringJoin(System.Linq.Enumerable.Select(rgt, x => alib.String._string_ext.SQRB(x._Name())), ", "));
			throw new Exception(msg);
#endif
		invoke:
			return (T)ci.Invoke(args);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static bool FilterApplyPrefixLookup(MemberInfo memberInfo, String name, bool ignoreCase)
		{
			if (ignoreCase)
			{
				if (!memberInfo.Name.ToLower(CultureInfo.InvariantCulture).StartsWith(name, StringComparison.Ordinal))
					return false;
			}
			else if (!memberInfo.Name.StartsWith(name, StringComparison.Ordinal))
			{
				return false;
			}
			return true;
		}
		static bool FilterApplyBase(MemberInfo memberInfo, BindingFlags bf, bool isPublic, bool isNonProtectedInternal, bool isStatic, string name, bool prefixLookup)
		{
			if (isPublic)
			{
				if ((bf & BindingFlags.Public) == BindingFlags.Default)
					return false;
			}
			else if ((bf & BindingFlags.NonPublic) == BindingFlags.Default)
			{
				return false;
			}

			bool flag = !object.ReferenceEquals(memberInfo.DeclaringType, memberInfo.ReflectedType);
			if (((bf & BindingFlags.DeclaredOnly) != BindingFlags.Default) && flag)
				return false;

			if ((memberInfo.MemberType != MemberTypes.TypeInfo) && (memberInfo.MemberType != MemberTypes.NestedType))
			{
				if (isStatic)
				{
					if (((bf & BindingFlags.FlattenHierarchy) == BindingFlags.Default) && flag)
						return false;
					if ((bf & BindingFlags.Static) == BindingFlags.Default)
						return false;
				}
				else if ((bf & BindingFlags.Instance) == BindingFlags.Default)
				{
					return false;
				}
			}
			if (prefixLookup && !FilterApplyPrefixLookup(memberInfo, name, (bf & BindingFlags.IgnoreCase) != BindingFlags.Default))
				return false;

			if (((((bf & BindingFlags.DeclaredOnly) == BindingFlags.Default) && flag) && (isNonProtectedInternal && ((bf & BindingFlags.NonPublic) != BindingFlags.Default))) && (!isStatic && ((bf & BindingFlags.Instance) != BindingFlags.Default)))
			{
				MethodInfo info = memberInfo as MethodInfo;
				if (info == null)
					return false;
				if (!info.IsVirtual && !info.IsAbstract)
					return false;
			}
			return true;
		}

		public static Type FilterApplyType(this Type type, BindingFlags bf, String name, bool prefixLookup, String ns)
		{
			bool isPublic = type.IsNestedPublic || type.IsPublic;
			bool isStatic = false;
			if (!FilterApplyBase(type, bf, isPublic, type.IsNestedAssembly, isStatic, name, prefixLookup))
				return null;

			if ((ns != null) && !type.Namespace.Equals(ns))
				return null;

			return type;
		}
	};
}
