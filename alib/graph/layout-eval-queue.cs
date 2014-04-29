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
	using math = alib.Math.math;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class LayoutEvaluator : MinMaxSet2<eval_layout>
	{
		public static readonly IComparer<eval_layout> TotalCrossingsComparer;
		public static readonly IComparer<eval_layout> CrossingsPerDepthComparer;
		//public static readonly IComparer<eval_layout> AveragedCrossingsComparer;

		static LayoutEvaluator()
		{
			TotalCrossingsComparer = _cmp_total_crossings._create();
			CrossingsPerDepthComparer = _cmp_crossings_per_depth._create();
			//AveragedCrossingsComparer = _cmp_avg_crossings._create();
		}

		public LayoutEvaluator()
			: base(TotalCrossingsComparer)
		{
			this.q = new PriorityQueue<eval_layout>(CrossingsPerDepthComparer);
		}

		readonly PriorityQueue<eval_layout> q;

		public int QueueCount { get { return q.Count; } }

		public eval_layout Dequeue() { return q.RemoveMin(); }

		public new bool Add(eval_layout hyp)
		{
			if (!base.Add(hyp))
				return false;
			q.Add(hyp);
			return true;
		}

		sealed class _cmp_total_crossings : IComparer<eval_layout>
		{
			public static _cmp_total_crossings _create() { return new _cmp_total_crossings(); }
			_cmp_total_crossings() { }
			public int Compare(eval_layout x, eval_layout y)
			{
				return x.total_crossings() - y.total_crossings();
			}
		};
		sealed class _cmp_crossings_per_depth : IComparer<eval_layout>
		{
			public static _cmp_crossings_per_depth _create() { return new _cmp_crossings_per_depth(); }
			_cmp_crossings_per_depth() { }
			public int Compare(eval_layout x, eval_layout y)
			{
				var k = x.CrossingsPerDepth - y.CrossingsPerDepth;
				return k <= math._ε ? -1 : k >= math.ε ? 1 : 0;
				//return (int)(x.CrossingsPerDepth * x.total_crossings()) -
				//		(int)(y.CrossingsPerDepth * y.total_crossings());
			}
		};
		//sealed class _cmp_avg_crossings : IComparer<eval_layout>
		//{
		//	public static _cmp_avg_crossings _create() { return new _cmp_avg_crossings(); }
		//	_cmp_avg_crossings() { }
		//	public int Compare(eval_layout x, eval_layout y)
		//	{
		//		if (x.GetHashCode() == y.GetHashCode() && x.Equals(y))
		//			return 0;

		//		eval_layout cur;
		//		int tot1 = 0, tot2 = 0;

		//		cur = x;
		//		do
		//			tot1 += cur.total_crossings();
		//		while ((cur = cur.hyp_prev) != null);

		//		cur = y;
		//		do
		//			tot2 += cur.total_crossings();
		//		while ((cur = cur.hyp_prev) != null);

		//		return (tot1 / x.depth) - (tot2 / y.depth);
		//	}
		//};
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class eval_layout : logical_layout
#if ACTIVE_LEVEL_EQUATABLE
, IEquatable<eval_layout>
#endif
	{
		public eval_layout(level_infos levels)
			: base(levels)
		{
			this.depth = 1;
			this.prv_cc = -1;
			this.i_active_level = -1;
		}
		public eval_layout(eval_layout to_copy)
			: base(to_copy)
		{
			//this.hyp_prev = to_copy;

			this.prv_cc = to_copy.c_crossings;
			this.prv_cc_tot = to_copy.cc_tot;

			this.depth = to_copy.depth + 1;
			this.i_active_level = -1;
			this.cc_tot = to_copy.cc_tot;
		}

		public int depth
			, i_active_level
			//, dir
			;
		public int cc_tot;

		//public eval_layout hyp_prev;
		int prv_cc;
		int prv_cc_tot;

		//public Double CrossingsPerDepth { get { return cc_tot / System.Math.Sqrt(depth); } }		// same as...
		//public Double CrossingsPerDepth { get { return cc_tot / System.Math.Sqrt(depth * levels.Count); } }		// 122 @119
		//public Double CrossingsPerDepth { get { return (cc_tot * levels.Count) / System.Math.Sqrt(depth); } }	// 122 @119
		//public Double CrossingsPerDepth { get { return (cc_tot / levels.Count) / System.Math.Sqrt(depth); } }	// 122 @119
		//public Double CrossingsPerDepth { get { return cc_tot / System.Math.Sqrt(depth * 2); } }	// 122 @119
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log10(depth + 1)); } }	// 127
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(depth + 1)); } }	// 119
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(depth * levels.Count)); } }	// 123 @293 fast to get good min
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(depth * depth)); } }	// 129 @629
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(1.0 / total_crossings())); } }	// 124 @4851 looks good but way too depth-y
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log10(1.0 / total_crossings())); } }	// try
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) / System.Math.Log(total_crossings())); } }	// 169 @94 too conservative?
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Log(depth) / total_crossings()); } }	// 167 @88 too conservative?
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() / System.Math.Log(depth, levels.Count); } }	// 167 @88
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() / System.Math.Log10(depth * levels.Count); } }	// try
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() / System.Math.Sqrt(depth * total_crossings()); } }	// 162 @110 promising breadth but too slow
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() / System.Math.Log(depth * total_crossings()); } }	// 176 @85 broad, consistent
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() / System.Math.Log(total_crossings() / (double)depth); } }	// 155 @39 not deep enough
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() * System.Math.Sqrt(total_crossings() / (double)depth); } }	// 
		//public Double CrossingsPerDepth { get { return total_crossings() * System.Math.Sqrt(cc_tot / (double)depth); } }	// 126 @479 super fast to 127... then nothing, wasting time at depth 1200+
		//public Double CrossingsPerDepth { get { return total_crossings() * System.Math.Log(cc_tot / (double)depth); } }	// same as sqrt?
		//public Double CrossingsPerDepth { get { return System.Math.Log(total_crossings()) * (cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(depth + 1))); } }	// 125 @465
		//public Double CrossingsPerDepth { get { return System.Math.Pow(total_crossings(), 2.0) / (cc_tot / (double)depth); } }	// 126 @165
		//public Double CrossingsPerDepth { get { return -System.Math.Pow(cc_tot / (double)depth, 1.414) / total_crossings(); } }	// 178 @65
		//public Double CrossingsPerDepth { get { return -System.Math.Pow(cc_tot / (double)depth, 2.0) / total_crossings(); } }	// 119 @90
		//public Double CrossingsPerDepth { get { return total_crossings() / System.Math.Pow(cc_tot / (double)depth, 2.1); } }	// try
		//public Double CrossingsPerDepth { get { return -System.Math.Pow(cc_tot / (double)depth, 0.5) / total_crossings(); } }	// 126 @165
		//public Double CrossingsPerDepth { get { return System.Math.Pow(cc_tot / (double)depth, 2.0) * total_crossings(); } }	// 195 @752 ??
		//public Double CrossingsPerDepth { get { return cc_tot * total_crossings() / System.Math.Log((double)depth / total_crossings()); } }	//  try
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(total_crossings())); } }	//...267 not working near boundary
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * total_crossings()); } }	//  ...464 not working near boundary
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * (1.0 / total_crossings())); } }	// 161 @166 nice looking but current score too powerful, trapped in local
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(depth)); } }	// 129 @629
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log10(depth)); } }	// 129 @629
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(depth + 1, levels.Count)); } } // 127
		//public Double CrossingsPerDepth { get { return (cc_tot / System.Math.Log(depth)) / System.Math.Sqrt(depth); } }		// 127, more thorough, cautious than (depth+1)
		//public Double CrossingsPerDepth { get { return (cc_tot / System.Math.Log10(depth)) / System.Math.Sqrt(depth); } }		// 131, log10 not as agressive
		//public Double CrossingsPerDepth { get { return (cc_tot / System.Math.Log(depth)) / System.Math.Sqrt(depth); } }	
		//public Double CrossingsPerDepth { get { return cc_tot / (System.Math.Sqrt(depth) * System.Math.Log(levels.Count)); } }
		//public Double CrossingsPerDepth { get { return cc_tot / System.Math.Log10(depth + 1); } }

		public Double CrossingsPerDepth
		{
			// 119 @90
			get
			{
				double cpd = cc_tot / (double)depth;

				return total_crossings() / (cpd * cpd);
			}
		}

		protected override void crossings_changed(int sum)
		{
			this.cc_tot = prv_cc_tot + sum;
		}

