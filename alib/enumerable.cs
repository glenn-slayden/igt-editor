//#define __MonoCS__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using alib.Collections;
using alib.Collections.ReadOnly;
using alib.Debugging;

namespace alib.Enumerable
{
	using String = System.String;
	using Array = System.Array;
	using arr = alib.Array.arr;

	public static class enum_ext
	{
		public static IEnumerable<TElem> Coalesce<TElem>(this IEnumerable<TElem> a, IEnumerable<TElem> b)
		{
			IEnumerable<TElem> x = (a ?? b), y = (b ?? a);
			return x == y ? (x ?? y ?? System.Linq.Enumerable.Empty<TElem>()) : x.Concat(y);
		}

		public static IEnumerable<T> AssertAll<T>(this IEnumerable<T> seq, Predicate<T> condition)
		{
#if DEBUG
			IEnumerator<T> e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				T t;
				Debug.Assert(condition(t = e.Current));
				yield return t;
			}
#else
			return seq;
#endif
		}

		public static IEnumerable<T> AssertFirst<T>(this IEnumerable<T> seq, Predicate<T> condition)
		{
#if DEBUG
			IEnumerator<T> e;
			Debug.Assert((e = seq.GetEnumerator()).MoveNext());
			Debug.Assert(condition(e.Current));
#endif
			return seq;
		}


		public static IEnumerable<T> AssertSequence<T>(this IEnumerable<T> seq, Predicate<IEnumerable<T>> condition)
		{
#if DEBUG
			Debug.Assert(condition(seq));
#endif
			return seq;
		}

		[DebuggerDisplay("{this.Index.ToString().PadLeft(3),nq} {this.Item.ToString(),ac,nq}")]
		public struct IndexTagged<T>
		{
			public int Index;
			public T Item;
		}

		public static IEnumerable<IndexTagged<T>> TagIndex<T>(this IEnumerable<T> seq)
		{
			int i = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				yield return new IndexTagged<T> { Index = i, Item = e.Current };
				i++;
			}
		}

		public static IEnumerable<int> IndicesWhere<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
		{
			int i = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if (predicate(e.Current))
					yield return i;
				i++;
			}
		}

		public static unsafe bool Contains(this String s, Char ch)
		{
			char* p;
			bool b;
			fixed (char* _p = s)
			{
				p = _p + s.Length;
				while ((b = (p > _p)) && ch != *--p)
					;
			}
			return b;
		}

		public static int WriteToArray<T>(this IEnumerable<T> ie, T[] rgt)
		{
			if (ie is T[])
				throw new Exception();
			int i = 0;
			ICollection ic = ie as ICollection;
			if (ic != null)
			{
				ic.CopyTo(rgt, 0);
				i = ic.Count;
			}
			else
			{
				IEnumerator<T> e = ie.GetEnumerator();
				while (e.MoveNext())
					rgt[i++] = e.Current;
			}
			return i;
		}

		/// <summary>
		/// always-copy semantics except for zero-sized array which will always return the same object
		/// </summary>
		public static T[] ToArray<T>(this T[] to_copy)
		{
			int c;
			if ((c = to_copy.Length) == 0)
				return alib.Collections.Collection<T>.None;
			var ret = new T[c];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = to_copy[i];
			return ret;
		}

		public static T[] ToArray<T>(this IEnumerable<T> ie, int max_hint)
		{
			T[] rgt;

			ICollection ic;
			if ((ic = ie as ICollection) != null)
			{
				rgt = new T[ic.Count];
				ic.CopyTo(rgt, 0);
			}
			else
			{
				T[] rgx = new T[max_hint];
				int i = 0;
				IEnumerator<T> e = ie.GetEnumerator();
				while (e.MoveNext())
					rgx[i++] = e.Current;
				if (i == max_hint)
					return rgx;
				Array.Copy(rgx, 0, rgt = new T[i], 0, i);
			}
			return rgt;
		}

		public static T[] ToArray<T>(this ICollection ic)
		{
			T[] rgt;
			if ((rgt = ic as T[]) == null)
			{
				rgt = new T[ic.Count];
				ic.CopyTo(rgt, 0);
			}
			return rgt;
		}


		public static List<T> ToList<T>(this IEnumerable<T> ie, int max_hint)
		{
			Debug.Assert(!(ie is ICollection));
			List<T> rgx;
			(rgx = new List<T>(max_hint)).AddRange(ie);
			Debug.Assert(rgx.Count <= max_hint);
			return rgx;
		}
		public static bool TryGetFirst<T>(this IEnumerable<T> seq, out T elem)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
			{
				elem = default(T);
				return false;
			}
			elem = e.Current;
			return true;
		}
		public static bool TryGetFirst<T>(this IEnumerable<T> seq, Func<T, bool> match, out T elem)
		{
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if (match(elem = e.Current))
					return true;
			}
			elem = default(T);
			return false;
		}

#if false
		public struct Mapping
		{
			public Mapping(int i_inner, int i_outer)
			{
				this.i_inner = i_inner;
				this.i_outer = i_outer;
			}
			public readonly int i_inner;
			public readonly int i_outer;
		};

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Enumerate all of the ways that every element of 'inner' can be mapped to an element of 'outer'
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<Mapping> Map<T>(IList<T> inner, IList<T> outer, IEqualityComparer<T> ceq)
		{
			for (int ii = 0; ii < inner.Count; ii++)
			{
				T i = inner[ii];
				for (int io = 0; io < outer.Count; io++)
				{
					T o = outer[io];

					if (ceq.Equals(i, o))
						yield return new Mapping(ii, io);
				}
			}
		}
#endif

		//public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossProduct<T>(this IEnumerable<IGrouping<T, T>> seq)
		//{
		//    IEnumerator<IGrouping<T, T>> ie = seq.GetEnumerator();
		//    if (!ie.MoveNext())
		//        yield break;
		//    IGrouping<T, T> cur = ie.Current;
		//    bool fany = ie.MoveNext();
		//    foreach (var yy in cur)
		//    {
		//        Pairing<T> pr = new Pairing<T>(cur.Key, yy);
		//        if (!fany)
		//            yield return new Pairing<T>[] { pr };
		//        else
		//            foreach (var zz in GroupCrossProduct<T>(seq.Skip(1)))
		//                yield return zz.Prepend(pr);
		//    }
		//}

#if UNIT_TEST_FOR_GroupCrossProduct
		String[] foo = { "qab", "r", "sab" };

		var gg = foo.SelectMany(s => s.Skip(1).Select(r => new { a = s[0], b = r })).GroupBy(s => s.a, s => s.b).ToArray();


		foreach (var sfddf in alib.Enumerable.Extensions.GroupCrossProduct<Char>(gg, EqualityComparer<Char>.Default))
		{
			foreach (var asdf in sfddf)
				Console.WriteLine("{0} - {1}", asdf.x, asdf.y);
			Console.WriteLine();

		}
		Environment.Exit(0);

	[DebuggerDisplay("{ch}")]
	class xyz// : List<xyz>
	{
		public xyz(Char ch)
		{
			this.ch = ch;
			//foreach (char c in sss)
			//    base.Add(new xyz();
		}
		public Char ch;

	}

	//static Char[][][] foo = new[] { new[] { "abc".ToCharArray(), "xyz".ToCharArray(), "def".ToCharArray() } };

#endif
#if false
		/// <summary>
		/// From a sequence of groupings where each grouping correlates a key of type T with some set of 'matched items'--each 
		/// item being also of type T--generate all possible complete 'match-ups' where every key is associated with exactly 
		/// one item from its matches. Each complete match-up is further subject to the constraint that all of the items its 
		/// keys select are set-distinct according to their default equality comparer.
		/// </summary>
		/// <param name="seq">Input sequence of groupings which associate a key with a set of 'matched items'</param>
		/// <returns>
		/// All possible sets of pairings between a key and one item from its group, where each set has exactly one pairing per 
		/// key from the input sequence</returns>
		public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossProduct<T>(this IEnumerable<IGrouping<T, T>> seq)
		{
			return GroupCrossProduct(seq, EqualityComparer<T>.Default);
		}
#endif

		/// <summary>
		/// From a sequence of groupings where each grouping correlates a key of type T with some set of 'matched items'--each 
		/// item being also of type T--generate all possible complete 'match-ups' where every key is associated with exactly 
		/// one item from its matches. Each complete match-up is further subject to the constraint that all of the items its 
		/// keys select are set-distinct according to the supplied equality comparer.
		/// </summary>
		/// <param name="seq">Input sequence of groupings which associate a key with a set of 'matched items'</param>
		/// <param name="cmp">Equality comparer used to determine distinctness of matched items within one set of pairings</param>
		/// <returns>
		/// All possible sets of pairings between a key and one item from its group, where each set has exactly one pairing per 
		/// key from the input sequence</returns>
#if true
		public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossProduct<T>(this IEnumerable<IGrouping<T, T>> seq, IEqualityComparer<T> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<T>.Default;

			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				yield break;

			var key = ie.Current.Key;
			var f_any = ie.MoveNext();

			foreach (var ySide in ie.Current)
			{
				Pairing<T> pr = new Pairing<T>(key, ySide);
				if (!f_any)
					yield return new[] { pr };
				else
					foreach (var rightwards in GroupCrossProduct<T>(seq.Skip(1), cmp))
						if (!rightwards.Any(so_far => cmp.Equals(ySide, so_far.y)))
							yield return rightwards.Prepend(pr);
			}
		}
#warning I believe the code below is fine and a bit improvement, but i didn't have time to test it
#elif true
		public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossProduct<T>(this IEnumerable<IGrouping<T, T>> seq, IEqualityComparer<T> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<T>.Default;

			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				yield break;

			var cur = ie.Current;
			var f_any = ie.MoveNext();

			foreach (var ySide in cur)
			{
				var rg = new[] { new Pairing<T>(cur.Key, ySide) };

				if (f_any)
					foreach (var rightwards in GroupCrossProduct<T>(seq.Skip(1), cmp))
						foreach (var so_far in rightwards)
							if (!cmp.Equals(ySide, so_far.y))
								rg = arr.Append(rg, so_far);
							else
								goto _no;
				yield return rg;
			_no:
				;
			}
		}
