using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using alib.Array;
using alib.Debugging;
using alib.Enumerable;
using alib.Hashing;
using alib.priority;

namespace alib.Wpf
{
	using math = alib.Math.math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class _LayoutEvaluator : MinMaxSet2<_eval_layout>, IEqualityComparer<_logical_layout>
	{
		public static readonly IComparer<_eval_layout> TotalCrossingsComparer;
		public static readonly IComparer<_eval_layout> CrossingsPerDepthComparer;
		//public static readonly IComparer<_eval_layout> AveragedCrossingsComparer;

		static _LayoutEvaluator()
		{
			TotalCrossingsComparer = _cmp_total_crossings._create();
			CrossingsPerDepthComparer = _cmp_crossings_per_depth._create();
			//AveragedCrossingsComparer = _cmp_avg_crossings._create();
		}

		public _LayoutEvaluator(LogLevelInfo[] levels)
			: base(TotalCrossingsComparer)
		{
			this.levels = levels;
			this.q = new PriorityQueue<_eval_layout>(CrossingsPerDepthComparer);
		}

		LogLevelInfo[] levels;

		readonly PriorityQueue<_eval_layout> q;

		public int QueueCount { get { return q.Count; } }

		public _eval_layout Dequeue() { return q.RemoveMin(); }

		public new bool Add(_eval_layout hyp)
		{
			if (!base.Add(hyp))
				return false;
			q.Add(hyp);
			return true;
		}

		sealed class _cmp_total_crossings : IComparer<_eval_layout>
		{
			public static _cmp_total_crossings _create() { return new _cmp_total_crossings(); }
			_cmp_total_crossings() { }
			public int Compare(_eval_layout x, _eval_layout y)
			{
				return x.total_crossings() - y.total_crossings();
			}
		};
		sealed class _cmp_crossings_per_depth : IComparer<_eval_layout>
		{
			public static _cmp_crossings_per_depth _create() { return new _cmp_crossings_per_depth(); }
			_cmp_crossings_per_depth() { }
			public int Compare(_eval_layout x, _eval_layout y)
			{
				var k = x.CrossingsPerDepth - y.CrossingsPerDepth;
				return k <= math._ε ? -1 : k >= math.ε ? 1 : 0;
				//return (int)(x.CrossingsPerDepth * x.total_crossings()) -
				//		(int)(y.CrossingsPerDepth * y.total_crossings());
			}
		};

		/////////////////////////////////////////////////////////////
		/// 
		bool IEqualityComparer<_logical_layout>.Equals(_logical_layout x, _logical_layout y)
		{
			if (x == y)
				return true;
			if (x.Count != y.Count)
				return false;

			for (int i_level = 0; i_level < Count; i_level++)
				if (!levels[i_level].Equals(x.L2P[i_level], y.L2P[i_level]))
					return false;
			return true;
		}

		int IEqualityComparer<_logical_layout>.GetHashCode(_logical_layout obj)
		{
			int h = base.GetHashCode();
			for (int i_level = 0; i_level < obj.Count; i_level++)
				h ^= levels[i_level].GetHashCode(obj.L2P[i_level]);
			if (h == 0)
				h = 1;
			return h;
		}
		/// 
		/////////////////////////////////////////////////////////////
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public unsafe class _eval_layout : _logical_layout, IEquatable<_eval_layout>
	{
		public _eval_layout(WpfGraphAdapter wga)
			: base(wga)
		{
			this.depth = 1;
			this.prv_cc = -1;
			this.i_active_level = -1;
		}

		public _eval_layout(_eval_layout to_copy)
			: base(to_copy)
		{
			//this.hyp_prev = to_copy;

			this.prv_cc = to_copy.c_crossings;
			this.prv_cc_tot = to_copy.cc_tot;

			this.depth = to_copy.depth + 1;
			this.i_active_level = -1;
			this.cc_tot = to_copy.cc_tot;
		}

		public int depth;
		public int i_active_level;
		public int cc_tot;

		//public eval_layout hyp_prev;
		int prv_cc;
		int prv_cc_tot;

		public Double CrossingsPerDepth
		{
			get
			{
				double cpd = cc_tot / (double)depth;
				return total_crossings() / (cpd * cpd);
			}
		}

		///////////////////////////////////////////////////////////
		///
		public override bool Equals(object obj)
		{
			return obj is _eval_layout && Equals((_eval_layout)obj);
		}
		public bool Equals(_eval_layout other)
		{
			if ((Object)this == (Object)other)
				return true;
			return other != null && i_active_level == other.i_active_level && base.Equals(other);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() ^ i_active_level;
		}
		///
		///////////////////////////////////////////////////////////

		public override String ToString()
		{
			var s_delta = prv_cc == -1 ? "" : (c_crossings - prv_cc).ToString();
			return String.Format("{0}  {1,6}  level:{2,3} {3,3}  depth:{4,4} {5,5} {6,6:N2}",
				base.ToString(),
				s_delta,
				i_active_level,
				"",//s_op(),
				depth,
				cc_tot,
				CrossingsPerDepth);
		}
	};
}