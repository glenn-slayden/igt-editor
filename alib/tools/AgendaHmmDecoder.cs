//#define VERBOSE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using alib.Dictionary;
using alib.Collections;
using alib.Hashing;
using alib.Enumerable;
using alib.Debugging;
using alib.priority;
using alib.Array;
using alib.Math;

namespace alib.Hmm
{
	using String = System.String;
	using Math = System.Math;
	using Array = System.Array;

	public sealed class TrellisCell
	{
		public static readonly TrellisCell BOSCell;

		static TrellisCell()
		{
			BOSCell = new TrellisCell(Bitag.EOSEOS, 0.0, 0);
			BOSCell.cache.Add(TrellisData.StartNode);
		}

		public TrellisCell(Bitag bitag, Double emit_logprob, int agenda_size)
		{
			this.bitag = bitag;
			this.emit_logprob = emit_logprob;
			this.agenda = new PriorityQueue<TrellisData>(agenda_size);
			this.cache = new List<TrellisData>();
		}

		public Bitag bitag;
		public readonly Double emit_logprob;
		public readonly PriorityQueue<TrellisData> agenda;
		public readonly List<TrellisData> cache;

		public String Info(StringIndex unitags)
		{
			return String.Format("{0} {1} = {2} -> {3} {4}",
									unitags[bitag.T1], unitags[bitag.T2],
									agenda.First().logprob,
									unitags[agenda.First().path.bitag.T1],
									unitags[agenda.First().path.bitag.T2]);
		}
	};

	public sealed class TrellisData : IComparable<TrellisData>
	{
		public static readonly TrellisData StartNode;

		static TrellisData()
		{
			StartNode = new TrellisData(0.0, null, 0);
		}

		public TrellisData(Double logprob, TrellisCell path, int path_idx)
		{
			this.logprob = logprob;
			this.path = path;
			this.path_idx = path_idx;
		}

		public readonly TrellisCell path;
		public Double logprob;
		public int path_idx;

		public int CompareTo(TrellisData other)
		{
			var k = this.logprob - other.logprob;
			return k <= math._ε ? -1 : k >= math.ε ? 1 : 0;
		}

		public override String ToString()
		{
			return String.Format("{0,6} {1} {2}, {3}", this.logprob, path.bitag.T1, path.bitag.T2, path_idx);
		}

#if HMM_DEBUGGING
		public int tag;
		public override String ToString()
		{
			StringIndex ut = ((StringHmm)_this).unitags;
			return String.Format("{0,6} {1} {2,4}", prob, path.ToString(), ut[tag]);
		}
#endif
	};

	public sealed class TrellisColumn
	{
		public TrellisColumn(int num_tags)
		{
			// note: not pre-initializing sub-arrays in 'cells' so that 'null' can indicate first use, 
			// allowing us to prevent adding duplicates to the list of non-null T2s

			this.cells = new TrellisCell[num_tags][];
			this.nonNullT2s = new RefList<int>(num_tags);
		}

		readonly TrellisCell[][] cells;
		readonly RefList<int> nonNullT2s;

		TrellisCell this[int T1, int T2]
		{
			get { return cells[T2][T1]; }
			set
			{
				TrellisCell[] rgtc;
				if ((rgtc = cells[T2]) == null)
				{
					cells[T2] = rgtc = new TrellisCell[cells.Length];
					nonNullT2s.Add(T2);
				}
				rgtc[T1] = value;
			}
		}

		public TrellisCell this[Bitag bitag]
		{
			get { return this[bitag.T1, bitag.T2]; }
			set { this[bitag.T1, bitag.T2] = value; }
		}

		public bool ContainsBitag(Bitag bitag)
		{
			return this[bitag] != null;
		}

		public IReadOnlyList<int> ValidPrevTags
		{
			get { return nonNullT2s; }
		}

