using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using alib.Enumerable;

namespace alib.Bits
{
	public static unsafe class Bitz
	{
		public static ulong[] Init(int c)
		{
			return new ulong[((c - 1) >> 6) + 1];
		}
		public static int ComputeSize(int c) { return ((c - 1) >> 6) + 1; }

		public static void Set(ulong* p, int ix)
		{
			p += (uint)ix >> 6;
			*p = *p | (1UL << (ix & 0x3F));
		}
		public static void Set(this ulong[] arr, int ix)
		{
			Debug.Assert(((uint)ix >> 6) < (uint)arr.Length);
			fixed (ulong* p = arr)
				Set(p, ix);
		}
		public static void Unset(ulong* p, int ix)
		{
			p += (uint)ix >> 6;
			*p = *p & ~(1UL << (ix & 0x3F));
		}
		public static void Unset(this ulong[] arr, int ix)
		{
			Debug.Assert(((uint)ix >> 6) < (uint)arr.Length);
			fixed (ulong* p = arr)
				Unset(p, ix);
		}
		public static bool Test(this ulong[] arr, int ix)
		{
			return (arr[ix >> 6] & (1UL << ix)) != 0;
		}
		public static bool Test(ulong* p, int ix)
		{
			return (p[ix >> 6] & (1UL << ix)) != 0;
		}
		public static bool TestAndSet(ulong* p, int ix)
		{
			ulong v, m = 1UL << ix;
			if (((v = *(p += (ix >> 6))) & m) != 0)
				return false;
			*p = v | m;
			return true;
		}
		public static bool TestAndSet(ulong[] arr, int ix)
		{
			ulong v, m = 1UL << ix;
			if (((v = arr[ix >> 6]) & m) != 0)
				return false;
			arr[ix >> 6] = v | m;
			return true;
		}

		public static void ClearAll(this ulong[] arr)
		{
			System.Array.Clear(arr, 0, arr.Length);
		}

		public static System.String ToString01(this ulong[] arr, int c = -1)
		{
			System.String s = "";
			if (c < 0)
				c = arr.Length << 6;
			int i = 0;
			while (c >= 64)
			{
				s += _bitarray_ext.DisplayUlongBitsLeftLSB(arr[i++], 64);
				c -= 64;
			}
			if (c > 0)
				s += _bitarray_ext.DisplayUlongBitsLeftLSB(arr[i], c);
			return s;
		}

		//struct _ba
		//{
		//    public int c_bits;
		//    public int c_ones;
		//    public fixed ulong summ[1];
		//    public fixed ulong data[1];
		//}
	};


#if false
	sealed class BitHelper
	{
		public static int ToIntArrayLength(int n)
		{
			if (n <= 0)
				return 0;
			return ((n - 1) / 0x20) + 1;
		}

		const byte IntSize = 0x20;
		const byte MarkedBitFlag = 1;

		public BitHelper(int* pi, int c)
		{
			m_pi = pi;
			m_c = c;
		}

		public BitHelper(int c)
		{
			m_arr = new int[c];
			m_c = c;
		}

		int m_c;
		int* m_pi;
		readonly int[] m_arr;

		public bool IsMarked(int bitPosition)
		{
			int index = bitPosition / 0x20;
			if (m_pi != null)
				return index < m_c && index >= 0 && (m_pi[index] & (1 << (bitPosition % 0x20))) != 0;
			return index < m_c && index >= 0 && (m_arr[index] & (1 << (bitPosition % 0x20))) != 0;
		}

		public void MarkBit(int bitPosition)
		{
			int ix = bitPosition / 0x20;

			if (ix < m_c && ix >= 0)
			{
				if (m_pi != null)
					m_pi[ix] |= 1 << (bitPosition % 0x20);
				else
					m_arr[ix] |= 1 << (bitPosition % 0x20);
			}
		}
	};
#endif

	public unsafe struct BitHelper
	{
		ulong* m_arrayPtr;
		int m_length;