#else
		public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossProduct<T>(this IEnumerable<IGrouping<T, T>> seq, IEqualityComparer<T> cmp = null)
		{
			IEnumerator<IEnumerable<Pairing<T>>> e2;
			IEnumerator<Pairing<T>> e3;
			bool f_any;

			var e0 = seq.GetEnumerator();
			if (!e0.MoveNext())
				yield break;

			var key = e0.Current.Key;

			if ((f_any = e0.MoveNext()) && cmp == null)
				cmp = EqualityComparer<T>.Default;

			var e1 = e0.Current.GetEnumerator();
			while (e1.MoveNext())
			{
				var pr = new Pairing<T>(key, e1.Current);
				if (!f_any)
					yield return new[] { pr };
				else
				{
					e2 = GroupCrossProduct<T>(seq.Skip(1), cmp).GetEnumerator();
					while (e2.MoveNext())
					{
						e3 = e2.Current.GetEnumerator();
						while (e3.MoveNext())
							if (cmp.Equals(pr.y, e3.Current.y))
								goto _any;

						yield return e2.Current.Prepend(pr);
					_any:
						;
					}
				}
			}
		}
#endif

		public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossMatch<T>(this IEnumerable<IGrouping<T, T>> seq, IEqualityComparer<T> ceq)
		{
			IEnumerator<IGrouping<T, T>> ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				yield break;
			IGrouping<T, T> cur = ie.Current;
			bool f_any = ie.MoveNext();
			foreach (var ySide in cur)
			{
				if (ceq.Equals(cur.Key, ySide))
				{
					Pairing<T> pr = new Pairing<T>(cur.Key, ySide);
					if (!f_any)
						yield return new[] { pr };
					else
						foreach (var zz in GroupCrossMatch<T>(seq.Skip(1), ceq))
							yield return zz.Prepend(pr);
				}
			}
		}
		public static IEnumerable<IEnumerable<Pairing<T>>> GroupCrossMatch<T>(this IEnumerable<IGrouping<T, T>> seq, Func<T, T, bool> match_predicate)
		{
			IEnumerator<IGrouping<T, T>> ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				yield break;
			IGrouping<T, T> cur = ie.Current;
			bool f_any = ie.MoveNext();
			foreach (var ySide in cur)
			{
				if (match_predicate(cur.Key, ySide))
				{
					Pairing<T> pr = new Pairing<T>(cur.Key, ySide);
					if (!f_any)
						yield return new[] { pr };
					else
						foreach (var rightwards in GroupCrossMatch<T>(seq.Skip(1), match_predicate))
							yield return rightwards.Prepend(pr);
				}
			}
		}

		public static IEnumerable<IReadWriteCollection<T>> UnaryExpand<T>(this IEnumerable<T> seq)
		{
			if (!seq.GetEnumerator().MoveNext())
				return Collection<T>.UnaryNone;
			return new _unary_expand<T>(seq);
		}

		sealed class _unary_expand<T> : IEnumerable<IReadWriteCollection<T>>
		{
			public _unary_expand(IEnumerable<T> seq) { this.seq = seq; }
			readonly IEnumerable<T> seq;
			public IEnumerator<IReadWriteCollection<T>> GetEnumerator()
			{
				var e = seq.GetEnumerator();
				while (e.MoveNext())
					yield return new UnaryCollection<T>(e.Current);
			}
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		};

		//public static IEnumerable<IEnumerable<T>> CrossProduct<T>(this IEnumerable<IGrouping<T, T>> input)
		//{
		//    return _rightwards(input as IList<IGrouping<T, T>> ?? input.ToArray(), 0);
		//}

		public static String StringJoin(this IEnumerable seq, String sep)
		{
			if (seq == null)
				return String.Empty;
			String[] tmp;
			if ((tmp = seq as String[]) != null)
				return String.Join(sep, tmp);
			return String.Join(sep, seq);
		}

		public static void ToConsole(this String s)
		{
			Console.WriteLine(s);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String StringJoin(this String[] arr, String sep)
		{
			int c;
			if (arr == null || (c = arr.Length) == 0)
				return String.Empty;
			if (c == 1)
				return arr[0];
			if (c == 2)
				return arr[0] + sep + arr[1];
			return String.Join(sep, arr);
		}
		public static String StringJoin<T>(this T[] arr, String sep)
		{
			int c;
			if (arr == null || (c = arr.Length) == 0)
				return String.Empty;
			if (c == 1)
				return arr[0].ToString();
			if (c == 2)
				return arr[0] + sep + arr[1];
			return String.Join(sep, arr);
		}
		public static String StringJoin(this IEnumerable<String> seq, String sep)
		{
			return seq == null ? String.Empty : String.Join(sep, seq);
		}
		public static String StringJoin<T>(this IEnumerable<T> seq, String sep)
		{
			return seq == null ? String.Empty : String.Join(sep, seq);
		}
		public static unsafe String StringJoin<T>(this IEnumerable<T> seq, int item_pad_left, String sep)
		{
			if (seq == null)
				return String.Empty;
			return String.Join(sep, seq.Select(x => x.ToString().PadLeft(item_pad_left)));
		}
		public static unsafe String StringJoin<T>(this IEnumerable<T> seq, String sep, int item_pad_right)
		{
			if (seq == null)
				return String.Empty;
			return String.Join(sep, seq.Select(x => x.ToString().PadRight(item_pad_right)));

		}

		public static IEnumerable<T>[] Divide<T>(this IReadOnlyCollection<T> seq, int count)
		{
			if (count == 0)
				throw new Exception();

			var arr = new IEnumerable<T>[(seq.Count - 1) / count + 1];
			for (int i = 0, j = 0; i < arr.Length; i++, j += count)
				arr[i] = seq.Skip(j).Take(count);
			return arr;
		}
#if PARTIAL_ORDER
		public static IEnumerable<IReadOnlyList<T>> PartialOrder<T>(this IEnumerable<T> seq, IPartialOrderComparer<T> cmp)
		{
			List<T> grp;
			T cur;

			var e = seq.Sort(cmp).GetEnumerator();
			bool f = e.MoveNext();
			while (f)
			{
				grp = new List<T> { (cur = e.Current) };

				while ((f = e.MoveNext()) && cmp.Equals(cur, e.Current))
					grp.Add(e.Current);

				yield return grp;
			}
		}
#endif
		public static IEnumerable<Grouping<TArg, T>> PartitionBy<T, TArg>(this IEnumerable<T> seq, Func<T, TArg> selector)
		{
			var e = seq.GetEnumerator();

			Grouping<TArg, T> grp = null;
			T t;

			while (e.MoveNext())
			{
				var arg = selector(t = e.Current);
				if (grp != null)
				{
					if (EqualityComparer<TArg>.Default.Equals(grp.Key, arg))
						goto match;

					yield return grp;
				}
				grp = new Grouping<TArg, T>(arg);
			match:
				grp.Add(t);
			}
			if (grp != null)
				yield return grp;
		}

		public static IEnumerable<IList<T>> Partition<T>(this IEnumerable<T> seq, T partition, bool f_include_empty = false, bool f_retain_splitter = false)
		{
			return Partition(seq, t => t.Equals(partition), f_include_empty, f_retain_splitter);
		}

		public static IEnumerable<IList<T>> Partition<T>(this IEnumerable<T> seq, Func<T, bool> partition_p, bool f_include_empty = false, bool f_retain_splitter = false)
		{
			RefList<T> list = null;
			foreach (T t in seq)
			{
				if (partition_p(t))
				{
					if (list != null && list.Count > 0)
					{
						yield return list;
						list = null;
					}
					else if (f_include_empty)
						yield return RefList<T>.Empty;
					if (f_retain_splitter)
					{
						list = new RefList<T>();
						list.Add(t);
					}
				}
				else
				{
					if (list == null)
						list = new RefList<T>();
					list.Add(t);
				}
			}
			if (list != null && list.Count > 0)
				yield return list;
		}

		public static IEnumerable<Grouping<T, T>> PartitionWhere<T>(this IEnumerable<T> seq, Func<T, bool> selector)
		{
			Grouping<T, T> grp = null;
			T t;

			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if (selector(t = e.Current))
				{
					if (grp != null)
						yield return grp;
					grp = new Grouping<T, T>(t);
				}
				else
				{
					if (grp == null)
						grp = new Grouping<T, T>(default(T));

					grp.Add(t);
				}
			}
			if (grp != null)
				yield return grp;
		}

		/// <summary>
		/// Reduce a sequence of 'n' elements to 'n-1' by combining adjacent elements
		/// </summary>
		public static IEnumerable<TDst> ReduceAdjacent<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, TSrc, TDst> combiner)
		{
			IEnumerator<TSrc> iei = seq.GetEnumerator();
			if (!iei.MoveNext())
				yield break;
			TSrc i_prev = iei.Current;
			while (iei.MoveNext())
				yield return combiner(i_prev, i_prev = iei.Current);
		}

		/// <summary>
		/// Reduce a sequence of 'n' elements to 'n-1' by pairing adjacent elements. If there
		/// is only one element, it is returned as 'x' in a pairing with default(TSrc)
		/// </summary>
		public static IEnumerable<Pairing<TSrc>> ReduceAdjacent<TSrc>(this IEnumerable<TSrc> seq)
		{
			IEnumerator<TSrc> iei = seq.GetEnumerator();
			if (!iei.MoveNext())
				yield break;
			TSrc i_prev = iei.Current;
			if (!iei.MoveNext())
				yield return new Pairing<TSrc>(i_prev, default(TSrc));
			else
				do
					yield return new Pairing<TSrc>(i_prev, i_prev = iei.Current);
				while (iei.MoveNext());
		}

		public static IEnumerable<IEnumerable<T>> Window<T>(this IEnumerable<T> seq, int size)
		{
			if (size < 0)
				throw new ArgumentException();
			if (size == 0)
				return seq.Select(_ => Collection<T>.None);
			if (size == 1)
				return seq.Select(t => new T[] { t });
			if (size == 2)
				return seq.ReduceAdjacent().Select(p => p.ToArray());
			return _window(seq, size);
		}

		static IEnumerable<IEnumerable<T>> _window<T>(this IEnumerable<T> seq, int size)
		{
			int ex_count = seq._Count() - (size - 1);

			for (int i = 0; i < ex_count; i++)
				yield return seq.Skip(i).Take(size);
		}

		public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> seq)
		{
			IEnumerator<IEnumerable<T>> e;
			return (e = seq.GetEnumerator()).MoveNext() ? _sm<T>(e) : Collection<T>.None;
		}

		static IEnumerable<T> _sm<T>(IEnumerator<IEnumerable<T>> e1)
		{
			IEnumerable<T> eec;
			IReadOnlyList<T> arr;
			do
			{
				if ((arr = (eec = e1.Current) as IReadOnlyList<T>) != null)
					for (int i = 0; i < arr.Count; i++)
						yield return arr[i];
				else
				{
					var e2 = eec.GetEnumerator();
					while (e2.MoveNext())
						yield return e2.Current;
				}
			}
			while (e1.MoveNext());
		}


		public static IEnumerable<T> Flatten<T>(this IEnumerable nested_seqs)
		{
			foreach (Object o in nested_seqs)
				if (o is T)
					yield return (T)o;
				else
					foreach (T t in Flatten<T>((IEnumerable)o))
						yield return t;
		}

		public static IEnumerable<IList<T>> Corral<T>(this IEnumerable<T> seq, HashSet<T> targets)
		{
			var e = seq.GetEnumerator();
			bool f_continue = e.MoveNext();
			while (f_continue)
			{
				T t = e.Current;
				if (!targets.Contains(t))
				{
					yield return new UnaryCollection<T>(t);
					f_continue = e.MoveNext();
				}
				else
					yield return e.ToArrayCur(x => targets.Contains(x), ref f_continue);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerator<T> Enumerator<T>(this T[] arr)
		{
			if (arr == null || arr.Length == 0)
				return Collection<T>.NoneEnumerator;
			return ((IEnumerable<T>)arr).GetEnumerator();
		}

		/// <summary>
		/// Enumerator's 'Current' property is already on a valid element. The array will have at 
		/// least this one element. See 'ContinueToArray'
		/// </summary>
#if WORKS
		public static T[] ToArrayCur<T>(this IEnumerator<T> seq, Func<T, bool> pred, out bool f_continue)
		{
			T t = seq.Current; 
			T[] rg = _to_arr_cur(seq, 1, pred, out f_continue);
			rg[0] = t;
			return rg;
		}
		static T[] _to_arr_cur<T>(IEnumerator<T> seq, int c, Func<T, bool> pred, out bool f_continue)
		{
			T t;
			if (!(f_continue = seq.MoveNext()) || !pred(t = seq.Current))
				return new T[c];
			T[] rg = _to_arr_cur(seq, c + 1, pred, out f_continue);
			rg[c] = t;
			return rg;
		}
#else
		public static T[] ToArrayCur<T>(this IEnumerator<T> seq, Func<T, bool> pred, ref bool f_continue)
		{
			return _to_arr_cur(seq, 0, pred, ref f_continue);
		}
		static T[] _to_arr_cur<T>(IEnumerator<T> seq, int c, Func<T, bool> pred, ref bool f_continue)
		{
			T t = seq.Current;
			if (!pred(t))
				return new T[c];
			T[] rg = (f_continue = seq.MoveNext()) ? _to_arr_cur(seq, c + 1, pred, ref f_continue) : new T[c + 1];
			rg[c] = t;
			return rg;
		}
#endif
		/// <summary>
		/// Enumerator's 'Current' property is not included, so the array may have zero elements. See 'ToArrayCur'
		/// </summary>
		public static T[] ContinueToArray<T>(this IEnumerator<T> e)
		{
			if (!e.MoveNext())
				return alib.Collections.Collection<T>.None;

			T[] tmp = new T[5];
			int c = 0;
			while (true)
			{
				tmp[c++] = e.Current;
				if (!e.MoveNext())
					break;
				if (c == tmp.Length)
					arr.Resize(ref tmp, c * 2);
			}
			return arr.Resize(tmp, c);
		}

		/// <summary>
		/// Enumerator's 'Current' property is not included, so the enumerable may yield zero elements.
		/// </summary>
		public static IEnumerable<T> ContinueEnumerable<T>(this IEnumerator<T> e)
		{
			while (e.MoveNext())
				yield return e.Current;
		}
		public static IEnumerable<T> ConsumeEnumerator<T>(this IEnumerator<T> e)
		{
			return new _e2e<T>(e);
		}
		sealed class _e2e<T> : IEnumerable<T>
		{
			public _e2e(IEnumerator<T> e) { this.e = e; }
			IEnumerator<T> e;
			public IEnumerator<T> GetEnumerator()
			{
				var _tmp = System.Threading.Interlocked.Exchange(ref e, null);
				if (_tmp == null)
					throw new Exception("Already used");
				return _tmp;
			}
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		};

		public static T[] ToArray<T>(this IEnumerator<T> seq, Func<T, bool> pred) { return _to_arr<T>(seq, 0, pred); }
		public static T[] ToArray<T>(this IEnumerator<T> seq) { return _to_arr<T>(seq, 0); }
		static T[] _to_arr<T>(IEnumerator<T> seq, int c)
		{
			if (!seq.MoveNext())
				return new T[c];
			T t = seq.Current;
			T[] rg = _to_arr(seq, c + 1);
			rg[c] = t;
			return rg;
		}
		static T[] _to_arr<T>(IEnumerator<T> seq, int c, Func<T, bool> pred)
		{
			T t;
			if (!(seq.MoveNext() && pred(t = seq.Current)))
				return new T[c];
			T[] rg = _to_arr(seq, c + 1, pred);
			rg[c] = t;
			return rg;
		}

		public static bool SetEquals<T>(this IEnumerable<T> S1, IEnumerable<T> S2)
		{
			Debug.Assert(!(S1 is ISet<T>) && !(S2 is ISet<T>));

			var e1 = S1.GetEnumerator();
			var e2 = S2.GetEnumerator();

			// if S1 is empty, S2 must be empty
			if (!e1.MoveNext())
				return !e2.MoveNext();

			// if S1 has at least one element, S2 cannot be empty
			if (!e2.MoveNext())
				return false;

			// handle singleton case in lightweight fashion
			T t = e1.Current;
			if (!e1.MoveNext())
			{
				// if S1 has exactly one element, it must be equal to every element of S2 
				do
					if (!t.Equals(e2.Current))
						return false;
				while (e2.MoveNext());
				return true;
			}
			// build hashset for S1
			int ix;
			var hs = new alib.Hashing.IndexedHash<T>() { t };
			do
				hs.TryAdd(e1.Current, out ix);
			while (e1.MoveNext());

			// check for extras--and count distinct--in S2
			int[] rgb = new int[hs.Count];
			int c = 0;
			do
				if ((ix = hs[e2.Current]) == -1)
					return false;
				else if (++rgb[ix] == 1)
					c++;
			while (e2.MoveNext());

			// distinct count must match
			return c == hs.Count;
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> ie)
		{
			return new HashSet<T>(ie);
		}
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> ie, IEqualityComparer<T> cmp)
		{
			return new HashSet<T>(ie, cmp);
		}
		public static HashSet<TDst> ToHashSet<TSrc, TDst>(this IEnumerable<TSrc> ie, Func<TSrc, TDst> selector, IEqualityComparer<TDst> cmp)
		{
			var hs = new HashSet<TDst>(cmp);
			var e = ie.GetEnumerator();
			while (e.MoveNext())
				hs.Add(selector(e.Current));
			return hs;
		}
		public static T First<T>(this HashSet<T> hs)
		{
			if (hs.Count == 0)
				throw new Exception();
			T[] arr = new T[1];
			hs.CopyTo(arr, 0, 1);
			return arr[0];
		}
		public static T First<T>(this List<T> L)
		{
			return L[0];
		}
		public static T First<T>(this T[] arr)
		{
			return arr[0];
		}

		public static unsafe IEnumerable<T> TakeTail<T>(this IEnumerable<T> seq, int n) where T : class
		{
			if (n < 0)
				throw new ArgumentOutOfRangeException("must not be negative", "n");
			if (n == 0)
				return Collection<T>.None;

			GCHandle* resul = stackalloc GCHandle[n];
			int i = 0, j = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				resul[i] = GCHandle.Alloc(e.Current, GCHandleType.Normal);
				i = (i + 1) % n;
				j++;
			}
			if (j <= n)
				return seq;
			T[] ret = new T[n];
			for (j = 0; j < n; j++)
			{
				GCHandle* p = resul + ((i + j) % n);
				ret[j] = (T)p->Target;
				p->Free();
			}
			return ret;
		}

		public static IEnumerable<T> TrimEnds<T>(this IEnumerable<T> seq)
		{
			var e = seq.GetEnumerator();
			return e.MoveNext() && e.MoveNext() ? _except_last_1(e) : Collection<T>.None;
		}

		public static unsafe IEnumerable<T> ExceptLast<T>(this IEnumerable<T> seq, int n = 1)
		{
			if (n < 0)
				throw new ArgumentOutOfRangeException("must not be negative", "n");

			IEnumerator<T> e;
			if (n == 0 || !(e = seq.GetEnumerator()).MoveNext())
				return seq;

			if (n == 1)
				return _except_last_1(e);

			T[] ret = new T[n];
			int i;
			for (i = 0; i < n; i++)
			{
				ret[i] = e.Current;
				if (!e.MoveNext())
					return Collection<T>.None;
			}
			return _except_last(e, ret);
		}

		static IEnumerable<T> _except_last_1<T>(IEnumerator<T> e)
		{
			while (true)
			{
				T t = e.Current;
				if (!e.MoveNext())
					yield break;
				yield return t;
			}
		}

		static IEnumerable<T> _except_last<T>(IEnumerator<T> e, T[] ret)
		{
			int n = ret.Length;
			int i = 0;
			do
			{
				yield return ret[i];

				ret[i] = e.Current;

				if (++i == n)
					i = 0;
			}
			while (e.MoveNext());
		}

		/// <summary>
		/// Append one element to the end of a sequence.
		/// </summary>
		/// <param name="seq">The original sequence. Can be null.</param>
		/// <param name="element">An element to append.</param>
		public static IEnumerable<T> Append<T>(this IEnumerable<T> seq, T element)
		{
			Debug.Assert(!(seq is T[]));
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				yield return e.Current;
			yield return element;
		}

		/// <summary>
		/// Prepend one element to the beginning of a sequence.
		/// </summary>
		/// <param name="seq">The original sequence. Can be null.</param>
		/// <param name="element">An element to prepend.</param>
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> seq, T element)
		{
			//Debug.Assert(!(seq is T[]));
			var tmp = seq as T[];
			return tmp != null ? arr.Prepend(tmp, element) : _Prepend(seq, element);
		}
		static IEnumerable<T> _Prepend<T>(this IEnumerable<T> seq, T element)
		{
			yield return element;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				yield return e.Current;
		}
		public static T[] Prepend<T>(this IReadOnlyList<T> seq, T element)
		{
			int c;
			T[] tmp;
			(tmp = new T[(c = seq.Count) + 1])[0] = element;
			while (c > 0)
				tmp[c--] = seq[c];
			return tmp;
		}
		public static _ICollection<T> Prepend<T>(this IReadOnlyCollection<T> seq, T element)
		{
			if (seq == null || seq.Count == 0)
				return new UnaryCollection<T>(element);
			if (seq.Count == 1)
				return new _coll_duple<T>(element, seq);
			return new _coll_prepend<T>(element, seq);
		}
		public static _ICollection<T> Append<T>(this IReadOnlyCollection<T> seq, T element)
		{
			if (seq == null || seq.Count == 0)
				return new UnaryCollection<T>(element);
			return new _coll_append<T>(seq, element);
		}

		public static IEnumerable<T> ConcatMany<T>(this IEnumerable<T> seq, IEnumerable<IEnumerable<T>> more)
		{
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				yield return e.Current;
			var e2 = more.GetEnumerator();
			while (e2.MoveNext())
			{
				e = e2.Current.GetEnumerator();
				while (e.MoveNext())
					yield return e.Current;
			}
		}

