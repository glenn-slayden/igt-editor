//#define CHECK_ARGS
//#define CHECK_RET

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Linq;

using alib.Debugging;
using alib.Enumerable;

#pragma warning disable 0162, 0164

namespace alib.BitArray
{
	using String = System.String;

	[DebuggerDisplay("{ToString(),nq}")]
	public unsafe sealed class BitArr : IEquatable<BitArr>
	{
		///////////////////////////////////////////////////////////////////////////////////////
		/// static tables
		///////////////////////////////////////////////////////////////////////////////////////

		const ulong de_Bruijn = 0x07EDD5E59A4E28C2;
		static readonly int* dbj64;
		static readonly byte* bc16;
		static readonly byte* bc8;

		static BitArr()
		{
			dbj64 = (int*)Marshal.AllocHGlobal(64 * sizeof(int));
			ulong ul = 1;
			for (int i = 0; i < 0x40; i++)
			{
				dbj64[((ul * de_Bruijn) >> 58)] = i;
				ul <<= 1;
			}

			bc16 = (byte*)Marshal.AllocHGlobal(0x10000);
			bc8 = (byte*)Marshal.AllocHGlobal(0x100);
			for (int i = 0; i < 0x10000; i++)
			{
				byte c = 0;
				int v = i;
				while (v != 0)
				{
					c++;
					v &= v - 1;
				}
				if (i < 0x100)
					bc8[i] = c;
				bc16[i] = c;
			}

			var bf = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			foreach (var mi in typeof(BitArr).GetMethods(bf))
				RuntimeHelpers.PrepareMethod(mi.MethodHandle);
		}

		///////////////////////////////////////////////////////////////////////////////////////
		/// constructors and serialization
		///////////////////////////////////////////////////////////////////////////////////////

		public BitArr(int c_bits)
		{
			m_c_bits = c_bits;
			m_c_ones = 0;
			c_bits--;
			m_c_qw = (c_bits >> 6) + 1;
			m_c_dist = (c_bits >> 12) + 1;
			m_arr = new ulong[m_c_qw + m_c_dist];
		}

		public BitArr(int c_bits, int ix)
			: this(c_bits)
		{
			this[ix] = true;
			Debug.Assert(m_c_ones == 1);
		}

		public BitArr(int c_bits, System.Collections.Generic.IEnumerable<int> ie)
			: this(c_bits)
		{
			foreach (int i in ie)
				if (i >= 0)
					this[i] = true;
			Debug.Assert(m_c_ones == ie.Where(_i => _i >= 0).CountDistinct());
		}

		public BitArr(BitArr arg)
			: this(arg.m_c_bits)
		{
			this.CopyFrom(arg);
		}

		public BitArr(int c_bits, BinaryReader br)
			: this(c_bits)
		{
			for (int i = 0; i < m_c_qw + m_c_dist; i++)
				m_arr[i] = br.ReadUInt64();
			throw new Exception("m_c_ones");
		}
#if false
		BitArr(int c_bits, ulong* pul, int _1s, int hc)
			: this(c_bits)
		{
			this.m_c_bits = c_bits;
			c_bits--;
			m_c_qw = (c_bits >> 6) + 1;
			m_c_dist = (c_bits >> 12) + 1;
			this.m_c_ones = _1s;
			this.hash_code = hc;

			//fixed (ulong* dst = m_arr)
			//    Marshal.Copy((IntPtr)pul, (long[])(Object)m_arr, 0, m_arr.Length);	// slower than...
			int c = m_arr.Length;
			fixed (ulong* _dst = m_arr)
			{
				ulong* dst = _dst;
				for (int i = 0; i < c; i++)
					*dst++ = *pul++;
			}
		}
#endif
		BitArr(int c_bits, ulong[] newarr, int _1s, ulong hc)
		{
			this.m_c_bits = c_bits;
			c_bits--;
			m_c_qw = (c_bits >> 6) + 1;
			m_c_dist = (c_bits >> 12) + 1;
			this.m_arr = newarr;
			this.m_c_ones = _1s;
			this.hash_code = hc;
		}

		public void Write(BinaryWriter bw)
		{
			for (int i = 0; i < m_c_qw + m_c_dist; i++)
				bw.Write(m_arr[i]);
			throw new Exception("m_c_ones");
		}

