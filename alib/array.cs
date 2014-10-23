
using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Generic;
using System.Reflection.Emit;

using alib.Collections;
using alib.Enumerable;

namespace alib.Array
{
	using Array = System.Array;
	using Bitz = alib.Bits.Bitz;
	using String = System.String;

#if ! __MOBILE__
	public static class arr<TElem>
	{
		static arr()
		{
			_newarr = get_ctor();
		}

		readonly public static Func<int, TElem[]> _newarr;

		static Func<int, TElem[]> get_ctor()
		{
			DynamicMethod dm = new DynamicMethod(
							String.Empty,
							typeof(TElem[]),
							new[] { typeof(int) },
							typeof(Object),
							true) { InitLocals = false };

			ILGenerator il = dm.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Newarr, typeof(TElem));
			il.Emit(OpCodes.Ret);

			return (Func<int, TElem[]>)dm.CreateDelegate(typeof(Func<int, TElem[]>));
		}
	};
#endif

	public static partial class arr
	{
		///////////////////////////////////////////////////////
		///
		public static unsafe int[] ToArray(int* pi, int c)
		{
			if (c == 0)
				return IntArray.Empty;
			var ret = new int[c];
			for (int i = 0; i < c; i++)
				ret[i] = *pi++;
			return ret;
		}

		///////////////////////////////////////////////////////
		///
		public static int ArrayHash<T>(this T[] src)
		{
#if DEBUG
			if (typeof(T) == typeof(int))
				throw new Exception("use IntArray.Comparer");
#endif
			if (src == null)
				return -1;
			int h, k, i, g;
			T t;
			h = i = src.Length;
			for (k = 7; --i >= 0; k += 7)
				if ((t = src[i]) != null)
					h ^= ((g = t.GetHashCode()) << k) | (int)((uint)g >> (32 - k));
			return h;
		}

		///////////////////////////////////////////////////////
		///
#if false
		public static bool ValueCompare<T>(this T[] a, T[] b) where T : struct
		{
#if DEBUG
			if (typeof(T) == typeof(int))
				throw new Exception("use IntArray.Comparer");
#endif
			if (a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; i++)
				if (!Object.Equals(a[i], b[i]))
					return false;

			return true;
		}
#endif
		public static bool ValueCompare<T>(this T[] a, int a_offset, T[] b) where T : struct
		{
			if (a_offset >= a.Length || a_offset + b.Length > a.Length)
				return false;

			for (int i = 0; i < b.Length; i++, a_offset++)
				if (!Object.Equals(a[a_offset], b[i]))
					return false;

			return true;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>(this T[] src, T item, IEqualityComparer<T> cmp)
		{
			int c = src.Length;
			for (int i = 0; i < c; i++)
				if (cmp.Equals(src[i], item))
					return i;
			return -1;
		}
		public static int IndexofAny<T>(this T[] src, T[] items)
		{
			int ci;
			if ((ci = items.Length) > 0 && src.Length > 0)
			{
				if (ci == 1)
					return Array.IndexOf(src, items[0]);

				for (int i = 0; i < src.Length; i++)
					if (Array.IndexOf(items, src[i]) != -1)
						return i;
			}
			return -1;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains<T>(this T[] src, T item, IEqualityComparer<T> cmp)
		{
			int i, c = src.Length;
			for (i = 0; i < c; i++)
				if (cmp.Equals(src[i], item))
					return true;
			return false;
		}
		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains<T>(this T[] src, T item)
		{
			int i, c = src.Length;
			for (i = 0; i < c; i++)
				if (Object.Equals(src[i], item))
					return true;
			return false;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Prepend<T>(ref T[] src, T t) { src = Prepend(src, t); }
		public static T[] Prepend<T>(this T[] src, T t)
		{
			T[] _new;
			int c = src == null ? 0 : src.Length;
			(_new = new T[c + 1])[0] = t;
			while (c > 0)
				_new[c] = src[--c];
			return _new;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Append<T>(this T[] src, T t)
		{
			T[] _new;
			int c = src == null ? 0 : src.Length;
			(_new = new T[c + 1])[c] = t;
			while (--c >= 0)
				_new[c] = src[c];
			return _new;
		}
		public static int Append<T>(ref T[] src, T t)
		{
			int c;
			T[] _new, _tmp = src;
			do
			{
				(_new = new T[(c = _tmp == null ? 0 : _tmp.Length) + 1])[c] = t;
				while (--c >= 0)
					_new[c] = _tmp[c];
			}
			while (_tmp != (_tmp = Interlocked.CompareExchange(ref src, _new, _tmp)));
			return _new.Length - 1;
		}
		public static int AppendDistinct<T>(ref T[] src, T t, IEqualityComparer<T> cmp = null)
		{
			if (src == null || src.Length == 0)
			{
				src = new[] { t };
				return 0;
			}
			int ix;
			return (ix = IndexOf(src, t, cmp ?? EqualityComparer<T>.Default)) != -1 ? ix : Append(ref src, t);
		}
		public static void AppendSafe<T>(ref T[] src, T t)
		{
			int i, c;
			T[] _cur, _new;

			_cur = src ?? Interlocked.CompareExchange(ref src, new[] { t }, null);
			if (_cur != null)
				do
				{
					_new = new T[(c = _cur.Length) + 1];
					for (i = 0; i < c; i++)
						_new[i] = _cur[i];
					_new[i] = t;
				}
				while (_cur != (_cur = Interlocked.CompareExchange(ref src, _new, _cur)));
		}
		public static T[] AppendSafeIfNotNull<T>(ref T[] src, T t)
		{
			T[] _cur = src, _new;
			do
				if (_cur == null)
					return null;
			while (_cur != (_cur = Interlocked.CompareExchange(ref src, _new = Append(_cur, t), _cur)));
			return _new;
		}
		public static T AppendSafeDistinct<T>(ref T[] src, T t, IEqualityComparer<T> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<T>.Default;
			int i, c;
			T[] _cur, _new;

			_cur = src ?? Interlocked.CompareExchange(ref src, new[] { t }, null);
			if (_cur != null)
				do
				{
					_new = new T[(c = _cur.Length) + 1];
					for (i = c - 1; i >= 0; --i)
						if (cmp.Equals(_new[i] = _cur[i], t))
							return _cur[i];
					_new[c] = t;
				}
				while (_cur != (_cur = Interlocked.CompareExchange(ref src, _new, _cur)));
			return t;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetOrAppend<T>(ref T[] src, int ix, T t)
		{
			int c;
			if (ix >= (c = src.Length))
				arr.Resize(ref src, c + 10);
			src[ix] = t;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Resize<T>(ref T[] src, int c) { src = Resize(src, c); }
		public static T[] Resize<T>(this T[] src, int c)
		{
			if (c == 0)
				return Collection<T>.None;

			if (src == null)
				return new T[c];

			int c0;
			if ((c0 = src.Length) == c)
				return src;

			T[] _new = new T[c];
			if (c0 < c)
				c = c0;
			if (c < 350)
				while (--c >= 0)
					_new[c] = src[c];
			else
				Array.Copy(src, 0, _new, 0, c);

			return _new;
		}
		public static T[] EnsureSize<T>(this T[] src, int c)
		{
			int c0;
			T[] _new = src ?? Collection<T>.None;

			if (c > (c0 = src.Length))
			{
				_new = new T[c];
				if (c0 < 350)
					while (--c0 >= 0)
						_new[c0] = src[c0];
				else
					Array.Copy(src, 0, _new, 0, c0);
			}
			return _new;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InsertAt<T>(ref T[] src, int ix, T t) { src = InsertAt(src, ix, t); }
		public static T[] InsertAt<T>(this T[] src, int ix, T t)
		{
			T[] _new;
			int i, c, j;
			if (ix > (c = src == null ? 0 : src.Length))
				throw new IndexOutOfRangeException();

			_new = new T[++c];
			for (i = 0, j = 0; i < c; i++)
				_new[i] = i == ix ? t : src[j++];
			return _new;
		}
		public static void InsertAt<T>(ref T[] src, int ix)
		{
			T[] _new;
			int c;
			if (ix > (c = src == null ? 0 : src.Length))
				throw new IndexOutOfRangeException();

			_new = new T[c + 1];
			while (c > ix)
				_new[c] = src[--c];
			while (--c >= 0)
				_new[c] = src[c];
			src = _new;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InsertRangeAt<T>(ref T[] rg0, int ix, T[] rg1) { rg0 = InsertRangeAt(rg0, ix, rg1); }
		public static T[] InsertRangeAt<T>(this T[] rg0, int idx, T[] rg1)
		{
			if ((uint)idx > (uint)rg0.Length)
				throw new IndexOutOfRangeException();
			if (idx == 0)
				return Concat(rg1, rg0);
			if (idx == rg0.Length)
				return Concat(rg0, rg1);

			int j, i;
			T[] _new = new T[rg0.Length + rg1.Length];
			for (i = j = 0; i < _new.Length; i++)
				_new[i] = i < idx || j == rg1.Length ? rg0[i - j] : rg1[j++];
			return _new;
		}

		///////////////////////////////////////////////////////
		///
		public static void RemoveLast<T>(ref T[] src, int c = 1)
		{
			if ((c = src.Length - c) <= 0)
				src = Collection<T>.None;
			else
			{
				T[] _new = new T[c];
				while (--c >= 0)
					_new[c] = src[c];
				src = _new;
			}
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RemoveAt<T>(ref T[] src, int ix) { src = RemoveAt(src, ix); }
		public static T[] RemoveAt<T>(T[] src, int ix)
		{
			int c, i;
			if ((uint)ix >= (uint)(c = src.Length))
				throw new IndexOutOfRangeException();

			if (c == 1)
				return Collection<T>.None;

			T[] _new = new T[c - 1];

			if (ix > 0)
				Array.Copy(src, 0, _new, 0, ix);	// part I.

			if ((c -= (i = ix + 1)) > 0)
				Array.Copy(src, i, _new, ix, c);	// part II.

			return _new;
		}

		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RemoveExactlyOne<T>(ref T[] src, T t, IEqualityComparer<T> cmp = null)
		{
			src = RemoveExactlyOne(src, t, cmp);
		}
		public static T[] RemoveExactlyOne<T>(T[] src, T t, IEqualityComparer<T> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<T>.Default;
			int j, i, c;
			if ((c = src.Length) == 0)
				throw new ArgumentException();
			if (c == 1)
			{
				if (!cmp.Equals(t, src[0]))
					throw new ArgumentException("value not found in the array");
				return Collection<T>.None;
			}
			var _new = new T[c - 1];
			for (i = 0, j = 0; j < _new.Length; i++)
				if (i >= c)
					throw new ArgumentException("more than one match in the array");
				else if (!cmp.Equals(t, _new[j] = src[i]))
					j++;
			if (i < c && !cmp.Equals(t, src[i]))
				throw new ArgumentException("value not found in the array");
			return _new;
		}

		///////////////////////////////////////////////////////
		///
		public static int TryRemoveItemFirst<T>(ref T[] src, T t)
		{
			if (src != null)
			{
				int c = src.Length;
				for (int i = 0; i < c; i++)
					if (src[i].Equals(t))
					{
						src = RemoveAt(src, i);
						return i;
					}
			}
			return -1;
		}
		public static T[] TryRemoveItemFirst<T>(T[] src, T t)
		{
			int c = src.Length;
			for (int i = 0; i < c; i++)
				if (src[i].Equals(t))
					return RemoveAt(src, i);
			return src;
		}


		///////////////////////////////////////////////////////
		///
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RemoveAll<T>(ref T[] src, T t) { src = RemoveAll(src, t); }
		public static T[] RemoveAll<T>(T[] src, T t)
		{
			T[] _new = null;
			int c, i, j;
			if ((c = src.Length - 1) >= 0)
			{
				i = j = 0;
				if (c > 0)
					for (; i < c; i++)
						if (!src[i].Equals(t))
							(_new ?? (_new = new T[c]))[j++] = src[i];

				if (!src[i].Equals(t))
				{
					if (i == j)
						return src;
					(_new = Resize(_new, ++j))[j - 1] = src[i];
				}
				else if (j > 0 && j < c)
					_new = Resize(_new, j);
			}
			return _new;
		}
		public static void RemoveAllSafe<T>(ref T[] src, T t)
		{
			T[] _tmp;
			if ((_tmp = src) != null)
				while (_tmp != (_tmp = Interlocked.CompareExchange(ref src, RemoveAll(_tmp, t), _tmp)))
					;
		}

		///////////////////////////////////////////////////////
		///
		public static void RemoveMany<T>(ref T[] src, IReadOnlyList<T> other) { src = RemoveMany(src, other); }
		public unsafe static T[] RemoveMany<T>(T[] src, IReadOnlyList<T> other)
		{
			int i, c, c0;
			T t;

			if ((c0 = src.Length) > 0 && other != null && other.Count > 0)
			{
				c = c0;
				ulong* pul = stackalloc ulong[Bitz.ComputeSize(c)];

				for (int j = 0; j < other.Count; j++)
					for (t = other[j], i = 0; i < c0; i++)
						if (src[i].Equals(t) && Bitz.TestAndSet(pul, i) && --c == 0)
						{
							src = Collection<T>.None;
							goto exit;
						}

				if (c < c0)
				{
					var _new = new T[c--];
					while (c >= 0)
						if (!Bitz.Test(pul, --c0))
							_new[c--] = src[c0];
					src = _new;
				}
			}
		exit:
			return src;
		}

		///////////////////////////////////////////////////////
		///
		public static void RemoveRange<T>(ref T[] src, int ix, int count) { src = RemoveRange(src, ix, count); }
		static T[] RemoveRange<T>(T[] src, int ix, int count)
		{
			int c0, ix_resume;

			if ((uint)ix >= (uint)(c0 = src.Length))
				throw new IndexOutOfRangeException();
			if (count == 0)
				return src;
			if (count < 0 || (ix_resume = ix + count) > c0)
				throw new ArgumentOutOfRangeException();
			if ((count = c0 - count) == 0)
				return Collection<T>.None;

			T[] newarr = new T[count];

			count = 0;
			while (count < ix)
				newarr[count] = src[count++];

			while (ix_resume < c0)
				newarr[count++] = src[ix_resume++];

			return newarr;
		}

		///////////////////////////////////////////////////////
		///
		public static T[] Concat<T>(this T[] src, T[] more)
		{
			int i, j, c;
			T[] _new;

			if (more == null || (c = more.Length) == 0)
				_new = src;
			else if (src == null || (j = src.Length) == 0)
				_new = more;
			else
			{
				_new = new T[j + c];
				i = 0;
				while (i < j)
					_new[i] = src[i++];
				j = 0;
				while (j < c)
					_new[i++] = more[j++];
			}
			return _new ?? Collection<T>.None;
		}
		public static void Concat<T>(ref T[] src, IReadOnlyList<T> more) { src = Concat(src, more); }
		public static T[] Concat<T>(this T[] src, IReadOnlyList<T> more)
		{
			int i, j, c;
			T[] _new;

			if (more == null || (c = more.Count) == (j = 0))
				_new = src;
			else if ((src != null && (j = src.Length) > 0) || (_new = more as T[]) == null)
			{
				_new = new T[j + c];
				i = 0;
				while (i < j)
					_new[i] = src[i++];
				j = 0;
				while (j < c)
					_new[i++] = more[j++];
			}
			return _new ?? Collection<T>.None;
		}

		///////////////////////////////////////////////////////
		///
		public static void UnionWith<T>(ref T[] src, T[] other, IEqualityComparer<T> cmp = null)
		{
			int i, c;
			T[] _new;
			T t;

			_new = src ?? Collection<T>.None;

			if ((c = other.Length) > 0)
			{
				if (cmp == null)
					cmp = EqualityComparer<T>.Default;

				i = 0;
				do
					if (!arr.Contains(_new, t = other[i], cmp))
						_new = arr.Append(_new, t);
				while (++i < c);
			}
			src = _new;
		}
		public static void UnionWith<T>(ref T[] src, IEnumerable<T> other, IEqualityComparer<T> cmp = null)
		{
			T[] _new;
			T t;

			_new = src ?? Collection<T>.None;

			var e = other.GetEnumerator();
			if (e.MoveNext())
			{
				if (cmp == null)
					cmp = EqualityComparer<T>.Default;

				do
					if (!arr.Contains(_new, t = e.Current, cmp))
						_new = arr.Append(_new, t);
				while (e.MoveNext());
			}
			src = _new;
		}


		///////////////////////////////////////////////////////
		///
		public static T[][] Divide<T>(this T[] src, int count)
		{
			if (count == 0)
				throw new Exception();

			int i, j, k, m;
			j = k = src.Length / count;
			if ((m = src.Length % count) != 0)
				j++;
			var _new = new T[j][];
			for (i = 0, j = 0; i < k; i++, j += count)
				Array.Copy(src, j, _new[i] = new T[count], 0, count);
			if (m != 0)
				Array.Copy(src, j, _new[i] = new T[m], 0, m);
			return _new;
		}

		///////////////////////////////////////////////////////
		///
		public static void FilterFor<T, TFilter>(ref T[] src)
			where T : class
			where TFilter : class, T
		{
			int c = src.Length;
			if (c > 0)
			{
				int j = 0;
				for (int i = 0; i < c; i++)
					if (!(src[i] is TFilter))
						j++;
				if (j > 0)
				{
					TFilter[] dst = new TFilter[c - j];
					TFilter t;
					j = 0;
					for (int i = 0; i < c; i++)
						if ((t = (src[i] as TFilter)) != null)
							dst[j++] = t;
					src = dst;
				}
			}
		}
	};
}