#if false
		public static IEnumerable<T> AsSingleton<T>(this T t)
		{
			return new[] { t };
		}
#endif


		public static Double[] VectorAverage<T>(this IEnumerable<T> seq, Func<T, IReadOnlyCollection<Double>> selector)
		{
			int i, cc;
			Double[] tmp = null;

			IEnumerator<T> e = seq.GetEnumerator();

			for (cc = 0; e.MoveNext(); cc++)
			{
				var rgd = selector(e.Current);

				if (tmp == null)
					tmp = new Double[rgd.Count];
				else if (rgd.Count != tmp.Length)
					throw new Exception();

				var f = rgd.GetEnumerator();
				for (i = 0; f.MoveNext(); )
					tmp[i++] += f.Current;
			}

			if (cc == 0)
				throw new Exception();

			for (i = 0; i < tmp.Length; i++)
				tmp[i] /= cc;
			return tmp;
		}

		public static Double WeightedAverage<T>(this IEnumerable<T> seq, Func<T, double> value_selector, Func<T, double> weight_selector)
		{
			Double sum = seq.Sum(weight_selector);
			Double avg = 0;
			foreach (T t in seq)
				avg += value_selector(t) * weight_selector(t) / sum;
			return avg;
		}

		public static T Middle<T>(this IEnumerable<T> seq)
		{
			var ie1 = seq.GetEnumerator();
			if (!ie1.MoveNext())
				throw new ArgumentException();
			T current = ie1.Current;

			var ie2 = seq.GetEnumerator();
			ie2.MoveNext();

			while (ie2.MoveNext())
			{
				if (!ie2.MoveNext())
					break;
				ie1.MoveNext();
				current = ie1.Current;
			}
			return current;
		}

		/// <summary>
		/// Return the (distinct) set union of all elements of type TDst generated by a selector which projects each 
		/// source element of type TSrc from the sequence into a new sequence of zero or more elements of type
		/// TDst
		/// </summary>
		public static IEnumerable<TDst> UnionMany<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, IEnumerable<TDst>> selector, IEqualityComparer<TDst> cmp = null)
		{
			var hs = new HashSet<TDst>(cmp ?? EqualityComparer<TDst>.Default);
			foreach (TSrc t in seq)
				foreach (TDst td in selector(t))
					if (hs.Add(td))
						yield return td;
		}

