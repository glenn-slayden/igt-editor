using System;
using System.Diagnostics;
using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class rw_graph
	{
		public const int EdgeFreelistCount = 64;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int[] edge_freelists;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int vertex_freelist, c_blocks_free;

		void create_freelists()
		{
			this.edge_freelists = new int[EdgeFreelistCount];
			for (int i = 0; i < EdgeFreelistCount; i++)
				edge_freelists[i] = (int)status.FreelistTerm;
			this.c_edge_free = 0;
			this.c_blocks_free = 0;

			this.vertex_freelist = (int)status.FreelistTerm;
			this.c_vertex_free = 0;
		}

		public bool _vertex_freelist_contains(Vref vr)
		{
			if ((uint)vr < (uint)VertexAlloc)
			{
				int i = vertex_freelist;
				while (i != (int)status.FreelistTerm)
				{
					if (vr == i)
						return true;
					i = V[i].nxt;
				}
			}
			return false;
		}

		public bool free_block_contains(int b, int seek)
		{
			if (b == seek)
				return true;
			if (b < 0 || b > seek || seek >= EdgeAlloc || !E[b].IsFree)
				return false;
			if (!E[b].IsBlockStart)
				throw new Exception();
			return seek < b + E[b].block_size;
		}

		public int find_edge_freelist(int ix_edge)
		{
			int k = get_edge_freelist(ix_edge);
			int i = edge_freelists[k];
			while (i != (int)status.FreelistTerm)
			{
				if (free_block_contains(i, ix_edge))
					return 0;
				i = E[i].nxt;
			}
			return -1;
		}

		[DebuggerStepThrough]
		bool is_freelist_entry(int ix_edge)
		{
			return (uint)ix_edge < (uint)EdgeAlloc && E[ix_edge].IsFree;
		}

		int get_edge_freelist(int ix_edge)
		{
			if ((uint)ix_edge >= (uint)EdgeAlloc || !E[ix_edge].HasSize)
				throw new Exception();
			int c = E[ix_edge].block_size;
			if (c <= 0)
				throw new Exception();
			return c < EdgeFreelistCount ? c : 0;
		}

		public int get_block_start(int ix_edge)
		{
			return ((int_λEdge)((int ix, ref Edge e) =>
			{
				Debug.Assert(e.HasSize);
				if (e.IsBlockStart)
					return ix;
				if (e.IsBlockEnd)
					return ix - (e.block_size - 1);
				throw new Exception();
			}))(ix_edge, ref E[ix_edge]);
		}

		public int get_block_end(int ix_edge)
		{
			return ((int_λEdge)((int ix, ref Edge e) =>
			{
				Debug.Assert(e.HasSize);
				if (e.IsBlockEnd)
					return ix;
				if (e.IsBlockStart)
					return ix + (e.block_size - 1);
				throw new Exception();
			}))(ix_edge, ref E[ix_edge]);
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		Eref try_freelist_alloc(int c)
		{
			Debug.Assert(c > 0);

			int ix;
			if (c <= c_edge_free)
			{
				for (int blk_size = c; blk_size < EdgeFreelistCount; blk_size++)
					if ((ix = edge_freelists[blk_size]) != (int)status.FreelistTerm)
						goto found_suitable;

				ix = edge_freelists[0];
				while (ix != (int)status.FreelistTerm)
					if (E[ix].block_size >= c)
						goto found_suitable;
					else
						ix = E[ix].nxt;
			}
			return Eref.NotValid;

		found_suitable:
			reclaim_freelist_edges(ix, ref E[ix], c);
			return new Eref(ix);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>'e' must be validly linked in the list and its link values are never changed</summary>
		/// <param name="e">valid freelist singleton or block</param>
		/// <param name="prv">new rightwards value for the leftwards link</param>
		/// <param name="nxt">new leftwards value for the rightwards link</param>
		void update_free_links(ref Edge e, int prv, int nxt)
		{
			Debug.Assert(e.HasList);

			if (e.nxt != (int)status.FreelistTerm)
				E[e.nxt].prv = prv;

			if (e.prv < 0)
				edge_freelists[~e.prv] = nxt;
			else
				E[e.prv].nxt = nxt;
		}

		[DebuggerStepThrough]
		void unlink_free_edge(ref Edge e)
		{
			update_free_links(ref e, e.prv, e.nxt);
			e.prv = e.nxt = (int)status.NotInFreelist;
		}

		[DebuggerStepThrough]
		void relink_free_edge(int ix) { _rfe(ix, ref E[ix]); }
		void _rfe(int ix, ref Edge e)
		{
			int size = e.block_size;
			if (size >= EdgeFreelistCount)
				size = 0;
			e.prv = ~size;
			e.nxt = edge_freelists[size];

			update_free_links(ref e, ix, ix);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void increase_block_size(int ix, ref Edge e, int c_add)
		{
			unlink_free_edge(ref e);

			int c = e.block_size;

			if (c_add == 1)
				E[ix + c].reset(status.fe_BlockEnd);
			else if (c_add > 1)
				E[ix + c].reset(status.free);

			if (c == 1)
				e.status = status.fe_BlockStart;
			else
				E[ix + (c - 1)].reset(status.free);

			c += c_add;

			e.block_size = E[ix + (c - 1)].block_size = c;

			relink_free_edge(ix);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void reclaim_freelist_edges(int ix, ref Edge e, int c)
		{
			Debug.Assert(e.IsBlockStart && e.block_size >= c);

			unlink_free_edge(ref e);

			int c_new = e.block_size - c;
			if (c_new == 0)
			{
				c_blocks_free--;
			}
			else
			{
				if (c_new == 1)
				{
					E[ix + c].reset(status.fe_BlockSingleton, 1);
				}
				else
				{
					e.block_size = E[get_block_end(ix)].block_size = c_new;

					E[ix + c] = e;
				}
				relink_free_edge(ix + c);
			}
			c_edge_free -= c;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[DebuggerStepThrough]
		void extend_right_fb_to_left(int ixL, int ixR)
		{
			Debug.Assert(0 <= ixL && ixL + 1 == ixR && ixR < EdgeAlloc);
			_erl(ixL, ref E[ixL], ref E[ixR]);
		}
		void _erl(int ixL, ref Edge L, ref Edge R)
		{
			Debug.Assert(!L.IsFree && R.IsBlockStart && R.block_size > 0);

			int c = (L = R).block_size;

			L.block_size = 1;

			increase_block_size(ixL, ref L, c);

			c_edge_free++;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void extend_left_fb_to_right(int ixL, int ixR)
		{
			Debug.Assert(ixL < ixR && get_block_end(ixL) + 1 == ixR);
			Debug.Assert(E[ixL].IsBlockStart && E[ixL].block_size > 0);

			increase_block_size(ixL, ref E[ixL], 1);

			c_edge_free++;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void join_free_blocks(int ixL, int ixR)
		{
			Debug.Assert(ixL < ixR && get_block_end(ixL) + 1 == ixR);
			Debug.Assert(E[ixL].IsBlockStart && E[ixL].block_size > 0);
			Debug.Assert(E[ixR].IsBlockStart && E[ixR].block_size > 0);

			int c_add = E[ixR].block_size;

			unlink_free_edge(ref E[ixR]);

			increase_block_size(ixL, ref E[ixL], c_add);

			c_blocks_free--;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void new_singleton_fb(int ix)
		{
			E[ix].reset(status.fe_BlockSingleton, 1);

			relink_free_edge(ix);

			c_edge_free++;
			c_blocks_free++;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void free_single_edge(int ix)
		{
			Debug.Assert(!is_freelist_entry(ix));

			int ixL = is_freelist_entry(ix - 1) ? get_block_start(ix - 1) : -1;

			if (is_freelist_entry(ix + 1))
			{
				extend_right_fb_to_left(ix, ix + 1);

				if (ixL >= 0)
					join_free_blocks(ixL, ix);
			}
			else if (ixL >= 0)
				extend_left_fb_to_right(ixL, ix);
			else
				new_singleton_fb(ix);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void free_edges(EdgeRange r)
		{
			Debug.Assert(!is_freelist_entry(r.ix));

			if (r.c == 1)
			{
				new_singleton_fb(r.ix);
			}
			else
			{
				int ix = r.Last;
				E[ix--].reset(status.fe_BlockEnd, r.c);

				while (ix > r.ix)
					E[ix--].reset(status.free);

				E[ix].reset(status.fe_BlockStart, r.c);
				relink_free_edge(ix);

				c_edge_free += r.c;
				c_blocks_free++;
			}

			if (is_freelist_entry(r.Next))
				join_free_blocks(r.ix, r.Next);

			if (is_freelist_entry(r.PrevEnd))
				join_free_blocks(get_block_start(r.PrevEnd), r.ix);
		}
	};
}
