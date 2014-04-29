using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.dg;
using alib.Enumerable;

namespace alib.Wpf
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class LogLevelInfo : IGraphExLayoutLevel, IGraphExProxy, IReadOnlyList<layout_vertex_base>, IEqualityComparer<int[]>
	{
		public LogLevelInfo(WpfGraphAdapter g, int i_level, layout_vertex_base[] rg)
		{
			this.g = g;
			this.i_level = i_level;
			this.rg = rg;
			if (rg.Length == 0)
				throw new Exception("Level cannot be empty");

			for (int i = 0; i < rg.Length; i++)
				rg[i].Column = i;
		}

		public void post_init()
		{
			if (i_level > 0)
				this.PrevLevel = levels[i_level - 1];

			if (i_level < levels.Length - 1)
				this.NextLevel = levels[i_level + 1];

			for (int i = 0; i < rg.Length; i++)
				rg[i].set_level_hints(ref c_edges_above, ref c_edges_below);

			bool can_cross_above = rg.Count(ent => ent.Upper.Length > 0 && ent.EdgeCount > 1) > 1;
			bool can_cross_below = rg.Count(ent => ent.Lower.Length > 0 && ent.EdgeCount > 1) > 1;

			this.NeedLayout = rg.Length > 1;// can_cross_above | can_cross_below;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly WpfGraphAdapter g;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IGraphEx IGraphExProxy.GraphInstance { get { return g; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public WpfGraphAdapter GraphInstance { get { return g; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		LogLevelInfo[] levels { get { return g.levels; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly int i_level;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int LevelNum { get { return i_level; } }

		public int c_edges_above, c_edges_below;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly layout_vertex_base[] rg;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LogLevelInfo PrevLevel, NextLevel;

		public bool NeedLayout;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsTopLevel { [DebuggerStepThrough] get { return i_level == 0; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsBottomLevel { [DebuggerStepThrough] get { return NextLevel == null; } }

		public layout_vertex_base this[int index] { get { return rg[index]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count { get { return rg.Length; } }

		public IEnumerator<layout_vertex_base> GetEnumerator() { return rg.Enumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return rg.Enumerator(); }

		ILogicalLayoutVertexEx IReadOnlyList<ILogicalLayoutVertexEx>.this[int index] { get { return rg[index]; } }

		IEnumerator<ILogicalLayoutVertexEx> IEnumerable<ILogicalLayoutVertexEx>.GetEnumerator() { return rg.Enumerator(); }

		///////////////////////////////////////////////////////////
		///
		public bool Equals(int[] l2p_1, int[] l2p_2)
		{
#if true
			return IntArray.EqualityComparer.Equals(l2p_1, l2p_2);
#else
			//if (NeedLayout)
			for (int j = 0; j < l2p_1.Length; j++)
				if (crossing_groups[l2p_1[j]] != crossing_groups[l2p_2[j]])
					return false;
			return true;
#endif
		}
		public int GetHashCode(int[] l2p)
		{
#if true
			return IntArray.EqualityComparer.GetHashCode(l2p);
#else
			int h, k, i, g;
			h = i = l2p.Length;
			//if (NeedLayout)
			for (k = 7; --i >= 0; k += 7)
				h ^= ((g = crossing_groups[l2p[i]]) << k) | (int)((uint)g >> (32 - k));
			return h;
#endif
		}
		///
		///////////////////////////////////////////////////////////

		public override string ToString()
		{
			return String.Format("L{0}  c={1}", i_level, rg.Length);
		}
	};
}