#if false
		/// <summary>
		/// Return one source element from each range result mapped by the selector function
		/// </summary>
		public static IEnumerable<TSrc> Distinct<TSrc, TKey>(this IEnumerable<TSrc> seq, Func<TSrc, TKey> keySelector)
		{
			if (seq == null)
				throw new ArgumentNullException("source");

			return seq.GroupBy(keySelector).Select(g =>
				{
					var q = g.GetEnumerator();
					q.MoveNext();
					return q.Current;
				});
		}
		public static IEnumerable<TSrc> Distinct<TSrc, TKey>(this IEnumerable<TSrc> seq, Func<TSrc, TKey> keySelector, IEqualityComparer<TKey> cmp)
		{
			if (seq == null)
				throw new ArgumentNullException("source");

			return seq._GroupBy(e => keySelector(e), cmp).Select(g => ((IList<TSrc>)g)[0]);
		}
#endif

		/// <summary>
		/// Although there's no problem on .NET, the 'GroupBy' extension method is unusably slow on Mono. Below, we 
		/// implement a usable version from scratch
		/// </summary>
		public static IEnumerable<_IGrouping<TKey, TSrc>> _GroupBy<TSrc, TKey>(this IEnumerable<TSrc> ie, Func<TSrc, TKey> fn)
		{
#if __MonoCS__ || true

			TKey k;
			TSrc item;
			Grouping<TKey, TSrc> l;

			var d = new alib.Dictionary.SlotDictionary<TKey, Grouping<TKey, TSrc>>();

			var e = ie.GetEnumerator();
			while (e.MoveNext())
			{
				if (!d.TryGetValue(k = fn(item = e.Current), out l))
					d.Add(k, l = new Grouping<TKey, TSrc>(k, item));
				else
					l.Add(item);
			}
			return d.Values;
#else
			return ie.GroupBy(fn);
#endif
		}

#if false
		public static IEnumerable<IGrouping<TKey, TSrc>> _GroupBy<TSrc, TKey>(this IEnumerable<TSrc> ie, Func<TSrc, TKey> fn, IEqualityComparer<TKey> c)
		{
#if __MonoCS__ 
			Dictionary<TKey, Grouping<TKey, TSrc>> d = new Dictionary<TKey, Grouping<TKey, TSrc>>(c);
			foreach (TSrc src in ie)
			{
				TKey k = fn(src);
				Grouping<TKey, TSrc> l;
				if (!d.TryGetValue(k, out l))
					d.Add(k, l = new Grouping<TKey, TSrc>(k));
				l.Add(src);
			}
			return d.Values;
#else
			return ie.GroupBy(fn, c);
#endif
		}

		public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
			this IEnumerable<TSource> seq,
			Func<TSource, TKey> keySelector,
			Func<TKey, TKey, bool> comparer)
		{
			return seq._GroupBy(keySelector, new LambdaComparer<TKey>(comparer));
		}
