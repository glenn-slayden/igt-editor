//#define CHECK

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	using Array = System.Array;
	using Enumerable = System.Linq.Enumerable;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class rw_graph : dg, IEditGraph
	{
		//static IReadOnlyList<Edge> promoteE(IGraph g)
		//{
		//	var dg = g as dg;
		//	if (dg != null)
		//		return dg.E;
		//	return Enumerable.ToArray(g._E_Raw);
		//}
		//static IReadOnlyList<Vertex> promoteV(IGraph g)
		//{
		//	var dg = g as dg;
		//	if (dg != null)
		//		return dg.V;
		//	return Enumerable.ToArray(g._V_Raw);
		//}

		public rw_graph(IReadOnlyList<Edge> E, IReadOnlyList<Vertex> V, IGraph source_graph)
			: base(E, V, source_graph)
		{
			this.c_edge_max = base.E.Length;
			this.c_vertex_max = base.V.Length;
			create_freelists();
		}

		public rw_graph()
			: this(default(Edge[]), default(Vertex[]), default(IGraph))
		{
		}

		/// <summary>
		/// note: now always copying the input arrays
		/// </summary>
		public rw_graph(IGraphRaw to_copy)
			: this(to_copy._E_Raw.ToArray(), to_copy._V_Raw.ToArray(), to_copy)
		{
			var rwg = to_copy as rw_graph;
			if (rwg != null)
			{
				this.c_edge_max = rwg.c_edge_max;
				this.c_edge_free = rwg.c_edge_free;
				this.c_vertex_max = rwg.c_vertex_max;
				this.c_vertex_free = rwg.c_vertex_free;

				this.vertex_freelist = rwg.vertex_freelist;
				this.edge_freelists = Enumerable.ToArray(rwg.edge_freelists);
				this.c_blocks_free = rwg.c_blocks_free;

				check();
			}
		}

		///////////////////////////////////////////////////////
		///
		protected int c_vertex_max;
		public int c_vertex_free;
		protected int c_edge_max;
		public int c_edge_free;

		public int VertexAlloc { [DebuggerStepThrough] get { return c_vertex_max; } }
		public override int VertexCount { [DebuggerStepThrough] get { return c_vertex_max - c_vertex_free; } }
		public int EdgeAlloc { [DebuggerStepThrough] get { return c_edge_max; } }
		public override int EdgeCount { [DebuggerStepThrough] get { return c_edge_max - c_edge_free; } }
		///
		///////////////////////////////////////////////////////

		public enum GraphEditOp
		{
			AddedVertex,
			AddedEdge,
			DeletedEdge,
			DeletedAllEdges,
			DeletedVertex,
			ClearedGraph,
			Unknown,
		};
		protected void RaiseEvent(GraphEditOp op, int data = default(int))
		{
			base.version++;
			Debug.Write(version + " ");

			check();

			var _tmp = GraphChangedEvent;
			if (_tmp != null)
				_tmp(op, data);
		}
		public event Action<GraphEditOp, int> GraphChangedEvent;


		///////////////////////////////////////////////////////
		///
		public Vref AddVertex(int value)
		{
			Vref vr;
			if (c_vertex_free > 0)
			{
				Debug.Assert(vertex_freelist >= 0 && vertex_freelist < VertexAlloc);
				vr = new Vref(vertex_freelist);
				c_vertex_free--;
				vertex_freelist = V[vr].nxt;
			}
			else
			{
				vr = new Vref(c_vertex_max);
				EnsureArray(ref V, ref c_vertex_max, 1);
			}

			V[vr] = new Vertex
			{
				e_in = Eref.None,
				e_out = Eref.NotValid,
				c_out = 0,
				value = value,
			};

			RaiseEvent(GraphEditOp.AddedVertex, vr);
			return vr;
		}
		///
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		///
		public void DeleteVertex(Vref vr)
		{
			if (_vertex_freelist_contains(vr))
				throw new Exception("The vertex is already free");

			if ((uint)vr >= (uint)VertexAlloc)
				throw new GraphException("The vertex is not valid");

			delete_vertex(vr, ref V[vr]);

			RaiseEvent(GraphEditOp.DeletedVertex, vr);
		}
		///
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		///
		public Eref CreateEdge(Vref v_from, Vref v_to, int value)
		{
			if (!vertex_is_valid(v_from))
				throw new GraphException("The 'from' vertex is not valid");

			if (!vertex_is_valid(v_to))
				throw new GraphException("The 'to' vertex is not valid");

			if (v_from == v_to)
				throw new GraphException("Cannot add self-edge to the graph");

			if (vertex_has_ancestor(v_to, v_from))
				throw new GraphException("Cannot create a cycle in the graph");

			Edge enew = new Edge
			{
				v_from = v_from,
				v_to = v_to,
				status = status.NotValid,
				value = value
			};

			Eref er;
			if ((er = try_reuse_edge(ref enew, ref V[v_from])) < 0)
				er = add_new_edge(ref enew, ref V[v_from]);

			RaiseEvent(GraphEditOp.AddedEdge, er);
			return er;
		}
		///
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		///
		public void DeleteEdge(Eref er)
		{
			if (er < 0 || er >= EdgeAlloc)
				throw new GraphException("The edge is not valid");
			if (E[er].IsFree)
				throw new GraphException("The edge is already free");

			remove_edge(er, ref E[er]);

			RaiseEvent(GraphEditOp.DeletedEdge, er);
		}
		///
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		///
		public void DeleteAllInEdges(Vref vr)
		{
			if (!vertex_is_valid(vr))
				throw new GraphException("The vertex is not valid");

			Eref er = vertex_unary_parent(vr);
			if (er >= 0)
			{
				remove_edge(er, ref E[er]);
			}
			else
			{
				var earr = vertex_in_edges(vr);
				Debug.Assert(earr.Length > 1 && earr.IsDistinct());

				Array.Sort(earr);

				for (int i = earr.Length - 1; i >= 0; i--)
				{
					var e = earr[i];
					remove_edge(e, ref E[e]);
				}
			}

			RaiseEvent(GraphEditOp.DeletedAllEdges, vr);
		}
		///
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////
		///
		public void ClearGraph()
		{
			create_freelists();

			RaiseEvent(GraphEditOp.ClearedGraph);
		}
		///
		///////////////////////////////////////////////////////


		public override String ToString()
		{
			return String.Format("[V: count.{0} alloc.{1} free.{2}]  [E: count.{3} alloc.{4} free.{5} blks.{6}]",
				VertexCount,
				VertexAlloc,
				c_vertex_free,
				EdgeCount,
				EdgeAlloc,
				c_edge_free,
				c_blocks_free);
		}
	};
}