		public void CopyFrom(BitArr arg)
		{
			_check_args(this, arg);
			arg.m_arr.CopyTo(this.m_arr, 0);
			this.hash_code = arg.hash_code;
			this.m_c_ones = arg.m_c_ones;
		}

		///////////////////////////////////////////////////////////////////////////////////////
		/// fields
		///////////////////////////////////////////////////////////////////////////////////////

		readonly ulong[] m_arr;
		ulong hash_code;
		readonly int m_c_bits;
		readonly int m_c_qw;
		readonly int m_c_dist;
		int m_c_ones;

		///////////////////////////////////////////////////////////////////////////////////////
		/// properties
		///////////////////////////////////////////////////////////////////////////////////////

		public int Count { get { return m_c_bits; } }

		public bool this[int idx]
		{
			get { return (m_arr[idx >> 6] & (1UL << idx)) != 0; }
			set
			{
				_check();
				if (value)
					_try_set(idx, idx >>= 6, ref m_arr[idx]);
				else
					_try_clear(idx, idx >>= 6, ref m_arr[idx]);
				_check();
			}
		}
		bool _try_set(int idx, int b_i, ref ulong ul)
		{
			Debug.Assert(m_c_ones >= 0);
			ulong cur, m = 1UL << idx;
			if (((cur = ul) & m) != 0)
				return false;
			ul = cur | m;
			m_arr[m_c_qw + (b_i >> 6)] |= (1UL << b_i);

			if (++m_c_ones == 1)
				hash_code = (uint)idx + 1;
			else
			{
				if (m_c_ones == 2)
					hash_code = 1UL << ((int)hash_code - 1);
				hash_code ^= m;
			}
			return true;
		}
		bool _try_clear(int idx, int b_i, ref ulong ul)
		{
			Debug.Assert(m_c_ones >= 0);
			if (m_c_ones == 0)
				return false;
			ulong cur, m = 1UL << idx;
			if (((cur = ul) & m) == 0)
				return false;
			if ((ul = cur & ~m) == 0)
				m_arr[m_c_qw + (b_i >> 6)] &= ~(1UL << b_i);

			if (--m_c_ones == 1)
				hash_code = (uint)idx + 1;
			else if (m_c_ones == 0)
				hash_code = 0;
			else
				hash_code ^= m;
			return true;
		}

		public bool TrySet(int idx)
		{
			_check();
			if (idx >= m_c_bits)
				throw new IndexOutOfRangeException();
			return _try_set(idx, idx >>= 6, ref m_arr[idx]);
		}

		public bool TryClear(int idx)
		{
			_check();
			if (idx >= m_c_bits)
				throw new IndexOutOfRangeException();
			return _try_clear(idx, idx >>= 6, ref m_arr[idx]);
		}

		public bool IsAllZeros { get { return m_c_ones == 0; } }
		public bool IsAllOnes { get { return m_c_ones == m_c_bits; } }
		public int OnesCount { get { return m_c_ones; } }
		public int ZerosCount { get { return m_c_bits - m_c_ones; } }

		///////////////////////////////////////////////////////////////////////////////////////
		/// methods
		///////////////////////////////////////////////////////////////////////////////////////

		public void SetAll()
		{
			ulong h;
			int i = 0;
			while (i < m_c_qw - 1)
				m_arr[i++] = 0xFFFFFFFFFFFFFFFF;
			m_arr[i++] = h = (1UL << m_c_bits) - 1;

			while (i < m_c_qw + m_c_dist - 1)
				m_arr[i++] = 0xFFFFFFFFFFFFFFFF;
			m_arr[i] = (1UL << m_c_qw) - 1;

			if (m_c_bits == 1)
				hash_code = 1;
			else
			{
				if ((m_c_qw & 1) == 0)
					h = ~h;
				hash_code = h;
			}
			m_c_ones = m_c_bits;
			_check();
		}

		public void ClearAll()
		{
			if (m_c_ones == 0)
				return;
			hash_code = 0;
			if (m_c_ones >= 7)
			{
				System.Array.Clear(m_arr, 0, m_arr.Length);
				m_c_ones = 0;
			}
			else
			{
				int x, i = m_c_qw, k = 0;
				fixed (ulong* _p = m_arr)
				{
					while (true)
					{
						if ((x = clear_indicated_bits(_p, _p[i], k)) != 0)
						{
							_p[i] = 0;
							if ((m_c_ones -= x) == 0) // (lucky)
								break;
						}
						if (++i == m_arr.Length)
						{
							m_c_ones = 0;
							break;
						}
						k += 0x40;
					}
				}
			}
		}

