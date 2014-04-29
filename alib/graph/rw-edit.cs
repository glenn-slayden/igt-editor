using System;
using System.Diagnostics;

using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	using Array = System.Array;

	public partial class rw_graph
	{
		struct EdgeRangeDelta
		{
			public EdgeRangeDelta(EdgeRange _src, int delta)
			{
				this._src = _src;
				this.delta = delta;
			}
			public EdgeRange _src;
			public int delta;

			public Eref shift_check(ref Eref _nxt, out Eref _old)
			{
				if (_src.Contains(_old = _nxt))
					_nxt += delta;
				return _nxt;
			}
		};

		static void EnsureArray<T>(ref T[] arr, ref int _max, int req)
			where T : struct
		{
			int _new = _max + req, _cap = arr.Length;
			if (_new > _cap)
			{
				_cap <<= 1;
				if (_cap < _new)
					_cap = _new;

				//Debug.Print("realloc {0} table, capacity {1} -> {2}",
				//	arr.GetType().GetElementType().Name,
				//	arr.Length,
				//	_cap);

				var _newarr = new T[_cap];
				if (_max > 0)
					Array.Copy(arr, 0, _newarr, 0, _max);
				arr = _newarr;
			}
			_max = _new;
		}

		delegate void λEdge(int ix, ref Edge e);
		delegate void λVertex(ref Vertex v);
		delegate int int_λVertex(ref Vertex v);
		delegate int int_λEdge(int ix, ref Edge e);

		void delete_vertex(Vref vr, ref Vertex v)
		{
			if (v.c_out > 0 || v.e_in != Eref.None)
				throw new Exception("Remove all edges before deleting a vertex");

			v.value = 0;
			v.status = status.free;
			v.prv = Vref.NotValid;
			v.nxt = vertex_freelist;

			vertex_freelist = vr;
			c_vertex_free++;
		}

		Eref alloc_edges(int c)
		{
			Debug.Assert(c > 0);

			Eref er;
			if ((er = try_freelist_alloc(c)) < 0)
			{
				er = new Eref(c_edge_max);
				EnsureArray(ref E, ref c_edge_max, c);
			}
#if TIDY_ALLOC
			dbg_initialize_allocation(er, c);
#endif
			return er;
		}

		Eref try_reuse_edge(ref Edge _new, ref Vertex vfrom)
		{
			Eref ix = vfrom.e_out + vfrom.c_out;

			if (!is_freelist_entry(ix))
				return Eref.NotValid;

			reclaim_freelist_edges(ix, ref E[ix], 1);

			vfrom.c_out++;

			_new.e_next = ix;
			E[ix] = _new;
			insert_peer_edge(ref E[ix].e_next, ref V[_new.v_to].e_in);

			return ix;
		}

		Eref add_new_edge(ref Edge _new, ref Vertex vfrom)
		{
			var rsrc = vfrom.OutEdgeRange;
			var edst = vfrom.e_out = alloc_edges(vfrom.c_out = rsrc.c + 1);

			Eref enew = _new.e_next = edst + rsrc.c;
			E[enew] = _new;

			insert_peer_edge(ref E[enew].e_next, ref V[_new.v_to].e_in);

			if (rsrc.c > 0)
			{
				shift_out_edges(rsrc, edst);

				free_edges(rsrc);
			}
			return enew;
		}

		/// new edge peer loop status: if it is the first (only) edge for
		/// the vertex, then it is the master for the target vertex and that
		/// vertex's 'e_in' value is the post-shift (updated) value. But
		/// even though this would make the vertex 'seen,' it doesn't need
		/// to be listed as 'seen' because it is impossible for another visit to
		/// be attempted. if it there are additional edges for the vertex, the
		/// vertex's master edge is not changed, and it will have a valid loop
		/// which may need updating if it has any duplicate-source edges so
		/// it should not be marked 'seen'
		///
		/// note: _new_nxt must be valid, which likely means a self-loop
		void insert_peer_edge(ref Eref _new_nxt, ref Eref _vto_ein)
		{
			insert_peer_edge(this.E, ref _new_nxt, ref _vto_ein);
		}
		public static void insert_peer_edge(Edge[] E, ref Eref _new_nxt, ref Eref _vto_ein)
		{
			if (_vto_ein < 0)
				_vto_ein = _new_nxt;
			else
				_new_nxt = swap(_new_nxt, ref E[_vto_ein].e_next);
		}
		static Eref swap(Eref _new_nxt, ref Eref _ein_nxt)
		{
			var _tmp = _ein_nxt;
			_ein_nxt = _new_nxt;
			return _tmp;
		}

		/// perilous: it is not just a matter of directly and independently updating each out-edge's 
		/// presence in its peer loop as the out-group is being relocated, because multiple edges 
		/// between the same two verticies are also peers to each other. instead, for all loops of 
		/// each targeted vertex, update all edge index values which are 'out of range' by modifying 
		/// their loop links, while at the same time traversing that loop (via the old values). the 
		/// loop may become temporarily corrupt in our wake, so be sure to use an updated value to 
		/// detect termination. furthermore, the 'out of range' method is insufficient when the 
		/// source and target ranges of the overlap, unless each we make sure never to update each
		/// target vertex only once

		unsafe void shift_out_edges(EdgeRange rsrc, Eref edst)
		{
			Array.Copy(E, rsrc.ix, E, edst, rsrc.c);

			var erd = new EdgeRangeDelta(rsrc, edst - rsrc.ix);

			Vref* seen = stackalloc Vref[rsrc.c];
			int c_seen = 0;

			Eref e;
			for (e = edst; e < edst + rsrc.c; e++)
			{
				Vref vt = E[e].v_to;
				for (int j = 0; j < c_seen; j++)
					if (seen[j] == vt)
						goto already_done;

				seen[c_seen++] = vt;
				update_peer_loop(erd, ref V[vt].e_in);

			already_done:
				;
			}
		}

		void update_peer_loop(EdgeRangeDelta erd, ref Eref _vtx_ein)
		{
			Eref _start, _old, _cur = erd.shift_check(ref _vtx_ein, out _start);
			do
				_cur = erd.shift_check(ref E[_cur].e_next, out _old);
			while (_start != _old);
		}

		void remove_edge(Eref er, ref Edge e)
		{
			if (e.e_next == er)
				V[e.v_to].e_in = Eref.None;
			else
				adjust_peer_list(ref V[e.v_to].e_in, er, e.e_next);

			remove_out_edge(ref V[e.v_from], er);
		}

		void adjust_peer_list(ref Eref _vto_ein, Eref _del, Eref _del_nxt)
		{
			if (_vto_ein == _del)
				_vto_ein = _del_nxt;

			E[edge_peer_list_prev(_del)].e_next = _del_nxt;
		}

		void remove_out_edge(ref Vertex vfrom, Eref e_dst)
		{
			Debug.Assert(vfrom.c_out > 0);

			int c = --vfrom.c_out;
			if (c == 0)
				vfrom.e_out = Eref.NotValid;
			else
			{
				var rsrc = new EdgeRange(e_dst + 1, c - ((int)e_dst - (int)vfrom.e_out));

				shift_out_edges(rsrc, e_dst);

				e_dst = rsrc.Last;
			}
			free_single_edge(e_dst);
		}
	};
}