#endif

		public static IEnumerable<IGrouping<TKey, TElement>> EnsureKeys<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> seq, IEnumerable<TKey> keys)
		{
			var e = keys.GetEnumerator();
			if (!e.MoveNext())
				return seq;
			var hs = new HashSet<TKey>(keys);	// even if no src grps because keys might not be distinct
			var ee = seq.GetEnumerator();
			if (!ee.MoveNext())
				return hs.Select(x => new Grouping<TKey, TElement>(x, Collection<TElement>.None));
			return _ensure_keys(ee, hs);
		}
		static IEnumerable<IGrouping<TKey, TElement>> _ensure_keys<TKey, TElement>(IEnumerator<IGrouping<TKey, TElement>> e, HashSet<TKey> hs)
		{
			do
			{
				IGrouping<TKey, TElement> g = e.Current;
				yield return g;
				hs.Remove(g.Key);
			}
			while (e.MoveNext());

			var ee = hs.GetEnumerator();
			while (ee.MoveNext())
				yield return new Grouping<TKey, TElement>(ee.Current, Collection<TElement>.None);
		}

		public static IEnumerable<T> Sort<T>(this IEnumerable<T> seq)
			where T : IComparable<T>
		{
			return seq.OrderBy(_identity<T>.func);
		}
		public static IEnumerable<T> SortDescending<T>(this IEnumerable<T> seq)
			where T : IComparable<T>
		{
			return seq.OrderByDescending(_identity<T>.func);
		}

		public static IOrderedEnumerable<T> Sort<T>(this IEnumerable<T> seq, IComparer<T> comparer)
		{
			var src = seq as T[];
			if (src != null)
			{
				var dst = new T[src.Length];
				src.CopyTo(dst, 0);
				arr.qsort(dst, comparer);
				return new _ordered_arr<T>(dst);
			}
			return seq.OrderBy(_identity<T>.func, comparer);
		}

		public static IEnumerable<T> SetOrder<T>(this IEnumerable<T> seq, params int[] ordinals)
			where T : IComparable<T>
		{
			T[] tmp = seq.Take(ordinals.Length).ToArray();
			if (tmp.Length > 1 && tmp.Length <= ordinals.Length)
			{
				Array.Sort(ordinals, tmp);
			}
			return tmp;
		}


		public static bool IsSorted<T>(this IEnumerable<T> seq, IComparer<T> cmp)
		{
			T i_prev;
			int i;

			T[] tmp;
			if ((tmp = seq as T[]) != null)
			{
				int c = tmp.Length;
				if (c == 0)
					throw new InvalidOperationException("Sequence contains no elements.");
				i_prev = tmp[0];
				for (i = 1; i < c; i++)
					if (cmp.Compare(i_prev, i_prev = tmp[i]) > 0)
						return false;
				return true;
			}

			IList<T> il;
			if ((il = seq as IList<T>) != null)
			{
				int c = il.Count;
				if (c == 0)
					throw new InvalidOperationException("Sequence contains no elements.");
				List<T> l;
				if ((l = seq as List<T>) != null)
				{
					i_prev = l[0];
					for (i = 1; i < c; i++)
						if (cmp.Compare(i_prev, i_prev = l[i]) > 0)
							return false;
				}
				else
				{
					i_prev = il[0];
					for (i = 1; i < c; i++)
						if (cmp.Compare(i_prev, i_prev = il[i]) > 0)
							return false;
				}
				return true;
			}

			IEnumerator<T> iei = seq.GetEnumerator();
			if (!iei.MoveNext())
				throw new InvalidOperationException("Sequence contains no elements.");
			i_prev = iei.Current;
			while (iei.MoveNext())
				if (cmp.Compare(i_prev, i_prev = iei.Current) > 0)
					return false;
			return true;
		}
		//public static bool IsSorted<T>(this IEnumerable<T> seq, int ix, int c, IComparer<T> cmp)
		//{
		//    IEnumerator<T> iei = seq.GetEnumerator();
		//    if (!iei.MoveNext())
		//        throw new InvalidOperationException("Sequence contains no elements.");
		//    T i_prev = iei.Current;
		//    while (iei.MoveNext())
		//        if (cmp.Compare(i_prev, i_prev = iei.Current) > 0)
		//            return false;
		//    return true;
		//}
		public static bool IsSorted(this int[] data)
		{
			int c, i, k;
			if ((c = data.Length) > 1)
			{
				k = data[0];
				for (i = 1; i < c; i++)
					if (k > (k = data[i]))
						return false;
			}
			return true;
		}
		public static bool IsSorted<T>(this IEnumerable<T> seq)
		{
			return IsSorted(seq, Comparer<T>.Default);
		}
		public static bool IsSequential(this IEnumerable<int> seq)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return true;
			int i = e.Current;
			while (e.MoveNext())
				if (i != (i = e.Current) - 1)
					return false;
			return true;
		}
		public static bool IsSequential<T>(this IEnumerable<T> seq, Func<T, int> selector)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return true;
			int i = selector(e.Current);
			while (e.MoveNext())
				if (i != (i = selector(e.Current)) - 1)
					return false;
			return true;
		}
		public static bool IsSorted<T>(this IEnumerable<T> seq, Comparison<T> cmp)
		{
			IEnumerator<T> iei = seq.GetEnumerator();
			if (!iei.MoveNext())
				throw new InvalidOperationException("Sequence contains no elements.");
			T i_prev = iei.Current;
			while (iei.MoveNext())
				if (cmp(i_prev, i_prev = iei.Current) > 0)
					return false;
			return true;
		}
		public static bool IsSorted<T, TDst>(this IEnumerable<T> seq, Func<T, TDst> selector)
		{
			IEnumerator<T> iei = seq.GetEnumerator();
			if (!iei.MoveNext())
				throw new InvalidOperationException("Sequence contains no elements.");
			IComparer<TDst> cmp = Comparer<TDst>.Default;
			TDst i_prev = selector(iei.Current);
			while (iei.MoveNext())
				if (cmp.Compare(i_prev, i_prev = selector(iei.Current)) > 0)
					return false;
			return true;
		}

		sealed class _ordered_arr<T> : IOrderedEnumerable<T>
		{
			public _ordered_arr(T[] arr) { this.tmp = arr; }
			readonly T[] tmp;
			public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
			{
				return descending ? tmp.OrderByDescending(keySelector, comparer) : tmp.OrderBy(keySelector, comparer);
			}
			public IEnumerator<T> GetEnumerator() { return tmp.Enumerator(); }
			IEnumerator IEnumerable.GetEnumerator() { return tmp.Enumerator(); }
		};


		public static _ICollection<T> RotateForward<T>(this IEnumerable<T> seq, int num = 1)
		{
			int c;
			if ((c = seq._Count()) == 0)
				throw new InvalidOperationException();
			if ((num = num % c) == 0)
				return seq as _ICollection<T> ?? new _coll_defer<T>(seq, c);
			return new _rot_fwd<T>(seq, c, num);
		}
		sealed class _rot_fwd<T> : ro_coll_base<T>, IReadWriteCollection<T>
		{
			public _rot_fwd(IEnumerable<T> src, int c, int num)
			{
				Debug.Assert(c == src._Count() && c > 0 && num > 0 && num < c);
				this.src = src;
				this.c = c;
				this.num = num;
			}
			readonly IEnumerable<T> src;
			readonly int c;
			readonly int num;

			public int Count { get { return c; } }

			public IEnumerator<T> GetEnumerator()
			{
				int i;
				var e = src.GetEnumerator();
				for (i = 0; i < num; i++)
					e.MoveNext();
				while (e.MoveNext())
					yield return e.Current;

				e = src.GetEnumerator();
				for (i = 0; i < num; i++)
				{
					e.MoveNext();
					yield return e.Current;
				}
			}
		};

		public static IEnumerable<T> RotateBackward<T>(this IEnumerable<T> seq)
		{
			int c;
			IEnumerator<T> e;
			IList<T> L = seq as IList<T>;
			if (L != null)
			{
				if ((c = L.Count - 1) > 0)
					return seq.Take(c).Prepend(L[c]);
			}
			else if ((e = seq.GetEnumerator()).MoveNext())
			{
				T t = e.Current;
				if (e.MoveNext())
				{
					c = 0;
					do
					{
						t = e.Current;
						c++;
					}
					while (e.MoveNext());
					return seq.Take(c).Prepend(t);
				}
			}
			return seq;
		}

		public static _ICollection<T> Decapitate<T>(this IReadOnlyCollection<T> coll, int i_skip)
		{
			if (i_skip <= 0)
				return coll as _ICollection<T> ?? new _coll_defer<T>(coll);
			if (i_skip >= coll.Count)
				return Collection<T>._Empty;
			return new _skip_coll<T>(coll, i_skip);
		}


#if false
		static IEnumerable<T> _stack_rev<T>(IEnumerator<T> e)
		{
			T t = e.Current;
			if (e.MoveNext())
				return _stack_rev<T>(e).Append(t);
			return new UnaryCollection<T>(t);
		}

		public static IEnumerable<T> Reverse<T>(this IEnumerable<T> seq)
		{
			var e = seq.GetEnumerator();
			return e.MoveNext() ? _stack_rev<T>(e) : System.Linq.Enumerable.Empty<T>();
		}
