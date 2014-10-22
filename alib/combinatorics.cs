#define perm_set_1
#define permutations_c

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using alib.Debugging;
using alib.Collections;
using alib.Collections.ReadOnly;
using alib.Enumerable;
using alib.Bits;
using alib.Math;

namespace alib.Combinatorics
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// combinatorics 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static partial class _comb_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the Cartesian product of the two sets as a set of pairings.
		/// </summary>
		public static IEnumerable<Pairing<T>> CrossProduct<T>(this IReadOnlyCollection<T> set1, IReadOnlyCollection<T> set2)
		{
			return set1.SelectMany(t1 => set2.Select(t2 => new Pairing<T>(t1, t2)));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the Cartesian product of two 0-based ranges of integers
		/// </summary>
		public static IEnumerable<Pairing<int>> CrossProduct(int count_x, int count_y)
		{
			for (int x = 0; x < count_x; x++)
				for (int y = 0; y < count_y; y++)
					yield return new Pairing<int>(x, y);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the Cartesian product across an arbitrarily extensive sequence of sets
		/// </summary>
		/// <param name="sets">The sequence of sets to cross-multiply. If any set has zero elements, the result set 
		/// will be empty</param>
		/// <returns>The (distinct) set of all result sets. Each result set has exactly one element from each set in 
		/// the input sequence. The result sets are considered ordered for the purpose of distinctness.</returns>
		static IEnumerable<_ICollection<T>> _vcp<T>(this IEnumerable<IReadOnlyCollection<T>> sets, int i = 0)
		{
			var ic = sets.ElementAtOrDefault(i);
			if (ic == null)
				return Collection<T>.UnaryNone;
			return ic.SelectMany(t => _vcp<T>(sets, i + 1).Select(x => x.Prepend(t)));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the Cartesian product across a set of sets.
		/// </summary>
		/// <param name="sets">one or more of sets to cross-multiply. If any set has zero elements, the result set 
		/// will be empty</param>
		/// <returns>The (distinct) set of all result sets. Each result set has exactly one element from each set in 
		/// the input sequence. The result sets are considered ordered for the purpose of distinctness.</returns>
#if false
		public static IEnumerable<_ICollection<T>> VariableCrossProduct<T>(params IReadOnlyCollection<T>[] sets)
#else
		public static IEnumerable<_ICollection<T>> VariableCrossProduct<T>(this IReadOnlyCollection<T>[] sets)
#endif
		{
			if (sets.Length == 0)
				throw new ArgumentException();
			return _vcp<T>(sets, 0);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns all permutations of the sequence.
		/// </summary>

#if permutations_a
		public static IEnumerable<_ICollection<T>> Permutations<T>(this IEnumerable<T> seq)
		{
			var c = seq._Count();
			if (c == 0)
				return Collection<T>.UnaryNone;
			if (c == 1)
				return new UnaryCollection<_ICollection<T>>(new _coll_defer<T>(seq, c));
			return new PermutationSet<T>(seq, c);
		}

#elif permutations_b

		static IEnumerable<_ICollection<T>> _perm_inner<T>(IEnumerable<T> seq, T t)
		{
			var e = seq.GetEnumerator();
			if (!e.MoveNext())
				yield return new UnaryCollection<T>(t);
			else
			{
				T t_next = e.Current;
				foreach (var x in _perm_inner(seq.Skip(1), t_next))
					foreach (var y in x.Rotations())
						yield return y.Prepend(t);
			}
		}

		public static IEnumerable<_ICollection<T>> Permutations<T>(this IEnumerable<T> seq)
		{
			foreach (var x in seq.Rotations())
			{
				if (x.Count <= 1)
					yield return x;
				else
					foreach (var y in _perm_inner(x.Skip(1), x.First()))
						yield return y;
			}
		}

#elif permutations_c

		public static IEnumerable<_ICollection<T>> Permutations<T>(this IEnumerable<T> seq)
		{
			foreach (var rr in seq.Rotations())
			{
				var e = rr.GetEnumerator();
				if (!e.MoveNext())
					yield return Collection<T>._Empty;
				else
					foreach (var pp in _perm_inner(e))
						yield return pp;
			}
		}

		static IEnumerable<_ICollection<T>> _perm_inner<T>(IEnumerator<T> e)
		{
			T t = e.Current;
			if (!e.MoveNext())
				yield return new UnaryCollection<T>(t);
			else
				foreach (var pp in _perm_inner(e))
					foreach (var rr in pp.Rotations())
						yield return rr.Prepend(t);
		}
#endif


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the number of unordered sets of size <paramref name="k"/> which can be formed from the 
		/// specified collection by selecting elements without replacement
		/// </summary>
		/// <param name="coll">collection to form combinations from</param>
		/// <param name="k">lower index, the number of elements that would be chosen</param>
		/// <returns>the number of possible sets of size <paramref name="k"/>.</returns>
		public static ulong BinomialCoefficient<T>(this IReadOnlyCollection<T> coll, uint k)
		{
			return alib.Math.math.BinomialCoefficient((uint)coll.Count, k);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the set of combinations which can be chosen from <paramref name="coll"/>. Each combination is a
		/// set of <paramref name="k"/> elements drawn without replacement and is taken to be unordered, meaning 
		/// that no two result sets will contain the same set of elements. The number of combinations is the same as
		/// the result of the BinomialCoefficient function.
		/// </summary>
		/// <param name="coll">collection to form combinations from.</param>
		/// <param name="k">lower index, the number of elements to choose</param>
#if false
		public static IEnumerable<_ICollection<T>> Choose<T>(this IReadOnlyCollection<T> coll, int k)
		{
			int c;
			if (k == 0)
				yield return Collection<T>._Empty;
			else if (k > (c = coll.Count))
				yield break;
			else if (c == 1 || k == c)
				yield return coll as _ICollection<T> ?? new _coll_defer<T>(coll, c);
			else if (k == 1)
				foreach (var x in coll)
					yield return new UnaryCollection<T>(x);
			else
			{
				var e = coll.GetEnumerator();
				for (int i = 0; i <= c - k; i++)
				{
					e.MoveNext();
					T t = e.Current;
					foreach (var ic in coll.Decapitate(i + 1).Choose(k - 1))
						yield return ic.Prepend(t);
				}
			}
		}
#else
		public static IEnumerable<_ICollection<T>> Choose<T>(this IReadOnlyCollection<T> coll, int k)
		{
			int c;
			if (k == 0)
				return Collection<T>.UnaryNone;
			else if (k > (c = coll.Count))
				return Collection<_ICollection<T>>.None;
			else if (c == 1 || k == c)
				return new UnaryCollection<_ICollection<T>>(coll as _ICollection<T> ?? new _coll_defer<T>(coll, c));
			else if (k == 1)
				return coll.UnaryExpand();
			else
			{
				return coll.Take(c - k + 1)
							.SelectMany((q, i) => coll.Decapitate(i + 1).Choose(k - 1).Select(r => r.Prepend(q)));
			}
		}
#endif
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the set of combinations which can be chosen, with replacement (and thus repetition in the result sets), 
		/// from <paramref name="coll"/>. 
		/// </summary>
		/// <param name="coll">collection to form combinations from.</param>
		/// <param name="k">lower index, the number of elements to choose, with replacement</param>
		public static IEnumerable<_ICollection<T>> ChooseWithRepetition<T>(this IReadOnlyCollection<T> coll, int k)
		{
			if (k == 0)
				yield return Collection<T>._Empty;
			else if (k == 1)
				foreach (var x in coll)
					yield return new UnaryCollection<T>(x);
			else
			{
				var e = coll.GetEnumerator();
				for (int i = 0; e.MoveNext(); i++)
				{
					T t = e.Current;
					foreach (var ic in coll.Decapitate(i).ChooseWithRepetition(k - 1))
						yield return ic.Prepend(t);
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns all permutations of the sets which can be formed by choosing, without replacement,
		/// <paramref name="k"/> elements from the collection.
		/// </summary>
		public static IEnumerable<_ICollection<T>> Variations<T>(this IReadOnlyCollection<T> coll, int k)
		{
			if (k == 0)
				yield return Collection<T>._Empty;
			else if (k > coll.Count)
				yield break;
			else if (k == 1)
				foreach (var x in coll)
					yield return new UnaryCollection<T>(x);
			else
			{
				var e = coll.GetEnumerator();
				for (int i = 0; e.MoveNext(); i++)
				{
					T t = e.Current;
					foreach (var ic in coll.ExceptElementAt(i).Variations(k - 1))
						yield return ic.Prepend(t);
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns all permutations of the sets which can be formed by choosing, with replacement,
		/// <paramref name="k"/> elements from the collection.
		/// </summary>
		public static IEnumerable<_ICollection<T>> VariationsWithRepetion<T>(this IReadOnlyCollection<T> coll, int k)
		{
			if (k == 0)
				yield return Collection<T>._Empty;
			else if (k == 1)
				foreach (var x in coll)
					yield return new UnaryCollection<T>(x);
			else
			{
				var e = coll.GetEnumerator();
				while (e.MoveNext())
				{
					T t = e.Current;
					foreach (var ic in coll.VariationsWithRepetion(k - 1))
						yield return ic.Prepend(t);
				}
			}
		}


		public static IEnumerable<T[]> Interleavings<T>(this T[] seq, T value)
		{
			int c = seq.Length + 1;
			for (int i = 0; i < c; )
			{
				var q = new T[c];
				System.Array.Copy(seq, 0, q, 0, i);
				q[i] = value;
				System.Array.Copy(seq, i, q, ++i, c - i);
				yield return q;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns all rotations of sequence <paramref name="seq"/>, including the sequence itself (an empty sequence thus
		/// returns one empty sequence as the only rotation). certified lazy evaluation for use with intractable inputs. each 
		/// result collection is read-only.
		/// </summary>
		/// <param name="seq">Sequence for which all forward rotations are generated</param>
		/// <returns>A set of 'n' rotated sequences, starting with <paramref name="seq"/> itself, where 'n' equals the 
		/// number of elements in <paramref name="seq"/>. The order of the returned sequences is 'forward' wrt the supplied
		/// enumerator.</returns>
		public static _ICollection<_ICollection<T>> Rotations<T>(this IEnumerable<T> seq)
		{
			int c;
			if ((c = seq._Count()) == 0)
				return Collection<T>.UnaryNone;
			var coll = seq as _ICollection<T> ?? new _coll_defer<T>(seq, c);
			if (c == 1)
				return new UnaryCollection<_ICollection<T>>(coll);
			return new _fwd_rots<T>(coll);
		}

		sealed class _fwd_rots<T> : coll_redirect_base<_ICollection<T>>, _ICollection<_ICollection<T>>
		{
			public _fwd_rots(_ICollection<T> seq) { this.seq = seq; }
			readonly _ICollection<T> seq;

			public int Count { get { return seq.Count; } }

			public IEnumerator<_ICollection<T>> GetEnumerator()
			{
				var r = seq;
				int c = seq.Count;
				for (int i = 0; i < c; i++)
				{
					yield return r;
					r = r.RotateForward();
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns all reverse rotations of sequence <paramref name="seq"/>. Lazy evaluation.
		/// </summary>
		/// <param name="seq">Sequence for which all reverse rotations are generated</param>
		/// <returns>A set of 'n' rotated sequences, starting with <paramref name="seq"/> itself (unless empty),
		/// where 'n' equals the number of elements in <paramref name="seq"/>. </returns>
		public static IEnumerable<IEnumerable<T>> BackwardRotations<T>(this IEnumerable<T> seq)
		{
			IEnumerable<T> rot_cur = seq;
			int c;
			if ((c = seq.CountIfAvail()) != -1)
				for (int i = 0; i < c; i++)
				{
					yield return rot_cur;
					rot_cur = rot_cur.RotateBackward();
				}
			else
			{
				IEnumerator<T> e = seq.GetEnumerator();
				while (e.MoveNext())
				{
					yield return rot_cur;
					rot_cur = rot_cur.RotateBackward();
				}
			}
		}

#if set_mappings
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the set of distinct mappings that can be formed between <paramref name="set1"/> and 
		/// <paramref name="set2"/>. Each mapping consists of one or more <i>pairings</i> where a pairing 
		/// consists of exactly one element from each set. The input sets are assumed not to share any elements
		/// and the elements used in pairings are consumed (no repetition)</summary>
		/// <returns>All possible mappings. Each of these mappings contains between 1 and 
		/// Min(<paramref name="count_x"/>,<paramref name="count_y"/>) pairings</returns> each.
#if false
		public static IEnumerable<IReadOnlyCollection<Pairing<T>>> SetMappings<T>(IReadOnlyCollection<T> set1, IReadOnlyCollection<T> set2, IEqualityComparer<T> cmp)
		{
			int k_max = System.Math.Min(set1.Count, set2.Count);
			for (int k = 1; k <= k_max; k++)
			{
				var arr = new Pairing<T>[k];
				foreach (var comb_L in Choose(set1, k))
				{
					foreach (var comb_R in Choose(set2, k))
					{
						var perms_R = new Permutations<T>(comb_R);
						foreach (var perm_R in perms_R)
						{
							var e1 = comb_L.GetEnumerator();
							var e2 = perm_R.GetEnumerator();
							for (int j = 0; j < k; j++)
							{
								e1.MoveNext();
								e2.MoveNext();
								if (!cmp.Equals(e1.Current, e2.Current))
									goto skip;

								arr[j] = new Pairing<T>(e1.Current, e2.Current);
							}
							yield return arr;
							arr = new Pairing<T>[k];
						skip: ;
						}
					}
				}
			}
		}
#else
		public static IEnumerable<IReadOnlyCollection<Pairing<T>>> SetMappings<T>(IReadOnlyCollection<T> set1, IReadOnlyCollection<T> set2, IEqualityComparer<T> cmp)
		{
			int k_max = System.Math.Min(set1.Count, set2.Count);
			for (int k = 1; k <= k_max; k++)
			{
				foreach (var comb_L in set1.Choose(k))
				{
					foreach (var comb_R in set2.Choose(k))
					{
						var perms_R = new PermutationSet<T>(comb_R);
						foreach (var perm_R in perms_R)
						{
							var arr = new Pairing<T>[k];
							var e1 = comb_L.GetEnumerator();
							var e2 = perm_R.GetEnumerator();
							for (int j = 0; j < k; j++)
							{
								e1.MoveNext();
								e2.MoveNext();
								arr[j] = new Pairing<T>(e1.Current, e2.Current);
							}
							yield return arr;
						}
					}
				}
			}
		}
#endif

		public static IEnumerable<IReadOnlyCollection<Pairing<T>>> SetMappings<T>(IReadOnlyCollection<T> set1, IReadOnlyCollection<T> set2)
		{
			return SetMappings(set1, set2, EqualityComparer<T>.Default);
		}

		/// <summary>
		/// Calculates the number of distinct mappings that can be formed from two sets with the specified cardinalities.
		/// A mapping consists of one or more <i>pairings</i> where a pairing consists of exactly one element from each set.
		/// Elements used in pairings are consumed (no repretition) and the two sets do not share any elements.
		/// </summary>
		/// <param name="count_x">Number of elements available for the <i>x</i>-side of the pairing</param>
		/// <param name="count_y">Number of elements available for the <i>y</i>-side of the pairing</param>
		/// <returns>the number of possible mappings. Each of these mappings contains between 1 and 
		/// Min(<paramref name="count_x"/>,<paramref name="count_y"/>) pairings</returns> each.
		public static ulong CountSetMappings(uint count_x, uint count_y)
		{
			//if (c1 == c2)
			//{
			//    if (c1 <= 1)
			//        return c1;
			//}

			ulong tot = 0;
			uint k = count_x < count_y ? count_x : count_y;
			do
				tot += alib.Math.math.BinomialCoefficient(count_x, k) *
						alib.Math.math.BinomialCoefficient(count_y, k) *
						alib.Math.math.factorials[k];
			while (k-- > 0);
			return tot;
		}
#endif
	};

#if perm_set_1
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Representing the set of all possible permutation sets of an input set.
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class PermutationSet<T> : ro_coll_base<_ICollection<T>>, _ICollection<_ICollection<T>>
	{
		public PermutationSet(IEnumerable<T> src, int n)
		{
			this.src = src;
			if (n >= alib.Math.math.factorials.Length)
				throw new OverflowException("Unwise to enumerate more than 2,432,902,008,176,640,000 permutations.");
			this.ul = alib.Math.math.factorials[n];
			if (ul == 1)
				throw new Exception();
		}
		public PermutationSet(IReadOnlyCollection<T> src)
			: this(src, src.Count)
		{
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IEnumerable<T> src;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly ulong ul;

		//[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		//public ICollection<T>[] _dbg_display { get { return this.ToArray(); } }

		public int Count
		{
			get
			{
				if (ul > int.MaxValue)
					throw new OverflowException();
				return (int)ul;
			}
		}

		public ulong LongCount { get { return ul; } }

		/// You can change the order of the result set from forward to reverse by changing .Prepend to .Append
		static IEnumerable<_ICollection<T>> _perm_inner(IEnumerator<T> e)
		{
			T t = e.Current;
			if (!e.MoveNext())
				yield return new UnaryCollection<T>(t);
			else
				foreach (var pp in _perm_inner(e))
					foreach (var rr in pp.Rotations())
						yield return rr.Prepend(t);
		}

		public IEnumerator<_ICollection<T>> GetEnumerator()
		{
			foreach (var rr in src.Rotations())
			{
				IEnumerator<T> e = rr.GetEnumerator();
				e.MoveNext();
				foreach (var pp in _perm_inner(e))
					yield return pp;
			}
		}
	};
#else

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	interface IMetaCollection<T> : IEnumerable<IReadOnlyList<T>>
	{
		ulong Count { get; }
		GenerateOption Type { get; }
		int UpperIndex { get; }
		int LowerIndex { get; }
	}

	public enum GenerateOption
	{
		WithoutRepetition,
		WithRepetition
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class permutation_base<T> : IMetaCollection<T>
	{
		protected permutation_base(IReadOnlyCollection<T> src_items, GenerateOption type, int lower_index)
		{
			this.type = type;
			this.lower_index = lower_index;
			this.count = ulong.MaxValue;

			this.values = new List<T>(src_items.Count);
			this.values.AddRange(src_items);
		}

		protected readonly List<T> values;

		readonly GenerateOption type;
		public GenerateOption Type { get { return type; } }

		public int UpperIndex { get { return values.Count; } }

		readonly int lower_index;
		public int LowerIndex { get { return lower_index; } }

		ulong count;
		public ulong Count
		{
			get
			{
				if (count == ulong.MaxValue)
					count = get_count();
				return count;
			}
		}

		protected abstract ulong get_count();

		///////////////////////////////////////////////////
		/// 
		protected abstract class _enum : IEnumerator<IReadOnlyList<T>>
		{
			protected _enum()
			{
				Reset();
			}

			protected enum Position { BeforeFirst, Valid, Finished };

			protected Position position;
			protected T[] cur_list;

			public virtual void Reset()
			{
				this.position = Position.BeforeFirst;
				this.cur_list = null;
			}

			public IReadOnlyList<T> Current
			{
				get
				{
					if (position != Position.Valid)
						throw not.valid;
					return cur_list;
				}
			}

			Object IEnumerator.Current { get { return Current; } }

			public abstract bool MoveNext();

			public void Dispose() { }
		};
		/// 
		///////////////////////////////////////////////////

		public abstract IEnumerator<IReadOnlyList<T>> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class PermutationSet<T> : permutation_base<T>
	{
		PermutationSet(IReadOnlyCollection<T> src_items, GenerateOption type)
			: base(src_items, type, src_items.Count)
		{
			lex_orders = new int[src_items.Count];
		}

		/// <summary> (with repetition) </summary>
		public PermutationSet(IReadOnlyCollection<T> src_items)
			: this(src_items, GenerateOption.WithRepetition)
		{
			for (int i = 0; i < lex_orders.Length; i++)
				lex_orders[i] = i;
		}

		/// <summary> (without repetition) </summary>
		public PermutationSet(IReadOnlyCollection<T> src_items, IComparer<T> comparer)
			: this(src_items, GenerateOption.WithoutRepetition)
		{
			if (comparer == null)
				comparer = Comparer<T>.Default;

			values.Sort(comparer);
			int j = 1;
			if (lex_orders.Length > 0)
				lex_orders[0] = j;

			for (int i = 1; i < lex_orders.Length; i++)
			{
				if (comparer.Compare(values[i - 1], values[i]) != 0)
					j++;
				lex_orders[i] = j;
			}
		}

		readonly int[] lex_orders;

		protected override ulong get_count()
		{
			int runCount = 1;
			var divisors = new List<ushort>();
			var numerators = new List<ushort>();
			for (uint i = 1; i < lex_orders.Length; i++)
			{
				numerators.AddRange(Primes.Factor(i + 1));

				if (lex_orders[i] == lex_orders[i - 1])
					runCount++;
				else
				{
					for (uint f = 2; f <= runCount; f++)
						divisors.AddRange(Primes.Factor(f));

					runCount = 1;
				}
			}
			for (uint f = 2; f <= runCount; f++)
				divisors.AddRange(Primes.Factor(f));

			return Primes.EvaluatePrimeFactors(Primes.DividePrimeFactors(numerators, divisors));
		}

		///////////////////////////////////////////////////
		/// 
		sealed class Enumerator : _enum
		{
			public Enumerator(PermutationSet<T> source)
			{
				this.source = source;
			}

			readonly PermutationSet<T> source;
			int[] lex_orders;

			bool NextPermutation()
			{
				int i = lex_orders.Length - 1;
				while (lex_orders[i - 1] >= lex_orders[i])
					if (--i == 0)
						return false;

				int j = lex_orders.Length;
				while (lex_orders[j - 1] <= lex_orders[i - 1])
					j--;

				Swap(i - 1, j - 1);
				i++;
				j = lex_orders.Length;
				while (i < j)
				{
					Swap(i - 1, j - 1);
					i++;
					j--;
				}
				return true;
			}

			void Swap(int i, int j)
			{
				T t1;
				t1 = cur_list[i];
				cur_list[i] = cur_list[j];
				cur_list[j] = t1;

				int t2;
				t2 = lex_orders[i];
				lex_orders[i] = lex_orders[j];
				lex_orders[j] = t2;
			}

			public override bool MoveNext()
			{
				if (position == Position.BeforeFirst)
				{
					lex_orders = new int[source.lex_orders.Length];
					source.lex_orders.CopyTo(lex_orders, 0);
					System.Array.Sort(lex_orders);

					cur_list = new T[source.values.Count];
					source.values.CopyTo(cur_list);

					position = Position.Valid;
				}
				else if (position == Position.Valid)
				{
					if (cur_list.Length < 2 || !NextPermutation())
						position = Position.Finished;
				}
				return position != Position.Finished;
			}
		};
		/// 
		///////////////////////////////////////////////////

		public override IEnumerator<IReadOnlyList<T>> GetEnumerator() { return new Enumerator(this); }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class CombinationSet<T> : permutation_base<T>
	{
		public CombinationSet(IReadOnlyCollection<T> src_items, int lower_index, GenerateOption type = GenerateOption.WithoutRepetition)
			: base(src_items, type, lower_index)
		{
			var map = new RefList<bool>();
			if (type == GenerateOption.WithoutRepetition)
			{
				for (int i = 0; i < values.Count; i++)
					map.Add(i < values.Count - lower_index);
			}
			else
			{
				for (int i = 0; i < src_items.Count - 1; i++)		// is this an error??
					map.Add(true);
				for (int i = 0; i < lower_index; i++)
					map.Add(false);
			}
			perms = new PermutationSet<bool>(map, Comparer<bool>.Default);
		}

		readonly PermutationSet<bool> perms;

		protected override ulong get_count() { return perms.Count; }

		///////////////////////////////////////////////////
		/// 
		sealed class Enumerator : _enum
		{
			public Enumerator(CombinationSet<T> source)
			{
				this.source = source;
				this.e_perm = source.perms.GetEnumerator();
			}

			readonly CombinationSet<T> source;
			readonly IEnumerator<IReadOnlyList<bool>> e_perm;

			public override bool MoveNext()
			{
				if (!e_perm.MoveNext())
				{
					position = Position.Finished;
					return false;
				}

				var cur_perm = e_perm.Current;
				var c = cur_perm.Count;
				var rl = new RefList<T>(c);
				int index = 0;
				for (int i = 0; i < c; i++)
				{
					if (cur_perm[i] == false)
					{
						rl.Add(source.values[index]);
						if (source.Type == GenerateOption.WithoutRepetition)
							index++;
					}
					else
						index++;
				}
				cur_list = rl.GetTrimmed();
				position = Position.Valid;
				return true;
			}

			public override void Reset() { e_perm.Reset(); }
		};
		/// 
		///////////////////////////////////////////////////

		public override IEnumerator<IReadOnlyList<T>> GetEnumerator() { return new Enumerator(this); }
	};
#endif


#if exclusion_pairs
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public interface IExclusionPair<T> : _ICollection<IReadOnlyCollection<Pairing<T>>>
	{
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class ExclusionPairBase<T> : IEnumerable
	{
		public void CopyTo(System.Array array, int index) { throw alib.not.impl; }
		public bool IsSynchronized { get { throw alib.not.impl; } }
		public object SyncRoot { get { throw alib.not.impl; } }
		public bool IsReadOnly { get { return true; } }
		public bool Contains(IReadOnlyCollection<Pairing<T>> item) { throw not.valid; }
		public void Add(IReadOnlyCollection<Pairing<T>> item) { throw not.valid; }
		public bool Remove(IReadOnlyCollection<Pairing<T>> item) { throw not.valid; }
		public void Clear() { throw not.valid; }

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IReadOnlyCollection<T>)this).GetEnumerator();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("Simple Count:{Count.ToString().PadRight(4),nq} {ToString(),nq}")]
	public sealed class ExclusionPair<T> : ExclusionPairBase<T>, IExclusionPair<T>
	{
		public ExclusionPair(T left, T righ)
		{
			this.left = left;
			this.righ = righ;
		}
		T left, righ;

		public void CopyTo(IReadOnlyCollection<Pairing<T>>[] array, int arrayIndex)
		{
			array[arrayIndex] = new UnaryCollection<Pairing<T>>(new Pairing<T>(left, righ));
		}
		public IEnumerator<IReadOnlyCollection<Pairing<T>>> GetEnumerator()
		{
			yield return new UnaryCollection<Pairing<T>>(new Pairing<T>(left, righ));
		}
		public int Count { get { return 1; } }

		public override string ToString()
		{
			return String.Format("{0}  -  {1}", left, righ);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("Left   Count:{Count.ToString().PadRight(4),nq} {ToString(),nq}")]
	public sealed class LeftExclusionPair<T> : ExclusionPairBase<T>, IExclusionPair<T>
	{
		public LeftExclusionPair(EqualitySet<T> left, T righ)
		{
			this.left = left;
			this.righ = righ;
		}
		public EqualitySet<T> left;
		T righ;

		public void CopyTo(IReadOnlyCollection<Pairing<T>>[] array, int arrayIndex)
		{
			foreach (T t in left)
				array[arrayIndex++] = new UnaryCollection<Pairing<T>>(new Pairing<T>(t, righ));
		}
		public IEnumerator<IReadOnlyCollection<Pairing<T>>> GetEnumerator()
		{
			foreach (T t in left)
				yield return new UnaryCollection<Pairing<T>>(new Pairing<T>(t, righ));
		}
		public int Count { get { return left.Count; } }

		public override string ToString()
		{
			return String.Format("{0}  -  {1}", left.StringJoin(" "), righ);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("Right  Count:{Count.ToString().PadRight(4),nq} {ToString(),nq}")]
	public sealed class RightExclusionPair<T> : ExclusionPairBase<T>, IExclusionPair<T>
	{
		public RightExclusionPair(T left, EqualitySet<T> righ)
		{
			this.left = left;
			this.righ = righ;
		}
		T left;
		public EqualitySet<T> righ;
		public void CopyTo(IReadOnlyCollection<Pairing<T>>[] array, int arrayIndex)
		{
			foreach (T t in righ)
				array[arrayIndex++] = new UnaryCollection<Pairing<T>>(new Pairing<T>(left, t));
		}
		public IEnumerator<IReadOnlyCollection<Pairing<T>>> GetEnumerator()
		{
			foreach (T t in righ)
				yield return new UnaryCollection<Pairing<T>>(new Pairing<T>(left, t));
		}
		public int Count { get { return righ.Count; } }

		public override string ToString()
		{
			return String.Format("{0}  -  {1}", left, righ.StringJoin(" "));
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("Multi  Count:{Count.ToString().PadRight(4),nq} {ToString(),nq}")]
	public sealed class MultiExclusionPair<T> : ExclusionPairBase<T>, IExclusionPair<T>
	{
		public MultiExclusionPair(EqualitySet<T> left, EqualitySet<T> right)
		{
			this.left = left;
			this.right = right;
		}
		public EqualitySet<T> left;
		public EqualitySet<T> right;

		public IEnumerator<IReadOnlyCollection<Pairing<T>>> GetEnumerator()
		{
			return _comb_ext.SetMappings<T>(left, right).GetEnumerator();
		}

		public int Count
		{
			get
			{
				ulong ul = alib.Combinatorics._comb_ext.CountSetMappings((uint)left.Count, (uint)right.Count);
				if (ul > int.MaxValue)
					throw new OverflowException();
				return (int)ul;
			}
		}
		public void CopyTo(IReadOnlyCollection<Pairing<T>>[] array, int arrayIndex)
		{
			foreach (var icpr in this)
				array[arrayIndex++] = icpr;
		}

		public override string ToString()
		{
			return String.Format("{0}  -  {1}", left.StringJoin(" "), right.StringJoin(" "));
		}
	};

	public static partial class _comb_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IReadOnlyCollection<IExclusionPair<T>> GroupExclusionPairs<T>(
						IReadOnlyList<EqualitySet<T>> left_grp,
						IReadOnlyList<EqualitySet<T>> righ_grp,
						IEqualityComparer<T> ceq)
		{
			List<IExclusionPair<T>> lep = new List<IExclusionPair<T>>();
			foreach (var left in left_grp)
				foreach (var righ in righ_grp)
					if (ceq.Equals(left.Key, righ.Key))
					{
						if (left.Count == 1)
						{
							if (righ.Count == 1)
								lep.Add(new ExclusionPair<T>(left.Key, righ.Key));
							else
								lep.Add(new RightExclusionPair<T>(left.Key, righ));
						}
						else if (righ.Count == 1)
							lep.Add(new LeftExclusionPair<T>(left, righ.Key));
						else
							lep.Add(new MultiExclusionPair<T>(left, righ));
					}
			return lep;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Returns the set of all possible distinct mappings between the elements of <paramref name="s1"/> and
		/// <paramref name="s2"/>.
		/// </summary>
		public static IEnumerable<IReadOnlyCollection<Pairing<T>>> CrossMappings<T>(
						this IReadOnlyList<T> s1,
						IReadOnlyList<T> s2,
						IEqualityComparer<T> comparer)
		{
			var eq_set1 = EqualitySets(s1, comparer);
			var eq_set2 = EqualitySets(s2, comparer);

			var excl_grps = GroupExclusionPairs(eq_set1, eq_set2, comparer);

#if false
			foreach (var x in SetMappings(eq_set1, eq_set2))
			{
				yield return x;
			}
#elif false
			for (int k = excl_grps.Count; k > 0; k--)
			{
				foreach (var comb in excl_grps.Choose(excl_grps.Count))
				{
					foreach (var perm in new Permutations<IExclusionPair<T>>(comb, GenerateOption.WithRepetition))
					{
						foreach (var sdf in comb)
						{
							foreach (var qwer in sdf)
								yield return qwer;
						}
					}
				}
			}
#else

			var ret1 = excl_grps.VariableCrossProduct();

			var ret2 = ret1.Select(icp =>
				{
					IReadOnlyCollection<Pairing<T>> ret;
					var e = icp.GetEnumerator();
					if (e.MoveNext())
						ret = new _coll_defer<Pairing<T>>(icp.SelectMany());
					else
						ret = Collection<Pairing<T>>.None;
					return ret;

					//return icp.SelectMany(q => q).ToCollection();
				});

			//var _dbg = ret1.Select(icp => icp.StringJoin(" ▀ ")).ToArray();
			//var _dbg = ret.Select(icp => icp.Select(icx => icx.StringJoin(", ")).StringJoin(" ▀ ")).ToArray();

			return ret2;
#endif
		}
	};
#endif


#if equality_sets
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public interface IEqualitySet<out T> : _IGrouping<T, T>
	{
	};

	public interface IEqualityGroup<out T> : _IGrouping<IEqualitySet<T>, T>
	{
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class EqualitySet<T> : RefList<T>, IEqualitySet<T>
	{
		public EqualitySet(T a) : base(5) { base.Add(a); }
		public T Key { get { return base[0]; } }
		public override String ToString()
		{
			return String.Format("{0}", this.StringJoin(", "));
		}
	};

#if false
	public interface IIndexed
	{
		int Index { get; }
	};

	public interface IIndexedCollection<T> : _ICollection<T>
		where T : IIndexed
	{
		int MaxIndex { get; }
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class IndexedEqualitySet<T> : EqualitySet<T>, IIndexedCollection<T>
		where T : IIndexed
	{
		public IndexedEqualitySet(T a)
			: base(a)
		{
			this.max_idx = a.Index;
		}

		public void Add(T item)
		{
			base.Add(item);
			if (item.Index > max_idx)
				max_idx = item.Index;
		}

		public int max_idx;
		public override String ToString()
		{
			return String.Format("max_idx:{0} {1}", max_idx, this.StringJoin(", "));
		}
		public int MaxIndex { get { return max_idx; } }
	};
#endif

	public static partial class _comb_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Groups the elements in <paramref name="arr"/> into sets by inter-equality. The result will consist of one or 
		/// more equality sets unless <paramref name="arr"/> is empty, in which case the result will be empty.
		/// </summary>
		/// <param name="arr">Sequence of elements to group according to inter-equality</param>
		/// <param name="ceq">Comparison to use for determining the grouping of elements</param>
		/// <returns></returns>
		static unsafe IReadOnlyList<EqualitySet<T>> EqualitySets<T>(this IReadOnlyList<T> arr, IEqualityComparer<T> ceq)
		{
			int c = arr.Count;
			if (c == 0)
				return Collection<EqualitySet<T>>.None;
			if (c == 1)
				return new[] { new EqualitySet<T>(arr[0]) };
			int i = (int)((uint)(c - 1) >> 6) + 1;
			ulong* pu = stackalloc ulong[i];
			alib.Bits.BitHelper bh = new alib.Bits.BitHelper(pu, i);

			List<EqualitySet<T>> leg = new List<EqualitySet<T>>(c);
			for (i = 0; i < c; i++)
			{
				if (!bh.IsMarked(i))
				{
					T t1 = arr[i], t2;
					EqualitySet<T> eg = null;
					for (int j = i + 1; j < c; j++)
					{
						if (!bh.IsMarked(j) && ceq.Equals(t1, t2 = arr[j]))
						{
							bh.SetBit(j);
							if (eg == null)
								leg.Add(eg = new EqualitySet<T>(t1));
							eg.Add(t2);
						}
					}
					if (eg == null)
						leg.Add(new EqualitySet<T>(t1));
				}
			}
			if (leg != null)
				return leg;
			return Collection<EqualitySet<T>>.Empty;
		}
	}
#endif
}
