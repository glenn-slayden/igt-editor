//#define IGN_BARR
//#define REPORT
#define PRIORITY_Q
#define ACTIVE_LEVEL_EQUATABLE
//#define AGGRESSIVE_CHECKING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Linq;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;
using alib.Hashing;
using alib.priority;

namespace alib.dg
{
	using Array = System.Array;
	using String = System.String;
	using Enumerable = System.Linq.Enumerable;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class dg_topographic : IGraphExImpl
	{
		public dg_topographic(IGraph to_sort)
		{
			Debug.Print("new topo");
			this.g = to_sort;
		}

		readonly IGraph g;
		public IGraphCommon SourceGraph { get { return g; } }

		int version;
		void check_version()
		{
			var dg = g as dg;
			if (dg != null && this.version != dg.version)
			{
				Debug.Print("invalidate");
				this.version = dg.version;
				vv = null;
				ee = null;
			}
		}

		public int VertexCount { get { return g.VertexCount; } }

		public int EdgeCount { get { return g.EdgeCount; } }

		IReadOnlyList<IEdgeEx> ee;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<IEdgeEx> Edges
		{
			get
			{
				check_version();

				IGraphExImpl gex;
				if (ee == null)
					ee = (gex = g as IGraphExImpl) != null ? gex.Edges : make_earr();
				return ee;
			}
		}

		EdgeEx[] make_earr()
		{
			var _ee = new EdgeEx[g.EdgeAlloc()];
			for (Eref er = Eref.Zero; er < _ee.Length; er++)
				if (g.edge_is_valid(er))
					_ee[er] = new EdgeEx(this, er);
			return _ee;
		}

		VertexEx[] vv;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IVertexEx> IGraphExImpl.Verticies { get { return this.Verticies; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<VertexEx> Verticies
		{
			get
			{
				check_version();

				if (vv == null)
				{
					vv = make_varr();
					recalculate_levels();
				}
				return vv;
			}
		}

		int c_levels;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int LevelCount { get { return c_levels; } }

		VertexEx[] make_varr()
		{
			Debug.Print("recalc levels");

			var _vv = new VertexEx[g.VertexAlloc()];
			for (Vref vr = Vref.Zero; vr < _vv.Length; vr++)
				if (g.vertex_is_valid(vr))
					_vv[vr] = new VertexEx(this, vr);
			return _vv;
		}

		int get_level(Vref vr)
		{
			int pl, lev;
			if ((lev = vv[vr].Row) == -1)
			{
				if (!g.vertex_is_root(vr))
					foreach (var f in g.vertex_parents(vr))
						if ((pl = get_level(f)) > lev)
							lev = pl;
				vv[vr].Row = ++lev;
				if (lev > c_levels)
					c_levels = lev;
			}
			return lev;
		}

		void recalculate_levels()
		{
			this.c_levels = -1;
			VertexEx vx;

			for (Vref vr = Vref.Zero; vr < vv.Length; vr++)
				if ((vx = vv[vr]) != null && vx.Row == -1 && g.vertex_is_leaf(vr))
					get_level(vr);

			c_levels++;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<IVertexEx> Roots
		{
			get
			{
				int c = g.VertexAlloc();
				var rg = Collection<VertexEx>.None;
				for (Vref v = Vref.Zero; v < c; v++)
					if (g.vertex_is_valid(v) && g.vertex_out_edge_count(v) == 0)
						arr.Append(ref rg, Verticies[v]);
				return rg;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<IVertexEx> Leaves { get { throw not.impl; } }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Main dag layout controller class
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class edge_proxy_graph : rw_graph
	{
		public edge_proxy_graph(IGraphRaw to_copy)
			: base(to_copy)
		{
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Main dag layout controller class
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public partial class layout_graph : rw_graph
	{
		public layout_graph(IGraphRaw to_copy)
			: base(to_copy)
		{
			this.max_iter = 10000;
		}


		public void DoLayout()
		{
#if AGGRESSIVE_CHECKING
			check();
#endif

			// data values in the layout_graph are Eref/Vref values mapping back to the source graph
			for (Vref v = Vref.Zero; v < VertexAlloc; v++)
				V[v].value = (int)v;

			for (Eref e = Eref.Zero; e < EdgeAlloc; e++)
				E[e].value = (int)e;

			//remove_monovertex_chains();

			do
				this.topo = new dg_topographic(this);
			while (insert_edge_proxies());

#if AGGRESSIVE_CHECKING
			check();
#endif

			this.levels = new level_infos(this, topo);

#if REPORT
			Debug.WriteLine(this.ToString());

			Debug.WriteLine(levels.report());
#endif

			this.layout_cur = find_layouts().First();

			//level_info.f_debug = true;
			//layout_cur = new logical_layout(layout_cur);
			//layout_cur.SetLogicalOrder(2, new[] { 4, 5, 6, 3, 1, 2, 0, 7 }, false);
			//layout_cur.levels[2].set_medians_from_above(layout_cur);

			//layout_cur.SetLogicalOrder(2, new[] { 4, 5, 1, 3, 0, 6, 2, 7 }, false);
			//layout_cur = new logical_layout(layout_cur);

			//layout_cur.levels[2].set_medians_from_below(layout_cur);
			//var _ = layout_cur.total_crossings();

			//_full_report(layout_cur);

			restore_ignored_verticies();

#if AGGRESSIVE_CHECKING
			check();
#endif
		}

		public dg_topographic topo;

		public level_infos levels;

		public logical_layout layout_cur;

		public int max_iter;

		/// <summary>
		/// warning:  this class must be careful to use 'base.' with edge_value() or vertex_value() internally 
		/// because it is trying to overload those remap to the data values from the source graph and we don't
		/// want to apply those mappings twice
		/// </summary>

		///
		public override String edge_string(Eref e)
		{
			var er_src = base.edge_value(e);
			if (er_src == -1)
				return String.Empty;
			else if (er_src < 0)
				throw new Exception();
			return source_graph.edge_string(new Eref(er_src));
		}

		public override String vertex_string(Vref v)
		{
			var vr_src = base.vertex_value(v);
			if (vr_src == -1)
				return String.Empty;
			else if (vr_src < 0)
				throw new Exception();
			return source_graph.vertex_string(new Vref(vr_src));
		}

		public override int edge_value(Eref e)
		{
			var er_src = base.edge_value(e);
			if (er_src == -1)
				return -1;
			else if (er_src < 0)
				throw new Exception();
			return source_graph.edge_value(new Eref(er_src));
		}
		public override int vertex_value(Vref v)
		{
			var vr_src = base.vertex_value(v);
			if (vr_src == -1)
				return -1;
			else if (vr_src < 0)
				throw new Exception();
			return source_graph.vertex_value(new Vref(vr_src));
		}

		public bool is_layout_proxy_vref(Vref vr) { return vertex_is_valid(vr) && base.vertex_value(vr) == -1; }

		//public bool is_layout_proxy_eref(Eref er) { return edge_is_valid(er) && base.edge_value(er) < 0; }

		public Vref add_dummy_vertex() { return AddVertex(-1); }

		public Eref create_dummy_edge(Vref v_from, Vref v_to) { return CreateEdge(v_from, v_to, -1); }

		public bool vertex_edges_can_cross(Vref vr)
		{
			var b = is_indexed_vref(vr) && vertex_in_edge_count(vr) + vertex_out_edge_count(vr) > 1;
			//Debug.Print("V{0,-3} {1}", (int)vr, is_indexed_vref(vr) ? b.ToString() : "not indexed");
			return b;
		}

		public bool is_indexed_vref(Vref vr)
		{
			return vertex_is_valid(vr) && !ignore_vertex(vr) && topo.Verticies[vr].Row >= 0;
		}


		/////////////////////////////////////////////////////////////
		/// 
#if IGN_BARR
		BitArray.BitArr _ign_vtx;
#endif
		public bool ignore_vertex(Vref vr)
		{
#if IGN_BARR
			return _ign_vtx != null && vr < _ign_vtx.Count && _ign_vtx[vr];
#else
			return false;
#endif
		}
		void restore_ignored_verticies()
		{
#if IGN_BARR

			if (_ign_vtx == null)
				return;
			//foreach (int ix in _ign_vtx.OnesPositions())
			//{
			//	Vref vr = new Vref(ix);
			//}
#endif
		}
		void remove_monovertex_chains()
		{
#if IGN_BARR
			_ign_vtx = new BitArray.BitArr(VertexAlloc);
#endif
			foreach (var vr in vr_leafs)
				trim_up_chain(vr);
		}
		void trim_up_chain(Vref vcur)
		{
			Vref[] vpar;
			while (!ignore_vertex(vcur) && (vpar = vertex_parents_distinct(vcur)).Length == 1)
			{
				var x = vertex_children_distinct(vpar[0]);
				Debug.Assert(x.Length > 0);
				if (x.Length == 1)
					Debug.Assert(x[0] == vcur);

#if IGN_BARR
				if (_ign_vtx != null)
					_ign_vtx[vcur] = true;
#else
				DeleteAllInEdges(vcur);
				DeleteVertex(vcur);
#endif
				if (x.Length > 1)
					break;

				vcur = vpar[0];
			}
		}
		/// 
		/////////////////////////////////////////////////////////////


		/////////////////////////////////////////////////////////////
		/// 
		bool insert_edge_proxies()
		{
			int c_vadded = 0;
			bool f_more = false;

			/// ce_sav: where to stop, since any edges we add during this loop may point to vertexes
			/// which are not in the vref_index. Accordingly, 'insert_edge_proxies' must be called
			/// repeatedly until it reports no new vertexes.
			int ce_sav = EdgeAlloc;

			for (Eref er = Eref.Zero; er < ce_sav; er++)
			{
				if (!edge_is_valid(er))
					continue;

				Vref v_from = edge_parent(er), v_to = edge_target(er);

				if (v_from >= topo.Verticies.Count || v_to >= topo.Verticies.Count)
				{
					f_more = true;
					continue;
				}

				if (ignore_vertex(v_from) || ignore_vertex(v_to))
					continue;

				int L0 = topo.Verticies[v_from].Row;
				int L1 = topo.Verticies[v_to].Row;
				if (L0 < 0 || L1 < 0)
				{
					f_more = true;
					continue;
				}

				int delta = L1 - L0;
				if (--delta > 0)
				{
					DeleteEdge(er);
					topo.Verticies[v_to].Row = -1;

					do
					{
						var v_new = add_dummy_vertex();
						c_vadded++;
						create_dummy_edge(v_from, v_new);
						v_from = v_new;
					}
					while (--delta > 0);
					CreateEdge(v_from, v_to, er);	//  <-- note 'er', maintining the data re-mapping into the source graph
					er--;
					f_more = true;
				}
			}

#if REPORT
			Debug.Print("inserted {0} proxy verticies", c_vadded);
#endif
			return f_more;
		}
		/// 
		/////////////////////////////////////////////////////////////
	};
}