		/// <param name="bitArrayPtr"></param>
		/// <param name="c_ulongs">((c_bits - 1) SHR 6) + 1</param>
		public BitHelper(ulong* bitArrayPtr, int c_ulongs)
		{
			m_arrayPtr = bitArrayPtr;
			m_length = c_ulongs;
		}

		public bool this[int index]
		{
			get { return IsMarked(index); }
			set { if (value) SetBit(index); else ClearBit(index); }
		}

		public bool IsMarked(int bitPosition)
		{
			uint num = (uint)bitPosition >> 6;
			return 0 <= num && num < m_length && (m_arrayPtr[num] & (1UL << (bitPosition & 0x3F))) != 0;
		}

		public void SetBit(int bitPosition)
		{
			uint num = (uint)bitPosition >> 6;
			if (0 <= num && num < m_length)
			{
				ulong* p = m_arrayPtr + num;
				*p = *p | (1UL << (bitPosition & 0x3F));
			}
		}
		public void ClearBit(int bitPosition)
		{
			uint num = (uint)bitPosition >> 6;
			if (0 <= num && num < m_length)
			{
				ulong* p = m_arrayPtr + num;
				*p = *p & ~(1UL << (bitPosition & 0x3F));
			}
		}


		public int[] Positions
		{
			get
			{
				int c = OnesCount;
				if (c == 0)
					return IntArray.Empty;
				int[] arr = new int[c];
				fixed (int* _pi = arr)
				{
					int* pi = _pi;
					ulong* p = m_arrayPtr;
					for (int i = 0, offs = 0; i < m_length; i++, p++)
						if (*p == 0xFFFFFFFFFFFFFFFF)
						{
							int j = offs;
							offs += 64;
							while (j < offs)
								*pi++ = j++;
						}
						else
						{
							pi += _bitarray_ext.OnesPositions(*p, pi, offs);
							offs += 64;
						}
				}
				return arr;
			}
		}

		public int OnesCount
		{
			get
			{
				int c = 0;
				ulong* p = m_arrayPtr;
				for (int i = 0; i < m_length; i++, p++)
					c += *p == 0xFFFFFFFFFFFFFFFF ? 64 : _bitarray_ext.OnesCount2(*p);
				return c;
			}
		}
	};

	public static class _bitarray_ext
	{
		static unsafe _bitarray_ext()
		{
			p_tab = (byte*)Marshal.AllocHGlobal(256);
			for (int i = 0; i < 256; i++)
				p_tab[i] = _bits_set_256[i];

			//p_ul_tab = (ulong*)Marshal.AllocHGlobal(64 * sizeof(ulong));
			//for (int i = 0; i < 64; i++)
			//    p_ul_tab[i] = _single_bits[i];
		}

		public static int SafeCount(this alib.BitArray.BitArr ba)
		{
			return ba == null ? 0 : ba.Count;
		}
		public static readonly uint[] _ror_singletons = 
		{
			0xFFFFFFFF,
			0x55555555,
			0x49249249,
			0x11111111,
			0x42108421,
			0x41041041,
			0x10204081,
			0x01010101,
			0x08040201,
			0x40100401,
			0x00400801,
			0x01001001,
			0x04002001,
			0x10004001,
			0x40008001,
			0x00010001,
			0x00020001,
			0x00040001,
			0x00080001,
			0x00100001,
			0x00200001,
			0x00400001,
			0x00800001,
			0x01000001,
			0x02000001,
			0x04000001,
			0x08000001,
			0x10000001,
			0x20000001,
			0x40000001,
			0x80000001,
		};

		static readonly byte[] _bits_set_256 = 
		{
			0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 
			1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
			1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
			2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
			1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
			2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
			2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
			3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
			1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
			2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
			2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
			3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
			2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
			3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
			3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
			4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
		};
		//static byte[] BitsSetTable256 = 
		//{
		//  0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 
		//  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
		//  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
		//  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
		//  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
		//  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
		//  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
		//  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
		//  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
		//  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
		//  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
		//  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
		//  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
		//  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
		//  3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
		//  4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
		//};

