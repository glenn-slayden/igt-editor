using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using alib.Array;

namespace alib.Collections
{
	using Array = System.Array;

	[DebuggerDisplay("Count = {this.m_c}  Type = {_item_type().Name,nq}")]
	public class RefList<T> : alib.Enumerable._IList<T>, IList<T>, IList, alib.Enumerable.IAddRangeColl<T> //where T : class
	{
		public static readonly RefList<T> Empty;

		static RefList()
		{
			Empty = new RefList<T>(Collection<T>.None, 0);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		T[] m_arr;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int m_c;

		[DebuggerStepThrough]
		public RefList(T[] usurp, int count)
		{
			m_arr = usurp;
			m_c = count;
		}
		[DebuggerStepThrough]
		public RefList(int capacity = -1, int count = 0)
			: this(new T[capacity < 0 ? 4 : capacity], count)
		{
		}
		[DebuggerStepThrough]
		public RefList(T[] usurp)
			: this(usurp, usurp.Length)
		{
		}
		[DebuggerStepThrough]
		public RefList(IEnumerable<T> src, int capacity = -1)
			: this(capacity, 0)
		{
			var e = src.GetEnumerator();
			while (e.MoveNext())
				Add(e.Current);
		}
		[DebuggerStepThrough]
		public RefList(IReadOnlyCollection<T> src)
			: this(src, src.Count)
		{
		}
		[DebuggerStepThrough]
		public RefList(ICollection<T> src)
			: this(src, src.Count)
		{
		}
		[DebuggerStepThrough]
		public RefList(IEnumerable src, int capacity = -1)
			: this(capacity, 0)
		{
			var e = src.GetEnumerator();
			while (e.MoveNext())
				Add((T)e.Current);
		}
		[DebuggerStepThrough]
		public RefList(ICollection src)
			: this(src, src.Count)
		{
		}

		[DebuggerStepThrough]
		public T[] GetUntrimmed(out int c)
		{
			c = m_c;
			return m_arr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
		public T[] GetUntrimmed() { return m_arr; }

		[DebuggerStepThrough]
		public T[] GetTrimmed()
		{
			T[] _tmp;
			int c;
			if ((_tmp = this.m_arr).Length == (c = this.m_c))
				return _tmp;

			var _new = new T[c];
			for (int i = 0; i < c; i++)
				_new[i] = _tmp[i];
			return _new;
		}

		[DebuggerStepThrough]
		public T[] ToArray()
		{
			T[] tmp = new T[m_c];
			Array.Copy(m_arr, 0, tmp, 0, m_c);
			return tmp;
		}

		[DebuggerStepThrough]
		void EnsureCapacity(int min)
		{
			if (m_arr.Length < min)
			{
				int i = m_arr.Length == 0 ? 4 : (m_arr.Length << 1);
				if (i < min)
					i = min;

				this.Capacity = i;
			}
		}

		[DebuggerStepThrough]
		public void TrimExcess()
		{
			if (m_c < m_arr.Length)
				arr.Resize(ref m_arr, m_c);
		}

		[DebuggerStepThrough]
		public void SetCount(int c)
		{
			EnsureCapacity(c);
			m_c = c;
		}

		[DebuggerStepThrough]
		public int AllocateNew(int c = 1)
		{
			int i;
			EnsureCapacity(c += (i = m_c));
			m_c = c;
			return i;
		}

		public int Capacity
		{
			[DebuggerStepThrough]
			get { return m_arr.Length; }

			[DebuggerStepThrough]
			set
			{
				if (value < m_c)
					throw new ArgumentException();

				arr.Resize(ref m_arr, value);
			}
		}

		[DebuggerStepThrough]
		public void AddRange(IEnumerable<T> collection)
		{
			InsertRange(m_c, collection);
		}

		[DebuggerStepThrough]
		public ReadOnlyCollection<T> AsReadOnly()
		{
			return new ReadOnlyCollection<T>(this);
		}

		[DebuggerStepThrough]
		public int BinarySearch(T item)
		{
			return BinarySearch(0, m_c, item, null);
		}

		[DebuggerStepThrough]
		public int BinarySearch(T item, IComparer<T> comparer)
		{
			return BinarySearch(0, m_c, item, comparer);
		}

		[DebuggerStepThrough]
		public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();

			return Array.BinarySearch<T>(m_arr, index, count, item, comparer);
		}

		[DebuggerStepThrough]
		public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> fn)
		{
			if (fn == null)
				throw new ArgumentNullException();

			List<TOutput> list = new List<TOutput>(m_c);
			for (int i = 0; i < m_c; i++)
				list.Add(fn(m_arr[i]));
			return list;
		}

