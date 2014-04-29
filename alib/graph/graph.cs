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

	public abstract partial class dg_base : IGraphRaw
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly Eref[] NoErefs = Collection<Eref>.None;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly Vref[] NoVrefs = Collection<Vref>.None;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly Edge[] NoEdges = Collection<Edge>.None;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly Vertex[] NoVerticies = Collection<Vertex>.None;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly Vref[] RootZero = new[] { Vref.Zero };

		public struct ctor_args
		{
			public Edge[] E;
			public Vertex[] V;
			public IGraph SourceGraph;
		};

		public dg_base(IReadOnlyList<Edge> E, IReadOnlyList<Vertex> V, IGraph source_graph = null)
		{
			this.source_graph = source_graph;
			this.E = E == null || E.Count == 0 ? NoEdges : E as Edge[] ?? E.ToArray();
			this.V = V == null || V.Count == 0 ? NoVerticies : V as Vertex[] ?? V.ToArray();
			this.version = 1;
		}

		/// <summary> shallow copy </summary>
		public dg_base(dg to_copy)
			: this(to_copy.E, to_copy.V, to_copy)
		{
		}

		/// <summary> deep copy </summary>
		public dg_base(IGraphRaw to_copy)
			: this(to_copy._E_Raw.ToArray(), to_copy._V_Raw.ToArray(), to_copy)
		{
		}

		public dg_base(ctor_args args)
			: this(args.E, args.V, args.SourceGraph)
		{
		}

		public dg_base()
			: this(default(ctor_args))
		{
		}

		public int version;

		readonly public IGraph source_graph;
		public IGraphCommon SourceGraph { get { return source_graph; } }

		protected Vertex[] V;
		public virtual int VertexCount { [DebuggerStepThrough] get { return V.Length; } }
		public IReadOnlyList<Vertex> _V_Raw { [DebuggerStepThrough] get { return V; } }
		protected Edge[] E;
		public virtual int EdgeCount { [DebuggerStepThrough] get { return E.Length; } }
		public IReadOnlyList<Edge> _E_Raw { [DebuggerStepThrough] get { return E; } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// VERTICIES
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		[DebuggerStepThrough]
		public bool vertex_is_valid(Vref vr)
		{
			if ((uint)vr >= (uint)this.VertexAlloc())
				return false;
			var rwg = this as rw_graph;
			return rwg == null || !rwg._vertex_freelist_contains(vr);
		}

		[DebuggerStepThrough]
		public virtual int vertex_value(Vref v) { return V[v].value; }

		[DebuggerStepThrough]
		public virtual String vertex_string(Vref v) { return v.ToString(); }

		[DebuggerStepThrough]
		public virtual Object vertex_ref(Vref v) { return null; }

		[DebuggerStepThrough]
		public int vertex_out_edge_count(Vref v) { return V[v].c_out; }

		public int vertex_in_edge_count(Vref v)
		{
			var e = V[v].e_in;
			if (e < 0)
				return 0;
			return edge_peer_count(e);
		}

		[DebuggerStepThrough]
		public Eref[] vertex_in_edges(Vref v)
		{
			var e = V[v].e_in;
			return e < 0 ? NoErefs : edge_peers(e);
		}

		public Eref vertex_unary_parent(Vref v)
		{
			var e = V[v].e_in;
			return e < 0 || E[e].e_next != e ? Eref.NotValid : e;
		}

		public bool vertex_is_unary_path(Vref v)
		{
			return vertex_out_edge_count(v) == 1 && vertex_in_edge_count(v) == 1;
		}

		[DebuggerStepThrough]
		public bool vertex_is_leaf(Vref v) { return vertex_is_valid(v) && V[v].c_out == 0; }

		[DebuggerStepThrough]
		public bool vertex_is_root(Vref v) { return vertex_is_valid(v) && V[v].e_in == Eref.None; }

		public bool vertex_has_edge_to(Vref v, Vref v_to)
		{
			int c = V[v].c_out;
			if (c > 0)
				for (Eref e = V[v].e_out, e_last = e + c; e < e_last; e++)
					if (E[e].v_to == v_to)
						return true;
			return false;
		}

		public unsafe bool vertex_has_ancestor(Vref v_upper, Vref v_lower)
		{
			int cb = ((this.VertexAlloc() - 1) >> 6) + 1;
			ulong* pul = stackalloc ulong[cb];
			var ba = new BitHelper(pul, cb);
			_mark_ancestors(v_lower, ref ba);
			return ba[v_upper];
		}

		[DebuggerStepThrough]
		public Eref[] vertex_out_edges(Vref v) { return V[v].out_edges; }

		public int[] vertex_out_values(Vref v)
		{
			int c;
			if ((c = V[v].c_out) == 0)
				return IntArray.Empty;

			var arr = new int[c];
			var e = V[v].e_out;
			for (int i = 0; i < c; i++, e++)
				arr[i] = E[e].value;
			return arr;
		}

		[DebuggerStepThrough]
		public Eref vertex_master_edge(Vref v) { return V[v].e_in; }

		[DebuggerStepThrough]
		public bool vertex_is_coreferenced(Vref v) { return edge_target_coreferenced(V[v].e_in); }

		public void vertex_switch_master_edge(Vref v, Eref e_new)
		{
			var e_old = V[v].e_in;
			if (e_old >= 0 && e_old != e_new)
			{
				if (!edge_has_peer(e_old, e_new))
					throw new Exception();
				V[v].e_in = e_new;
				version++;
			}
		}

		public Vref[] vertex_parents(Vref v)
		{
			Eref e = V[v].e_in;
			if (e == Eref.None)
				return NoVrefs;

			int c = edge_peer_count(e);
			if (c == 1)
				return new[] { E[e].v_from };

			var arr = new Vref[c];
			while (--c >= 0)
			{
				arr[c] = E[e].v_from;
				e = E[e].e_next;
			}
			return arr;
		}
#if true
		//public Vref[] vertex_parents_distinct(Vref v)
		//{
		//	var a = vertex_parents_distinct1(v);
		//	var b = vertex_parents_distinct2(v);

		//	Array.Sort(a);
		//	Array.Sort(b);
		//	if (!a.SequenceEqual(b))
		//	{
		//		throw new Exception();
		//	}
		//	return a;
		//}

		public Vref[] vertex_parents_distinct(Vref v)
		{
			Eref e;
			if ((e = V[v].e_in) == Eref.None)
				return NoVrefs;

			int c = edge_peer_count(e);
			if (c == 1)
				return new[] { E[e].v_from };

			var a = new Vref[c];
			c = 0;
			var e_start = e;
			do
			{
				v = E[e].v_from;
				for (int j = 0; j < c; j++)
					if (a[j] == v)
						goto already_have;
				a[c++] = v;
			already_have:
				;
			}
			while ((e = E[e].e_next) != e_start);

			if (c < a.Length)
				arr.Resize(ref a, c);
			return a;
		}
#else
		public Vref[] vertex_parents_distinct(Vref v)
		{
			Eref e;
			if ((e = V[v].e_in) == Eref.None)
				return NoVrefs;

			int c = edge_peer_count(e);
			var arr = new Vref[c];
			arr[0] = E[e].v_from;

			if (c > 1 && c > (c = _vpd(e, arr)))
				arr = AlibArr.Resize(arr, c);

			return arr;
		}

		int _vpd(Eref e, Vref[] arr)
		{
			var e_start = e;
			var v = arr[0];
			int j, c = 1;
			while ((e = E[e].e_next) != e_start)
			{
				if (v == (v = E[e].v_from))
					continue;
				for (j = c - 1; j >= 0; j--)
					if (arr[j] == v)
						goto already_have;
				arr[c++] = v;
			already_have:
				;
			}
			return c;
		}
#endif

		public int vertex_vref_parent_count(Vref v, Vref par)
		{
			Nop.Untested();
			Eref e;
			int c = 0;
			if ((e = V[v].e_in) >= 0)
			{
				Eref e_start = e;
				do
					if (E[e].v_from == par)
						c++;
				while ((e = E[e].e_next) != e_start);
			}
			return c;
		}

		public unsafe Vref[] vertex_ancestors(Vref v)
		{
			if (V[v].e_in == Eref.None)
				return NoVrefs;

			int cb = ((this.VertexAlloc() - 1) >> 6) + 1;
			ulong* pul = stackalloc ulong[cb];
			var ba = new BitHelper(pul, cb);

			return _from_bitarray(_mark_ancestors(v, ref ba), ref ba);
		}

		public unsafe Vref[] vertex_ancestors_and_self(Vref v)
		{
			if (V[v].e_in == Eref.None)
				return new[] { v };

			int cb = ((this.VertexAlloc() - 1) >> 6) + 1;
			ulong* pul = stackalloc ulong[cb];
			var ba = new BitHelper(pul, cb);

			ba[v] = true;
			return _from_bitarray(_mark_ancestors(v, ref ba) + 1, ref ba);
		}

		public Vref[] vertex_children(Vref v)
		{
			int c = V[v].c_out;
			if (c == 0)
				return NoVrefs;

			var arr = new Vref[c];
			Eref e = V[v].e_out;
			for (int i = 0; i < c; i++, e++)
				arr[i] = E[e].v_to;
			return arr;
		}

		public Vref[] vertex_children_distinct(Vref v)
		{
			int i, j, c;

			if ((c = V[v].c_out) == 0)
				return NoVrefs;
			Eref e = V[v].e_out;
			if (c == 1)
				return new[] { E[e].v_to };

			var a = new Vref[c];
			for (c = i = 0; i < a.Length; i++)
			{
				v = E[e + i].v_to;
				for (j = 0; j < c; j++)
					if (a[j] == v)
						goto already_have;
				a[c++] = v;
			already_have:
				;
			}
			if (c < a.Length)
				arr.Resize(ref a, c);
			return a;
		}

		public Eref[] vertex_cotargeted_edges(Vref v)
		{
			int c;

			if ((c = V[v].c_out) < 2)
				return NoErefs;

			//int c = VertexAlloc;
			//for (Vref v = Vref.Zero; v < c; v++)
			//	if (vertex_is_valid(v) && !vertex_children(v).IsDistinct())
			//		return true;
			//return false;
			throw new NotImplementedException();
		}

		public unsafe Vref[] vertex_descendants(Vref v)
		{
			if (V[v].c_out == 0)
				return NoVrefs;

			int cb = ((this.VertexAlloc() - 1) >> 6) + 1;
			ulong* pul = stackalloc ulong[cb];
			var ba = new BitHelper(pul, cb);

			return _from_bitarray(_mark_descendants(v, ref ba), ref ba);
		}

		public unsafe Vref[] vertex_descendants_and_self(Vref v)
		{
			int c = V[v].c_out;
			if (c == 0)
				return new[] { v };

			int cb = ((this.VertexAlloc() - 1) >> 6) + 1;
			ulong* pul = stackalloc ulong[cb];
			var ba = new BitHelper(pul, cb);

			ba[v] = true;
			return _from_bitarray(_mark_descendants(v, ref ba) + 1, ref ba);
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// EDGES
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		[DebuggerStepThrough]
		public bool edge_is_valid(Eref er)
		{
			if ((uint)er >= (uint)this.EdgeAlloc())
				return false;
			return !(this is rw_graph) || !E[er].IsFree;
		}

		[DebuggerStepThrough]
		public Vref edge_parent(Eref e) { return E[e].v_from; }

		[DebuggerStepThrough]
		public Eref edge_next_peer(Eref e) { return E[e].e_next; }

		[DebuggerStepThrough]
		public virtual int edge_value(Eref e) { return E[e].value; }

		[DebuggerStepThrough]
		public virtual String edge_string(Eref e) { return e.ToString(); }

		[DebuggerStepThrough]
		public virtual Object edge_ref(Eref e) { return null; }

		[DebuggerStepThrough]
		public Vref edge_target(Eref e) { return E[e].v_to; }

		[DebuggerStepThrough]
		public bool edge_target_coreferenced(Eref e) { return e != E[e].e_next; }

		[DebuggerStepThrough]
		public bool edge_is_non_coref_or_master(Eref e) { return e == V[E[e].v_to].e_in; }

		public void edge_set_master(Eref e) { vertex_switch_master_edge(E[e].v_to, e); }

		public bool edge_is_coref_and_master(Eref e) { return e != E[e].e_next && e == V[E[e].v_to].e_in; }

		public Eref get_edge(Vref v_from, Vref v_to)
		{
			int c = V[v_from].c_out;
			if (c > 0)
				for (Eref e = V[v_from].e_out, e_end = e + c; e < e_end; e++)
					if (E[e].v_to == v_to)
						return e;
			return Eref.NotValid;
		}

		public int edge_peer_count(Eref e)
		{
			if (e < 0)
				return 0;
			int c = 1;
			var i = e;
			while ((i = E[i].e_next) != e)
				c++;
			return c;
		}

		public Eref edge_peer_list_prev(Eref e)
		{
			Eref tmp, cur = e;
			while (e != (tmp = E[cur].e_next))
				if (e == (cur = E[tmp].e_next))
					return tmp;
			return cur;
		}

		public bool edge_has_peer(Eref e, Eref e_unk)
		{
			if (e_unk < 0 || e < 0)
				return false;

			var i = e;
			do
				if (i == e_unk)
					return true;
			while ((i = E[i].e_next) != e);
			return false;
		}

		public Eref[] edge_peers(Eref e)
		{
			int c = edge_peer_count(e);
			if (c == 0)
				return NoErefs;
			if (c == 1)
				return new[] { e };

			var arr = new Eref[c];
			while (--c >= 0)
				arr[c] = e = E[e].e_next;

			return arr;
		}

		public IEnumerable<Eref[]> edge_paths(Eref e)
		{
			if (e < 0)
				yield return NoErefs;
			else
				foreach (var e_back in edge_peers(e))
					foreach (var q in edge_paths(V[E[e_back].v_from].e_in))
						yield return q.Append(e_back);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// GRAPH
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public Vref[] vr_verticies
		//{
		//	get
		//	{
		//		var arr = new Vref[VertexCount];
		//		int i = 0;
		//		for (Vref vr = Vref.Zero; i < arr.Length; vr++)
		//			if (vertex_is_valid(vr))
		//				arr[i++] = vr;
		//		Debug.Assert(i == VertexCount);
		//		return arr;
		//	}
		//}

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public IReadOnlyCollection<Eref> er_edges
		//{
		//	get
		//	{
		//		var arr = new Eref[EdgeCount];
		//		int i = 0;
		//		for (Eref er = Eref.Zero; i < arr.Length; er++)
		//			if (edge_is_valid(er))
		//				arr[i++] = er;
		//		Debug.Assert(i == EdgeCount);
		//		return arr;
		//	}
		//}

		protected class roots_ver
		{
			public roots_ver(dg_base dg)
			{
				this.version = dg.version;
				int c = dg.VertexAlloc();
				this._roots = Collection<Vref>.None;
				for (Vref v = Vref.Zero; v < c; v++)
					if (dg.vertex_is_valid(v) && dg.V[v].e_in == Eref.None)
						arr.Append(ref this._roots, v);
			}
			public roots_ver(dg_base dg, Vref single_root)
			{
				this.version = dg.version;
				this._roots = new[] { single_root };
			}
			public Vref[] _roots;
			public int version;
		};

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected roots_ver _vr_roots;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Vref[] vr_roots
		{
			get
			{
				var _tmp = _vr_roots;
				if (_tmp == null || _tmp.version != version)
				{
					var _new = new roots_ver(this);
					if (_tmp == (_tmp = Interlocked.CompareExchange(ref _vr_roots, _new, _tmp)))
						_tmp = _new;
				}
				return _tmp._roots;
			}
		}

		protected class leafs_ver
		{
			public leafs_ver(dg_base dg)
			{
				this.version = dg.version;
				int c = dg.VertexAlloc();
				this._leafs = Collection<Vref>.None;
				for (Vref v = Vref.Zero; v < c; v++)
					if (dg.vertex_is_valid(v) && dg.V[v].c_out == 0)
						arr.Append(ref this._leafs, v);
			}
			public Vref[] _leafs;
			public int version;
		};

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected leafs_ver _vr_leafs;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Vref[] vr_leafs
		{
			get
			{
				var _tmp = _vr_leafs;
				if (_tmp == null || _tmp.version != version)
				{
					var _new = new leafs_ver(this);
					if (_tmp == (_tmp = Interlocked.CompareExchange(ref _vr_leafs, _new, _tmp)))
						_tmp = _new;
				}
				return _tmp._leafs;
			}
		}

		//public bool has_duplicate_edges()
		//{
		//	int c = VertexAlloc;
		//	for (Vref v = Vref.Zero; v < c; v++)
		//		if (vertex_is_valid(v) && !vertex_children(v).IsDistinct())
		//			return true;
		//	return false;
		//}

		public Vref[][] GetVertexLevelGroups()
		{
			var fl = new _find_levels(this);
			var levels = fl.levels;
			int cv = levels.Length, c_levels = fl.max_level + 1;

			Vref[][] grps = new Vref[c_levels][];
			for (int i = 0; i < cv; i++)
			{
				int vl = levels[i];
				alib.Array.arr.Append(ref grps[vl], (Vref)i);
			}
			Debug.Assert(grps.Sum(x => x.Length) == VertexCount);
			return grps;
		}

		public int[] GetVertexLevels() { return new _find_levels(this).levels; }

		////////////////////////////////////////////////////////////
		/// 
		struct _find_levels
		{
			public _find_levels(dg_base graph)
			{
				this.graph = graph;
				this.max_level = 0;
				int c = graph.VertexAlloc();
				this.levels = new int[c];
				for (int i = 0; i < c; i++)
				{
					if (graph.V[i].c_out == 0)
						get_level((Vref)i);
				}
			}

			readonly dg_base graph;
			public readonly int[] levels;
			public int max_level;

			int get_level(Vref v)
			{
				int pl, lev = levels[v];
				Eref e;
				if (lev == 0 && (e = graph.V[v].e_in) != Eref.None)
				{
					var arr = graph.vertex_in_edges(v);
					for (int i = 0; i < arr.Length; i++)
						if ((pl = get_level(graph.edge_parent(arr[i]))) > lev)
							lev = pl;
					if ((levels[v] = ++lev) > max_level)
						max_level = lev;
				}
				return lev;
			}
		};
		/// 
		////////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// non-public
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		protected unsafe int _mark_ancestors(Vref vr, ref BitHelper ba)
		{
			var rgp = vertex_parents(vr);
			int c = 0, c_par = rgp.Length;
			Vref par;
			for (int j = 0; j < c_par; j++)
				if (!ba[par = rgp[j]])
				{
					ba[par] = true;
					c += _mark_ancestors(par, ref ba) + 1;
				}
			return c;
		}

		protected int _mark_descendants(Vref vr, ref BitHelper ba)
		{
			var rgc = vertex_children(vr);
			int c = 0, cc = rgc.Length;
			Vref child;
			for (int j = 0; j < cc; j++)
				if (!ba[child = rgc[j]])
				{
					ba[child] = true;
					c += _mark_descendants(child, ref  ba) + 1;
				}
			return c;
		}

		Vref[] _from_bitarray(int c, ref BitHelper ba)
		{
			var ret = new Vref[c];
			for (int i = 0, j = 0; j < c; i++)
				if (ba[i])
					ret[j++] = new Vref(i);
			return ret;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class dg_E : dg_base, IReadOnlyList<ILogicalLayoutEdgeEx>
	{
		public dg_E(IReadOnlyList<Edge> E, IReadOnlyList<Vertex> V, IGraph source_graph = null) : base(E, V, source_graph) { }

		public dg_E(dg to_copy) : this(to_copy.E, to_copy.V, to_copy) { }

		public dg_E(IGraphRaw to_copy) : this(to_copy._E_Raw.ToArray(), to_copy._V_Raw.ToArray(), to_copy) { }

		public dg_E(ctor_args args) : this(args.E, args.V, args.SourceGraph) { }

		public dg_E() : this(default(ctor_args)) { }

		ILogicalLayoutEdgeEx[] _rg;
		ILogicalLayoutEdgeEx[] ee()
		{
			ILogicalLayoutEdgeEx[] _tmp;
			if ((_tmp = _rg) == null)
			{
				_tmp = new ILogicalLayoutEdgeEx[this.EdgeAlloc()];
				_tmp = Interlocked.CompareExchange(ref _rg, _tmp, null) ?? _tmp;
			}
			return _tmp;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int IReadOnlyCollection<ILogicalLayoutEdgeEx>.Count { get { return ee().Length; } }

		ILogicalLayoutEdgeEx IReadOnlyList<ILogicalLayoutEdgeEx>.this[int index]
		{
			get
			{
				var _tmp = ee()[index];
				if (_tmp == null)
				{
					_tmp = new EdgeEx((IGraphExImpl)this, new Eref(index));
					_tmp = Interlocked.CompareExchange(ref ee()[index], _tmp, null) ?? _tmp;
				}
				return _tmp;
			}
		}

		IEnumerator<ILogicalLayoutEdgeEx> IEnumerable<ILogicalLayoutEdgeEx>.GetEnumerator()
		{
			var _tmp = ee();
			for (int i = 0; i < _tmp.Length; i++)
				yield return ((IReadOnlyList<ILogicalLayoutEdgeEx>)this)[i];
		}

		IEnumerator IEnumerable.GetEnumerator() { return ((IReadOnlyList<ILogicalLayoutEdgeEx>)this).GetEnumerator(); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class dg_V : dg_E, IReadOnlyList<ILogicalLayoutVertexEx>
	{
		public dg_V(IReadOnlyList<Edge> E, IReadOnlyList<Vertex> V, IGraph source_graph) : base(E, V, source_graph) { }

		public dg_V(dg to_copy) : this(to_copy.E, to_copy.V, to_copy) { }

		public dg_V(IGraphRaw to_copy) : this(to_copy._E_Raw.ToArray(), to_copy._V_Raw.ToArray(), to_copy) { }

		public dg_V(ctor_args args) : this(args.E, args.V, args.SourceGraph) { }

		public dg_V() : this(default(ctor_args)) { }

		ILogicalLayoutVertexEx[] _rg;
		ILogicalLayoutVertexEx[] vv()
		{
			ILogicalLayoutVertexEx[] _tmp;
			if ((_tmp = _rg) == null)
			{
				_tmp = new ILogicalLayoutVertexEx[this.VertexAlloc()];
				_tmp = Interlocked.CompareExchange(ref _rg, _tmp, null) ?? _tmp;
			}
			return _tmp;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int IReadOnlyCollection<ILogicalLayoutVertexEx>.Count { get { return vv().Length; } }

		ILogicalLayoutVertexEx IReadOnlyList<ILogicalLayoutVertexEx>.this[int index]
		{
			get
			{
				var _tmp = vv()[index];
				if (_tmp == null)
				{
					_tmp = promote_vref(new Vref(index));
					_tmp = Interlocked.CompareExchange(ref vv()[index], _tmp, null) ?? _tmp;
				}
				return _tmp;
			}
		}

		protected abstract ILogicalLayoutVertexEx promote_vref(Vref vr);

		IEnumerator<ILogicalLayoutVertexEx> IEnumerable<ILogicalLayoutVertexEx>.GetEnumerator()
		{
			var _tmp = vv();
			for (int i = 0; i < _tmp.Length; i++)
				yield return ((IReadOnlyList<ILogicalLayoutVertexEx>)this)[i];
		}

		IEnumerator IEnumerable.GetEnumerator() { return ((IReadOnlyList<ILogicalLayoutVertexEx>)this).GetEnumerator(); }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class dg : dg_V, IGraphFull
	{
#if DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static dg _singleton;
#endif

		public dg(IReadOnlyList<Edge> E, IReadOnlyList<Vertex> V, IGraph source_graph = null)
			: base(E, V, source_graph)
		{
#if DEBUG
			_singleton = this;
#endif
		}

		public dg(dg to_copy) : this(to_copy.E, to_copy.V, to_copy) { }

		public dg(IGraphRaw to_copy) : this(to_copy._E_Raw.ToArray(), to_copy._V_Raw.ToArray(), to_copy) { }

		public dg(ctor_args args) : this(args.E, args.V, args.SourceGraph) { }

		public dg() : this(default(ctor_args)) { }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// IGraphEx
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		protected override ILogicalLayoutVertexEx promote_vref(Vref vr) { return new VertexEx(this, vr); }

		ILogicalLayoutVertexEx[] promote(Vref[] rgvr)
		{
			var _tmp = new ILogicalLayoutVertexEx[rgvr.Length];
			for (int i = 0; i < _tmp.Length; i++)
				_tmp[i] = Verticies[rgvr[i]];
			return _tmp;
		}

		public IReadOnlyList<ILogicalLayoutVertexEx> Verticies { get { return this; } }

		public IReadOnlyList<ILogicalLayoutEdgeEx> Edges { get { return this; } }

		IReadOnlyList<IVertexEx> IGraphExImpl.Verticies { get { return this; } }

		IReadOnlyList<IEdgeEx> IGraphExImpl.Edges { get { return this; } }

		public IReadOnlyList<ILogicalLayoutVertexEx> Roots { get { return promote(vr_roots); } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Roots { get { return promote(vr_roots); } }

		public IReadOnlyList<ILogicalLayoutVertexEx> Leaves { get { return promote(vr_leafs); } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Leaves { get { return promote(vr_leafs); } }


		public LayoutDirection LayoutDirection
		{
			get { return LayoutDirection.TopToBottom; }
		}

		public bool ExtendLeafVerticies { get { return false; } }

		public bool CompactRootVerticies { get { return false; } }

		public IGraphExLayout ActiveLayout { get { throw new NotImplementedException(); } }


		IGraphExLayoutLevel IReadOnlyList<IGraphExLayoutLevel>.this[int index]
		{
			get { throw new NotImplementedException(); }
		}

		int IReadOnlyCollection<IGraphExLayoutLevel>.Count
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerator<IGraphExLayoutLevel> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public override String ToString()
		{
			return String.Format("edges: {0}/{1}  verticies:{2}/{3}", EdgeCount, this.EdgeAlloc(), VertexCount, this.VertexAlloc());
		}
	};
}
