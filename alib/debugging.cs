using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace alib.Debugging
{
	using String = System.String;

	public static class Nop
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void X(params Object[] objs)
		{
			_run_nop_hooks();
		}

		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void X()
		{
			_run_nop_hooks();
		}

		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void Expected()
		{
			_run_nop_hooks();
		}
		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void NotExpected()
		{
			_run_nop_hooks();
		}

		[Conditional("DEBUG"), DebuggerHidden]
		static void _run_nop_hooks()
		{
#if DEBUG
			var _tmp = nop_hooks;
			if (_tmp != null)
				for (int i = 0; i < _tmp.Length; i++)
					_tmp[i]();
#endif
		}

		[Conditional("DEBUG")]
		public static void ThrowIf(this Exception ex, bool f)
		{
			if (f)
				throw ex;
		}

#if DEBUG
		static Action[] nop_hooks;
#endif
		[Conditional("DEBUG")]
		public static void SetNopHook(Action a)
		{
#if DEBUG
			Action[] rg;
			var _tmp = nop_hooks;
			if (_tmp == null && (_tmp = Interlocked.CompareExchange(ref nop_hooks, rg = new[] { a }, null)) != null)
				do
					if (System.Array.IndexOf<Action>(_tmp, a) == -1)
					{
						(rg = new Action[_tmp.Length + 1])[_tmp.Length] = a;
						_tmp.CopyTo(rg, 0);
					}
				while ((_tmp = Interlocked.CompareExchange(ref nop_hooks, rg, null)) != null);
#endif
		}

		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void CanOptimize(String msg = null)
		{
			throw new Exception(msg ?? "Can optimize");
		}

		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void CodeCoverage(bool condition = true, String msg = null)
		{
			msg = msg ?? String.Empty;
			if (condition)
			{
				if (Debugger.IsAttached)
					Debugger.Break();
				else if (!_dbg_util.IsGuiApplication)
				{
#if ! __MOBILE__
					Console.Out.WriteLineColor("$red code coverage: " + msg);
#endif
					Console.WriteLine(new StackTrace().GetFrame(1).ToString());
				}
				else
				{
					MessageBox("code coverage");
				}
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void CodeCoverage(String msg)
		{
			if (Debugger.IsAttached)
				Debugger.Break();
			else if (!_dbg_util.IsGuiApplication)
			{
#if ! __MOBILE__
				Console.Out.WriteLineColor("$red code coverage: " + msg);
#endif
				Console.WriteLine(new StackTrace().GetFrame(1).ToString());
			}
			else
			{
				MessageBox("code coverage: " + msg + " " + new StackTrace().GetFrame(1).ToString());
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining), DebuggerHidden]
		public static void Untested()
		{
			if (Debugger.IsAttached)
				Debugger.Break();
			else if (!_dbg_util.IsGuiApplication)
			{
#if ! __MOBILE__
				Console.Out.WriteLineColor("$red untested code");
#endif
				Console.WriteLine(new StackTrace().GetFrame(1).ToString());
			}
			else
			{
				MessageBox("untested code" + new StackTrace().GetFrame(1).ToString());
			}
		}

		public static void MessageBox(String text)
		{
			var pf_mod = AppDomain.CurrentDomain
				.GetAssemblies()
				.First(a => a.ManifestModule.Name.ToLower().StartsWith("presentationframework."));

			if (pf_mod == null)
				throw new Exception(text);

			pf_mod.GetType("System.Windows.MessageBox")
				.GetMethod("Show", new[] { typeof(String) })
				.Invoke(null, new Object[] { text });
		}

		public static readonly bool False = false;
	};

	public static class _dbg_util
	{
		public static bool single_threaded = false;

		static int gui = -1;
		public static bool IsGuiApplication
		{
			get
			{
				if (gui == -1)
					gui = Console.OpenStandardInput(1) == Stream.Null ? 1 : 0;
				return gui == 1;
			}
		}
	};

#if ! __MOBILE__
	public class DebugTextWriter : StreamWriter
	{
		public static readonly DebugTextWriter Instance;

		static DebugTextWriter() { Instance = new DebugTextWriter(); }

		DebugTextWriter()
			: base(new DebugOutStream(), Encoding.Unicode, 1024)
		{
			this.AutoFlush = true;
		}

		class DebugOutStream : Stream
		{
			public override void Write(byte[] buffer, int offset, int count)
			{
				Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
			}

			public override bool CanRead { get { return false; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }
			public override void Flush() { Debug.Flush(); }
			public override long Length { get { throw not.valid; } }
			public override int Read(byte[] buffer, int offset, int count) { throw not.valid; }
			public override long Seek(long offset, SeekOrigin origin) { throw not.valid; }
			public override void SetLength(long value) { throw not.valid; }
			public override long Position
			{
				get { throw not.valid; }
				set { throw not.valid; }
			}
		};
	};

	public class TimingReport : IDisposable
	{
		Stopwatch sw = new Stopwatch();
		TextWriter tw;
		[MethodImpl(MethodImplOptions.NoInlining)]
		public TimingReport(TextWriter tw, String name)
		{
			this.tw = tw;
			tw.Write("========== ");
			if (tw == Console.Out)
				Console.ForegroundColor = ConsoleColor.Yellow;
			tw.Write(name);
			if (tw == Console.Out)
				Console.ResetColor();
			tw.WriteLine(" " + new String('=', System.Math.Max(80 - name.Length, 5)));
			sw.Start();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Dispose()
		{
			sw.Stop();
			String time;
			Double ms = sw.Elapsed.TotalMilliseconds;
			if (ms < 10000)
				time = ms.ToString("0") + " ms";
			else
			{
				ms /= 1000.0;
				if (ms < 120)
					time = ms.ToString("0.00") + " s";
				else
					time = sw.Elapsed.ToString("hh\\:mm\\:ss");
			}
			String m = "ok: " + time;
			tw.Write("============================================================ ");
			if (tw == Console.Out)
				Console.ForegroundColor = ConsoleColor.Green;
			tw.Write(m);
			if (tw == Console.Out)
				Console.ResetColor();
			tw.WriteLine(" " + new String('=', 30 - m.Length));
			sw = null;
			tw = null;
		}
	};

	public static class _debugging_ext
	{
		const int PercentCols = 120;
		static ConsoleColor[] color_range = 
		{ 
			ConsoleColor.DarkRed, 
			ConsoleColor.Cyan, 
			ConsoleColor.Gray, 
			ConsoleColor.Magenta, 
			ConsoleColor.Green, 
		};
		public static void PercentColorBars(params long[] nums)
		{
			lock (Console.Out)
			{
				Double tot = nums.Sum();
				Double[] dpcts = new Double[nums.Length];
				int[] pcts = new int[nums.Length];

				int over = -PercentCols;
				for (int i = 0; i < nums.Length; i++)
				{
					dpcts[i] = (nums[i] * 100) / tot;
					over += pcts[i] = (int)System.Math.Round((nums[i] * PercentCols) / tot);
				}

				if (over > 0)
					pcts[alib.Enumerable._enumerable_ext.IndexOfMax(dpcts)] -= over;
				else if (over < 0)
					pcts[alib.Enumerable._enumerable_ext.IndexOfMin(dpcts)] -= over;

				Console.ForegroundColor = ConsoleColor.Black;
				String overflow = String.Empty;
				for (int i = 0; i < nums.Length; i++)
				{
					Console.BackgroundColor = color_range[i];
					int pct = pcts[i];
					String s = overflow + String.Format(" {0:0.##} ", dpcts[i]);

					if (i > 0 && overflow == String.Empty && pct <= 1)
						s = s.TrimStart();

					if (s.Length > pct)
					{
						overflow = s.Substring(pct);
						s = s.Remove(pct);
					}
					else
					{
						overflow = String.Empty;
						s = s.PadRight(pct);
					}
					Console.Write(s);
				}
				Console.ResetColor();
				Console.WriteLine();
			}
		}

		static String[] ccx = { "black", "darkblue", "darkgreen", "darkcyan", "darkred",
								 "darkmagenta", "darkyellow", "gray", "darkgray", 
								 "blue", "green", "cyan", "red", "magenta", "yellow", "white" };

		public const ConsoleColor ConsoleColorReset = (ConsoleColor)(-1);

		public static void WriteLineColor(this TextWriter tw, String fmt, params Object[] args)
		{
			_i2(tw, true, fmt + Environment.NewLine, args);
		}

		public static void WriteColor(this TextWriter tw, String fmt, params Object[] args)
		{
			_i2(tw, true, fmt, args);
		}

		public static void WriteLineColorNoSync(String fmt, params Object[] args)
		{
			_i2(Console.Out, false, fmt + Environment.NewLine, args);
		}

		public static void WriteColorNoSync(String fmt, params Object[] args)
		{
			_i2(Console.Out, false, fmt, args);
		}

		static void _i2(TextWriter tw, bool f_lock, String fmt, params Object[] args)
		{
			var z = args.Length > 0 ? String.Format(fmt, args) : fmt;

			var ies = ((IEnumerable<String>)z.Split(alib.Character.Charset.dollar, StringSplitOptions.None)).GetEnumerator();
			if (!ies.MoveNext())
				return;

			if (f_lock)
				Monitor.Enter(tw);

			_inner(tw, ies);

			if (f_lock)
				Monitor.Exit(tw);
		}

		static void _inner(TextWriter tw, IEnumerator<String> ies)
		{
			String part, q;

			tw.Write(ies.Current);

			ConsoleColor _new, _cur = ConsoleColorReset;
			while (ies.MoveNext())
			{
				if ((part = ies.Current).Length == 0)
				{
					if (_cur != ConsoleColorReset)
					{
						Console.ResetColor();
						_cur = ConsoleColorReset;
					}
				}
				else
				{
					var seek = part.ToLower();
					for (_new = (ConsoleColor)(ccx.Length - 1); _new >= 0; _new--)
					{
						if (seek.StartsWith(q = ccx[(int)_new]))
						{
							part = part.Substring(q.Length);
							break;
						}
					}
					if (_cur != _new)
					{
						if ((_cur = _new) == ConsoleColorReset)
							Console.ResetColor();
						else
							Console.ForegroundColor = _cur;
					}
					if (part.Length > 0)
						tw.Write(part);
				}
			}
			if (_cur != ConsoleColorReset)
				Console.ResetColor();
		}
	};
#endif
}
