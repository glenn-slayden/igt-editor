using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;
using alib.Combinatorics;

namespace alib.dg
{
	using Array = System.Array;
	using String = System.String;
	using Enumerable = System.Linq.Enumerable;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public unsafe class logical_layout : IReadOnlyList<int[]>, IEquatable<logical_layout>
	{
		static int[][] make_identity_layout<T>(IReadOnlyList<T> levels)
			where T : IReadOnlyCollection<Object>
		{
			var a = new int[levels.Count][];
			for (int i = 0; i < a.Length; i++)
				a[i] = IntArray.MakeIdentity(levels[i].Count);
			return a;
		}
#if false
		static int[][] make_vref_layout(level_infos levels)
		{
			var a = new int[levels.Count][];
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = make_identity_array(levels[i].Count);
				var xx = levels[i].arr.Select(x => (int)x.vr).ToArray();
				Array.Sort(xx, a[i]);
			}
			return a;
		}
#endif

		logical_layout(level_infos levels, int[][] a)
		{
			this.levels = levels;
			this.L2P = a;
			this.P2L = new int[a.Length][];
			_invalidate_crossings();
		}

		public logical_layout(level_infos levels)
			: this(levels, make_identity_layout(levels))
		{
			//initial_approximation(out this.L2P);
		}

		public logical_layout(logical_layout to_copy)
		{
			this.levels = to_copy.levels;
			this.c_crossings = to_copy.c_crossings;
			this.c_lev_crossings = to_copy.c_lev_crossings;

			this.L2P = to_copy.L2P.ToArray();
			this.P2L = to_copy.P2L.ToArray();
		}

		[DebuggerDisplay("")]
		public readonly level_infos levels;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected int c_crossings;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int[] c_lev_crossings;
		void _invalidate_crossings()
		{
			if (_hc != 0)
				throw new Exception("Cannot modify this object after the hash code has been accessed.");
			c_crossings = -1;
			c_lev_crossings = new int[L2P.Length - 1];
		}

		/// <summary> interior arrays are intended to be readonly and must never be 
		/// altered (e.g. sorted); they must always be entirely replaced </summary>
		readonly int[][] L2P, P2L;