		//const String _s_bits_set_256 =
		//    "\x00\x01\x01\x02\x01\x02\x02\x03\x01\x02\x02\x03\x02\x03\x03\x04" +
		//    "\x01\x02\x02\x03\x02\x03\x03\x04\x02\x03\x03\x04\x03\x04\x04\x05" +
		//    "\x01\x02\x02\x03\x02\x03\x03\x04\x02\x03\x03\x04\x03\x04\x04\x05" +
		//    "\x02\x03\x03\x04\x03\x04\x04\x05\x03\x04\x04\x05\x04\x05\x05\x06" +
		//    "\x01\x02\x02\x03\x02\x03\x03\x04\x02\x03\x03\x04\x03\x04\x04\x05" +
		//    "\x02\x03\x03\x04\x03\x04\x04\x05\x03\x04\x04\x05\x04\x05\x05\x06" +
		//    "\x02\x03\x03\x04\x03\x04\x04\x05\x03\x04\x04\x05\x04\x05\x05\x06" +
		//    "\x03\x04\x04\x05\x04\x05\x05\x06\x04\x05\x05\x06\x05\x06\x06\x07" +
		//    "\x01\x02\x02\x03\x02\x03\x03\x04\x02\x03\x03\x04\x03\x04\x04\x05" +
		//    "\x02\x03\x03\x04\x03\x04\x04\x05\x03\x04\x04\x05\x04\x05\x05\x06" +
		//    "\x02\x03\x03\x04\x03\x04\x04\x05\x03\x04\x04\x05\x04\x05\x05\x06" +
		//    "\x03\x04\x04\x05\x04\x05\x05\x06\x04\x05\x05\x06\x05\x06\x06\x07" +
		//    "\x02\x03\x03\x04\x03\x04\x04\x05\x03\x04\x04\x05\x04\x05\x05\x06" +
		//    "\x03\x04\x04\x05\x04\x05\x05\x06\x04\x05\x05\x06\x05\x06\x06\x07" +
		//    "\x03\x04\x04\x05\x04\x05\x05\x06\x04\x05\x05\x06\x05\x06\x06\x07" +
		//    "\x04\x05\x05\x06\x05\x06\x06\x07\x05\x06\x06\x07\x06\x07\x07\x08";

		//public static readonly ulong[] _single_bits = 
		//{
		//    0x0000000000000001,			0x0000000000000002,			0x0000000000000004,			0x0000000000000008,
		//    0x0000000000000010,			0x0000000000000020,			0x0000000000000040,			0x0000000000000080,
		//    0x0000000000000100,			0x0000000000000200,			0x0000000000000400,			0x0000000000000800,
		//    0x0000000000001000,			0x0000000000002000,			0x0000000000004000,			0x0000000000008000,
		//    0x0000000000010000,			0x0000000000020000,			0x0000000000040000,			0x0000000000080000,
		//    0x0000000000100000,			0x0000000000200000,			0x0000000000400000,			0x0000000000800000,
		//    0x0000000001000000,			0x0000000002000000,			0x0000000004000000,			0x0000000008000000,
		//    0x0000000010000000,			0x0000000020000000,			0x0000000040000000,			0x0000000080000000,
		//    0x0000000100000000,			0x0000000200000000,			0x0000000400000000,			0x0000000800000000,
		//    0x0000001000000000,			0x0000002000000000,			0x0000004000000000,			0x0000008000000000,
		//    0x0000010000000000,			0x0000020000000000,			0x0000040000000000,			0x0000080000000000,
		//    0x0000100000000000,			0x0000200000000000,			0x0000400000000000,			0x0000800000000000,
		//    0x0001000000000000,			0x0002000000000000,			0x0004000000000000,			0x0008000000000000,
		//    0x0010000000000000,			0x0020000000000000,			0x0040000000000000,			0x0080000000000000,
		//    0x0100000000000000,			0x0200000000000000,			0x0400000000000000,			0x0800000000000000,
		//    0x1000000000000000,			0x2000000000000000,			0x4000000000000000,			0x8000000000000000,
		//};

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int OnesCount(this uint v)
		{
			int c = _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			return c + _bits_set_256[v & 0xFF];
		}

