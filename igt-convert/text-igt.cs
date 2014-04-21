using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using alib;
using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	public class SourceFileInfo
	{
		public String Filename { get; set; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public String DocId { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int FromLine { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int ToLine { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public String Language { get; set; }
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class TextIgt : SourceFileInfo, IReadOnlyList<RawTextLine>
	{
		public TextIgt(String filename)
		{
			this.Filename = filename;
		}

		public TextIgt(String filename, String[] s_lines)
			: this(filename)
		{
			this.s_raw = s_lines.StringJoin(Environment.NewLine);

			int line_ix = 0;

			var s_docid = s_lines[line_ix++];
			if (!s_docid.StartsWith("doc_id="))
				throw new Exception();
			s_docid = s_docid.Substring(7);

			var rgs_parts = s_docid.Split(default(Char[]), StringSplitOptions.RemoveEmptyEntries);
			this.DocId = rgs_parts[0];

			if (s_lines[line_ix].StartsWith("language: "))
				this.Language = s_lines[line_ix++].Substring(10);
			else
				this.Language = "";

			this.FromLine = int.Parse(rgs_parts[1]);
			this.ToLine = int.Parse(rgs_parts[2]);

			this.content = new RawTextLine[s_lines.Length - line_ix];

			int _cur = FromLine;
			for (int i = 0; ; i++)
			{
				content[i] = new RawTextLine(s_lines[line_ix]);

				if (content[i].Line != _cur)
				{
					Debug.Print("Warning: '{0}' in file '{1}': correcting incorrect line number range in header", s_docid, filename);
					this.FromLine = -1;
					_cur = content[i].Line;
				}
				if (++line_ix == s_lines.Length)
					break;
				_cur++;
			}
			if (_cur != ToLine)
			{
				Debug.Print("Warning: '{0}' in file '{1}': correcting incorrect line number range in header", s_docid, filename);
				this.ToLine = -1;
			}

			if (this.FromLine < 0 || this.ToLine < 0)
			{
				this.FromLine = content.Min(rt => rt.Line);
				this.ToLine = content.Max(rt => rt.Line);
			}
		}

		public String s_raw;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		RawTextLine[] content;

		public RawTextLine this[int index] { get { return content[index]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count { get { return content.Length; } }

		public IEnumerator<RawTextLine> GetEnumerator() { return content.Enumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public static Igt ToIgt(TextIgt tigt) { return tigt.ToIgt(); }
		Igt ToIgt()
		{
			var ii = new Igt
			{
				//Name = this.ToString(),
				DocId = this.DocId,
				FromLine = this.FromLine,
				ToLine = this.ToLine,
				Language = this.Language,
			};

			ii.Add(new TextTier
			{
				Text = this.s_raw,
				TierType = "odin-txt",
			});

			TierGroupTier tier_L = null, tier_G = null, tier_T = null, tier_M = null;
			//TierGroupTier tier_Other = null;

			foreach (var raw in this)
			{
				var tags = ParseTags(raw.Tag);

				bool unk = true;
				String prev_tag = null;
				foreach (var _tag in tags)
				{
					String tag = prev_tag != null && _tag == "C" ? prev_tag : _tag;
					switch (tag)
					{
						case "L":
							if (tier_L == null)
								tier_L = new TierGroupTier { TierType = "Lang" };
							tier_L.Add(new TextTier { Text = raw.Content, TierType = raw.Tag + "-" + raw.Line });
							unk = false;
							break;
						case "-G":
						case "G":
							if (tier_G == null)
								tier_G = new TierGroupTier { TierType = "Gloss" };
							tier_G.Add(new TextTier { Text = raw.Content, TierType = raw.Tag + "-" + raw.Line });
							unk = false;
							break;
						case "-T":
						case "T":
							if (tier_T == null)
								tier_T = new TierGroupTier { TierType = "Transl." };
							tier_T.Add(new TextTier { Text = raw.Content, TierType = raw.Tag + "-" + raw.Line });
							unk = false;
							break;
						case "M":
							if (tier_M == null)
								tier_M = new TierGroupTier { TierType = "Misc." };
							tier_M.Add(new TextTier { Text = raw.Content, TierType = raw.Tag + "-" + raw.Line });
							unk = false;
							break;
						case "B":
							unk = false;
							break;
						default:
							//if (tier_Other == null)
							//	tier_Other = new TierGroupTier { TierType = "Other" };
							//tier_Other.Add(new TextTier { Text = raw.Content, TierType = raw.Tag });
							break;
					}
					prev_tag = tag;
				}

				if (unk)
				{
					ii.Add(new TextTier
					{
						Text = raw.Content,
						TierType = raw.Tag,
					});
				}
			}

			if (tier_L != null)
				ii.Add(tier_L);
			if (tier_G != null)
				ii.Add(tier_G);
			if (tier_T != null)
				ii.Add(tier_T);
			if (tier_M != null)
				ii.Add(tier_M);

			return ii;
		}

		static List<String> ParseTags(String _s_tags)
		{
			var ret = new List<String>();
			var s_tags = new StringBuilder(_s_tags);

			if (s_tags.Length != s_tags.Replace("+AC", String.Empty).Length)	// citation
				ret.Add("+AC");
			if (s_tags.Length != s_tags.Replace("+AL", String.Empty).Length)	// alteratives
				ret.Add("+AL");
			if (s_tags.Length != s_tags.Replace("+CR", String.Empty).Length)	// corrupted
				ret.Add("+CR");
			if (s_tags.Length != s_tags.Replace("+CN", String.Empty).Length)	// citation
				ret.Add("+CN");
			if (s_tags.Length != s_tags.Replace("+DB", String.Empty).Length)	// double column
				ret.Add("+DB");
			if (s_tags.Length != s_tags.Replace("+EX", String.Empty).Length)	// 
				ret.Add("+EX");
			if (s_tags.Length != s_tags.Replace("+LN", String.Empty).Length)	// language name
				ret.Add("+LN");
			if (s_tags.Length != s_tags.Replace("+LT", String.Empty).Length)	// literal translation
				ret.Add("+LT");
			if (s_tags.Length != s_tags.Replace("+SY", String.Empty).Length)	// syntactic construction
				ret.Add("+SY");
			if (s_tags.Length != s_tags.Replace("-G", String.Empty).Length)
				ret.Add("-G");
			if (s_tags.Length != s_tags.Replace("-T", String.Empty).Length)
				ret.Add("-T");
			if (s_tags.Length != s_tags.Replace("L", String.Empty).Length)
				ret.Add("L");
			if (s_tags.Length != s_tags.Replace("G", String.Empty).Length)
				ret.Add("G");
			if (s_tags.Length != s_tags.Replace("T", String.Empty).Length)
				ret.Add("T");
			if (s_tags.Length != s_tags.Replace("B", String.Empty).Length)		// blank
				ret.Add("B");
			if (s_tags.Length != s_tags.Replace("M", String.Empty).Length)		// misc
				ret.Add("M");
			if (s_tags.Length != s_tags.Replace("C", String.Empty).Length)		// continuation
				ret.Add("C");

			if (s_tags.Length > 0)
				Debug.Print("unprocessed tag(s): {0} in {1}", s_tags, _s_tags);

			return ret;
		}

		public override String ToString()
		{
			var s = String.Format("doc_id={0} <{1}-{2}>", DocId, FromLine, ToLine);
			if (!String.IsNullOrWhiteSpace(Language))
				s += " language=" + Language;
			return s;
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class RawTextLine
	{
		public RawTextLine(int Line, String Tag, String Content)
		{
			this.Line = Line;
			this.Tag = Tag;
			this.Content = Content;
		}
		public RawTextLine(String s_line)
		{
			int ixc = s_line.IndexOf(':');
			this.Content = s_line.Substring(ixc + 1);
			var rgs = s_line.Substring(0, ixc).Split(default(Char[]), StringSplitOptions.RemoveEmptyEntries);
			if (rgs.Length != 2)
				throw new Exception();

			if (!rgs[0].StartsWith("line="))
				throw new Exception();
			this.Line = int.Parse(rgs[0].Substring(5));

			if (!rgs[1].StartsWith("tag="))
				throw new Exception();
			this.Tag = rgs[1].Substring(4);
			//if (tags.Add(this.Tag))
			//	Debug.Print("{0}", this.Tag);
		}
		public int Line { get; set; }
		public String Tag { get; set; }
		public String Content { get; set; }

		public override String ToString()
		{
			return String.Format("line={0} tag={1} [{2}]", Line, Tag, Content);
		}

		//static HashSet<String> tags = new HashSet<string>();
	};
}
