using System;
using System.Diagnostics;
using alib.Debugging;
using alib.Hashing;

namespace alib.dg
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class ro_data_graph<TEdge, TVertex> : dg, IGraphData<TEdge, TVertex>
	{
		public ro_data_graph(IIndexedHash<TEdge> edict, IIndexedHash<TVertex> vdict, Edge[] E, Vertex[] V)
			: base(E, V)
		{
			this.edict = edict ?? new IndexedHash<TEdge>();
			this.vdict = vdict ?? new IndexedHash<TVertex>();
		}
		public ro_data_graph(Edge[] E, Vertex[] V)
			: this(default(IndexedHash<TEdge>), default(IndexedHash<TVertex>), E, V)
		{
		}
		public ro_data_graph()
			: this(default(Edge[]), default(Vertex[]))
		{
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IIndexedHash<TEdge> edict;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IIndexedHash<TVertex> vdict;

		IIndexedHash<TEdge> IGraphData<TEdge, TVertex>.EDict { [DebuggerStepThrough] get { return edict; } }
		IIndexedHash<TVertex> IGraphData<TEdge, TVertex>.VDict { [DebuggerStepThrough] get { return vdict; } }

		public override String edge_string(Eref er)
		{
			TEdge ee;
			return (ee = edict[edge_value(er)]) as String ?? ee.ToString();
		}
		public override String vertex_string(Vref vr)
		{
			TVertex vv;
			return (vv = vdict[vertex_value(vr)]) as String ?? vv.ToString();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class ro_string_graph : ro_data_graph<String, String>, IStringGraph
	{
		public ro_string_graph(IIndexedHash<String> edict, IIndexedHash<String> vdict, Edge[] E, Vertex[] V)
			: base(edict ?? new StringIndex(), vdict ?? new StringIndex(), E, V)
		{
		}
		public ro_string_graph(Edge[] E, Vertex[] V)
			: this(default(StringIndex), default(StringIndex), E, V)
		{
		}
	};
}