		public static unsafe int OnesPositions(ulong v, int* rg_64_ints, int offs)
		{
			int* p = rg_64_ints;
			while (v != 0)
			{
				ulong b = v & (ulong)-(long)v;
				*(bool*)p = ((b & 0xFFFFFFFF00000000UL) > 0UL);
				*p <<= 1;
				*(bool*)p |= ((b & 0xFFFF0000FFFF0000UL) > 0UL);
				*p <<= 1;
				*(bool*)p |= ((b & 0xFF00FF00FF00FF00UL) > 0UL);
				*p <<= 1;
				*(bool*)p |= ((b & 0xF0F0F0F0F0F0F0F0UL) > 0UL);
				*p <<= 1;
				*(bool*)p |= ((b & 0xCCCCCCCCCCCCCCCCUL) > 0UL);
				*p <<= 1;
				*(bool*)p |= ((b & 0xAAAAAAAAAAAAAAAAUL) > 0UL);
				*p++ += offs;
				v ^= b;
			}
			return (int)(p - rg_64_ints);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<int> OnesPositions(this ulong ul)
		{
			ulong _tmp;
			while (ul != 0)
			{
				yield return SoleBitPosition(_tmp = ul & (ulong)-(long)ul);
				ul ^= _tmp;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SoleBitPosition(this ulong ul)
		{
			return (0 == (ul & 0xAAAAAAAAAAAAAAAAUL) ? 0 : 1) +
				   (0 == (ul & 0xCCCCCCCCCCCCCCCCUL) ? 0 : 2) +
				   (0 == (ul & 0xF0F0F0F0F0F0F0F0UL) ? 0 : 4) +
				   (0 == (ul & 0xFF00FF00FF00FF00UL) ? 0 : 8) +
				   (0 == (ul & 0xFFFF0000FFFF0000UL) ? 0 : 16) +
						((ul < 0xFFFFFFFF00000000UL) ? 0 : 32);
		}

		static unsafe readonly byte* p_tab;


#if false
		public static int OnesCount(this ulong v)
		{
			int c = _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			c += _bits_set_256[v & 0xFF];
			v >>= 8;
			return c + _bits_set_256[v & 0xFF];
		}
#else
		public static unsafe int OnesCount(this ulong v)
		{
			byte* q = p_tab;
			return
				(byte)*(q + *(((byte*)&v) + 0)) +
				(byte)*(q + *(((byte*)&v) + 1)) +
				(byte)*(q + *(((byte*)&v) + 2)) +
				(byte)*(q + *(((byte*)&v) + 3)) +
				(byte)*(q + *(((byte*)&v) + 4)) +
				(byte)*(q + *(((byte*)&v) + 5)) +
				(byte)*(q + *(((byte*)&v) + 6)) +
				(byte)*(q + *(((byte*)&v) + 7));
		}
		public static unsafe int OnesCount2(ulong v)
		{
			int c = 0;
			while (v != 0)
			{
				c++;
				v &= v - 1;
			}
			return c;
		}
#endif

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// ONLY WORKS WHEN THERE IS EXACTLY ONE BIT SET
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int OnlyBitPosition(this uint z)
		{
			int c = ((z & 0xAAAAAAAA) != 0) ? 1 : 0;
			if ((z & 0xCCCCCCCC) != 0)
				c |= 0x2;
			if ((z & 0xF0F0F0F0) != 0)
				c |= 0x4;
			if ((z & 0xFF00FF00) != 0)
				c |= 0x8;
			if ((z & 0xFFFF0000) != 0)
				c |= 0x10;
			return c;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int LowestOne(this short z)
		{
			z = (short)(z & -z);
			int c = ((z & 0xAAAA) != 0) ? 1 : 0;
			if ((z & 0xCCCC) != 0)
				c |= 0x2;
			if ((z & 0xF0F0) != 0)
				c |= 0x4;
			if ((z & 0xFF00) != 0)
				c |= 0x8;
			return c;
		}

		public static int HighestOne(this ulong ui64)
		{
			int o = 0;
			if (ui64 != 0)
			{
				if ((ui64 & 0xFFFFFFFF00000000) != 0)
				{
					ui64 >>= 32;
					o += 32;
				}
				if ((ui64 & 0xFFFF0000) != 0)
				{
					ui64 >>= 16;
					o += 16;
				}
				if ((ui64 & 0xFF00) != 0)
				{
					ui64 >>= 8;
					o += 8;
				}
				if ((ui64 & 0xF0) != 0)
				{
					ui64 >>= 4;
					o += 4;
				}
				if ((ui64 & 0xC) != 0)
				{
					ui64 >>= 2;
					o += 2;
				}
				if ((ui64 & 0x2) != 0)
					o++;
			}
			return o;
		}

		public static int HighestOne(UInt32 ui32)
		{
			int o = 0;
			if (ui32 != 0)
			{
				if ((ui32 & 0xFFFF0000) != 0)
				{
					ui32 >>= 16;
					o += 16;
				}
				if ((ui32 & 0xFF00) != 0)
				{
					ui32 >>= 8;
					o += 8;
				}
				if ((ui32 & 0xF0) != 0)
				{
					ui32 >>= 4;
					o += 4;
				}
				if ((ui32 & 0xC) != 0)
				{
					ui32 >>= 2;
					o += 2;
				}
				if ((ui32 & 0x2) != 0)
					o++;
			}
			return o;
		}

		public static int HighestOne(Int32 i32)
		{
			int o = 0;
			if (i32 != 0)
			{
				if ((i32 & 0xFFFF0000) != 0)
				{
					i32 >>= 16;
					o += 16;
				}
				if ((i32 & 0xFF00) != 0)
				{
					i32 >>= 8;
					o += 8;
				}
				if ((i32 & 0xF0) != 0)
				{
					i32 >>= 4;
					o += 4;
				}
				if ((i32 & 0xC) != 0)
				{
					i32 >>= 2;
					o += 2;
				}
				if ((i32 & 0x2) != 0)
					o++;
			}
			return o;
		}

		public static System.String DisplayUlongBits(ulong ul, int c)
		{
			if (c > 64)
				c = 64;
			else if (c <= 0)
				return System.String.Empty;
			var rgch = new Char[c];
			ulong msk = 1;
			for (int i = 1; i <= c; i++)
			{
				rgch[c - i] = (ul & msk) == 0 ? '0' : '1';
				msk <<= 1;
			}
			return new System.String(rgch);
		}
		public static System.String DisplayUlongBitsLeftLSB(ulong ul, int c)
		{
			if (c > 64)
				c = 64;
			else if (c <= 0)
				return System.String.Empty;
			var rgch = new Char[c];
			ulong msk = 1;
			for (int i = 0; i < c; i++)
			{
				rgch[i] = (ul & msk) == 0 ? '0' : '1';
				msk <<= 1;
			}
			return new System.String(rgch);
		}
	};

	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
	public struct bytehash32
	{
		[FieldOffset(0)]
		int _int32;

		[FieldOffset(0)]
		byte a;
		[FieldOffset(1)]
		byte b;
		[FieldOffset(2)]
		byte c;
		[FieldOffset(3)]
		byte d;

		public static int Get(int x, int y)
		{
			var h = default(bytehash32);
			h._int32 = x ^ y;
			return h.a ^ h.b ^ h.c ^ h.d;
		}
		public static int Get(int x)
		{
			var h = default(bytehash32);
			h._int32 = x;
			return h.a ^ h.b ^ h.c ^ h.d;
		}
		public static int Get(uint x)
		{
			var h = default(bytehash32);
			h._int32 = (int)x;
			return h.a ^ h.b ^ h.c ^ h.d;
		}
	};
}
