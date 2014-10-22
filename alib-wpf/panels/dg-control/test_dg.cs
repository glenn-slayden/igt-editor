using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using alib.Hashing;
using alib.Wpf;
using System.Globalization;
using alib.Debugging;
using alib.Array;

using alib.Enumerable;
using alib.Collections;
using alib.Graph;

namespace alib.Wpf
{
	using String = System.String;

	[DebuggerDisplay("{s_feat,nq} {in_mark} {s_type,nq} {out_mark}")]
	public struct __ate
	{
		public __ate(String s_feat, int in_mark, String s_type, int out_mark)
		{
			this.s_feat = s_feat;
			this.in_mark = in_mark;
			this.s_type = s_type;
			this.out_mark = out_mark;
		}
		public String s_feat;
		public int in_mark;
		public String s_type;
		public int out_mark;
	};

	public unsafe class dg_ate_test_builder
	{
		public dg_ate_test_builder(IDataMap<String> edict, IDataMap<String> vdict, __ate[] rgate, out Edge[] _E, out Vertex[] _V)
		{
			this.edict = edict;
			this.vdict = vdict;
			this.rgate = rgate;
			int i, c = rgate.Length;
			this.offs = 100;// tfs.c_corefs;
			this.IMR = new int[offs + c + 2][];		// should actually be: offs+rgate.Max(ate=>ate.mark)+1
			for (i = 0; i < c; i++)
				if (rgate[i].s_type != null)
					alib.Array.arr.Append(ref IMR[rgate[i].in_mark + offs], i);

			m_v = default(Vref);
			m_e = default(Eref);
			int* CC = stackalloc int[c];
			Edge* EE = stackalloc Edge[c];
			Vertex* VV = stackalloc Vertex[c + 1];

			this.corefs = CC;
			this.E = EE;
			this.V = VV;

			build_node(Eref.None, 0);

			_E = new Edge[m_e];
			for (c = 0; c < m_e; c++)
				_E[c] = *E++;

			_V = new Vertex[m_v];
			for (c = 0; c < m_v; c++)
				_V[c] = *V++;
		}

		IDataMap<String> edict;
		IDataMap<String> vdict;
		__ate[] rgate;
		int offs;
		int[][] IMR;

		Vref m_v;
		Eref m_e;
		int* corefs;
		Edge* E;
		Vertex* V;

		Vref build_node(Eref e, int i_ate)
		{
			Vref v = m_v;
			int m;

			if ((m = rgate[i_ate].out_mark) < 0)
			{
				Debug.Assert(v != 0);

				int ec;
				if ((ec = corefs[~m]) != 0)
				{
					E[e].e_next = E[ec].e_next;
					E[ec].e_next = e;
					return E[ec].v_to;
				}
				corefs[~m] = e;
			}

			m_v++;

			V[v] = new Vertex
			{
				e_in = e,
				value = vdict.GetOrAdd(rgate[i_ate].s_type),
			};

			build_arcs(v, ref V[v], m);

			return v;
		}

		void build_arcs(Vref v_par, ref Vertex par, int in_mark)
		{
			int c;
			int[] fvp;

			if (in_mark == 0 || (fvp = IMR[in_mark + offs]) == null || (c = fvp.Length) == 0)
			{
				par.e_out = Eref.NotValid;
			}
			else
			{
				Eref e;
				Edge* pe = E + (e = par.e_out = m_e);
				m_e += par.c_out = c;

				for (int q = 0; q < c; q++, e++, pe++)
				{
					pe->v_from = v_par;
					pe->value = edict.GetOrAdd(rgate[fvp[q]].s_feat);
					pe->e_next = e;
					pe->v_to = build_node(e, fvp[q]);
				}
			}
		}

		public static rw_string_graph FromArrayTfs()
		{
			var edict = new graph_data_map<String>();
			var vdict = new graph_data_map<String>();
			Edge[] E;
			Vertex[] V;
			new dg_ate_test_builder(edict, vdict, sample_data, out E, out V);

			var graph = new rw_string_graph(edict, vdict, E, V, default(IGraph));

			return graph;
		}

