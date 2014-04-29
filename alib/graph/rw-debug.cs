using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Enumerable;

namespace alib.dg
{
#if DEBUG
	using debug;
#endif
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class rw_graph
	{
		[Conditional("DEBUG")]
		void dbg_initialize_allocation(int ix, int c)
		{
			for (int i = 0; i < c; i++, ix++)
				E[ix].reset(status.NotValid);
		}

#if !DEBUG
		[Conditional("DEBUG")]
		public void check() { }
#else
		public void check()
		{
			try
			{
				if (VertexAlloc - c_vertex_free != VertexCount)
					throw new Exception("AA");
				if (c_vertex_free != check_vertex_freelist())
					throw new Exception("CC");

				if (EdgeAlloc - c_edge_free != EdgeCount)
					throw new Exception("BB");
				int c_blks;
				if (c_edge_free != check_edge_freelist(out c_blks))
					throw new Exception("DD");
				if (c_blocks_free != c_blks)
					throw new Exception("EE");

				new check_helper(this);

				for (Eref i = Eref.Zero; i < EdgeAlloc; i++)
				{
					var eed = new eent_dbg(this, i);
#if false
					var _x5 = eed.ProxyOk;
					var _x1 = eed._e_next;
					var _x2 = eed._v_from;
					var _x3 = eed._v_to;
					var _x4 = eed._value;
					var _x6 = eed.z10_coref;
					var _x7 = eed.z20_parent;
					var _x10 = eed.ToString();
#endif
				}
				for (Vref i = Vref.Zero; i < VertexAlloc; i++)
				{
					var vvd = new vent_dbg(this, i);
#if false
					var _x1 = vvd.ProxyOk;
					var _x2 = vvd._e_in;
					var _x3 = vvd._e_out;
					var _x4 = vvd._c_out;
					var _x5 = vvd._value;
					var _x6 = vvd.z10_up;
					var _x7 = vvd.z11_down;
					var _x8 = vvd.z30_paths;
					var _x9 = vvd.get_below();
					var _x13 = vvd.ToString();
#endif
				}
			}
			catch (Exception ex)
			{
				Debug.Print(ex.StackTrace.ToString());
			}
		}

		[DebuggerStepThrough]
		/// <summary> works with free list entries only </summary>
		int _discover_self_ix_via_freelist(int prv)
		{
			int nxt = -1;
			if (prv < 0 && ~prv < edge_freelists.Length)
				nxt = edge_freelists[~prv];
			else if (prv >= 0 && prv < EdgeAlloc)
				nxt = E[prv].nxt;
			return nxt >= 0 && nxt < EdgeAlloc && E[nxt].IsFree && E[nxt].prv == prv ?
				nxt : (int)status.NotValid;
		}

		int check_vertex_freelist()
		{
			int ix = vertex_freelist, c = 0;
			while (ix != (int)status.FreelistTerm)
			{
				ix = ((int_λVertex)((ref Vertex x) =>
				{
					if (x.status != status.free || x.prv != (int)status.NotValid || x.value != 0)
						throw new Exception();
					return x.nxt;
				}))(ref V[ix]);
				if (c++ > c_vertex_free)
					throw new Exception();
			}
			return c;
		}

		int check_edge_freelist(out int c_blks)
		{
			int tot = 0;
			c_blks = 0;
			for (int block_size = 0; block_size < edge_freelists.Length; block_size++)
			{
				int _tmp;
				tot += check_edge_freelist(block_size, out _tmp);
				c_blks += _tmp;
			}
			return tot;
		}

		int check_edge_freelist(int list_num, out int c_blks)
		{
			int i = edge_freelists[list_num], c_rows = 0;
			c_blks = 0;
			while (i != (int)status.FreelistTerm)
			{
				int cblk = E[i].block_size;

				if (list_num > 0 && cblk != list_num)
					throw new Exception();

				if (cblk <= 0)
					throw new Exception();
				else if (cblk == 1)
				{
					if (E[i].status != status.fe_BlockSingleton)
						throw new Exception();
				}
				else
				{
					int i_end = get_block_end(i);
					for (int j = i; j <= i_end; j++)
					{
						if (j == i)
						{
							if (E[j].status != status.fe_BlockStart)
								throw new Exception();
							if (E[j].block_size != cblk)
								throw new Exception();
							if (E[j].prv == (int)status.NotInFreelist)
								throw new Exception();
							if (E[j].nxt == (int)status.NotInFreelist)
								throw new Exception();
						}
						else
						{
							if (j == i_end)
							{
								if (E[j].status != status.fe_BlockEnd)
									throw new Exception();
								if (E[j].block_size != cblk)
									throw new Exception();
							}
							else
							{
								if (E[j].status != status.free)
									throw new Exception();
								if (E[j].block_size != 0)
									throw new Exception();
							}

							if (E[j].prv != (int)status.NotInFreelist)
								throw new Exception();
							if (E[j].nxt != (int)status.NotInFreelist)
								throw new Exception();
						}
					}
				}

				c_rows += cblk;
				i = E[i].nxt;
				c_blks++;
			}
			return c_rows;
		}

		public IEnumerable<Eref> freelist_entries(int block_size)
		{
			int i = edge_freelists[block_size];
			while (i != (int)status.FreelistTerm)
			{
				yield return new Eref(i);
				i = E[i].nxt;
			}
		}
#endif
	};


#if DEBUG
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class debug_freelist_entry
	{
		public debug_freelist_entry(rw_graph g, int block_size)
		{
			this.g = g;
			this.block_size = block_size;
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly rw_graph g;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly int block_size;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public eent_dbg[] _entries
		{
			get
			{
				var arr = g.freelist_entries(block_size).ToArray();
				eent_dbg[] ret = new eent_dbg[arr.Length];
				for (int i = 0; i < arr.Length; i++)
					ret[i] = new eent_dbg(g, arr[i], 0);
				return ret;
			}
		}

		public override String ToString()
		{
			int c = g.freelist_entries(block_size)._Count();
			return c == 0 ? "" : c.ToString();
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class rw_string_graph
	{
		[DebuggerStepThrough]
		public static bool moniker(Vref vr, out String s)
		{
			if (vr == Vref.NotValid)
				s = "NotValid";
			else if (vr == (int)status.NotInFreelist)
				s = "NotInFreelist";
			else if (vr < 0)
				s = "(vref < 0)";
			else
			{
				s = "V" + ((int)vr);
				return false;
			}
			return true;
		}
		[DebuggerStepThrough]
		public static bool moniker(Eref er, out String s)
		{
			status es = (status)er;
			if (es == status.NotValid)
				s = "NotValid";
			else if (es == status.fe_BlockSingleton)
				s = "sing";
			else if (es == status.fe_BlockStart)
				s = "start";
			else if (es == status.fe_BlockEnd)
				s = "end";
			else if (er == Eref.None)
				s = "Eref.None";
			else if (er < 0)
				s = "(eref < 0)";
			else
			{
				//var g = dg._singleton;
				//s = String.Format("Eref error: v_from:{0} v_to:{1} e_next:{2} value{3}",
				//	(int)g.E[er].e_next,
				//	(int)g.E[er].v_from,
				//	(int)g.E[er].v_to,
				//	(int)g.E[er].value);

				s = "E" + ((int)er);
				return false;
			}
			return true;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		eent_dbg[] edbg;
		//[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		[DebuggerDisplay("Count: {EdgeCount}/{EdgeAlloc} free: {c_edge_free}", Name = "=== EDGES")]
		public eent_dbg[] z01_EDGES
		{
			[DebuggerStepThrough]
			get
			{
				int c = EdgeAlloc;
				if (edbg == null || edbg.Length != c)
				{
					edbg = new eent_dbg[c];
					for (Eref i = Eref.Zero; i < c; i++)
						edbg[i] = new eent_dbg(this, i);
				}
				return edbg;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		vent_dbg[] vdbg;
		//[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		[DebuggerDisplay("Count: {VertexCount}/{VertexAlloc} free: {c_vertex_free}", Name = "=== VERTICIES")]
		public vent_dbg[] z02_VERTICIES
		{
			[DebuggerStepThrough]
			get
			{
				int c = VertexAlloc;
				if (vdbg == null || vdbg.Length != c)
				{
					vdbg = new vent_dbg[c];
					for (Vref i = Vref.Zero; i < c; i++)
						vdbg[i] = new vent_dbg(this, i);
				}
				return vdbg;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		debug_freelist_entry[] fldbg;
		[DebuggerDisplay("", Name = "=== EDGE FREELISTS")]
		public debug_freelist_entry[] z03_FREELISTS
		{
			[DebuggerStepThrough]
			get
			{
				if (fldbg == null)
				{
					fldbg = new debug_freelist_entry[EdgeFreelistCount];
					for (int i = 0; i < EdgeFreelistCount; i++)
						fldbg[i] = new debug_freelist_entry(this, i);
				}
				return fldbg;
			}
		}
	};
#endif
}
