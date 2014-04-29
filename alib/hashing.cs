using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using alib.Debugging;
using alib.Enumerable;
using alib.Array;

namespace alib.Hashing
{
	using String = System.String;

	public interface IIndexedHash<T> : _IList<T>
	{
		int this[T to_find] { get; }
		T[] ConvertTo(IEnumerable<int> items);
		int[] ConvertFrom(IEnumerable<T> items);
		IEqualityComparer<T> Comparer { get; }
		int Add(T item);
		int GetOrAdd(T item);
		bool TryAdd(T item, out int ix);
	};

	/// <summary>
	/// A distinct set where a unique, contiguous, 0-based index is provided for each item 
	/// and O(1) access is provided for both item-to-index and index-to-item lookup. Most importantly,
	/// each index value is permanent; index values do not change across internal resizing operations.
	/// Cannot delete items once added.
	/// </summary>
	[DebuggerDisplay("Count={Count}")]
	public class IndexedHash<T> : IIndexedHash<T>, IAddRangeColl<T>, ISet<T>
	{
		const int MinCapacity = 17;

		protected IndexedHash(int capacity, int hash_size, IEqualityComparer<T> cmp)
		{
			if (capacity < MinCapacity)
				capacity = MinCapacity;
			if (hash_size < capacity)
				hash_size = alib.Math.primes.HashFriendly(capacity);

			this.cmp = cmp ?? EqualityComparer<T>.Default;
			this.items_arr = new T[capacity];
			this.rgrgh = new int[hash_size][];
		}
		public IndexedHash(int capacity = -1)
			: this(capacity, -1, default(IEqualityComparer<T>))
		{
		}
		public IndexedHash(IEnumerable<T> items_in)
			: this(items_in._Count())
		{
			if (items_in == null)
				return;
			var e = items_in.GetEnumerator();
			while (e.MoveNext())
				Add(e.Current);
		}
		public IndexedHash(IndexedHash<T> to_copy)
			: this()
		{
			this.cmp = to_copy.cmp;
			this.i_next = to_copy.i_next;
			this.items_arr = to_copy.items_arr;
			this.rgrgh = to_copy.rgrgh;
		}

		readonly public IEqualityComparer<T> cmp;
		protected int i_next;
		protected T[] items_arr;
		protected int[][] rgrgh;

		public IEqualityComparer<T> Comparer { get { return cmp; } }

		/// <summary> Note: may have excess length </summary>
		public T[] GetItemsArray() { return items_arr; }

		public T this[int index] { get { return items_arr[index]; } }

		public int this[T item] { get { return find(item); } }

		public bool Contains(T item) { return find(item) != -1; }

		public int Count { get { return i_next; } }

		public int Capacity { get { return items_arr.Length; } }

		public void TrimExcess()
		{
			arr.Resize(ref items_arr, i_next);
		}

		public int Add(T item)
		{
			int ix;
			if (!TryAdd(item, out ix))
				throw new DuplicateKeyException();
			return ix;
		}

		public int GetOrAdd(T item)
		{
			int ix;
			TryAdd(item, out ix);
			return ix;
		}

#if DEBUG
		public bool TryAdd(T item, out int ix)
		{
			if (++thread_check != 1)
				throw new Exception("Need thread safety");
			var b = _Add(item, out ix);
			thread_check--;
			return b;
		}

		int thread_check;

		public bool _Add(T item, out int ix)
#else
		public bool TryAdd(T item, out int ix)
#endif
		{
#if STRING_INTERN
			if (cmp is alib.String.StringInternEqualityComparer)
				item = (T)(Object)String.Intern(item as String);
#endif
			uint h;
			if ((ix = find(item, out h)) != -1)
				return false;

			if (i_next == items_arr.Length)
			{
				resize();
				h = (uint)cmp.GetHashCode(item) % (uint)rgrgh.Length;
			}

			items_arr[ix = i_next++] = item;
			arr.Append(ref rgrgh[h], ix);
			return true;
		}

		T[] get_safe_members(out int c)
		{
			c = this.i_next;
			var _tmp = items_arr;
			if (c >= _tmp.Length)
				c = _tmp.Length - 1;
			return _tmp;
		}