		static int clear_indicated_bits(ulong* _p, ulong t, int k)
		{
			if (t == 0)
				return 0;
			int c = 0;
			ulong m = 0;
			do
			{
				_p[k + dbj64[((m = t & (ulong)-(long)t) * de_Bruijn) >> 58]] = 0;
				t ^= m;
				c++;
			}
			while (t != 0);
			return c;
		}

		public int[] OnesPositions()
		{
			Debug.Assert(m_c_ones >= 0);
			if (m_c_bits == 0)
				return IntArray.Empty;
			int[] ret = new int[m_c_ones];
			fixed (int* _pi = ret)
			fixed (ulong* _p = m_arr)
				write_1s_to_array(m_arr.Length, m_c_qw, _pi, _p);
			return ret;
		}

		static void write_1s_to_array(int c, int i, int* pi, ulong* _p)
		{
			int k = 0;
			int* _dbj64 = dbj64;
			while (true)
			{
				ulong b = _p[i];
				while (b != 0)
				{
					ulong m = b & (ulong)-(long)b;
					int j = k + _dbj64[(m * de_Bruijn) >> 58];
					store_indexes_from_quad(&pi, j << 6, _p[j]);
					b ^= m;
				}

				if (++i == c)
					return;
				k += 0x40;
			}
		}

		public void GetOnesPositions(int* pi)
		{
			Debug.Assert(m_c_ones >= 0);
			fixed (ulong* _p = m_arr)
				write_1s_to_array(m_arr.Length, m_c_qw, pi, _p);
		}

		static void store_indexes_from_quad(int** pi, int j, ulong b)
		{
			Debug.Assert(b != 0);
			int* _dbj64 = dbj64;
			do
			{
				ulong m = b & (ulong)-(long)b;
				**pi = j + _dbj64[(m * de_Bruijn) >> 58];
				(*pi)++;
				b ^= m;
			}
			while (b != 0);
		}

		public void GetOnesPositionsReverse(int* pi)
		{
			Debug.Assert(m_c_ones >= 0);
			fixed (ulong* _p = m_arr)
				write_1s_to_array_reverse(m_c_qw, m_arr.Length, m_c_dist << 6, pi, _p);
		}

		static void write_1s_to_array_reverse(int c, int i, int k, int* pi, ulong* _p)
		{
			int* _dbj64 = dbj64;
			while (true)
			{
				k -= 0x40;
				ulong b = _p[--i];
				while (b != 0)
				{
					ulong m = b & (ulong)-(long)b;
					int j = k + _dbj64[(m * de_Bruijn) >> 58];
					store_indexes_from_quad_reverse(&pi, j << 6, _p[j]);
					b ^= m;
				}
				if (i == c)
					return;
			}
		}

		static void store_indexes_from_quad_reverse(int** pi, int j, ulong b)
		{
			Debug.Assert(b != 0);
			int* _dbj64 = dbj64;
			do
			{
				ulong m = b & (ulong)-(long)b;
				(*pi)--;
				**pi = j + _dbj64[(m * de_Bruijn) >> 58];
				b ^= m;
			}
			while (b != 0);
		}