#if ACTIVE_LEVEL_EQUATABLE
		///////////////////////////////////////////////////////////
		///
		public override bool Equals(object obj)
		{
			return obj is eval_layout && Equals((eval_layout)obj);
		}
		public bool Equals(eval_layout other)
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
#endif

		//public void dump_ops()
		//{
		//	int i = depth;
		//	var _tmp = new logical_layout[i];
		//	var cur = this;
		//	do
		//		_tmp[--i] = cur;
		//	while ((cur = cur.hyp_prev) != null);

		//	Debug.WriteLine("");
		//	for (i = 0; i < depth; i++)
		//	{
		//		Debug.WriteLine(_tmp[i].ToString());
		//		//Debug.Write(_tmp[i].report());
		//	}
		//}

		//String s_op()
		//{
		//	if (dir == -1)
		//		return (i_active_level - 1) + "▼";
		//	else if (dir == 1)
		//		return (i_active_level + 1) + "▲";
		//	return "";
		//}

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


#if false
		PriorityQueue<eval_layout> q;

		int qCount { get { return q.Count; } }

		PriorityQueue<eval_layout> qClear()
		{
			//return new PriorityQueue<eval_layout>(eval_layout.TotalCrossingsComparer);
			return new PriorityQueue<eval_layout>(eval_layout.CrossingsPerDepthComparer);
			//return new PriorityQueue<logical_layout>(logical_layout.AveragedCrossingsComparer);
		}
		void qEnqueue(eval_layout hyp)
		{
			q.Add(hyp);
		}
		eval_layout qDequeue()
		{
			return q.RemoveMin();
		}
