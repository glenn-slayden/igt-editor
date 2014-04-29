using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace alib.Tally
{
	[DebuggerDisplay("{ToString(),nq}")]
	public struct Tally<T>
	{
		public Tally(T item, int count)
		{
			this.Item = item;
			this.Count = count;
		}
		public readonly T Item;
		public int Count;
		public override string ToString()
		{
			return System.String.Format("[{0}] Tally:{1}", Item.ToString(), Count);
		}
	};

	public interface ITallySet<T> : IEnumerable<Tally<T>>, alib.Enumerable._ICollection<T>//, alib.Enumerable._IList<T>
	{
		int this[T item] { get; }
	};

	[DebuggerDisplay("count={Count}")]
	public class TallySet<T> : Dictionary<T, int>, ITallySet<T>
	{
		public TallySet(IEqualityComparer<T> c)
			: base(c)
		{
		}
		public TallySet()
		{
		}
		public bool Add(T item)
		{
			int i;
			if (base.TryGetValue(item, out i))
			{
				base[item] = i + 1;
				return false;
			}
			base.Add(item, 1);
			return true;
		}

		void ICollection<T>.Add(T item) { Add(item); }

		public new bool Remove(T item)
		{
			if (--base[item] > 0)
				return false;
			base.Remove(item);
			return true;
		}

		public bool Contains(T item)
		{
			return base.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Keys.CopyTo(array, arrayIndex);
		}

		void ICollection.CopyTo(System.Array array, int index)
		{
			foreach (T t in base.Keys)
				array.SetValue(t, index++);
		}

		public bool IsReadOnly { get { return false; } }

		public new IEnumerator<Tally<T>> GetEnumerator()
		{
			return ((Dictionary<T, int>)this).Select(kvp => new Tally<T>(kvp.Key, kvp.Value)).GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return Keys.GetEnumerator();
		}
	};

	public static class Ext
	{
		/// <summary>
		/// Determine tallies for each distinct group of source elements
		/// </summary>
		public static ITallySet<TSrc> ToTallies<TSrc>(this IEnumerable<TSrc> seq)
		{
			TallySet<TSrc> d = new TallySet<TSrc>();
			foreach (TSrc t in seq)
				if (d.ContainsKey(t))
					d[t]++;
				else
					d.Add(t, 1);
			return d;
		}
		/// <summary>
		/// Determine tallies for each distinct group of source elements
		/// </summary>
		public static ITallySet<TSrc> ToTallies<TSrc>(this IEnumerable<TSrc> seq, IEqualityComparer<TSrc> c)
		{
			TallySet<TSrc> d = new TallySet<TSrc>(c);
			foreach (TSrc t in seq)
				if (d.ContainsKey(t))
					d[t]++;
				else
					d.Add(t, 1);
			return d;
		}

		/// <summary>
		/// Return tallies for the distinct groups of source elements, determined according to the key selector. 
		/// </summary>
		public static IEnumerable<Tally<TKey>> ToTallies<TSrc, TKey>(this IEnumerable<TSrc> ie, Func<TSrc, TKey> fn, IEqualityComparer<TKey> c)
		{
			Dictionary<TKey, Tally<TKey>> d = new Dictionary<TKey, Tally<TKey>>(c);
			foreach (TSrc t in ie)
			{
				Tally<TKey> tal;
				TKey k = fn(t);
				if (d.TryGetValue(k, out tal))
					d[k] = new Tally<TKey>(k, tal.Count + 1);
				else
					d.Add(k, new Tally<TKey>(k, 1));
			}
			return d.Values;
		}

		/// <summary>
		/// Determine tallies for each distinct group of dictionary values
		/// </summary>
		public static ITallySet<V> TallyValues<K, V>(this IDictionary<K, V> dict)
		{
			TallySet<V> d = new TallySet<V>();
			foreach (var kvp in dict)
				if (d.ContainsKey(kvp.Value))
					d[kvp.Value]++;
				else
					d.Add(kvp.Value, 1);
			return d;
		}

		/// <summary>
		/// Determine tallies for each distinct group of dictionary values
		/// </summary>
		public static ITallySet<V> TallyValues<K, V>(this IDictionary<K, V> dict, IEqualityComparer<V> c)
		{
			TallySet<V> d = new TallySet<V>(c);
			foreach (var kvp in dict)
				if (d.ContainsKey(kvp.Value))
					d[kvp.Value]++;
				else
					d.Add(kvp.Value, 1);
			return d;
		}
	};
}