		public void OrEq(BitArr arg)
		{
			_check_args(this, arg);

			int i, _1s;
			if (arg.m_c_ones == 0)
				return;
			if ((_1s = m_c_ones) == m_c_bits)
				return;
			if (arg.m_c_ones == 1)
				_try_set(i = (int)arg.hash_code - 1, i >>= 6, ref m_arr[i]);
			else
			{
				ulong x, v, h, di = 1;
				h = v = 0;
				for (i = 0; i < m_c_qw; i++)
				{
					x = m_arr[i];
					h ^= (v = (x | arg.m_arr[i]));
					if (v != x)
					{
						_1s += cb((m_arr[i] = v) ^ x);
						m_arr[m_c_qw + (i >> 6)] |= di;
					}

					if ((di <<= 1) == 0)
						di = 1;
				}
				if (_1s <= 1)
				{
					// since this is an 'or,' the only way we can end up with 1 or fewer bits is
					// if the argument has 1 (or 0) bits. both cases are handled above.
					throw new Exception();
				}
				m_c_ones = _1s;
				hash_code = h;
			}
			_check();
		}
#if false
		public bool AndEq(BitArr arg)
		{
			_check_args(this, arg);
			int _1s = m_c_ones;
			ulong di = 1;
			ulong v, h = v = 0;
			int om;
			for (int i = 0; i < m_c_qw; i++)
			{
				ulong x = m_arr[i];
				h ^= (v = (x & arg.m_arr[i]));
				if (v != x)
				{
					_1s -= cb((m_arr[i] = v) ^ x);
					if (_1s == 1)
					{
						throw new NotImplementedException();
					}
					if (v == 0)
						m_arr[m_c_qw + (i >> 6)] &= ~di;
				}
				if ((di <<= 1) == 0)
					di = 1;
			}

			if (_1s == 0)
				hash_code = 0;
			else if (_1s == 1)
			{
				throw new NotImplementedException();
				if (_1s == 1)
					h = (uint)((om << 6) + dbj64[(h * de_Bruijn) >> 58]) + 1;
			}
			else
			{
				m_c_ones = _1s;
				hash_code = h;
			}
			_check();
			return m_c_ones > 0;
		}
#endif

		public bool IsSubsumedByFull(BitArr arg)
		{
			if (this == arg || m_c_ones == 0)
				return true;
			if (m_c_ones > arg.m_c_ones)
				return false;
			if (m_c_ones == 1)
				return arg[this.OnesPositions()[0]];
			return IsSubsumedBy(arg);
		}

		public bool IsSubsumedBy(BitArr arg)
		{
			_check_args(this, arg);
			if (m_c_ones == 1)
				throw new Exception();
			if (this == arg)
				throw new Exception();
			if (m_c_ones > arg.m_c_ones)
				return false;
			/// any zero in the 'and' of the two any-ones hints positively rules out a
			/// quad that doesn't need to be checked. we start by extracting only the 1-bits,
			/// counting how many we do. If it looks like we might end up extracting almost
			/// every bit, we switch to just checking all of the target quads.

			ulong t;
			int i, c = m_arr.Length;
			ulong[] a2 = arg.m_arr;
			for (i = m_c_qw; i < c; i++)
			{
				if (((t = m_arr[i]) & a2[i]) != t)
					return false;
				if (t != 0)
					goto do_full;
			}
			throw new Exception();
		do_full:
			bool ret;
			fixed (ulong* _p1 = this.m_arr, _p2 = a2)
				ret = _sb_full(m_c_qw, i, c, _p1, _p2, t);
			return ret;
		}

		static bool _sb_full(int c_qw, int i, int c, ulong* _p1, ulong* _p2, ulong t)
		{
			int k = i == c_qw ? 0 : 0x20 << (i - c_qw);
			int* _dbj64 = dbj64;
			do
			{
				do
				{
					ulong m = t & (ulong)-(long)t;
					int j = k + _dbj64[(m * de_Bruijn) >> 58];

					/// if more than a certain number of hint bits is set then we're wasting time finding 
					/// their logarithms, so just switch over to checking all of the actual bits and ignore 
					/// the current hint quad
					ulong tx;
					if (((tx = _p1[j]) & _p2[j]) != tx)
						return false;

					t ^= m;
				}
				while (t != 0);
			next:
				if (++i == c)
					break;
				if (((t = _p1[i]) & _p2[i]) != t)
					return false;
				k += 0x40;
			}
			while (t != 0);
			return true;
		}

		static bool _ft_full(int c_qw, int i, int c, ulong* _p1, ulong* _p2, ulong t)
		{
			int k = i == c_qw ? 0 : 0x20 << (i - c_qw);
			int* _dbj64 = dbj64;
			do
			{
				do
				{
					ulong m = t & (ulong)-(long)t;
					int j = k + _dbj64[(m * de_Bruijn) >> 58];

					/// if more than a certain number of hint bits is set then we're wasting time finding 
					/// their logarithms, so just switch over to checking all of the actual bits and ignore 
					/// the current hint quad
					if ((_p1[j] & _p2[j]) != 0)
						return true;

					t ^= m;
				}
				while (t != 0);
			next:
				if (++i == c)
					break;
				k += 0x40;
			}
			while ((t = (_p1[i] & _p2[i])) != 0);
			return false;
		}

