using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;
using alib.Bits;
using alib.Hashing;

namespace alib.dg
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class _dg_promoter_base<V> : IGraphExImpl
		where V : class, IVertexEx
	{
		public _dg_promoter_base(IGraph g)
		{
			this.g = g;
		}
		readonly IGraph g;
		public IGraphCommon SourceGraph { get { return g; } }

		public int VertexCount { get { return g.VertexCount; } }

		public int EdgeCount { get { return g.EdgeCount; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public abstract IReadOnlyList<IVertexEx> Verticies { get; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public abstract IReadOnlyList<IEdgeEx> Edges { get; }

		public IReadOnlyList<IVertexEx> Roots { get { return promote(g.vr_roots); } }

		public IReadOnlyList<IVertexEx> Leaves { get { return promote(g.vr_leafs); } }

		IVertexEx[] promote(Vref[] rgvr)
		{
			var _tmp = new IVertexEx[rgvr.Length];
			for (int i = 0; i < _tmp.Length; i++)
				_tmp[i] = Verticies[rgvr[i]];
			return _tmp;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class _dg_promoter<V> : _dg_promoter_base<V>
		where V : class, IVertexEx
	{
		public _dg_promoter(IGraph g)
			: base(g)
		{
			this.esynth = new edge_synth(this);
			this.vsynth = new vtx_synth(this);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IReadOnlyList<V> vsynth;
		public override IReadOnlyList<IVertexEx> Verticies { get { return vsynth; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IReadOnlyList<IEdgeEx> esynth;
		public override IReadOnlyList<IEdgeEx> Edges { get { return esynth; } }

		///////////////////////////////////////////////////////////
		///
		class vtx_synth : IReadOnlyList<V>
		{
			public vtx_synth(_dg_promoter<V> gx)
			{
				this.gx = gx;
				this.rgvx = new V[gx.SourceGraph.VertexAlloc()];
			}

			readonly _dg_promoter<V> gx;
			readonly V[] rgvx;

			public int Count { get { return rgvx.Length; } }

			public V this[int index]
			{
				get
				{
					var _tmp = rgvx[index];
					if (_tmp == null)
					{
						if (typeof(V) == typeof(VertexEx))
							_tmp = (V)(Object)new VertexEx(gx, new Vref(index));
						else if (typeof(V) == typeof(VertexEx))
							_tmp = (V)(Object)new VertexEx(gx, new Vref(index));
						else
							throw not.impl;

						_tmp = Interlocked.CompareExchange(ref rgvx[index], _tmp, null) ?? _tmp;
					}
					return _tmp;
				}
			}

			public IEnumerator<V> GetEnumerator()
			{
				var c = rgvx.Length;
				for (int i = 0; i < c; i++)
					yield return this[i];
			}

			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		};
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		class edge_synth : IReadOnlyList<IEdgeEx>
		{
			public edge_synth(_dg_promoter<V> gx)
			{
				this.gx = gx;
				this.rgex = new IEdgeEx[gx.SourceGraph.EdgeAlloc()];
			}

			readonly _dg_promoter<V> gx;
			readonly IEdgeEx[] rgex;

			public int Count { get { return rgex.Length; } }

			public IEdgeEx this[int index]
			{
				get
				{
					var _tmp = rgex[index];
					if (_tmp == null)
					{
						_tmp = new EdgeEx(gx, new Eref(index));
						_tmp = Interlocked.CompareExchange(ref rgex[index], _tmp, null) ?? _tmp;
					}
					return _tmp;
				}
			}

			public IEnumerator<IEdgeEx> GetEnumerator()
			{
				var c = rgex.Length;
				for (int i = 0; i < c; i++)
					yield return this[i];
			}

			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		};
		///
		///////////////////////////////////////////////////////////
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class GraphExElement : IGraphExElement
	{
		protected GraphExElement(IGraphExImpl gx, int ix)
		{
			this.gx = gx;
			this.ix = ix;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IGraphExImpl gx;
		public IGraphEx GraphInstance { get { return gx; } }

		protected IGraph g { get { return gx as IGraph ?? gx.SourceGraph as IGraph; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly protected int ix;
		public int Index { get { return ix; } }

		String text_label;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public String TextLabel
		{
			get
			{
				var _tmp = text_label;
				if (_tmp == null)
					text_label = _tmp = get_text_label();
				return _tmp;
			}
		}
		protected abstract String get_text_label();
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class EdgeEx : GraphExElement, ILogicalLayoutEdgeEx
	{
		public EdgeEx(IGraphExImpl gx, Eref er)
			: base(gx, (int)er)
		{
			//if (!g.edge_is_valid(er))
			//	throw new Exception();
		}

		Eref er { get { return new Eref(base.ix); } }

		IVertexEx IEdgeEx.From { get { return From; } }

		IVertexEx IEdgeEx.To { get { return To; } }

		protected override String get_text_label()
		{
			return g.edge_string(er);
		}

		public override String ToString()
		{
			return String.Format("E{0}  From V{1}  To V{2} {3}",
				Index,
				From.Index,
				To.Index,
				get_text_label());
		}

		public ILogicalLayoutVertexEx From
		{
			get
			{
				var v_from = g.edge_parent(er);
				Debug.Assert(g.vertex_is_valid(v_from));
				return ((IGraphExLayoutProvider)GraphInstance).Verticies[v_from];
			}
		}

		public ILogicalLayoutVertexEx To
		{
			get
			{
				var v_to = g.edge_target(er);
				Debug.Assert(g.vertex_is_valid(v_to));
				return ((IGraphExLayoutProvider)GraphInstance).Verticies[v_to];
			}
		}

		public EdgeDirection EdgeDirection { get { return EdgeDirection.Normal; } }

		public bool HideContent { get; set; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class VertexEx : GraphExElement, ILogicalLayoutVertexEx
	{
		public VertexEx(IGraphExImpl gx, Vref vr)
			: base(gx, (int)vr)
		{
			//if (!g.vertex_is_valid(vr))
			//	throw new Exception();
			log_pos.Row = -1;
		}

		Vref vr { get { return (Vref)base.ix; } }

		EdgeEx[] rgi;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IEdgeEx> IVertexEx.InEdges { get { return InEdges; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<ILogicalLayoutEdgeEx> InEdges
		{
			get
			{
				if (g is IEditGraph)
					return get_ins();
				var _tmp = rgi;
				if (_tmp == null)
					rgi = _tmp = get_ins();
				return _tmp;
			}
		}
		EdgeEx[] get_ins()
		{
			var rg = g.vertex_in_edges(vr);
			if (rg.Length == 0)
				return Collection<EdgeEx>.None;
			var ret = new EdgeEx[rg.Length];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = (EdgeEx)((IGraphExImpl)GraphInstance).Edges[rg[i]];
			return ret;
		}

		EdgeEx[] rgo;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IEdgeEx> IVertexEx.OutEdges { get { return OutEdges; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IReadOnlyList<ILogicalLayoutEdgeEx> OutEdges
		{
			get
			{
				if (g is IEditGraph)
					return get_outs();
				var _tmp = rgo;
				if (_tmp == null)
					rgo = _tmp = get_outs();
				return _tmp;
			}
		}
		EdgeEx[] get_outs()
		{
			var rg = g.vertex_out_edges(vr);
			if (rg.Length == 0)
				return Collection<EdgeEx>.None;
			var ret = new EdgeEx[rg.Length];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = (EdgeEx)((IGraphExImpl)GraphInstance).Edges[rg[i]];
			return ret;
		}

		public bool LayoutExempt { get; set; }

		public bool HideContent { get; set; }

		LogicalPosition log_pos;
		public LogicalPosition LogicalPosition { get { return log_pos; } }

		public int Row
		{
			get { return log_pos.Row; }
			set { log_pos.Row = value; }
		}
		public int Column
		{
			get { return log_pos.Column; }
			set { log_pos.Column = value; }
		}

		protected override String get_text_label()
		{
			return g.vertex_string(vr);
		}

		public override String ToString()
		{
			var s = String.Format("V{0}  {{ {1} }}  {{ {2} }}  {3}",
				Index,
				get_ins().Select(e => "E" + e.Index).StringJoin(" "),
				get_outs().Select(e => "E" + e.Index).StringJoin(" "),
				get_text_label());

			return String.Format("{0}  row={1} col={2}",
				s,
				log_pos.Row,
				log_pos.Column);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class empty_layout_provider : IGraphExLayoutProvider
	{
		public static readonly empty_layout_provider Instance = new empty_layout_provider();

		public LayoutDirection LayoutDirection { get { return LayoutDirection.TopToBottom; } }

		public bool ExtendLeafVerticies { get { return false; } }

		public bool CompactRootVerticies { get { return false; } }

		public IGraphExLayout ActiveLayout
		{
			get { throw new NotImplementedException(); }
		}

		public IGraphCommon SourceGraph
		{
			get { throw new NotImplementedException(); }
		}

		public int EdgeCount { get { return 0; } }
		public int VertexCount { get { return 0; } }

		public IReadOnlyList<ILogicalLayoutVertexEx> Verticies { get { return Collection<ILogicalLayoutVertexEx>.None; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Verticies { get { return Collection<IVertexEx>.None; } }

		IReadOnlyList<IEdgeEx> IGraphExImpl.Edges { get { return Collection<IEdgeEx>.None; } }
		public IReadOnlyList<ILogicalLayoutEdgeEx> Edges { get { return Collection<ILogicalLayoutEdgeEx>.None; } }

		IReadOnlyList<IVertexEx> IGraphExImpl.Roots { get { return Collection<IVertexEx>.None; } }
		public IReadOnlyList<ILogicalLayoutVertexEx> Roots { get { return Collection<ILogicalLayoutVertexEx>.None; } }

		IReadOnlyList<IVertexEx> IGraphExImpl.Leaves { get { return Collection<IVertexEx>.None; } }
		public IReadOnlyList<ILogicalLayoutVertexEx> Leaves { get { return Collection<ILogicalLayoutVertexEx>.None; } }

		public IGraphExLayoutLevel this[int index] { get { throw new IndexOutOfRangeException(); } }

		public int Count { get { return 0; } }

		public IEnumerator<IGraphExLayoutLevel> GetEnumerator() { return Collection<IGraphExLayoutLevel>.NoneEnumerator; }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	};
}