		public IEnumerable<TrellisCell> GetCellsForPrevTag(int prev_tag)
		{
			TrellisCell[] _tmp;
			if ((_tmp = cells[prev_tag]) != null)
				for (int i = 0; i < _tmp.Length; i++)
					if (_tmp[i] != null)
						yield return _tmp[i];
		}

		public IEnumerable<TrellisCell> AllCells
		{
			get
			{
				for (int i = 0; i < cells.Length; i++)
					foreach (var tc in GetCellsForPrevTag(i))
						yield return tc;
			}
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public struct HmmPath : IReadOnlyList<int>
	{
		public HmmPath(StringIndex si, Double p, int c)
		{
			this.si = si;
			this.p = p;
			this.path = new int[c];
		}
		StringIndex si;
		public Double p;
		public int[] path;
		public int Count { get { return path.Length; } }

		public int this[int index]
		{
			get { return path[index]; }
			set { if (path != null) path[index] = value; }
		}

		public IEnumerator<int> GetEnumerator() { return path.Enumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public override String ToString()
		{
			StringIndex _si = si;
			Double _p = p;

			return String.Format("{0} = {1,12:G5}",
				path.Select(i_tag => _si[i_tag].PadLeft(5)).StringJoin(String.Empty),
				_p);
		}
	};


	public class AgendaHmmDecoder
	{
		public AgendaHmmDecoder(StringHmm hmm, String[] tokens)
		{
			this._hmm = hmm;
			this._tokens = tokens;
			this.agenda_size = hmm.unitags.Count * hmm.unitags.Count;
			this.trans_logprob_cache = new Dictionary<Tritag, Double>();

			initialize_trellis(hmm.convertInputToItemInfos(tokens), out this.trellis);
		}

		readonly StringHmm _hmm;
		readonly String[] _tokens;
		readonly int agenda_size;
		readonly TrellisColumn[] trellis;
		readonly Dictionary<Tritag, Double> trans_logprob_cache;

		const String EOS = "EOS";
		const String OOV = "OOV";

		void initialize_trellis(StringHmm.ItemInfo[] input, out TrellisColumn[] rgtc)
		{
			int c = input.Length + 2;
			rgtc = new TrellisColumn[c];

			for (int i = 0; i < c; i++)
				rgtc[i] = new TrellisColumn(_hmm.unitags.Count);

			rgtc[0][Bitag.EOSEOS] = TrellisCell.BOSCell;

			for (int i = 0; i < input.Length; i++)
				populate_next_column(input[i], i);

			rgtc[--c][Bitag.EOSEOS] = populate_trellis_cell(default(StringHmm.ItemInfo), Bitag.EOSEOS, trellis[--c], true);
		}

		void populate_next_column(StringHmm.ItemInfo item, int i_col)
		{
			for (int tag = 2; tag < _hmm.p_tree.Length; tag++)
			{
				foreach (int last_tag in trellis[i_col].ValidPrevTags)
				{
					var bitag = new Bitag(last_tag, tag);
					var tc = populate_trellis_cell(item, bitag, trellis[i_col], false);
					if (tc != null)
						trellis[i_col + 1][bitag] = tc;
				}
			}
		}

		Double get_emission_prob(StringHmm.ItemInfo item, Bitag tags, bool isEOS)
		{
			if (isEOS)
				return 1.0;

			Double emit_prob;

			if (item.emission_overrides != null)
			{
				emit_prob = item.emission_overrides[tags.T2];

				if (emit_prob.IsZero())
				{
					Debug.Assert(!_hmm.prob_ep.TryGetValue(new Emission(item.item, tags.T2), out emit_prob) || emit_prob.IsZero());
					Debug.Assert(!_hmm.prob_ep.TryGetValue(new Emission(-1, tags.T2), out emit_prob) || emit_prob.IsZero());
				}
				return emit_prob;
			}

			_hmm.prob_ep.TryGetValue(new Emission(item.item, tags.T2), out emit_prob);

			if (emit_prob.IsZero())
				_hmm.prob_ep.TryGetValue(new Emission(-1, tags.T2), out emit_prob);

			return emit_prob;
		}

		TrellisCell populate_trellis_cell(StringHmm.ItemInfo item, Bitag tags, TrellisColumn prev_col, bool isEOS = false)
		{
			Double emit_prob;

			if ((emit_prob = get_emission_prob(item, tags, isEOS)).IsZero())
				return null;

			var cell = new TrellisCell(tags, Math.Log10(emit_prob), agenda_size);

			bool keepCell = false;

			foreach (var prev_cell in isEOS ? prev_col.AllCells : prev_col.GetCellsForPrevTag(tags.T1))
			{
				TrellisData prev_td;
				if ((prev_td = GetNthBestTrellisData(prev_cell, 0)) != null && !Double.IsNaN(prev_td.logprob))
				{
					var trans_logprob = evalSmoothedTritag(prev_cell.bitag.T1, prev_cell.bitag.T2, tags.T2);

					if (!Double.IsNaN(trans_logprob))
					{
						Double this_log_prob = prev_td.logprob + trans_logprob + cell.emit_logprob;
						cell.agenda.Add(new TrellisData(this_log_prob, prev_cell, 0));
						keepCell = true;
					}
				}
			}
			return keepCell ? cell : null;
		}

		public TrellisData GetNthBestTrellisData(int col, Bitag cell_bitag, int rank)
		{
			if ((uint)col >= (uint)trellis.Length || !trellis[col].ContainsBitag(cell_bitag))
				return null;

			if (col == 0 && (rank != 0 || !cell_bitag.Equals(Bitag.EOSEOS)))	//just for efficiency
				return null;

			return GetNthBestTrellisData(trellis[col][cell_bitag], rank);
		}

		public TrellisData GetNthBestTrellisData(TrellisCell cell, int rank)
		{
			while (true)
			{
				if (rank < cell.cache.Count)
					return cell.cache[rank];

				if (cell.agenda.Count <= 0)
					return null;

				TrellisData prev, cur = cell.agenda.RemoveMax();

				cell.cache.Add(cur);

				if ((prev = GetNthBestTrellisData(cur.path, cur.path_idx + 1)) != null)
				{
					var trans_logprob = evalSmoothedTritag(cur.path.bitag.T1, cur.path.bitag.T2, cell.bitag.T2);

					cell.agenda.Add(new TrellisData(
											prev.logprob + trans_logprob + cell.emit_logprob,
											cur.path,
											cur.path_idx + 1));
				}
			}
		}

		Double evalSmoothedTritag(Tritag tt)
		{
			Double lgP;
			if (!trans_logprob_cache.TryGetValue(tt, out lgP))
			{
				var P = _hmm.lambdas[2] * _hmm.get_tt_prob(tt.T1, tt.T2, tt.T3) +
						_hmm.lambdas[1] * _hmm.get_bt_prob(tt.T2, tt.T3) +
						_hmm.lambdas[0] * _hmm.get_ut_prob(tt.T3);

				trans_logprob_cache.Add(tt, lgP = P == 0 ? Double.NaN : Math.Log10(P));
			}
			return lgP;
		}

		Double evalSmoothedTritag(int T1, int T2, int T3)
		{
			return evalSmoothedTritag(new Tritag(T1, T2, T3));
		}

		Double evalSmoothedTritag(String T1, String T2, String T3)
		{
			var unitags = _hmm.unitags;
			return evalSmoothedTritag(T1 == EOS ? 1 : unitags[T1], T2 == EOS ? 1 : unitags[T2], T3 == EOS ? 1 : unitags[T3]);
		}

		Double evalNgram(String[] ngram)
		{
			var unitags = _hmm.unitags;
			int[] int_ngram = ngram.Select(x => x == EOS ? 1 : unitags[x]).ToArray();

			if (int_ngram.Length == 1)
				return _hmm.get_ut_prob(int_ngram[0]);
			if (int_ngram.Length == 2)
				return _hmm.get_bt_prob(int_ngram[0], int_ngram[1]);
			if (int_ngram.Length == 3)
				return _hmm.get_tt_prob(int_ngram[0], int_ngram[1], int_ngram[2]);
			return 0.0;
		}

		Double emissionProb(String item, String tag)
		{
			int int_item = item == OOV ? -1 : ((StringHmm)_hmm).items[item];
			int int_tag = ((StringHmm)_hmm).unitags[tag];

			Double emit_prob;
			Emission e = new Emission(int_item, int_tag);
			if (!_hmm.prob_ep.TryGetValue(e, out emit_prob) || (emit_prob.IsZero() && int_item != -1))
			{
				if (!_hmm.prob_ep.TryGetValue(new Emission(-1, int_tag), out emit_prob))
					return Double.NaN;
			}
			return Math.Log10(emit_prob);
		}

		public pos_prob[][] ReadbackPaths()
		{
			var ret = new pos_prob[trellis.Length - 2][];

#if VERBOSE
			var paths = new List<HmmPath>();
#else
			var paths = default(List<HmmPath>);
#endif

			GatherPaths(trellis[trellis.Length - 1][Bitag.EOSEOS], ret, paths);

#if VERBOSE
			if (paths != null)
				Console.WriteLine(paths.StringJoin(Environment.NewLine));
			Console.WriteLine();
#endif

			RemoveTailsAndRenormalize(ret);

			return ret;
		}

		/// <summary> paths can be null </summary>
		void GatherPaths(TrellisCell endpoint, pos_prob[][] ret, List<HmmPath> paths)
		{
			int i, j;
			pos_prob pp = default(pos_prob);
			Double tot_mass = 0.0;
			TrellisData bt;
			HmmPath path = default(HmmPath);

			for (i = 0; (bt = GetNthBestTrellisData(endpoint, i)) != null; i++)
			{
				pp.p = Math.Pow(10, bt.logprob);

				if (tot_mass != 0.0 && pp.p < tot_mass / 100000)
					break;

				tot_mass += pp.p;

				j = trellis.Length - 2;

				path = new HmmPath(_hmm.unitags, pp.p, j);

				/// walk back through trellis and record best path
				do
				{
					j--;
					if ((pp.i_tag = bt.path.bitag.T2) < 2)
						break;

					if (paths != null)
						path[j] = pp.i_tag;

					pp.AddToTagProbArr(ref ret[j]);

					bt = GetNthBestTrellisData(bt.path, bt.path_idx);
				}
				while (j > 0 || bt != null);

				if (paths != null)
					paths.Add(path);
			}
		}

		void RemoveTailsAndRenormalize(pos_prob[][] ret)
		{
			int i, j;
			pos_prob[] cur;

			for (i = 0; i < ret.Length; i++)
			{
				if ((cur = ret[i]).Length == 1)
				{
					cur[0].p = 1.0;
				}
				else
				{
					Array.Sort(cur);

					Double val, total = cur[0].p;
					j = 1;
					do
					{
						if ((val = cur[j].p) < .01 * (total + val))
						{
							ret[i] = cur = cur.Resize(j);
							break;
						}
						total += val;
					}
					while (++j < cur.Length);

					total = Math.Log10(total);

					for (j = 0; j < cur.Length; j++)
						cur[j].p = Math.Pow(10, Math.Log10(cur[j].p) - total);
				}
#if VERBOSE
				Console.WriteLine(_tokens[i]);
				Console.WriteLine(cur
								.Select(x => String.Format("{0,5} {1,12:N7}", _hmm.unitags[x.i_tag], x.p))
								.StringJoin(Environment.NewLine) +
								Environment.NewLine);
#endif
			}
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////

		static public TrellisData[] EmptyPQ(PriorityQueue<TrellisData> pq)
		{
			return pq.SortDescending().ToArray();
		}

		String[] readableTrellisData(TrellisColumn col)
		{
			return col.AllCells
						.OrderByDescending(x => x.agenda.First().logprob)
						.Select(cell => cell.Info(_hmm.unitags))
						.ToArray();
		}
	};
}