		public bool FastTest(BitArr arg)
		{
			_check_args(this, arg);

			/// any zero in the 'and' of the two any-ones hints positively rules out a
			/// quad that doesn't need to be checked. we start by extracting only the 1-bits,
			/// counting how many we do. If it looks like we might end up extracting almost
			/// every bit, we switch to just checking all of the target quads.
			ulong t;
			int i, c = m_arr.Length;
			ulong[] a2 = arg.m_arr;
			for (i = m_c_qw; i < c; i++)
				if ((t = m_arr[i] & a2[i]) != 0)
					goto do_full;
			return false;
		do_full:
			bool ret;
			fixed (ulong* _p1 = this.m_arr, _p2 = a2)
				ret = _ft_full(m_c_qw, i, c, _p1, _p2, t);
			return ret;
		}

#if false
		static int fast_and(int i, int c, ulong* _p1, ulong* _p2, ulong* ret, out int _1s)
		{
			ulong t, m, v, d;
			int j, k = 0;
			int* _dbj64 = dbj64;
			_1s = 0;
			ulong h = 0;
			while (true)
			{
				if ((t = _p1[i] & _p2[i]) != 0)
				{
					d = 0;
					do
					{
						m = t & (ulong)-(long)t;
						j = k + _dbj64[(m * de_Bruijn) >> 58];
						if ((v = _p1[j] & _p2[j]) != 0)
						{
							_1s += cb(ret[j] = v);
							h ^= v;
							d |= m;
						}
						t ^= m;
					}
					while (t != 0);
					if (d != 0)
						ret[i] = d;
				}
				if (++i == c)
					break;
				k += 0x40;
			}
			throw new NotImplementedException();
			return (int)h ^ (int)(h >> 32);
		}

		static bool _compare_singletons(int i, ulong[] a1, ulong[] a2)
		{
			ulong v;
			while ((v = a1[i]) == 0)
				i++;
			if (a2[i] != v)
				return false;
			int j = dbj64[(v * de_Bruijn) >> 58];
			return a1[j] == a2[j];
		}
#endif

		public BitArr AndWithHash(BitArr arg)
		{
			Debug.Assert(this.m_c_ones != 1 || arg.m_c_ones != 1);
			//if (this.m_c_ones == 1 && arg.m_c_ones == 1)
			//    return _compare_singletons(m_c_qw, m_arr, arg.m_arr) ? this : null;

			_check_args(this, arg);

			int i = m_c_qw, j;
			ulong t, m, v, d;
			int k = 0, _1s = 0, om = 0;
			int* _dbj64 = dbj64;
			ulong h = 0;

			ulong[] newarr = new ulong[m_arr.Length];
			fixed (ulong* _p1 = m_arr, _p2 = arg.m_arr, _ret = newarr)
			{
				while (true)
				{
					if ((t = _p1[i] & _p2[i]) != 0)
					{
						d = 0;
						do
						{
							m = t & (ulong)-(long)t;
							j = k + _dbj64[(m * de_Bruijn) >> 58];
							if ((v = _p1[j] & _p2[j]) != 0)
							{
								if ((_1s += cb(_ret[j] = v)) == 1)
									om = j;
								h ^= v;
								d |= m;
							}
							t ^= m;
						}
						while (t != 0);
						if (d != 0)
							_ret[i] = d;
					}
					if (++i == m_arr.Length)
						break;
					k += 0x40;
				}
			}
			BitArr ret = null;
			if (_1s != 0)
			{
				if (_1s == 1)
					h = (uint)((om << 6) + _dbj64[(h * de_Bruijn) >> 58]) + 1;
				ret = new BitArr(m_c_bits, newarr, _1s, h);
				ret._check();
			}
			return ret;
		}

