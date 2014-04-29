using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using alib.Hashing;

namespace alib.Dictionary
{
	using String = System.String;

	public static class _dictionary_ext
	{
		public static void AddIncTally<TKey>(this Dictionary<TKey, int> d, TKey k)
		{
			int v;
			if (d.TryGetValue(k, out v))	// O(1)
				d[k]++;
			else
				d.Add(k, 1);
		}
		public static void AddIncTally<TKey>(this SlotDictionary<TKey, int> d, TKey k)
		{
			int v;
			if ((v = d.FindEntry(k)) == -1)
				d.Add(k, 1);
			else
				d.entries[v].value++;
		}

		public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> d, Predicate<KeyValuePair<TKey, TValue>> f)
		{
			List<TKey> keys_to_remove = new List<TKey>();
			foreach (var kvp in d)
				if (f(kvp))
					keys_to_remove.Add(kvp.Key);
			foreach (TKey k in keys_to_remove)
				d.Remove(k);
		}

		public static void AddMany<TKey, TValue, TSrc>(
			this Dictionary<TKey, TValue> d,
			IEnumerable<TSrc> src,
			Converter<TSrc, TKey> key_selector,
			Converter<TSrc, TValue> value_selector)
		{
			foreach (TSrc e in src)
				d.Add(key_selector(e), value_selector(e));
		}