#endif

		/// <summary>
		/// Puts the sequence into the default order (for the element type), and then returns the distinct sequence
		/// by returning only one element each adjacency group
		/// </summary>
		public static IEnumerable<T> OrderByDistinct<T>(this IEnumerable<T> seq)
			where T : IComparable<T>
		{
			IEnumerator<T> iei = seq.OrderBy(_identity<T>.func).GetEnumerator();
			if (!iei.MoveNext())
				yield break;
			T t, t2;
			yield return t = iei.Current;
			while (iei.MoveNext())
			{
				if (!t.Equals(t2 = iei.Current))
					yield return t = t2;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public static IEnumerable<T> OrderByDistinct<T, TKey>(this IEnumerable<T> seq, Func<T, TKey> selector)
			where TKey : IComparable<TKey>
		{
			IEnumerator<T> iei = seq.OrderBy(selector).GetEnumerator();
			if (!iei.MoveNext())
				yield break;
			T t, t2;
			yield return t = iei.Current;
			while (iei.MoveNext())
			{
				if (!t.Equals(t2 = iei.Current))
					yield return t = t2;
			}
		}

		/// <summary>
		/// Exclude all instances of element <paramref name="element"/> from the sequence <paramref name="seq"/>.
		/// </summary>
		/// <param name="seq">The original sequence.</param>
		/// <param name="element">The element to exclude.</param>
		public static IEnumerable<T> Exclude<T>(this IEnumerable<T> seq, T element)
		{
			T t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (!(t = e.Current).Equals(element))
					yield return t;
		}

		public static IEnumerable<T> ExceptWhere<T>(this IEnumerable<T> seq, Func<T, bool> filter)
		{
			T t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (!filter(t = e.Current))
					yield return t;
		}

		public static IEnumerable<T> ExceptElementAt<T>(this IEnumerable<T> seq, int ix)
		{
			int i = 0;
			foreach (T t in seq)
			{
				if (i != ix)
					yield return t;
				i++;
			}
		}

		public static void AddRange<T>(this ICollection<T> seq, IEnumerable<T> items)
		{
			List<T> list;
			ISet<T> set;

			if ((set = seq as ISet<T>) != null)
			{
				set.UnionWith(items);
			}
			else if ((list = seq as List<T>) != null)
			{
				int c;
				if ((c = items.CountIfAvail()) >= 0)
					list.Capacity = seq.Count + c;
				list.AddRange(items);
			}
			else
			{
				var e = items.GetEnumerator();
				while (e.MoveNext())
					seq.Add(e.Current);
			}
		}

		public static _ICollection<T> ExceptElementAt<T>(this IReadOnlyCollection<T> seq, int ix)
		{
			return new _coll_except_ix<T>(seq, seq.Count, ix);
		}
		/// <summary>
		/// Returns objects of the original sequence for which the given projection is a derived class
		/// </summary>
		public static IEnumerable<TSrc> FilterType<TSrc, TBase, TDerived>(this IEnumerable<TSrc> seq, Func<TSrc, TBase> can_convert)
			where TDerived : class, TBase
		{
			foreach (TSrc t in seq)
				if (can_convert(t) is TDerived)
					yield return t;
		}

		//public static IList<TSrc> FilterFor<TSrc, TBase, TDerived>(this IList<TSrc> arr, Func<TSrc, TBase> func)
		//    where TDerived : class, TBase
		//{
		//    int c = arr.Count;
		//    if (c == 0)
		//        return arr;
		//    int j = 0;
		//    for (int i = 0; i < c; i++)
		//        if (!(arr[i] is TDerived))
		//            j++;
		//    if (j == 0)
		//        return arr;
		//    TSrc[] dst = new TSrc[c - j];
		//    TSrc t;
		//    j = 0;
		//    for (int i = 0; i < c; i++)
		//        if ((t = (func(arr[i]) as TDerived)) != null)
		//            dst[j++] = t;
		//    return dst;
		//}


#if false
		/// <summary>
		/// Returns objects which project to type TDst
		/// </summary>
		public static IEnumerable<TDst> WhereType<TSrc, TDst>(this IEnumerable seq, Func<TSrc, Object> fn)
			where TDst : class
		{
			TDst z;
			Object t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if ((t = e.Current) is TSrc && (z = fn((TSrc)t) as TDst) != null)
					yield return z;
		}
#endif

		public static IEnumerable<T> OfExactType<T>(this IEnumerable seq)
		{
			Object o;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if ((o = e.Current).GetType() == typeof(T))
					yield return (T)o;
		}

		public static bool AllOfType<T>(this IEnumerable seq)
		{
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (!(e.Current is T))
					return false;
			return true;
		}

		public static bool AnyOfType<T>(this IEnumerable seq)
		{
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (e.Current is T)
					return true;
			return false;
		}

		public static T FirstOfType<T>(this IEnumerable seq) where T : class
		{
			T t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if ((t = e.Current as T) != null)
					return t;
			return null;
		}

		public static int CountOfType<T>(this IEnumerable seq) where T : class
		{
			int c = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (e.Current is T)
					c++;
			return c;
		}

		/// <summary>
		/// Returns the index of the first element in a sequence that is compatible with type T
		/// </summary>
		public static int IndexOfType<T>(this IEnumerable seq) where T : class
		{
			var e = seq.GetEnumerator();
			for (int i = 0; e.MoveNext(); i++)
				if (e.Current is T)
					return i;
			return -1;
		}

#if false
		/// <summary>
		/// Group elements of the sequence according to the key generated by keySelector, where each group contains a distinct
		/// set of elements projected by elementSelector.
		/// </summary>
		public static IEnumerable<DistinctGrouping<TKey, TElement>> GroupIntoSets<TSrc, TKey, TElement>(
			this IEnumerable<TSrc> seq,
			Func<TSrc, TKey> keySelector,
			Func<TSrc, TElement> elementSelector)
			where TKey : IEquatable<TKey>
			where TElement : IEquatable<TElement>
		{
			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				return DistinctGrouping<TKey, TElement>.None;

			var d = new Dictionary<TKey, DistinctGrouping<TKey, TElement>>();
			do
			{
				TSrc t = ie.Current;
				TKey k = keySelector(t);
				DistinctGrouping<TKey, TElement> l;
				if (!d.TryGetValue(k, out l))
					d.Add(k, l = new DistinctGrouping<TKey, TElement>(k));
				l.Add(elementSelector(t));
			}
			while (ie.MoveNext());
			return d.Values;
		}
#endif

		public static IEnumerable<TDst> SelectDistinct<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, TDst> selector)
		{
			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				yield break;
			TDst d = selector(ie.Current);
			yield return d;
			if (!ie.MoveNext())
				yield break;
			HashSet<TDst> hs = new HashSet<TDst> { d };
			do
				if (hs.Add(d = selector(ie.Current)))
					yield return d;
			while (ie.MoveNext());
		}

		public static IEnumerable<TDst> SelectManyNotNull<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, IEnumerable<TDst>> selector)
			where TSrc : class
		{
			TSrc tsrc;
			IEnumerator<TDst> ee;
			IEnumerable<TDst> en;

			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if ((tsrc = e.Current) != null && (en = selector(tsrc)) != null)
				{
					ee = en.GetEnumerator();
					while (ee.MoveNext())
						yield return ee.Current;
				}
			}
		}

		public static IReadOnlyCollection<TDst> SelectManyDistinct<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, IEnumerable<TDst>> selector, IEqualityComparer<TDst> cmp = null)
		{
			IEnumerator<TSrc> eSrc = seq.GetEnumerator();
			IEnumerator<TDst> eDst;

			/// avoid creating a hashset if there are no elements
			do
				if (!eSrc.MoveNext())
					return Collection<TDst>.None;
			while (!(eDst = selector(eSrc.Current).GetEnumerator()).MoveNext());

			/// found an element
			TDst e1, e0 = eDst.Current;

			if (cmp == null)
				cmp = EqualityComparer<TDst>.Default;

			/// avoid creating a hashset if the first element is the only one, or if all are identical
			while (true)
			{
				while (eDst.MoveNext())
					if (!cmp.Equals(e1 = eDst.Current, e0))
						goto full;
				if (!eSrc.MoveNext())
					return new[] { e0 };
				eDst = selector(eSrc.Current).GetEnumerator();
			}

		full:
			/// found 2 distinct elements; create hashset
			var hs = new ListHashSet<TDst>(cmp) { e0, e1 };
			while (true)
			{
				while (eDst.MoveNext())
					hs.Add(eDst.Current);
				if (!eSrc.MoveNext())
					return hs;
				eDst = selector(eSrc.Current).GetEnumerator();
			}
		}

		public static int CountManyDistinct<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, IEnumerable<TDst>> selector)
		{
			IEnumerator<TSrc> eSrc = seq.GetEnumerator();
			IEnumerator<TDst> eDst;

			/// avoid creating a hashset if there are no elements
			do
				if (!eSrc.MoveNext())
					return 0;
			while (!(eDst = selector(eSrc.Current).GetEnumerator()).MoveNext());

			/// found an element
			TDst e1, e0 = eDst.Current;

			/// avoid creating a hashset if the first element is the only one, or if all are identical
			while (true)
			{
				while (eDst.MoveNext())
					if (!(e1 = eDst.Current).Equals(e0))
						goto full;
				if (!eSrc.MoveNext())
					return 1;
				eDst = selector(eSrc.Current).GetEnumerator();
			}

		full:
			/// found 2 distinct elements; create hashset
			var hs = new ListHashSet<TDst> { e0, e1 };
			while (true)
			{
				while (eDst.MoveNext())
					hs.Add(eDst.Current);
				if (!eSrc.MoveNext())
					return hs.Count;
				eDst = selector(eSrc.Current).GetEnumerator();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		/// <summary> won't get Collection(T) </summary>
		public static int CountIfAvail(this IEnumerable _this)
		{
			if (_this != null)
			{
				IReadOnlyCollection<Object> a3;
				if ((a3 = _this as IReadOnlyCollection<Object>) != null)
					return a3.Count;

				ICollection a2;
				if ((a2 = _this as ICollection) != null)
					return a2.Count;

				Array a1;
				if ((a1 = _this as Array) != null)
					return a1.Length;
#if false
				System.Reflection.MethodInfo mi;
				if ((mi = _this.GetType().GetMethod("get_Count", (System.Reflection.BindingFlags)0x14)) != null)
					return (int)mi.Invoke(_this, null);
#endif
			}
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int _Count<T>(this IEnumerable<T> seq)
		{
			int c;
			if (seq == null)
				c = 0;
			else if ((c = CountIfAvail(seq)) < 0)
			{
				c = 0;
				var e = seq.GetEnumerator();
				while (e.MoveNext())
					c++;
			}
			return c;
		}

		public static int _Count<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
		{
			int c = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (predicate(e.Current))
					c++;
			return c;
		}

		public static int _Count<T>(this IEnumerable<T> seq, Func<T, int, bool> predicate)
		{
			int i = 0, c = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (predicate(e.Current, i++))
					c++;
			return c;
		}

		public static int CountWhile<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
		{
			int c = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext() && predicate(e.Current))
				c++;
			return c;
		}

		public static int CountWhile<T>(this IEnumerable<T> seq, Func<T, int, bool> predicate)
		{
			int i = 0, c = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext() && predicate(e.Current, i++))
				c++;
			return c;
		}

		public static int CountDistinct<TSrc, TDst>(this IEnumerable<TSrc> seq, Func<TSrc, TDst> selector, IEqualityComparer<TDst> cmp = null)
		{
			if (cmp == null)
				cmp = EqualityComparer<TDst>.Default;
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return 0;
			TSrc t = e.Current;
			if (!e.MoveNext())
				return 1;
			TDst d0 = selector(t), d1;
			while (cmp.Equals(d0, d1 = selector(e.Current)))
				if (!e.MoveNext())
					return 1;
			var hs = new HashSet<TDst>(cmp) { d0, d1 };
			while (e.MoveNext())
				hs.Add(selector(e.Current));
			return hs.Count;
		}
		public static int CountDistinct<T>(this IEnumerable<T> seq)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return 0;
			T t = e.Current;
			if (!e.MoveNext())
				return 1;
			var hs = new HashSet<T>() { t };
			do
				hs.Add(e.Current);
			while (e.MoveNext());
			return hs.Count;
		}
		public static int CountDistinctValues<K, V>(this IDictionary<K, V> seq)
		{
			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				return 0;
			HashSet<V> hs = new HashSet<V>();
			do
				hs.Add(ie.Current.Value);
			while (ie.MoveNext());
			return hs.Count;
		}

		public static bool Same<T>(this IEnumerable<T> seq)
		{
			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				throw new Exception("Sequence contains no elements.");
			T t;
			IEquatable<T> ieq;
			if (Object.Equals(t = ie.Current, default(T)))
			{
				while (ie.MoveNext())
					if (!Object.Equals(ie.Current, default(T)))
						return false;
			}
			else if ((ieq = t as IEquatable<T>) != null)
			{
				while (ie.MoveNext())
					if (!ieq.Equals(ie.Current))
						return false;
			}
			else
			{
				while (ie.MoveNext())
					if (!t.Equals(ie.Current))
						return false;
			}
			return true;
		}

		public static bool Same<TSrc, TSel>(this IEnumerable<TSrc> seq, Func<TSrc, TSel> fn)
		{
			var ie = seq.GetEnumerator();
			if (!ie.MoveNext())
				throw new Exception("Sequence contains no elements.");
			TSel t;
			IEquatable<TSel> ieq;
			if (Object.Equals(t = fn(ie.Current), default(TSel)))
			{
				while (ie.MoveNext())
					if (!Object.Equals(fn(ie.Current), default(TSel)))
						return false;
			}
			else if ((ieq = t as IEquatable<TSel>) != null)
			{
				while (ie.MoveNext())
					if (!ieq.Equals(fn(ie.Current)))
						return false;
			}
			else
			{
				while (ie.MoveNext())
					if (!t.Equals(fn(ie.Current)))
						return false;
			}
			return true;
		}

		public static int HashAll<T>(this IEnumerable<T> seq)
		{
			var e = seq.GetEnumerator();
			int h = 0;
			while (e.MoveNext())
				h ^= e.Current.GetHashCode();
			return h;
		}

		public static bool IsDistinct<T>(this IEnumerable<T> seq, IEqualityComparer<T> ceq = null)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return true;
			T first = e.Current, second;
			if (!e.MoveNext())
				return true;
			if (ceq == null)
				ceq = EqualityComparer<T>.Default;
			if (ceq.Equals(first, second = e.Current))
				return false;
			if (!e.MoveNext())
				return true;
			var hs = new HashSet<T>(ceq);
			hs.Add(first);
			hs.Add(second);
			do
				if (!hs.Add(e.Current))
					return false;
			while (e.MoveNext());
			return true;
		}
		public static bool IsDistinct<T, X>(this IEnumerable<T> seq, Func<T, X> fn, IEqualityComparer<X> ceq = null)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return true;
			T tfirst = e.Current;
			if (!e.MoveNext())
				return true;
			if (ceq == null)
				ceq = EqualityComparer<X>.Default;
			X first, second;
			if (ceq.Equals(first = fn(tfirst), second = fn(e.Current)))
				return false;
			if (!e.MoveNext())
				return true;
			var hs = new HashSet<X>(ceq);
			hs.Add(first);
			hs.Add(second);
			do
				if (!hs.Add(fn(e.Current)))
					return false;
			while (e.MoveNext());
			return true;
		}

		public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> seq)
		{
			return Duplicates(seq, EqualityComparer<T>.Default);
		}

		public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> seq, IEqualityComparer<T> cmp)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				yield break;
			T t = e.Current;
			if (!e.MoveNext())
				yield break;
			var hs = new HashSet<T>(cmp);
			hs.Add(t);
			do
				if (!hs.Add(t = e.Current))
					yield return t;
			while (e.MoveNext());
		}

		/// <summary>
		/// Return a sequence where only the elements from the original sequence which match the predicate are transformed.
		/// </summary>
		/// <param name="seq">The original sequence.</param>
		/// <param name="filter">A predicate for matching elements from the original sequence.</param>
		/// <param name="selector">A transformation which will be applied to matching elements.</param>
		public static IEnumerable<TResult> WhereSelect<TSrc, TResult>(this IEnumerable<TSrc> seq, Predicate<TSrc> filter, Func<TSrc, TResult> selector)
		{
			TSrc t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (filter(t = e.Current))
					yield return selector(t);
		}

		/// <summary>
		/// Returns true if the sequence is empty, that is, if it has no elements. Also returns null if <paramref name="seq"/> is <i>null</i>.
		/// </summary>
		/// <param name="seq">The sequence to examine, or null.</param>
		/// <returns>True if the sequence is empty or null.</returns>
		public static bool None<T>(this IEnumerable<T> seq)
		{
			if (seq == null)
				return true;
			int c = CountIfAvail(seq);
			if (c != -1)
				return c == 0;
			return !seq.GetEnumerator().MoveNext();
		}

		/// <summary>
		/// Returns true if the sequence is empty, that is, if it has no elements. Also returns null if <paramref name="seq"/> is <i>null</i>.
		/// </summary>
		/// <param name="seq">The sequence to examine, or null.</param>
		/// <param name="predicate">A filter function which identifies elements to ignore when considering if the list is empty</param>
		/// <returns>True if the sequence is empty or null.</returns>
		public static bool None<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
		{
			return seq == null ? true : !seq.Any(predicate);
		}

		public static bool All<T>(this IEnumerable<T> seq, Func<T, int, bool> pred)
		{
			int i = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (!pred(e.Current, i++))
					return false;
			return true;
		}

		public static bool Any<T>(this IEnumerable<T> seq, Func<T, int, bool> predicate)
		{
			int i = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (predicate(e.Current, i++))
					return true;
			return false;
		}