#elif false
		SortedList<Double, List<eval_layout>> q;
		int qCount { get { return q.Count; } }
		SortedList<Double, List<eval_layout>> qClear()
		{
			if (q == null)
				q = new SortedList<Double, List<eval_layout>>();
			else
				q.Clear();
			return q;
		}
		void qEnqueue(eval_layout hyp)
		{
			Double v = -hyp.CrossingsPerDepth;
			int ix = q.IndexOfKey(v);
			if (ix == -1)
				q.Add(v, new List<eval_layout> { hyp });
			else
				q.Values[ix].Add(hyp);
		}
		eval_layout qDequeue()
		{
			int j, i;
			var lst = q.Values[i = q.Count - 1];
			var hyp = lst[j = lst.Count - 1];
			if (j == 0)
				q.RemoveAt(i);
			else
				lst.RemoveAt(j);
			return hyp;
		}
#elif false
		SortedList<int, List<eval_layout>> q;
		int qCount { get { return q.Count; } }
		SortedList<int, List<eval_layout>> qClear()
		{
			if (q == null)
				q = new SortedList<int, List<eval_layout>>();
			else
				q.Clear();
			return q;
		}
		void qEnqueue(eval_layout hyp)
		{
			int x = hyp.total_crossings();
			int d = hyp.depth;
			//int v = (1000 * (hyp.cc_tot / d)) / (x * x);	seems very high quality, has integer binning

			int ix = q.IndexOfKey(v);
			if (ix == -1)
				q.Add(v, new List<eval_layout> { hyp });
			else
				q.Values[ix].Add(hyp);
		}
		eval_layout qDequeue()
		{
			int j, i;
			var lst = q.Values[i = q.Count - 1];
			var hyp = lst[j = lst.Count - 1];
			if (j == 0)
				q.RemoveAt(i);
			else
				lst.RemoveAt(j);
			return hyp;
		}
#elif false
		Queue<eval_layout> q;
		int qCount { get { return q.Count; } }
		Queue<eval_layout> qClear()
		{
			if (q == null)
				q = new Queue<eval_layout>();
			else
				q.Clear();
			return q;
		}
		void qEnqueue(eval_layout hyp) { q.Enqueue(hyp); }
		eval_layout qDequeue() { return q.Dequeue(); }
#endif
}
