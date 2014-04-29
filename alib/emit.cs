using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using alib;
using alib.String;

namespace alib.Reflection.emit
{
	using String = System.String;
	using SysType = System.Type;

	public struct Retrolabel
	{
		public Label l;
		public int il_offset;
		public Retrolabel(ILGenerator il)
		{
			l = il.DefineLabel();
			il_offset = il.ILOffset;
			il.MarkLabel(l);
		}
	};

	struct __FixupData
	{
		public int m_fixupLabel;
		public int m_fixupPos;
		public int m_fixupInstSize;
	};

	[DebuggerDisplay("target:{fud.m_fixupPos}  delta:{delta8}  size:{fud.m_fixupInstSize}  {(CanShorten?\"CanShorten\":\"\"),nq}")]
	struct _fixup_resolve
	{
		public __FixupData fud;
		public int[] rg_lab;
		public int delta8 { get { return rg_lab[fud.m_fixupLabel] - (fud.m_fixupPos + sizeof(sbyte)); } }
		public int delta32 { get { return rg_lab[fud.m_fixupLabel] - (fud.m_fixupPos + sizeof(int)); } }
		public bool CanShorten { get { return fud.m_fixupInstSize == 4 && delta8 == (sbyte)delta8; } }
	};

	public static unsafe class _ext
	{
		const BindingFlags bf = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		public static Retrolabel MarkRetrolabel(this ILGenerator il)
		{
			return new Retrolabel(il);
		}

		public static void Emit(this ILGenerator il, OpCode op, Retrolabel retro)
		{
			if (il.ILOffset < retro.il_offset)
				throw not.valid;
			bool f_s = (il.ILOffset + 7) - retro.il_offset <= 127;
			if (op == OpCodes.Br || op == OpCodes.Br_S)
				il.Emit(f_s ? OpCodes.Br_S : OpCodes.Br, retro.l);
			else if (op == OpCodes.Brtrue || op == OpCodes.Brtrue_S)
				il.Emit(f_s ? OpCodes.Brtrue_S : OpCodes.Brtrue, retro.l);
			else if (op == OpCodes.Brfalse || op == OpCodes.Brfalse_S)
				il.Emit(f_s ? OpCodes.Brfalse_S : OpCodes.Brfalse, retro.l);
			else
				throw new NotImplementedException();
		}

		public static byte[] AdjustShortLabels(this ILGenerator il, int[] relocs, int c_relocs)
		{
			int cb = (int)typeof(ILGenerator).GetField("m_length", bf).GetValue(il);
			byte[] rgb = (byte[])typeof(ILGenerator).GetField("m_ILStream", bf).GetValue(il);

			fixed (byte* pb = rgb)
			{
				int[] rg_lab = (int[])typeof(ILGenerator).GetField("m_labelList", bf).GetValue(il);
				int c_lab = (int)typeof(ILGenerator).GetField("m_labelCount", bf).GetValue(il);

				_fixup_resolve[] fur;
				{
					int c_fixup = (int)typeof(ILGenerator).GetField("m_fixupCount", bf).GetValue(il);
					GCHandle gch = GCHandle.Alloc(typeof(ILGenerator).GetField("m_fixupData", bf).GetValue(il), GCHandleType.Pinned);
					__FixupData* p_fixup = (__FixupData*)gch.AddrOfPinnedObject();
					fur = new _fixup_resolve[c_fixup];
					for (int i = 0; i < c_fixup; i++)
					{
						fur[i] = new _fixup_resolve { fud = *p_fixup, rg_lab = rg_lab };
						p_fixup++;
					}
					gch.Free();
				}

				bool f_any;
				do
				{
					f_any = false;
					for (int i = 0; i < fur.Length; i++)
					{
						if (fur[i].CanShorten)
						{
							int ip = fur[i].fud.m_fixupPos;
							byte* psb = pb + ip;
							byte* p_op = psb - 1;
							if (*p_op != OpCodes.Brfalse.Value)
								throw new Exception();
							*p_op = (byte)OpCodes.Brfalse_S.Value;

							fur[i].fud.m_fixupInstSize = 1;

							cb -= 3;
							alib.Memory.Kernel32.MoveMemory(psb, psb + 3, cb - ip);

							for (int k = 0; k < fur.Length; k++)
								if (fur[k].fud.m_fixupPos > ip)
									fur[k].fud.m_fixupPos -= 3;

							for (int k = 0; k < c_lab; k++)
								if (rg_lab[k] > ip)
									rg_lab[k] -= 3;

							if (relocs != null)
								for (int k = 0; k < c_relocs; k++)
									if (relocs[k] > ip)
										relocs[k] -= 3;

							f_any = true;
						}
					}
				}
				while (f_any);

				for (int i = 0; i < fur.Length; i++)
				{
					_fixup_resolve fr = fur[i];
					if (fr.fud.m_fixupInstSize == 1)
						*(sbyte*)(pb + fr.fud.m_fixupPos) = (sbyte)fr.delta8;
					else if (fr.fud.m_fixupInstSize == 4)
						*(int*)(pb + fr.fud.m_fixupPos) = fr.delta32;
					else
						throw new Exception();
				}

				if (cb < rgb.Length)
				{
					rgb = new byte[cb];
					fixed (byte* pbnew = rgb)
						alib.Memory.Kernel32.CopyMemory(pbnew, pb, cb);
				}
			}
			return rgb;
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void _dump_dynamic_emit(Func<TypeBuilder, ILGenerator> amb)
		{
			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("foo22"), AssemblyBuilderAccess.Save);
			var mb = ab.DefineDynamicModule("foo22", "foo22.dll");

			var tb = mb.DefineType("xyz", TypeAttributes.Public);

			var il = amb(tb);

			int[] relocs = (int[])typeof(ILGenerator).GetField("m_RelocFixupList", bf).GetValue(il);
			int c_relocs = (int)typeof(ILGenerator).GetField("m_RelocFixupCount", bf).GetValue(il);
			byte[] patch = AdjustShortLabels(il, relocs, c_relocs);

			typeof(ILGenerator).GetField("m_ILStream", bf).SetValue(il, patch);
			typeof(ILGenerator).GetField("m_length", bf).SetValue(il, patch.Length);
			typeof(ILGenerator).GetField("m_labelCount", bf).SetValue(il, 0);
			typeof(ILGenerator).GetField("m_fixupCount", bf).SetValue(il, 0);

			SysType t = tb.CreateType();

			ab.Save("foo22.dll");
		}