		[DebuggerStepThrough]
		public void CopyTo(T[] dst)
		{
			Array.Copy(m_arr, 0, dst, 0, m_c);
		}
		[DebuggerStepThrough]
		public void CopyTo(T[] dst, int i_dst)
		{
			Array.Copy(m_arr, 0, dst, i_dst, m_c);
		}

		[DebuggerStepThrough]
		public void CopyTo(int i_src, T[] dst, int i_dst, int count)
		{
			if ((m_c - i_src) < count)
				throw new ArgumentException();

			Array.Copy(m_arr, i_src, dst, i_dst, count);
		}

		[DebuggerStepThrough]
		public bool TrueForAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			for (int i = 0; i < m_c; i++)
				if (!match(m_arr[i]))
					return false;
			return true;
		}

		[DebuggerStepThrough]
		public bool Exists(Predicate<T> match)
		{
			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return true;
			return false;
		}

		[DebuggerStepThrough]
		public bool Any(Predicate<T> match)
		{
			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return true;
			return false;
		}

		[DebuggerStepThrough]
		public T Find(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return m_arr[i];
			return default(T);
		}

		[DebuggerStepThrough]
		public RefList<T> FindAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			T t;
			int i;
			for (i = 0; i < m_c; i++)
				if (match(t = m_arr[i]))
					goto setup;
			return this;
		setup:
			T[] _new;
			(_new = new T[m_c])[0] = t;
			int j = 1;
			while (++i < m_c)
				if (match(t = m_arr[i]))
					_new[j++] = t;
			return new RefList<T>(_new, j);
		}

		[DebuggerStepThrough]
		public int FindIndex(Predicate<T> match)
		{
			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return i;
			return -1;
		}

		[DebuggerStepThrough]
		public int FindIndex(int i_start, Predicate<T> match)
		{
			return FindIndex(i_start, m_c - i_start, match);
		}

		[DebuggerStepThrough]
		public int FindIndex(int i_start, int count, Predicate<T> match)
		{
			if (i_start > m_c || count < 0 || i_start > m_c - count)
				throw new ArgumentOutOfRangeException();

			if (match == null)
				throw new ArgumentNullException();

			int i_end = i_start + count;
			for (int i = i_start; i < i_end; i++)
				if (match(m_arr[i]))
					return i;
			return -1;
		}

		[DebuggerStepThrough]
		public T FindLast(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			for (int i = m_c - 1; i >= 0; i--)
				if (match(m_arr[i]))
					return m_arr[i];
			return default(T);
		}

		[DebuggerStepThrough]
		public int FindLastIndex(Predicate<T> match)
		{
			return FindLastIndex(m_c - 1, m_c, match);
		}

		[DebuggerStepThrough]
		public int FindLastIndex(int startIndex, Predicate<T> match)
		{
			return FindLastIndex(startIndex, startIndex + 1, match);
		}

		[DebuggerStepThrough]
		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();
			if (m_c == 0)
			{
				if (startIndex != -1)
					throw new ArgumentOutOfRangeException();
			}
			else if (startIndex >= m_c)
				throw new ArgumentOutOfRangeException();

			if ((count < 0) || (((startIndex - count) + 1) < 0))
				throw new ArgumentOutOfRangeException();

			int num = startIndex - count;
			for (int i = startIndex; i > num; i--)
				if (match(m_arr[i]))
					return i;
			return -1;
		}

		[DebuggerStepThrough]
		public void ForEach(Action<T> action)
		{
			if (action == null)
				throw new ArgumentNullException();

			for (int i = 0; i < m_c; i++)
				action(m_arr[i]);
		}

		[DebuggerStepThrough]
		public RefList<T> GetRange(int index, int count)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();

			RefList<T> list = new RefList<T>(count);
			Array.Copy(m_arr, index, list.m_arr, 0, count);
			list.m_c = count;
			return list;
		}

		[DebuggerStepThrough]
		public int IndexOf(T item, int index)
		{
			if (index > m_c)
				throw new ArgumentOutOfRangeException();

			return Array.IndexOf<T>(m_arr, item, index, m_c - index);
		}

		[DebuggerStepThrough]
		public int IndexOf(T item, int index, int count)
		{
			if (index > m_c || (count < 0) || (index > (m_c - count)))
				throw new ArgumentOutOfRangeException();

			return Array.IndexOf<T>(m_arr, item, index, count);
		}

		[DebuggerStepThrough]
		public void InsertRange(int index, IEnumerable<T> src)
		{
			if (src == null)
				throw new ArgumentNullException();
			if (index > m_c)
				throw new ArgumentOutOfRangeException();

			ICollection c0;
			ICollection<T> c1;
			IReadOnlyCollection<T> c2;

			int c;
			if ((c1 = src as ICollection<T>) != null)
				c = c1.Count;
			else if ((c0 = src as ICollection) != null)
				c = c0.Count;
			else if ((c2 = src as IReadOnlyCollection<T>) != null)
				c = c2.Count;
			else
			{
				var e = src.GetEnumerator();
				while (e.MoveNext())
					Add(e.Current);
				return;
			}

			if (c < 0)
				return;

			EnsureCapacity(m_c + c);

			if (index < m_c)
				Array.Copy(m_arr, index, m_arr, index + c, m_c - index);

			if (this == src)
			{
				Array.Copy(m_arr, 0, m_arr, index, index);
				Array.Copy(m_arr, index + c, m_arr, index << 1, m_c - index);
			}
			else
			{
				var e = src.GetEnumerator();
				while (e.MoveNext())
					m_arr[index++] = e.Current;
			}
			m_c += c;
		}

		[DebuggerStepThrough]
		public int LastIndexOf(T item)
		{
			if (m_c == 0)
				return -1;
			return LastIndexOf(item, m_c - 1, m_c);
		}

		[DebuggerStepThrough]
		public int LastIndexOf(T item, int index)
		{
			if (index >= m_c)
				throw new ArgumentOutOfRangeException();

			return LastIndexOf(item, index, index + 1);
		}

		[DebuggerStepThrough]
		public int LastIndexOf(T item, int index, int count)
		{
			if (index < 0 || count < 0)
				throw new ArgumentOutOfRangeException();
			if (m_c == 0)
				return -1;
			if (index >= m_c || count > (index + 1))
				throw new ArgumentOutOfRangeException();

			return Array.LastIndexOf<T>(m_arr, item, index, count);
		}

		[DebuggerStepThrough]
		public int RemoveAll(int src, Predicate<T> _remove)
		{
			int dst = 0;
			while (src < m_c)
			{
				T t;
				if (!_remove(t = m_arr[src]))
				{
					if (src != dst)
						m_arr[dst] = t;
					dst++;
				}
				src++;
			}
			if ((src -= dst) > 0)
				m_c = dst;
			return src;
		}

		[DebuggerStepThrough]
		public T RemoveItem(T item)
		{
			int ix = Array.IndexOf<T>(m_arr, item, 0, m_c);
			if (ix == -1)
				return default(T);
			T t = m_arr[ix];

			m_c--;
			Array.Copy(m_arr, ix + 1, m_arr, ix, m_c - ix);
			m_arr[m_c] = default(T);
			return t;
		}

		[DebuggerStepThrough]
		public T RemoveFirst(Predicate<T> _remove)
		{
			for (int i = 0; i < m_c; i++)
			{
				T t;
				if (_remove(t = m_arr[i]))
				{
					m_c--;
					Array.Copy(m_arr, i + 1, m_arr, i, m_c - i);
					m_arr[m_c] = default(T);
					return t;
				}
			}
			return default(T);
		}

		[DebuggerStepThrough]
		public int RemoveAll(Predicate<T> _remove)
		{
#if true
			int src = 0, dst = 0;
			while (src < m_c)
			{
				T t;
				if (!_remove(t = m_arr[src]))
				{
					if (src != dst)
						m_arr[dst] = t;
					dst++;
				}
				src++;
			}
			if ((src -= dst) > 0)
				m_c = dst;
			return src;
#else
			if (match == null)
				throw new ArgumentNullException();

			int index = 0;
			while ((index < m_c) && !match(m_arr[index]))
				index++;

			if (index >= m_c)
				return 0;

			int i = index + 1;
			while (i < m_c)
			{
				while ((i < m_c) && match(m_arr[i]))
					i++;

				if (i < m_c)
					m_arr[index++] = m_arr[i++];

			}
			Array.Clear(m_arr, index, m_c - index);
			int j = m_c - index;
			m_c = index;
			return j;
#endif
		}

		[DebuggerStepThrough]
		public void RemoveRange(int index, int count)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();

			if (count > 0)
			{
				m_c -= count;
				if (index < m_c)
					Array.Copy(m_arr, index + count, m_arr, index, m_c - index);
				Array.Clear(m_arr, m_c, count);
			}
		}

		[DebuggerStepThrough]
		public void Reverse()
		{
			Array.Reverse(m_arr, 0, m_c);
		}

		[DebuggerStepThrough]
		public void Reverse(int index, int count)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();
			Array.Reverse(m_arr, index, count);
		}

		[DebuggerStepThrough]
		public bool IsSorted(IComparer<T> cmp)
		{
			if (m_c >= 2)
			{
				T item = m_arr[0];
				for (int i = 1; i < m_c; i++)
					if (cmp.Compare(item, item = m_arr[i]) > 0)
						return false;
			}
			return true;
		}

		[DebuggerStepThrough]
		public void Sort()
		{
			//Sort(0, c, null);
			alib.Array.arr.qsort(m_arr, 0, m_c - 1, Comparer<T>.Default);
		}

		[DebuggerStepThrough]
		public void Sort(IComparer<T> comparer)
		{
			//Sort(0, m_c, comparer);
			alib.Array.arr.qsort(m_arr, 0, m_c - 1, comparer);
		}

		//public void Sort(Comparison<T> comparison)
		//{
		//    if (comparison == null)
		//        throw new ArgumentNullException();

		//    if (_size > 0)
		//    {
		//        IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
		//        Array.Sort<T>(_items, 0, _size, comparer);
		//    }
		//}

		[DebuggerStepThrough]
		public void Sort(int index, int count, IComparer<T> comparer)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();
			//Array.Sort<T>(m_arr, index, count, comparer);
			alib.Array.arr.qsort(m_arr, index, count - 1, comparer);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// IList(T)
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public int Count { [DebuggerStepThrough] get { return m_c; } }

		public T this[int index]
		{
			[DebuggerStepThrough]
			get
			{
				if (index >= m_c)
					throw new ArgumentOutOfRangeException();
				return m_arr[index];
			}
			[DebuggerStepThrough]
			set
			{
				if (index >= m_c)
					throw new ArgumentOutOfRangeException();
				m_arr[index] = value;
			}
		}

		[DebuggerStepThrough]
		public int IndexOf(T item)
		{
			return Array.IndexOf<T>(m_arr, item, 0, m_c);
		}

		[DebuggerStepThrough]
		public bool Contains(T item)
		{
			if (item == null)
			{
				for (int j = 0; j < m_c; j++)
					if (m_arr[j] == null)
						return true;
				return false;
			}
			var comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < m_c; i++)
				if (comparer.Equals(m_arr[i], item))
					return true;
			return false;
		}

		[DebuggerStepThrough]
		public void Add(T item)
		{
			if (m_c >= m_arr.Length)
				EnsureCapacity(m_c + 1);

			m_arr[m_c++] = item;
		}

		[DebuggerStepThrough]
		public void Insert(int index, T item)
		{
			if (index > m_c)
				throw new ArgumentOutOfRangeException();

			if (m_c == m_arr.Length)
				EnsureCapacity(m_c + 1);

			if (index < m_c)
				Array.Copy(m_arr, index, m_arr, index + 1, m_c - index);

			m_arr[index] = item;
			m_c++;
		}

		[DebuggerStepThrough]
		public bool Remove(T item)
		{
			int index;
			if ((index = IndexOf(item)) < 0)
				return false;
			RemoveAt(index);
			return true;
		}

		[DebuggerStepThrough]
		public void RemoveAt(int index)
		{
			if (index >= m_c)
				throw new ArgumentOutOfRangeException();

			m_c--;
			if (index < m_c)
				Array.Copy(m_arr, index + 1, m_arr, index, m_c - index);

			m_arr[m_c] = default(T);
		}

		[DebuggerStepThrough]
		public void Clear()
		{
			if (m_c > 0)
			{
				Array.Clear(m_arr, 0, m_c);
				m_c = 0;
			}
		}

		[DebuggerStepThrough]
		public IEnumerator<T> GetEnumerator()
		{
			if (m_c == m_arr.Length)
				return ((IEnumerable<T>)m_arr).GetEnumerator();
			return m_arr.Take(m_c).GetEnumerator();
		}

		[DebuggerStepThrough]
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsReadOnly { [DebuggerStepThrough] get { return false; } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// IList
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		Object IList.this[int index]
		{
			[DebuggerStepThrough]
			get { return this[index]; }
			[DebuggerStepThrough]
			set { this[index] = (T)value; }
		}

		[DebuggerStepThrough]
		int IList.IndexOf(Object item)
		{
			return item is T ? IndexOf((T)item) : -1;
		}

		[DebuggerStepThrough]
		bool IList.Contains(Object item)
		{
			return item is T && Contains((T)item);
		}

		[DebuggerStepThrough]
		int IList.Add(Object item)
		{
			Add((T)item);
			return m_c - 1;
		}

		[DebuggerStepThrough]
		void IList.Insert(int index, Object item)
		{
			Insert(index, (T)item);
		}

		[DebuggerStepThrough]
		void IList.Remove(Object item)
		{
			if (item is T)
				Remove((T)item);
		}

		[DebuggerStepThrough]
		void ICollection.CopyTo(Array array, int index)
		{
			if (array != null && array.Rank != 1)
				throw new ArgumentException();
			Array.Copy(m_arr, 0, array, index, m_c);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IList.IsFixedSize { [DebuggerStepThrough] get { return false; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized { [DebuggerStepThrough] get { return false; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Object ICollection.SyncRoot { [DebuggerStepThrough] get { return this; } }

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		T[] _dbg_display { [DebuggerStepThrough] get { return this.ToArray(); } }

		Type _item_type() { return typeof(T); }
	};
}
