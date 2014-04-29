#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using alib.String;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	using String = System.String;
	using debug;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public partial class ro_string_graph
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		eent_dbg[] edbg;
		//[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		[DebuggerDisplay("Count: {EdgeCount}", Name = "=== EDGES")]
		public eent_dbg[] z01_EDGES
		{
			[DebuggerStepThrough]
			get
			{
				int c = this.EdgeAlloc();
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
		[DebuggerDisplay("Count: {VertexCount}", Name = "=== VERTICIES")]
		public vent_dbg[] z02_VERTICIES
		{
			[DebuggerStepThrough]
			get
			{
				int c = this.VertexAlloc();
				if (vdbg == null || vdbg.Length != c)
				{
					vdbg = new vent_dbg[c];
					for (Vref i = Vref.Zero; i < c; i++)
						vdbg[i] = new vent_dbg(this, i);
				}
				return vdbg;
			}
		}
	};
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public partial class layout_graph
	{
#if REPORT
		void _full_report(eval_layout hyp)
		{
			Debug.WriteLine("");
			for (int i_level = 0; i_level < levels.Count; i_level++)
			{
				var ss = new debug_logical_level(hyp, i_level).ToString();
				Debug.WriteLine(ss.CondenseSpaces());

				foreach (int ixp in hyp[i_level])
					Debug.WriteLine(new debug_logical_entry(hyp, i_level, ixp).ToString());
			}
		}
#endif

		public override String ToString()
		{
			if (levels == null)
				return "layout levels not initialized";
			var s = String.Format("layout_graph === verticies:{0} -- edges:{1} -- levels:", levels.VertexCount, levels.EdgeCount);
			return s;// +(c_levels == NeedRecalc ? "(need recalc)" : LevelCount.ToString());
		}
	};
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}


namespace alib.dg.debug
{
	using Math = System.Math;
	using String = System.String;

