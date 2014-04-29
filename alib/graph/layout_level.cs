using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	using Array = System.Array;
	using String = System.String;
	using Enumerable = System.Linq.Enumerable;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public struct layout_entry
	{
		public layout_entry(Vref vr)
		{
			this.vr = vr;
			this.Upper = this.Lower = null;
		}

		public Vref vr;

		public int[] Upper, Lower;

		public bool IsSimpleLayout { get { return EdgeCount == 1; } }

		public int EdgeCount { get { return Upper.Length + Lower.Length; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class level_infos : IReadOnlyList<level_info>, IEqualityComparer<IReadOnlyList<int[]>>
	{
		public level_infos(layout_graph lg, dg_topographic topo)
		{
			this.lg = lg;
			this.a = initialize_layers(topo);

			int c_missing_vertex_proxies = 0;

			for (int i = 0; i < a.Length; i++)
				c_missing_vertex_proxies += a[i].post_init(this);

			if (c_missing_vertex_proxies > 0)
				Debug.Print("warning: missing at least {0} vertex proxies. Edges between non-adjacent layout levels will not affect layout.", c_missing_vertex_proxies);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly layout_graph lg;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly level_info[] a;

		level_info[] initialize_layers(dg_topographic topo)
		{
			int i, lev;
			Vref vr;

			var vv = topo.Verticies;
			var rgc = new int[topo.LevelCount];
#if false
			var rgc_xr = new int[lg.LevelCount];

			for (vr = Vref.Zero; vr < vv.Length; vr++)
				if (lg.is_indexed_vref(vr))
				{
					rgc[lev = vv[vr].i_level]++;
					if (lg.vertex_edges_can_cross(vr))
						rgc_xr[lev]++;
				}

			var _tmp = new level_info[rgc.Length];
			for (i = 0; i < rgc.Length; i++)
				_tmp[i] = new level_info(this, i, rgc[i], rgc_xr[i]);

			var _brk = rgc_xr.ToArray();
			for (vr = new Vref(vv.Length - 1); vr >= 0; vr--)
				if (lg.is_indexed_vref(vr))
				{
					lev = vv[vr].i_level;

					int ix_phys = vv[vr].ix_phys = lg.vertex_edges_can_cross(vr) ? --_brk[lev] : --rgc[lev];

					_tmp[lev].arr[ix_phys] = new layout_entry(vr);
				}
#else
			VertexEx vx;

			for (vr = Vref.Zero; vr < vv.Count; vr++)
				if (lg.vertex_is_valid(vr) && (vx = vv[vr]) != null && !vx.LayoutExempt && (lev = vx.Row) != -1)
					rgc[lev]++;

			var _tmp = new level_info[rgc.Length];
			for (i = 0; i < rgc.Length; i++)
				_tmp[i] = new level_info(this, i, rgc[i]);

			for (vr = new Vref(vv.Count - 1); vr >= 0; vr--)
				if ((vx = vv[vr]) != null && !vx.LayoutExempt)
				{
					int row, col;
					row = vx.Row;
					vx.Column = col = --rgc[row];
					_tmp[row].arr[col] = new layout_entry(vr);
				}

#endif
			return _tmp;
		}

		/// <summary> counts only verticies used/active in the layouter </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int VertexCount { get { return a.Sum(ll => ll.arr.Length); } }

		/// <summary> counts only edges used/active in the layouter </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int EdgeCount { get { return a.Sum(ll => ll.c_edges_below); } }

		public level_info this[int index] { [DebuggerStepThrough] get { return a[index]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count { [DebuggerStepThrough] get { return a.Length; } }

		public IEnumerator<level_info> GetEnumerator() { return a.Enumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public String report()
		{
			var sb = new System.Text.StringBuilder();
			foreach (var lev in a)
				sb.AppendLine(lev.ToString());
			return sb.ToString();
		}


		/////////////////////////////////////////////////////////////
		/// 
		public bool Equals(IReadOnlyList<int[]> x, IReadOnlyList<int[]> y)
		{
			if (x == y)
				return true;
			if (x.Count != y.Count)
				return false;

			for (int i_level = 0; i_level < Count; i_level++)
				if (!a[i_level].Equals(x[i_level], y[i_level]))
					return false;
			return true;
		}
		public int GetHashCode(IReadOnlyList<int[]> obj)
		{
			int h = base.GetHashCode();
			for (int i_level = 0; i_level < obj.Count; i_level++)
				h ^= a[i_level].GetHashCode(obj[i_level]);
			if (h == 0)
				h = 1;
			return h;
		}
		/// 
		/////////////////////////////////////////////////////////////
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary> one layer of a topologially-sorted directed graph </summary>
	/// <remarks>
	/// maintains a fixed number of verticies in a fixed physical order determined by the index of a respective
	/// <code>layout_entry</code> structs within <code>arr</code>. Each <code>layout_entry</code> contains an index 
	/// which establishes a logical ordering for this layer, and this class (level_info) maintains an index from
	/// these logical indicies back to the physical index.
	/// </remarks>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class level_info : /* for Count only: */ IReadOnlyCollection<Object>, IEqualityComparer<int>, IEqualityComparer<int[]>
	{
		public level_info(level_infos levels, int i_level, int c_alloc)
		{
			this.levels = levels;
			this.i_level = i_level;
			this.arr = new layout_entry[c_alloc];
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly level_infos levels;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly int i_level;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal
		readonly layout_entry[] arr;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int c_edges_above /*can probably get rid of one of these*/, c_edges_below;

		public int[] crossing_groups;
		public int c_crossing_groups;

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool NeedLayout;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public level_info PrevLevel, NextLevel;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsTopLevel { [DebuggerStepThrough] get { return i_level == 0; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsBottomLevel { [DebuggerStepThrough] get { return NextLevel == null; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int[] EdgeCounts
		{
			get
			{
				int c;
				if ((c = arr.Length) == 0)
					return IntArray.Empty;
				var ret = new int[c];
				for (int i = 0; i < c; i++)
					ret[i] = arr[i].EdgeCount;
				return ret;
			}
		}

		/////////////////////////////////////////////////////////////
		/// 
		public int post_init(level_infos levels)
		{
			if (i_level > 0)
				this.PrevLevel = levels[i_level - 1];
			if (i_level < levels.Count - 1)
				this.NextLevel = levels[i_level + 1];

			int c_missing = set_level_hints();

			int c;
			this.crossing_groups = build_crossing_groups(out c);
			this.c_crossing_groups = c;

			//if (CanCrossBelow != (c_crossing_groups > 1))
			//	Nop.X();

			//show_crossing_groups();

			bool can_cross_above = arr.Count(ent => ent.Upper.Length > 0 && ent.EdgeCount > 1) > 1;
			bool can_cross_below = arr.Count(ent => ent.Lower.Length > 0 && ent.EdgeCount > 1) > 1;
			this.NeedLayout = can_cross_above | can_cross_below;

			return c_missing;
		}
		int set_level_hints()
		{
			c_edges_above = c_edges_below = 0;
			int c_missing = 0;
			for (int i = 0; i < arr.Length; i++)
			{
				c_missing += set_vertex_hints(ref arr[i]);

				//if (arr[i].VertexEdgesCanCross != levels.lg.vertex_edges_can_cross(arr[i].vr))
				//	Nop.X();
			}

			//rgcc_above = Enumerable.Range(0, arr.Length).Where(z => arr[z].Upper.Length > _?_).ToArray();
			//rgcc_below = Enumerable.Range(0, arr.Length).Where(z => arr[z].Lower.Length > 0).ToArray();

			return c_missing;
		}
		int[] build_crossing_groups(out int count)
		{
			var ret = new int[arr.Length];
			count = 0;
			foreach (var g in Enumerable.Range(0, ret.Length).GroupBy<int, int>(z => z, this))
			{
				var garr = g.ToArray();
				int grp_num = ~garr.Min();
				for (int i = 0; i < garr.Length; i++)
					ret[garr[i]] = grp_num;
				count++;
			}
			return ret;
		}
		int set_vertex_hints(ref layout_entry ent)
		{
			int c_missing = make_hint_arrays(ent.vr, out ent.Upper, out ent.Lower);
			c_edges_above += ent.Upper.Length;
			c_edges_below += ent.Lower.Length;
			return c_missing;
		}
		int make_hint_arrays(Vref vr, out int[] upper, out int[] lower)
		{
			Vref[] rgvr;
			int c_missing = 0;

			if (IsTopLevel || (rgvr = levels.lg.vertex_parents_distinct(vr)).Length == 0)
				upper = IntArray.Empty;
			else
				c_missing += rgvr.Length - (upper = PrevLevel.create_hint_arr(rgvr)).Length;

			if (IsBottomLevel || (rgvr = levels.lg.vertex_children_distinct(vr)).Length == 0)
				lower = IntArray.Empty;
			else
				c_missing += rgvr.Length - (lower = NextLevel.create_hint_arr(rgvr)).Length;

			return c_missing;
		}
		int[] create_hint_arr(Vref[] rgvr)
		{
			Debug.Assert(rgvr.Length > 0 && rgvr.IsDistinct());

			int i, j, c = rgvr.Length;
			int[] cache = new int[c];
			for (i = j = 0; i < c; i++)
			{
				Vref vr = rgvr[i];
				if (levels.lg.ignore_vertex(vr))
					continue;
				var vx = levels.lg.topo.Verticies[vr].LogicalPosition;

				/// if you hit this assert then perhaps verticies are not in adjacent layers as they should be.
				Debug.Assert(vx.Row == this.i_level && this.arr[vx.Column].vr == vr);
				//if (vx.i_level != i_level || arr[vx.ix_phys].vr != vr)
				//	continue;

				cache[j++] = vx.Column;
			}
			if (j < c)
				alib.Array.arr.Resize(ref cache, j);
			if (c > 1)
				Array.Sort(cache);

			return cache;
		}
#if false
		bool _can_cross_below()
		{
			int c;
			if (c_crossing_groups <= 1 || c_edges_below <= 1 || (c = arr.Length) <= 1)
				return false;

			/// to be strict, there cannot be more than exactly one non-leaf in this row.
			/// but if we trust the median machinery, it would be safe to relax this condition and allow
			/// any number of non-interconnected subtrees
			bool f_one = false;
			while (--c > 0)
				if (f_one & (f_one = arr[c].Lower.Length != 0))
					return true;
			return false;
		}
#endif
		/// 
		/////////////////////////////////////////////////////////////


#if false
		void foo(logical_layout hyp, int[] rgl)
		{
			var p2l = hyp.phys2log(i_level);

			for (int i = 0; i < rgl.Length; i++)
			{
				if (rgl[i] == int.MinValue)
				{
					var ixl = p2l[i];

					int _left, _right;

					var q = hyp[i_level].Take(ixl).Select(z => rgl[z]).Where(z => z != int.MinValue).ToArray();
					if (q.Length == 0)
						_left = -rgl.Length;
					else
						_left = (int)q.Average();

					q = hyp[i_level].Skip(ixl + 1).Select(z => rgl[z]).Where(z => z != int.MinValue).ToArray();
					if (q.Length == 0)
						_right = rgl.Length * rgl.Length;
					else
						_right = (int)q.Average();

					rgl[i] = (_left + _right) / 2 + ixl;
				}
			}
		}
#endif

		///////////////////////////////////////////////////////////
		///
		public bool Equals(int[] l2p_1, int[] l2p_2)
		{
			//return IntArray.EqualityComparer.Equals(l2p_1, l2p_2);

			//if (NeedLayout)
			for (int j = 0; j < l2p_1.Length; j++)
				if (crossing_groups[l2p_1[j]] != crossing_groups[l2p_2[j]])
					return false;
			return true;
		}
		public int GetHashCode(int[] l2p)
		{
			//return IntArray.EqualityComparer.GetHashCode(l2p);

			int h, k, i, g;
			h = i = l2p.Length;
			//if (NeedLayout)
			for (k = 7; --i >= 0; k += 7)
				h ^= ((g = crossing_groups[l2p[i]]) << k) | (int)((uint)g >> (32 - k));
			return h;
		}
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		bool IEqualityComparer<int>.Equals(int ixp_a, int ixp_b)
		{
			if (crossing_groups != null)
				return crossing_groups[ixp_a] == crossing_groups[ixp_b];

#if true
			int[] ua = arr[ixp_a].Upper,
				  ul = arr[ixp_a].Lower;
			return (ua.Length == 0 || ul.Length == 0) &&
					ua.SequenceEqual(arr[ixp_b].Upper) &&
					ul.SequenceEqual(arr[ixp_b].Lower);
#else
			if (!arr[ixp_a].Upper.SequenceEqual(arr[ixp_b].Upper))
				return false;

			int[] la, lb;

			if ((la = arr[ixp_a].Lower).Length != (lb = arr[ixp_b].Lower).Length)
				return false;

			if (la.Length == 0 || la.SequenceEqual(lb))
				return true;

			if (la.Length != 1)
				return false;

			var lg = levels.lg;
			var qa = lg.vertex_descendants(arr[ixp_a].vr);
			var qb = lg.vertex_descendants(arr[ixp_b].vr);
			if (qa.Length != qb.Length)
				return false;
			if (qa.SequenceEqual(qb))
				return true;

			for (int i = 0; i < qa.Length; i++)
				if (qa[i] != qb[i] && (!lg.vertex_is_unary_path(qa[i]) || !lg.vertex_is_unary_path(qb[i])))
					return false;
			return true;
#endif
		}
		int IEqualityComparer<int>.GetHashCode(int x)
		{
			return crossing_groups == null ? 0 : crossing_groups[x];
		}
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		public Vref this[int ix_phys]
		{
			get { return arr[ix_phys].vr; }
		}
		public IEnumerable<Vref> VrefsPhysical()
		{
			for (int ix_phys = 0; ix_phys < arr.Length; ix_phys++)
				yield return arr[ix_phys].vr;
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count { [DebuggerStepThrough] get { return arr.Length; } }
		public IEnumerator<Object> GetEnumerator() { throw not.impl; }
		IEnumerator IEnumerable.GetEnumerator() { throw not.impl; }
		///
		///////////////////////////////////////////////////////////
	};
}
