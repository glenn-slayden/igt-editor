using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
#if DEBUG
	using String = System.String;

	public struct check_helper
	{
		public check_helper(dg graph)
		{
			this.graph = graph;
			this.peer_edge_count = 0;
			this.v_in_counts = new int[graph._V_Raw.Count];

			if (graph is rw_graph)
				check_rw((rw_graph)graph);
			else
				check_ro();

			int ccc = v_in_counts.Sum();

			if (peer_edge_count != graph.EdgeCount)
				throw new Exception(String.Format("peer list error: {0} (expecting {1})", peer_edge_count, graph.EdgeCount));

			//Debug.Print("Peer count: {0}  EdgeCount: {1}", peer_edge_count, rwg.EdgeCount);

			//Debug.Print("{0}-{1:X8}  e:{2}  v:{3} check ok", graph.GetType().Name, graph.GetHashCode(), graph.EdgeCount, graph.VertexCount);
		}

		readonly dg graph;
		dg.Edge[] E { get { return (dg.Edge[])graph._E_Raw; } }
		dg.Vertex[] V { get { return (dg.Vertex[])graph._V_Raw; } }
		int[] v_in_counts;
		int peer_edge_count;

		void check_ro()
		{
			_check_verticies();

			_check_edges();
		}

		void check_rw(rw_graph graph)
		{
			int ckv, cke;
			if ((ckv = _check_rw_verticies(graph)) != graph.c_vertex_free)
				throw new Exception();

			if ((cke = _check_rw_edges(graph)) != graph.c_edge_free)
				throw new Exception();
		}

		void _check_verticies()
		{
			int _cva = graph.VertexAlloc();
			for (Vref vr = new Vref(0); vr < _cva; vr++)
				check_vertex(vr, ref V[vr]);
		}

		int _check_rw_verticies(rw_graph graph)
		{
			int _cva = graph.VertexAlloc, fv = 0;
			bool b;
			for (Vref vr = new Vref(0); vr < _cva; vr++)
			{
				if ((b = graph._vertex_freelist_contains(vr)) != (V[vr].c_out < 0))
					throw new Exception("vertex free state is inconsitent");
				else if (!b)
					check_vertex(vr, ref V[vr]);
				else
					fv++;
			}
			return fv;
		}

		void _check_edges()
		{
			int _cea = graph.EdgeAlloc();
			for (Eref er = Eref.Zero; er < _cea; er++)
				check_edge(er, ref E[er]);
		}

		int _check_rw_edges(rw_graph graph)
		{
			int _cea = graph.EdgeAlloc, fe = 0;
			for (Eref er = Eref.Zero; er < _cea; er++)
			{
				if (E[er].IsFree)
				{
					if (E[er].HasSize)
					{
						int q = graph.find_edge_freelist(er);
						if (q == -1)
							throw new Exception("edge free state is inconsitent");
					}
					fe++;
				}
				else
					check_edge(er, ref E[er]);
			}
			return fe;
		}

		void check_vertex(Vref vr, ref dg.Vertex v)
		{
			if (v.e_in == Eref.None)
			{
				/// top o.k.
			}
			else if (!graph.edge_is_valid(v.e_in))
				throw new Exception("in-edge must be valid and not free");
			else
			{
				if (graph.edge_target(v.e_in) != vr)
					throw new Exception("in-edge must target this vertex");
				/// ...
			}

			if (v.c_out < 0)
				throw new Exception("out-edge count cannot be negative");
			else if (v.c_out > 0)
			{
				if (v.e_out < 0)
					throw new Exception("out-edge pointer must be non-negative");

				var er_last = v.e_out + v.c_out;
				for (var er = v.e_out; er < er_last; er++)
				{
					if (E[er].IsFree)
						throw new Exception("out-edge is free");
					if (E[er].v_from != vr)
						throw new Exception("out-edge v_from does not refer to parent vertex");
					if (!graph.edge_is_valid(E[er].e_next))
						throw new Exception();
				}
			}
			else if (v.e_out != Eref.NotValid)
				throw new Exception("out-edge pointer must be Eref.NotValid if there are no out-edges");
		}

		void check_edge(Eref er, ref dg.Edge e)
		{
			if (e.IsFree)
				throw new Exception("appears free but not marked in freelist");

			if (!graph.vertex_is_valid(e.v_from))
				throw new Exception("'from' vertex must be valid and not free");

			if (!graph.vertex_is_valid(e.v_to))
				throw new Exception("'to' vertex must be valid and not free");

			check_edge_target(er, ref e);
		}

		void check_edge_target(Eref er, ref dg.Edge e)
		{
			Vref v_target = e.v_to;

			if (e.v_from == v_target)
				throw new Exception("edge cannot connect to itself");

			if (V[v_target].IsRoot)
				throw new Exception("edge cannot target a 'top' vertex");

			bool s1 = er == V[v_target].e_in;
			bool s2 = graph.edge_is_non_coref_or_master(er);
			if (s1 != s2)
				throw new Exception("inconsistent edge master status");

			int c = check_edge_next(er);

			if (s2)
				peer_edge_count += c;

			v_in_counts[v_target]++;
		}

		int check_edge_next(Eref er)
		{
			Eref e_cur = E[er].e_next;

			if (!graph.edge_is_valid(e_cur))
				throw new Exception("'e_next' must be valid ");

			Vref v_target = E[er].v_to;

			int c = 1;
			while (e_cur != er)
			{
				if (e_cur == Eref.NotValid)
					throw new Exception("invalid edge in peer list");

				if (e_cur < 0)
					throw new Exception("unknown edge in peer list");

				if (E[e_cur].v_to != v_target)
					throw new Exception("peer edges don't point to same target");

				e_cur = E[e_cur].e_next;
				if (++c > 200)
					throw new Exception("probable cycle in peer list");
			}

			if (c > 1 && !graph.edge_target_coreferenced(er))
				throw new Exception("wrong coref status");

			if (c != graph.vertex_in_edges(v_target).Length)
				throw new Exception("incorrect master in-edge count");

			return c;
		}
	};
#endif
}