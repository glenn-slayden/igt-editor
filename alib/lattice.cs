using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using alib.Collections;
using alib.Array;
using alib.Debugging;
using alib.Enumerable;
using alib.String;

namespace alib.Lattice
{
	using SysArray = System.Array;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public class Lattice<T>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly LatticeEdge[] NoEdges = Collection<LatticeEdge>.None;

		public Lattice()
		{
			this.Vertices = Collection<LatticeVertex>.None;
#if edge_list
			rge = new RefList<LatticeEdge>(19);
#endif
			need_level_reset = true;
		}

		bool need_level_reset;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int next_vertex, next_edge = 1;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LatticeVertex[] Vertices;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IEnumerable<LatticeEdge> Edges
		{
			get { return Vertices.SelectManyDistinct(v => v.rights); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int VertexCount { get { return Vertices.Length; } }

#if !edge_list
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int EdgeCount { get { return Edges._Count(); } }
#else
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IEnumerable<LatticeEdge> Edges { get { return rge; } }

		public int EdgeCount { get { return rge.Count; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		RefList<LatticeEdge> rge;
#endif

		public LatticeVertex NewVertex()
		{
			var v = new LatticeVertex(this);
			arr.Append(ref Vertices, v);
			return v;
		}

		public LatticeEdge AddEdge(T data, LatticeVertex source = null, LatticeVertex target = null)
		{
			return new LatticeEdge(this, source, target, data);
		}

		public bool IsAncestor(LatticeVertex v1, LatticeVertex v2)
		{
			//if (v1 == null || v2 == null)
			//	throw new Exception();
			if (v1 == null)
				return false;
			if (v2 == null)
				return false;

			if (v1 == v2)
				return true;
			if (v1.Level >= v2.Level)
				return false;

			return v1.rights.SelectDistinct(e => e.Target).Any(v => IsAncestor(v, v2));
		}

		public IEnumerable<LatticeEdge> CospanningEdges(LatticeEdge e)
		{
			var t = e.Target;
			if (t == null)
				throw new Exception();
			foreach (var ecsp in e.Source.rights)
				if (e != ecsp && ecsp.Target == t)
					yield return ecsp;
		}

		public virtual String GetItemDisplay(LatticeItem item) { return ""; }

		public abstract class LatticeItem
		{
			public LatticeItem(Lattice<T> lat, int id)
			{
				this.lat = lat;
				this._id = id;
			}
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public readonly Lattice<T> lat;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public readonly int _id;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public String StringId
			{
				get
				{
					if (this is Trellis<T>.start_vertex)
						return "StartVertex";
					if (this is Trellis<T>.end_vertex)
						return "EndVertex";
					return this.GetType().Name[7] + _id.ToString();
				}
			}

			public abstract String LongDisplay();
			public abstract String ShortDisplay();
			public sealed override String ToString() { return LongDisplay(); }
		};

		[DebuggerDisplay("{ToString(),nq}")]
		public class LatticeVertex : LatticeItem, IComparable<LatticeVertex>
		{
			public LatticeVertex(Lattice<T> lat)
				: base(lat, lat.next_vertex++)
			{
				this._lefts = NoEdges;
				this._rights = NoEdges;
				this._level = -1;	/// level reset on the lattice not needed until edges are attached
			}

			/// keep these private so that we can correctly control the level-resetting for the lattice
			LatticeEdge[] _lefts, _rights;

			public LatticeEdge[] lefts { get { return _lefts; } }

			public LatticeEdge[] rights { get { return _rights; } }

			public void AddLeftEdge(LatticeEdge e)
			{
				Debug.Assert(_lefts == null || SysArray.IndexOf<LatticeEdge>(_lefts, e) == -1);

				if (this is Trellis<T>.start_vertex)
					throw new Exception("cannot precede the start vertex in a trellis");

				arr.Append(ref _lefts, e);
				lat.need_level_reset = true;
			}
			public void AddRightEdge(LatticeEdge e)
			{
				Debug.Assert(_rights == null || SysArray.IndexOf<LatticeEdge>(_rights, e) == -1);

				if (this is Trellis<T>.end_vertex)
					throw new Exception("cannot succeed the end vertex in a trellis");

				arr.Append(ref _rights, e);
				lat.need_level_reset = true;
			}

			public void RemoveLeftEdge(LatticeEdge e)
			{
				Debug.Assert(SysArray.IndexOf<LatticeEdge>(_lefts, e) != -1);
				if (_lefts.Length == 1 && _rights.Length == 0)
					lat.Vertices = lat.Vertices.Remove(this);
				else
					_lefts = _lefts.Remove(e);
				lat.need_level_reset = true;
			}
			public void RemoveRightEdge(LatticeEdge e)
			{
				Debug.Assert(SysArray.IndexOf<LatticeEdge>(_rights, e) != -1);
				if (_rights.Length == 1 && _lefts.Length == 0)
					lat.Vertices = lat.Vertices.Remove(this);
				else
					_rights = _rights.Remove(e);
				lat.need_level_reset = true;
			}

			//partial ordering -- number of edges on the longest path from the root node
			int _level;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int Level
			{
				get
				{
					if (lat.need_level_reset)
					{
						foreach (var v in lat.Vertices)
							v._level = -1;
						lat.need_level_reset = false;
					}
					return find_level();
				}
			}

			int find_level()
			{
				if (_level == -1)
				{
					if (this._lefts.Length == 0)
						_level = 0;
					else
						_level = _lefts.Max(le => le.Source.find_level()) + 1;
				}
				return _level;
			}

			public IEnumerable<LatticeEdge> DescendantEdges()
			{
				return _rights.Concat(_rights.SelectMany(x => x.Target.DescendantEdges())).Distinct();
			}

			public IEnumerable<LatticeEdge> AnscestorEdges()
			{
				return _lefts.Concat(_lefts.SelectMany(x => x.Source.AnscestorEdges())).Distinct();
			}

#if PATHS
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public IEnumerable<ICollection<LatticeEdge>> RightwardsPaths
			{
				get { return _rights.SelectMany(e => e._r_paths); }
			}
#endif
			public override String ShortDisplay()
			{
				var s = lat.GetItemDisplay(this);
				if (String.IsNullOrWhiteSpace(s))
					return StringId;
				return String.Format("[{0} {1}]", StringId, s);
			}

			static String _fmt(LatticeEdge[] _in)
			{
				return _in.Select(e => e.ShortDisplay()).StringJoin(" ");
			}
			public override String LongDisplay()
			{
				return String.Format("{{ {0} }} -{1}- {{ {2} }}",
					_fmt(_lefts).PadLeft(60),
					String.Format("{0}", this.ShortDisplay(), Level).PadCenter(20, '-'),
					_fmt(_rights));
			}

			public int CompareTo(LatticeVertex other)
			{
				var d = this.Level - other.Level;
				if (d != 0)
					return d;
				return this._id - other._id;
			}
		};

		[DebuggerDisplay("{ToString(),nq}")]
		public class LatticeEdge : LatticeItem, IComparable<LatticeEdge>
		{
			public LatticeEdge(Lattice<T> lat, T data)
				: base(lat, lat.next_edge++)
			{
				this.data = data;
#if edge_list
				lat.rge.Add(this);
#endif
			}

			public LatticeEdge(Lattice<T> lat, LatticeVertex source, LatticeVertex target, T data)
				: this(lat, data)
			{
				this.Source = source;
				this.Target = target;
			}

			public void Remove()
			{
				this.Source = null;
				this.Target = null;
#if edge_list
				lat.rge.Remove(this);
#endif
			}

			public T data;

			//[DebuggerDisplay("{alib.String._string_ext.CondenseSpaces(_source.ToString()),nq}")]
			LatticeVertex _source;
			//[DebuggerDisplay("{alib.String._string_ext.CondenseSpaces(_target.ToString()),nq}")]
			LatticeVertex _target;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public LatticeVertex Source	// (leftwards, this edge is a 'right' from the perspective of that vertex)
			{
				get { return this._source; }
				set
				{
					if (value == _source)
						return;

					if (_source != null)
						_source.RemoveRightEdge(this);

					if ((_source = value) == null)
					{
						//if (_target == null)
						//	lat.rge.Remove(this);
						return;
					}

					if (value == _target)
						Debugger.Break(); // self-loop

					_source.AddRightEdge(this);
				}
			}
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public LatticeVertex Target		// (rightwards, this edge is a 'left' from the perspective of that vertex)
			{
				get { return this._target; }
				set
				{
					if (value == _target)
						return;

					if (_target != null)
						_target.RemoveLeftEdge(this);

					if ((_target = value) == null)
					{
						//if (_source == null)
						//	lat.rge.Remove(this);
						return;
					}

					if (value == _source)
						Debugger.Break(); // self-loop

					_target.AddLeftEdge(this);
				}
			}
#if PATHS
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public IEnumerable<ICollection<LatticeEdge>> _r_paths
			{
				get
				{
					bool f_any = false;
					foreach (var ic in _target.RightwardsPaths)
					{
						yield return ic.Prepend(this);
						f_any = true;
					}
					if (!f_any)
						yield return new[] { this };
				}
			}
#endif
			public override String ShortDisplay()
			{
				var s = lat.GetItemDisplay(this);
				if (String.IsNullOrWhiteSpace(s))
					return StringId;
				return String.Format("[{0} {1}]", StringId, s);
			}

			public override String LongDisplay()
			{
				return String.Format("{0,30} -{1}- {2}",
					_source == null ? "(null)" : _source.ShortDisplay(),
					this.ShortDisplay().PadCenter(35, '-'),
					_target == null ? "(null)" : _target.ShortDisplay());
			}

			public int CompareTo(LatticeEdge other)
			{
				var d = this.Source.CompareTo(other.Source);
				if (d != 0)
					return d;
				return this._id - other._id;
			}
		};

		//[DebuggerDisplay("{VertexCount} verticies")]
		//public LatticeVertex[] VERTICIES
		//{
		//	get
		//	{
		//		return Vertices.OrderBy(v => v.Level).ToArray();
		//		//return Vertices;
		//	}
		//}

		//[DebuggerDisplay("{_edges_dbg.Length} edges", Name = "EDGES")]
		//LatticeEdge[] _edges_dbg { get { return Edges.ToArray(); } }


#if false

		public struct LatticeSpan
		{
			public LatticeSpan(LatticeVertex v1, LatticeVertex v2)
			{
				Debug.Assert(v1.Level <= v2.Level);
				this.v1 = v1;
				this.v2 = v2;
			}
			readonly LatticeVertex v1;
			readonly LatticeVertex v2;
		};

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TOther"></typeparam>
		/// <param name="new_lattice">new_lattice is a newly created lattice to be populated</param>
		/// <param name="f"></param>
		public void ConvertLatticeEdges<TOther>(Lattice<TOther> new_lattice, Func<Lattice<T>.LatticeEdge, IEnumerable<TOther>> f)
		{
			Debug.Assert(new_lattice.EdgeCount == 0);

			var vertex_map = new Dictionary<Lattice<T>.LatticeVertex, Lattice<TOther>.LatticeVertex>();

			foreach (var v in this.Vertices)
			{
				if (v.rights.Length > 0 || v.lefts.Length > 0)
				{
					vertex_map[v] = new_lattice.NewVertex();
				}
			}

			new_lattice.StartVertex = vertex_map[this.StartVertex];
			new_lattice.EndVertex = vertex_map[this.EndVertex];

			foreach (var e in this.Edges)
			{
				foreach (var new_data in f(e))
					new_lattice.AddEdge(new_data, vertex_map[e.Source], vertex_map[e.Target]);
			}
		}


		public void CleanUpOrphans()
		{
			//this is horribly inefficient. Ideally should do one pass through marking all "good" edges that 
			//are on a path between start and end vertices, but it's a little tricky...
			foreach (var v in Vertices)
			{
				if (!IsAncestor(StartVertex, v) || !IsAncestor(v, EndVertex))
				{
					foreach (var e in v.lefts.Concat(v.rights))
						e.Remove();
				}

				Vertices = Vertices.Where(vx => StartVertex == vx || EndVertex == vx || vx.lefts.Length > 0).ToArray();
			}
			need_level_reset = true;
		}

		public bool CheckWellFormedness()
		{
			return (StartVertex != null && EndVertex != null && IsAncestor(StartVertex, EndVertex));

			//if (StartVertex == null || EndVertex == null)
			//	return false;

			//foreach (var v in Vertices)
			//{
			//	//orphaned vertices are allowed
			//	if (v.lefts.Length == 0 && v.rights.Length == 0)
			//		continue;

			//	if (!this.IsAncestor(StartVertex, v))
			//		return false;
			//	if (!this.IsAncestor(v, EndVertex))
			//		return false;
			//}

			//return true;
		}
#endif

#if PATHS
		//[DebuggerDisplay("{Paths.Length} paths", Name = "PATHS")]
		//[DebuggerBrowsable( DebuggerBrowsableState.Never)]
		//public LatticePath[] Paths
		//{
		//	get
		//	{
		//		if (StartVertex == null)
		//			return NoPaths;
		//		var arr = StartVertex.RightwardsPaths.Select(ic => new LatticePath(ic)).ToArray();
		//		if (arr.Length == 0)
		//			return NoPaths;
		//		return arr;
		//	}
		//}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public static readonly LatticePath[] NoPaths = Collection<LatticePath>.None;

		[DebuggerDisplay("{ToString(),nq}")]
		public class LatticePath : LatticeItem, _ICollection<LatticeEdge>
		{
			public LatticePath(Lattice<T> lat, IEnumerable<LatticeEdge> _ice)
				: base(lat)
			{
				this.rge = _ice as LatticeEdge[];
				if (rge == null)
				{
					this.rge = new LatticeEdge[_ice._Count()];
					var en = _ice.GetEnumerator();
					for (int i = 0; en.MoveNext(); i++)
						rge[i] = en.Current;
				}
				else if (rge.Length == 0)
				{
					this.rge = NoEdges;
					return;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			readonly LatticeEdge[] rge;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public LatticeEdge[] Rightwards { get { return rge; } }

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public IEnumerable<LatticeEdge> Leftwards { get { return this; } }

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int Count { get { return rge.Length; } }

			IEnumerator<LatticeEdge> IEnumerable<LatticeEdge>.GetEnumerator()
			{
				for (int i = rge.Length - 1; i >= 0; i--)
					yield return rge[i];
			}

			IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable<LatticeEdge>)this).GetEnumerator(); }
#if DEBUG
			public override String ToString()
			{
				if (rge.Length == 0)
					return "(empty path)";
				StringBuilder sb = new StringBuilder();
				LatticeEdge e_cur = null;
				foreach (var e in rge)
				{
					e_cur = e;
					sb.AppendFormat("{0} {1} ", e.Source.StringId, e.ToString());
				}
				sb.Append(e_cur.Target.StringId);
				return sb.ToString();
			}
#endif

			public void Add(LatticeEdge item) { throw not.impl; }
			public void Clear() { throw not.impl; }
			public bool Contains(LatticeEdge item) { throw not.impl; }
			public void CopyTo(LatticeEdge[] array, int arrayIndex) { throw not.impl; }
			public bool IsReadOnly { get { throw not.impl; } }
			public bool Remove(LatticeEdge item) { throw not.impl; }
			public void CopyTo(SysArray array, int index) { throw not.impl; }
			public bool IsSynchronized { get { throw not.impl; } }
			public object SyncRoot { get { throw not.impl; } }
		}
#endif
	};


	public class Trellis<T> : Lattice<T>
	{
		public Trellis()
		{
			var arr = new LatticeVertex[2];
			arr[0] = this.v_start = new start_vertex(this);
			arr[1] = this.v_end = new end_vertex(this);
			base.Vertices = arr;
		}

		public class start_vertex : LatticeVertex
		{
			public start_vertex(Trellis<T> trell) : base(trell) { }
		}
		public class end_vertex : LatticeVertex
		{
			public end_vertex(Trellis<T> trell) : base(trell) { }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly start_vertex v_start;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly end_vertex v_end;

		//[DebuggerDisplay("{alib.String._string_ext.CondenseSpaces(v_start.ToString()),nq}")]
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LatticeVertex StartVertex { get { return v_start; } }

		//[DebuggerDisplay("{alib.String._string_ext.CondenseSpaces(v_end.ToString()),nq}")]
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LatticeVertex EndVertex { get { return v_end; } }

		public LatticeVertex[] CreateVertexMap(int c)
		{
			int i;
			var rgvtx = new LatticeVertex[c];

			rgvtx[0] = StartVertex;
			rgvtx[--c] = EndVertex;

			for (i = 1; i < c; i++)
				rgvtx[i] = NewVertex();

			return rgvtx;
		}

	};
}