		public static void AddToValueHashset<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> d, TKey k, TValue v)
		{
			HashSet<TValue> hs;
			if (d.TryGetValue(k, out hs))
				hs.Add(v);
			else
				d.Add(k, new HashSet<TValue> { v });
		}
		public static void AddToValueHashset<TKey, TValue>(this SlotDictionary<TKey, HashSet<TValue>> d, TKey k, TValue v)
		{
			HashSet<TValue> hs;
			if (d.TryGetValue(k, out hs))
				hs.Add(v);
			else
				d.Add(k, new HashSet<TValue> { v });
		}

		public static SlotDictionary<TKey, HashSet<TValue>> ToLookupDict<TSrc, TKey, TValue>(
			this IEnumerable<TSrc> seq,
			Func<TSrc, TKey> key_selector,
			Func<TSrc, TValue> value_selector)
		{
			SlotDictionary<TKey, HashSet<TValue>> d = new SlotDictionary<TKey, HashSet<TValue>>();
			foreach (var t in seq)
				d.AddToValueHashset(key_selector(t), value_selector(t));
			return d;
		}

		public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		{
			TValue v;
			return dict.TryGetValue(key, out v) ? v : default(TValue);
		}
	};

	[DebuggerDisplay("Count={Count}")]
	public class SlotDictionary<TKey, TValue> :
		IDictionary<TKey, TValue>,
		alib.Enumerable._ICollection<KeyValuePair<TKey, TValue>>,
		IDictionary
	{
		[StructLayout(LayoutKind.Sequential)]
		[DebuggerDisplay("{ToString(),nq}")]
		public struct _entry
		{
			public int hash_code;
			public int next;
			public TKey key;
			public TValue value;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public KeyValuePair<TKey, TValue> kvp { get { return new KeyValuePair<TKey, TValue>(key, value); } }

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public DictionaryEntry dent { get { return new DictionaryEntry(key, value); } }

			public override String ToString()
			{
				return String.Format("{0,8} {1}", key.ToString(), value.ToString());
			}
		};

		[DebuggerDisplay("{entries[ix].value.ToString(),nq}", Name = "{name_disp(),nq}", Type = "{entries[ix].value.GetType().Name,nq}")]
		public struct _disp_entry
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public _entry[] entries;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int ix;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public TValue _dbg_root { get { return entries[ix].value; } }

			public String name_disp()
			{
				//return (alib.String._string_ext.SQRB(ix)).PadRight(5) + entries[ix].key.ToString();
				return alib.String._string_ext.SQRB(entries[ix].key.ToString());
			}
		};

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public _disp_entry[] _dbg_entries
		{
			get
			{
				int c = next_index - freeCount;
				var arr = new _disp_entry[c];
				for (int i = 0, j = 0; i < c; i++)
				{
					if (entries[i].hash_code != ListTerm)
					{
						arr[j].entries = entries;
						arr[j].ix = i;
						j++;
						if (j == c)
							break;
					}
				}
				return arr;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public _entry[] entries;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int[] buckets;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int next_index;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int freeCount;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public const int ListTerm = -1;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int freeList;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IEqualityComparer<TKey> m_cmp;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		KeyCollection keys;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ValueCollection values;
#if false
		int mode;
#endif
		public SlotDictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException();
#if false
			mode = -1;				// m_cmp.Compare()
			if (comparer == StringComparer.OrdinalIgnoreCase)
				mode = 1;			// String.CompareOrdinal
			else if (comparer == StringComparer.Ordinal)
				mode = 2;			// String.CompareOrdinal
			else if (comparer != null)
				this.m_cmp = comparer;
			else if (typeof(TKey).IsValueType)
				mode = 3;			// obj.Equals()
			else if (Object.ReferenceEquals(default(TKey), null))
				mode = 0;			//  x==y
			else
				this.m_cmp = EqualityComparer<TKey>.Default;
#else
			this.m_cmp = comparer ?? EqualityComparer<TKey>.Default;
#endif

			if (capacity > 0)
				this._initialize(capacity);
		}

		public SlotDictionary()
			: this(0, null)
		{
		}

		public SlotDictionary(IDictionary<TKey, TValue> dictionary)
			: this(dictionary, null)
		{
		}

		public SlotDictionary(IEqualityComparer<TKey> comparer)
			: this(0, comparer)
		{
		}

		public SlotDictionary(int capacity)
			: this(capacity, null)
		{
		}

		public SlotDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
			: this(dictionary != null ? dictionary.Count : 0, comparer)
		{
			if (dictionary == null)
				throw new ArgumentNullException();
			foreach (KeyValuePair<TKey, TValue> pair in dictionary)
				this.Add(pair.Key, pair.Value);
		}


		void _initialize(int capacity)
		{
			int prime = alib.Math.primes.HashFriendly(capacity);
			buckets = new int[prime];
			for (int i = 0; i < prime; i++)
				buckets[i] = ListTerm;
			entries = new _entry[prime];
			freeList = ListTerm;
		}

#if true
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int FindEntry(TKey key)
		{
			if (buckets != null)
			{
				int h = m_cmp.GetHashCode(key) & 0x7fffffff;
				for (int i = buckets[h % buckets.Length]; i != ListTerm; i = entries[i].next)
					if ((entries[i].hash_code == h) && m_cmp.Equals(entries[i].key, key))
						return i;
			}
			return -1;
		}
#else
		public int FindEntry(TKey key)
		{
			return buckets == null ? -1 : new entry_finder(this, key).FindEntry(entries);
		}

		struct entry_finder
		{
			public entry_finder(SlotDictionary<TKey, TValue> d, TKey key)
			{
				var b = d.buckets;
				this.i = b[(this.h = (this.cmp = d.m_cmp).GetHashCode(this.key = key) & 0x7fffffff) % b.Length];
			}
			TKey key;
			IEqualityComparer<TKey> cmp;
			int h, i;
			public int FindEntry(_entry[] entries)
			{
				while (i != ListTerm && _more(ref entries[i]))
					;
				return i;
			}
			bool _more(ref _entry e)
			{
				if (e.hash_code == h && cmp.Equals(e.key, key))
					return false;
				i = e.next;
				return true;
			}
		};
#endif
#if false
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (buckets != null)
			{
				TypedReference tr;
				int h = m_cmp.GetHashCode(key) & 0x7fffffff;
				int i = buckets[h % buckets.Length];
				while (i != ListTerm)
				{
					if (( __refvalue(tr = __makeref(entries[i]), _entry).hash_code == h) && m_cmp.Equals( __refvalue(tr, _entry).key, key))
					{
						value = __refvalue( tr, _entry).value;
						return true;
					}
					i = __refvalue( tr, _entry).next;
				}
			}
			value = default(TValue);
			return false;
		}
