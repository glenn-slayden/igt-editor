using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using alib.Debugging;
using alib.Enumerable;
using alib.Hashing;

namespace alib.dg
{
#if DEBUG
	using alib.dg.debug;
#endif
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public struct list_info
	{
		public Eref first;
		public Eref rest;
		public bool IsValid { get { return first >= 0 && rest >= 0; } }
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public struct LogicalPosition
	{
		public LogicalPosition(int row, int col)
		{
			this.Row = row;
			this.Column = col;
		}
		public int Row;
		public int Column;
		public override String ToString()
		{
			return String.Format("({0},{1})", Row, Column);
		}
	};

	public interface IGraphCommon
	{
		IGraphCommon SourceGraph { get; }
		int EdgeCount { get; }
		int VertexCount { get; }
	};

	public interface IGraph : IGraphCommon
	{
		Vref[] vr_roots { get; }
		Vref[] vr_leafs { get; }

		bool edge_is_valid(Eref e);
		Vref edge_parent(Eref e);
		int edge_peer_count(Eref e);
		Eref edge_next_peer(Eref e);
		Vref edge_target(Eref e);
		int edge_value(Eref e);
		String edge_string(Eref e);
		Object edge_ref(Eref e);

		bool vertex_is_root(Vref v);
		bool vertex_is_leaf(Vref v);
		bool vertex_is_valid(Vref v);
		Eref[] vertex_in_edges(Vref v);
		Vref[] vertex_parents(Vref v);
		Eref vertex_master_edge(Vref v);
		int vertex_out_edge_count(Vref v_target);
		Eref[] vertex_out_edges(Vref v);
		int vertex_value(Vref v);
		String vertex_string(Vref v);
		Object vertex_ref(Vref v);
	};

	public interface IGraphRaw : IGraph
	{
		IReadOnlyList<dg.Edge> _E_Raw { get; }
		IReadOnlyList<dg.Vertex> _V_Raw { get; }
	};

	public interface IAvm : IGraph
	{
		bool edge_target_coreferenced(Eref e);
		bool edge_is_non_coref_or_master(Eref e);
		Eref[] vertex_out_edges_display_order(Vref v);
		Eref get_edge(Vref v_from, Vref v_to);
		bool vertex_is_list(Vref v);
		bool vertex_is_coreferenced(Vref v);
		IEnumerable<list_info> vertex_list_infos(Vref v);
	};

	public interface IEditGraph : IGraphRaw
	{
		int EdgeAlloc { get; }
		int VertexAlloc { get; }
		Vref AddVertex(int value);
		Eref CreateEdge(Vref v_from, Vref v_to, int value);
		void DeleteEdge(Eref er);
		void DeleteAllInEdges(Vref vr);
		void DeleteVertex(Vref vr);
	};

	public interface IGraphData<TEdge, TVertex> : IGraphRaw
	{
		IIndexedHash<TEdge> EDict { get; }
		IIndexedHash<TVertex> VDict { get; }
	};

	public interface IStringGraph : IGraphData<String, String>
	{
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[Flags]
	public enum EdgeDirection
	{
		None = 0, Normal = 1, Reversed = 2, Both = 3
	};

	public enum LayoutDirection
	{
		LeftToRight = 0,	// System.Windows.Controls.Orientation.Horizontal
		TopToBottom = 1,	// System.Windows.Controls.Orientation.Vertical
		RightToLeft = 2,
		BottomToTop = 3,
	};

	public interface IGraphEx
	{
	}
	public interface IGraphExImpl : IGraphEx, IGraphCommon
	{
		IReadOnlyList<IVertexEx> Roots { get; }
		IReadOnlyList<IVertexEx> Leaves { get; }
		IReadOnlyList<IVertexEx> Verticies { get; }
		IReadOnlyList<IEdgeEx> Edges { get; }
	};
	public interface IGraphExProxy : IGraphEx
	{
		IGraphEx GraphInstance { get; }
	};
	public interface IGraphExElement : IGraphExProxy
	{
	};
	public interface IGraphExContentElement : IGraphExElement
	{
		int Index { get; }
		String TextLabel { get; }
	};
	public interface IEdgeEx : IGraphExContentElement
	{
		IVertexEx From { get; }
		IVertexEx To { get; }
	};
	public interface IVertexEx : IGraphExContentElement
	{
		IReadOnlyList<IEdgeEx> InEdges { get; }
		IReadOnlyList<IEdgeEx> OutEdges { get; }
	};
	public interface IGraphExLogicalElement : IGraphExContentElement
	{
		bool HideContent { get; set; }
	};
	public interface ILogicalLayoutEdgeEx : IEdgeEx, IGraphExLogicalElement
	{
		new ILogicalLayoutVertexEx From { get; }
		new ILogicalLayoutVertexEx To { get; }
		EdgeDirection EdgeDirection { get; }
	};
	public interface ILogicalLayoutVertexEx : IVertexEx, IGraphExLogicalElement
	{
		new IReadOnlyList<ILogicalLayoutEdgeEx> InEdges { get; }
		new IReadOnlyList<ILogicalLayoutEdgeEx> OutEdges { get; }
		LogicalPosition LogicalPosition { get; }
		bool LayoutExempt { get; }
	};
	public interface IGraphExLayoutLevel : IGraphExProxy, IReadOnlyList<ILogicalLayoutVertexEx>
	{
		int LevelNum { get; }
	};
	public interface IGraphExLayout : IReadOnlyList<IGraphExLayoutLevel>
	{
		int TotalCrossings { get; }
	};
	public interface IGraphExLayoutProvider : IGraphExImpl, IReadOnlyList<IGraphExLayoutLevel>
	{
		new IReadOnlyList<ILogicalLayoutVertexEx> Roots { get; }
		new IReadOnlyList<ILogicalLayoutVertexEx> Leaves { get; }
		new IReadOnlyList<ILogicalLayoutVertexEx> Verticies { get; }
		new IReadOnlyList<ILogicalLayoutEdgeEx> Edges { get; }

		LayoutDirection LayoutDirection { get; }
		bool ExtendLeafVerticies { get; }
		bool CompactRootVerticies { get; }
		IGraphExLayout ActiveLayout { get; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public interface IGraphFull : IGraph, IGraphExLayoutProvider
	{
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[Flags]
	public enum status : int
	{
		Zero /*					*/ = 0,
		_neg /*					*/ = unchecked((int)0x80000000),
		None /*					*/ = _neg /*						*/ | 0x0001,
		NotValid /*				*/ = _neg /*						*/ | 0x0002,
		FreelistTerm /*			*/ = _neg /*						*/ | 0x0003,
		NotInFreelist /*		*/ = _neg /*						*/ | 0x0004,
		free /*					*/ = _neg /*						*/ | 0x0010,
		fe_HasList /*			*/ = free /*						*/ | 0x0020,
		fe_HasSize /*			*/ = free /*						*/ | 0x0040,
		fe_BlockStart /*		*/ = fe_HasSize | fe_HasList /*		*/ | 0x0100,
		fe_BlockEnd /*			*/ = fe_HasSize /*					*/ | 0x0200,
		fe_BlockSingleton /*	*/ = fe_BlockStart | fe_BlockEnd,
	};


	[DebuggerDisplay("{_dbg.ToString(),nq}", Type = "Eref")]
	public struct Eref : IEquatable<Eref>, IComparable<Eref>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static Eref Zero = new Eref(status.Zero);
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static Eref None = new Eref(status.None);
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static Eref NotValid = new Eref(status.NotValid);

		[DebuggerStepThrough]
		public Eref(int ix) { this.ix = ix; }
		[DebuggerStepThrough]
		public Eref(status es) { this.ix = (int)es; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int ix;
		[DebuggerStepThrough]
		public static implicit operator int(Eref er) { return er.ix; }
		[DebuggerStepThrough]
		public static implicit operator uint(Eref er) { return (uint)er.ix; }
		[DebuggerStepThrough]
		public static implicit operator status(Eref er) { return (status)er.ix; }
		[DebuggerStepThrough]
		public static Eref operator +(Eref er, int i) { er.ix += i; return er; }
		[DebuggerStepThrough]
		public static int operator +(int i, Eref er) { return er.ix + i; }
		[DebuggerStepThrough]
		public static Eref operator ++(Eref er) { er.ix++; return er; }
		[DebuggerStepThrough]
		public static Eref operator --(Eref er) { er.ix--; return er; }
		[DebuggerStepThrough]
		public static bool operator ==(Eref a, Eref b) { return a.ix == b.ix; }
		[DebuggerStepThrough]
		public static bool operator !=(Eref a, Eref b) { return a.ix != b.ix; }
		[DebuggerStepThrough]
		public int CompareTo(Eref other) { return this.ix - other.ix; }
		[DebuggerStepThrough]
		public bool Equals(Eref other) { return this.ix == other.ix; }
		public override bool Equals(Object obj) { return obj is Eref && this.ix == ((Eref)obj).ix; }
		public override int GetHashCode() { return ix; }
#if DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public eent_dbg _dbg
		{
			[DebuggerStepThrough]
			get { return new eent_dbg(dg._singleton, this); }
		}
#endif
	};

	[DebuggerDisplay("{_dbg.ToString(),nq}", Type = "Vref")]
	public struct Vref : IEquatable<Vref>, IComparable<Vref>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static Vref Zero = new Vref(status.Zero);
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static Vref NotValid = new Vref(status.NotValid);
		[DebuggerStepThrough]
		public Vref(status s) { this.ix = (int)s; }
		[DebuggerStepThrough]
		public Vref(int ix) { this.ix = ix; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int ix;
		[DebuggerStepThrough]
		public static implicit operator int(Vref vr) { return vr.ix; }
		[DebuggerStepThrough]
		public static implicit operator uint(Vref vr) { return (uint)vr.ix; }
		[DebuggerStepThrough]
		public static explicit operator Vref(int ix) { Vref vr; vr.ix = ix; return vr; }
		[DebuggerStepThrough]
		public static Vref operator ++(Vref vr) { vr.ix++; return vr; }
		[DebuggerStepThrough]
		public static Vref operator --(Vref vr) { vr.ix--; return vr; }
		[DebuggerStepThrough]
		public static bool operator ==(Vref a, Vref b) { return a.ix == b.ix; }
		[DebuggerStepThrough]
		public static bool operator !=(Vref a, Vref b) { return a.ix != b.ix; }
		[DebuggerStepThrough]
		public int CompareTo(Vref other) { return this.ix - other.ix; }
		[DebuggerStepThrough]
		public bool Equals(Vref other) { return this.ix == other.ix; }
		public override bool Equals(Object obj) { return obj is Vref && this.ix == ((Vref)obj).ix; }
		public override int GetHashCode() { return ix; }
#if DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public vent_dbg _dbg
		{
			[DebuggerStepThrough]
			get { return new vent_dbg(dg._singleton, this); }
		}
#endif
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class _dg_ext
	{
#if false
		//public static Eref[] vertex_in_edges(this IGraph g, Vref v)
		//{
		//	return ((dg)g).vertex_in_edges(v);
		//}
		public static Eref vertex_master_edge(this IGraph g, Vref v)
		{
			return ((dg)g).vertex_master_edge(v);
		}
		public static void vertex_switch_master_edge(this IGraph g, Vref v, Eref e_new)
		{
			((dg)g).vertex_switch_master_edge(v, e_new);
		}

		//public static Eref[] vertex_out_edges(this IGraph g, Vref v)
		//{
		//	return ((dg)g).vertex_out_edges(v);
		//}


		//public static Vref edge_parent(this IGraph g, Eref e)
		//{
		//	return ((dg)g).edge_parent(e);
		//}
		//public static Vref edge_target(this IGraph g, Eref e)
		//{
		//	return ((dg)g).edge_target(e);
		//}
		public static int edge_peer_count(this IGraph g, Eref e)
		{
			return ((dg)g).edge_peer_count(e);
		}
		public static Eref edge_next_peer(this IGraph g, Eref e)
		{
			return ((dg)g).edge_next_peer(e);
		}
		public static bool edge_is_non_coref_or_master(this IGraph g, Eref e)
		{
			return ((dg)g).edge_is_non_coref_or_master(e);
		}
#endif
		public static int EdgeAlloc(this IGraphCommon g)
		{
			var rwg = g as IEditGraph;
			return rwg != null ? rwg.EdgeAlloc : g.EdgeCount;
		}
		public static int VertexAlloc(this IGraphCommon g)
		{
			var rwg = g as IEditGraph;
			return rwg != null ? rwg.VertexAlloc : g.VertexCount;
		}
		public static bool edge_is_valid(this IGraphCommon g, Eref e)
		{
			var dg = g as IGraph;
			if (dg != null)
				return dg.edge_is_valid(e);
			return ((IGraphExImpl)g).Edges[e] != null;
		}
		public static bool vertex_is_valid(this IGraphCommon g, Vref v)
		{
			var dg = g as IGraph;
			if (dg != null)
				return dg.vertex_is_valid(v);
			return ((IGraphExImpl)g).Verticies[v] != null;
		}
		public static bool vertex_is_root(this IGraphCommon g, Vref v)
		{
			var dg = g as IGraph;
			if (dg != null)
				return dg.vertex_is_root(v);
			return ((IGraphExImpl)g).Verticies[v].InEdges.Count == 0;
		}
		public static Vref[] vertex_parents(this IGraphCommon g, Vref v)
		{
			var dg = g as IGraph;
			if (dg != null)
				return dg.vertex_parents(v);

			var rge = ((IGraphExImpl)g).Verticies[v].InEdges;
			var ret = new Vref[rge.Count];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = new Vref(rge[i].From.Index);
			return ret;
		}
		public static bool vertex_is_leaf(this IGraphCommon g, Vref v)
		{
			return vertex_out_edge_count(g, v) == 0;
		}
		public static int vertex_out_edge_count(this IGraphCommon g, Vref v)
		{
			var dg = g as IGraph;
			if (dg != null)
				return dg.vertex_out_edge_count(v);
			return ((IGraphExImpl)g).Verticies[v].OutEdges.Count;
		}

		public static bool edge_is_coref_and_master(this IGraph g, Eref e)
		{
			return ((dg)g).edge_is_coref_and_master(e);
		}
		public static IEnumerable<Eref[]> edge_paths(this IGraph g, Eref e)
		{
			return ((dg)g).edge_paths(e);
		}
		public static IEnumerable<String> path_strings(this IGraph g, Eref e)
		{
			if (!g.edge_is_valid(e))
				yield break;
			foreach (var p in g.edge_paths(e))
				yield return p.SelectDistinct(_er => g.edge_string(_er)).StringJoin(".");
		}

		public static Vref AddVertex(this IGraph g, int value)
		{
			var rwg = g as IEditGraph;
			if (rwg == null)
				throw new Exception();
			return rwg.AddVertex(value);
		}
		public static Eref CreateEdge(this IGraph g, Vref v_from, Vref v_to, int value)
		{
			var rwg = g as IEditGraph;
			if (rwg == null)
				throw new Exception();
			return rwg.CreateEdge(v_from, v_to, value);
		}
		public static void DeleteEdge(this IGraph g, Eref er)
		{
			var rwg = g as IEditGraph;
			if (rwg == null)
				throw new Exception();
			rwg.DeleteEdge(er);
		}
		public static void DeleteAllInEdges(this IGraph g, Vref vr)
		{
			var rwg = g as IEditGraph;
			if (rwg == null)
				throw new Exception();
			rwg.DeleteAllInEdges(vr);
		}
		public static void DeleteVertex(this IGraph g, Vref vr)
		{
			var rwg = g as IEditGraph;
			if (rwg == null)
				throw new Exception();
			rwg.DeleteVertex(vr);
		}

		public static void check(this IGraph g)
		{
#if DEBUG
			new check_helper((dg)g);
#endif
		}

		public static bool IsRoot(this IVertexEx v)
		{
			return v.InEdges.Count == 0;
		}
		public static bool IsLeaf(this IVertexEx v)
		{
			return v.OutEdges.Count == 0;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public class GraphException : Exception
	{
		public GraphException(String fmt, params Object[] args)
			: base(String.Format(fmt, args))
		{
		}
	};
}