#if false
		sealed class xx : IReadOnlyCollection<int>
		{
			public xx(params int[] rg) { this.rg = rg; }
			public int Count { get { return rg.Length; } }
			readonly int[] rg;
			public IEnumerator<int> GetEnumerator() { return rg.Enumerator(); }
			IEnumerator IEnumerable.GetEnumerator() { return rg.Enumerator(); }
		};
		static void _contains_any_unit_test()
		{
			bool bb;

			bb = "abcde".ContainsAny("xyz".ToCharArray());
			bb = "abcde".ContainsAny("".ToCharArray());
			bb = "abcde".ContainsAny("a".ToCharArray());
			bb = "abcde".ContainsAny("b".ToCharArray());
			bb = "abcde".ContainsAny("c".ToCharArray());
			bb = "abcde".ContainsAny("d".ToCharArray());
			bb = "abcde".ContainsAny("e".ToCharArray());

			bb = "abcde".ContainsAny("ayz".ToCharArray());
			bb = "abcde".ContainsAny("xaz".ToCharArray());
			bb = "abcde".ContainsAny("xya".ToCharArray());

			bb = "abcde".ContainsAny("eyz".ToCharArray());
			bb = "abcde".ContainsAny("xez".ToCharArray());
			bb = "abcde".ContainsAny("xye".ToCharArray());

			bb = "".ContainsAny("".ToCharArray());
			bb = "".ContainsAny("a".ToCharArray());
			bb = "".ContainsAny("xyz".ToCharArray());

			bb = new[] { 1, 2, 3, 4, 5 }.ContainsAny(new xx(7, 8, 9, 1, 10, 11, 12));
			bb = new[] { 1, 2, 3, 4, 5 }.ContainsAny(new xx(1, 2, 3, 4, 5, 6, 7));
			bb = new[] { 1, 2, 3, 4, 5 }.ContainsAny(new xx());
			bb = new[] { 1, 2, 3, 4, 5 }.ContainsAny(new xx(0));
			bb = new[] { 1, 2, 3, 4, 5 }.ContainsAny(new xx(99));
			bb = new[] { 1, 2, 3, 4, 5 }.ContainsAny(new xx(2, 3, 4));
		}
