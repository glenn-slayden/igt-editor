//#define LOGGING

using System;
using System.Diagnostics;
using alib.Array;
using alib.Debugging;
using alib.Enumerable;

namespace alib.dg
{
	using String = System.String;

	public enum LogOp
	{
		StartLogInstance = 1,
		CreateVertex,
		CreateEdge,
		DeleteVertex,
		DeleteEdge,
		ClearGraph,
		DebugMessages,
		Abort,
		AssertGraphVertexCount,
		AssertGraphEdgeCount,
		AssertVertexInEdgeCount,
		AssertVertexOutEdgeCount,
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// automated operations for debugging, testing, and scripting
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class rw_string_graph : rw_data_graph<String, String>, IStringGraph
	{
		static String _log_file;
		static int _seq_num;

		public static void log_op(LogOp op, params String[] args)
		{
			if (op == LogOp.StartLogInstance)
			{
				if (_log_file != null)
					log_op(LogOp.Abort);
				_log_file = String.Format("dg-log-{0:yyyyMMdd\\-hhmmss}.txt", DateTime.Now);
				_seq_num = 0;
			}
			if (_log_file == null)
				return;

			switch (op)
			{
				case LogOp.Abort:
					log_write(op);
					break;
				case LogOp.CreateVertex:
					log_write(op, args[0]);
					break;
				case LogOp.CreateEdge:
					log_write(op, args);
					break;
				case LogOp.DeleteVertex:
					log_write(op, args[0]);
					break;
				case LogOp.DeleteEdge:
					log_write(op, args[0]);
					break;
				case LogOp.ClearGraph:
					log_write(op);
					break;
			}
		}
		static void log_write(LogOp op, params String[] args)
		{
			var s = _seq_num.ToString() + "\t" + op.ToString();
			if (args.Length > 0)
				s += "\t" + args.StringJoin("\t");
			System.IO.File.AppendAllText(_log_file, s + Environment.NewLine);
			_seq_num++;
		}

		static bool f_logging;

		public static rw_string_graph from_log(String file)
		{
			var g = new rw_string_graph();
			g.enable_vdata_index();
			g.enable_edata_index();
			Debug.AutoFlush = true;

			f_logging = false;
#if LOGGING
			f_logging = true;
#endif

			foreach (var _cmd in System.IO.File.ReadAllLines(file))
			{
				var cmd = _cmd.TrimStart();
				int semi = cmd.IndexOf(';');
				if (semi >= 0)
					cmd = cmd.Substring(0, semi);
				cmd = cmd.TrimEnd();
				if (cmd.Length == 0)
					continue;

				var parts = cmd.Split(default(Char[]), StringSplitOptions.RemoveEmptyEntries);

				int seq_num;
				if (int.TryParse(parts[0], out seq_num))
					parts = parts.RemoveAt(0);
				else
					seq_num = -1;

				LogOp op;
				if (!System.Enum.TryParse<LogOp>(parts[0], true, out op))
					throw new Exception();
				if (!g.run_cmd(seq_num, op, parts.RemoveAt(0)))
					break;

				//g.check();
			}
			return g;
		}

		bool run_cmd(int seq_num, LogOp op, String[] cmd)
		{
			bool f_continue = true;
			String msg = null;

			if (op == LogOp.DebugMessages)
				f_logging = cmd[0].ToLower() == "true" || cmd[0] == "1";

			if (f_logging)
			{
				String s="";
				if (seq_num >= 0)
					s = seq_num.ToString().PadLeft(4);
				s += String.Format("{0,25} {1}...", op, cmd.StringJoin(" "));
				Debug.Write(s);
			}

			switch (op)
			{
				case LogOp.Abort:
					f_continue = false;
					break;
				case LogOp.CreateVertex:
					{
						Vref vr = add_vertex(cmd[0]);

						if (f_logging)
						{
							if (vr < 0)
								msg = vr.ToString();
							else
								msg = String.Format(" ok (V{0})", (int)vr);
						}
					}
					break;
				case LogOp.CreateEdge:
					{
						var vr_from = find_vertex_for_data(cmd[0]);
						var vr_to = find_vertex_for_data(cmd[1]);

						if (vr_from < 0 || vr_to < 0)
							throw new Exception();

						Eref er = create_edge(vr_from, vr_to, cmd.Length == 3 ? cmd[2] : null);

						if (f_logging)
						{
							if (er < 0)
								msg = er.ToString();
							else
								msg = String.Format(" ok (V{0} → E{1} → V{2})", (int)vr_from, (int)er, (int)vr_to);
						}
					}
					break;
				case LogOp.DeleteVertex:
					{
						var vr = find_vertex_for_data(cmd[0]);
						if (vr < 0)
							throw new Exception();
						DeleteVertex(vr);
					}
					break;
				case LogOp.DeleteEdge:
					{
						var er = find_edge_for_data(cmd[0]);
						if (er < 0)
							throw new Exception();
						DeleteEdge(er);
					}
					break;
				case LogOp.ClearGraph:
					ClearGraph();
					break;
				case LogOp.AssertGraphVertexCount:
					{
						if (VertexCount != int.Parse(cmd[0]))
							throw new Exception();
					}
					break;
				case LogOp.AssertGraphEdgeCount:
					{
						if (EdgeCount != int.Parse(cmd[0]))
							throw new Exception();
					}
					break;
				case LogOp.AssertVertexInEdgeCount:
					{
						var vr = find_vertex_for_data(cmd[0]);
						int c = int.Parse(cmd[1]);
						if (vertex_in_edge_count(vr) != c)
							throw new Exception();
					}
					break;
				case LogOp.AssertVertexOutEdgeCount:
					{
						var vr = find_vertex_for_data(cmd[0]);
						int c = int.Parse(cmd[1]);
						if (vertex_out_edge_count(vr) != c)
							throw new Exception();
					}
					break;
			}

			if (f_logging)
				Debug.Write((msg ?? " ok") + Environment.NewLine);

			return f_continue;
		}
	};
}