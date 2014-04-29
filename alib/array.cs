#define swapper
#define INT_QSORT_UNSAFE

using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Generic;

namespace alib.Array
{
	using SysArray = System.Array;

	public static class arr
	{
		public static int ArrayHash<T>(this T[] arr)
		{
#if DEBUG
			if (typeof(T) == typeof(int))
				throw new Exception("use IntArray.Comparer");
#endif
			if (arr == null)
				return -1;
			int h, k, i, g;
			T t;
			h = i = arr.Length;
			for (k = 7; --i >= 0; k += 7)
				if ((t = arr[i]) != null)
					h ^= ((g = t.GetHashCode()) << k) | (int)((uint)g >> (32 - k));
			return h;
		}
		public static bool ValueCompare<T>(this T[] a, T[] b) where T : struct
		{
#if DEBUG
			if (typeof(T) == typeof(int))
				throw new Exception("use IntArray.Comparer");
#endif
			if (a.Length != b.Length)
				return false;

			var q = EqualityComparer<T>.Default;
			for (int i = 0; i < a.Length; i++)
				if (!q.Equals(a[i], b[i]))
					return false;

			return true;
		}
		public static bool ValueCompare<T>(this T[] a, int a_offset, T[] b) where T : struct
		{
			if (a_offset >= a.Length || a_offset + b.Length > a.Length)
				return false;

			var q = EqualityComparer<T>.Default;
			for (int i = 0; i < b.Length; i++, a_offset++)
				if (!q.Equals(a[a_offset], b[i]))
					return false;

			return true;
		}


		/// <summary>
		/// You can use this extension method on a null 'this' value (i.e. properly-typed).
		/// A single-element array is returned. This function is thread-safe.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Append<T>(this T[] src, T item)
		{
			T[] dst;
			int c = src == null ? 0 : src.Length;
			(dst = new T[c + 1])[c] = item;
			while (--c >= 0)
				dst[c] = src[c];
			return dst;
		}
		public static int Append<T>(ref T[] src, T item)
		{
			int c;
			T[] _new, _tmp = src;
			do
			{
				(_new = new T[(c = _tmp == null ? 0 : _tmp.Length) + 1])[c] = item;
				while (--c >= 0)
					_new[c] = _tmp[c];
			}
			while (_tmp != (_tmp = Interlocked.CompareExchange(ref src, _new, _tmp)));
			return _new.Length - 1;
		}

		/// <summary>
		/// Doesn't check for duplicates
		/// </summary>
		public static void AppendSafe<T>(ref T[] rgh, T t)
		{
			int i, c;
			T[] _cur, _new;

			_cur = rgh ?? Interlocked.CompareExchange(ref rgh, new[] { t }, null);
			if (_cur != null)
				do
				{
					_new = new T[(c = _cur.Length) + 1];
					for (i = 0; i < c; i++)
						_new[i] = _cur[i];
					_new[i] = t;
				}
				while (_cur != (_cur = Interlocked.CompareExchange(ref rgh, _new, _cur)));
		}

		public static T[] AppendSafeIfNotNull<T>(ref T[] rgh, T t)
		{
			T[] _cur = rgh, _new;
			do
				if (_cur == null)
					return null;
			while (_cur != (_cur = Interlocked.CompareExchange(ref rgh, _new = Append(_cur, t), _cur)));
			return _new;
		}

		public static T AppendSafeDistinct<T>(ref T[] rgh, T t, IEqualityComparer<T> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<T>.Default;
			int i, c;
			T[] _cur, _new;

			_cur = rgh ?? Interlocked.CompareExchange(ref rgh, new[] { t }, null);
			if (_cur != null)
				do
				{
					_new = new T[(c = _cur.Length) + 1];
					for (i = c - 1; i >= 0; --i)
						if (cmp.Equals(_new[i] = _cur[i], t))
							return _cur[i];
					_new[c] = t;
				}
				while (_cur != (_cur = Interlocked.CompareExchange(ref rgh, _new, _cur)));
			return t;
		}

