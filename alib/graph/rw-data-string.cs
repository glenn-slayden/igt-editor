using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using alib.Debugging;

using alib.Hashing;
using alib.Enumerable;
using alib.Collections;
using alib.Array;

namespace alib.dg
{
	using String = System.String;
	using Enumerable = System.Linq.Enumerable;
	using Array = System.Array;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class rw_data_graph<TEdge, TVertex> : rw_graph, IGraphData<TEdge, TVertex>
	{
		public rw_data_graph(IIndexedHash<TEdge> edict, IIndexedHash<TVertex> vdict, Edge[] E, Vertex[] V, IGraph source_graph)
			: base(E, V, source_graph)
		{
			attach_dicts(edict, vdict);
		}
		public rw_data_graph()
			: this(null, null, null, null, default(IGraph))
		{
		}
		public rw_data_graph(IGraphRaw to_copy)
			: base()
		{
			var igd = to_copy as IGraphData<TEdge, TVertex>;
			if (igd != null)
				attach_dicts(igd.EDict, igd.VDict);
			else
				attach_dicts(null, null);
		}

		public IIndexedHash<TEdge> edict;
		public IIndexedHash<TVertex> vdict;

		IIndexedHash<TEdge> IGraphData<TEdge, TVertex>.EDict { [DebuggerStepThrough] get { return edict; } }
		IIndexedHash<TVertex> IGraphData<TEdge, TVertex>.VDict { [DebuggerStepThrough] get { return vdict; } }

		void attach_dicts(IIndexedHash<TEdge> edict, IIndexedHash<TVertex> vdict)
		{
			if (edict == null)
				this.edict = new IndexedHash<TEdge>();
			else
				this.edict = edict as IIndexedHash<TEdge> ?? new IndexedHash<TEdge>(edict);

			if (vdict == null)
				this.vdict = new IndexedHash<TVertex>();
			else
				this.vdict = vdict as IIndexedHash<TVertex> ?? new IndexedHash<TVertex>(vdict);
		}

		public Vref add_vertex(TVertex data)
		{
			int x = vdict.Add(data);
			Vref vr = AddVertex(x);

			if (vdata_index != null)
			{
				int c = vdata_index.Length;
				if (x >= c)
				{
					alib.Array.arr.Resize(ref vdata_index, x * 2);
					for (; c < vdata_index.Length; c++)
						vdata_index[c] = Vref.NotValid;
				}
				var _xv = vdata_index[x];
				if (_xv < 0)
					vdata_index[x] = vr;
				else if (_xv != vr)
					Debug.Print("warning: vertex data values might be not unique");
			}
			return vr;
		}
		public Eref create_edge(Vref v_from, Vref v_to, TEdge data)
		{
			int x = data == null ? -1 : edict.Add(data);
			Eref er = CreateEdge(v_from, v_to, x);
			if (x >= 0 && edata_index != null)
			{
				int c = edata_index.Length;
				if (x >= c)
				{
					alib.Array.arr.Resize(ref edata_index, x * 2);
					for (; c < edata_index.Length; c++)
						edata_index[c] = Eref.NotValid;
				}
				var _xe = edata_index[x];
				if (_xe < 0)
					edata_index[x] = er;
				else if (_xe != er)
					Debug.Print("warning: edge data values might be not unique");
			}
			return er;
		}

		public Vref[] vdata_index;
		public Eref[] edata_index;

		public void enable_vdata_index()
		{
			if (vdata_index == null)
			{
				int c = vdict.Count;
				vdata_index = new Vref[c];
				for (int i = 0; i < c; i++)
					vdata_index[i] = Vref.NotValid;

				for (Vref vr = Vref.Zero; vr < VertexAlloc; vr++)
					if (vertex_is_valid(vr))
						vdata_index[V[vr].value] = vr;
			}
		}
		public Vref find_vertex_for_data(TVertex data)
		{
			if (vdata_index == null)
				throw new Exception("vertex data index not active");
			int x = vdict[data];
			return x < 0 ? Vref.NotValid : vdata_index[x];
		}

		public void enable_edata_index()
		{
			if (edata_index == null)
			{
				int c = edict.Count;
				edata_index = new Eref[c];
				for (int i = 0; i < c; i++)
					edata_index[i] = Eref.NotValid;

				for (Eref er = Eref.Zero; er < EdgeAlloc; er++)
					if (edge_is_valid(er))
						edata_index[E[er].value] = er;
			}
		}
		public Eref find_edge_for_data(TEdge data)
		{
			if (edata_index == null)
				throw new Exception("edge data index not active");
			int x = edict[data];
			return x < 0 ? Eref.NotValid : edata_index[x];
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class rw_string_graph : rw_data_graph<String, String>, IStringGraph
	{
		public rw_string_graph(IIndexedHash<String> edict, IIndexedHash<String> vdict, Edge[] E, Vertex[] V, IGraph source_graph)
			: base(edict ?? new StringIndex(), vdict ?? new StringIndex(), E, V, source_graph)
		{
		}
		public rw_string_graph(IGraphRaw to_copy)
			: base(to_copy)
		{
		}
		public rw_string_graph()
		{
		}

		public override String edge_string(Eref e)
		{
			return edict[edge_value(e)];
		}
		public override String vertex_string(Vref v)
		{
			return vdict[vertex_value(v)];
		}

		//public rw_string_graph deep_copy()
		//{
		//	var rwg = new rw_string_graph(this);
		//	Debug.Assert(this.E == rwg.E && this.V == rwg.V && this.edge_freelists == rwg.edge_freelists);
		//	rwg.E = System.Linq.Enumerable.ToArray(this.E);
		//	rwg.V = System.Linq.Enumerable.ToArray(this.V);
		//	rwg.edge_freelists = System.Linq.Enumerable.ToArray(this.edge_freelists);
		//	rwg.check();
		//	return rwg;
		//}
	};
}

