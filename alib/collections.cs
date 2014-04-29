//#define __MonoCS__
using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using alib.Debugging;
using alib.Collections;
using alib.Collections.ReadOnly;
using System.Runtime.InteropServices;

namespace alib.Enumerable
{
	using String = System.String;

	public interface IClone<out T> : ICloneable
		where T : class
	{
		new T Clone();
	};

	public interface _IList<out T> : IReadOnlyList<T>, ICollection
	{
		new int Count { get; }
	};
	public interface _ICollection<T> : IReadOnlyCollection<T>, ICollection<T>, ICollection
	{
		new int Count { get; }
	};
	public interface IAddRange<T> : IReadOnlyCollection<T>
	{
		void AddRange(IEnumerable<T> rg);
	};
	public interface IAddRangeColl<T> : IAddRange<T>, _ICollection<T>
	{
	};
	public interface IAddRangeSet<T> : IAddRangeColl<T>, ISet<T>
	{
	};

	/// since we've lost covariance here anyway, we'll throw in IList to make WPF and XAML very happy
	public interface IAddRangeList<T> : _IList<T>, IList<T>, IList, IAddRangeColl<T>
	{
	};
#if false
	[StructLayout(LayoutKind.Explicit)]
	struct _union
	{
		[FieldOffset(0)]
		public Object singleton;
		[FieldOffset(0)]
		public Object[] arr;
	};
	public struct Collector<T> : _IList<T>
		where T : class
	{
		public Collector(IEnumerable<T> items)
		{
			o = default(_union);
			count = -1;

			T t;
			var arr = items as T[];
			if (arr != null)
			{
				if ((count = arr.Length) == 0)
					o.arr = Collection<T>.None;
				else if (count == 1)
					o.singleton = arr[0];
				else
					o.arr = arr;
			}
			else
			{
				new RefList<T>(items);
				if ((count = items._Count()) == 0)
				{
					o.arr = Collection<T>.None;
				}
				else if (count == 1)
				{
					o.singleton = default(T);// t;
				}
				else
				{
					o.arr = arr;
				}
			}

		}
		_union o;
		int count;

		public T this[int index]
		{
			get { return count == 1 ? (T)o.singleton : (T)o.arr[index]; }
		}

		public int Count { get { return count; } }

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public void CopyTo(System.Array array, int index)
		{
			throw new NotImplementedException();
		}

		public bool IsSynchronized
		{
			get { throw new NotImplementedException(); }
		}

		public Object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}
	};
#endif