		public static int AppendDistinct<T>(ref T[] src, T item, IEqualityComparer<T> cmp = null)
		{
			if (src == null || src.Length == 0)
			{
				src = new[] { item };
				return 0;
			}
			int ix;
			return (ix = IndexOf(src, item, cmp ?? EqualityComparer<T>.Default)) != -1 ? ix : Append(ref src, item);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>(this T[] arr, T item, IEqualityComparer<T> cmp)
		{
			int c = arr.Length;
			for (int i = 0; i < c; i++)
				if (cmp.Equals(arr[i], item))
					return i;
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains<T>(this T[] arr, T item, IEqualityComparer<T> cmp)
		{
			int c = arr.Length;
			for (int i = 0; i < c; i++)
				if (cmp.Equals(arr[i], item))
					return true;
			return false;
		}

		public static void UnionWith<T>(ref T[] rgh, T[] items, IEqualityComparer<T> cmp = null)
		{
			int i, c;
			T[] _cur;
			T t;

			_cur = rgh ?? alib.Collections.Collection<T>.None;

			if ((c = items.Length) > 0)
			{
				if (cmp == null)
					cmp = EqualityComparer<T>.Default;

				i = 0;
				do
					if (!arr.Contains(_cur, t = items[i], cmp))
						_cur = arr.Append(_cur, t);
				while (++i < c);
			}
			rgh = _cur;
		}

		public static void UnionWith<T>(ref T[] rgh, IEnumerable<T> items, IEqualityComparer<T> cmp = null)
		{
			T[] _cur;
			T t;

			_cur = rgh ?? alib.Collections.Collection<T>.None;

			var e = items.GetEnumerator();
			if (e.MoveNext())
			{
				if (cmp == null)
					cmp = EqualityComparer<T>.Default;

				do
					if (!arr.Contains(_cur, t = e.Current, cmp))
						_cur = arr.Append(_cur, t);
				while (e.MoveNext());
			}
			rgh = _cur;
		}

		/// <summary>
		/// You can use this extension method on a null 'this' value (i.e. properly-typed).
		/// A single-element array is returned. This function is thread-safe.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Prepend<T>(this T[] src, T item)
		{
			T[] dst;
			int c = src == null ? 0 : src.Length;
			(dst = new T[c + 1])[0] = item;
			while (c > 0)
				dst[c] = src[--c];
			return dst;
		}
		public static void Prepend<T>(ref T[] src, T item)
		{
			src = Prepend(src, item);
		}
		public static T[] Resize<T>(this T[] src, int c)
		{
			if (c == 0)
				return Collections.Collection<T>.None;

			if (src == null)
				return new T[c];

			int cc;
			if ((cc = src.Length) == c)
				return src;

			T[] tmp = new T[c];
			if (cc < c)
				c = cc;
			if (c < 350)
				while (--c >= 0)
					tmp[c] = src[c];
			else
				System.Array.Copy(src, 0, tmp, 0, c);

			return tmp;
		}

		public static void Resize<T>(ref T[] arr, int c) { arr = Resize(arr, c); }

		public static T[][] Divide<T>(this T[] src, int count)
		{
			if (count == 0)
				throw new Exception();

			int i, j, k, m;
			j = k = src.Length / count;
			if ((m = src.Length % count) != 0)
				j++;
			var ret = new T[j][];
			for (i = 0, j = 0; i < k; i++, j += count)
				SysArray.Copy(src, j, ret[i] = new T[count], 0, count);
			if (m != 0)
				SysArray.Copy(src, j, ret[i] = new T[m], 0, m);
			return ret;
		}

		/// <summary>
		/// You can use this extension method on a null 'this' value (i.e. properly-typed).
		/// The 'more' array (or Collection(T).None if null) is returned in this case.
		/// </summary>
		public static T[] Concat<T>(this T[] src, T[] more)
		{
			int c_src, c_more;
			if (more == null || (c_more = more.Length) == 0)
				return src ?? Collections.Collection<T>.None;

			if (src == null || (c_src = src.Length) == 0)
				return more ?? Collections.Collection<T>.None;

			T[] dst = new T[c_src + c_more];
			int i = 0;
			for (; i < c_src; i++)
				dst[i] = src[i];

			for (int j = 0; j < c_more; j++)
				dst[i++] = more[j];
			return dst;
		}

		public static void FilterFor<TArr, TFilter>(ref TArr[] arr)
			where TArr : class
			where TFilter : class, TArr
		{
			int c = arr.Length;
			if (c > 0)
			{
				int j = 0;
				for (int i = 0; i < c; i++)
					if (!(arr[i] is TFilter))
						j++;
				if (j > 0)
				{
					TFilter[] dst = new TFilter[c - j];
					TFilter t;
					j = 0;
					for (int i = 0; i < c; i++)
						if ((t = (arr[i] as TFilter)) != null)
							dst[j++] = t;
					arr = dst;
				}
			}
		}

		public static int IndexofAny<T>(this T[] arr, T[] items)
		{
			int ci;
			if ((ci = items.Length) > 0 && arr.Length > 0)
			{
				if (ci == 1)
					return System.Array.IndexOf(arr, items[0]);

				for (int i = 0; i < arr.Length; i++)
					if (System.Array.IndexOf<T>(items, arr[i]) != -1)
						return i;
			}
			return -1;
		}

		public static void InsertAt<T>(ref T[] arr, int idx, T t)
		{
			arr = InsertAt(arr, idx, t);
		}

		public static T[] InsertAt<T>(this T[] arr, int idx, T t)
		{
			int i, c, j;
			if (arr == null || idx == (c = arr.Length))
				return Append(arr, t);

			T[] newarr = new T[++c];
			for (i = 0, j = 0; i < c; i++)
				newarr[i] = i == idx ? t : arr[j++];
			return newarr;
		}

		public static void InsertRangeAt<T>(ref T[] rg0, int idx, T[] rg1)
		{
			rg0 = InsertRangeAt(rg0, idx, rg1);
		}
		public static T[] InsertRangeAt<T>(this T[] rg0, int idx, T[] rg1)
		{
			if ((uint)idx > (uint)rg0.Length)
				throw new IndexOutOfRangeException();
			if (idx == 0)
				return Concat(rg1, rg0);
			if (idx == rg0.Length)
				return Concat(rg0, rg1);

			int j, i;
			T[] ret = new T[rg0.Length + rg1.Length];
			for (i = j = 0; i < ret.Length; i++)
				ret[i] = i < idx || j == rg1.Length ? rg0[i - j] : rg1[j++];
			return ret;
		}

		public static void RemoveAt<T>(ref T[] arr, int idx)
		{
			arr = RemoveAt(arr, idx);
		}
		/// <summary> remember to re-store the return value of this extension </summary>
		public static T[] RemoveAt<T>(this T[] arr, int idx)
		{
			int c, i_src;
			if ((uint)idx >= (uint)(c = arr.Length))
				throw new IndexOutOfRangeException();

			if (c == 1)
				return alib.Collections.Collection<T>.None;

			T[] newarr = new T[c - 1];

			if (idx > 0)
				SysArray.Copy(arr, 0, newarr, 0, idx);		// part I.

			if ((c -= (i_src = idx + 1)) > 0)
				SysArray.Copy(arr, i_src, newarr, idx, c);	// part II.

			return newarr;
		}
		public static void RemoveOne<T>(ref T[] arr, T t, IEqualityComparer<T> cmp = null)
		{
			arr = RemoveOne(arr, t, cmp);
		}
		public static T[] RemoveOne<T>(this T[] arr, T t, IEqualityComparer<T> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<T>.Default;
			int j, i, c;
			if ((c = arr.Length) == 0)
				throw new ArgumentException();
			if (c == 1)
			{
				if (!cmp.Equals(t, arr[0]))
					throw new ArgumentException("value not found in the array");
				return alib.Collections.Collection<T>.None;
			}
			var newarr = new T[c - 1];
			for (i = 0, j = 0; j < newarr.Length; i++)
				if (i >= c)
					throw new ArgumentException("more than one match in the array");
				else if (!cmp.Equals(t, newarr[j] = arr[i]))
					j++;
			if (i < c && !cmp.Equals(t, arr[i]))
				throw new ArgumentException("value not found in the array");
			return newarr;
		}
		public static bool Remove<T>(ref T[] arr, T t)
		{
			if (arr != null)
			{
				int c = arr.Length;
				for (int i = 0; i < c; i++)
					if (arr[i].Equals(t))
					{
						arr = RemoveAt(arr, i);
						return true;
					}
			}
			return false;
		}
		public static T[] Remove<T>(this T[] arr, T t)
		{
			int c = arr.Length;
			for (int i = 0; i < c; i++)
				if (arr[i].Equals(t))
					return RemoveAt(arr, i);
			throw new Exception();
		}
		public static void RemoveSafe<T>(ref T[] arr, T t)
		{
			T[] _tmp;
			if ((_tmp = arr) != null)
				while (_tmp != (_tmp = Interlocked.CompareExchange(ref arr, remove_all(_tmp, t), _tmp)))
					;
		}

		static T[] remove_all<T>(T[] arr, T t)
		{
			T[] _new = null;
			int c, i, j;
			if ((c = arr.Length - 1) >= 0)
			{
				i = j = 0;
				if (c > 0)
					for (; i < c; i++)
						if (!arr[i].Equals(t))
							(_new ?? (_new = new T[c]))[j++] = arr[i];

				if (!arr[i].Equals(t))
				{
					if (i == j)
						return arr;
					(_new = _new.Resize(++j))[j - 1] = arr[i];
				}
				else if (j > 0 && j < c)
					Resize(ref _new, j);
			}
			return _new;
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// remember to re-store the return value of this extension
		/// Array.Resize always copies to a new array. Rather than sliding the original array and doing a 
		/// resize, we'll just create the new array ourselves and avoid Array.Resize.
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static T[] RemoveRange<T>(this T[] arr, int ix, int count)
		{
			int _arr, ix_resume;

			if ((uint)ix >= (uint)(_arr = arr.Length))
				throw new IndexOutOfRangeException();
			if (count == 0)
				return arr;
			if (count < 0 || (ix_resume = ix + count) > _arr)
				throw new ArgumentOutOfRangeException();
			if ((count = _arr - count) == 0)
				return alib.Collections.Collection<T>.None;

			T[] newarr = new T[count];

			count = 0;
			while (count < ix)
				newarr[count] = arr[count++];

			while (ix_resume < _arr)
				newarr[count++] = arr[ix_resume++];

			return newarr;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// In a list of objects of type T which are sorted in order based on type K, search for a value of type K.
		/// </summary>
		/// <remarks>
		/// http://www.removingalldoubt.com/permalink.aspx/f7e6feff-8257-4efe-ad64-acd1c7a4a1e3
		/// </remarks>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int BinarySearch<T, K>(this IReadOnlyList<T> list, K value, Converter<T, K> convert, Comparison<K> compare)
		{
			int i = 0;
			int j = list.Count - 1;
			while (i <= j)
			{
				int m = i + (j - i) / 2;
				int r = compare(convert(list[m]), value);
				if (r == 0)
					return m;
				if (r < 0)
					i = m + 1;
				else
					j = m - 1;
			}
			return ~i;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// In a list of objects of type T which are sorted an order based on type K, search for a value of type K.
		/// </summary>
		/// <typeparam name="T">Type the objects in the list</typeparam>
		/// <typeparam name="K">Type of the key by by which the objects in the list are sorted</typeparam>
		/// <param name="list">IList interface handle to a list of objects of type T</param>
		/// <param name="item">Object of type K to search for</param>
		/// <param name="convert">Function which derives a key of type K from an object of type T</param>
		/// <returns>
		/// The zero-based index of the item in the list if it is found; otherwise, a negative number that is 
		/// the bitwise complement of the index of the next element that is larger than item
		/// </returns>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int BinarySearch<T, K>(this IReadOnlyList<T> list, K item, Converter<T, K> convert)
			where K : IComparable<K>
		{
			return BinarySearch<T, K>(list, item, list.Count, convert);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Among the first 'c' elements of a list of objects of type T which are sorted an order based on type K, 
		/// search for a value of type K.
		/// </summary>
		/// <typeparam name="T">Type the objects in the list</typeparam>
		/// <typeparam name="K">Type of the key by by which the objects in the list are sorted</typeparam>
		/// <param name="list">IList interface handle to a list of objects of type T</param>
		/// <param name="item">Object of type K to search for</param>
		/// <param name="c">Number of objects at the beginning of the list to search within</param>
		/// <param name="convert">Function which derives a key of type K from an object of type T</param>
		/// <returns>
		/// The zero-based index of the item in the list if it is found; otherwise, a negative number that is 
		/// the bitwise complement of the index of the next element that is larger than item
		/// </returns>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int BinarySearch<T, K>(this IReadOnlyList<T> list, K item, int c, Converter<T, K> convert)
			where K : IComparable<K>
		{
			int i = 0;
			int j = c - 1;
			while (i <= j)
			{
				int m = i + (j - i) / 2;
				int r = convert(list[m]).CompareTo(item);
				if (r == 0)
					return m;
				if (r < 0)
					i = m + 1;
				else
					j = m - 1;
			}
			return ~i;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Swap<T>(this IList<T> rg, int i_l, int i_r)
		{
			if (i_l != i_r)
			{
				T _tmp = rg[i_r];
				rg[i_r] = rg[i_l];
				rg[i_l] = _tmp;
			}
		}
		public static void Swap<T>(this T[] rg, int a, int b)
		{
			if (a != b)
			{
				T _tmp = rg[b];
				rg[b] = rg[a];
				rg[a] = _tmp;
			}
		}

		[ThreadStatic]
		static Random rnd;

		public static void Shuffle<T>(this T[] arr)
		{
			if (rnd == null)
				rnd = new Random();
			int r, c = arr.Length;
			for (int i = 0; i < c; i++)
				if ((r = rnd.Next(c)) != i)
					_misc_ext.Swap(ref arr[i], ref arr[r]);
		}

		public static bool IsSorted<T>(this T[] arr, IComparer<T> cmp)
		{
			int c = arr.Length;
			if (c >= 2)
			{
				T item = arr[0];
				for (int i = 1; i < c; i++)
					if (cmp.Compare(item, item = arr[i]) > 0)
						return false;
			}
			return true;
		}

		public static bool IsSorted<T>(this T[] arr, int c, IComparer<T> cmp)
		{
			if (c >= 2)
			{
				T item = arr[0];
				for (int i = 1; i < c; i++)
					if (cmp.Compare(item, item = arr[i]) > 0)
						return false;
			}
			return true;
		}


		public static bool IsSorted<T>(this T[] arr, int i, int count, IComparer<T> cmp)
		{
			if (count > 1)
			{
				count += i;
				T i_prev = arr[i++];
				while (i < count)
					if (cmp.Compare(i_prev, i_prev = arr[i++]) > 0)
						return false;
			}
			return true;
		}

		public static unsafe int[] ToArray(int* pi, int c)
		{
			if (c == 0)
				return Enumerable.IntArray.Empty;
			var ret = new int[c];
			for (int i = 0; i < c; i++)
				ret[i] = *pi++;
			return ret;
		}

		public static void qsort<T>(this T[] rg, IComparer<T> cmp)
		{
			if (rg.Length > 1)
				qsort<T>(rg, 0, rg.Length - 1, cmp);
		}

		//static void swap_if<T>(T[] keys, IComparer<T> comparer, int a, int b)
		//{
		//	if (a != b && comparer.Compare(keys[a], keys[b]) > 0)
		//	{
		//		T tmp = keys[a];
		//		keys[a] = keys[b];
		//		keys[b] = tmp;
		//	}
		//}
#if true

		struct swap_help<T>
		{
			public IComparer<T> comparer;

			public void cmp_swap(ref T x, ref T y)
			{
				T tmp;
				if (comparer.Compare(tmp = x, y) != 0)
				{
					x = y;
					y = tmp;
				}
			}

			public void go(int f, int g, ref T a, ref T b, ref T c)
			{
				if (f != 0)
					cmp_swap(ref a, ref c);

				if (f != g)
					cmp_swap(ref a, ref b);

				if (0 != g)
					cmp_swap(ref c, ref b);
			}
		};

		static void swapper<T>(T[] keys, IComparer<T> comparer, int a, int b, int c)
		{
			swap_help<T> sh;
			sh.comparer = comparer;
			sh.go(a ^ c, c ^ b, ref keys[a], ref keys[b], ref keys[c]);
		}

#else
		static void swapper<T>(T[] keys, IComparer<T> comparer, int a, int b, int c)
		{
			T x, y;
			if (a != c && comparer.Compare(x = keys[a], y = keys[c]) > 0)
			{
				keys[a] = y;
				keys[c] = x;
			}

			if (a != b && comparer.Compare(x = keys[a], y = keys[b]) > 0)
			{
				keys[a] = y;
				keys[b] = x;
			}

			if (c != b && comparer.Compare(x = keys[c], y = keys[b]) > 0)
			{
				keys[c] = y;
				keys[b] = x;
			}
		}
#endif
		public static void qsort<T>(this T[] arr, int left, int right, IComparer<T> cmp)
		{
			do
			{
				int a = left, b = right, mid = a + ((b - a) >> 1);
#if swapper
				swapper(arr, cmp, a, b, mid);
#else
				swap_if(keys, cmp, a, mid);
				swap_if(keys, cmp, a, b);
				swap_if(keys, cmp, mid, b);
#endif
				T y = arr[mid];
				do
				{
					T ta;
					while (!(ta = arr[a]).Equals(y) && cmp.Compare(ta, y) < 0)
						a++;
					T tb = a == b ? ta : arr[b];
					while (!tb.Equals(y) && cmp.Compare(y, tb) < 0)
						tb = arr[--b];

					if (a > b)
						break;
					if (a < b)
					{
						arr[a] = tb;
						arr[b] = ta;
					}
					a++;
					b--;
				}
				while (a <= b);

				if ((b - left) <= (right - a))
				{
					if (left < b)
						qsort(arr, left, b, cmp);
					left = a;
				}
				else
				{
					if (a < right)
						qsort(arr, a, right, cmp);
					right = b;
				}
			}
			while (left < right);
		}

		static unsafe void swapper(IntPtr* keys, IComparer<IntPtr> comparer, int a, int b, int q)
		{
			IntPtr ta = IntPtr.Zero, tb = IntPtr.Zero, tq = IntPtr.Zero;
			if (a != q && comparer.Compare(ta = keys[a], tq = keys[q]) > 0)
			{
				keys[a] = tq;
				keys[q] = ta;
				ta = tq = default(IntPtr);
			}
			if (a != b && comparer.Compare(ta = ta != IntPtr.Zero ? ta : keys[a], tb = keys[b]) > 0)
			{
				keys[a] = tb;
				keys[b] = ta;
				tb = default(IntPtr);
			}
			if (q != b && comparer.Compare(tq = tq != IntPtr.Zero ? tq : keys[q], tb = tb != IntPtr.Zero ? tb : keys[b]) > 0)
			{
				keys[q] = tb;
				keys[b] = tq;
			}
		}

		public static unsafe void qsort(IntPtr* arr, int left, int right, IComparer<IntPtr> cmp)
		{
			do
			{
				int a = left, b = right, mid = a + ((b - a) >> 1);
#if swapper
				swapper(arr, cmp, a, b, mid);
#else
				swap_if(keys, cmp, a, mid);
				swap_if(keys, cmp, a, b);
				swap_if(keys, cmp, mid, b);
#endif
				IntPtr y = arr[mid];
				do
				{
					IntPtr ta;
					while ((ta = arr[a]) != y && cmp.Compare(ta, y) < 0)
						a++;
					IntPtr tb = a == b ? ta : arr[b];
					while (tb != y && cmp.Compare(y, tb) < 0)
						tb = arr[--b];

					if (a > b)
						break;
					if (a < b)
					{
						arr[a] = tb;
						arr[b] = ta;
					}
					a++;
					b--;
				}
				while (a <= b);

				if ((b - left) <= (right - a))
				{
					if (left < b)
						qsort(arr, left, b, cmp);
					left = a;
				}
				else
				{
					if (a < right)
						qsort(arr, a, right, cmp);
					right = b;
				}
			}
			while (left < right);
		}

		public static unsafe void swap_if(int* x, int* y)
		{
			if (x != y && *x > *y)
			{
				int tmp = *x;
				*x = *y;
				*y = tmp;
			}
		}

		public static unsafe void qsort(int* arr, int length)
		{
			if (--length > 0)
				qsort(arr, 0, length);
		}
		static unsafe void qsort(int* arr, int left, int right)
		{
			do
			{
				int a = left, b = right, mid = a + ((b - a) >> 1);

				swap_if(arr + a, arr + mid);
				swap_if(arr + a, arr + b);
				swap_if(arr + mid, arr + b);

				int y = arr[mid];
				do
				{
					int ta;
					while ((ta = arr[a]) < y)
						a++;
					int tb = a == b ? ta : arr[b];
					while (y < tb)
						tb = arr[--b];

					if (a > b)
						break;
					if (a < b)
					{
						arr[a] = tb;
						arr[b] = ta;
					}
					a++;
					b--;
				}
				while (a <= b);

				if (b - left <= right - a)
				{
					if (left < b)
						qsort(arr, left, b);
					left = a;
				}
				else
				{
					if (a < right)
						qsort(arr, a, right);
					right = b;
				}
			}
			while (left < right);
		}

		public static unsafe void qsort(int[] keys, int[] values)
		{
			int c;
			if ((c = keys.Length) != values.Length)
				throw new ArgumentException();
			if (--c > 0)
			{
#if INT_QSORT_UNSAFE
				fixed (int* _keys = keys, _values = values)
				{
					qsort(_keys, _values, 0, c);

					//_qs_struct qss;
					//qss.keys = _keys;
					//qss.values = _values;
					//qss.qsort(0, c);
				}
#else
				//qsort(keys, values, 0, c);
#endif
			}
		}

#if false
		unsafe struct _qs_struct
		{
			public int* keys, values;

			void swap_if(int* pkx, int* pky)
			{
				int tmp;
				int* p1, p2;
				if ((tmp = *pkx) > *pky)
				{
					*pkx = *pky;
					*pky = tmp;
					tmp = *(p1 = values + (pkx - keys));
					*p1 = *(p2 = values + (pky - keys));
					*p2 = tmp;
				}
			}

			public void qsort(int left, int right)
			{
				do
				{
					int a = left, b = right, mid = a + ((b - a) >> 1);

					if (a != mid)
						swap_if(keys + a, keys + mid);
					if (a != b)
						swap_if(keys + a, keys + b);
					if (mid != b)
						swap_if(keys + mid, keys + b);

					int y = keys[mid];
					do
					{
						int ta;
						while ((ta = keys[a]) < y)
							a++;
						int tb = a == b ? ta : keys[b];
						while (y < tb)
							tb = keys[--b];

						if (a > b)
							break;
						if (a < b)
						{
							keys[a] = tb;
							keys[b] = ta;
							int t = values[a];
							values[a] = values[b];
							values[b] = t;
						}
						a++;
						b--;
					}
					while (a <= b);

					if (b - left <= right - a)
					{
						if (left < b)
							qsort(left, b);
						left = a;
					}
					else
					{
						if (a < right)
							qsort(a, right);
						right = b;
					}
				}
				while (left < right);
			}
		};
#endif
#if !INT_QSORT_UNSAFE
		static void swap_if(int[] keys, int[] values, int x, int y)
		{
			int tmp;
			if (x != y && (tmp = keys[x]) > keys[y])
			{
				keys[x] = keys[y];
				keys[y] = tmp;
				tmp = values[x];
				values[x] = values[y];
				values[y] = tmp;
			}
		}

		static void swap_if(ref int kx, ref int ky, ref int vx, ref int vy)
		{
			int tmp;
			if ((tmp = kx) > ky)
			{
				kx = ky;
				ky = tmp;
				tmp = vx;
				vx = vy;
				vy = tmp;
			}
		}
		static void qsort(int[] keys, int[] values, int left, int right)
		{
			do
			{
				int a = left, b = right, mid = a + ((b - a) >> 1);

				if (a != mid)
					swap_if(ref keys[a], ref keys[mid], ref values[a], ref values[mid]);
				if (a != b)
					swap_if(ref keys[a], ref keys[b], ref values[a], ref values[b]);
				if (mid != b)
					swap_if(ref keys[mid], ref keys[b], ref values[mid], ref values[b]);

				int y = keys[mid];
				do
				{
					int ta;
					while ((ta = keys[a]) < y)
						a++;
					int tb = a == b ? ta : keys[b];
					while (y < tb)
						tb = keys[--b];

					if (a > b)
						break;
					if (a < b)
					{
						keys[a] = tb;
						keys[b] = ta;
						int t = values[a];
						values[a] = values[b];
						values[b] = t;
					}
					a++;
					b--;
				}
				while (a <= b);

				if (b - left <= right - a)
				{
					if (left < b)
						qsort(keys, values, left, b);
					left = a;
				}
				else
				{
					if (a < right)
						qsort(keys, values, a, right);
					right = b;
				}
			}
			while (left < right);
		}
#else
		static unsafe void swap_if(int* keys, int* values, int x, int y)
		{
			int tmp;
			if (x != y && (tmp = keys[x]) > keys[y])
			{
				keys[x] = keys[y];
				keys[y] = tmp;
				tmp = values[x];
				values[x] = values[y];
				values[y] = tmp;
			}
		}
		static unsafe void qsort(int* keys, int* values, int left, int right)
		{
			do
			{
				int a = left, b = right, mid = a + ((b - a) >> 1);

				swap_if(keys, values, a, mid);
				swap_if(keys, values, a, b);
				swap_if(keys, values, mid, b);

				int y = keys[mid];
				do
				{
					int ta;
					while ((ta = keys[a]) < y)
						a++;
					int tb = a == b ? ta : keys[b];
					while (y < tb)
						tb = keys[--b];

					if (a > b)
						break;
					if (a < b)
					{
						keys[a] = tb;
						keys[b] = ta;
						int t = values[a];
						values[a] = values[b];
						values[b] = t;
					}
					a++;
					b--;
				}
				while (a <= b);

				if (b - left <= right - a)
				{
					if (left < b)
						qsort(keys, values, left, b);
					left = a;
				}
				else
				{
					if (a < right)
						qsort(keys, values, a, right);
					right = b;
				}
			}
			while (left < right);
		}


		public static void InsertionSort(int[] keys, int[] values)
		{
			int c;
			if ((c = keys.Length) != values.Length)
				throw new ArgumentException();
			if (--c > 0)
			{
				//fixed (int* _keys = keys, _values = values)
				//	InsertionSort(_keys, _values, c);
				InsertionSort(keys, values, 0, c);
			}
		}
#if !dotnet
		static void InsertionSort(int[] keys, int[] values, int lo, int hi)
		{
			for (int i = lo; i < hi; i++)
			{
				int index = i;
				int x = keys[i + 1];
				int local2 = values[i + 1];
				while ((index >= lo) && x < keys[index])
				{
					keys[index + 1] = keys[index];
					values[index + 1] = values[index];
					index--;
				}
				keys[index + 1] = x;
				values[index + 1] = local2;
			}
		}
#else
		static void InsertionSort(int[] keys, int[] vals, int lo, int hi)
		{
			int i = lo, ix, k, v, tk;
			while (i < hi)
			{
				if ((tk = keys[i++]) > (k = keys[i]))
				{
					v = vals[ix = i];
					do
					{
						keys[ix] = tk;
						vals[ix] = vals[--ix];
					}
					while (ix > 0 && k < (tk = keys[ix - 1]));

					keys[ix] = k;
					vals[ix] = v;
				}
			}
		}
#endif
		//static void InsertionSort(int[] keys, int[] vals, int hi)
		//{
		//	int i = 1, index = 0, k, v;
		//	do
		//	{
		//		k = keys[i];
		//		v = vals[i];

		//		while (index >= 0 && k < keys[index])
		//		{
		//			keys[index + 1] = keys[index];
		//			vals[index + 1] = vals[index];
		//			index--;
		//		}
		//		keys[index + 1] = k;
		//		vals[index + 1] = v;
		//		index = i;
		//	}
		//	while (++i < hi);
		//}

		//static unsafe void InsertionSort(int* keys, int* vals, int c)
		//{
		//	int i,  k, v;
		//	int* pk=keys+1, pv=vals+1;
		//	//for (i = lo; i < hi; )
		//	int* pstop = keys + c;
		//	do
		//	{
		//		//pk = keys + i;
		//		//pv = vals + i;
		//		k = keys[i];
		//		v = vals[i];

		//		while (index >= lo && k < keys[index])
		//		{
		//			keys[index + 1] = keys[index];
		//			vals[index + 1] = vals[index];
		//			index--;
		//		}
		//		keys[index + 1] = k;
		//		vals[index + 1] = v;
		//	}
		//	while (
		//}

		static void Heapsort(int[] keys, int[] values, int lo, int hi)
		{
			int n = (hi - lo) + 1;
			for (int i = n / 2; i >= 1; i--)
			{
				DownHeap(keys, values, i, n, lo);
			}
			for (int j = n; j > 1; j--)
			{
				Swap(keys, values, lo, (lo + j) - 1);
				DownHeap(keys, values, 1, j - 1, lo);
			}
		}

		static void Swap(int[] keys, int[] values, int i, int j)
		{
			if (i != j)
			{
				int t = keys[i];
				keys[i] = keys[j];
				keys[j] = t;
				int local2 = values[i];
				values[i] = values[j];
				values[j] = local2;
			}
		}

		static void SwapIfGreaterWithItems(int[] keys, int[] values, int a, int b)
		{
			if (a != b && keys[a] > keys[b])
			{
				int t = keys[a];
				keys[a] = keys[b];
				keys[b] = t;
				int local2 = values[a];
				values[a] = values[b];
				values[b] = local2;
			}
		}

		static void DownHeap(int[] keys, int[] values, int i, int n, int lo)
		{
			int x = keys[(lo + i) - 1];
			int local2 = values[(lo + i) - 1];
			while (i <= (n / 2))
			{
				int num = 2 * i;
				if ((num < n) && keys[(lo + num) - 1] < keys[lo + num])
					num++;

				if (x >= keys[(lo + num) - 1])
					break;

				keys[(lo + i) - 1] = keys[(lo + num) - 1];
				values[(lo + i) - 1] = values[(lo + num) - 1];

				i = num;
			}
			keys[(lo + i) - 1] = x;
			values[(lo + i) - 1] = local2;
		}

		static int FloorLog2(int n)
		{
			int num = 0;
			while (n >= 1)
			{
				num++;
				n /= 2;
			}
			return num;
		}

		public static void IntrospectiveSort(int[] keys, int[] values/*, int left, int length*/)
		{
			int length;
			if ((length = keys.Length) >= 2)
				IntroSort(keys, values, 0, length - 1, 2 * FloorLog2(length));
		}

		static void IntroSort(int[] keys, int[] values, int lo, int hi, int depthLimit)
		{
			while (hi > lo)
			{
				int num = (hi - lo) + 1;
				if (num <= 0x10)
				{
					switch (num)
					{
						case 1:
							return;

						case 2:
							SwapIfGreaterWithItems(keys, values, lo, hi);
							return;

						case 3:
							SwapIfGreaterWithItems(keys, values, lo, hi - 1);
							SwapIfGreaterWithItems(keys, values, lo, hi);
							SwapIfGreaterWithItems(keys, values, hi - 1, hi);
							return;
					}
					InsertionSort(keys, values, lo, hi);
					return;
				}
				if (depthLimit == 0)
				{
					Heapsort(keys, values, lo, hi);
					return;
				}
				depthLimit--;
				int num2 = PickPivotAndPartition(keys, values, lo, hi);
				IntroSort(keys, values, num2 + 1, hi, depthLimit);
				hi = num2 - 1;
			}
		}

		static int PickPivotAndPartition(int[] keys, int[] values, int lo, int hi)
		{
			int i, j;
			int mid = lo + ((hi - lo) / 2);
			SwapIfGreaterWithItems(keys, values, lo, mid);
			SwapIfGreaterWithItems(keys, values, lo, hi);
			SwapIfGreaterWithItems(keys, values, mid, hi);
			int y = keys[mid];
			Swap(keys, values, mid, hi - 1);
			i = lo;
			j = hi - 1;
			while (i < j)
			{
				while (keys[++i] < y)
				{
				}
				while (y < keys[--j])
				{
				}
				if (i >= j)
					break;
				Swap(keys, values, i, j);
			}
			Swap(keys, values, i, hi - 1);
			return i;
		}
#endif
	};
}