		public int[] this[int i_level] { get { return L2P[i_level]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count { [DebuggerStepThrough] get { return L2P.Length; } }

		public IEnumerator<int[]> GetEnumerator() { return L2P.Enumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/////////////////////////////////////////////////////////////
		/// 
		[DebuggerDisplay("{ToString(),nq}")]
		struct logical_state
		{
			public logical_state(logical_layout hyp)
			{
				this.c = hyp.total_crossings();
				this.l2p = hyp.L2P.ToArray();
			}
			public int c;
			public int[][] l2p;

			[DebuggerStepThrough, DebuggerHidden]
			public int[][] GetState(logical_layout hyp)
			{
				for (int i = 0; i < l2p.Length; i++)
					hyp.P2L[i] = null;
				hyp._invalidate_crossings();
				return l2p;
			}
			public override String ToString()
			{
				return String.Format("crossings:{0} {{ {1} }}", c, l2p.Select(q => "{ " + q.StringJoin(" ") + " }").StringJoin(" "));
			}
		};

		void initial_approximation(out int[][] L2P)
		{
			var states = new logical_state[7];
			int c_states = 0;

			states[c_states++] = new logical_state(this);

			if (median_sweep_upwards())
			{
				states[c_states++] = new logical_state(this);

				if (median_sweep_downwards())
				{
					states[c_states++] = new logical_state(this);

					if (median_sweep_upwards())
						states[c_states++] = new logical_state(this);
				}
			}

			L2P = states[0].GetState(this);

			if (median_sweep_downwards())
			{
				states[c_states++] = new logical_state(this);

				if (median_sweep_upwards())
				{
					states[c_states++] = new logical_state(this);

					if (median_sweep_downwards())
						states[c_states++] = new logical_state(this);
				}
			}

			L2P = states.Take(c_states).ArgMin(z => z.c).GetState(this);
		}
		/// 
		/////////////////////////////////////////////////////////////


		/////////////////////////////////////////////////////////////
		/// 
		public IEnumerable<Vref> VrefsLogical(int i_level)
		{
			var l2p = L2P[i_level];
			var lev = levels[i_level];
			for (int ix_log = 0; ix_log < l2p.Length; ix_log++)
				yield return lev[l2p[ix_log]];
		}
		public int[] phys2log(int i_level)
		{
			int i, c;
			int[] _tmp, l2p;
			if ((_tmp = P2L[i_level]) != null)
				return _tmp;

			_tmp = new int[c = (l2p = L2P[i_level]).Length];
			for (i = 0; i < c; i++)
				_tmp[l2p[i]] = i;
			return P2L[i_level] = _tmp;
		}

		public bool SetLogicalOrder(int i_level, int[] rg_phys)
		{
			//Debug.Assert(rg_phys.Length == L2P[i_level].Length);
			if (!rg_phys.Sort().SequenceEqual(Enumerable.Range(0, levels[i_level].Count)))
				throw new Exception();

			if (levels[i_level].Equals(L2P[i_level], rg_phys))
				return false;
			//if (L2P[i_level].SequenceEqual(rg_phys))
			//	return false;

			P2L[i_level] = null;
			L2P[i_level] = rg_phys;
			_invalidate_crossings();
			return true;
		}
		public int ix_logical(LogicalPosition vrix)
		{
			return phys2log(vrix.Row)[vrix.Column];
		}
		/// <summary> doesn't conflate for crossing group membership </summary>
		bool HasLogicalOrder(int i_level)
		{
			var _tmp = L2P[i_level];
			for (int i = 0; i < _tmp.Length; i++)
				if (_tmp[i] != i)
					return true;
			return false;
		}

		public static int[] normalize(int[] _order)
		{
			Debug.Assert(_order.Length > 1);

			var ret = IntArray.MakeIdentity(_order.Length);

			//alib.Array.arr.IntrospectiveSort(_order, ret);
			// .NET is way faster than my identical code; could it be NGEN?
			Array.Sort(_order, ret);	/// there can be lots of dups and -1's in _order; is there something deterministic to do?

			return ret;
		}
#if false
		int[] convert_to_physical(int i_level, int[] rg_log)
		{
			int c = rg_log.Length;
			if (c == 0)
				return level_info.NoEdges;

			var l2p = L2P[i_level];
			var rg_phys = new int[c];
			for (int i = 0; i < c; i++)
				rg_phys[i] = l2p[rg_log[i]];
			return rg_phys;
		}
		static int[] convert_to_logical(int[] rg_phys, int[] p2l)
		{
			int c = rg_phys.Length;
			if (c == 0)
				return level_info.NoEdges;

			var rg_log = new int[c];
			for (int i = 0; i < c; i++)
				rg_log[i] = p2l[rg_phys[i]];
			return rg_log;
		}
		int[] convert_to_logical(int i_level, int[] rg_phys)
		{
			return convert_to_logical(rg_phys, phys2log(i_level));
		}
#endif
		/// 
		/////////////////////////////////////////////////////////////


		/////////////////////////////////////////////////////////////
		/// 
		public bool median_sweep_downwards()
		{
			bool f_any = false;
			for (int i = 1; i < Count; i++)
				f_any |= set_medians_from_above(i);
			return f_any;
		}
		public bool median_sweep_upwards()
		{
			bool f_any = false;
			for (int i = Count - 2; i >= 0; i--)
				f_any |= set_medians_from_below(i);
			return f_any;
		}
		int[] LogicalUpper(int i_level, int ix_phys, out int lmax)
		{
			lmax = int.MinValue;
			int i, c;
			var upper = levels[i_level].arr[ix_phys].Upper;
			if ((c = upper.Length) == 0)
				return IntArray.Empty;
			var p2l_upper = phys2log(i_level - 1);
			var rg_log = new int[c];
			for (i = 0; i < c; i++)
				alib.Math.math.Maximize(ref lmax, rg_log[i] = p2l_upper[upper[i]]);
			return rg_log;
		}
		int[] LogicalLower(int i_level, int ix_phys, out int lmax)
		{
			lmax = int.MinValue;
			int i, c;
			var lower = levels[i_level].arr[ix_phys].Lower;
			if ((c = lower.Length) == 0)
				return IntArray.Empty;
			var p2l_lower = phys2log(i_level + 1);
			var rg_log = new int[c];
			for (i = 0; i < c; i++)
				alib.Math.math.Maximize(ref lmax, rg_log[i] = p2l_lower[lower[i]]);
			return rg_log;
		}
		int[] LogicalLowerSorted(int i_level, int ix_phys)
		{
			int i, c;
			var lower = levels[i_level].arr[ix_phys].Lower;
			if ((c = lower.Length) == 0)
				return IntArray.Empty;
			var p2l_lower = phys2log(i_level + 1);
			var rg_log = new int[c];
			for (i = 0; i < c; i++)
				rg_log[i] = p2l_lower[lower[i]];
			Array.Sort(rg_log);
			return rg_log;
		}
		int[] LogicalUpperSorted(int i_level, int ix_phys)
		{
			int i, c;
			var upper = levels[i_level].arr[ix_phys].Upper;
			if ((c = upper.Length) == 0)
				return IntArray.Empty;
			var p2l_upper = phys2log(i_level - 1);
			var rg_log = new int[c];
			for (i = 0; i < c; i++)
				rg_log[i] = p2l_upper[upper[i]];
			Array.Sort(rg_log);
			return rg_log;
		}
		public bool set_medians_from_above(int i_level)
		{
			if (i_level <= 0 || i_level >= levels.Count)
				return false;
			if (!levels[i_level].NeedLayout)
				return false;

			int[] _tmp, rgl = new int[levels[i_level].Count];
			int c, lmax;
			int m_prv = -rgl.Length;
			var p2l = phys2log(i_level);
			for (int i = 0; i < rgl.Length; i++)
			{
				int ixl = p2l[i];
				if ((c = (_tmp = LogicalUpper(i_level, i, out lmax)).Length) == 0)
				{
					Nop.CodeCoverage();
					rgl[i] = m_prv + ixl;
				}
				else
				{
					int ix = c >> 1;
					alib.Math.math.SelectMedian(_tmp, ix);
					rgl[i] = _tmp[ix] * rgl.Length + ixl;
					m_prv = lmax * rgl.Length;
				}
			}
			rgl = normalize(rgl);
			return SetLogicalOrder(i_level, rgl);
		}
		public bool set_medians_from_below(int i_level)
		{
			if (i_level < 0 || i_level >= levels.Count - 1)
				return false;
			if (!levels[i_level].NeedLayout)
				return false;

			int[] _tmp, rgl = new int[levels[i_level].Count];
			int[] rgixp_leaves = null;
			int c, lmax;
			int m_prv = -rgl.Length;
			var p2l = phys2log(i_level);
			for (int i = 0; i < rgl.Length; i++)
			{
				int ixl = p2l[i];
				if ((c = (_tmp = LogicalLower(i_level, i, out lmax)).Length) == 0)
				{
					rgl[i] = m_prv + ixl;
					alib.Array.arr.Append(ref rgixp_leaves, i);
				}
				else
				{
					int ix = c >> 1;
					alib.Math.math.SelectMedian(_tmp, ix);
					rgl[i] = _tmp[ix] * rgl.Length + ixl;
					m_prv = lmax * rgl.Length;
				}
			}

			rgl = normalize(rgl);

			//if (i_level > 0 && rgixp_leaves != null
			//	//&& !L2P[i_level].IsSorted() && !p2l.IsSorted() && !p2l.SequenceEqual(L2P[i_level])
			//	)
			//{
			//	rgl = set_leaf_positions_from_above(i_level, rgl, rgixp_leaves);
			//}

			return SetLogicalOrder(i_level, rgl);
		}

		public bool set_leaf_positions_from_above(int i_level)
		{
			if (i_level == 0)
				return false;
			if (!levels[i_level].NeedLayout)
				return false;


			var p2l = phys2log(i_level);
			var l2p = L2P[i_level];

			//if (l2p.IsSorted() || p2l.SequenceEqual(l2p))
			//	return false;

			var rgixp_leaves = Enumerable.Range(0, l2p.Length).Where(i => levels[i_level].arr[i].IsSimpleLayout).ToArray();

			var rgl = set_leaf_positions_from_above(i_level, l2p, rgixp_leaves);

			//var rg_log = new int[p2l.Length];
			//for (int i = 0; i < rg_log.Length; i++)
			//	rg_log[i] = p2l[rgl[i]];


			P2L[i_level] = null;
			L2P[i_level] = rgl;
			_invalidate_crossings();
			return true;
			//return SetLogicalOrder(i_level, rgl);
		}

		public int[] set_leaf_positions_from_above(int i_level, int[] rgl, int[] rgixp_leaves)
		{
			//if (!rgixp_leaves.IsSorted())
			//	throw new Exception();

			var fixedpos = rgl.Where(qq => Array.IndexOf(rgixp_leaves, qq) == -1).ToArray();

			var leaf_insert_grps = rgixp_leaves
							.Select(ixp_leaf => fixedpos.Interleavings(ixp_leaf).Select((hyp, insert_at) =>
							{
								var uppers = hyp.SelectMany(ixp => LogicalUpperSorted(i_level, ixp)).ToArray();
								return new
								{
									ixp_leaf,
									insert_at,
									cost = reordering_cost(uppers)
								};
							})
							.ArgMins(a => a.cost)
							.Middle())
						.GroupBy(a => a.insert_at)
						.OrderBy(g => g.Key);

			var p2l = phys2log(i_level);
			int accum = 0;
			foreach (var g in leaf_insert_grps)
			{
				var items = g.Select(a => a.ixp_leaf).OrderBy(_ixp => p2l[_ixp]).ToArray();
				arr.InsertRangeAt(ref fixedpos, g.Key + accum, items);
				accum += items.Length;
			}

			return fixedpos;
		}
#if false
		/// <summary>
		/// returns the LOGICAL median of a vertex with respect to another layer. This value
		/// is not any kind of index in this vertex's *own* layer, that is, it only has meaning in
		/// relation to other logical median values obtained between the same two layers
		/// </summary>
		public int log_median_from_phys(int i_level, int[] rg_phys)
		{
			var p2l = phys2log(i_level);
			int ix;
			if ((ix = rg_phys.Length) == 1)
				return p2l[rg_phys[0]];

			// note: doesn't matter which median we pick because it won't change the order
			ix >>= 1;
			alib.Math.math.SelectMedian(rg_phys, ix, p2l, __mc);
			return p2l[rg_phys[ix]];
		}
		static int __mc(int[] p2l, int x, int y) { return p2l[x] - p2l[y]; }
#endif
		/// 
		/////////////////////////////////////////////////////////////


		/////////////////////////////////////////////////////////////
		/// 
		public int total_crossings()
		{
			int sum;
			if ((sum = this.c_crossings) == -1)
			{
				int c = L2P.Length - 1;
				sum = 0;
				for (int i = 0; i < c; i++)
					sum += crossings_below(i);
				this.c_crossings = sum;
				crossings_changed(sum);
			}
			return sum;
		}
		protected virtual void crossings_changed(int sum)
		{
		}
		public int[] max_crossings_levels()
		{
			int i, x, x_max = 1, c_max = 0, c = L2P.Length - 1;
			for (i = 0; i < c; i++)
				if ((x = crossings_below(i)) == x_max)
					c_max++;
				else if (x > x_max)
				{
					x_max = x;
					c_max = 1;
				}
			if (c_max == 0)
				return IntArray.Empty;
			var ret = new int[c_max];
			while (c_max > 0)
				if (crossings_below(--i) == x_max)
					ret[--c_max] = i;
			return ret;
		}
		int crossings_below(int i_level)
		{
			//return levels[i_level].NeedLayout ? xb_compute_and_cache(i_level, ref c_lev_crossings[i_level]) : 0;
			if (i_level == c_lev_crossings.Length)
				return 0;
			return xb_compute_and_cache(i_level, ref c_lev_crossings[i_level]);
		}
		int xb_compute_and_cache(int i_level, ref int _cache)
		{
			int xb1, eb;
			if ((xb1 = _cache) == 0)
			{
				var lev = levels[i_level];
				if ((eb = lev.c_edges_below) > 0 /*&& lev.NeedLayout*/)
					xb1 = xb_compute(eb, i_level);
				_cache = ++xb1;
			}
			return xb1 - 1;
		}

		/// Wilhelm Barth and Petra Mutzel. 2004. "Simple and Efficient Bilayer Cross Counting"
		/// in: Journal of Graph Algorithms and Applications vol.8. no 2. pp. 179-194
		int xb_compute(int eb, int i_level)
		{
			int* work = stackalloc int[eb];

			add_work_ixs(levels[i_level].arr, work, L2P[i_level], phys2log(i_level + 1));

			return reordering_cost(work, eb);
		}

		static void add_work_ixs(layout_entry[] rgent, int* work, int[] l2p, int[] p2l_below)
		{
			int i, j, c;
			int[] rg;
			for (i = 0; i < l2p.Length; i++)
			{
				if ((c = (rg = rgent[l2p[i]].Lower).Length) > 0)
				{
					for (j = 0; j < c; j++)
						*work++ = p2l_below[rg[j]];

					/// the key to Barth and Mutzel is the following, which ensures that there is no latent
					/// cost associated with the ordering of lower verticies with respect to their (upper)
					/// connected vertex. Also note that we do not sort the overall work array itself.
					switch (c)
					{
						case 1:
							continue;
						default:
							arr.qsort(work - c, c);
							continue;
						case 2:
							break;
						case 3:
							arr.swap_if(work - 3, work - 2);
							arr.swap_if(work - 3, work - 1);
							break;
					}
					arr.swap_if(work - 2, work - 1);
				}
			}
		}
		static int reordering_cost(int* work, int c)
		{
			int* pi = work, pc = work + c;
			int v = *pi, cost = 0;
			while (++pi < pc)
				if (v <= *pi)
					v = *pi;
				else
				{
					int* pj = pi - 1;
					while (pj > work && *(pj - 1) > *pi)
						*pj = *--pj;
					*pj = *pi;
					*pi = v;
					cost += (int)(pi - pj);
				}
			return cost;
		}
#if false
		static void add_work_ixs(layout_entry[] rgent, int[] work, int[] l2p, int[] p2l_below, out int[] w2)
		{
			int i, j, c, k;
			int[] rg;
			w2 = work.ToArray();
			for (i = k = 0; i < l2p.Length; i++)
			{
				c = (rg = rgent[l2p[i]].Lower).Length;
				for (j = 0; j < c; j++)
				{
					work[k] = p2l_below[rg[j]];
					w2[k] = rg[j];
					k++;
				}

				/// the key to Barth and Mutzel is the following, which ensures that there is no latent
				/// cost associated with the ordering of lower verticies with respect to their (upper)
				/// connected vertex. Also note that we do not sort the overall work array itself.
				if (c > 1)
					Array.Sort(work, k - c, c);
			}
		}
#endif
		static int reordering_cost(int[] work)
		{
			int i = 0;
			int v = work[0], cost = 0;
			while (++i < work.Length)
				if (v <= work[i])
					v = work[i];
				else
				{
					int j = i - 1;
					while (j > 0 && work[j - 1] > work[i])
						work[j] = work[--j];
					work[j] = work[i];
					work[i] = v;
					cost += i - j;
				}
			return cost;
		}
		/// 
		/////////////////////////////////////////////////////////////



		///////////////////////////////////////////////////////////
		///
		public override bool Equals(Object obj)
		{
			var hyp = obj as logical_layout;
			return hyp != null && Equals(hyp);
		}
		public bool Equals(logical_layout other)
		{
			if (_hc == 0)
				Debug.Print("warning: logical_layout hash code was not checked before calling Equals");

			return levels.Equals(this, other);
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int _hc;
		public override int GetHashCode()
		{
			return _hc != 0 ? _hc : _hc = levels.GetHashCode(this);
		}
		///
		///////////////////////////////////////////////////////////


#if false
		public void randomize_level(int i_level)
		{
			var rg_phys = L2P[i_level].ToArray();
			rg_phys.Shuffle();
			SetLogicalOrder(i_level, rg_phys, false);
		}
		public bool center_edge_mass_triples(int incr)
		{
			bool f_any = false;
			for (int i_level = 0; i_level < Count; i_level++)
			{
				f_any |= center_edge_mass_triple(i_level, incr);
			}
			return f_any;
		}

		static void double_swap(ref int a, ref int b, ref int x, ref int y)
		{
			int t = a; a = b; b = t; t = x; x = y; y = t;
		}

		public bool center_edge_mass_triple(int i_level, int incr)
		{
			var lev = levels[i_level];
			if (lev.Count < 3)
				return false;

			var _new = this[i_level].ToArray();
			//int a, b, c, ix = incr > 0 ? 0 : _new.Length - 1;
			bool f_any = false;

			//var e = edge_masses(lev, _new, true).GetEnumerator();
			//if (!e.MoveNext())
			//	return false;
			//a = e.Current;
			//if (!e.MoveNext())
			//	return false;
			//b = e.Current;
			var lem = lev.EdgeMasses;

			//while (e.MoveNext())
			var rgent = lev.arr;
			for (int ix = 0; ix <= rgent.Length - 3; ix++)
			{
				//a = lev.arr[_new[ix - 2]].EdgeMass;
				//b = lev.arr[_new[ix - 1]].EdgeMass;
				//c = lev.arr[_new[ix/**/]].EdgeMass;

				//c = e.Current;
				//;
				//if (a < b && a < c)
				//else if (c < a && c < b)
				//double_swap(ref _new[ix - 1], ref _new[ix]);

				if (lem[_new[ix]] < lem[_new[ix + 1]] && lem[_new[ix]] < lem[_new[ix + 2]])
					double_swap(ref _new[ix], ref _new[ix + 1], ref lem[_new[ix]], ref lem[_new[ix + 1]]);
				if (lem[_new[ix + 2]] < lem[_new[ix]] && lem[_new[ix + 2]] < lem[_new[ix + 1]])
					double_swap(ref _new[ix + 2], ref _new[ix + 1], ref lem[_new[ix + 2]], ref lem[_new[ix + 1]]);
				else
					continue;
				f_any = true;
			}
			//while ((uint)(ix += incr) < (uint)lev.Count) ;
			if (!f_any)
				return false;
			SetPhysicalOrder(i_level, _new);
			return true;
		}

		static IEnumerable<int> edge_masses(level_info lev, int[] l2p, bool f_left)
		{
			var rgent = lev.arr;
			int c = rgent.Length - 1;
			for (int i = 0; i <= c; i++)
				yield return rgent[l2p[f_left ? c - i : i]].EdgeMass;
		}
#endif

		public override String ToString()
		{
			//return String.Format("logical_layout-{0:X8}({1:X8})  levels:{2,-2}  verticies:{3,-3}  edges:{4,-3}  total crossings:{5,3}",
			//	_hc != 0 ? _hc : compute_hash_code(),
			//	RuntimeHelpers.GetHashCode(this),
			//	Count,
			//	levels.VertexCount,
			//	levels.EdgeCount,
			//	c_crossings == -1 ? "(not computed)" : c_crossings.ToString());

			//var s_delta = hyp_prev == null ? "" : (c_crossings - hyp_prev.c_crossings).ToString();
			return String.Format("{0}-{1:X8}({2:X8})   crossings:{3,4}",//				depth:{2,4} {5,6} {6,5}",
				GetType().Name,
				_hc != 0 ? _hc : levels.GetHashCode(this),
				RuntimeHelpers.GetHashCode(this),
				c_crossings == -1 ? "(not computed)" : c_crossings.ToString());
		}

		public String report()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine(alib.String._string_ext.CondenseSpaces(this.ToString()));
			for (int i_level = 0; i_level < Count; i_level++)
				sb.AppendLine("  " + log_level_display(i_level));
			//sb.AppendLine("total crossings:" + total_crossings());
			return sb.ToString();
		}

		public String log_level_display(int i_level)
		{
			var lev = levels[i_level];
			int x = crossings_below(i_level);
			return String.Format("L{0,-3} {1} -- groups:{2} -- crossings:{3,3} -- {4}",
				i_level,
				HasLogicalOrder(i_level) ? " LOG" : "PHYS",
				lev.c_crossing_groups.ToString().PadLeft(3) + "/" + lev.Count.ToString().PadRight(3),
				x == 0 ? "" : x.ToString(),
				VrefsLogical(i_level).Select(vr => "V" + (int)vr).StringJoin(" "));
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public debug_logical_level[] _dbg__
		{
			get
			{
				var ret = new debug_logical_level[Count];
				for (int i = 0; i < ret.Length; i++)
					ret[i] = new debug_logical_level(this, i);
				return ret;
			}
		}
	};
}