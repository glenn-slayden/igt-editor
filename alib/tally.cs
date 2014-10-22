using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Collections;
using alib.Enumerable;
using alib.Dictionary;

namespace alib.Tally
{
	using Array = System.Array;

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
				base[item]++;
				return false;
			}
			base.Add(item, 1);
			return true;
		}

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

		public new IEnumerator<Tally<T>> GetEnumerator()
		{
			return ((Dictionary<T, int>)this).Select(kvp => new Tally<T>(kvp.Key, kvp.Value)).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	};

	public static class _ext
	{
		/// <summary>
		/// Determine tallies for each distinct group of source elements
		/// </summary>
		public static ITallySet<TSrc> ToTallies<TSrc>(this IEnumerable<TSrc> seq)
		{
			var d = new TallySet<TSrc>();
			foreach (TSrc t in seq)
				d.Add(t);
			return d;
		}
		/// <summary>
		/// Determine tallies for each distinct group of source elements
		/// </summary>
		public static ITallySet<TSrc> ToTallies<TSrc>(this IEnumerable<TSrc> seq, IEqualityComparer<TSrc> c)
		{
			var d = new TallySet<TSrc>(c);
			foreach (TSrc t in seq)
				d.Add(t);
			return d;
		}

		/// <summary>
		/// Return tallies for the distinct groups of source elements, determined according to the key selector. 
		/// </summary>
		public static ITallySet<TKey> ToTallies<TSrc, TKey>(this IEnumerable<TSrc> ie, Func<TSrc, TKey> fn, IEqualityComparer<TKey> c)
		{
			var d = new TallySet<TKey>(c);
			foreach (TSrc t in ie)
				d.Add(fn(t));
			return d;
		}
	};
}