		public static __ate[] sample_data = new __ate[] 
		{
#if true
			new __ate("(TFS ROOT)",   0, "phrase",   1),
			new __ate("CATEG",   1, "s",   0),
			new __ate("NUMAGR",   1, "sg",  -1),
			new __ate("ARGS",   1, "*ne-list*",   9),
			new __ate("FIRST",   9, "phrase",   6),
			new __ate("REST",   9, "*ne-list*",   8),
			new __ate("FIRST",   8, "sg-word",   7),
			new __ate("REST",   8, "*null*",   0),
			new __ate("CATEG",   6, "np",   0),
			new __ate("NUMAGR",   6, "sg",  -1),
			new __ate("ARGS",   6, "*ne-list*",   5),
			new __ate("FIRST",   5, "sg-word",   2),
			new __ate("REST",   5, "*ne-list*",   4),
			new __ate("FIRST",   4, "sg-word",   3),
			new __ate("REST",   4, "*null*",   0),
			new __ate("CATEG",   2, "det",   0),
			new __ate("ORTH",   2, "this",   0),
			new __ate("NUMAGR",   2, "sg",  -1),
			new __ate("CATEG",   3, "n",   0),
			new __ate("ORTH",   3, "dog",   0),
			new __ate("NUMAGR",   3, "sg",  -1),
			new __ate("CATEG",   7, "vp",   0),
			new __ate("ORTH",   7, "sleeps",   0),
			new __ate("NUMAGR",   7, "sg",  -1),
#else
			new __ate("(TFS ROOT)",   0, "hcomp_rule",   1),
			new __ate("SYNSEM",   1, "nonlex_synsem", 109),
			new __ate("KEY-ARG",   1, "bool",   0),
			new __ate("ORTH",   1, "orthog", 110),
			new __ate("INFLECTD",   1, "+",   0),
			new __ate("GENRE",   1, "genre",   0),
			new __ate("DIALECT",   1, "dialect",   0),
			new __ate("IDIOM",   1, "bool",   0),
			new __ate("C-CONT",   1, "mrs", 113),
			new __ate("LIST", 111, "*cons*", -41),
			new __ate("LAST", 111, "*cons*", -41),
			new __ate("LIST", 112, "*cons*", -42),
			new __ate("LAST", 112, "*cons*", -42),
			new __ate("MIN",   4, "nonaux_v_rel", -37),
			new __ate("HOOK",  90, "hook", -40),
			new __ate("RELS",  90, "*diff-list*",  78),
			new __ate("HCONS",  90, "*diff-list*",  89),
			new __ate("LOCAL", 109, "local",  91),
			new __ate("NONLOC", 109, "non-local",  95),
			new __ate("OPT", 109, "bool",   0),
			new __ate("--MIN", 109, "nonaux_v_rel", -37),
			new __ate("--SIND", 109, "non_conj_event", -11),
			new __ate("LEX", 109, "-", -38),
			new __ate("MODIFD", 109, "xmod_min",   0),
			new __ate("PHON", 109, "phon", 104),
			new __ate("PUNCT", 109, "punctuation", 108),
			new __ate("HEAD",  26, "verb",  11),
			new __ate("VAL",  26, "valence_sp",  25),
			new __ate("MC",  26, "bool",   0),
			new __ate("POSTHD",  26, "bool",   0),
			new __ate("HC-LEX",  26, "-", -38),
			new __ate("HS-LEX",  26, "luk",   0),
			new __ate("NEGPOL",  26, "luk",   0),
			new __ate("CAT",  91, "cat",  26),
			new __ate("CONT",  91, "mrs",  90),
			new __ate("AGR",  91, "full_ref-ind",  -5),
			new __ate("CONJ",  91, "cnil",   0),
			new __ate("CTXT",  91, "ctxt_min",   0),
			new __ate("ARG-S",  91, "*null*",   0),
			new __ate("ONSET", 104, "con", 103),
			new __ate("SLASH",  95, "0-1-dlist",  92),
			new __ate("QUE",  95, "0-dlist",  93),
			new __ate("REL",  95, "0-dlist",  94),
			new __ate("LIST",  78, "*cons*", -41),
			new __ate("LAST",  78, "*list*", -23),
			new __ate("LIST",  89, "*cons*", -42),
			new __ate("LAST",  89, "*list*", -33),
			new __ate("XARG", -40, "*top*",   0),
			new __ate("LTOP", -40, "handle",  -9),
			new __ate("INDEX", -40, "non_conj_event", -11),
			new __ate("--SLTOP", -40, "handle",  31),
			new __ate("SUBJ",  25, "*cons*",  24),
			new __ate("SPR",  25, "*null*",   0),
			new __ate("COMPS",  25, "*null*",   0),
			new __ate("SPEC",  25, "*list*",   0),
			new __ate("SPCMPS",  25, "*list*",   0),
			new __ate("PNCTPR", 108, "pnctpair",   0),
			new __ate("LPUNCT", 108, "no_punct", 105),
			new __ate("RPUNCT", 108, "clause_punct", 106),
			new __ate("PAIRED", 108, "no_ppair",   0),
			new __ate("RCLSTR", 108, "rpunct_cluster", 107),
			new __ate("HOOK", 113, "hook", -40),
			new __ate("RELS", 113, "*diff-list*", 111),
			new __ate("HCONS", 113, "*diff-list*", 112),
			new __ate("FIRST", 110, "*top*",   0),
			new __ate("REST", 110, "*top*",   0),
			new __ate("FORM", 110, "string",   0),
			new __ate("FROM", 110, "0",  -7),
			new __ate("TO", 110, "string", -22),
			new __ate("LIST",  93, "0-1-list", -35),
			new __ate("LAST",  93, "0-1-list", -35),
			new __ate("LIST",  94, "0-1-list", -36),
			new __ate("LAST",  94, "0-1-list", -36),
			new __ate("+FORM", 101, "don’t", -39),
			new __ate("+CLASS", 101, "non_alphanumeric",  96),
			new __ate("+TRAIT", 101, "native_trait",   0),
			new __ate("+PRED", 101, "predsort",   0),
			new __ate("+CARG", 101, "don’t", -39),
			new __ate("+ID", 101, "*diff-list*",  98),
			new __ate("+FROM", 101, "0",  -7),
			new __ate("+TO", 101, "5",   0),
			new __ate("+TNT", 101, "null_tnt", 100),
			new __ate("+TAG",  99, "VBP",   0),
			new __ate("+PRB",  99, "1.00",   0),
			new __ate("MOD",  11, "*cons*",   3),
			new __ate("PRD",  11, "-",   0),
			new __ate("MINORS",  11, "minors_basic",   4),
			new __ate("AUX",  11, "-",   0),
			new __ate("CASE",  11, "case",   0),
			new __ate("TAM",  11, "tam",   5),
			new __ate("LSYNSEM",  11, "no_synsem",   9),
			new __ate("POSS",  11, "bool",   0),
			new __ate("VFORM",  11, "imp_vform",   0),
			new __ate("INV",  11, "-",   0),
			new __ate("LPRED",  11, "predsort",   0),
			new __ate("--ADDIN",  11, "addinfl",  10),
			new __ate("ADDPN",  10, "pn",   0),
			new __ate("ADDTAM",  10, "tam_min",   0),
			new __ate("FIRST",   3, "anti_synsem_min",   2),
			new __ate("REST",   3, "*null*",   0),
			new __ate("LOCAL",   2, "mod_local",   0),
			new __ate("NONLOC",   2, "non-local_min",   0),
			new __ate("TENSE",   5, "basic_tense",   0),
			new __ate("ASPECT",   5, "basic_aspect",   0),
			new __ate("MOOD",   5, "ind_or_subj",  -4),
			new __ate("NONLOC",   9, "non-local_min",   0),
			new __ate("LOCAL",   9, "local_min1",   8),
			new __ate("CAT",   8, "cat_min1",   7),
			new __ate("HEAD",   7, "head_min",   0),
			new __ate("VAL",   7, "valence",   6),
			new __ate("SUBJ",   6, "*list*",   0),
			new __ate("SPR",   6, "*list*",   0),
			new __ate("COMPS",   6, "*null*",   0),
			new __ate("FIRST",  24, "anti_synsem",  23),
			new __ate("REST",  24, "*null*",   0),
			new __ate("LOCAL",  23, "local_min1",  16),
			new __ate("NONLOC",  23, "non-local",  20),
			new __ate("OPT",  23, "bool",   0),
			new __ate("--MIN",  23, "predsort",   0),
			new __ate("--SIND",  23, "full_ref-ind",  -5),
			new __ate("LEX",  23, "luk",   0),
			new __ate("MODIFD",  23, "xmod_min",   0),
			new __ate("PHON",  23, "phon_min",   0),
			new __ate("PUNCT",  23, "punctuation_min",   0),
			new __ate("CAT",  16, "cat_min1",  15),
			new __ate("HEAD",  15, "head",  13),
			new __ate("VAL",  15, "valence_sp",  14),
			new __ate("SUBJ",  14, "*list*",   0),
			new __ate("SPR",  14, "*list*",   0),
			new __ate("COMPS",  14, "*list*",   0),
			new __ate("SPEC",  14, "*list*",   0),
			new __ate("SPCMPS",  14, "*null*",   0),
			new __ate("SLASH",  20, "0-dlist",  17),
			new __ate("QUE",  20, "0-dlist",  18),
			new __ate("REL",  20, "0-dlist",  19),
			new __ate("LIST",  17, "*locallist*",  -1),
			new __ate("LAST",  17, "*locallist*",  -1),
			new __ate("LIST",  18, "0-1-list",  -2),
			new __ate("LAST",  18, "0-1-list",  -2),
			new __ate("LIST",  19, "0-1-list",  -3),
			new __ate("LAST",  19, "0-1-list",  -3),
			new __ate("MOD",  13, "*list*",   0),
			new __ate("PRD",  13, "bool",   0),
			new __ate("MINORS",  13, "minors_basic",  12),
			new __ate("AUX",  13, "luk",   0),
			new __ate("CASE",  13, "acc",   0),
			new __ate("MIN",  12, "predsort",   0),
			new __ate("INSTLOC",  -5, "string",   0),
			new __ate("SORT",  -5, "entity",   0),
			new __ate("--TPC",  -5, "luk",   0),
			new __ate("PNG",  -5, "png",  21),
			new __ate("DIV",  -5, "bool",   0),
			new __ate("IND",  -5, "bool",   0),
			new __ate("PRONTYPE",  -5, "zero_pron",   0),
			new __ate("TENSE",  29, "present",   0),
			new __ate("ASPECT",  29, "no_aspect",  28),
			new __ate("MOOD",  29, "ind_or_subj",  -4),
			new __ate("INSTLOC",  -9, "string",   0),
			new __ate("SORT",  -9, "*sort*",   0),
			new __ate("INSTLOC",  31, "string",   0),
			new __ate("SORT",  31, "*sort*",   0),
			new __ate("FIRST", -41, "quant_or_wh_relation",  36),
			new __ate("REST", -41, "*cons*",  76),
			new __ate("LBL",  36, "handle",  33),
			new __ate("LNK",  36, "*list*",   0),
			new __ate("CFROM",  36, "0",  -7),
			new __ate("CTO",  36, "string",  -8),
			new __ate("PRED",  36, "pronoun_q_rel",   0),
			new __ate("ARG0",  36, "full_ref-ind",  -5),
			new __ate("RSTR",  36, "handle", -24),
			new __ate("BODY",  36, "handle",  35),
			new __ate("INSTLOC",  33, "string",   0),
			new __ate("SORT",  33, "*sort*",   0),
			new __ate("INSTLOC", -24, "string",  -6),
			new __ate("SORT", -24, "*sort*",   0),
			new __ate("INSTLOC",  35, "string",   0),
			new __ate("SORT",  35, "*sort*",   0),
			new __ate("FIRST",  76, "arg0_relation",  38),
			new __ate("REST",  76, "*cons*",  75),
			new __ate("PRED",  38, "pron_rel",   0),
			new __ate("LBL",  38, "handle", -25),
			new __ate("LNK",  38, "*list*",   0),
			new __ate("CFROM",  38, "0",  -7),
			new __ate("CTO",  38, "string",  -8),
			new __ate("ARG0",  38, "full_ref-ind",  -5),
			new __ate("FIRST", -42, "qeq",  79),
			new __ate("REST", -42, "*cons*",  87),
			new __ate("HARG",  79, "handle", -24),
			new __ate("LARG",  79, "handle", -25),
			new __ate("INSTLOC", -25, "string",  -6),
			new __ate("SORT", -25, "*sort*",   0),
			new __ate("FIRST",  75, "basic_arg01_relation",  41),
			new __ate("REST",  75, "*cons*",  74),
			new __ate("PRED",  41, "neg_rel",   0),
			new __ate("LBL",  41, "handle",  -9),
			new __ate("LNK",  41, "*list*",   0),
			new __ate("CFROM",  41, "0",  -7),
			new __ate("CTO",  41, "string",  -8),
			new __ate("ARG0",  41, "event_or_index",  39),
			new __ate("ARG1",  41, "handle", -26),
			new __ate("INSTLOC",  39, "string",   0),
			new __ate("SORT",  39, "*sort*",   0),
			new __ate("--TPC",  39, "luk",   0),
			new __ate("FIRST",  87, "qeq",  80),
			new __ate("REST",  87, "*cons*",  86),
			new __ate("HARG",  80, "handle", -26),
			new __ate("LARG",  80, "handle", -27),
			new __ate("INSTLOC", -26, "string", -10),
			new __ate("SORT", -26, "*sort*",   0),
			new __ate("--TL", 103, "native_token_cons", 102),
			new __ate("FIRST", 102, "token", 101),
			new __ate("REST", 102, "native_token_list",   0),
			new __ate("LIST",  98, "*cons*",  97),
			new __ate("LAST",  98, "*list*",   0),
			new __ate("+MAIN", 100, "tnt_main",  99),
			new __ate("+TAGS", 100, "*null*",   0),
			new __ate("+PRBS", 100, "*null*",   0),
			new __ate("+INITIAL",  96, "+",   0),
			new __ate("FIRST",  97, "0",   0),
			new __ate("REST",  97, "*null*",   0),
			new __ate("PSF", 105, "iforce",   0),
			new __ate("PRF",  28, "-",   0),
			new __ate("PROGR",  28, "-",   0),
			new __ate("LAST",  92, "*locallist*", -34),
			new __ate("LIST",  92, "*locallist*", -34),
			new __ate("PN",  43, "3s",   0),
			new __ate("GEN",  43, "real_gender",   0),
			new __ate("PRED",  46, "_get_v_state_rel",   0),
			new __ate("LBL",  46, "handle", -27),
			new __ate("LNK",  46, "*list*",   0),
			new __ate("CFROM",  46, "6",   0),
			new __ate("CTO",  46, "string",   0),
			new __ate("ARG0",  46, "non_conj_event", -11),
			new __ate("ARG1",  46, "full_ref-ind",  -5),
			new __ate("ARG2",  46, "nonconj_ref-ind", -12),
			new __ate("ARG3",  46, "handle", -28),
			new __ate("INSTLOC", -11, "string",   0),
			new __ate("SORT", -11, "*sort*",   0),
			new __ate("--TPC", -11, "luk",   0),
			new __ate("PNG", -11, "png_min",   0),
			new __ate("DIV", -11, "bool",   0),
			new __ate("IND", -11, "bool",   0),
			new __ate("E", -11, "tam",  29),
			new __ate("PERF", -11, "luk",   0),
			new __ate("PROG", -11, "luk",   0),
			new __ate("SF", -11, "comm",   0),
			new __ate("FIRST",  74, "arg123_relation",  46),
			new __ate("REST",  74, "*cons*",  73),
			new __ate("FIRST",  86, "qeq",  81),
			new __ate("REST",  86, "*cons*",  85),
			new __ate("INSTLOC", -28, "string", -17),
			new __ate("SORT", -28, "*sort*",   0),
			new __ate("TENSE",  57, "no_tense",   0),
			new __ate("ASPECT",  57, "basic_aspect",   0),
			new __ate("MOOD",  57, "mood",   0),
			new __ate("PN",  21, "2",   0),
			new __ate("GEN",  21, "real_gender",   0),
			new __ate("HARG",  81, "handle", -28),
			new __ate("LARG",  81, "handle", -29),
			new __ate("LBL",  50, "handle",  47),
			new __ate("LNK",  50, "*list*",   0),
			new __ate("CFROM",  50, "10", -14),
			new __ate("CTO",  50, "string", -16),
			new __ate("PRED",  50, "udef_q_rel",   0),
			new __ate("ARG0",  50, "nonconj_ref-ind", -12),
			new __ate("RSTR",  50, "handle", -30),
			new __ate("BODY",  50, "handle",  49),
			new __ate("HARG",  82, "handle", -30),
			new __ate("LARG",  82, "handle", -15),
			new __ate("INSTLOC",  47, "string",   0),
			new __ate("SORT",  47, "*sort*",   0),
			new __ate("INSTLOC", -27, "string", -10),
			new __ate("SORT", -27, "*sort*",   0),
			new __ate("FIRST",  73, "quant_or_wh_relation",  50),
			new __ate("REST",  73, "*cons*",  72),
			new __ate("INSTLOC", -30, "string", -13),
			new __ate("SORT", -30, "*sort*",   0),
			new __ate("INSTLOC",  49, "string",   0),
			new __ate("SORT",  49, "*sort*",   0),
			new __ate("FIRST",  85, "qeq",  82),
			new __ate("REST",  85, "*cons*",  84),
			new __ate("TENSE",  52, "no_tense",   0),
			new __ate("ASPECT",  52, "basic_aspect",   0),
			new __ate("MOOD",  52, "mood",   0),
			new __ate("INSTLOC",  53, "string",   0),
			new __ate("SORT",  53, "basic-entity-or-event",   0),
			new __ate("--TPC",  53, "luk",   0),
			new __ate("PNG",  53, "png_min",   0),
			new __ate("DIV",  53, "bool",   0),
			new __ate("IND",  53, "bool",   0),
			new __ate("E",  53, "tam",  52),
			new __ate("PERF",  53, "luk",   0),
			new __ate("PROG",  53, "luk",   0),
			new __ate("SF",  53, "iforce",   0),
			new __ate("PRED",  54, "_plastic_a_1_rel",   0),
			new __ate("LBL",  54, "handle", -15),
			new __ate("LNK",  54, "*list*",   0),
			new __ate("CFROM",  54, "10", -14),
			new __ate("CTO",  54, "string",   0),
			new __ate("ARG0",  54, "non_conj_event",  53),
			new __ate("ARG1",  54, "nonconj_ref-ind", -12),
			new __ate("FIRST",  72, "adj_relation",  54),
			new __ate("REST",  72, "*cons*",  71),
			new __ate("INSTLOC", -15, "string", -13),
			new __ate("SORT", -15, "*sort*",   0),
			new __ate("INSTLOC", -12, "string",   0),
			new __ate("SORT", -12, "entity",   0),
			new __ate("--TPC", -12, "luk",   0),
			new __ate("PNG", -12, "png",  43),
			new __ate("DIV", -12, "+",   0),
			new __ate("IND", -12, "bool",   0),
			new __ate("PRONTYPE", -12, "prontype",   0),
			new __ate("FIRST",  71, "reg_nom_relation",  55),
			new __ate("REST",  71, "*cons*",  70),
			new __ate("PRED",  55, "_surgery_n_1_rel",   0),
			new __ate("LBL",  55, "handle", -15),
			new __ate("LNK",  55, "*list*",   0),
			new __ate("CFROM",  55, "18",   0),
			new __ate("CTO",  55, "string", -16),
			new __ate("ARG0",  55, "nonconj_ref-ind", -12),
			new __ate("LBL",  61, "handle", -29),
			new __ate("LNK",  61, "*list*",   0),
			new __ate("CFROM",  61, "26", -19),
			new __ate("CTO",  61, "string", -22),
			new __ate("ARG0",  61, "non_conj_event",  58),
			new __ate("ARG1",  61, "nonconj_ref-ind", -12),
			new __ate("ARG2",  61, "nonconj_ref-ind", -20),
			new __ate("PRED",  61, "loc_nonsp_rel",   0),
			new __ate("INSTLOC", -29, "string", -17),
			new __ate("SORT", -29, "*sort*",   0),
			new __ate("INSTLOC",  58, "string",   0),
			new __ate("SORT",  58, "time", -18),
			new __ate("--TPC",  58, "luk",   0),
			new __ate("PNG",  58, "png_min",   0),
			new __ate("DIV",  58, "bool",   0),
			new __ate("IND",  58, "bool",   0),
			new __ate("E",  58, "tam",  57),
			new __ate("PERF",  58, "luk",   0),
			new __ate("PROG",  58, "luk",   0),
			new __ate("SF",  58, "iforce",   0),
			new __ate("FIRST",  70, "prep_relation",  61),
			new __ate("REST",  70, "*cons*",  69),
			new __ate("INSTLOC",  62, "string",   0),
			new __ate("SORT",  62, "*sort*",   0),
			new __ate("FIRST",  69, "quant_or_wh_relation",  65),
			new __ate("REST",  69, "*cons*",  68),
			new __ate("LBL",  65, "handle",  62),
			new __ate("LNK",  65, "*list*",   0),
			new __ate("CFROM",  65, "26", -19),
			new __ate("CTO",  65, "string",   0),
			new __ate("PRED",  65, "_this_q_dem_rel",   0),
			new __ate("ARG0",  65, "nonconj_ref-ind", -20),
			new __ate("RSTR",  65, "handle", -31),
			new __ate("BODY",  65, "handle",  64),
			new __ate("INSTLOC", -31, "string", -21),
			new __ate("SORT", -31, "*sort*",   0),
			new __ate("INSTLOC",  64, "string",   0),
			new __ate("SORT",  64, "*sort*",   0),
			new __ate("FIRST",  84, "qeq",  83),
			new __ate("REST",  84, "*list*", -33),
			new __ate("HARG",  83, "handle", -31),
			new __ate("LARG",  83, "handle", -32),
			new __ate("INSTLOC", -32, "string", -21),
			new __ate("SORT", -32, "*sort*",   0),
			new __ate("RPAREN", 107, "-",   0),
			new __ate("RFP", 107, "-",   0),
			new __ate("PSF", 106, "punct-prop-comm",   0),
			new __ate("PRED",  67, "_week_n_1_rel",   0),
			new __ate("LBL",  67, "handle", -32),
			new __ate("LNK",  67, "*list*",   0),
			new __ate("CFROM",  67, "31",   0),
			new __ate("CTO",  67, "string", -22),
			new __ate("ARG0",  67, "nonconj_ref-ind", -20),
			new __ate("INSTLOC", -20, "string",   0),
			new __ate("SORT", -20, "time", -18),
			new __ate("--TPC", -20, "luk",   0),
			new __ate("PNG", -20, "png",  59),
			new __ate("DIV", -20, "-",   0),
			new __ate("IND", -20, "+",   0),
			new __ate("PRONTYPE", -20, "prontype",   0),
			new __ate("PN",  59, "3s",   0),
			new __ate("GEN",  59, "real_gender",   0),
			new __ate("FIRST",  68, "nom_relation",  67),
			new __ate("REST",  68, "*list*", -23),
#endif
		};
	}
}