#if false
	public sealed class Sequence<T> : _ICollection<T>
	{
		public Sequence(IEnumerable<T> seq_in)
		{
			this.seq_in = seq_in;
			this.count = seq_in.CountIfAvail<T>();
		}
		readonly IEnumerable<T> seq_in;
		int count;
		public int Count
		{
			get
			{
				int _tmp;
				if ((_tmp = count) == -1)
				{
					_tmp = 0;
					var e = seq_in.GetEnumerator();
					while (e.MoveNext())
						_tmp++;
					this.count = _tmp;
				}
				return _tmp;
			}
		}
		public IEnumerator<T> GetEnumerator()
		{
			return this.count == -1 ? new _enum(this) : seq_in.GetEnumerator();
		}
		public void CopyTo(T[] array, int index)
		{
			System.Array arr;
			if ((arr = seq_in as System.Array) != null)
				System.Array.Copy(arr, array, index);
			else
			{
				var e = GetEnumerator();
				while (e.MoveNext())
					array[index++] = e.Current;
			}
		}
		public void CopyTo(System.Array array, int index)
		{
			System.Array arr;
			if ((arr = seq_in as System.Array) != null)
				System.Array.Copy(arr, array, index);
			else
			{
				var e = GetEnumerator();
				while (e.MoveNext())
					array.SetValue(e.Current, index++);
			}
		}
		public bool Contains(T item)
		{
			var e = GetEnumerator();
			while (e.MoveNext())
				if (e.Current.Equals(item))
					return true;
			return false;
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public bool IsSynchronized { get { return false; } }
		public Object SyncRoot { get { return this; } }
		class _enum : IEnumerator<T>
		{
			public _enum(Sequence<T> obj)
			{
				this.obj = obj;
				Reset();
			}
			int c;
			Sequence<T> obj;
			IEnumerator<T> e;
			public void Reset()
			{
				this.c = 0;
				this.e = obj.seq_in.GetEnumerator();
			}
			public bool MoveNext()
			{
				bool b;
				if (!(b = e.MoveNext()) && obj.count == -1)
					obj.count = c;
				c++;
				return b;
			}
			public T Current { get { return e.Current; } }
			Object IEnumerator.Current { get { return e.Current; } }
			public void Dispose() { }
		};
		public bool IsReadOnly { get { return true; } }
		public void Add(T item) { throw not.valid; }
		public void Clear() { throw not.valid; }
		public bool Remove(T item) { throw not.valid; }
	};
#endif
	public abstract class readonly_list_seq_list_base<T> : IList
	{
		public static IAddRangeList<T> Empty = new _empty();

		public void AddRange(IEnumerable<T> rg) { throw not.valid; }
		public void Add(T item) { throw not.valid; }
		public void Insert(int index, T item) { throw not.valid; }
		public void RemoveAt(int index) { throw not.valid; }
		public bool Remove(T item) { throw not.valid; }
		public void Clear() { throw not.valid; }
		public bool IsReadOnly { get { return true; } }
		bool ICollection.IsSynchronized { get { return false; } }
		Object ICollection.SyncRoot { get { return this; } }

		bool IList.IsFixedSize { get { return true; } }
		int IList.Add(Object value) { throw not.valid; }
		void IList.Insert(int index, Object value) { throw not.valid; }
		void IList.Remove(Object value) { throw not.valid; }

		sealed class _empty : readonly_list_seq_list_base<T>, IAddRangeList<T>
		{
			public T this[int index]
			{
				get { throw new IndexOutOfRangeException(); }
				set { throw not.valid; }
			}
			public int IndexOf(T item) { return -1; }
			public bool Contains(T item) { return false; }
			public int Count { get { return 0; } }
			public void CopyTo(T[] array, int arrayIndex) { }
			public IEnumerator<T> GetEnumerator() { return Collection<T>.NoneEnumerator; }
			IEnumerator IEnumerable.GetEnumerator() { return Collection<T>.NoneEnumerator; }

			public new void Clear() { /* we'll allow it */}
		};

		int ICollection.Count { get { return ((ICollection<T>)this).Count; } }
		IEnumerator IEnumerable.GetEnumerator() { return ((ICollection<T>)this).GetEnumerator(); }
		bool IList.Contains(Object item)
		{
			return item is T && ((IList<T>)this).Contains((T)item);
		}
		int IList.IndexOf(Object item)
		{
			return item is T ? ((IList<T>)this).IndexOf((T)item) : -1;
		}
		Object IList.this[int index]
		{
			get { return ((IList<T>)this)[index]; }
			set { ((IList<T>)this)[index] = (T)value; }
		}
		void ICollection.CopyTo(System.Array array, int index)
		{
			var e = ((ICollection<T>)this).GetEnumerator();
			while (e.MoveNext())
				array.SetValue(e.Current, index);
		}
	};


	[DebuggerDisplay("{_disp(),nq}")]
	public sealed class UnaryCollection<T> : readonly_list_seq_list_base<T>, IAddRangeList<T>
	{
		public T unary;
		public int Count { get { return 1; } }

		public UnaryCollection(T unary) { this.unary = unary; }
		public UnaryCollection(IEnumerable<T> single_element_seq)
		{
			var e = single_element_seq.GetEnumerator();
			e.MoveNext();
			unary = e.Current;
		}
		public IEnumerator<T> GetEnumerator() { return new _enum(unary); }

		public void CopyTo(T[] array, int arrayIndex) { array[arrayIndex] = unary; }
		public bool Contains(T item) { return item.Equals(unary); }
		public int IndexOf(T item) { return item.Equals(unary) ? 0 : -1; }
		public T this[int index]
		{
			get { if (index != 0) throw new IndexOutOfRangeException(); return unary; }
			set { throw not.valid; }
		}

		public sealed class _enum : IEnumerator<T>
		{
			readonly T elem;
			int i;
			public _enum(T elem) { this.elem = elem; i = -1; }
			public bool MoveNext() { return ++i == 0; }
			public T Current { get { return elem; } }
			Object IEnumerator.Current { get { return elem; } }
			public void Reset() { i = -1; }
			public void Dispose() { }
		};
#if DEBUG
		public override String ToString() { return unary.ToString(); }
		String _disp() { return unary.ToString(); }
#endif
	};

	/// <summary>
	/// Warning: do not cast this to 'List(T)' or the distinct monitoring will be lost
	/// </summary>
	public class DistinctList<T> : List<T>, IAddRangeList<T>
	{
		static bool f_vt;
		static DistinctList() { f_vt = typeof(T).IsValueType; }

		public DistinctList(IEqualityComparer<T> cmp)
		{
			if (cmp != EqualityComparer<T>.Default)
				this.cmp = cmp;
		}
		public DistinctList(ICollection<T> items)
			: base(items)
		{
		}
		public DistinctList()
		{
		}

		readonly IEqualityComparer<T> cmp;
		public IEqualityComparer<T> Comparer { get { return cmp; } }

		bool _equals(T a, T b)
		{
			return cmp != null ? cmp.Equals(a, b) : f_vt ? a.Equals(b) : (Object)a == (Object)b;
		}

		public new int IndexOf(T b)
		{
			if (cmp == null)
				return base.IndexOf(b);
			int c = base.Count;
			for (int i = 0; i < c; i++)
				if (f_vt ? base[i].Equals(b) : (Object)base[i] == (Object)b)
					return i;
			return -1;
		}

		public new bool Contains(T b) { return this.IndexOf(b) != -1; }

		public new T this[int i]
		{
			get { return base[i]; }
			set
			{
				if (_equals(base[i], value))
					return;
				if (this.Contains(value))
					throw not.valid;
				base[i] = value;
			}
		}

		public new void Add(T item)
		{
			if (!this.Contains(item))
				base.Add(item);
		}

		public new void AddRange(IEnumerable<T> items)
		{
			T t;
			var e = items.GetEnumerator();
			while (e.MoveNext())
				if (!this.Contains(t = e.Current))
					base.Add(t);
		}
		public new void Insert(int index, T item)
		{
			if (this.Contains(item))
				throw not.valid;
			base.Insert(index, item);
		}
		public new void InsertRange(int ix, IEnumerable<T> items)
		{
			throw not.impl;
		}
		public new bool Remove(T item)
		{
			int ix = this.IndexOf(item);
			if (ix == -1)
				return false;
			base.RemoveAt(ix);
			return true;
		}
	};

	public class HashSetSequence<T> : HashSet<T>, IAddRangeSet<T>
	{
		public HashSetSequence(IEnumerable<T> items, IEqualityComparer<T> cmp)
			: base(items ?? Collection<T>.None, cmp ?? EqualityComparer<T>.Default)
		{
		}
		public HashSetSequence(IEqualityComparer<T> cmp)
			: this(default(IEnumerable<T>), cmp)
		{
		}
		public HashSetSequence(IEnumerable<T> items)
			: this(items, default(IEqualityComparer<T>))
		{
		}
		public HashSetSequence()
			: this(default(IEnumerable<T>))
		{
		}

		public void AddRange(IEnumerable<T> items)
		{
			var e = items.GetEnumerator();
			while (e.MoveNext())
				base.Add(e.Current);
		}
		public void CopyTo(System.Array array, int index)
		{
			var e = base.GetEnumerator();
			while (e.MoveNext())
				array.SetValue(e.Current, index++);
		}
		public bool IsSynchronized { get { return false; } }
		public Object SyncRoot { get { return this; } }
	};


	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class _coll_prepend<T> : readonly_list_seq_list_base<T>, IAddRangeList<T>
	{
		readonly IEnumerable<T> ie;
		readonly T element;
		readonly int c;
		public _coll_prepend(T element, IEnumerable<T> ie, int c)
		{
			if (ie == null)
				throw new ArgumentNullException();
			this.ie = ie;
			this.element = element;
			this.c = c + 1;
		}
		public _coll_prepend(T element, ICollection<T> ie)
			: this(element, ie, ie.Count)
		{
		}

		public int IndexOf(T item)
		{
			if (element.Equals(item))
				return 0;
			var e = ie.GetEnumerator();
			for (int i = 1; e.MoveNext(); i++)
				if (e.Current.Equals(item))
					return i;
			return -1;
		}

		public T this[int index]
		{
			get
			{
				if (index == 0)
					return element;
				index--;
				IList<T> il;
				if ((il = ie as IList<T>) != null)
					return il[index];
				var e = ie.GetEnumerator();
				for (int i = 0; i < index; i++)
					if (!e.MoveNext())
						throw new IndexOutOfRangeException();
				return e.Current;
			}
			set { throw not.valid; }
		}
		public int Count { get { return c; } }
		public bool Contains(T item) { return this.IndexOf(item) != -1; }
		public void CopyTo(T[] array, int index)
		{
			array[index++] = element;
			var e = ie.GetEnumerator();
			while (e.MoveNext())
				array[index++] = e.Current;
		}
		public IEnumerator<T> GetEnumerator() { return new _enum(this); }
#if DEBUG
		public override String ToString() { return this.StringJoin(" "); }
#endif
		class _enum : IEnumerator<T>
		{
			int i;
			_coll_prepend<T> obj;
			IEnumerator<T> e_in;
			public _enum(_coll_prepend<T> obj)
			{
				this.obj = obj;
				this.i = -1;
				this.e_in = obj.ie.GetEnumerator();
			}
			public T Current { get { return i == 0 ? obj.element : e_in.Current; } }
			Object IEnumerator.Current { get { return i == 0 ? obj.element : e_in.Current; } }
			public bool MoveNext() { return ++i == 0 || e_in.MoveNext(); }
			public void Reset() { e_in.Reset(); i = -1; }
			public void Dispose() { }
		};
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class _coll_append<T> : readonly_list_seq_list_base<T>, IAddRangeList<T>
	{
		readonly ICollection<T> c_in;
		readonly T element;
		readonly int c;
		public _coll_append(ICollection<T> c_in, T element)
		{
			if (c_in == null)
				throw new ArgumentNullException();
			this.c_in = c_in;
			this.element = element;
			this.c = c_in.Count + 1;
		}
		public int IndexOf(T item)
		{
			if (element.Equals(item))
				return c - 1;
			var e = c_in.GetEnumerator();
			for (int i = 0; e.MoveNext(); i++)
				if (e.Current.Equals(item))
					return i;
			return -1;
		}

		public T this[int index]
		{
			get
			{
				if (index == c - 1)
					return element;
				IList<T> il;
				if ((il = c_in as IList<T>) != null)
					return il[index];
				var e = c_in.GetEnumerator();
				for (int i = 0; i < index; i++)
					if (!e.MoveNext())
						throw new IndexOutOfRangeException();
				return e.Current;
			}
			set { throw not.impl; }
		}
		public IEnumerator<T> GetEnumerator() { return new _enum(this); }
		public int Count { get { return c; } }
		public bool Contains(T item) { return element.Equals(item) || c_in.Contains(item); }
		public void CopyTo(T[] array, int arrayIndex)
		{
			c_in.CopyTo(array, arrayIndex);
			arrayIndex += c_in.Count;
			array[arrayIndex] = element;
		}
#if DEBUG
		public override String ToString() { return this.StringJoin(" "); }
#endif
		class _enum : IEnumerator<T>
		{
			int i;
			_coll_append<T> obj;
			IEnumerator<T> e_in;
			public _enum(_coll_append<T> obj)
			{
				this.obj = obj;
				this.i = 0;
				this.e_in = obj.c_in.GetEnumerator();
			}
			public T Current { get { return i == -2 ? obj.element : e_in.Current; } }
			Object IEnumerator.Current { get { return i == -2 ? obj.element : e_in.Current; } }
			public bool MoveNext()
			{
				if (i == -2)
					return false;
				i++;
				if (e_in.MoveNext())
					return true;
				Debug.Assert(i == obj.Count);
				i = -2;
				return true;
			}
			public void Reset() { e_in.Reset(); i = 0; }
			public void Dispose() { }
		};
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class _coll_duple<T> : readonly_list_seq_list_base<T>, IAddRangeList<T>
	{
		public readonly T t0, t1;
		public _coll_duple(T t0, ICollection<T> c_in)
		{
			if (c_in.Count != 1 || t0 == null)
				throw new ArgumentNullException();
			this.t0 = t0;
			var u = c_in as UnaryCollection<T>;
			if (u != null)
				this.t1 = u.unary;
			else
			{
				var e = c_in.GetEnumerator();
				e.MoveNext();
				this.t1 = e.Current;
			}
		}

		public T this[int index]
		{
			get
			{
				if ((index & 0xFFFFFFFE) != 0)
					throw new IndexOutOfRangeException();
				return index == 0 ? t0 : t1;
			}
			set { throw not.impl; }
		}
		public int Count { get { return 2; } }
		public int IndexOf(T item) { return t0.Equals(item) ? 0 : t1.Equals(item) ? 1 : -1; }
		public bool Contains(T item) { return t0.Equals(item) || t1.Equals(item); }
		public void CopyTo(T[] array, int ix) { array[ix++] = t0; array[ix] = t1; }
		public IEnumerator<T> GetEnumerator() { return new _enum(this); }
#if DEBUG
		public override String ToString()
		{
			return String.Format("{0} {1}", t0.ToString(), t1.ToString());
		}
#endif
		class _enum : IEnumerator<T>
		{
			int i;
			_coll_duple<T> obj;
			public _enum(_coll_duple<T> obj)
			{
				this.obj = obj;
				this.i = -1;
			}
			public T Current { get { return i == 0 ? obj.t0 : obj.t1; } }
			Object IEnumerator.Current { get { return i == 0 ? obj.t0 : obj.t1; } }
			public bool MoveNext() { return ++i < 2; }
			public void Reset() { i = -1; }
			public void Dispose() { }
		};
	};

	public class AddRangeList<T> : List<T>, IAddRangeList<T>
	{
		public AddRangeList() { }
		public AddRangeList(int capacity) : base(capacity) { }
		public AddRangeList(IEnumerable<T> collection) : base(collection) { }
	};

#if false
	public class AddRangeQueue<T> : Queue<T>, IAddRange<T>
	{
		public AddRangeQueue() { }
		public AddRangeQueue(int capacity) : base(capacity) { }
		public AddRangeQueue(IEnumerable<T> collection) : base(collection) { }
		public void Add(T t) { base.Enqueue(t); }
		public virtual void AddRange(IEnumerable<T> rg)
		{
			foreach (T t in rg)
				base.Enqueue(t);
		}
	};
	public class AddRangeCallbackQueue<T> : AddRangeQueue<T>
	{
		public AddRangeCallbackQueue(int c_expected_ranges, Action<AddRangeCallbackQueue<T>> callback)
			: base(c_expected_ranges)
		{
			this.c_expected_ranges = c_expected_ranges;
			this.callback = callback;
		}
		int c_expected_ranges;
		Action<AddRangeCallbackQueue<T>> callback;
		public override void AddRange(IEnumerable<T> rg)
		{
			base.AddRange(rg);
			if (callback != null && --c_expected_ranges == 0)
				callback(this);
		}
	};

	/// <summary>
	/// Case-insensitive (ASCII, not culture-aware)
	/// </summary>
	[DebuggerDisplay("{ToString(),nq}")]
	public class CharMap<T>
	{
		public CharMap(String s)
		{
			if (s.Length == 0)
				throw new Exception();

			var r = new Range(s);
			this._offs = r.Min - 1;
			this._map = new T[r.Extent + 1];
			this.c = 0;
		}
		public CharMap(Char[] iech)
			: this(new String(iech))
		{
		}

		public void SetDefaultValue(T default_value)
		{
			//if (default_value.Equals(default(T)))
			//	return;
			for (int i = 0; i < _map.Length; i++)
				_map[i] = default_value;
			this.c = 0;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly T[] _map;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly int _offs;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int c;

		public T this[Char ch]
		{
			get { return _map[get_index(ch)]; }
			set
			{
				int ix = get_index(ch);
				if (ix > 0 && !_map[ix].Equals(value))
				{
					if (!_map[ix].Equals(DefaultValue))
						c--;
					if (!(_map[ix] = value).Equals(DefaultValue))
						c++;
				}
			}
		}

		int get_index(Char ch) { return clamp((ch & 0xFFDF) - _offs); }

		int clamp(int ix1) { return ix1 < 0 || ix1 >= _map.Length ? 0 : ix1; }

		Char reverse(int ix1) { return (Char)(ix1 + _offs); }

		public T DefaultValue { get { return _map[0]; } }
		public int MapSize { get { return _map.Length - 1; } }
		public int Count { get { return c; } }
		public MapEntry First { get { return new MapEntry(this, 1); } }
		public MapEntry Last { get { return new MapEntry(this, _map.Length - 1); } }

		[DebuggerDisplay("{ToString(),nq}")]
		public struct MapEntry
		{
			public MapEntry(CharMap<T> charmap, int ix)
			{
				this.Index = (this.charmap = charmap).clamp(ix);
			}
			readonly CharMap<T> charmap;
			public readonly int Index;

			public Char Key { get { return charmap.reverse(Index); } }
			public T Value { get { return charmap._map[Index]; } }

			public override String ToString()
			{
				return String.Format("{0,4} '{1}'  [{2}]", Index, Key, Value);
			}
		};

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		MapEntry[] _dbg_used { get { return _dbg_used_fn().ToArray(); } }
		IEnumerable<MapEntry> _dbg_used_fn()
		{
			var d = this.DefaultValue;
			for (int ix = 1; ix < _map.Length; ix++)
				if (!_map[ix].Equals(d))
					yield return new MapEntry(this, ix);
		}
		//IEnumerable<MapEntry> _dbg_all_fn
		//{
		//	get
		//	{
		//		for (int ix = 1; ix < _map.Length; ix++)
		//			yield return new MapEntry(this, ix);
		//	}
		//}

		public override String ToString()
		{
			if (_map == null)
				return "(null)";
			return String.Format("Size={0} (/{1}/ - /{2}/)  Count={3}",
				MapSize,
				First.ToString().Trim(),
				Last.ToString().Trim(),
				Count);
		}
	};
#endif
	[DebuggerDisplay("{ToString(),nq}")]
	public struct Range : IEquatable<Range>
	{
		public static Range Entire = new Range { Min = int.MinValue, Max = int.MaxValue };
		public static Range Vacant = new Range { Min = int.MaxValue, Max = int.MinValue };

		public Range(int min_max)
		{
			this.Max = this.Min = min_max;
		}
		public Range(int min, int max)
		{
			this.Min = min;
			this.Max = max;
			_normalize();
		}
		/// <summary>
		/// Case-insensitive, ASCII only
		/// </summary>
		public Range(String s_ascii)
		{
			this.Min = int.MaxValue;
			this.Max = int.MinValue;
			int c = s_ascii.Length;
			while (--c >= 0)
			{
				int x = s_ascii[c] & 0xFFDF;
				if (x < this.Min)
					this.Min = x;
				if (x > this.Max)
					this.Max = x;
			}
		}

		void _normalize()
		{
			if (Min > Max)
				this = Vacant;
		}
		public int Min;
		public int Max;
		public bool Any { get { return Max >= Min; } }
		public bool None { get { return Min > Max; } }
		public int Extent
		{
			get { return Max - Min + 1; }
		}
		public int Size
		{
			get { return Max - Min; }
		}
		public void Extend(Range other)
		{
			if (other.Min < this.Min)
				this.Min = other.Min;
			if (other.Max > this.Max)
				this.Max = other.Max;
		}
		public override String ToString()
		{
			return String.Format("[{0}, {1}]", Min, Max);
		}

		public bool Equals(Range other)
		{
			this._normalize();
			other._normalize();
			return this.Min == other.Min && this.Max == other.Max;
		}

		public override bool Equals(Object obj)
		{
			return obj is Range && this.Equals((Range)obj);
		}

		public override int GetHashCode()
		{
			return (int)((Min << 16) | (int)((uint)Min >> 16)) ^ Max;
		}
	};

	public class HistoryBuf<T> : IList<T> where T : class
	{
		public HistoryBuf()
		{
			Clear();
		}

		public void Add(T t)
		{
			full = history[i_put] != null;
			history[i_put++] = t;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		T[] history;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		byte i_put;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool full;

		public void Clear()
		{
			this.history = new T[256];
			this.i_put = 0;
			this.full = false;
		}

		public int Count { get { return full ? 256 : i_put; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsReadOnly { get { return false; } }

		public T this[int index]
		{
			get
			{
				if (index >= Count)
					throw new IndexOutOfRangeException();
				return history[(byte)(i_put - 1 - index)];
			}
			set { throw not.valid; }
		}

		public int IndexOf(T item) { throw not.valid; }
		public bool Contains(T item) { throw not.valid; }
		public void Insert(int index, T item) { throw not.valid; }
		public bool Remove(T item) { throw not.valid; }
		public void RemoveAt(int index) { throw not.valid; }

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T t in this)
				array[arrayIndex++] = t;
		}

		public IEnumerator<T> GetEnumerator()
		{
			var _f = full;
			byte bix = i_put;
			int c = _f ? 256 : bix;
			for (int i = 0; i < c; i++)
				yield return history[--bix];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
#if DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] _for_debug { get { return this.ToArray(); } }
#endif
	};

}

namespace alib.Collections
{
	using alib.Enumerable;

	public static class Collection<T>
	{
		static Collection()
		{
			Empty = None = new T[0];
			var ec = new _empty_coll();
			NoneCollection = ec;
			NoneEnumerator = ec;
			NoneColl = ec;
			UnaryNone = new UnaryCollection<_ICollection<T>>(NoneCollection);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly T[] None;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly IReadOnlyList<T> Empty;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly _ICollection<T> NoneCollection;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly IEnumerator<T> NoneEnumerator;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly ICollection<T> NoneColl;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly _ICollection<_ICollection<T>> UnaryNone;

		sealed class _empty_coll : IAddRangeList<T>, IAddRangeColl<T>, IAddRangeSet<T>, IEnumerator<T>
		{
			public int Count { get { return 0; } }
			public IEnumerator<T> GetEnumerator() { return this; }
			public bool Contains(T item) { return false; }
			public void CopyTo(T[] array, int arrayIndex) { }
			public bool IsReadOnly { get { return true; } }
			void ICollection.CopyTo(System.Array array, int index) { }
			void ICollection<T>.Add(T item) { throw not.valid; }
			bool ICollection<T>.Remove(T item) { throw not.valid; }
			void ICollection<T>.Clear() { throw not.valid; }
			bool ICollection.IsSynchronized { get { return true; } }
			Object ICollection.SyncRoot { get { return this; } }
			IEnumerator IEnumerable.GetEnumerator() { return this; }

			public int IndexOf(T item) { return -1; }
			void IList<T>.Insert(int index, T item) { throw not.valid; }
			void IList<T>.RemoveAt(int index) { throw not.valid; }
			T IList<T>.this[int index] { get { throw not.valid; } set { throw not.valid; } }

			T IReadOnlyList<T>.this[int index] { get { throw not.valid; } }
			void IAddRange<T>.AddRange(IEnumerable<T> rg) { throw not.valid; }

			bool IList.Contains(Object value) { return false; }
			int IList.IndexOf(Object value) { return -1; }
			bool IList.IsFixedSize { get { return true; } }
			int IList.Add(object value) { throw not.valid; }
			void IList.Insert(int index, Object value) { throw not.valid; }
			void IList.RemoveAt(int index) { throw not.valid; }
			void IList.Remove(Object value) { throw not.valid; }
			void IList.Clear() { throw not.valid; }
			Object IList.this[int index] { get { throw not.valid; } set { throw not.valid; } }

			bool ISet<T>.Overlaps(IEnumerable<T> other) { return false; }
			bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) { return true; }
			bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) { return false; }
			bool ISet<T>.IsSubsetOf(IEnumerable<T> other) { return _enumerable_ext._Count(other) == 0; }
			bool ISet<T>.IsSupersetOf(IEnumerable<T> other) { return _enumerable_ext._Count(other) == 0; }
			bool ISet<T>.SetEquals(IEnumerable<T> other) { return _enumerable_ext._Count(other) == 0; }
			bool ISet<T>.Add(T item) { throw not.valid; }
			void ISet<T>.ExceptWith(IEnumerable<T> other) { throw not.valid; }
			void ISet<T>.IntersectWith(IEnumerable<T> other) { throw not.valid; }
			void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) { throw not.valid; }
			void ISet<T>.UnionWith(IEnumerable<T> other) { throw not.valid; }

			bool IEnumerator.MoveNext() { return false; }
			T IEnumerator<T>.Current { get { throw not.valid; } }
			Object IEnumerator.Current { get { throw not.valid; } }
			void IEnumerator.Reset() { }
			void IDisposable.Dispose() { }
		};
	};
}

namespace alib.Collections
{
	public static class _coll_ext
	{
		public static bool Remove<T>(this List<T> list, T item, out int ix)
		{
			if ((ix = list.IndexOf(item)) == -1)
				return false;
			list.RemoveAt(ix);
			return true;
		}

		public static int RemoveRange<T>(this List<T> list, IEnumerable<T> seq)
		{
			int c = 0;
			foreach (T t in seq)
				if (list.Remove(t))
					c++;
			return c;
		}

		public static List<T> Extract<T>(this List<T> list, Predicate<T> predicate)
		{
			int i = 0;
			List<T> newlist = new List<T>();
			while (i < list.Count)
			{
				T t = list[i];
				if (predicate(t))
				{
					newlist.Add(t);
					list.RemoveAt(i);
				}
				else
					i++;
			}
			return newlist;
		}

		public static T RemoveItem<T>(this List<T> list, T item)
		{
			int ix;
			if ((ix = list.IndexOf(item)) == -1)
				return default(T);
			T t = list[ix];
			list.RemoveAt(ix);
			return t;
		}
		public static T RemoveFirst<T>(this List<T> list)
		{
			T t = list[0];
			list.RemoveAt(0);
			return t;
		}
		public static T RemoveItemAt<T>(this List<T> list, int ix)
		{
			T t = list[ix];
			list.RemoveAt(ix);
			return t;
		}
		public static T RemoveFirst<T>(this List<T> list, Predicate<T> predicate)
		{
			int ix;
			if ((ix = list.FindIndex(predicate)) == -1)
				return default(T);
			T t = list[ix];
			list.RemoveAt(ix);
			return t;
		}
		public static T RemoveLast<T>(this List<T> list)
		{
			int ix = list.Count - 1;
			if (ix < 0)
				throw not.valid;
			T t = list[ix];
			list.RemoveAt(ix);
			return t;
		}

		static System.Reflection.FieldInfo fi_list_items;
		public static T[] ToArray<T>(this List<T> L)
		{
			if (L.Count < L.Capacity)
				return L.ToArray();
			if (fi_list_items == null)
				fi_list_items = typeof(List<T>).GetField("_items", (System.Reflection.BindingFlags)0x24);
			return (T[])fi_list_items.GetValueDirect(__makeref(L));
		}
	};

	public class ObservableStack<T> : Stack<T>, INotifyCollectionChanged//, INotifyPropertyChanged
	{
		public ObservableStack()
		{
		}

		public ObservableStack(IEnumerable<T> collection)
		{
			foreach (var item in collection)
				base.Push(item);
		}

		public ObservableStack(List<T> list)
		{
			foreach (var item in list)
				base.Push(item);
		}

		//public event PropertyChangedEventHandler PropertyChanged;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public new void Clear()
		{
			base.Clear();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public new T Pop()
		{
			var item = base.Pop();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
			return item;
		}

		public new void Push(T item)
		{
			base.Push(item);
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			this.RaiseCollectionChanged(e);
		}

		//protected void OnPropertyChanged(PropertyChangedEventArgs e)
		//{
		//    this.RaisePropertyChanged(e);
		//}

		void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (this.CollectionChanged != null)
				this.CollectionChanged(this, e);
		}

		//void RaisePropertyChanged(PropertyChangedEventArgs e)
		//{
		//    if (this.PropertyChanged != null)
		//        this.PropertyChanged(this, e);
		//}

		//event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		//{
		//    add { this.PropertyChanged += value; }
		//    remove { this.PropertyChanged -= value; }
		//}
	};

	public class ObservableQueue<T> : Queue<T>, INotifyCollectionChanged//, INotifyPropertyChanged
	{
		public ObservableQueue()
		{
		}

		public ObservableQueue(IEnumerable<T> collection)
		{
			foreach (var item in collection)
				base.Enqueue(item);
		}

		public ObservableQueue(List<T> list)
		{
			foreach (var item in list)
				base.Enqueue(item);
		}

		//public event PropertyChangedEventHandler PropertyChanged;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public new void Clear()
		{
			base.Clear();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public new void Enqueue(T item)
		{
			base.Enqueue(item);
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		public new T Dequeue()
		{
			var item = base.Dequeue();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
			return item;
		}

		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			this.RaiseCollectionChanged(e);
		}

		//protected void OnPropertyChanged(PropertyChangedEventArgs e)
		//{
		//    this.RaisePropertyChanged(e);
		//}

		void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (this.CollectionChanged != null)
				this.CollectionChanged(this, e);
		}

		//void RaisePropertyChanged(PropertyChangedEventArgs e)
		//{
		//    if (this.PropertyChanged != null)
		//        this.PropertyChanged(this, e);
		//}

		//event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		//{
		//    add { this.PropertyChanged += value; }
		//    remove { this.PropertyChanged -= value; }
		//}
	};
}