		///////////////////////////////////////////////////////////////////////////////////////
		/// overloaded operators
		///////////////////////////////////////////////////////////////////////////////////////
#if false
		public static BitArr operator &(BitArr arg1, BitArr arg2)
		{
			_check_args(arg1, arg2);
			BitArr ret = new BitArr(arg1.m_c_bits);
			int c_qw = ret.m_c_qw;
			ulong d = 0;
			ulong di = 1;
			ulong v;
			ulong h = 0;
			int _1s = 0;
			for (int i = 0; i < c_qw; i++)
			{
				if ((v = arg1.m_arr[i] & arg2.m_arr[i]) != 0)
				{
					h ^= v;
					_1s += cb(ret.m_arr[i] = v);
					d |= di;
				}
				if ((di <<= 1) == 0)
				{
					ret.m_arr[c_qw + (i >> 6)] = d;
					d = 0;
					di = 1;
				}
			}
			ret.m_arr[c_qw + ((c_qw - 1) >> 6)] = d;
			if ((ret.m_c_ones = _1s)==1)
				throw new NotImplementedException();
			else
				ret.hash_code = h;
			ret._check();
			return ret;
		}
#endif

		public static BitArr operator |(BitArr arg1, BitArr arg2)
		{
			if (arg1 == arg2 || arg2.m_c_ones == 0)
				return new BitArr(arg1);
			if (arg1.m_c_ones == 0)
				return new BitArr(arg2);

			_check_args(arg1, arg2);
			BitArr ret = new BitArr(arg1.m_c_bits);
			int c_qw = ret.m_c_qw;
			ulong d = 0;
			ulong di = 1;
			ulong v;
			ulong h = 0;
			int _1s = 0;
			for (int i = 0; i < c_qw; i++)
			{
				if ((v = arg1.m_arr[i] | arg2.m_arr[i]) != 0)
				{
					h ^= v;
					_1s += cb(ret.m_arr[i] = v);
					d |= di;
				}
				if ((di <<= 1) == 0)
				{
					ret.m_arr[c_qw + (i >> 6)] = d;
					d = 0;
					di = 1;
				}
			}
			ret.m_arr[c_qw + ((c_qw - 1) >> 6)] = d;
			ret.m_c_ones = _1s;
			Debug.Assert(ret.m_c_ones > 1);
			ret.hash_code = h;
			ret._check();
			return ret;
		}

		public static BitArr operator ~(BitArr arg)
		{
			Nop.Untested();
			int cb = arg.m_c_bits;
			BitArr ret = new BitArr(cb);
			int c_qw = ret.m_c_qw;
			ulong m = 1;
			for (int i = 0; i < c_qw; i++)
			{
				if ((ret.m_arr[i] = ~arg.m_arr[i]) != 0)
				{
					int z = c_qw + (i >> 6);
					ret.m_arr[z] |= m;
				}
				if ((m <<= 1) == 0)
					m = 1;
			}
			ret.m_c_ones = cb - arg.m_c_ones;
			ret._check();
			return ret;
		}

		///////////////////////////////////////////////////////////////////////////////////////
		/// IEquatable, Object overrides, Display
		///////////////////////////////////////////////////////////////////////////////////////

		public override int GetHashCode()
		{
			Debug.Assert(m_c_ones >= 0 && (m_c_ones != 0 || hash_code == 0) && (m_c_ones != 1 || hash_code != 0));

			ulong h = hash_code;
			if (m_c_ones <= 1)
				return (int)h;

			if (h == 0)
			{
				for (int i = 0; i < m_c_qw; i++)
					h ^= m_arr[i];
				this.hash_code = h;
			}
			int* pi;
			return *(pi = (int*)&h) ^ pi[1];
		}

		public bool Equals(BitArr arg)
		{
			bool ret = (hash_code == arg.hash_code && m_c_ones == arg.m_c_ones);
			if (ret)
			{
				if (m_c_ones > 3100)
				{
					for (int i = 0; i < m_c_qw; i++)
						if (m_arr[i] != arg.m_arr[i])
						{
							ret = false;
							break;
						}
				}
				else if (m_c_ones > 1)
				{
					fixed (ulong* _p1 = m_arr, _p2 = arg.m_arr)
						ret = _equals(m_c_qw, m_arr.Length, _p1, _p2);
				}
			}
			return ret;
		}

		static bool _equals(int i, int c, ulong* _p1, ulong* _p2)
		{
			int k = 0;
			int* _dbj64 = dbj64;
			ulong b;
			while ((b = _p1[i]) == _p2[i])
			{
				while (b != 0)
				{
					ulong m = b & (ulong)-(long)b;
					int j = k + _dbj64[(m * de_Bruijn) >> 58];
					if (_p1[j] != _p2[j])
						return false;
					b ^= m;
				}

				if (++i == c)
					return true;
				k += 0x40;
			}
			return false;
		}

