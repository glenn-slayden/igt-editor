using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using alib.Enumerable;

namespace alib.Collections.ReadOnly
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class _ro_coll_base<T>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsReadOnly { get { return true; } }
		public void Add(T item) { throw not.valid; }
		public bool Remove(T item) { throw not.valid; }
		public void Clear() { throw not.valid; }
		/// add ICollection for covariance
		public void CopyTo(System.Array array, int index) { throw not.valid; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsSynchronized { get { throw not.valid; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public object SyncRoot { get { throw not.valid; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class _ro_list_base<T> : _ro_coll_base<T>
	{
		public void Insert(int index, T item) { throw not.valid; }
		public void RemoveAt(int index) { throw not.valid; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class _ro_coll_enum_base<T> : _ro_coll_base<T>
	{
		public _ro_coll_enum_base(IEnumerable<T> ie, int c)
		{
#if DEBUG
			if (c == 0)
				throw new Exception();
#endif
			this.ie = ie;
			this.c = c;
		}
		public int Count { get { return c; } }
		protected readonly IEnumerable<T> ie;
		protected int c;
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class ArrayWrapper<T> : _ro_coll_base<T>, IReadOnlyList<T>, _ICollection<T>
	{
		public ArrayWrapper(T[] arr) { this.arr = arr; }
		public ArrayWrapper(IEnumerable<T> seq, int count)
		{
			this.arr = new T[count];
			int i = 0;
			foreach (T t in seq)
				arr[i++] = t;
		}
		public ArrayWrapper(ICollection<T> coll)
			: this(coll, coll.Count)
		{
		}
		public ArrayWrapper(ICollection coll)
		{
			this.arr = new T[coll.Count];
			int i = 0;
			foreach (T t in coll)
				arr[i++] = t;
		}
		public ArrayWrapper(IEnumerable<T> en) { arr = en.ToArray(); }

		protected readonly T[] arr;

		public int Count { get { return arr.Length; } }
		public T this[int ix] { get { return arr[ix]; } }
		public int IndexOf(T item) { return System.Array.IndexOf<T>(arr, item); }
		public bool Contains(T item) { return System.Array.IndexOf<T>(arr, item) != -1; }
		public void CopyTo(T[] array, int arrayIndex) { arr.CopyTo(array, arrayIndex); }

		public IEnumerator<T> GetEnumerator() { return arr.Enumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return arr.GetEnumerator(); }

		public static explicit operator T[](ArrayWrapper<T> ro)
		{
			return ro.arr;
		}
		public static explicit operator ArrayWrapper<T>(T[] seq)
		{
			return new ArrayWrapper<T>(seq);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	sealed class _coll_take<T> : _ro_coll_enum_base<T>, _ICollection<T>, ICollection
	{
		public _coll_take(ICollection<T> seq, int c)
			: base(seq, c)
		{
#if DEBUG
			if (c >= seq.Count)
				throw new Exception();
#endif
		}

		public bool Contains(T item)
		{
			throw not.impl;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			T[] rg;
			if ((rg = ie as T[]) != null)
				System.Array.Copy(rg, 0, array, arrayIndex, c);
			else
			{
				var e = ie.GetEnumerator();
				for (int take = c; take > 0 && e.MoveNext(); take--)
					array[arrayIndex++] = e.Current;
			}
		}

		public IEnumerator<T> GetEnumerator() { return c == 0 ? Collection<T>.NoneEnumerator : new _enum(this); }
		IEnumerator IEnumerable.GetEnumerator() { return c == 0 ? Collection<T>.NoneEnumerator : new _enum(this); }
		class _enum : IEnumerator<T>
		{
			public _enum(_coll_take<T> ct)
			{
				this.e = ct.ie.GetEnumerator();
				this.take = ct.c;
			}
			IEnumerator<T> e;
			int take;
			public bool MoveNext()
			{
				bool b = (take > 0 && e.MoveNext());
				if (b)
					take--;
				return b;
			}
			public T Current { get { return e.Current; } }
			object IEnumerator.Current { get { return e.Current; } }
			public void Reset() { throw not.impl; }
			public void Dispose() { e.Dispose(); e = null; }
		};
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class _coll_defer<T> : _ICollection<T>
	{
		public _coll_defer(IEnumerable<T> ie, int c)
		{
			if (c == 0 || ie is _ICollection<T>)
				throw new Exception();
			this.ie = ie;
			this.m_count = c == -1 ? ie.CountIfAvail() : c;
		}
		public _coll_defer(IEnumerable<T> ie) : this(ie, -1) { }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count
		{
			get
			{
				if (m_count == -1)
					m_count = ie._Count();
				return m_count;
			}
		}
		readonly IEnumerable<T> ie;
		int m_count;

		public IEnumerator<T> GetEnumerator()
		{
			if (m_count > 0)
				return ie.GetEnumerator();
			if (m_count == 0)
				return Collection<T>.NoneEnumerator;
			return e();
		}
		IEnumerator<T> e()
		{
			int i = 0;
			var e = ie.GetEnumerator();
			while (e.MoveNext())
			{
				yield return e.Current;
				i++;
			}
			m_count = i;
		}
		public bool Contains(T item)
		{
			if (m_count == 0)
				return false;
			var e = GetEnumerator();
			while (e.MoveNext())
				if (e.Current.Equals(item))
					return true;
			return false;
		}
		public void CopyTo(T[] array, int ix)
		{
			if (m_count == 0)
				return;
			var e = GetEnumerator();
			while (e.MoveNext())
				array[ix++] = e.Current;
		}
		void ICollection.CopyTo(System.Array array, int ix)
		{
			if (m_count == 0)
				return;
			var e = GetEnumerator();
			while (e.MoveNext())
				array.SetValue(e.Current, ix++);
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		void ICollection<T>.Add(T item) { throw not.valid; }
		bool ICollection<T>.Remove(T item) { throw not.valid; }
		void ICollection<T>.Clear() { throw not.valid; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection<T>.IsReadOnly { get { return true; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized { get { throw not.impl; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Object ICollection.SyncRoot { get { throw not.impl; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[System.Diagnostics.DebuggerDisplay("Count: {Count}")]
	public unsafe class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, _ICollection<KeyValuePair<TKey, TValue>>
	{
		public static readonly ReadOnlyDictionary<TKey, TValue> Empty;

		static ReadOnlyDictionary()
		{
			Empty = new ReadOnlyDictionary<TKey, TValue>();
		}

		[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
		struct Entry
		{
			public TKey key;
			public TValue value;
			public uint next;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public KeyValuePair<TKey, TValue> kvp
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get { return new KeyValuePair<TKey, TValue>(key, value); }
			}

			public override string ToString()
			{
				System.String s2 = next == Term ? "(last)" : next == Unused ? "(unused)" : next.ToString();
				//if (typeof(TValue).UnderlyingSystemType == typeof(int))
				//    return System.String.Format("{0} {1:X8} {2}", key, value, s2);
				//else
				return System.String.Format("key: {0}  value: {1}  next: {2}", key, value, s2);
			}
		};

		readonly IEqualityComparer<TKey> m_cmp;
		readonly int count;
		readonly Entry[] entries;

		const uint Term = unchecked((uint)-1);
		const uint Unused = unchecked((uint)-2);

		struct _initializer
		{
			readonly IEqualityComparer<TKey> cmp;
			readonly Entry[] entries;

			readonly Entry[] rgdefer;
			int i_defer;

			readonly int c_alloc;
			readonly ulong* rgul;

			bool avail(uint i)
			{
				return (rgul[i >> 6] & (1UL << (int)i)) == 0;
			}
			void set(uint i)
			{
				ulong* p = rgul + (i >> 6);
				*p = *p | (1UL << ((int)i & 0x3F));
			}

			public _initializer(ReadOnlyDictionary<TKey, TValue> _d, Object e, Func<TKey, TValue> value_selector)
			{
				this.cmp = _d.m_cmp;
				this.c_alloc = (this.entries = _d.entries).Length;
				this.rgdefer = new Entry[_d.count];
				this.i_defer = 0;

				ulong* _rgul = stackalloc ulong[((c_alloc - 1) >> 6) + 1];
				this.rgul = _rgul;

				if (value_selector == null)
					fill_slots_kvp((IEnumerator<KeyValuePair<TKey, TValue>>)e);
				else if (e is TKey[])
					fill_slots_arr((TKey[])e, value_selector);
				else
					fill_slots_k((IEnumerator<TKey>)e, value_selector);

				uint i = resolve_collisions();
				for (; i < c_alloc; i++)
					if (avail(i))
						entries[i].next = Unused;
			}

			void fill_slots_kvp(IEnumerator<KeyValuePair<TKey, TValue>> e)
			{
				KeyValuePair<TKey, TValue> kvp;
				while (e.MoveNext())
					hash_store((kvp = e.Current).Key, kvp.Value);
			}

			void fill_slots_k(IEnumerator<TKey> e, Func<TKey, TValue> value_selector)
			{
				TKey k;
				while (e.MoveNext())
					hash_store(k = e.Current, value_selector(k));
			}

			void fill_slots_arr(TKey[] rg, Func<TKey, TValue> value_selector)
			{
				TKey k;
				for (int i = 0; i < c_alloc; i++)
					hash_store(k = rg[i], value_selector(k));
			}

			void hash_store(TKey k, TValue v)
			{
				uint ix = (uint)cmp.GetHashCode(k) % (uint)c_alloc;
				if (avail(ix))
				{
					store(ref entries[ix], k, v, Term);
					set(ix);
				}
				else
				{
					store(ref rgdefer[i_defer], k, v, ix);
					i_defer++;
				}
			}

			static void store(ref Entry d, TKey k, TValue v, uint ix)
			{
				d.key = k;
				d.value = v;
				d.next = ix;
			}

			static uint find_store(Entry[] entries, ref Entry e, Entry d)
			{
				uint n, ix = d.next;
				while ((int)(n = entries[ix].next) >= 0)
					ix = n;
				e = d;
				e.next = Term;
				return ix;
			}

			uint resolve_collisions()
			{
				uint i = 0;
				for (int j = 0; i < c_alloc && j < i_defer; i++)
					if (avail(i))
						entries[find_store(entries, ref entries[i], rgdefer[j++])].next = i;
				return i;
			}
		};

		ReadOnlyDictionary(IEqualityComparer<TKey> cmp)
		{
			this.m_cmp = cmp ?? EqualityComparer<TKey>.Default;
			this.count = 0;
		}
		ReadOnlyDictionary(int c, Object items, Func<TKey, TValue> value_selector, IEqualityComparer<TKey> cmp)
			: this(cmp)
		{
			IEnumerator<TKey> e0;
			IEnumerable<TKey> e1 = null;
			IEnumerable<KeyValuePair<TKey, TValue>> e2 = null;
			TKey[] a = null;

			if ((e0 = items as IEnumerator<TKey>) != null)
			{
				if (c < 0)
					throw new ArgumentException("Item count must be provided when supplying enumerator.");
			}
			else if ((a = items as TKey[]) != null)
			{
				c = a.Length;
			}
			else if ((e2 = items as IEnumerable<KeyValuePair<TKey, TValue>>) != null)
			{
				if (c < 0)
					c = alib.Enumerable._enumerable_ext._Count(e2);
			}
			else if ((e1 = items as IEnumerable<TKey>) != null)
			{
				if (c < 0)
					c = alib.Enumerable._enumerable_ext._Count(e1);
			}
			else
				throw new ArgumentException("Input structure has an unrecognized sequence.");

			if ((this.count = c) == 0)
			{
				this.entries = Collection<Entry>.None;
			}
			else
			{
				this.entries = new Entry[Math.primes.HashFriendly(count)];

				if (e0 != null)
					new _initializer(this, e0, value_selector);
				else if (a != null)
					new _initializer(this, a, value_selector);
				else if (e2 != null)
					new _initializer(this, e2.GetEnumerator(), null);
				else if (e1 != null)
					new _initializer(this, e1.GetEnumerator(), value_selector);
				else
					throw new Exception();
			}
		}
		public ReadOnlyDictionary(int c_in, IEnumerable<KeyValuePair<TKey, TValue>> rg_kvp, IEqualityComparer<TKey> cmp = null)
			: this(c_in, (Object)rg_kvp, default(Func<TKey, TValue>), cmp)
		{
		}
		public ReadOnlyDictionary(int c_in, IEnumerator<TKey> e, Func<TKey, TValue> value_selector, IEqualityComparer<TKey> cmp = null)
			: this(c_in, (Object)e, value_selector, cmp)
		{
		}
		public ReadOnlyDictionary(int c_in, IEnumerable<TKey> seq, Func<TKey, TValue> value_selector, IEqualityComparer<TKey> cmp = null)
			: this(c_in, (Object)seq, value_selector, cmp)
		{
		}
		public ReadOnlyDictionary(TKey[] seq, Func<TKey, TValue> value_selector, IEqualityComparer<TKey> cmp = null)
			: this(seq.Length, (Object)seq, value_selector, cmp)
		{
		}
		public ReadOnlyDictionary()
			: this(default(IEqualityComparer<TKey>))
		{
			this.entries = Collection<Entry>.None;
		}

		public IEqualityComparer<TKey> Comparer { get { return m_cmp; } }

		public int GetKeyIndex(TKey key)
		{
			var _tmp = entries;
			if (_tmp.Length > 0)
			{
				uint ix = (uint)m_cmp.GetHashCode(key) % (uint)_tmp.Length;
				do
					if (m_cmp.Equals(_tmp[ix].key, key))
						return (int)ix;
				while ((int)(ix = _tmp[ix].next) >= 0);
			}
			return -1;
		}
		public TValue GetIndexValue(int ix)
		{
			var _tmp = entries;
			if ((uint)ix >= (uint)_tmp.Length || _tmp[ix].next == Unused)
				throw new ArgumentOutOfRangeException();
			return _tmp[ix].value;
		}
		public TKey GetIndexKey(int ix)
		{
			var _tmp = entries;
			if ((uint)ix >= (uint)_tmp.Length || _tmp[ix].next == Unused)
				throw new ArgumentOutOfRangeException();
			return _tmp[ix].key;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			var _tmp = entries;
			if (_tmp.Length > 0)
			{
				uint ix = (uint)m_cmp.GetHashCode(key) % (uint)_tmp.Length;
				do
				{
					if (m_cmp.Equals(_tmp[ix].key, key))
					{
						value = _tmp[ix].value;
						return true;
					}
				}
				while ((int)(ix = _tmp[ix].next) >= 0);//!= Term);
			}
			value = default(TValue);
			return false;
		}

		public TValue this[TKey key]
		{
			get
			{
				var _tmp = entries;
				if (_tmp.Length > 0)
				{
					uint ix = (uint)m_cmp.GetHashCode(key) % (uint)_tmp.Length;
					do
						if (m_cmp.Equals(_tmp[ix].key, key))
							return _tmp[ix].value;
					while ((int)(ix = _tmp[ix].next) >= 0);
				}
				throw new KeyNotFoundException();
			}
		}

		public bool ContainsKey(TKey key)
		{
			var _tmp = entries;
			if (_tmp.Length > 0)
			{
				uint ix = (uint)m_cmp.GetHashCode(key) % (uint)_tmp.Length;
				do
					if (m_cmp.Equals(_tmp[ix].key, key))
						return true;
				while ((int)(ix = _tmp[ix].next) >= 0);
			}
			return false;
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			var _tmp = entries;
			if (_tmp.Length > 0)
			{
				uint ix = (uint)m_cmp.GetHashCode(item.Key) % (uint)_tmp.Length;
				do
					if (m_cmp.Equals(_tmp[ix].key, item.Key))
						return _tmp[ix].value.Equals(item.Value);
				while ((int)(ix = _tmp[ix].next) >= 0);
			}
			return false;
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int i_dst)
		{
			var _tmp = entries;
			for (int i = 0; i < _tmp.Length; i++)
				if (_tmp[i].next != Unused)
					array[i_dst++] = _tmp[i].kvp;
		}

		public TValue[] GetValuesArray()
		{
			var arr = new TValue[count];
			var _tmp = entries;
			for (int c = 0, i = 0; c < arr.Length; i++)
				if (_tmp[i].next != Unused)
					arr[c++] = _tmp[i].value;

			return arr;
		}

		public int Count { get { return count; } }
		public bool IsReadOnly { get { return true; } }
		public ICollection<TKey> Keys { get { return new KeyCollection(this); } }
		public ICollection<TValue> Values { get { return new _val_enum(this); } }

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys { get { return new KeyCollection(this); } }
		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values { get { return new _val_enum(this); } }

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			var _tmp = entries;
			for (int i = 0; i < _tmp.Length; i++)
				if (_tmp[i].next != Unused)
					yield return _tmp[i].kvp;
		}

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public class KeyCollection : _ro_coll_base<TKey>, _ICollection<TKey>, ISet<TKey>
		{
			ReadOnlyDictionary<TKey, TValue> d;
			public KeyCollection(ReadOnlyDictionary<TKey, TValue> d)
			{
				this.d = d;
			}
			public bool Contains(TKey item)
			{
				return d.ContainsKey(item);
			}

			public void CopyTo(TKey[] array, int i_dst)
			{
				var _tmp = d.entries;
				for (int i = 0; i < _tmp.Length; i++)
					if (_tmp[i].next != Unused)
						array[i_dst++] = _tmp[i].key;
			}

			public int Count { get { return d.count; } }

			public IEnumerator<TKey> GetEnumerator()
			{
				var _tmp = d.entries;
				for (int i = 0; i < _tmp.Length; i++)
					if (_tmp[i].next != Unused)
						yield return _tmp[i].key;
			}

			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

			public bool IsProperSubsetOf(IEnumerable<TKey> other) { throw not.impl; }
			public bool IsProperSupersetOf(IEnumerable<TKey> other) { throw not.impl; }
			public bool IsSubsetOf(IEnumerable<TKey> other) { throw not.impl; }
			public bool IsSupersetOf(IEnumerable<TKey> other) { throw not.impl; }
			public bool Overlaps(IEnumerable<TKey> other) { throw not.impl; }

			public bool SetEquals<TExt>(Dictionary<TKey, TExt>.KeyCollection other)
			{
				if (other.Count != d.Count)
					return false;
				var ie = other.GetEnumerator();
				while (ie.MoveNext())
					if (d.GetKeyIndex(ie.Current) == -1)
						return false;
				return true;
			}
			public unsafe bool SetEquals(IEnumerable<TKey> other)
			{
				if ((Object)other == null)
					return false;
				if ((Object)this == (Object)other)
					return true;

				ISet<TKey> is1;

				int c_coll, c_set = d.Count;
				if ((is1 = other as ISet<TKey>) != null)
				{
					if (c_set != is1.Count)
						return false;

					IEqualityComparer<TKey> _cmp = null;
					KeyCollection _kc;
					HashSet<TKey> _hs;
					if ((_kc = is1 as KeyCollection) != null)
						_cmp = _kc.d.m_cmp;
					else if ((_hs = is1 as HashSet<TKey>) != null)
						_cmp = _hs.Comparer;

					if (_cmp != null && Object.ReferenceEquals(d.m_cmp, _cmp))
					{
						var ie = is1.GetEnumerator();
						while (ie.MoveNext())
							if (d.GetKeyIndex(ie.Current) == -1)
								return false;
						return true;
					}
					goto full;
				}
				else if ((c_coll = other.CountIfAvail<TKey>()) != -1)
				{
				}
				else if (c_set == 0)
					return !other.GetEnumerator().MoveNext();
				else
					goto full;

				if (c_coll < c_set || (c_set == 0 && c_coll > 0))
					return false;
			full:
				return CountUnique(other) == c_set;
			}

			unsafe int CountUnique(IEnumerable<TKey> other)
			{
				int ix, c_marked = 0;
				int n = (int)((uint)(d.count - 1) >> 6) + 1;
				ulong* bitArrayPtr = stackalloc ulong[n];
				alib.Bits.BitHelper helper = new alib.Bits.BitHelper(bitArrayPtr, n);

				var ie = other.GetEnumerator();
				while (ie.MoveNext())
					if ((ix = d.GetKeyIndex(ie.Current)) == -1)
						return -1;
					else if (!helper.IsMarked(ix))
					{
						helper.SetBit(ix);
						c_marked++;
					}
				return c_marked;
			}

			bool ISet<TKey>.Add(TKey item) { throw not.valid; }
			public void SymmetricExceptWith(IEnumerable<TKey> other) { throw not.impl; }
			public void UnionWith(IEnumerable<TKey> other) { throw not.impl; }
			public void ExceptWith(IEnumerable<TKey> other) { throw not.impl; }
			public void IntersectWith(IEnumerable<TKey> other) { throw not.impl; }
		};

		class _val_enum : _ro_coll_base<TValue>, _ICollection<TValue>
		{
			ReadOnlyDictionary<TKey, TValue> d;
			public _val_enum(ReadOnlyDictionary<TKey, TValue> d)
			{
				this.d = d;
			}
			public bool Contains(TValue item)
			{
				var _tmp = d.entries;
				for (int i = 0; i < _tmp.Length; i++)
					if (_tmp[i].next != Unused && _tmp[i].value.Equals(item))
						return true;
				return false;
			}

			public void CopyTo(TValue[] array, int i_dst)
			{
				var _tmp = d.entries;
				for (int i = 0; i < _tmp.Length; i++)
					if (_tmp[i].next != Unused)
						array[i_dst++] = _tmp[i].value;
			}

			public int Count { get { return d.count; } }

			public IEnumerator<TValue> GetEnumerator()
			{
				var _tmp = d.entries;
				for (int i = 0; i < _tmp.Length; i++)
					if (_tmp[i].next != Unused)
						yield return _tmp[i].value;
			}

			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		};

		void ICollection.CopyTo(System.Array array, int index) { throw not.valid; }
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { throw not.valid; }
		void ICollection<KeyValuePair<TKey, TValue>>.Clear() { throw not.valid; }
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) { throw not.valid; }
		bool ICollection.IsSynchronized { get { return true; } }
		Object ICollection.SyncRoot { get { return this; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class _readonly_ext
	{
		public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> ie)
		{
			return new ReadOnlyDictionary<TKey, TValue>(-1, ie);
		}
		public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> ie, IEqualityComparer<TKey> cmp)
		{
			return new ReadOnlyDictionary<TKey, TValue>(-1, ie, cmp);
		}
		public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TSrc, TKey, TValue>(
			this IEnumerable<TSrc> ie,
			Func<TSrc, TKey> key_selector,
			Func<TSrc, TValue> value_selector)
		{
			return new ReadOnlyDictionary<TKey, TValue>(-1, ie.Select(s => new KeyValuePair<TKey, TValue>(key_selector(s), value_selector(s))));
		}
		//public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TSrc, TKey, TValue>(
		//	this IEnumerable<TSrc> ie,
		//	Func<TSrc, TKey> key_selector,
		//	Func<TSrc, int, TValue> value_selector,
		//	IEqualityComparer<TKey> cmp = null)
		//{
		//	return new ReadOnlyDictionary<TKey, TValue>(
		//		-1,
		//		ie.Select((s, ix) => new KeyValuePair<TKey, TValue>(key_selector(s), value_selector(s, ix))),
		//		cmp);
		//}
		public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(
			this IEnumerable<TKey> ie,
			int count,
			Func<TKey, TValue> value_selector,
			IEqualityComparer<TKey> cmp)
		{
			return new ReadOnlyDictionary<TKey, TValue>(count, ie, value_selector, cmp);
		}
		/// <summary>
		/// Note: IEqualityComparer(TKey) comparison type is propagated
		/// </summary>
		public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> d)
		{
			return new ReadOnlyDictionary<TKey, TValue>(d.Count, d, d.Comparer);
		}

		public static ICollection<T> Concat<T>(this ICollection<T> s0, ICollection<T> s1)
		{
			Debug.Assert((s0 as T[] ?? s1 as T[]) == null);

			int c0 = s0.Count, c1 = s1.Count;
			if (c1 == 0)
				return c0 != 0 ? s0 : Collection<T>.None;
			if (c0 == 0)
				return s1;
			return new _coll_defer<T>(System.Linq.Enumerable.Concat(s0, s1), c0 + c1);
		}
		public static ICollection<T> Take<T>(this ICollection<T> seq, int c)
		{
			return c == 0 ? Collection<T>.None : c >= seq.Count ? seq : new _coll_take<T>(seq, c);
		}

		/// <summary>
		/// Contravariant overload: Upcasts the returned value, if it is found and if it is compatible with <typeparamref name="TDerived"/>.
		/// </summary>
		/// <typeparam name="TKey">Type of the dictionary keys</typeparam>
		/// <typeparam name="TValue">Type of the dictionary values</typeparam>
		/// <typeparam name="TDerived">Type of a derived value stored in the dictionary</typeparam>
		/// <param name="d">the dictionary to check</param>
		/// <param name="key">the key for the desired value</param>
		/// <param name="value">[out] returned value</param>
		/// <returns><value>false</value> if the key is not found. Also returns 'false' if the type 
		/// of the value retrieved for <paramref name="key"/> is not compatible with 
		/// <typeparamref name="TDerived"/>
		/// </returns>
		public static bool TryGetValue<TKey, TValue, TDerived>(this IDictionary<TKey, TValue> d, TKey key, out TDerived value)
			where TDerived : TValue
		{
			TValue v;
			if (!d.TryGetValue(key, out v) || !(v is TDerived))
			{
				value = default(TDerived);
				return false;
			}
			value = (TDerived)v;
			return true;
		}
	};
}
