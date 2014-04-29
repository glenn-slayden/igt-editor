using System;
using System.Diagnostics;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using alib;
using alib.String;

#if false
namespace alib
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class Anon
	{
		public static T New<T>(Action<T> a) where T : new()
		{
			T t = new T();
			a(t);
			return t;
		}
		public static void IfType<T>(Object o, Action<T> f) where T : class
		{
			T t;
			if ((t = o as T) != null)
				f(t);
		}
		public static void IfType<T>(Object o, Action<T> f_true, Action f_false) where T : class
		{
			T t;
			if ((t = o as T) != null)
				f_true(t);
			else
				f_false();
		}
		public static void NotNull<T>(T t, Action<T> a) where T : class
		{
			if (t != null)
				a(t);
		}
	};
}
#endif

namespace alib.Reflection
{
	using String = System.String;
	using SysType = System.Type;

	public static class _ext
	{
		public static IEnumerable<Object> PromoteAnonymous(this IEnumerable seq, String s_typename)
		{
			IEnumerator ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				yield break;
			Object o = ie.Current;

			var e = AnonymousTypePromoter.GetPromotionInfo(s_typename, o, null, null, null);
			yield return e.Promote(o);

			while (ie.MoveNext())
				yield return e.Promote(ie.Current);
		}
	}

	public class AnonymousTypePromoter : CodeCompileUnit
	{
		public struct TypePromotionInfo
		{
			internal TypePromotionInfo(Assembly asm, Type type)
			{
				this.asm = asm;
				this.type = type;
			}

			readonly Assembly asm;
			readonly Type type;

			public Assembly Assembly { get { return asm; } }
			public Type Type { get { return type; } }

			public Object Promote(Object a)
			{
				return asm.CreateInstance(type.Name, false,
					BindingFlags.Public | BindingFlags.Instance, null, new Object[] { a }, null, null);
			}
			public IEnumerable<Object> Promote(IEnumerable seq)
			{
				foreach (Object o in seq)
					yield return asm.CreateInstance(type.Name, false,
						BindingFlags.Public | BindingFlags.Instance, null, new Object[] { o }, null, null);
			}
		};

		static Dictionary<Type, TypePromotionInfo> dict = new Dictionary<Type, TypePromotionInfo>();

		CodeTypeDeclaration target_class;
		String[] rgra = null;

		/// <summary>
		/// Create a class based on the fields in the specified prototypical instance of an anonymous type.
		/// The returned structure indicates the created type and assembly, and provides a method for converting
		/// other instances of the anonymous type to the newly created type. The specified name for the newly
		/// created class type is only used if this anonymous type has not been converted before.
		/// </summary>
		public static TypePromotionInfo GetPromotionInfo(
							String s_typename,
							Object prototype,
							String[] usings,
							String[] referenced_assemblies,
							Type[] base_types)
		{
			TypePromotionInfo e;
			Type T = prototype.GetType();
			if (!dict.TryGetValue(T, out e))
			{
				var atp = new AnonymousTypePromoter(s_typename, prototype, usings, referenced_assemblies, base_types);
#if false
				Debug.WriteLine(atp.ToString());
#endif
				Assembly asm = atp.Compile();
				e = new TypePromotionInfo(asm, asm.GetType(s_typename, true, false));
				dict.Add(T, e);
			}
			return e;
		}

		AnonymousTypePromoter(String s_typename, Object prototype, String[] usings, String[] rgra, Type[] base_types)
		{
			this.rgra = rgra;
			CodeNamespace _namespace = new CodeNamespace();
			_namespace.Imports.Add(new CodeNamespaceImport("System"));
			if (usings != null)
				foreach (String u in usings)
					_namespace.Imports.Add(new CodeNamespaceImport(u));
			target_class = new CodeTypeDeclaration(s_typename);
			target_class.IsClass = true;
			target_class.TypeAttributes = TypeAttributes.Public;
			if (base_types != null)
				foreach (Type bt in base_types)
					target_class.BaseTypes.Add(new CodeTypeReference(bt));
			_namespace.Types.Add(target_class);
			this.Namespaces.Add(_namespace);

			AddConstructor(prototype);
		}