		int find(T item, out uint h)
		{
#if STRING_INTERN
			if (cmp is alib.String.StringInternEqualityComparer)
			{
				String s;
				if ((s = String.IsInterned(item as String)) == null)
					return -1;
				item = (T)(Object)s;
			}
#endif
			var rgh = rgrgh[h = (uint)cmp.GetHashCode(item) % (uint)rgrgh.Length];
			if (rgh != null)
				for (int ix, i = rgh.Length - 1; i >= 0; i--)
					if (cmp.Equals(items_arr[ix = rgh[i]], item))
						return ix;
			return -1;
		}

		int find(T item)
		{
			uint h;
			return find(item, out h);
		}

		void resize()
		{
			Debug.Assert(i_next == items_arr.Length);

			var _new = new T[i_next << 1];
			var d = Math.primes.HashFriendly(_new.Length);
			this.rgrgh = new int[d][];

			for (int i = 0; i < i_next; i++)
				arr.Append(ref rgrgh[(uint)cmp.GetHashCode(_new[i] = items_arr[i]) % (uint)d], i);

			this.items_arr = _new;
		}

		public void AddRange(IEnumerable<T> rg)
		{
			var e = rg.GetEnumerator();
			while (e.MoveNext())
				Add(e.Current);
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			var e = other.GetEnumerator();
			uint h;
			while (e.MoveNext())
				if (find(e.Current, out h) != -1)
					return true;
			return false;
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			throw not.valid;
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			throw not.valid;
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			throw not.valid;
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			throw not.valid;
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			throw not.valid;
		}

		public void UnionWith(IEnumerable<T> other) { this.AddRange(other); }
		public void ExceptWith(IEnumerable<T> other) { throw not.valid; }
		public void IntersectWith(IEnumerable<T> other) { throw not.valid; }
		public void SymmetricExceptWith(IEnumerable<T> other) { throw not.valid; }

		public T[] ToArray()
		{
			int c;
			var _tmp = get_safe_members(out c);
			return arr.Resize(_tmp, c);
		}

		public T[] ConvertTo(IEnumerable<int> items)
		{
			T[] _ret = new T[items._Count()];
			int i = 0;
			var e = items.GetEnumerator();
			while (e.MoveNext())
				_ret[i++] = this[e.Current];
			return _ret;
		}

		public int[] ConvertFrom(IEnumerable<T> items)
		{
			int[] _ret = new int[items._Count()];
			int i = 0;
			var e = items.GetEnumerator();
			while (e.MoveNext())
				_ret[i++] = this[e.Current];
			return _ret;
		}

		public void CopyTo(T[] array, int index)
		{
			int c;
			var _tmp = get_safe_members(out c);
			for (int i = 0; i < c; i++)
				array[index++] = _tmp[i];
		}

		public void CopyTo(System.Array array, int index)
		{
			int c;
			var _tmp = get_safe_members(out c);
			for (int i = 0; i < c; i++)
				array.SetValue(_tmp[i], index++);
		}

		public IEnumerator<T> GetEnumerator()
		{
			int c;
			var _tmp = get_safe_members(out c);
			for (int i = 0; i < c; i++)
				yield return _tmp[i];
		}

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		void ICollection<T>.Add(T item) { this.Add(item); }

		bool ISet<T>.Add(T item)
		{
			int ix;
			return TryAdd(item, out ix);
		}

		public bool IsReadOnly { get { return false; } }
		public bool IsSynchronized { get { return false; } }
		public bool Remove(T item) { throw not.valid; }
		public void Clear() { throw not.valid; }
		public Object SyncRoot { get { return this; } }
	};