#endif
		public static bool ContainsAny<T>(this IEnumerable<T> seq, IReadOnlyCollection<T> other)
		{
			int i;
			IEnumerator<T> e;
			IReadOnlyList<T> rg;
			int c;

			if ((c = other.Count) > 0 && (e = seq.GetEnumerator()).MoveNext())
			{
				if ((rg = other as IReadOnlyList<T>) == null)
				{
					var _tmp = new T[c];
					var ee = other.GetEnumerator();
					for (i = 0; i < _tmp.Length; i++)
					{
#if DEBUG
						if (!ee.MoveNext())
							throw new Exception();
#else
						ee.MoveNext();
#endif
						if (Object.Equals(e.Current, _tmp[i] = ee.Current))
							return true;
					}
					if (!e.MoveNext())
						goto exit;
					rg = _tmp;
				}

				do
					for (i = 0; i < c; i++)
						if (Object.Equals(e.Current, rg[i]))
							return true;
				while (e.MoveNext());
			}
		exit:
			return false;
		}


		/// <summary>
		/// Returns the element from a sequence that maximizes the function <paramref name="objective"/>.
		/// </summary>
		/// <typeparam name="TVal">A type which implements IComparable&lt;T&gt;</typeparam>
		/// <param name="seq">A sequence of at least one element.</param>
		/// <param name="objective">An objective function to maximize. <paramref name="objective"/> will not be called more than once per element.</param>
		/// <returns>The element in the sequence that maximizes the function. If more than one element equivalently maximize the function, the first such element is returned.</returns>
		/// <remarks>There must be at least one element in the sequence.</remarks>
		public static TArg ArgMax<TArg, TVal>(this IEnumerable<TArg> seq, Func<TArg, TVal> objective)
			where TVal : IComparable<TVal>
		{
			IEnumerator<TArg> e = seq.GetEnumerator();
			if (!e.MoveNext())
				throw new InvalidOperationException("Sequence has no elements.");

			TArg t, t_max = e.Current;
			TVal v, v_max;

			if (e.MoveNext())
			{
				v_max = objective(t_max);
				do
				{
					if ((v = objective(t = e.Current)).CompareTo(v_max) > 0)
					{
						t_max = t;
						v_max = v;
					}
				}
				while (e.MoveNext());
			}
			return t_max;
		}

		public static TArg ArgMax<TArg, TVal>(this IEnumerable<TArg> seq, Func<TArg, TVal> objective, out TVal v_max)
			where TVal : IComparable<TVal>
		{
			IEnumerator<TArg> e = seq.GetEnumerator();
			if (!e.MoveNext())
				throw new InvalidOperationException("Sequence has no elements.");

			TArg t_max;
			v_max = objective(t_max = e.Current);

			while (e.MoveNext())
			{
				TArg t;
				TVal v;
				if ((v = objective(t = e.Current)).CompareTo(v_max) > 0)
				{
					t_max = t;
					v_max = v;
				}
			}
			return t_max;
		}

		/// <summary>
		/// Returns the element from a sequence that minimizes the function <paramref name="objective"/>.
		/// </summary>
		/// <typeparam name="TVal">A type which implements IComparable&lt;T&gt;</typeparam>
		/// <param name="seq">A sequence of at least one element.</param>
		/// <param name="objective">An objective function to minimize. <paramref name="objective"/> will not be called more than once per element.</param>
		/// <returns>The element in the sequence that maximizes the function. If more than one element equivalently minimize the function, the first such element is returned.</returns>
		/// <remarks>There must be at least one element in the sequence.</remarks>
		public static TArg ArgMin<TArg, TVal>(this IEnumerable<TArg> seq, Func<TArg, TVal> objective)
			where TVal : IComparable<TVal>
		{
			TVal _;
			return ArgMin(seq, objective, out _);
		}

		public static TArg ArgMin<TArg, TVal>(this IEnumerable<TArg> seq, Func<TArg, TVal> objective, out TVal v_min)
			where TVal : IComparable<TVal>
		{
			IEnumerator<TArg> e = seq.GetEnumerator();
			if (!e.MoveNext())
				throw new InvalidOperationException("Sequence has no elements.");

			TArg a_min;
			v_min = objective(a_min = e.Current);

			while (e.MoveNext())
			{
				TArg t;
				TVal v;
				if ((v = objective(t = e.Current)).CompareTo(v_min) < 0)
				{
					a_min = t;
					v_min = v;
				}
			}
			return a_min;
		}

		public static TSrc ArgMinOrDefault<TSrc, TArg>(this IEnumerable<TSrc> seq, Func<TSrc, TArg> objective) where TArg : IComparable<TArg>
		{
			IEnumerator<TSrc> e = seq.GetEnumerator();
			if (!e.MoveNext())
				return default(TSrc);

			TSrc t = e.Current;
			if (e.MoveNext())
			{
				TArg v, min_val = objective(t);
				do
				{
					TSrc t_try;
					if ((v = objective(t_try = e.Current)).CompareTo(min_val) < 0)
					{
						t = t_try;
						min_val = v;
					}
				}
				while (e.MoveNext());
			}
			return t;
		}

		public static IEnumerable<TArg> ArgMins<TArg, TVal>(this IEnumerable<TArg> seq, Func<TArg, TVal> objective)
			where TVal : IComparable<TVal>
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				throw new InvalidOperationException("Sequence has no elements.");

			TArg t;
			TVal v_min = objective(t = e.Current);
			var rg_min = new List<TArg> { t };
			int d;

			while (e.MoveNext())
			{
				TVal v;
				if ((d = (v = objective(t = e.Current)).CompareTo(v_min)) < 0)
				{
					rg_min.Clear();
					v_min = v;
				}
				if (d <= 0)
					rg_min.Add(t);
			}
			return rg_min;
		}

		public static T FirstAnyOrDefault<T>(this IEnumerable<T> seq, Func<T, bool> predicate)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return default(T);
			T t;
			while (!predicate(t = e.Current) && e.MoveNext())
			{ }
			return t;
		}

		public static int IndexOf<TSrc>(this IEnumerable<TSrc> seq, TSrc element)
		{
			int i = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if (e.Current.Equals(element))
					return i;
				i++;
			}
			return -1;
		}

		/// <summary>
		/// Returns the index of the first element in a sequence that satisfies the predicate.
		/// </summary>
		public static int IndexOfFirst<TSrc>(this IEnumerable<TSrc> seq, Func<TSrc, bool> predicate)
		{
			int i = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if (predicate(e.Current))
					return i;
				i++;
			}
			return -1;
		}


		/// <summary>
		/// Returns the index of the only element in a sequence that satisfies the predicate, or -1 if there is not exactly one such element.
		/// </summary>
		public static int IndexOfOnly<TSrc>(this IEnumerable<TSrc> seq, Func<TSrc, bool> predicate)
		{
			int i = 0;
			int i_found = -1;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				if (predicate(e.Current))
				{
					if (i_found != -1)
						return -1;
					i_found = i;
				}
				i++;
			}
			return i_found;
		}

		public static T FirstNonNull<T>(this IEnumerable<T> seq) where T : class
		{
			T t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if ((t = e.Current) != null)
					return t;
			return null;
		}

		public static TResult FirstNonNull<T, TResult>(this IEnumerable<T> seq, Func<T, TResult> selector) where TResult : class
		{
			TResult t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if ((t = selector(e.Current)) != null)
					return t;
			return null;
		}

		public static bool TryMax(this IEnumerable<int> seq, out int t)
		{
			IEnumerator<int> e = seq.GetEnumerator();
			if (!e.MoveNext())
			{
				t = int.MinValue;
				return false;
			}

			t = e.Current;
			if (!e.MoveNext())
				return true;

			do
			{
				int tx = e.Current;
				if (tx > t)
					t = tx;
			}
			while (e.MoveNext());
			return true;
		}

		public static bool TryMax<TSrc>(this IEnumerable<TSrc> seq, Func<TSrc, int> objective, out int t)
		{
			IEnumerator<TSrc> e = seq.GetEnumerator();
			if (!e.MoveNext())
			{
				t = int.MinValue;
				return false;
			}
			t = objective(e.Current);
			if (e.MoveNext())
			{
				do
				{
					int tx = objective(e.Current);
					if (tx > t)
						t = tx;
				}
				while (e.MoveNext());
			}
			return true;
		}



		/// <summary>
		/// Returns the index of the element in a sequence that maximizes the function <paramref name="objective"/>.
		/// </summary>
		/// <typeparam name="TArg">A type which implements IComparable&lt;T&gt;</typeparam>
		/// <param name="seq">A sequence of zero or more elements.</param>
		/// <param name="objective">An objective function to maximize. <paramref name="objective"/> will not be called more than once per element.</param>
		/// <returns>The index of the element in the sequence that maximizes the function, or <value>-1</value> if the sequence has no elements. If more than one element 
		/// equivalently maximize the function, the index of the first such element is returned.</returns>
		public static int IndexOfMax<TSrc, TArg>(this IEnumerable<TSrc> seq, Func<TSrc, TArg> objective) where TArg : IComparable<TArg>
		{
			IEnumerator<TSrc> e = seq.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			int max_ix = 0;
			TSrc t = e.Current;
			if (!e.MoveNext())
				return max_ix;

			TArg tx, max_val = objective(t);
			int i = 1;
			do
			{
				if ((tx = objective(e.Current)).CompareTo(max_val) > 0)
				{
					max_val = tx;
					max_ix = i;
				}
				i++;
			}
			while (e.MoveNext());
			return max_ix;
		}
		public static int IndexOfMax<TSrc>(this IEnumerable<TSrc> seq) where TSrc : IComparable<TSrc>
		{
			IEnumerator<TSrc> e = seq.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			int max_ix = 0;
			TSrc max_val = e.Current;
			if (!e.MoveNext())
				return max_ix;

			int i = 1;
			do
			{
				TSrc tx = e.Current;
				if (tx.CompareTo(max_val) > 0)
				{
					max_val = tx;
					max_ix = i;
				}
				i++;
			}
			while (e.MoveNext());
			return max_ix;
		}

		/// <summary>
		/// Returns the index of the element in a sequence that minimizes the function <paramref name="objective"/>.
		/// </summary>
		/// <typeparam name="TArg">A type which implements IComparable&lt;T&gt;</typeparam>
		/// <param name="seq">A sequence of zero or more elements.</param>
		/// <param name="objective">An objective function to minimize. <paramref name="objective"/> will not be called more than once per element.</param>
		/// <returns>The index of the element in the sequence that minimize the function, or <value>-1</value> if the sequence has no elements. If more than one element 
		/// equivalently minimize the function, the index of the first such element is returned.</returns>
		public static int IndexOfMin<TSrc, TArg>(this IEnumerable<TSrc> seq, Func<TSrc, TArg> objective) where TArg : IComparable<TArg>
		{
			IEnumerator<TSrc> e = seq.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			int min_ix = 0;
			TSrc t = e.Current;
			if (!e.MoveNext())
				return min_ix;

			TArg tx, min_val = objective(t);
			int i = 1;
			do
			{
				if ((tx = objective(e.Current)).CompareTo(min_val) < 0)
				{
					min_val = tx;
					min_ix = i;
				}
				i++;
			}
			while (e.MoveNext());
			return min_ix;
		}
		public static int IndexOfMin<TSrc>(this IEnumerable<TSrc> seq) where TSrc : IComparable<TSrc>
		{
			IEnumerator<TSrc> e = seq.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			int min_ix = 0;
			TSrc min_val = e.Current;
			if (!e.MoveNext())
				return min_ix;

			int i = 1;
			do
			{
				TSrc tx = e.Current;
				if (tx.CompareTo(min_val) < 0)
				{
					min_val = tx;
					min_ix = i;
				}
				i++;
			}
			while (e.MoveNext());
			return min_ix;
		}

		public static int Sum(this IEnumerable<byte> seq)
		{
			int sum = 0;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				sum += e.Current;
			return sum;
		}

		public static Range Range(this IEnumerable<int> seq)
		{
			int _min, _max, v;
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return Enumerable.Range.Vacant;

			_min = _max = e.Current;
			while (e.MoveNext())
			{
				if ((v = e.Current) < _min)
					_min = v;
				else if (v > _max)
					_max = v;
			}
			return new Range(_min, _max);
		}
		public static Range Range<T>(this IEnumerable<T> seq, Func<T, int> selector)
		{
			var r = Enumerable.Range.Vacant;
			ExtendRange<T>(seq, ref r, selector);
			return r;
		}

		public static RangeF RangeF(this IEnumerable<Double> seq)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return Enumerable.RangeF.Vacant;

			var r = new RangeF(e.Current);
			Double d;
			while (e.MoveNext())
				if ((d = e.Current) < r.Min)
					r.Min = d;
				else if (d > r.Max)
					r.Max = d;
			return r;
		}
		public static RangeF RangeF<T>(this IEnumerable<T> seq, Func<T, Double> selector)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return Enumerable.RangeF.Vacant;

			var r = new Enumerable.RangeF(selector(e.Current));
			Double d;
			while (e.MoveNext())
				if ((d = selector(e.Current)) < r.Min)
					r.Min = d;
				else if (d > r.Max)
					r.Max = d;
			return r;
		}

		public static Range BuildRange<T>(this IEnumerable<T> seq, Func<T, Range> selector)
		{
			var r = Enumerable.Range.Vacant;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				r.UnionWith(selector(e.Current));
			return r;
		}

		public static void ExtendRange<T>(this IEnumerable<T> seq, ref Range r, Func<T, int> selector)
		{
			var e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				int v = selector(e.Current);
				if (v < r.Min)
					r.Min = v;
				if (v > r.Max)
					r.Max = v;
			}
		}

		/// <summary>
		/// Pair-up the elements of a vector of elements of type T
		/// </summary>
#if false
		public static IEnumerable<KeyValuePair<T, T>> PairOff<T>(this IEnumerable<T> seq)
		{
			IEnumerator<T> e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				T first = e.Current;
				if (e.MoveNext())
					yield return new KeyValuePair<T, T>(first, e.Current);
			}
		}
#else
		public static IEnumerable<Pairing<T>> PairOff<T>(this IEnumerable<T> seq)
		{
			IEnumerator<T> e = seq.GetEnumerator();
			while (e.MoveNext())
			{
				T first = e.Current;
				if (e.MoveNext())
					yield return new Pairing<T>(first, e.Current);
			}
		}
#endif

		public static IEnumerable<Pairing<T>> PairWith<T>(this IEnumerable<T> seq, IEnumerable<T> other)
		{
			var e1 = seq.GetEnumerator();
			var e2 = other.GetEnumerator();
			while (e1.MoveNext() & e2.MoveNext())
				yield return new Pairing<T>(e1.Current, e2.Current);
		}

		public static bool MoreThanOne<T>(this IEnumerable<T> seq)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				return false;
			return e.MoveNext();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Filter for non-default(T) values (i.e. non-null references, non-default values)
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<T> NotDefault<T>(this IEnumerable<T> seq)
		{
			T t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (!Object.Equals(t = e.Current, default(T)))
					yield return t;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Filter for non-default(T) values (i.e. non-null references, non-default values)
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<TSrc> NotDefault<TSrc, TAny>(this IEnumerable<TSrc> seq, Func<TSrc, TAny> selector)
		{
			TSrc t;
			var e = seq.GetEnumerator();
			while (e.MoveNext())
				if (!Object.Equals(selector(t = e.Current), default(TAny)))
					yield return t;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<TDst> SelectNotNull<TSrc, TDst>(this IEnumerable<TSrc> source, Func<TSrc, TDst> selector)
			where TDst : class
		{
			var ie = source.GetEnumerator();
			TDst e;
			while (ie.MoveNext())
				if ((e = selector(ie.Current)) != null)
					yield return e;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<TDst> SelectNotNull<TSrc, TDst>(this IEnumerable<TSrc> source, Func<TSrc, int, TDst> selector)
			where TDst : class
		{
			var ie = source.GetEnumerator();
			TDst e;
			int i = 0;
			while (ie.MoveNext())
				if ((e = selector(ie.Current, i++)) != null)
					yield return e;
		}
	};
}