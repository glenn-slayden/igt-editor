using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.Win32;

using alib.Enumerable;
using alib.Debugging;
using alib.Hashing;

namespace xigt2
{
	public class igt_source_line
	{
		public igt_source_line(String name, String text)
		{
			this.name = name;
			this.text = text;

			this.itl = new igt_tok_lattice(this);
		}

		public String name;

		public String text;

		public igt_tok_lattice itl;

		public lattice_edge get_edge(int i_from, int i_to)
		{
			var e = itl.edges.FirstOrDefault(x => x.i_from == i_from && x.i_to == i_to);
			if (e != null)
				return e;
			e = new lattice_edge(itl, i_from, i_to);
			itl.edges.Add(e);
			return e;
		}
	};

	public class igt_tok_lattice
	{
		public igt_tok_lattice(igt_source_line isl)
		{
			this.edges = new List<lattice_edge>();
			this.isl = isl;
		}
		public igt_source_line isl;

		public List<lattice_edge> edges;
	};

	public class lattice_edge
	{
		public lattice_edge(igt_tok_lattice itl, int i_from, int i_to)
		{
			this.itl = itl;
			this.i_from = i_from;
			this.i_to = i_to;
		}
		public igt_tok_lattice itl;
		public int i_from;
		public int i_to;

		public String ComputedText
		{
			get
			{
				return itl.isl.text.Substring(i_from, i_to - i_from);
			}
		}
	};



	public struct align_pair
	{
		public lattice_edge e1;
		public lattice_edge e2;
	};

	public class igt_alignment
	{
		public String name;

		public align_pair[] pairs;
	};

}