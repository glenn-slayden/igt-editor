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

namespace alib.dg
{
	using Array = System.Array;
	using String = System.String;
	using Enumerable = System.Linq.Enumerable;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class layout_graph
	{
		int depth_limit;
		int min_seen;
		int __last_update;
		int c_iter;
		int min_all;

		LayoutEvaluator evaluator;

		public IEnumerable<logical_layout> find_layouts()
		{
			IEnumerable<logical_layout> results;
			if (max_iter == 0)
			{
				results = new[] { new logical_layout(levels) };
			}
			else
			{
#if !REPORT
				results = engine_setup(levels);
#else
				var sw = Stopwatch.StartNew();

				var results = find_layouts(levels);

				sw.Stop();
				Debug.WriteLine("elapsed: {0:#,###}", sw.ElapsedTicks);

				Debug.WriteLine("\r\n=== RESULTS ===");
				int i = 0;
				foreach (var hyp in results)
				{
					Debug.Print("#{0,-3} {1}", i++, hyp.report().Trim());
				}
				Debug.WriteLine("===============");
				Debug.WriteLine("");
#endif
			}
			return results;
		}

		bool add_hyp(eval_layout hyp, int i_active_level)//, int dir)
		{
			//if (hyp.depth >= depth_limit)
			//	return false;
			hyp.i_active_level = i_active_level;
			//hyp.dir = dir;
			if (!evaluator.Add(hyp))
				return false;
			int c;
			if ((c = hyp.total_crossings()) < min_all)
				min_all = c;
			return true;
		}

		IEnumerable<logical_layout> engine_setup(level_infos levels)
		{
			this.min_all = int.MaxValue;
			this.min_seen = int.MaxValue;
			this.depth_limit = levels.Count * levels.Count;
			//this.depth_limit = (int)System.Math.Pow(LevelCount, 1.5);

			var initial_layout = new eval_layout(levels);

			this.evaluator = new LayoutEvaluator();

			//for (int j = 1; j < LevelCount; j++)
			//	initial_layout.set_leaf_positions_from_above(j);

#if REPORT
			Debug.WriteLine(initial_layout.report());
#endif

			//return new[] { initial_layout };

			//Debug.WriteLine("=== initial layouts ===");
			//foreach (var x in q)
			//	Debug.WriteLine(x.ToString());
			//Debug.WriteLine("");

			//for (int j = 1; j < LevelCount; j++)
			//{
			//	var copy = new eval_layout(initial_layout);
			//	add_hyp(copy, j);

			//	//if (copy.set_leaf_positions_from_above(j))
			//	//{
			//	//	add_hyp(copy, j);
			//	//	copy = new eval_layout(initial_layout);
			//	//}
			//}
			add_hyp(initial_layout, levels.Count / 2);//, 0);
			//add_hyp(initial_layout, 0, eval_op.Start);
			//add_hyp(initial_layout, LevelCount - 1, eval_op.Start);

			eval_loop();

			/// interesting case: 'levels' is a comparer for a supertype of the sequence type, but if you
			/// leave off the type argument on the linq function, C# type inference uses the comparer item 
			/// type--rather than the sequence type--for inference even though the comparer type is 
			/// marked contravariant. 
			var ret = evaluator.Mins.Distinct<logical_layout>(levels);

			return ret;
		}

		void eval_loop()
		{
			int c;

			//while ((c = layouts.Count) > 0 && min_all > 0 && qCount > 0 && c < 2000000)
			//while ((c = layouts.Count) > 0 && min_all > 0 && qCount > 0)
			while ((c = evaluator.Count) > 0 && min_all > 0 && evaluator.QueueCount > 0 && c < max_iter)
			{
				var d_mem = alib.Memory.Kernel32.MemoryStatus.ullAvailPhys / alib.Int._int_ext.GigabyteD;
				if (d_mem < 1.6)
					break;

				var hyp = evaluator.Dequeue();

				var c_cur = hyp.total_crossings();

				//if (hyp.levels.Count == 19 && c_cur == 108)
				//	break;

#if REPORT
				bool __f_report;
				if (__f_report = c_cur < min_seen)
					min_seen = c_cur;

				if (__f_report || qCount == 0 || (c_iter - __last_update) % 10000 == 0)
				{
					Debug.Print("{0,8} -- cur:{1,4} {2,5} -- min-seen:{3,4} -- min-all:{4,4} -- d-max:{5,4} -- q:{6,7} -- tot:{7,8} -- mem:{8,6:#.00}",
						c_iter,
						c_cur,
						"@" + hyp.depth,
						min_seen,
						min_all,
						eval_layout.max_depth,
						qCount,
						c,
						d_mem);

					__last_update = (c_iter / 10000 + 1) * 10000;
				}
#endif
				c_iter++;

				start_hyp(hyp, hyp.i_active_level);
			}
		}

		void start_hyp(eval_layout hyp, int i)
		{
			eval_layout copy;

			copy = new eval_layout(hyp);

			if (hyp.total_crossings() <= min_seen + 5)
			{
				bool f_any = false;
				for (int j = 1; j < levels.Count; j++)
					f_any |= copy.set_leaf_positions_from_above(j);

				if (f_any)
				{
					add_hyp(copy, i);
					copy = new eval_layout(hyp);
				}
			}

			if (copy.set_medians_from_above(i + 1))
			{
				add_hyp(copy, i + 1);
				copy = new eval_layout(hyp);
			}

			if (copy.set_medians_from_below(i - 1))
			{
				add_hyp(copy, i - 1);
				copy = new eval_layout(hyp);
			}

			if (copy.set_medians_from_above(i - 1))
			{
				add_hyp(copy, i - 1);
				copy = new eval_layout(hyp);
			}

			if (copy.set_medians_from_below(i + 1))
			{
				add_hyp(copy, i + 1);
				copy = new eval_layout(hyp);
			}
		}
	};
}
