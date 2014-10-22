#define DOTNET_45
using System;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace alib.Memory
{
	using String = System.String;
	using Array = System.Array;


	public static class _ext
	{
		public static String MemoryDisplay(byte[] mem, int i = 0, int c = -1)
		{
			if (mem == null || (uint)((c = c < 0 ? mem.Length : c) - i) > (uint)mem.Length)
				return String.Empty;

			StringBuilder sb = new StringBuilder();
			for (; i < c; i += 32)
			{
				byte[] line = mem.Skip(i).Take(32).ToArray();
				String s = String.Format("{0:x8} {1,-96}{2}",
					i,
					String.Join(" ", line.Select(e => e.ToString("x2")).ToArray()),
					new String(line.Select(e => 32 <= e && e <= 127 ? (Char)e : '.').ToArray()));
				sb.AppendLine(s);
			}
			return sb.ToString();
		}
	};

	public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
	{
		public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();
		private ByteArrayEqualityComparer() { }

		public bool Equals(byte[] x, byte[] y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			if (x.Length != y.Length)
				return false;
			for (var i = 0; i < x.Length; i++)
				if (x[i] != y[i])
					return false;
			return true;
		}

		public int GetHashCode(byte[] obj)
		{
			if (obj == null || obj.Length == 0)
				return 0;
			var hashCode = 0;
			for (var i = 0; i < obj.Length; i++)
				// Rotate by 3 bits and XOR the new value. 
				hashCode = (hashCode << 3) | (int)((uint)hashCode >> 29) ^ obj[i];
			return hashCode;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if !DOTNET_45
	[Serializable]
	public sealed class WeakReference<T> : WeakReference
	{
		public WeakReference(T target)
			: base(target)
		{
		}
		public WeakReference(T target, bool trackResurrection)
			: base(target, trackResurrection)
		{
		}
		public new T Target { get { return (T)base.Target; } }
	};
#endif

	public class AdjPriv
	{
		[DllImport("advapi32", ExactSpelling = true, SetLastError = true)]
		internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

		[DllImport("kernel32", ExactSpelling = true)]
		internal static extern IntPtr GetCurrentProcess();

		[DllImport("advapi32", ExactSpelling = true, SetLastError = true)]
		internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

		[DllImport("advapi32", SetLastError = true)]
		internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct TokPriv1Luid
		{
			public int Count;
			public long Luid;
			public int Attr;
		}

		internal const int SE_PRIVILEGE_ENABLED /*		*/ = 0x00000002;
		internal const int TOKEN_QUERY /*				*/ = 0x00000008;
		internal const int TOKEN_ADJUST_PRIVILEGES /*	*/ = 0x00000020;

		public static bool SetPriv(String sz_priv)
		{
			bool retVal;
			TokPriv1Luid tp;
			IntPtr hproc = GetCurrentProcess();
			IntPtr htok = IntPtr.Zero;
			retVal = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
			tp.Count = 1;
			tp.Luid = 0;
			tp.Attr = SE_PRIVILEGE_ENABLED;
			retVal = LookupPrivilegeValue(null, sz_priv, ref tp.Luid);
			retVal = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
			return retVal;
		}
	};

	public static class interlocked
	{
		/// <summary>
		/// atomically set a bit in a 32-bit integer in shared memory
		/// </summary>
		/// <param name="status">memory location at which to atomically set a single bit</param>
		/// <param name="singleton_bit">should have only one bit set (i.e. a power of two);
		/// otherwise it is not possible to tell who enacted the change.</param>
		/// <returns>
		/// Returns the previous value at <paramref name="pi"/>. If the indicated bit is cleared 
		/// in this return value then this caller enacted the change. If the indicated bit is set 
		/// in the return value then no action was taken.
		/// </returns>
		public static int SetBit(ref int pi, int singleton_bit)
		{
			Debug.Assert((singleton_bit & (singleton_bit - 1)) == 0);
			int _cur = pi;
			do
				if ((_cur & singleton_bit) != 0)
					return _cur;
			while (_cur != (_cur = Interlocked.CompareExchange(ref pi, _cur ^ singleton_bit, _cur)));
			return _cur;
		}
		public static long SetBit(ref long pi, long singleton_bit)
		{
			Debug.Assert((singleton_bit & (singleton_bit - 1)) == 0);
			long _cur = pi;
			do
				if ((_cur & singleton_bit) != 0)
					return _cur;
			while (_cur != (_cur = Interlocked.CompareExchange(ref pi, _cur ^ singleton_bit, _cur)));
			return _cur;
		}

		/// <summary>
		/// atomically clear a bit in a 32-bit integer in shared memory
		/// </summary>
		/// <param name="status">memory location at which to atomically clear a single bit</param>
		/// <param name="singleton_bit">should have only one bit set (i.e. a power of two);
		/// otherwise it is not possible to tell who enacted the change.</param>
		/// <returns>
		/// Returns the previous value at <paramref name="pi"/>. If the indicated bit is set 
		/// in this return value then this caller enacted the change. If the indicated bit is clear 
		/// in the return value then no action was taken.
		/// </returns>
		public static int ClearBit(ref int pi, int singleton_bit)
		{
			Debug.Assert((singleton_bit & (singleton_bit - 1)) == 0);
			int _cur = pi;
			do
				if ((_cur & singleton_bit) == 0)
					return _cur;
			while (_cur != (_cur = Interlocked.CompareExchange(ref pi, _cur ^ singleton_bit, _cur)));
			return _cur;
		}
		public static long ClearBit(ref long pi, long singleton_bit)
		{
			Debug.Assert((singleton_bit & (singleton_bit - 1)) == 0);
			long _cur = pi;
			do
				if ((_cur & singleton_bit) == 0)
					return _cur;
			while (_cur != (_cur = Interlocked.CompareExchange(ref pi, _cur ^ singleton_bit, _cur)));
			return _cur;
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static unsafe class Kernel32
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESSORCORE
		{
			public byte Flags;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct NUMANODE
		{
			public uint NodeNumber;
		}

		public enum PROCESSOR_CACHE_TYPE
		{
			CacheUnified,
			CacheInstruction,
			CacheData,
			CacheTrace
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CACHE_DESCRIPTOR
		{
			public byte Level;
			public byte Associativity;
			public ushort LineSize;
			public uint Size;
			public PROCESSOR_CACHE_TYPE Type;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION
		{
			[FieldOffset(0)]
			public PROCESSORCORE ProcessorCore;
			[FieldOffset(0)]
			public NUMANODE NumaNode;
			[FieldOffset(0)]
			public CACHE_DESCRIPTOR Cache;
			[FieldOffset(0)]
			private UInt64 Reserved1;
			[FieldOffset(8)]
			private UInt64 Reserved2;
		}

		public enum LOGICAL_PROCESSOR_RELATIONSHIP
		{
			RelationProcessorCore,
			RelationNumaNode,
			RelationCache,
			RelationProcessorPackage,
			RelationGroup,
			RelationAll = 0xffff
		}

		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
		{
			public UIntPtr ProcessorMask;
			public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
			public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION ProcessorInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORYSTATUSEX
		{
			public uint dwLength;
			public uint dwMemoryLoad;
			public ulong ullTotalPhys;
			public ulong ullAvailPhys;
			public ulong ullTotalPageFile;
			public ulong ullAvailPageFile;
			public ulong ullTotalVirtual;
			public ulong ullAvailVirtual;
			public ulong ullAvailExtendedVirtual;
		}

		static unsafe class _internal
		{
			[DllImport("kernel32", SetLastError = true)]
			public static extern bool GetLogicalProcessorInformation(IntPtr Buffer, ref uint ReturnLength);

			[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern bool GlobalMemoryStatusEx([In, Out] ref MEMORYSTATUSEX lpBuffer);

			[DllImport("kernel32")]
			public static extern void CopyMemory(void* dest, void* src, int length);

			[DllImport("kernel32", EntryPoint = "RtlMoveMemory")]
			public static extern void MoveMemory(void* dest, void* src, int length);

			[DllImport("kernel32", EntryPoint = "RtlZeroMemory", SetLastError = false)]
			public static extern void ZeroMemory(void* dest, int size);

			[DllImport("kernel32", EntryPoint = "RtlFillMemory")]
			public static extern void FillMemory(void* dest, int length, byte value);

#if MSVCRT	// slightly faster than the preceding but who knows if we want a dependency on the C runtime
			[DllImport("msvcrt")]
			public static unsafe extern void* memset(void* dest, int c, int count);
#endif
		};
		[DllImport("kernel32")]
		public static extern int InterlockedOr(ref int Target, int Value);

#if ILFUNC
		[ILFunc(@"
	ldarg.0
	ldarg.1
	ldarg.2
	cpblk
	ret
")]
#endif
		public static void MoveMemory(void* dest, void* src, int length)
		{
			_internal.MoveMemory(dest, src, length);
		}
		public static void CopyMemory(void* dest, void* src, int length)
		{
			_internal.CopyMemory(dest, src, length);
		}
		public static void ZeroMemory(void* dest, int length)
		{
			_internal.ZeroMemory(dest, length);
		}
#if MSVCRT
		public static void __Memset(void* dest, int c, int length)
		{
			_internal.memset(dest, c, length);
		}
#elif true
		public static void Memset(void* dest, int c, byte value)
		{
			_internal.FillMemory(dest, c, value);
		}
		public static void Memset(byte[] a, byte value)
		{
			fixed (byte* p = a)
				_internal.FillMemory(p, a.Length, value);
		}
#else
		public static void Memset(byte[] a, byte value)
		{
			int c;
			if ((c = a.Length) > 0)
			{
				if (a.Length < 775)
					memset_small(a, c, value);
				else
					memset_large(a, c, value);
			}
		}
		static unsafe void memset_small(byte[] a, int c, byte value)
		{
			ulong v;
			if (value == 0xff)
				v = 0xffffffffffffffff;
			else if (value == 0)
				v = 0;
			else
			{
				var pb = (byte*)&v;
				*pb++ = value;
				*pb++ = value;
				*pb++ = value;
				*pb++ = value;
				*pb++ = value;
				*pb++ = value;
				*pb++ = value;
				*pb++ = value;
			}

			fixed (byte* _p = a)
			{
				ulong* pul = (ulong*)_p;
				while ((c -= 8) >= 0)
					*pul++ = v;

				var pb = (byte*)pul;
				while (c-- > -8)
					*pb++ = value;
			}
		}
		static void memset_large(byte[] a, int c, byte value)
		{
			int i;
			a[0] = a[1] = a[2] = a[3] = a[4] = a[5] = a[6] = a[7] = value;
			var c_src = c >> 1;
			for (i = 8; i < c; i += i)
				Array.Copy(a, 0, a, i, i <= c_src ? i : c - i);
		}
#endif
		public static MEMORYSTATUSEX MemoryStatus
		{
			get
			{
				MEMORYSTATUSEX ms = new MEMORYSTATUSEX();
				ms.dwLength = (uint)sizeof(MEMORYSTATUSEX);
				_internal.GlobalMemoryStatusEx(ref ms);
				return ms;
			}
		}

		private const int ERROR_INSUFFICIENT_BUFFER = 122;

		static int _phys_cores = -1;
		public static int PhysicalProcessorCores
		{
			get
			{
				if (_phys_cores == -1)
					_phys_cores = GetLogicalProcessorInformation()
							.Where(lpi => lpi.Relationship == alib.Memory.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
							.Count();
				return _phys_cores;
			}
		}

		public static bool IsHyperthreading { get { return PhysicalProcessorCores != Environment.ProcessorCount; } }

		public static SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] GetLogicalProcessorInformation()
		{
			uint ReturnLength = 0;
			_internal.GetLogicalProcessorInformation(IntPtr.Zero, ref ReturnLength);
			if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
			{
				IntPtr Ptr = Marshal.AllocHGlobal((int)ReturnLength);
				try
				{
					if (_internal.GetLogicalProcessorInformation(Ptr, ref ReturnLength))
					{
						int size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
						int len = (int)ReturnLength / size;
						SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] Buffer = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION[len];
						IntPtr Item = Ptr;
						for (int i = 0; i < len; i++)
						{
							Buffer[i] = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION)Marshal.PtrToStructure(Item, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
							Item += size;
						}
						return Buffer;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(Ptr);
				}
			}
			return null;
		}
	};

	public struct InsituArray16<T> where T : class
	{
		T t00;
		T t01;
		T t02;
		T t03;
		T t04;
		T t05;
		T t06;
		T t07;
		T t08;
		T t09;
		T t10;
		T t11;
		T t12;
		T t13;
		T t14;
		T t15;
		T t16;
		byte m_c;

		public byte Add(T t)
		{
			if (m_c >= 16)
				throw new InvalidOperationException();
			byte c = m_c++;
			this[c] = t;
			return c;
		}

		public T this[int ix]
		{
			get
			{
				if (ix >= m_c)
					throw new IndexOutOfRangeException();
				switch (ix)
				{
					default:
						throw new IndexOutOfRangeException();
					case 0:
						return t00;
					case 1:
						return t01;
					case 2:
						return t02;
					case 3:
						return t03;
					case 4:
						return t04;
					case 5:
						return t05;
					case 6:
						return t06;
					case 7:
						return t07;
					case 8:
						return t08;
					case 9:
						return t09;
					case 10:
						return t10;
					case 11:
						return t11;
					case 12:
						return t12;
					case 13:
						return t13;
					case 14:
						return t14;
					case 15:
						return t15;
					case 16:
						return t16;
				}
			}
			set
			{
				if (ix >= m_c)
					throw new IndexOutOfRangeException();
				switch (ix)
				{
					default:
						throw new IndexOutOfRangeException();
					case 0:
						t00 = value;
						break;
					case 1:
						t01 = value;
						break;
					case 2:
						t02 = value;
						break;
					case 3:
						t03 = value;
						break;
					case 4:
						t04 = value;
						break;
					case 5:
						t05 = value;
						break;
					case 6:
						t06 = value;
						break;
					case 7:
						t07 = value;
						break;
					case 8:
						t08 = value;
						break;
					case 9:
						t09 = value;
						break;
					case 10:
						t10 = value;
						break;
					case 11:
						t11 = value;
						break;
					case 12:
						t12 = value;
						break;
					case 13:
						t13 = value;
						break;
					case 14:
						t14 = value;
						break;
					case 15:
						t15 = value;
						break;
					case 16:
						t16 = value;
						break;
				}
			}
		}
	};

	unsafe public struct VmProtector : IDisposable
	{
		public VmProtector(uint cb_alloc, out long* pl)
		{
			this.size = (UIntPtr)cb_alloc;
			pl = (long*)(this.ip = Virtual.VirtualAlloc(
							IntPtr.Zero,
							size,
							Virtual.AllocationType.COMMIT | Virtual.AllocationType.RESERVE,
							Virtual.MemoryProtection.READWRITE));
		}

		UIntPtr size;
		IntPtr ip;

		public void Dispose()
		{
			var mp = Virtual.MemoryProtection.READONLY;
			Virtual.VirtualProtect(ip, size, mp, out mp);
		}
	};

	//if (f_large)
	//{
	//    if (!AdjPriv.SetPriv("SeLockMemoryPrivilege"))
	//        throw new Exception();
	//}

	public static class Virtual
	{
		[Flags()]
		public enum AllocationType : uint
		{
			COMMIT /*			*/ = 0x00001000,
			RESERVE /*			*/ = 0x00002000,
			RESET /*			*/ = 0x00080000,
			LARGE_PAGES /*		*/ = 0x20000000,
			PHYSICAL /*			*/ = 0x00400000,
			TOP_DOWN /*			*/ = 0x00100000,
			WRITE_WATCH /*		*/ = 0x00200000
		};

		[Flags()]
		public enum MemoryProtection : uint
		{
			EXECUTE /*						*/ = 0x010,
			EXECUTE_READ /*					*/ = 0x020,
			EXECUTE_READWRITE /*			*/ = 0x040,
			EXECUTE_WRITECOPY /*			*/ = 0x080,
			NOACCESS /*						*/ = 0x001,
			READONLY /*						*/ = 0x002,
			READWRITE /*					*/ = 0x004,
			WRITECOPY /*					*/ = 0x008,
			GUARD_Modifierflag /*			*/ = 0x100,
			NOCACHE_Modifierflag /*			*/ = 0x200,
			WRITECOMBINE_Modifierflag /*	*/ = 0x400
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_INFO
		{
			public ushort processorArchitecture;
			ushort reserved;
			public uint pageSize;
			public IntPtr minimumApplicationAddress;
			public IntPtr maximumApplicationAddress;
			public IntPtr activeProcessorMask;
			public uint numberOfProcessors;
			public uint processorType;
			public uint allocationGranularity;
			public ushort processorLevel;
			public ushort processorRevision;
		}

		[DllImport("kernel32", SetLastError = true)]
		static extern IntPtr HeapCreate(uint flOptions, UIntPtr dwInitialSize, UIntPtr dwMaximumSize);

		[DllImport("kernel32", SetLastError = false)]
		static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);

		[DllImport("kernel32", SetLastError = true)]
		static extern bool HeapDestroy(IntPtr hHeap);

		[DllImport("kernel32", SetLastError = true)]
		static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

		[DllImport("kernel32")]
		static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

		[DllImport("kernel32")]
		static extern IntPtr GlobalFree(IntPtr hMem);

		[DllImport("kernel32")]
		static extern UInt32 GetLargePageMinimum();

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr VirtualAlloc(IntPtr lpStartAddr, UIntPtr size, AllocationType flAllocationType, MemoryProtection flProtect);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

		[DllImport("kernel32")]
		public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
	};
}
