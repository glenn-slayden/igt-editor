using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;

using alib.Debugging;
using alib.Enumerable;

namespace xigt2.config
{
	[DebuggerDisplay("{ToString(),nq}")]
	//[ContentProperty("Patterns")]
	[UsableDuringInitialization(true)]
	public abstract class Pattern : name_base
	{
		public Pattern()
		{
			//this.children = new List<Pattern>();
		}

		//public XigtConfig xc;

		internal Regex rx;

		public String Expression { get; set; }

		//readonly List<Pattern> children;
		//public IList<Pattern> Patterns { get { return children; } }

		public abstract bool allow_multiple();
		public abstract bool continue_matching();
		//public abstract bool consume();

		public _line_match FindMatch(String text)
		{
			var m = rx.Match(text);
			if (!m.Success)
				return null;

			Debug.Assert(m.Captures.Count == 1);

			return new _line_match(this as content_pattern_base, m);
		}

		public override void EndInit()
		{
			base.EndInit();

			if (this.Expression == null)
				throw new Exception("Regular expression or match text is required");
			this.rx = new Regex(this.Expression, RegexOptions.Singleline | RegexOptions.Compiled);
		}

		public String _dbg_name
		{
			get
			{
				var h = (uint)this.GetHashCode();
				h ^= (h >> 16 << 9) ^ (~h >> 23);
				return this.GetType().Name.Replace("Pattern", "") + "-" + ((ushort)h).ToString("X4");
			}
		}

		public override String ToString()
		{
			return String.Format("{0,22} {1,1} {{ {2} }}",
				_dbg_name,
				this is content_pattern_base && ((content_pattern_base)this).IsTierInstance ? "T" : "",
				rx == null ? "(null)" : rx.ToString());
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class IgtDelimiterPattern : Pattern
	{
		public override void EndInit()
		{
			if (String.IsNullOrEmpty(this.Name))
				this.Name = "_igt_delim";
			base.EndInit();
		}

		public override bool allow_multiple() { return false; }
		public override bool continue_matching() { return false; }
		//public override bool consume() { return false; }
	};

	//[DebuggerDisplay("{ToString(),nq}")]
	//public abstract class content_pattern_base : Pattern
	//{
	//};

	//[DebuggerDisplay("{ToString(),nq}")]
	//public sealed class TierInstancePattern : content_pattern_base
	//{
	//	public TierInstancePattern()
	//	{
	//	}

	//	public override bool allow_multiple() { return true; }
	//	public override bool continue_matching() { return true; }
	//};

	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class content_pattern_base : Pattern
	{
		public content_pattern_base()
		{
			this.AllowMultiple = false;
			this.ContinueMatching = true;
			//this.Consume = false;
		}

		public bool AllowMultiple { get; set; }
		public sealed override bool allow_multiple() { return this.AllowMultiple; }

		public bool ContinueMatching { get; set; }
		public sealed override bool continue_matching() { return this.ContinueMatching; }

		public bool IsTierInstance { get; set; }

		//public Boolean Consume { get; set; }
		//public sealed override bool consume() { return this.Consume; }
		public override void EndInit()
		{
			if (AllowMultiple)
			{
				if (!ContinueMatching)
					throw new Exception("'AllowMultiple' should specify 'ContinueMatching'");
			}
			else
			{
				if (IsTierInstance)
					throw new Exception("'IsTierInstance' should specify 'AllowMultiple'");
			}
			//if (Consume && !ContinueMatching)
			//	throw new Exception("'ContinueMatching' should be enabled, because otherwise 'Consume' is vacuous");
			base.EndInit();
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class SourceLinePattern : content_pattern_base
	{
		public SourceLinePattern()
		{
			this.Ignore = false;
		}
		public Boolean Ignore { get; set; }

		//public override void EndInit()
		//{
		//	base.EndInit();

		//	foreach (var p in Patterns.OfType<SourceValuePattern>())
		//	{
		//		p._value_source = new SourceValueRef(this, p.SourceIndex);
		//	}
		//}
	};


	[DebuggerDisplay("{ToString(),nq}")]
	public class SourceValuePattern : content_pattern_base, ISourceTextProvider
	{
		public SourceValuePattern()
		{
			this.Ignore = false;
		}

		public Boolean Ignore { get; set; }

		public int SourceIndex { get; set; }

		//public ISourceTextProvider _value_source;
		public ISourceTextProvider ValueSource { get; set; }

		public ValueMap ValueMap { get; set; }

		public bool TryGetSourceValue(IReadOnlyList<_line_match> rglm, out Object value)
		{
			//var q = _value_source.TryGetSourceValue(rglm, out value);
			var q = ValueSource.TryGetSourceValue(rglm, out value);

			return q;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class SourceValueRef : MarkupExtension, ISourceTextProvider
	{
		public SourceValueRef(content_pattern_base source, int index)
		{
			this.Source = source;
			this.Index = index;
		}
		public SourceValueRef(content_pattern_base source)
			: this(source, 0)
		{
		}
		public SourceValueRef()
			: this(default(content_pattern_base))
		{
		}

		public content_pattern_base Source { get; set; }

		public int Index { get; set; }

		public override object ProvideValue(IServiceProvider sp)
		{
			return this;
		}

		public bool TryGetSourceValue(IReadOnlyList<_line_match> rglm, out Object value)
		{
			value = null;
			var lm = rglm.Where(x => this.Source == x.pattern).ToArray();
			if (lm.Length == 0)
				return false;

			if (lm.Length > 1)
				throw new Exception();

			//value = lm.consume_matches[this.Index];
			value = lm[0][this.Index].Value;
			return true;
		}
	};
}
