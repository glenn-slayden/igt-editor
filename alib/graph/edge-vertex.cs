using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using alib.Debugging;
using alib.Enumerable;
using alib.Hashing;

namespace alib.dg
{
#if DEBUG
	using alib.dg.debug;
#endif
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public partial class dg_base
	{
		[DebuggerDisplay("{ToString(dg._singleton),nq}", Type = "dg.Edge")]
		[StructLayout(LayoutKind.Explicit, Size = 16)]
		public struct Edge
		{
			[FieldOffset(0), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Vref v_from;
			[FieldOffset(0), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int prv;

			[FieldOffset(4), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Vref v_to;
			[FieldOffset(4), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int nxt;

			[FieldOffset(8), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Eref e_next;
			[FieldOffset(8), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public status status;

			[FieldOffset(12), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int value;
			[FieldOffset(12), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int block_size;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool IsFree { [DebuggerStepThrough] get { return (status & status.free) == status.free; } }
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool HasSize { [DebuggerStepThrough] get { return (status & status.fe_HasSize) == status.fe_HasSize; } }
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool HasList { [DebuggerStepThrough] get { return (status & status.fe_HasList) == status.fe_HasList; } }
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool IsBlockSingleton { [DebuggerStepThrough] get { return (status & status.fe_BlockSingleton) == status.fe_BlockSingleton; } }
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool IsBlockStart { [DebuggerStepThrough] get { return (status & status.fe_BlockStart) == status.fe_BlockStart; } }
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool IsBlockEnd { [DebuggerStepThrough] get { return (status & status.fe_BlockEnd) == status.fe_BlockEnd; } }

			public void reset(status s, int block_size = 0)
			{
				this.status = s;
				this.prv = this.nxt = (int)status.NotInFreelist;
				this.block_size = block_size;
			}

#if DEBUG
			public unsafe Eref _discover_eref()
			{
				Edge[] arr;
				if (dg._singleton != null && (arr = dg._singleton.E) != null)
				{
					long d = -1;
					fixed (Edge* _pe = &arr[0], _this = &this)
						d = _this - _pe;
					if ((uint)d < (uint)arr.Length)
						return new Eref((int)d);
				}
				return Eref.NotValid;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public eent_dbg _dbg
			{
				[DebuggerStepThrough]
				get
				{
					var g = dg._singleton;
					return g != null ? new eent_dbg(g, _discover_eref()) : new eent_dbg(null, Eref.NotValid);
				}
			}
			public String ToString(IGraphRaw g)
			{
				Eref er;
				if (g == null || ((er = _discover_eref()) < 0))
					return String.Format("v_from:{0} v_to:{1} e_next:{2} value:{3}",
						(int)v_from,
						(int)v_to,
						(int)e_next,
						value);
				return new eent_dbg(g, er).ToString();
			}

			public override String ToString() { return ToString(dg._singleton); }
#endif
		};

		public struct EdgeRange
		{
			public static readonly EdgeRange NotValid = new EdgeRange(Eref.NotValid, 0);

			public EdgeRange(Eref _start, int _length)
			{
				//Debug.Assert(_length >= 0 && (_start == Eref.NotValid) == (_length == 0));
				this.ix = _start;
				this.c = _length;
			}
			public readonly Eref ix;
			public readonly int c;

			public bool IsValid { get { return ix >= 0; } }

			public bool Contains(Eref e)
			{
				if (!IsValid)
					throw new Exception();
				return ix <= e && e < ix + c;
			}

			public Eref Last
			{
				get
				{
					if (!IsValid)
						throw new Exception();
					return ix + (c - 1);
				}
			}

			public Eref Next
			{
				get
				{
					if (!IsValid)
						throw new Exception();
					return ix + c;
				}
			}
			public Eref PrevEnd
			{
				get
				{
					if (!IsValid)
						throw new Exception();
					return ix + (-1);
				}
			}

			public static int operator -(EdgeRange a, EdgeRange b)
			{
				if (!a.IsValid || !b.IsValid)
					throw new Exception();
				return (int)a.ix - (int)b.ix;
			}
		};

		[DebuggerDisplay("{ToString(dg._singleton),nq}", Type = "dg.Vertex")]
		[StructLayout(LayoutKind.Explicit, Size = 16)]
		public struct Vertex
		{
			[FieldOffset(0), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Eref e_in;
			[FieldOffset(0), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int prv;

			[FieldOffset(4), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Eref e_out;
			[FieldOffset(4), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int nxt;

			[FieldOffset(8), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int c_out;
			[FieldOffset(8), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public status status;

			[FieldOffset(12), DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int value;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Eref e_last { get { return e_out + c_out; } }

			public EdgeRange OutEdgeRange
			{
				get { return new EdgeRange(e_out, c_out); }
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Eref[] out_edges
			{
				get
				{
					if (c_out == 0)
						return NoErefs;
					var arr = new Eref[c_out];
					var e = e_out;
					for (int i = 0; i < c_out; i++, e++)
						arr[i] = e;
					return arr;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool IsLeaf { [DebuggerStepThrough] get { return c_out == 0; } }

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public bool IsRoot { [DebuggerStepThrough] get { return e_in == Eref.None; } }

#if DEBUG

			public unsafe Vref _discover_vref()
			{
				Vertex[] arr;
				if (dg._singleton != null && (arr = dg._singleton.V) != null)
				{
					long d = -1;
					fixed (Vertex* _pv = &arr[0], _this = &this)
						d = _this - _pv;
					if ((uint)d < (uint)arr.Length)
						return new Vref((int)d);
				}
				return Vref.NotValid;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public vent_dbg _dbg
			{
				[DebuggerStepThrough]
				get
				{
					var g = dg._singleton;
					return g != null ? new vent_dbg(g, _discover_vref()) : new vent_dbg(null, Vref.NotValid);
				}
			}

			public String ToString(IGraphRaw g)
			{
				Vref vr;
				if (g == null || ((vr = _discover_vref()) < 0))
					return String.Format("e_in:{0} e_out:{1} c_out:{2} value:{3}",
						e_in == Eref.None ? "Eref.None" : ((int)e_in).ToString(),
						e_out == Eref.NotValid ? "Eref.NotValid" : ((int)e_out).ToString(),
						(int)c_out,
						value);
				return new vent_dbg(g, vr).ToString();
			}

			public override String ToString() { return ToString(dg._singleton); }
#endif
		};
	};

}