		void AddFieldAndAccessors(String s_field, String s_prop, Type t)
		{
			CodeMemberField fld = new CodeMemberField();
			fld.Attributes = MemberAttributes.Private;
			fld.Name = s_field;
			fld.Type = new CodeTypeReference(t);
			target_class.Members.Add(fld);

			CodeMemberProperty prop = new CodeMemberProperty();
			prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			prop.Name = s_prop;
			prop.HasGet = true;
			prop.HasSet = true;
			prop.Type = new CodeTypeReference(t);
			prop.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(
										new CodeThisReferenceExpression(), s_field)));
			prop.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(
										new CodeThisReferenceExpression(), s_field),
										new CodeArgumentReferenceExpression("value")));
			target_class.Members.Add(prop);
		}

		void AddConstructor(Object prototype)
		{
			CodeConstructor constructor = new CodeConstructor();
			constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			target_class.Members.Add(constructor);
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Object), "obj"));
			CodeArgumentReferenceExpression o_arg = new CodeArgumentReferenceExpression("obj");

			constructor.Statements.Add(new CodeVariableDeclarationStatement
			{
				Type = new CodeTypeReference(typeof(FieldInfo[])),
				Name = "rgfi",
				InitExpression = new CodeMethodInvokeExpression
				{
					Method = new CodeMethodReferenceExpression(new CodeMethodInvokeExpression
					{
						Method = new CodeMethodReferenceExpression(o_arg, "GetType")
					}, "GetFields"),
					Parameters = 
					{
						new CodeBinaryOperatorExpression(
						new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "NonPublic"),
						CodeBinaryOperatorType.BitwiseOr,
						new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "Instance"))
					}
				}
			});

			CodeVariableReferenceExpression o_rgfi = new CodeVariableReferenceExpression("rgfi");

			int q = 0;
			foreach (var mi in prototype.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
			{
				String s_prop = mi.Name.ExtractAngleTagged();
				if (String.IsNullOrEmpty(s_prop))
					throw new Exception();
				String s_field = "__" + s_prop;
				Type T_prop = mi.FieldType;

				CodeFieldReferenceExpression fld_target = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), s_field);

				CodeCastExpression fld_source = new CodeCastExpression(new CodeTypeReference(T_prop),
					new CodeMethodInvokeExpression(new CodeMethodReferenceExpression
					{
						TargetObject = new CodeArrayIndexerExpression
						{
							Indices = { new CodePrimitiveExpression(q) },
							TargetObject = o_rgfi,
						},
						MethodName = "GetValue",
					},o_arg));

				constructor.Statements.Add(new CodeAssignStatement(fld_target, fld_source));

				AddFieldAndAccessors(s_field, s_prop, T_prop);
				q++;
			}
		}

		CodeDomProvider Provider
		{
			get { return CodeDomProvider.CreateProvider("csharp", new Dictionary<String, String> { { "CompilerVersion", "v4.0" } }); }
		}

		public override String ToString()
		{
			CodeGeneratorOptions opt = new CodeGeneratorOptions();
			opt.BracingStyle = "C";
			using (StringWriter sw = new StringWriter())
			{
				Provider.GenerateCodeFromCompileUnit(this, sw, opt);
				return sw.ToString();
			}
		}

		public Assembly Compile()
		{
			CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
			var cp = new CompilerParameters();
			cp.GenerateInMemory = true;
			if (rgra != null)
				foreach (String ra in rgra)
					cp.ReferencedAssemblies.Add(ra);
			var cr = Provider.CompileAssemblyFromDom(cp, new CodeCompileUnit[] { this });
#if false
			if (cr.Errors.Count > 0)
				Debugger.Break();
#endif
			return cr.CompiledAssembly;
		}
	};
}