		/// <summary>
		/// Checks for duplicate signature tokens when building DynamicMethod with DynamicILGenerator
		/// </summary>
		public static void EmitCalli(ILGenerator il, CallingConventions callingConvention, SysType returnType, SysType[] parameterTypes, SysType[] optionalParameterTypes)
		{
			int stackchange = 0;
			if ((optionalParameterTypes != null) && ((callingConvention & CallingConventions.VarArgs) == 0))
				throw new Exception();

			bool f_dyn = il.GetType().Name == "DynamicILGenerator";
			ModuleBuilder module = null;
			if (!f_dyn)
			{
				module = (ModuleBuilder)((MethodInfo)il.GetType().GetField("m_methodBuilder", bf).GetValue(il)).Module;
			}

			SignatureHelper helper = (SignatureHelper)il.GetType().GetMethod(
						"GetMemberRefSignature",
						bf,
						null,
						new[] { typeof(CallingConventions), typeof(SysType), typeof(SysType).MakeArrayType(), typeof(SysType).MakeArrayType() },
						null)
				.Invoke(il, new Object[] { callingConvention, returnType, parameterTypes, optionalParameterTypes });

			il.GetType().GetMethod("EnsureCapacity", bf).Invoke(il, new Object[] { 7 });
			il.Emit(OpCodes.Calli);
			if (returnType != typeof(void))
				stackchange++;
			if (parameterTypes != null)
				stackchange -= parameterTypes.Length;
			if (optionalParameterTypes != null)
				stackchange -= optionalParameterTypes.Length;
			if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
				stackchange--;
			stackchange--;
			il.GetType().GetMethod("UpdateStackSize", bf).Invoke(il, new Object[] { OpCodes.Calli, stackchange });

			int sig_tok;
			if (f_dyn)
			{
				byte[] sig = (byte[])helper.GetType().GetMethod("GetSignature", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(helper, new Object[] { true });

				Object o_scope = il.GetType().GetField("m_scope", bf).GetValue(il);
				List<Object> rgtok = (List<Object>)o_scope.GetType().GetField("m_tokens", bf).GetValue(o_scope);
				for (int i = 0; i < rgtok.Count; i++)
				{
					var tok = rgtok[i] as byte[];
					if (tok != null && tok.Length == sig.Length && alib.Memory.ByteArrayEqualityComparer.Default.Equals(sig, tok))
					{
						sig_tok = 0x11000000 | i;
						goto ok;
					}
				}
				sig_tok = (int)il.GetType().GetMethod("AddSignature", bf).Invoke(il, new Object[] { sig });
			ok: ;
			}
			else
			{
				il.GetType().GetMethod("RecordTokenFixup", bf).Invoke(il, null);
				sig_tok = module.GetSignatureToken(helper).Token;
			}
			il.GetType().GetMethod("PutInteger4", bf).Invoke(il, new Object[] { sig_tok });
		}

		static OpCode[] _tab;
		public static OpCode[] OpcodesTable
		{
			get
			{
				if (_tab == null)
				{
					{
						OpCode[] _tmp = new OpCode[256];
						foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
						{
							OpCode code = (OpCode)field.GetValue(null);
							if (code.Size == 1)
								_tmp[code.Value] = code;
						}
						_tab = _tmp;
					}
				}
				return _tab;
			}
		}

		public static void EmitLdcI4(this ILGenerator il, int i)
		{
			switch (i)
			{
				case -1:
					il.Emit(OpCodes.Ldc_I4_M1);
					break;
				case 0:
					il.Emit(OpCodes.Ldc_I4_0);
					break;
				case 1:
					il.Emit(OpCodes.Ldc_I4_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldc_I4_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldc_I4_3);
					break;
				case 4:
					il.Emit(OpCodes.Ldc_I4_4);
					break;
				case 5:
					il.Emit(OpCodes.Ldc_I4_5);
					break;
				case 6:
					il.Emit(OpCodes.Ldc_I4_6);
					break;
				case 7:
					il.Emit(OpCodes.Ldc_I4_7);
					break;
				case 8:
					il.Emit(OpCodes.Ldc_I4_8);
					break;
				default:
					{
						if (-128 <= i && i <= 127)
							il.Emit(OpCodes.Ldc_I4_S, (byte)i);
						else
							il.Emit(OpCodes.Ldc_I4, i);
					}
					break;
			}
		}
	};
}