#else
		public bool TryGetValue(TKey key, out TValue value)
		{
			int i;
			if ((i = FindEntry(key)) != -1)
			{
				value = entries[i].value;
				return true;
			}
			value = default(TValue);
			return false;
		}
#endif

		public TValue this[TKey key]
		{
			get
			{
				int i;
				if ((i = FindEntry(key)) != -1)
					return entries[i].value;
				throw new KeyNotFoundException();
			}
			set
			{
				Insert(key, ref value, 2);
			}
		}
		// mode 0 : try add, don't throw, 
		// mode -1 : return value for duplicate key
		// mode 1 : try add, throw if duplicate
		// mode 2 : overwrite

		public bool TryAdd(TKey key, TValue value)
		{
			return Insert(key, ref value, 0);
		}

		public bool TryAdd(TKey key, ref TValue value)
		{
			return Insert(key, ref value, -1);
		}

		public void Add(TKey key, TValue value)
		{
			Insert(key, ref value, 1);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> kvp)
		{
			Add(kvp.Key, kvp.Value);
		}

		void IDictionary.Add(Object key, Object value)
		{
			if (key == null)
				throw new ArgumentNullException();

			TKey local = (TKey)key;
			Add(local, (TValue)value);
		}

		bool Insert(TKey key, ref TValue value, int mode)
		{
			if (key == null)
				throw new ArgumentNullException();
			if (buckets == null)
				_initialize(0);

			int h = m_cmp.GetHashCode(key) & 0x7fffffff, index = h % buckets.Length, i = buckets[index];
			TypedReference tr;
			while (i != ListTerm)
			{
				tr = __makeref(entries[i]);
				if ( __refvalue(tr, _entry).hash_code == h && m_cmp.Equals( __refvalue(tr, _entry).key, key))
				{
					if (mode == -1)
						value = __refvalue( tr, _entry).value;
					if (mode <= 0)
						return false;
					if (mode == 1)
						throw new DuplicateKeyException();
					__refvalue(tr, _entry).value = value;
					return true;
				}
				i = __refvalue( tr, _entry).next;
			}
			if (freeCount > 0)
			{
				tr = __makeref(entries[i = freeList]);
				freeList = __refvalue( tr , _entry).next;
				freeCount--;
			}
			else
			{
				if (next_index == entries.Length)
				{
					Resize();
					index = h % buckets.Length;
				}
				tr = __makeref(entries[i = this.next_index]);
				next_index++;
			}
			__refvalue(tr, _entry).hash_code = h;
			__refvalue(tr, _entry).next = buckets[index];
			__refvalue(tr, _entry).key = key;
			__refvalue(tr, _entry).value = value;
			buckets[index] = i;
			return true;
		}

		public bool Remove(TKey key)
		{
			TValue removed;
			return RemoveItem(key, out removed);
		}

		void IDictionary.Remove(Object key)
		{
			if (key is TKey)
				Remove((TKey)key);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> kvp)
		{
			return RemoveValue(kvp.Key, kvp.Value);
		}

		public bool RemoveItem(TKey key, out TValue removed)
		{
			if (key == null)
				throw new ArgumentNullException();
			if (buckets != null)
			{
				int h = m_cmp.GetHashCode(key) & 0x7fffffff;
				int index = h % buckets.Length, i = buckets[index], j = ListTerm;
				TypedReference tr;
				while (i != ListTerm)
				{
					tr = tr = __makeref(entries[i]);
					if ( __refvalue(tr, _entry).hash_code == h &&
						m_cmp.Equals( __refvalue(tr, _entry).key, key))
					{
						if (j != ListTerm)
							entries[j].next = __refvalue( tr, _entry).next;
						else
							buckets[index] = __refvalue( tr, _entry).next;
						__refvalue(tr, _entry).hash_code = ListTerm;
						__refvalue(tr, _entry).next = freeList;
						__refvalue(tr, _entry).key = default(TKey);
						removed = __refvalue( tr, _entry).value;
						__refvalue(tr, _entry).value = default(TValue);
						freeList = i;
						freeCount++;
						return true;
					}
					j = i;
					i = __refvalue( tr, _entry).next;
				}
			}
			removed = default(TValue);
			return false;
		}

		void Resize()
		{
			int prime = alib.Math.primes.HashFriendly(next_index * 2);
			int[] numArray = new int[prime];
			int i;
			for (i = 0; i < prime; i++)
				numArray[i] = ListTerm;

			_entry[] new_arr = new _entry[prime];
			System.Array.Copy(entries, 0, new_arr, 0, next_index);
			for (i = 0; i < next_index; i++)
			{
				/// todo: can the hash code be -1 here... if so, then don't need to do it?
				int index = new_arr[i].hash_code % prime;
				new_arr[i].next = numArray[index];
				numArray[index] = i;
			}
			buckets = numArray;
			entries = new_arr;
		}

		public void Clear()
		{
			if (next_index > 0)
			{
				for (int i = 0; i < buckets.Length; i++)
					buckets[i] = ListTerm;
				System.Array.Clear(entries, 0, next_index);
				freeList = ListTerm;
				next_index = 0;
				freeCount = 0;
			}
		}

		public bool ContainsKey(TKey key)
		{
			return FindEntry(key) != -1;
		}

		public bool ContainsValue(TValue value)
		{
			int i;
			if (value == null)
			{
				for (i = 0; i < next_index; i++)
					if (entries[i].hash_code != ListTerm && entries[i].value == null)
						return true;
			}
			else
			{
				var cmp = EqualityComparer<TValue>.Default;
				for (i = 0; i < next_index; i++)
					if (entries[i].hash_code != ListTerm && cmp.Equals(entries[i].value, value))
						return true;
			}
			return false;
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> kvp)
		{
			int index = FindEntry(kvp.Key);
			return index != -1 && EqualityComparer<TValue>.Default.Equals(entries[index].value, kvp.Value);
		}

		public bool RemoveValue(TKey key, TValue value)
		{
			if (key == null)
				throw new ArgumentNullException();
			if (buckets != null)
			{
				int h = m_cmp.GetHashCode(key) & 0x7fffffff;
				int index = h % buckets.Length, i = buckets[index], j = ListTerm;
				TypedReference tr;
				while (i != ListTerm)
				{
					tr = __makeref(entries[i]);
					if ( __refvalue(tr, _entry).hash_code == h &&
						m_cmp.Equals( __refvalue(tr, _entry).key, key))
					{
						if (!EqualityComparer<TValue>.Default.Equals( __refvalue(tr, _entry).value, value))
							return false;
						if (j != ListTerm)
							entries[j].next = __refvalue( tr, _entry).next;
						else
							buckets[index] = __refvalue( tr, _entry).next;
						__refvalue(tr, _entry).hash_code = ListTerm;
						__refvalue(tr, _entry).next = freeList;
						__refvalue(tr, _entry).key = default(TKey);
						__refvalue(tr, _entry).value = default(TValue);
						freeList = i;
						freeCount++;
						return true;
					}
					j = i;
					i = __refvalue( tr, _entry).next;
				}
			}
			return false;
		}


		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count { get { return next_index - freeCount; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IEqualityComparer<TKey> Comparer { get { return m_cmp; } }

		bool IDictionary.Contains(Object key) { return key is TKey && ContainsKey((TKey)key); }

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			int c = next_index;
			_entry[] _tmp = entries;
			for (int i = 0; i < c; i++)
				if (_tmp[i].hash_code != ListTerm)
					yield return _tmp[i].kvp;
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		IDictionaryEnumerator IDictionary.GetEnumerator() { return new Enumerator1(this); }

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			this.CopyTo(array, index);
		}

		void ICollection.CopyTo(System.Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException();
			if (array.Rank != 1)
				throw new ArgumentException();
			if (array.GetLowerBound(0) != 0)
				throw new ArgumentException();
			if ((index < 0) || (index > array.Length))
				throw new ArgumentOutOfRangeException();
			if ((array.Length - index) < this.Count)
				throw new ArgumentException();

			KeyValuePair<TKey, TValue>[] pairArray = array as KeyValuePair<TKey, TValue>[];
			_entry[] _tmp;
			int i, c;
			if (pairArray != null)
				CopyTo(pairArray, index);
			else if (array is DictionaryEntry[])
			{
				DictionaryEntry[] entryArray = array as DictionaryEntry[];
				c = next_index;
				_tmp = entries;
				for (i = 0; i < c; i++)
					if (_tmp[i].hash_code != ListTerm)
						entryArray[index++] = new DictionaryEntry(_tmp[i].key, _tmp[i].value);
			}
			else
			{
				Object[] objArray = array as Object[];
				if (objArray == null)
					throw new ArgumentException();

				c = next_index;
				_tmp = entries;
				for (i = 0; i < c; i++)
					if (_tmp[i].hash_code != ListTerm)
						objArray[index++] = new KeyValuePair<TKey, TValue>(_tmp[i].key, _tmp[i].value);
			}
		}

		void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			if (array == null)
				throw new ArgumentNullException();
			if (index < 0 || index > array.Length)
				throw new ArgumentOutOfRangeException();
			if ((array.Length - index) < this.Count)
				throw new ArgumentException();

			int c = this.next_index;
			_entry[] _tmp = this.entries;
			for (int i = 0; i < c; i++)
				if (_tmp[i].hash_code != ListTerm)
					array[index++] = new KeyValuePair<TKey, TValue>(_tmp[i].key, _tmp[i].value);
		}


		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public KeyCollection Keys
		{
			get
			{
				if (keys == null)
					keys = new KeyCollection(this);
				return keys;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection<TKey> IDictionary<TKey, TValue>.Keys { get { return this.Keys; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection IDictionary.Keys { get { return this.Keys; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ValueCollection Values
		{
			get
			{
				if (values == null)
					values = new ValueCollection(this);
				return values;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection IDictionary.Values { get { return this.Values; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection<TValue> IDictionary<TKey, TValue>.Values { get { return this.Values; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get { return false; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized { get { return false; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Object ICollection.SyncRoot { get { return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IDictionary.IsFixedSize { get { return false; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IDictionary.IsReadOnly { get { return false; } }

		Object IDictionary.this[Object key]
		{
			get
			{
				if (key is TKey)
				{
					int index = FindEntry((TKey)key);
					if (index != -1)
						return entries[index].value;
				}
				return null;
			}
			set
			{
				if (key == null)
					throw new ArgumentNullException();
				TKey local = (TKey)key;
			}
		}

		public class Enumerator1 : IDictionaryEnumerator
		{
			internal Enumerator1(SlotDictionary<TKey, TValue> dictionary)
			{
				this.d = dictionary;
				this.ix = -1;
			}

			readonly SlotDictionary<TKey, TValue> d;
			int ix;

			public void Reset() { ix = -1; }

			public DictionaryEntry Entry
			{
				get
				{
					if ((uint)ix >= (uint)d.next_index)
						throw not.valid;
					return d.entries[ix].dent;
				}
			}

			public object Key
			{
				get
				{
					if ((uint)ix >= (uint)d.next_index)
						throw not.valid;
					return d.entries[ix].key;
				}
			}

			public object Value
			{
				get
				{
					if ((uint)ix >= (uint)d.next_index)
						throw not.valid;
					return d.entries[ix].value;
				}
			}

			public object Current
			{
				get
				{
					if ((uint)ix >= (uint)d.next_index)
						throw not.valid;
					return d.entries[ix].dent;
				}
			}

			public bool MoveNext()
			{
				while (++ix < d.next_index)
					if (d.entries[ix].hash_code != ListTerm)
						return true;
				ix = d.next_index;
				return false;
			}
		};

		[DebuggerDisplay("Count = {Count}")]
		public sealed class KeyCollection : alib.Enumerable._ICollection<TKey>, ISet<TKey>
		{
			readonly SlotDictionary<TKey, TValue> d;

			public KeyCollection(SlotDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
					throw new ArgumentNullException();
				this.d = dictionary;
			}

			public void CopyTo(TKey[] array, int index)
			{
				if (array == null)
					throw new ArgumentNullException();
				if (index < 0 || index > array.Length)
					throw new ArgumentOutOfRangeException();
				if ((array.Length - index) < d.Count)
					throw new ArgumentException();

				int c = d.next_index;
				_entry[] _tmp = d.entries;
				for (int i = 0; i < c; i++)
					if (_tmp[i].hash_code != ListTerm)
						array[index++] = _tmp[i].key;
			}

			bool ICollection<TKey>.Contains(TKey item) { return d.ContainsKey(item); }

			public IEnumerator<TKey> GetEnumerator()
			{
				int c = d.next_index;
				_entry[] _tmp = d.entries;
				for (int i = 0; i < c; i++)
					if (_tmp[i].hash_code != ListTerm)
						yield return _tmp[i].key;
			}
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
			void ICollection<TKey>.Add(TKey item) { throw new NotSupportedException(); }
			void ICollection<TKey>.Clear() { throw new NotSupportedException(); }
			bool ICollection<TKey>.Remove(TKey item) { throw new NotSupportedException(); }
			public int Count { get { return d.Count; } }
			bool ICollection<TKey>.IsReadOnly { get { return true; } }
			bool ICollection.IsSynchronized { get { return false; } }
			Object ICollection.SyncRoot { get { return d; } }

			void ICollection.CopyTo(System.Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException();
				if (array.Rank != 1)
					throw new ArgumentException();
				if (array.GetLowerBound(0) != 0)
					throw new ArgumentException();
				if (index < 0 || index > array.Length)
					throw new ArgumentOutOfRangeException();
				if ((array.Length - index) < this.Count)
					throw new ArgumentException();

				int c = d.next_index;
				TKey[] localArray = array as TKey[];
				if (localArray != null)
				{
					CopyTo(localArray, index);
				}
				else
				{
					Object[] objArray = array as Object[];
					if (objArray == null)
						throw new ArgumentException();

					_entry[] _tmp = d.entries;
					for (int i = 0; i < c; i++)
						if (_tmp[i].hash_code != ListTerm)
							objArray[index++] = _tmp[i].key;
				}
			}

			/// <summary>
			/// ISet(TKey)
			/// </summary>

			public bool IsProperSubsetOf(IEnumerable<TKey> other)
			{
				throw new NotImplementedException();
			}

			public bool IsProperSupersetOf(IEnumerable<TKey> other)
			{
				throw new NotImplementedException();
			}

			public bool IsSubsetOf(IEnumerable<TKey> other)
			{
				throw new NotImplementedException();
			}

			public bool IsSupersetOf(IEnumerable<TKey> other)
			{
				throw new NotImplementedException();
			}

			public bool Overlaps(IEnumerable<TKey> other)
			{
				throw new NotImplementedException();
			}

			public bool SetEquals<TExt>(Dictionary<TKey, TExt>.KeyCollection other)
			{
				if (other.Count != d.Count)
					return false;
				var ie = other.GetEnumerator();
				while (ie.MoveNext())
					if (d.FindEntry(ie.Current) == -1)
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
							if (d.FindEntry(ie.Current) == -1)
								return false;
						return true;
					}
					goto full;
				}
				else if ((c_coll = alib.Enumerable._enumerable_ext.CountIfAvail<TKey>(other)) != -1)
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
				int n = (int)((uint)(d.next_index - 1) >> 6) + 1;
				ulong* bitArrayPtr = stackalloc ulong[n];
				alib.Bits.BitHelper helper = new alib.Bits.BitHelper(bitArrayPtr, n);

				var ie = other.GetEnumerator();
				while (ie.MoveNext())
					if ((ix = d.FindEntry(ie.Current)) == -1)
						return -1;
					else if (!helper.IsMarked(ix))
					{
						helper.SetBit(ix);
						c_marked++;
					}
				return c_marked;
			}

			bool ISet<TKey>.Add(TKey item) { throw not.valid; }
			public void SymmetricExceptWith(IEnumerable<TKey> other) { throw not.valid; }
			public void UnionWith(IEnumerable<TKey> other) { throw not.valid; }
			public void ExceptWith(IEnumerable<TKey> other) { throw not.valid; }
			public void IntersectWith(IEnumerable<TKey> other) { throw not.valid; }
		};

		[DebuggerDisplay("Count = {Count}")]
		public sealed class ValueCollection : alib.Enumerable._ICollection<TValue>
		{
			SlotDictionary<TKey, TValue> d;

			public ValueCollection(SlotDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
					throw new ArgumentNullException();
				this.d = dictionary;
			}

			public void CopyTo(TValue[] array, int index)
			{
				if (array == null)
					throw new ArgumentNullException();
				if (index < 0 || index > array.Length)
					throw new ArgumentOutOfRangeException();
				if ((array.Length - index) < d.Count)
					throw new ArgumentException();

				int c = d.next_index;
				_entry[] _tmp = d.entries;
				for (int i = 0; i < c; i++)
					if (_tmp[i].hash_code != ListTerm)
						array[index++] = _tmp[i].value;
			}

			public IEnumerator<TValue> GetEnumerator()
			{
				int c = d.next_index;
				_entry[] _tmp = d.entries;
				for (int i = 0; i < c; i++)
					if (_tmp[i].hash_code != ListTerm)
						yield return _tmp[i].value;
			}
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

			void ICollection<TValue>.Add(TValue item) { throw new NotSupportedException(); }
			void ICollection<TValue>.Clear() { throw new NotSupportedException(); }
			bool ICollection<TValue>.Contains(TValue item) { return d.ContainsValue(item); }
			bool ICollection<TValue>.Remove(TValue item) { throw new NotSupportedException(); }
			public int Count { get { return d.Count; } }
			bool ICollection<TValue>.IsReadOnly { get { return true; } }
			bool ICollection.IsSynchronized { get { return false; } }
			Object ICollection.SyncRoot { get { return d; } }

			void ICollection.CopyTo(System.Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException();
				if (array.Rank != 1)
					throw new ArgumentException();
				if (array.GetLowerBound(0) != 0)
					throw new ArgumentException();
				if (index < 0 || index > array.Length)
					throw new ArgumentOutOfRangeException();
				if ((array.Length - index) < this.Count)
					throw new ArgumentException();

				TValue[] localArray = array as TValue[];
				if (localArray != null)
				{
					CopyTo(localArray, index);
				}
				else
				{
					Object[] objArray = array as Object[];
					if (objArray == null)
						throw new ArgumentException();

					int c = d.next_index;
					_entry[] _tmp = d.entries;
					for (int i = 0; i < c; i++)
						if (_tmp[i].hash_code != ListTerm)
							objArray[index++] = _tmp[i].value;
				}
			}
		};
	};
}
