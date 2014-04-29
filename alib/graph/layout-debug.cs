using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	using Array = System.Array;
	using String = System.String;
	using Enumerable = System.Linq.Enumerable;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public partial class level_info
	{
#if DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		debug_layout_entry[] _de;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		debug_layout_entry[] _debug_entries
		{
			get
			{
				var _tmp = _de;
				if (_tmp == null)
				{
					_tmp = new debug_layout_entry[arr.Length];
					for (int i = 0; i < arr.Length; i++)
						_tmp[i] = new debug_layout_entry(this, i);
					this._de = _tmp;
				}
				return _tmp;
			}
		}

		public void show_crossing_groups()
		{
			Debug.WriteLine("=== level " + i_level + " crossing groups (" + c_crossing_groups + ") ===");
			Debug.WriteLine(Enumerable.Range(0, crossing_groups.Length).Select(z => z.ToString("G3")).StringJoin(" "));
			Debug.WriteLine(crossing_groups.Select(z => z.ToString("G3")).StringJoin(" "));
		}

		[DebuggerDisplay("{_phys_order,nq}", Name = "Physical Order:")]
		String _phys_order { get { return arr.Select((e, ix) => String.Format("[{0}]V{1}", ix, (int)e.vr)).StringJoin(" "); } }
		[DebuggerDisplay("{PrevLevel==null?\"\":PrevLevel.ToString(),nq}", Name = "Previous Level")]
		level_info _y_prev { get { return PrevLevel; } }
		[DebuggerDisplay("{NextLevel==null?\"\":NextLevel.ToString(),nq}", Name = "Next Level")]
		level_info _z_next { get { return NextLevel; } }
#else
		[Conditional("DEBUG")]
		void _debug_init() { }
#endif

		public override String ToString()
		{
			return String.Format("L{0,-2} -- groups:{1} -- edges-up:{2,3} -- edges-dn:{3,3}{4}",
				i_level,
				c_crossing_groups.ToString().PadLeft(3) + "/" + this.Count.ToString().PadRight(3),
				c_edges_above,
				c_edges_below,
				NeedLayout ? " (layout)" : "");
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{lev.ToString(),nq}")]
	public abstract class debug_level
	{
		public debug_level(level_info lev)
		{
			this.lev = lev;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public level_info lev;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int i_level { get { return lev.i_level; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class debug_layout_entry : debug_level
	{
		public debug_layout_entry(level_info lev, int ix_phys)
			: base(lev)
		{
			Debug.Assert((uint)ix_phys < (uint)lev.arr.Length);
			this.ix_phys = ix_phys;
		}

		[DebuggerDisplay("[{ix_phys}]", Name = "Physical Index")]
		public readonly int ix_phys;

		[DebuggerDisplay("V{(int)vr}", Name = "Vertex")]
		public Vref vr { get { return lev.arr[ix_phys].vr; } }

		[DebuggerDisplay("{_phys_order,nq}", Name = "Physical Order:")]
		public String _phys_order { get { return lev.arr.Select((e, ix) => String.Format("[{0}]V{1}", ix, (int)e.vr)).StringJoin(" "); } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int[] Upper { get { return lev.arr[ix_phys].Upper; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int[] Lower { get { return lev.arr[ix_phys].Lower; } }

		[DebuggerDisplay("Count: {Upper.Length}", Name = "Upper")]
		debug_layout_entry[] y_upper { get { return _upper().ToArray(); } }

		[DebuggerDisplay("Count: {Lower.Length}", Name = "Lower")]
		debug_layout_entry[] z_lower { get { return _lower().ToArray(); } }

		IEnumerable<debug_layout_entry> _upper()
		{
			var arr = Upper;
			if (arr != null)
				for (int i = 0; i < arr.Length; i++)
					yield return new debug_layout_entry(lev.PrevLevel, arr[i]);
		}

		IEnumerable<debug_layout_entry> _lower()
		{
			var arr = Lower;
			if (arr != null)
				for (int i = 0; i < arr.Length; i++)
					yield return new debug_layout_entry(lev.NextLevel, arr[i]);
		}

		public override String ToString()
		{
			return String.Format("L{0,-3} {1,4} {2,-3}", this.i_level, "V" + (int)vr, ix_phys);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{_dbg_disp(),nq}")]
	public class debug_logical_entry : debug_layout_entry
	{
		public debug_logical_entry(logical_layout hyp, int i_level, int ix_phys)
			: base(hyp.levels[i_level], ix_phys)
		{
			this.hyp = hyp;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly logical_layout hyp;

		[DebuggerDisplay("{ix_logical}", Name = "Logical Index")]
		public int ix_logical { get { return hyp.phys2log(i_level)[ix_phys]; } }

		String map_above_display(int[] _upper)
		{
			return lev.IsTopLevel ? String.Empty : mappings(hyp.phys2log(i_level - 1), _upper);
		}
		String map_below_display(int[] _lower)
		{
			return lev.IsBottomLevel ? String.Empty : mappings(hyp.phys2log(i_level + 1), _lower);
		}
		static String mappings(int[] p2l, int[] arr_phys)
		{
			String s = String.Empty;
			for (int i = 0; i < arr_phys.Length; i++)
			{
				if (i > 0)
					s += " ";
				var ixp = arr_phys[i];
				s += String.Format("{0}→{1}", ixp, p2l[ixp]);
			}
			return s;
		}

		public String _dbg_disp()
		{
			var ixl = ix_logical;
			return String.Format("{0,-5} → {1,-2} {2} {{ {3,20} }} {4,4} {{ {5,-20} }}",
					"L" + i_level + "." + ix_phys,
					ixl,
					ixl == ix_phys ? " " : "*",
					map_above_display(lev.arr[ix_phys].Upper),
					"V" + (int)vr,
					map_below_display(lev.arr[ix_phys].Lower));
		}

		public override String ToString()
		{
			return _dbg_disp();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{hyp.log_level_display(i_level),nq}")]
	public class debug_logical_level : debug_level
	{
		public debug_logical_level(logical_layout hyp, level_info lev)
			: base(lev)
		{
			this.hyp = hyp;
		}
		public debug_logical_level(logical_layout hyp, int i_level)
			: this(hyp, hyp.levels[i_level])
		{
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly logical_layout hyp;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int[] p2l { get { return hyp.phys2log(i_level); } }

		public Vref vr(int ix_phys) { return lev.arr[ix_phys].vr; }

		public int ix_logical(int ix_phys) { var _tmp = p2l; return _tmp == null ? ix_phys : p2l[ix_phys]; }

		[DebuggerDisplay("{_phys_order,nq}", Name = "Physical Order:")]
		public debug_logical_entry[] _phys_order_map
		{
			get { return Enumerable.Range(0, lev.Count).Select(ix => new debug_logical_entry(hyp, i_level, ix)).ToArray(); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public String _phys_order
		{
			get { return lev.arr.Select((e, ix) => String.Format("[{0}]V{1}", ix, (int)e.vr)).StringJoin(" "); }
		}

		[DebuggerDisplay("{_log_order,nq}", Name = "Logical Order:")]
		public debug_logical_entry[] _log_order_map
		{
			get
			{
				return hyp[i_level].Select(ix_phys => new debug_logical_entry(hyp, i_level, ix_phys)).ToArray();
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		String _log_order
		{
			get
			{
				var l2p = hyp[i_level];
				var s = l2p.StringJoin(" ") + " -- ";
				s += l2p.Select(ix_phys => "V" + (int)lev.arr[ix_phys].vr).StringJoin(" ");
				return s;
			}
		}
	};
}