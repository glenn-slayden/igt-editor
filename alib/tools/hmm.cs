#if DEBUG
//#define HMM_DEBUGGING
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

using alib.String;
using alib.Debugging;
using alib.Dictionary;
using alib.Enumerable;
using alib.Hashing;
using alib.IO;
using alib.Array;

namespace alib.Hmm
{
	using Math = System.Math;
	using String = System.String;
	using Array = System.Array;
	using math = alib.Math.math;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class TrigramHmm
	{
#if HMM_DEBUGGING
		public static TrigramHmm _this;
#endif
		public TrigramHmm(Dictionary<Emission, Double> prob_ep, ProbabilityTree[] p_tree, double[] lambdas)
		{
#if HMM_DEBUGGING
			_this = this;
#endif
			this.prob_ep = prob_ep;
			this.p_tree = p_tree;
			this.lambdas = lambdas;
		}
		protected TrigramHmm(_BinaryReader br)
			: this(read_1(br), read_2(br), read_3(br))
		{
		}
		static Dictionary<Emission, Double> read_1(_BinaryReader br)
		{
			int c = br.ReadInt32();
			var prob_ep = new Dictionary<Emission, Double>(c);
			for (int i = 0; i < c; i++)
				prob_ep.Add(new Emission(br.Read7BitEncodedInt(), br.Read7BitEncodedInt()), br.ReadDouble());
			return prob_ep;
		}
		static ProbabilityTree[] read_2(_BinaryReader br)
		{
			var pt = new ProbabilityTree();
			pt.Read(br);
			return pt.probs;
		}
		static double[] read_3(_BinaryReader br)
		{
			double[] lambdas = new double[3];
			for (int i = 0; i < 3; i++)
				lambdas[i] = br.ReadDouble();
			return lambdas;
		}

		[DebuggerDisplay("{tag,nq} {P}")]
		public struct TagProb
		{
			public Double P;
			public String tag;
		}

		public readonly Dictionary<Emission, Double> prob_ep;
		public readonly ProbabilityTree[] p_tree;
		public readonly double[] lambdas;

#if HMM_TUNING
		public static Double pval = 1e-11;
		public static Double bitag_pref = 1.0;
		public static Double tritag_pref = 4.0;
#else
		const Double pval = 1e-11;
		//const Double bitag_pref = 1.0;
		//const Double tritag_pref = 4.0;
#endif
#if CLOSED_CLASS_TAGS
		static String[] closed_class = { "IN", "TO", "MD", "EX", "CC", "DT", "PRP", "PRP$", "POS", "$", "#" };

		public bool[] closed_class_tags;

		void setup_closed_classes()
		{
			var ut = ((StringHmm)this).unitags;
			closed_class_tags = new bool[ut.Count];
			for (int i = 0; i < ut.Count; i++)
				closed_class_tags[i] = ut[i].Length <= 1 || Array.IndexOf<String>(closed_class, ut[i]) != -1;
		}
#endif

		public const int NUL = 0;
		public const int EOS = 1;

		protected virtual void WriteModel(_BinaryWriter bw)
		{
			int c = prob_ep.Count;
			bw.Write(c);
			foreach (var kvp in prob_ep)
			{
				bw.Write7BitEncodedInt(kvp.Key.item);
				bw.Write7BitEncodedInt(kvp.Key.tag);
				bw.Write(kvp.Value);
			}
			new ProbabilityTree { probs = p_tree }.Write(bw);
			for (int i = 0; i < 3; i++)
				bw.Write(lambdas[i]);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////
		public Double get_tt_prob(int T1, int T2, int T3)
		{
			ProbabilityTree[] a1, a2;
			return (a1 = p_tree[T1].probs) != null && (a2 = a1[T2].probs) != null ? a2[T3].P : 0.0;
		}
		public Double get_bt_prob(int T1, int T2)
		{
			ProbabilityTree[] a1;
			return (a1 = p_tree[T1].probs) != null ? a1[T2].P : 0.0;
		}
		public Double get_ut_prob(int T1)
		{
			return p_tree[T1].P;
		}
	};


	public struct Emission : IEquatable<Emission>
	{
		public Emission(int item, int tag)
		{
			this.item = item;
			this.tag = tag;
		}
		public int item, tag;
		public override int GetHashCode()
		{
			return (tag << 16) ^ item;
		}
		public override bool Equals(object obj)
		{
			Emission other = (Emission)obj;
			return this.item == other.item && this.tag == other.tag;
		}
		public bool Equals(Emission other)
		{
			return this.item == other.item && this.tag == other.tag;
		}
	};


	public struct StringEmission : IEquatable<StringEmission>
	{
		public StringEmission(String item, int tag, bool unkCase = false)
		{
			this.item = item;
			this.tag = tag;
			this.unkCase = unkCase;
		}
		public String item;
		public int tag;
		public bool unkCase;
		public override int GetHashCode()
		{
			return (tag << 16) ^ item.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			StringEmission other = (StringEmission)obj;
			return this.item == other.item && this.tag == other.tag;
		}
		public bool Equals(StringEmission other)
		{
			return this.item == other.item && this.tag == other.tag;
		}
	};

	public struct Bitag : IEquatable<Bitag>
	{
		//public static Bitag NUL = default(Bitag);
		public static Bitag EOSEOS = new Bitag(TrigramHmm.EOS, TrigramHmm.EOS);

		public Bitag(int T1, int T2)
			: this()
		{
			this.T1 = T1;
			this.T2 = T2;
		}
		public int T1, T2;
		public bool IsNUL
		{
			get { return T1 == 0 && T2 == 0; }
		}
		public override int GetHashCode()
		{
			return (T1 << 16) ^ T2;
		}
		public override bool Equals(object obj)
		{
			Bitag other = (Bitag)obj;
			return this.T2 == other.T2 && this.T1 == other.T1;
		}
		public bool Equals(Bitag other)
		{
			return this.T2 == other.T2 && this.T1 == other.T1;
		}
#if HMM_DEBUGGING
		public override String ToString()
		{
			var ut = ((StringHmm)TrigramHmm._this).unitags;
			return String.Format("[{0} {1}]", ut[T1], ut[T2]);
		}
#endif
	};

	public struct Tritag : IEquatable<Tritag>
	{
		public Tritag(int ttt, int tt, int t)
		{
			_data = (ttt << 20) | (tt << 10) | t;
		}
		public Tritag(int ttt, Bitag bt)
			: this(ttt, bt.T1, bt.T2)
		{
		}
		int _data;
		public Bitag bt
		{
			get { return new Bitag((int)((uint)_data >> 20), (_data >> 10) & 0x3FF); }
		}
		public int T1 { get { return (int)((uint)_data >> 20); } }
		public int T2 { get { return (_data >> 10) & 0x3FF; } }
		public int T3 { get { return _data & 0x3FF; } }
		public unsafe override int GetHashCode()
		{
			return _data;
		}
		public override bool Equals(object obj)
		{
			return this._data == ((Tritag)obj)._data;
		}
		public bool Equals(Tritag other)
		{
			return this._data == other._data;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public struct ProbabilityTree
	{
		public Double P;
		public ProbabilityTree[] probs;
		public override String ToString()
		{
			return String.Format("{0:N5} {1}",
				P,
				probs == null ? "(none)" : new String(probs.Select(ps => ps.P != 0 ? 'x' : '_').ToArray()));
		}

		public void Write(_BinaryWriter bw)
		{
			bw.Write(P);
			int c = probs == null ? 0 : probs.Length;
			bw.Write(c);
			for (int i = 0; i < c; i++)
				probs[i].Write(bw);
		}

		public void Read(_BinaryReader br)
		{
			this.P = br.ReadDouble();
			int c = br.ReadInt32();
			if (c > 0)
			{
				this.probs = new ProbabilityTree[c];
				for (int i = 0; i < c; i++)
					probs[i].Read(br);
			}
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public struct pos_prob : IEquatable<pos_prob>, IComparable<pos_prob>
	{
		public pos_prob(int i_tag, Double p)
		{
			this.i_tag = i_tag;
			this.p = p;
		}

		public int i_tag;
		public Double p;

		public void AddToTagProbArr(ref pos_prob[] cur)
		{
			int ix;
			if (cur == null || cur.Length == 0)
				cur = new[] { this };
			else if ((ix = find(cur, this.i_tag)) < 0)
				cur = arr.InsertAt(cur, ~ix, this);
			else
				cur[ix].p += p;
		}

		static int find(pos_prob[] _tmp, int i_tag)
		{
			int m, i = 0;
			if (_tmp != null)
			{
				int d, j = _tmp.Length - 1;
				while (i <= j)
				{
					if ((d = _tmp[m = i + (j - i) / 2].i_tag - i_tag) == 0)
						return m;
					if (d < 0)
						i = m + 1;
					else
						j = m - 1;
				}
			}
			return ~i;
		}

		public int CompareTo(pos_prob other)
		{
			var k = other.p - this.p;
			return k <= math._ε ? -1 : k >= math.ε ? 1 : 0;
		}
		public bool Equals(pos_prob other)
		{
			return this.i_tag == other.i_tag && this.p == other.p;
		}
		public override bool Equals(object obj)
		{
			return this.Equals((PosProb)obj);
		}
		public override int GetHashCode()
		{
			return p.GetHashCode() ^ i_tag;
		}

		public override String ToString()
		{
			return String.Format("{0} {1:G6}", i_tag, p);
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public struct PosProb : IEquatable<PosProb>, IComparable<PosProb>
	{
		public PosProb(String pos, Double p)
		{
			this.pos = pos;
			this.p = p;
		}
		public String pos;
		public Double p;

		public bool Equals(PosProb other)
		{
			return String.Equals(this.pos, other.pos, StringComparison.OrdinalIgnoreCase) && this.p == other.p;
		}
		public override bool Equals(Object obj)
		{
			return this.Equals((PosProb)obj);
		}
		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(pos) ^ p.GetHashCode();
		}

		public override String ToString()
		{
			return pos + "/" + ((p - 1.0 > math._ε) ? "1.0" : p >= .1 ? p.ToString(".####") : p.ToString("E2").SmallExp());
		}

		public int CompareTo(PosProb other)
		{
			var d = this.p - other.p;
			return d < math._ε ? 1 : d > math.ε ? -1 : 0;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Wraps the TrigramHmm for use with string-typed inputs and outputs
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class StringHmm : TrigramHmm
	{
		public StringHmm(
			StringIndex items,
			StringIndex unitags,
			Dictionary<Emission, Double> prob_ep,
			ProbabilityTree[] ps,
			double[] lambdas)
			: base(prob_ep, ps, lambdas)
		{
			this.items = items;
			this.unitags = unitags;
		}
		protected StringHmm(_BinaryReader br)
			: base(br)
		{
			this.items = StringIndex.Read(br);
			this.unitags = StringIndex.Read(br);
		}
		StringHmm(_BinaryReader _ctor_plug, Stream str)
			: this(_ctor_plug = new _BinaryReader(str))
		{
			_ctor_plug.Close();
		}
		public StringHmm(String filename)
			: this(null, new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
		}
		public
		readonly StringIndex items, unitags;

		public int GetItemIndex(String item)
		{
			return items[item];
		}
		public String GetTag(int i_tag)
		{
			return unitags[i_tag];
		}
		public PosProb Convert(pos_prob pp)
		{
			return new PosProb(unitags[pp.i_tag], pp.p);
		}

		public struct ItemInfo
		{
			public int item;
			public Double[] emission_overrides;
		};

		public ItemInfo[] convertInputToItemInfos(String[] _tokens)
		{
			bool seenAlpha = false;

			var arr_inp = new ItemInfo[_tokens.Length];

			for (int i_tok = 0; i_tok < _tokens.Length; i_tok++)
			{
				Double[] overrides = null;

				if (!seenAlpha && _tokens[i_tok].Any(x => char.IsLetter(x)))
				{
					seenAlpha = true;

					if ((items.Contains(_tokens[i_tok]) || items.Contains(_tokens[i_tok].ToLowerInvariant()))
						&& Char.IsUpper(_tokens[i_tok][0]) && _tokens[i_tok].ToCharArray().Skip(1).All(x => !Char.IsUpper(x)))
					{
						overrides = new Double[unitags.Count];

						for (int j_tag = 2; j_tag < unitags.Count; j_tag++)
						{
							var upper_em = new Emission(items[_tokens[i_tok]], j_tag);
							var lower_em = new Emission(items[_tokens[i_tok].ToLowerInvariant()], j_tag);

							var oov_em = new Emission(-1, j_tag);
							var oov_prob = prob_ep.ContainsKey(oov_em) ? prob_ep[new Emission(-1, j_tag)] : 0.0;

							//subtract oov_prob because it is built into all of the other probabilities and would otherwise be counted twice here
							overrides[j_tag] = (prob_ep.ContainsKey(upper_em) ? prob_ep[upper_em] : oov_prob) +
								(prob_ep.ContainsKey(lower_em) ? prob_ep[lower_em] : oov_prob) -
								oov_prob;
						}
					}
				}

				arr_inp[i_tok] = new ItemInfo
				{
					item = items[_tokens[i_tok]],
					emission_overrides = overrides
				};
			}
			return arr_inp;
		}

		protected override void WriteModel(_BinaryWriter bw)
		{
			base.WriteModel(bw);
			items.Write(bw);
			unitags.Write(bw);
		}
		public void WriteToFile(String filename)
		{
			using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
			//using (var ds = new DeflateStream(fs, CompressionMode.Compress))
			using (var bw = new _BinaryWriter(fs))
				WriteModel(bw);
		}

		public static StringHmm ReadModel(String filename)
		{
			using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			//using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
			using (var br = new _BinaryReader(fs))
				return new StringHmm(br);
		}

		/// debugging code below here

		public Double Prob(String word, String tag)
		{
			return prob_ep[new Emission(items[word], unitags[tag])];
		}
		public TagProb[] Prob(String word)
		{
			int i_word = items[word];
			return prob_ep.Where(kvp => kvp.Key.item == i_word).Select(kvp => new TagProb { P = kvp.Value, tag = unitags[kvp.Key.tag] }).ToArray();
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public struct TaggedItem
	{
		public TaggedItem(String Item, String Tag, bool UnkCase)
		{
			this.Item = Item;
			this.Tag = Tag;
			this.UnkCase = UnkCase;
		}
		public String Item;
		public String Tag;
		public bool UnkCase;

		public override String ToString()
		{
			return Item + " " + Tag;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	struct tagged_item
	{
		public tagged_item(String s_item, int i_tag, bool unkCase)
		{
			this.s_item = s_item;
			this.i_tag = i_tag;
			this.unkCase = unkCase;
		}
		public String s_item;
		public int i_tag;
		public bool unkCase;
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class TrigramHmmTrainer
	{
		public TrigramHmmTrainer()
		{
			//throw new Exception("english-pos.hmm running with OrdinalIgnoreCase");
			this.items = new StringIndex();
			this.unitags = new StringIndex();
			this.τ_unitag = new List<int>();

			this.singleton_words = new List<int>(new int[] { 0, 0 });
			this.item_counts = new List<int>();
			this.unk_case = new Dictionary<StringEmission, int>();

			if (this.unitags.Add("NUL") != TrigramHmm.NUL)
				throw new Exception();
			τ_unitag.Add(0);

			if (this.unitags.Add("EOS") != TrigramHmm.EOS)
				throw new Exception();
			τ_unitag.Add(0);

			//NUL and EOS don't really count as far as singleton words are concerned, but we need a placeholder for them for
			//indices to line up

			this.τ_itemtag = new Dictionary<Emission, int>();
			this.τ_bitag = new Dictionary<Bitag, int>();
			this.τ_tritag = new Dictionary<Tritag, int>();

			prev_tag = prev_prev_tag = TrigramHmm.EOS;

			c_lines = -1;
		}

		const String eos_item = "<eos>";
		const String oov_item = "<oov>";

		/// for the first pass (tallying)
		List<int> τ_unitag;
		Dictionary<Emission, int> τ_itemtag;
		Dictionary<Bitag, int> τ_bitag;
		Dictionary<Tritag, int> τ_tritag;

		//keeps counts of unknown case words (i.e. they were capitalized, but first word of the sentence)
		Dictionary<StringEmission, int> unk_case;

		List<int> item_counts;
		List<int> singleton_words;

		int c_obs, c_lines, c_tags, prev_tag, prev_prev_tag;

		/// final products stored in the model
		StringIndex items;
		StringIndex unitags;
		ProbabilityTree[] p_tree;

		public void AddSamples(String file)
		{
			var samples = get_file_sentence_pairs(file);

			tally_samples(samples);
		}

		public void AddSamples(IEnumerable<String> sentences)
		{
			foreach (String sent in sentences)
			{
				prev_tag = prev_prev_tag = TrigramHmm.EOS;
				τ_bitag.AddIncTally(Bitag.EOSEOS);

				tally_samples(get_sentence_pairs(sent));
				EndSentence();
			}
		}

		public void AddSamples(IEnumerable<TaggedItem> rgti)
		{
			tally_samples(rgti);
		}

		public void EndSentence()
		{
			c_lines++;
			tally_sample(eos_item, TrigramHmm.EOS, false);
		}

		public StringHmm BuildModel()
		{
			this.c_tags = unitags.Count;

			this.p_tree = new ProbabilityTree[c_tags];

			//enhance_tallies();

#if VERBOSE
			Console.WriteLine("sent: {0:#,###}  obs: {1:#,###}  items: {2:#,###}  tags: {3}  tag bigrams: {4}",
					c_lines,
					c_obs,
					items.Count,
					c_tags,
					τ_bitag.Count);
			Console.WriteLine(unitags.StringJoin(" "));
			Console.WriteLine();
#endif

			var prob_ep = compute_probabilities();

			var lambdas = compute_interpolation_lambdas();

			return new StringHmm(items, unitags, prob_ep, p_tree, lambdas);
		}

		IEnumerable<tagged_item> get_file_sentence_pairs(String file)
		{
			using (var sr = new StreamReader(file))
				foreach (var ti in get_file_sentence_pairs(sr))
					yield return ti;
		}

		IEnumerable<tagged_item> get_file_sentence_pairs(StreamReader sr)
		{
			String line;
			while (null != (line = sr.ReadLine()))
			{
				prev_tag = prev_prev_tag = TrigramHmm.EOS;
				τ_bitag.AddIncTally(Bitag.EOSEOS);

				//unigram EOS will already be taken care of at the end of the sentence so we shouldn't count it here.

				foreach (var ti in get_sentence_pairs(line))
					yield return ti;

				EndSentence();
			}
		}

		IEnumerable<tagged_item> get_sentence_pairs(String line)
		{
			var rgs = line.Split(alib.Character.Charset.tab);
			if ((rgs.Length & 1) != 0)
				throw new Exception(line);

			bool seenAlpha = false;

			for (int i = 0; i < rgs.Length; i += 2)
			{
				bool unkCase = false;
				if (!seenAlpha && rgs[i].Any(x => char.IsLetter(x)))
				{
					seenAlpha = true;
					if (Char.IsUpper(rgs[i][0]) && rgs[i].ToCharArray().Skip(1).All(x => !Char.IsUpper(x)))
						unkCase = true;
				}

				yield return new tagged_item(rgs[i], unitags.Add(rgs[i + 1]), unkCase);
			}
		}

		void tally_samples(IEnumerable<TaggedItem> pairs)
		{
			foreach (var ti in pairs)
				tally_sample(ti.Item, unitags.Add(ti.Tag), ti.UnkCase);
		}

		void tally_samples(IEnumerable<tagged_item> pairs)
		{
			foreach (var ti in pairs)
			{
				if (ti.unkCase && ti.s_item != String.Empty &&
					Char.IsUpper(ti.s_item[0]) &&
					ti.s_item.ToCharArray().Skip(1).All(x => !Char.IsUpper(x)))
				{
					tally_sample(ti.s_item, ti.i_tag, true);
				}
				else
				{
					tally_sample(ti.s_item, ti.i_tag, false);
				}
			}
		}

		void tally_sample(String s_item, int tag, bool unkCase)
		{
			//Console.WriteLine("{0}/{1}", s_item, unitags[tag]);

			c_obs++;
			if (tag == τ_unitag.Count)
			{
				τ_unitag.Add(1);
				singleton_words.Add(0);
			}
			else
				τ_unitag[tag]++;

			if (!items.Contains(s_item))
			{
				items.Add(s_item);
				item_counts.Add(1);
			}
			else
			{
				item_counts[items[s_item]]++;
			}

			Emission em = new Emission(items[s_item], tag);
			if (unkCase)
			{
				StringEmission s_em = new StringEmission(s_item, tag, true);
				unk_case.AddIncTally(s_em);
			}
			else
			{
				//this doesn't take into account unknown case words, so may be a little off, but hopefully negligible.
				//kinda tricky to figure out how exactly it should work with unk_case words
				τ_itemtag.AddIncTally(em);

				if (τ_itemtag[em] == 1)
					singleton_words[tag]++;
				else if (τ_itemtag[em] == 2)
					singleton_words[tag]--;
			}

			τ_bitag.AddIncTally(new Bitag(prev_tag, tag));
			τ_tritag.AddIncTally(new Tritag(prev_prev_tag, prev_tag, tag));

			prev_prev_tag = prev_tag;
			prev_tag = tag;
		}



		double tag_word_backoff(int word)
		{
			int count = 0;
			if (word >= 0)
				count = item_counts[word];

			return (double)(count + 1) / (double)(c_obs + item_counts.Count);
		}

		Dictionary<Emission, Double> compute_probabilities()
		{
			int c;
			var prob_ep = new Dictionary<Emission, Double>(τ_itemtag.Count + 1);

			//distribute unknown case / first word in sentence over capitol and lower case versions according to their respective ratios
			foreach (var unk in unk_case)
			{
				String s_item = unk.Key.item;

				int lowerItem = items[s_item.ToLowerInvariant()];
				int upperItem = items[s_item];
				Emission lowerEm = new Emission(lowerItem, unk.Key.tag);
				Emission upperEm = new Emission(upperItem, unk.Key.tag);
				int upperCnt = τ_itemtag.ContainsKey(upperEm) ? τ_itemtag[upperEm] : 0;
				int lowerCnt = τ_itemtag.ContainsKey(lowerEm) ? τ_itemtag[lowerEm] : 0;

				if (lowerCnt == 0 && upperCnt == 0)
				{
					//assume lowercase if upper is never seen in the wild.  Why? Intuitively it seems good, but may need adjustment
					if (lowerItem == -1)
					{
						lowerItem = items.Add(unk.Key.item.ToLowerInvariant());
						lowerEm = new Emission(lowerItem, unk.Key.tag);
					}
					τ_itemtag[lowerEm] = unk.Value;
				}
				else if (lowerCnt > 0 && upperCnt > 0)
				{
					double ratio = lowerCnt / (double)(lowerCnt + upperCnt);
					int lower_part = (int)Math.Ceiling(ratio * unk.Value);
					τ_itemtag[lowerEm] += lower_part;
					τ_itemtag[upperEm] += unk.Value - lower_part;
				}
				else if (lowerCnt > 0)
				{
					τ_itemtag[lowerEm] += unk.Value;
				}
				else
				{
					τ_itemtag[upperEm] += unk.Value;
				}
			}

			//these are "oov" accounts
			for (int t = 2; t < τ_unitag.Count; t++)
				τ_itemtag[new Emission(-1, t)] = 0;

			foreach (var τit in τ_itemtag)
			{
				int sing = singleton_words[τit.Key.tag];
				double prob = (τit.Value + sing * tag_word_backoff(-1)) / (double)(τ_unitag[τit.Key.tag] + sing);

				if (prob != 0.0)
					prob_ep.Add(τit.Key, prob);

				//prob_ep.Add(τit.Key, τit.Value / (Double)τ_unitag[τit.Key.tag]);
			}


			int T1;
			for (int t = 2; t < τ_unitag.Count; t++)
			{
				set_unitag_prob(t, (double)τ_unitag[t] / c_obs);
			}

			foreach (var τbt in τ_bitag)
				if ((T1 = (τbt.Key.T1)) != TrigramHmm.NUL)
					set_bitag_prob(T1, τbt.Key.T2, τbt.Value / (Double)τ_unitag[T1]);

			foreach (var τtt in τ_tritag)
			{
				Tritag tt = τtt.Key;
				if (τ_bitag.TryGetValue(new Bitag(tt.T1, tt.T2), out c))
					set_tritag_prob(tt.T1, tt.T2, tt.T3, τtt.Value / (Double)c);
			}
			return prob_ep;
		}

		double[] compute_interpolation_lambdas()
		{
			int tri_sum = 0;
			int bi_sum = 0;
			int uni_sum = 0;

			foreach (var trigram in τ_tritag.Keys)
			{
				int tri_cnt = τ_tritag[trigram];
				Bitag tri_denom = new Bitag(trigram.T1, trigram.T2);
				int tri_denom_cnt = τ_bitag[tri_denom];
				double tri_prob = (double)tri_cnt / (double)tri_denom_cnt;

				Bitag bigram = new Bitag(trigram.T2, trigram.T3);
				int bi_cnt = τ_bitag[bigram];
				int bi_denom_cnt = τ_unitag[bigram.T1];
				double bi_prob = (double)bi_cnt / (double)bi_denom_cnt;

				int uni_cnt = τ_unitag[bigram.T2];
				double uni_prob = (double)uni_cnt / (double)c_obs;

				if (tri_prob >= bi_prob && tri_prob >= uni_prob)
					tri_sum += tri_cnt;
				else if (bi_prob >= uni_prob)
					bi_sum += tri_cnt;
				else
					uni_sum += tri_cnt;
			}

			double total = tri_sum + bi_sum + uni_sum;
			double[] lambdas = new double[3];
			lambdas[0] = (double)uni_sum / total;
			lambdas[1] = (double)bi_sum / total;
			lambdas[2] = (double)tri_sum / total;

			return lambdas;
		}

		void set_unitag_prob(int T1, Double P)
		{
			p_tree[T1].P = P;
		}

		void set_bitag_prob(int T1, int T2, Double P)
		{
			ProbabilityTree[] a1;
			if ((a1 = p_tree[T1].probs) == null)
				a1 = p_tree[T1].probs = new ProbabilityTree[c_tags];
			a1[T2].P = P;
		}

		void set_tritag_prob(int T1, int T2, int T3, Double P)
		{
			ProbabilityTree[] a1, a2;
			if ((a1 = p_tree[T1].probs) == null)
				a1 = p_tree[T1].probs = new ProbabilityTree[c_tags];
			if ((a2 = a1[T2].probs) == null)
				a2 = a1[T2].probs = new ProbabilityTree[c_tags];
			a2[T3].P = P;
		}
	};
}

