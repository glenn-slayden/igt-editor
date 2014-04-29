using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using alib.Enumerable;
using alib.String;

namespace alib.EditDistance
{
	using String = System.String;
	public static class EditDist
	{
		const double k_ins = 1, k_del = 1, k_sub = 2;

		//static void Main(string[] args)
		//{
		//    int show_diff,show_matrix;
		//    if ((show_diff = Array.IndexOf<String>(args, "-d")) != -1)
		//        args = args.Where((e, x) => x != show_diff).ToArray();
		//    if ((show_matrix = Array.IndexOf<String>(args, "-m")) != -1)
		//        args = args.Where((e, x) => x != show_diff).ToArray();

		//    dynamic a;
		//    if (!args[0].Contains('/'))
		//        a = EditDistance(args[0].ToCharArray(), args[1].ToCharArray(), (i1, i2) => k_sub);
		//    else
		//    {
		//        String[] doc1 = File.ReadAllLines(args[0]);
		//        String[] doc2 = File.ReadAllLines(args[1]);

		//        a = EditDistance(doc1, doc2, (line1, line2) =>
		//            {
		//                Alignment<Char> a2 = EditDistance(line1.ToCharArray(), line2.ToCharArray(), (chr1, chr2) => k_sub);
		//                return 0.5 + a2.d_norm;
		//            });
		//    }

		//    if (show_diff != -1)
		//        a.ShowDiff();

		//    if (show_matrix != -1)
		//        a.ShowMatrix();

		//    Console.WriteLine(a.d_norm);
		//}


		public static Double EditDistance(IEnumerable<String> src, IEnumerable<String> tgt)
		{
			String[] doc1 = src as String[] ?? src.ToArray();
			String[] doc2 = tgt as String[] ?? tgt.ToArray();

			return EditDistance(doc1, doc2, (line1, line2) =>
				{
					Alignment<Char> a2 = EditDistance(line1.ToCharArray(), line2.ToCharArray(), (chr1, chr2) => k_sub);
					return 0.5 + a2.d_norm;
				}).d_norm;
		}

		public static Alignment<T> EditDistance<T>(T[] src, T[] tgt, Func<T, T, double> sub_func) where T : IEquatable<T>
		{
			Alignment<T> a = new Alignment<T>(src, tgt);
			a[0, 0] = 0.0;
			for (int i = 1; i <= src.Length; i++)
				a[0, i] = i;
			for (int i = 1; i <= tgt.Length; i++)
				a[i, 0] = i;

			for (int i = 1; i <= tgt.Length; i++)
				for (int j = 1; j <= src.Length; j++)
				{
					Double t_ins = a[i - 1, j] + k_ins;
					Double t_del = a[i, j - 1] + k_del;
					Double t_sub = a[i - 1, j - 1];
					if (!src[j - 1].Equals(tgt[i - 1]))
						t_sub += sub_func(src[j - 1], tgt[i - 1]);

					if (t_ins < t_del)
						a[i, j] = t_sub < t_ins ? t_sub : t_ins;
					else
						a[i, j] = t_sub < t_del ? t_sub : t_del;
					//a[i, j] = Math.Min(Math.Min(t_ins, t_del), t_sub);
				}
			return a;
		}

		public class EditDistArray<T>
		{
			protected double[,] arr;
			protected T[] src, tgt;

			public EditDistArray(T[] src, T[] tgt)
			{
				this.src = src;
				this.tgt = tgt;
				arr = new double[tgt.Length + 1, src.Length + 1];
			}

			public double this[int i, int j]
			{
				get { return arr[i, j]; }
				set { arr[i, j] = value; }
			}

			public double d_norm
			{
				get
				{
					int d1 = tgt.Length;
					int d2 = src.Length;
					if (d1 + d2 == 0)
						return 0;
					return arr[d1, d2] / (d1 + d2);
				}
			}
		};

		public class Alignment<T> : EditDistArray<T>
		{
			public Alignment(T[] src, T[] tgt)
				: base(src, tgt)
			{
			}
			[DebuggerDisplay("{i_src.ToString().PadLeft(3),nq}:{src,nq}  →  {i_tgt.ToString().PadLeft(3),nq}:{tgt,nq}")]
			public class path_element
			{
				public path_element(int j, int i) { i_src = j; i_tgt = i; }
				public String src = String.Empty;
				public String tgt = String.Empty;
				public int i_src;
				public int i_tgt;
				public Double d_norm;
			}
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public List<path_element> path = null;

			public void FindBacktrace()
			{
				if (path == null)
				{
					path = new List<path_element>();
					int i = tgt.Length, j = src.Length;
					while (i > 0 || j > 0)
					{
						Double t_ins = i > 0 ? arr[i - 1, j] : Double.MaxValue;
						Double t_del = j > 0 ? arr[i, j - 1] : Double.MaxValue;
						Double t_sub = i > 0 && j > 0 ? arr[i - 1, j - 1] : Double.MaxValue;
						Double d;
						if (t_ins < t_del)
							d = t_sub < t_ins ? t_sub : t_ins;
						else
							d = t_sub < t_del ? t_sub : t_del;
						//Double d = Math.Min(Math.Min(t_ins, t_del), t_sub);

						// find movement directions
						int horz = -1, vert = -1;
						if (d == t_sub)
						{
							horz = i--;
							vert = j--;
						}
						else if (d == t_ins)
							horz = i--;
						else if (d == t_del)
							vert = j--;

						// store path information
						path_element pe = new path_element(j, i);
						if (vert != -1)
							pe.src = src[j].ToString();
						if (horz != -1)
							pe.tgt = tgt[i].ToString();
						pe.d_norm = EditDistance(pe.src.ToCharArray(), pe.tgt.ToCharArray(), (z1, z2) => k_sub).d_norm;
						path.Add(pe);
					}
					path.Reverse();
				}
			}

			public void ShowDiff()
			{
				FindBacktrace();
				int wl = path.Max(e => e.src.Length);
				int wr = path.Max(e => e.tgt.Length);
				foreach (path_element pe in path)
					Console.WriteLine("{0," + wl + "} {1,4:N} {2,-" + wr + "}", pe.src, pe.d_norm, pe.tgt);
				Console.WriteLine();
			}

			public void ShowMatrix()
			{
				FindBacktrace();
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("      ");
				for (int j = 0; j < tgt.Length; j++)
					Console.Write("{0,3}", tgt[j].ToString()[0]);

				Console.WriteLine();
				Console.ResetColor();
				for (int j = 0; j <= src.Length; j++)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					if (j > 0)
						Console.Write("{0,3} ", src[j - 1].ToString()[0]);
					else
						Console.Write("    ");
					Console.ResetColor();
					for (int i = 0; i <= tgt.Length; i++)
					{
						if (path.Any(pe => i == tgt.Length && j == src.Length || pe.i_src == j && pe.i_tgt == i))
							Console.ForegroundColor = ConsoleColor.Red;
						Console.Write("{0,2:G} ", arr[i, j]);
						Console.ResetColor();
					}
					Console.WriteLine();
				}
			}
		};

	}
}