		public override bool Equals(Object obj) { throw new NotImplementedException(); }

		///////////////////////////////////////////////////////////////////////////////////////
		/// internal
		///////////////////////////////////////////////////////////////////////////////////////

		int _count_set_bits()
		{
			int count = 0;
			int* _dbj64 = dbj64;
			fixed (ulong* _p = m_arr)
			{
				int i = m_c_qw, k = 0;
				while (true)
				{
					ulong b = _p[i];
					while (b != 0)
					{
						ulong m = b & (ulong)-(long)b;
						count += cb(_p[k + _dbj64[(m * de_Bruijn) >> 58]]);
						b ^= m;
					}
					if (++i == m_arr.Length)
						break;
					k += 0x40;
				}
			}
			return count;
		}

		static int cb(ulong v)
		{
			byte* pv, _bc8;
			return (_bc8 = bc8)[*(pv = (byte*)&v)] + _bc8[pv[1]] + _bc8[pv[2]] + _bc8[pv[3]] +
				_bc8[pv[4]] + _bc8[pv[5]] + _bc8[pv[6]] + _bc8[pv[7]];
		}

		///////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////

		public override String ToString()
		{
			String op = IsAllZeros ? "none" : IsAllOnes ? "all" : String.Join(" ", OnesPositions());
			return String.Format("len:{0} 1s:{1} {{ {2} }}", m_c_bits, m_c_ones, op);
			//Char[] wc_arr = new Char[m_c_bits];
			//for (int i = 0; i < m_c_bits; i++)
			//    wc_arr[i] = this[m_c_bits - i - 1] ? '1' : '0';
			//return new String(wc_arr);
		}

		public String ToHex(Char sep_char = default(Char))
		{
			int c = m_c_bits / 0x40;
			int cch = (m_c_bits / 8) + 1;
			if (sep_char != default(Char))
				cch += c;
			int end = cch;
			Char[] wc_arr = new Char[cch];
			String s;
			int i;
			for (i = 0; i < c; i++)
			{
				if (sep_char != default(Char))
				{
					s = m_arr[i].ToString("X");
					cch -= s.Length;
				}
				else
				{
					s = m_arr[i].ToString("X16");
					cch -= 8;
				}
				s.ToCharArray().CopyTo(wc_arr, cch);
				if (sep_char != default(Char))
					wc_arr[--cch] = sep_char;
			}
			if (sep_char != default(Char))
				s = m_arr[i].ToString("X");
			else
				s = m_arr[i].ToString("X" + cch.ToString());
			cch -= s.Length;
			s.ToCharArray().CopyTo(wc_arr, cch);
			return new String(wc_arr, cch, end - cch);
		}

		///////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////

		[Conditional("CHECK_ARGS")]
		static void _check_args(BitArr arg1, BitArr arg2)
		{
			if (arg1 == arg2)
				throw new Exception();
			if (arg1.m_c_ones != arg1._count_set_bits())
				throw new Exception();
			if (arg2.m_c_ones != arg2._count_set_bits())
				throw new Exception();

			if ((Object)arg1 == (Object)arg2)
				throw new Exception();
			if (arg1.m_c_bits != arg2.m_c_bits ||
				arg1.m_c_dist != arg2.m_c_dist ||
				arg1.m_c_qw != arg2.m_c_qw)
				throw new ArgumentException();
		}

		[Conditional("CHECK_RET")]
		public void _check()
		{
			if (m_c_ones != _count_set_bits())
				throw new Exception();

			ulong h = 0;
			ulong di = 1;
			for (int i = 0; i < m_c_qw; i++)
			{
				ulong z = m_arr[i];
				if (z != 0)
				{
					if ((m_arr[m_c_qw + (i >> 6)] & di) == 0)
						throw new Exception();
				}
				else
				{
					if ((m_arr[m_c_qw + (i >> 6)] & di) != 0)
						throw new Exception();
				}
				if ((di <<= 1) == 0)
					di = 1;
				h ^= z;
			}
			if (m_c_ones == 0)
			{
				if (h != 0)
					throw new Exception();
				if (hash_code != 0)
					throw new Exception();
			}
			else if (m_c_ones == 1)
			{
				if ((int)this.hash_code != OnesPositions()[0] + 1)
					throw new Exception();
			}
			else if (h != this.hash_code)
				throw new Exception();
		}
	};
}