	/// <summary>
	/// A distinct set where a unique, contiguous, 0-based index is provided for each item 
	/// and O(1) access is provided for both item-to-index and index-to-item lookup. Most importantly,
	/// each index value is permanent; index values do not change across internal resizing operations.
	/// Cannot delete items once added.
	/// </summary>
	public class IndexedHashSafe<T> : IReadOnlyList<T>
		where T : class
	{
		public IndexedHashSafe(int capacity = 0, IEqualityComparer<T> cmp = null)
		{
			if (capacity <= 0)
				capacity = 37;
			data = new ih(capacity, cmp ?? EqualityComparer<T>.Default);
		}

		public IndexedHashSafe(IEnumerable<T> items_in)
			: this(items_in._Count(), default(IEqualityComparer<T>))
		{
			var e = items_in.GetEnumerator();
			while (e.MoveNext())
				Add(e.Current);
		}

		ih data;

		public T this[int index] { get { return data.get(index); } }

		public int this[T to_find]
		{
			get
			{
				uint h;
				return data.find(to_find, out h);
			}
		}

		public int Count { get { return data.Count; } }

		public T[] ToArray() { return data.ToArray(); }

		public int Add(T item)
		{
			int i;
			var _tmp = data;
			while (_tmp != (_tmp = _tmp.add(ref data, item, out i)))
				;
			return i;
		}

		sealed class ih
		{
			public ih(int capacity, IEqualityComparer<T> cmp)
			{
				this.cmp = cmp;
				this.items_arr = new T[capacity];
				this.rgrgh = new int[Math.primes.HashFriendly(capacity)][];
				this.i_next = 0;
			}
			ih(ih _prv)
				: this(_prv.items_arr.Length * 2, _prv.cmp)
			{
				var _arr = _prv.items_arr;
				this.i_next = _arr.Length;

				int c, d = rgrgh.Length;
				int[] src, dst;
				for (int i = 0; i < i_next; i++)
				{
					uint j = (uint)cmp.GetHashCode(items_arr[i] = _arr[i]) % (uint)d;
					if ((src = rgrgh[j]) == null)
						rgrgh[j] = new[] { i };
					else
					{
						(rgrgh[j] = dst = new int[(c = src.Length) + 1])[c] = i;
						while (--c >= 0)
							dst[c] = src[c];
					}
				}
			}
			readonly IEqualityComparer<T> cmp;
			int i_next;
			readonly T[] items_arr;
			readonly int[][] rgrgh;

			public T get(int index) { return items_arr[index]; }

			public int find(T item, out uint h)
			{
				var rgh = rgrgh[h = (uint)cmp.GetHashCode(item) % (uint)rgrgh.Length];
				if (rgh != null)
					for (int ix, i = rgh.Length - 1; i >= 0; i--)
						if (cmp.Equals(items_arr[ix = rgh[i]], item))
							return ix;
				return -1;
			}
			public ih add(ref ih _target, T item, out int ix)
			{
				while (true)
				{
					uint h;
					if ((ix = find(item, out h)) != -1)
						return this;

					if ((ix = i_next) == items_arr.Length)
					{
						ih _tmp, _new = new ih(this);
						if ((_tmp = _target) != this)
							return _tmp;

						return this == (_tmp = Interlocked.CompareExchange(ref _target, _new, this)) ? _new : _tmp;
					}
					if (null == Interlocked.CompareExchange(ref items_arr[ix], item, null))
					{
						arr.AppendSafe(ref rgrgh[h], ix);
						if (ix == Interlocked.CompareExchange(ref i_next, ix + 1, ix))
							return this;
					}
				}
			}
			public int Count { get { return i_next; } }

			public T[] ToArray()
			{
				var c = i_next;
				return arr.Resize(items_arr, c);
			}

			public IEnumerator<T> GetEnumerator()
			{
				for (int i = 0; i < i_next; i++)
					yield return items_arr[i];
			}
		};

		public IEnumerator<T> GetEnumerator() { return data.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return data.GetEnumerator(); }
	};

#if false
	public class NotifyingIndexedHash<T> : IndexedHash<T>, INotifyCollectionChanged
	{
		protected NotifyingIndexedHash(int capacity)
			: base(capacity, -1, default(IEqualityComparer<T>))
		{
		}

		public NotifyingIndexedHash(IEnumerable<T> items_in)
			: base(items_in)
		{
		}

		public NotifyingIndexedHash()
			: base()
		{
		}

		public sealed override int Add(T item)
		{
			int i = base.Add(item);
			var _tmp = CollectionChanged;
			if (_tmp != null)
				_tmp(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (Object)item));
			return i;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;
	};
#endif

	[DebuggerDisplay("Count={Count}")]
	public class StringIndex : IndexedHash<String>
	{
		protected StringIndex(int capacity, int hash_size, StringComparison comparison_type)
			: base(capacity, hash_size, alib.String._string_ext.GetComparer(comparison_type))
		{
			this.sc = comparison_type;
		}
		public StringIndex(int capacity = 0, StringComparison sc = StringComparison.Ordinal)
			: this(capacity, -1, sc)
		{
		}
		public StringIndex(ICollection<String> items_in)
			: this(items_in.Count)
		{
			var e = items_in.GetEnumerator();
			while (e.MoveNext())
				Add(e.Current);
		}
		public StringIndex(StringIndex to_copy)
			: base(to_copy)
		{
			this.sc = to_copy.sc;
		}

