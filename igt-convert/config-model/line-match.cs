using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using alib.Debugging;
using alib.Enumerable;

namespace xigt2.config
{
	[DebuggerDisplay("{ToString(),nq}")]
	public class _line_match
	{
		static readonly System.Reflection.FieldInfo __fi_text = typeof(Match)
				.GetField("_text", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

		public _line_match(content_pattern_base pat, Match m)
		{
			Debug.Assert(m.Success);

			this.pattern = pat;
			this.m = m;

			//this.consume_matches = new String[m.Groups.Count];
			//for (int i = 0; i < consume_matches.Length; i++)
			//	consume_matches[i] = m.Groups[i].Value;
		}

		public String filename;
		public int ix_file;
		public int ix_igt;

		//public String[] consume_matches;

		public content_pattern_base pattern;

		public String SourceText { get { return (String)__fi_text.GetValue(m); } }

		Match m;
		public String MatchedText { get { return m.ToString(); } }

		public String _matched_text_display
		{
			get { return SourceText.Insert(m.Index + m.Length, "◀").Insert(m.Index, "▶"); }	//◤◢
		}

		public int GroupCount { get { return m.Groups.Count; } }

		public Group this[int ix] { get { return m.Groups[ix]; } }

		public override String ToString()
		{
			var s = SourceText;
			return String.Format("{0,21}-{1,-3} {2,1} [{3}]",
				pattern._dbg_name,
				ix_igt,
				pattern.IsTierInstance ? "T" : "",
				s == MatchedText ? s : _matched_text_display);
		}
	};
}