	[Flags]
	public enum DisplayOpts
	{
		None /*					*/ = 0x0000,
		Errors /*				*/ = 0x0001,
		Status /*				*/ = 0x0002,
		HideIndexColumn /*		*/ = 0x0004,
		HideColumn0 /*			*/ = 0x0008,
		HideColumn1 /*			*/ = 0x0010,
		HideColumn2 /*			*/ = 0x0020,
		CondenseSpaces /*		*/ = 0x0040,
		Mode /*					*/ = 0x0080,
		RowAwareDrawing /*		*/ = 0x0100,
	}


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{_disp((DisplayOpts)0x00E),nq}", Name = "{_disp((DisplayOpts)0x0B2),nq}", Type = "Eref (debug)")]
	//[DebuggerDisplay("{ToString(),nq}", Type = "Eref (debug)")]
	public struct eent_dbg
	{
		[DebuggerStepThrough]
		public eent_dbg(IGraphRaw graph, Eref ix, DisplayOpts opts = DisplayOpts.None)
		{
			this.graph = graph ?? dg._singleton;
			this.ix = ix;
			this.opts = opts;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IGraphRaw graph;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Eref ix;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly DisplayOpts opts;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Vref _v_from { get { return ix < 0 ? Vref.NotValid : graph._E_Raw[ix].v_from; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Vref _v_to { get { return ix < 0 ? Vref.NotValid : graph._E_Raw[ix].v_to; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Eref _e_next { get { return ix < 0 ? Eref.NotValid : graph._E_Raw[ix].e_next; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int _value { get { return ix < 0 ? 0 : graph._E_Raw[ix].value; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool ProxyOk { get { return (uint)ix < (uint)graph.EdgeAlloc(); } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsFree { get { return ProxyOk && graph._E_Raw[ix].IsFree; } }

		public vent_dbg From { get { return new vent_dbg(graph, _v_from); } }

		public vent_dbg To { get { return new vent_dbg(graph, _v_to); } }

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public eent_dbg next_peer { get { return graph.z01_EDGES[_e_next]; } }
		public String data { get { return ix < 0 ? "(not valid)" : graph.edge_string(ix); } }

		[DebuggerDisplay("", Name = "--- Peer List ({z10_coref.Length})")]
		public eent_dbg[] z10_coref
		{
			get
			{
				int c;
				if (IsFree || ix < 0 || (c = graph.edge_peer_count(ix)) == 0)
					return Collection<eent_dbg>.None;
				if (c == 1)
					return new[] { this };

				Eref e = ix;
				var arr = new eent_dbg[c];
				while (--c >= 0)
					arr[c] = new eent_dbg(graph, e = graph._E_Raw[e].e_next);

				return arr;
			}
		}

		[DebuggerDisplay("", Name = "--- Parent vertex ({graph.VDict[z20_parent._value],nq})")]
		public vent_dbg z20_parent
		{
			get
			{
				Vref v = _v_from;
				if (!ProxyOk)
					return vent_dbg.NotValid;
				if (graph is rw_graph && ((rw_graph)graph)._vertex_freelist_contains(v))
					return vent_dbg.Free;
				if (!graph.vertex_is_valid(v))
					return vent_dbg.NotValid;
				return new vent_dbg(graph, v);
			}
		}

		public override String ToString()
		{
			return _disp(DisplayOpts.CondenseSpaces);
		}

		public String _disp(DisplayOpts opt_bits = 0, DisplayOpts opt_mask = (DisplayOpts)int.MaxValue)
		{
			try
			{
				return new display_builder(graph, ix, (opts & opt_mask) | opt_bits).render();
			}
			catch (Exception ex)
			{
				return ex.GetType().Name + "\r\n" + ex.Message + "\r\n" + ex.StackTrace.ToString();
			}
		}

		///////////////////////////////////////////////////////
		/// 
		struct display_builder
		{
			[DebuggerStepThrough]
			public display_builder(IGraphRaw dg, Eref _ix, DisplayOpts opts)
				: this()
			{
				this.graph = dg;
				this.ix = _ix;
				this.opts = opts;
			}

			IGraphRaw graph;
			IReadOnlyList<dg.Edge> E { get { return graph._E_Raw; } }
			IReadOnlyList<dg.Vertex> V { get { return graph._V_Raw; } }
			DisplayOpts opts;
			Eref ix;

			String s_status, s_mode;
			String s_src, s_tgt;

			public String render()
			{
				String s_edge = String.Empty;
				s_mode = s_src = s_tgt = String.Empty;

				String s_index;
				if (rw_string_graph.moniker(ix, out s_index))
				{
					s_status = ((int)ix).ToString();
					goto finish;
				}

				if (graph == null)
				{
					s_status = "no graph";
					goto finish;
				}

				if (ix >= E.Count)
				{
					s_status = String.Format("array length {0}", ix, E.Count);
					goto finish;
				}
				if (ix >= graph.EdgeAlloc())
				{
					s_status = String.Format("graph alloc {1}", ix, ((IEditGraph)graph).EdgeAlloc);
					goto finish;
				}

				s_edge = edge_detail(E[ix]);

			finish:
				String s_all = "";

				if (!opts.HasFlag(DisplayOpts.HideIndexColumn))
					s_all += s_index.SQRB().PadLeft(5);

				if (opts.HasFlag(DisplayOpts.Status) && !String.IsNullOrEmpty(s_status))
					s_all += (s_all.Length > 0 ? " " : "") + s_status;

				if (opts.HasFlag(DisplayOpts.Mode))
					s_all += (s_all.Length > 0 ? "  " : "") + s_mode;

				if (!opts.HasFlag(DisplayOpts.HideColumn0))
					s_all += s_src.PadLeft(5);

				if (!opts.HasFlag(DisplayOpts.HideColumn1))
					s_all += (s_all.Length > 0 ? " " : "") + s_edge;

				if (!opts.HasFlag(DisplayOpts.HideColumn2))
					s_all += (s_all.Length > 0 ? " " : "") + s_tgt;

				if (opts.HasFlag(DisplayOpts.CondenseSpaces))
					s_all = s_all.CondenseSpaces();

				return s_all;
			}

			String edge_detail(dg.Edge e)
			{
				if (graph != null)
				{
					if (e.IsFree)
					{
						//if (f1 == f2)
						{
							s_mode = "ⵔ";
							String bks;
							if (e.IsBlockSingleton)
								bks = "-s/e";
							else if (e.IsBlockStart)
								bks = "-start";
							else if (e.IsBlockEnd)
								bks = "-end";
							else if (e.IsFree)
								bks = "  │";
							else
								bks = "-0x" + ((int)e.status).ToString("X8") + "??";

							if (e.block_size > 0)
								bks += "-" + e.block_size.ToString();

							String qq = (("E" + ((int)ix).ToString()).PadLeft(4) + bks).PadRight(23);

							if (e.prv == (int)status.NotInFreelist && e.nxt == (int)status.NotInFreelist)
								return qq;

							String aa, bb;
							if (e.prv < 0)
								aa = "#" + (~e.prv).ToString();
							else
								aa = e.prv.ToString();

							if (e.nxt == (int)status.FreelistTerm)
								bb = "♦";
							else
								bb = e.nxt.ToString();

							return String.Format("{0} {1,5} ←/→ {2}",
								qq,
								aa,
								bb);
						}
						//s_mode = "✘ " + (f1 ? "free" : "alloc");// : f1 ? "ⵔ" : "●";	//ⴲ
						//return String.Format("from:{0} to:{1} next:{2} value:{3}",
						//	e.v_from < 0 ? "✘" : "V" + (int)e.v_from,
						//	e.v_to < 0 ? "✘" : "V" + (int)e.v_to,
						//	e.e_next < 0 ? "✘" : "E" + (int)e.e_next,
						//	(int)e.value);
					}
					s_mode = e.e_next < 0 || e.v_from < 0 || e.v_to < 0 || e.v_to == e.v_from ? " " : "●";	//×
				}

				Vref vf = e.v_from;
				{
					String s_vf;
					if (vf == (int)status.NotInFreelist)
						s_src = "(limbo)";
					else if (rw_string_graph.moniker(e.v_from, out s_vf))
						s_src = s_vf;
					else if (vf < 0)
						s_src = "(-" + (-(int)vf).ToString() + ")";
					else if (vf >= V.Count)
						s_src = "(" + ((int)vf).ToString() + ")";
					else
					{
						var eo = V[vf].e_out;
						if (ix == eo || !opts.HasFlag(DisplayOpts.RowAwareDrawing))
						{
							//s_src += graph.VDict[graph.Verticies[vf].value];
							s_src += graph.vertex_string(vf);
							s_src += " ";
							s_src += s_vf.PadRight(3);
						}
						else if (ix < eo + V[vf].c_out - 1)
							s_src += "│  ";
						else
							s_src += "└──";

						s_src += " ←";

						s_src = s_src.PadLeft(20);
					}
				}

				String s_this = graph.edge_string(ix);
				String s_enxt = String.Empty;
				Vref vt = e.v_to;
				int c = 1;

				{
					if (e.e_next == ix)
					{
						String others = "";
						for (int _e = Eref.Zero; _e < graph.EdgeAlloc(); _e++)
							if (_e != ix && !E[_e].IsFree && E[_e].v_to == vt)
								others += " ×E" + ((int)_e);
						if (others != String.Empty)
						{
							s_enxt = "E" + ((int)ix) + others;
						}
					}
					else if (e.e_next < 0)
						s_enxt = "×";
					else
					{
						Eref i = ix;
						while (true)
						{
							if ((i = E[i].e_next) < 0)
							{
								s_enxt = "×";
								s_mode = " ";
								break;
							}
							if (i == ix)
							{
								s_enxt = e.e_next > ix ? "￬" : "￪";
								break;
							}
							c++;
							if (i == E[i].e_next)
							{
								s_enxt = "→∞";
								s_mode = " ";
								break;
							}
							if (c > 300)
							{
								s_enxt = "→→∞";
								s_mode = " ";
								break;
							}
						}

						s_enxt = s_enxt + "E" + ((int)e.e_next);
					}
				}

				{
					if (vt == (int)status.NotInFreelist)
					{
						s_tgt = "(limbo)";
						s_mode = " ";
					}
					else if (vt == Vref.NotValid)
					{
						s_tgt = "NotValid";
						s_mode = " ";
					}
					else if (vt == vf)
					{
						s_tgt = "↩";
						s_mode = " ";
					}
					else if (vt < 0)
					{
						s_tgt = "(-" + (-(int)vt).ToString() + ")";
						s_mode = " ";
					}
					else if (vt >= V.Count)
					{
						s_tgt = "(" + ((int)vt).ToString() + ")";
						s_mode = " ";
					}
					else
						edge_target_detail(ref e, c, vt, V[vt]);
				}

				int fill = Math.Max(0, 18 - (s_this.Length + s_enxt.Length));

				return (s_this + new String(' ', fill) + s_enxt).SQRB();
			}

			void edge_target_detail(ref dg.Edge e, int c, Vref vt, dg.Vertex v)
			{
				String s_vt, s_link = "";

				if (v.e_in < 0)
				{
					s_link = "✘←";
					s_mode = " ";
				}
				else
				{
					s_link = c == 1 ? "→" : "⇉" + c.SubscriptNum();

					if (v.e_in == ix)
					{
					}
					else if (E[v.e_in].e_next < 0 || E[v.e_in].e_next == v.e_in)
					{
						s_mode = " ";
						s_link = "←";
					}
				}

				rw_string_graph.moniker(vt, out s_vt);
				if (v.e_in == ix && c > 1)
					s_vt = "▲" + s_vt;

				s_tgt += s_link.PadRight(3);
				s_tgt += s_vt.PadLeft(5);
				s_tgt += " ";
				s_tgt += graph.vertex_string(vt);
				s_tgt += v.c_out == 0 ? " ⊣" : " …";
			}
		};
		/// 
		///////////////////////////////////////////////////////
	};
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{_disp((DisplayOpts)0x00E),nq}", Name = "{_disp((DisplayOpts)0x0B2),nq}", Type = "Vref (debug)")]
	//[DebuggerDisplay("{ToString(),nq}", Type = "Vref (debug)")]
	public struct vent_dbg
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly vent_dbg NotValid;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly vent_dbg Free;

		static vent_dbg()
		{
			vent_dbg.Free.ix = new Vref(status.free);
			vent_dbg.NotValid.ix = Vref.NotValid;
		}

		public vent_dbg(IGraphRaw graph, Vref ix, DisplayOpts opts = DisplayOpts.None)
		{
			this.graph = graph ?? dg._singleton;
			this.ix = ix;
			this.opts = opts;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IGraphRaw graph;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Vref ix;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly DisplayOpts opts;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool ProxyOk { get { return (uint)ix < (uint)graph.VertexAlloc(); } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsFree { get { return graph is rw_graph && ((rw_graph)graph)._vertex_freelist_contains(ix); } }

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		[DebuggerDisplay("{_e_in.ToString(),nq}", Name = "--- e_in")]
		public eent_dbg _e_in
		{
			get
			{
				var er = !ProxyOk || IsFree ? Eref.NotValid : graph._V_Raw[ix].e_in;
				return new eent_dbg(graph, er);
			}
		}
		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		[DebuggerDisplay("{_e_out.ToString(),nq}", Name = "--- e_out (first of {_c_out})")]
		public eent_dbg _e_out
		{
			get
			{
				var er = !ProxyOk || IsFree ? Eref.NotValid : graph._V_Raw[ix].e_out;
				return new eent_dbg(graph, er);
			}
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int _c_out
		{
			get
			{
				if (!ProxyOk || IsFree)
					return 0;
				return graph._V_Raw[ix].c_out;
			}
		}
		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int _value
		{
			get
			{
				if (!ProxyOk || IsFree)
					return 0;
				return graph._V_Raw[ix].value;
			}
		}

		[DebuggerDisplay("", Name = "--- Above ({z10_up.Length})")]
		public eent_dbg[] z10_up
		{
			get
			{
				if (!ProxyOk || IsFree)
					return Collection<eent_dbg>.None;

				var src = graph.vertex_in_edges(ix);
				var dst = new eent_dbg[src.Length];
				for (int i = 0; i < src.Length; i++)
					dst[i] = new eent_dbg(graph, src[i]);
				return dst;
			}
		}

		[DebuggerDisplay("", Name = "--- Below ({z11_down.Length})")]
		public eent_dbg[] z11_down
		{
			get
			{
				if (!ProxyOk || IsFree)
					return Collection<eent_dbg>.None;
				return get_below();
			}
		}

		[DebuggerDisplay("", Name = "--- Paths ({z30_paths.Length})")]
		public String[] z30_paths
		{
			get
			{
				if (!ProxyOk || IsFree)
					return Collection<String>.None;
				var er = graph._V_Raw[ix].e_in;
				return er < 0 ? Collection<String>.None : graph.path_strings(er).ToArray();
			}
		}

		public eent_dbg[] get_below()
		{
			if (!ProxyOk || IsFree)
				return Collection<eent_dbg>.None;
			eent_dbg[] arr = new eent_dbg[_c_out];
			Eref e = graph._V_Raw[ix].e_out;
			for (int i = 0; i < _c_out; i++)
				arr[i] = new eent_dbg(graph, e++);
			return arr;
		}

		public override String ToString() { return _disp(DisplayOpts.CondenseSpaces); }

		public String _disp(DisplayOpts opt_bits = 0, DisplayOpts opt_mask = (DisplayOpts)int.MaxValue)
		{
			try
			{
				var db = new display_builder(graph, ix, (opts & opt_mask) | opt_bits);
				return db.render();
			}
			catch (Exception ex)
			{
				return ex.GetType().Name + "\r\n" + ex.Message + "\r\n" + ex.StackTrace.ToString();
			}
		}

		///////////////////////////////////////////////////
		/// 
		struct display_builder
		{
			[DebuggerStepThrough]
			public display_builder(IGraphRaw dg, Vref _ix, DisplayOpts opts)
				: this()
			{
				this.graph = dg;
				this.ix = _ix;
				this.opts = opts;
			}

			IGraphRaw graph;
			IReadOnlyList<dg.Edge> E { get { return graph._E_Raw; } }
			IReadOnlyList<dg.Vertex> V { get { return graph._V_Raw; } }
			DisplayOpts opts;
			Vref ix;
			bool IsFree { get { return graph is rw_graph && ((rw_graph)graph)._vertex_freelist_contains(ix); } }

			String s_status, s_mode;
			String s_from, s_parent, s_to;

			public String render()
			{
				String s_index = ((int)ix).ToString();
				if (graph == null)
				{
					if (ix == Vref.NotValid)
						return s_index + " (Vref.NotValid)";
					//if (ix == Vref.Free)
					//	return s_index + " (Vref.Free)";
					return s_index;
				}

				String s_vertex = String.Empty;
				s_mode = s_from = s_parent = s_to = String.Empty;

				bool bb = rw_string_graph.moniker(ix, out s_status);

				s_index = s_status;
				s_status = null;

				if (bb)
					goto finish;

				if (ix >= V.Count)
				{
					s_status = String.Format("array length {0}", ix, V.Count);
					goto finish;
				}
				if (ix >= graph.VertexAlloc())
				{
					s_status = String.Format("graph alloc {1}", ix, graph.VertexAlloc());
					goto finish;
				}

				s_vertex = vertex_detail(V[ix]);

			finish:
				String s_all = "";

				if (!opts.HasFlag(DisplayOpts.HideIndexColumn))
					s_all += s_index.SQRB().PadLeft(5);

				if (opts.HasFlag(DisplayOpts.Status) && !String.IsNullOrEmpty(s_status))
					s_all += (s_all.Length > 0 ? "  " : "") + s_status;

				if (opts.HasFlag(DisplayOpts.Mode))
					s_all += (s_all.Length > 0 ? "  " : "") + s_mode;

				if (!opts.HasFlag(DisplayOpts.HideColumn0))
				{
					if (s_all.Length > 0)
						s_all += " ";
					s_all += s_from.PadLeft(5);
					s_all += s_parent.PadLeft(3);
				}

				if (!opts.HasFlag(DisplayOpts.HideColumn1))
					s_all += (s_all.Length > 0 ? "  " : "") + s_vertex;

				if (!opts.HasFlag(DisplayOpts.HideColumn2))
					s_all += (s_all.Length > 0 ? "  " : "") + s_to;

				if (opts.HasFlag(DisplayOpts.CondenseSpaces))
					s_all = s_all.CondenseSpaces();

				return s_all;
			}

			unsafe String vertex_detail(dg.Vertex v)
			{
				String s_this, children;
				s_this = children = String.Empty;

				s_mode = "●";
				if (IsFree)
				{
					s_mode = "ⵔ";
					s_to = "→" + (v.nxt == (int)status.FreelistTerm ? "♦" : v.nxt.ToString());
					return (("V" + (int)ix) + "-free").PadRight(15);
				}
				else if (v.c_out < 0)
					s_mode = "(c_out: -" + (-v.c_out).ToString() + ")";
				else if (v.e_in == Eref.None)
					s_parent = "⊢".PadLeft(15);
				else if (v.e_in < 0)
					s_mode = "(e_in: -" + (-(int)v.e_in).ToString() + ")";
				else if (E[v.e_in].v_to != ix)
					s_mode = "✘E" + ((int)v.e_in) + "→V" + ((int)E[v.e_in].v_to);
				else
				{
					if (rw_string_graph.moniker(v.e_in, out s_status))
						return String.Empty;
					s_from += s_status.PadLeft(3);
					s_status = null;
					s_from += graph.edge_is_coref_and_master(v.e_in) ? "▲" : " ";
					if (v.e_in >= 0)
					{
						String sd = graph.edge_string(v.e_in);
						if (sd != null)
							s_from = sd.PadLeft(12) + " " + s_from;
					}

					//var c = graph.edge_peer_count(v.e_in);

					int c1 = 0, c2 = 0;

					//BitArray ba = new BitArray(graph.EdgeAlloc);
					int cb = ((graph.EdgeAlloc() - 1) >> 6) + 1;
					ulong* pul = stackalloc ulong[cb];
					var ba = new alib.Bits.BitHelper(pul, cb);

					for (int _e = Eref.Zero; _e < graph.EdgeAlloc(); _e++)
						if (!E[_e].IsFree && E[_e].v_to == ix)
						{
							ba[_e] = true;
							c1++;
						}

					Eref i = v.e_in;
					if (E[i].IsFree)
					{
						s_parent += " ✘E" + ((int)i) + "(free)";
						c2 = c1;
					}
					else
					{
						while (true)
						{
							if (i < 0)
							{
								s_parent += " ✘";
								break;
							}
							if (E[i].v_to != ix)
								s_parent += " ✘E" + ((int)i) + "→V" + ((int)E[i].v_to);

							ba[i] = false;
							c2++;
							i = E[i].e_next;
							if (i == v.e_in)
								break;

							if (c2 > graph.EdgeAlloc())
							{
								s_parent += "∞";
								break;
							}
						}
					}

					if (c1 != c2)
					{
						for (int j = 0; j < cb; j++)
							if (ba[j])
								s_parent += " ✘E" + ((int)j);
						s_mode = "✘";
					}
					else
					{
						s_parent += c1 == 1 ? "←" : (c1.SubscriptNum() + "⇇");
					}
				}

				s_this = graph.vertex_string(ix);

				if (v.c_out > 0)
				{
					children = v.c_out == 1 ? "→" : "⇉";
					children += v.c_out.SubscriptNum();

					s_to = "";
					for (Eref eo = v.e_out; eo < v.e_out + v.c_out; eo++)
					{
						if (eo > v.e_out)
							s_to += " ";
						if (rw_string_graph.moniker(eo, out s_status))
							return String.Empty;
						s_to += s_status;

						if (eo == E[eo].e_next)
						{
							//s_to += "₁";	//↺₁↩ˢ×
						}
						else if (E[eo].v_to < 0)
						{
							s_to += "✘";
							s_mode = "✘";
						}
						else if (graph.edge_is_coref_and_master(eo))
							s_to += "▲";
					}
					s_status = null;
				}
				else
					children = "　";

				return "〈 "
					+ s_this
					+ new String(' ', Math.Max(0, 15 - (s_this.Length + children.Length)))
					+ children
					+ " 〉 ";
			}
		};
		/// 
		///////////////////////////////////////////////////
	};
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
#endif