		readonly StringComparison sc;

		public void Write(alib.IO._BinaryWriter bw)
		{
			bw.Write((int)sc);

			int c = items_arr.Length;
			bw.Write(c);

			int ch = rgrgh.Length;
			bw.Write(ch);

			bw.Write(i_next);

			for (int i = 0; i < i_next; i++)
				bw.Write(items_arr[i]);

			for (int i = 0; i < ch; i++)
			{
				var rgh = rgrgh[i];
				int cc = rgh == null ? 0 : rgh.Length;
				bw.Write7BitEncodedInt(cc);
				for (int j = 0; j < cc; j++)
					bw.Write7BitEncodedInt(rgh[j]);
			}
		}

		public static StringIndex Read(alib.IO._BinaryReader br)
		{
			var sc = (StringComparison)br.ReadInt32();

			int c = br.ReadInt32();

			int ch = br.ReadInt32();

			var s = new StringIndex(c, ch, sc);

			s.i_next = br.ReadInt32();

			for (int i = 0; i < s.i_next; i++)
				s.items_arr[i] = br.ReadString();

			for (int i = 0; i < ch; i++)
			{
				int cc = br.Read7BitEncodedInt();
				if (cc > 0)
				{
					var rgh = s.rgrgh[i] = new int[cc];
					for (int j = 0; j < cc; j++)
						rgh[j] = br.Read7BitEncodedInt();
				}
			}
			return s;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class MinMaxLimitSet<TObj, TScore> : HashSet<TObj>
		where TScore : IComparable<TScore>
	{
		public MinMaxLimitSet(Func<TObj, TScore> key_extractor, int set_size_limit = int.MaxValue)
		{
			this.key_extractor = key_extractor;
			this.Limit = System.Math.Min(set_size_limit, 50000);
#warning fix fix
			this.maxes_stack = new TScore[this.Limit];
			this.c_maxes = 0;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Func<TObj, TScore> key_extractor;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly int Limit;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		TScore[] maxes_stack;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int c_maxes;

		[DebuggerDisplay("({c_maxes}) {alib.Enumerable._enumerable_ext.StringJoin(this.Scores,\" \"),nq}")]
		public TScore[] Scores
		{
			get
			{
				var _tmp = new TScore[c_maxes];
				System.Array.Copy(maxes_stack, _tmp, c_maxes);
				return _tmp;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TScore MaxScore { get { return maxes_stack[c_maxes - 1]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TScore MinScore { get { return maxes_stack[0]; } }

		void insert_max(TScore max)
		{
			int ix = 0;
			if (c_maxes > 0)
			{
				if ((ix = System.Array.BinarySearch(maxes_stack, 0, c_maxes, max)) >= 0)
					return;
				System.Array.Copy(maxes_stack, ix = ~ix, maxes_stack, ix + 1, c_maxes - ix);
			}
			maxes_stack[ix] = max;
			c_maxes++;
		}

		TObj[] get_score_items(TScore score, out int ix)
		{
			ix = -1;
			if (c_maxes == 0 || (ix = System.Array.BinarySearch(maxes_stack, 0, c_maxes, score)) < 0)
				return alib.Collections.Collection<TObj>.None;

			if (c_maxes == 1)
				return this.ToArray();

			return this.Where(x => key_extractor(x).CompareTo(score) == 0).ToArray();
		}

		public TObj[] GetScoreItems(TScore score)
		{
			int _;
			return get_score_items(score, out _);
		}

		public TObj[] RemoveScoreItems(TScore score)
		{
			int ix;
			TObj[] ret;

			if ((ret = get_score_items(score, out ix)).Length == 0)
				return ret;

			if (--c_maxes == 0)
			{
				Debug.Assert(ix == 0 && key_extractor(ret[ix]).CompareTo(score) == 0);
				base.Clear();
			}
			else
			{
				System.Array.Copy(maxes_stack, ix + 1, maxes_stack, ix, c_maxes - ix);
				base.ExceptWith(ret);
			}
			return ret;
		}

		public new bool Add(TObj _new)
		{
			TScore x_new, x_max;

			if (c_maxes == 0)
			{
				if (!base.Add(_new))
					throw new Exception();
				x_new = key_extractor(_new);
			}
			else
			{
				if (base.Contains(_new))
					return false;

				bool f_limit = Count >= Limit;

				x_new = key_extractor(_new);
				x_max = MaxScore;

				var d = x_new.CompareTo(x_max);

				if (f_limit && d > 0)			// allowing overage in the case of x_new==x_max
					return false;

				if (!base.Add(_new))
					throw new Exception();

				if (f_limit && d < 0)
					RemoveScoreItems(x_max);
			}

			insert_max(x_new);

			//Debug.Print("{0} {1}", ("(" + Count + ")").PadLeft(5), alib.Enumerable._enumerable_ext.StringJoin(this.Scores, " "));

			return true;
		}

		public new void Clear()
		{
			c_maxes = 0;
			base.Clear();
		}

		public new void SymmetricExceptWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void UnionWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void IntersectWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void ExceptWith(IEnumerable<TObj> other) { throw not.impl; }
		public new int RemoveWhere(Predicate<TObj> match) { throw not.impl; }
		public new bool Remove(TObj _rem) { throw not.impl; }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class MinMaxSet<TObj, TScore> : HashSet<TObj>
		where TScore : IComparable<TScore>
	{
		public MinMaxSet(Func<TObj, TScore> key_extractor)
		{
			this.key_extractor = key_extractor;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Func<TObj, TScore> key_extractor;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TScore MaxScore { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TScore MinScore { get; set; }

		public new bool Add(TObj _new)
		{
			if (!base.Add(_new))
				return false;

			var x_new = key_extractor(_new);

			if (base.Count == 1)
				MinScore = MaxScore = x_new;
			else
			{
				if (x_new.CompareTo(MaxScore) > 0)
					MaxScore = x_new;
				else if (x_new.CompareTo(MinScore) < 0)
					MinScore = x_new;
			}

			//Debug.Print("{0} {1}", ("(" + Count + ")").PadLeft(5), alib.Enumerable._enumerable_ext.StringJoin(this.Scores, " "));

			return true;
		}

		public new void Clear() { throw not.impl; }
		public new void SymmetricExceptWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void UnionWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void IntersectWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void ExceptWith(IEnumerable<TObj> other) { throw not.impl; }
		public new int RemoveWhere(Predicate<TObj> match) { throw not.impl; }
		public new bool Remove(TObj _rem) { throw not.impl; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class MinMaxSet2<TObj> : HashSet<TObj>
		where TObj : IEquatable<TObj>
	{
		public MinMaxSet2(IComparer<TObj> comparer)
		{
			this.comparer = comparer;
			this._maxes = new Collections.RefList<TObj>();
			this._mins = new Collections.RefList<TObj>();
		}

		alib.Collections.RefList<TObj> _maxes, _mins;


		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IComparer<TObj> comparer;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<TObj> Maxes { get { return _maxes; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<TObj> Mins { get { return _mins; } }

		public new bool Add(TObj _new)
		{
			if (!base.Add(_new))
				return false;

			int c;
			if (base.Count == 1)
			{
				_maxes.Add(_new);
				_mins.Add(_new);
			}
			else
			{
				if ((c = comparer.Compare(_new, _maxes[0])) >= 0)
				{
					if (c > 0)
						_maxes.Clear();
					_maxes.Add(_new);
				}
				if ((c = comparer.Compare(_new, _mins[0])) <= 0)
				{
					if (c < 0)
						_mins.Clear();
					_mins.Add(_new);
				}
			}

			//Debug.Print("{0} {1}", ("(" + Count + ")").PadLeft(5), alib.Enumerable._enumerable_ext.StringJoin(this.Scores, " "));

			return true;
		}

		public new void Clear() { throw not.impl; }
		public new void SymmetricExceptWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void UnionWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void IntersectWith(IEnumerable<TObj> other) { throw not.impl; }
		public new void ExceptWith(IEnumerable<TObj> other) { throw not.impl; }
		public new int RemoveWhere(Predicate<TObj> match) { throw not.impl; }
		public new bool Remove(TObj _rem) { throw not.impl; }
	};
}
