using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using alib.Enumerable;

namespace alib.Wpf
{
	using String = System.String;

	public static class Html
	{
		/// umm, wtf?
		static Dictionary<String, Func<Span, Dictionary<String, String>, Dictionary<String, String>, Span>> tag_dict =
		new Dictionary<String, Func<Span, Dictionary<String, String>, Dictionary<String, String>, Span>>
			{
				{ "a",  (cur,avd,sd) =>
					{
						if (avd == null)
							return null;
						Hyperlink h = new Hyperlink();
						foreach (var kvp in avd)
						{
							if (kvp.Key=="href")
							{
								h.NavigateUri = new Uri(kvp.Value);
							}
						}
						return h;
					}
				},
				{ "i",  (cur,avd,sd) =>
					{
						Span sp = new Span();
						sp.FontStyle = FontStyles.Italic;
						return sp;
					}
				},
				{ "pre",  (cur,avd,sd) =>
					{
						Span sp = new Span();
						sp.FontFamily = new FontFamily("Consolas");
						return sp;
					}
				},
				{ "b",  (cur,avd,sd) =>
					{
						Span sp = new Span();
						sp.FontWeight = FontWeights.Bold;
						return sp;
					}
				},
				{ "u",  (cur,avd,sd) =>
					{
						Span sp = new Span();
						sp.TextDecorations.Add(TextDecorations.Underline);
						return sp;
					}
				},
				{ "br",  (cur,avd,sd) =>
					{
						cur.Inlines.Add(new LineBreak());
						return null;
					}
				},
				{ "hr",  (cur,avd,sd) =>
					{
						cur.Inlines.Add(new InlineUIContainer
						{
							Child = new System.Windows.Controls.Separator
							{
								Width = 200,
								Margin = new Thickness(0,-10,0,-10),
							}
						});
						cur.Inlines.Add(new LineBreak());
						return null;
					}
				},
				{ "sup", (cur,avd,sd) =>
					{
						Span sp = new Span();
						sp.BaselineAlignment = BaselineAlignment.Superscript;
						sp.FontSize = 9;
						return sp;
					}
				},
				{ "span",  (cur,avd,sd) =>
					{
						if (sd == null)
							return null;
						Span sp = new Span();
						foreach (var kvp in sd)
						{
							if (kvp.Key=="color")
							{
								try {
									Color z = (Color)ColorConverter.ConvertFromString(kvp.Value);
									sp.Foreground = new SolidColorBrush(z);
								}
								catch
								{
								}
							}
							else if (kvp.Key=="font-weight" && kvp.Value=="bold")
							{
								sp.FontWeight = FontWeights.Bold;
							}
							else if (kvp.Key=="font-style" && kvp.Value=="italic")
							{
								sp.FontStyle = FontStyles.Italic;
							}
						}
						return sp;
					}
				},
			};

		struct StackEntry
		{
			public StackEntry(String tag, Span obj)
			{
				this.tag = tag;
				this.obj = obj;
			}
			public String tag;
			public Span obj;
		};

		enum TagType { None = 0, Open, Close, AutoClose };

		static Char[] tag_term = { ' ', '\t', '\r', '\n' };
		static Char[] style_split = { ':', ';' };

		public static Span HTMLtoWPF(String s)
		{
			Stack<StackEntry> stk = new Stack<StackEntry>();
			Span sp_base = new Span();
			Span sp_cur = sp_base;

			/// remove control characters
			//s = s.Select(ch => ch < ' ' ? ' ' : ch).NewString();

			/// condense spaces
			int ix;
			while ((ix = s.IndexOf("  ")) != -1)
				s = s.Remove(ix, 1);

			/// find HTML tags
			String[] parts = s.Split(new[] { '<' });

			for (int i = 0; i < parts.Length; i++)
			{
				String part = parts[i];
				if (i > 0)
				{
					ix = part.IndexOf('>');
					if (ix < 1)
					{
						sp_cur.Inlines.Add(new Run("HTML parse error, missing closing tag '>' or empty HTML tag"));
						break;
					}
					/// Get the HTML tag type
					String s_tag = part.Remove(ix).Trim().ToLower();
					part = part.Substring(ix + 1);

					/// is it a closing tag?
					TagType found_tag_type = TagType.Open;
					if (s_tag[0] == '/')
					{
						found_tag_type = TagType.Close;
						s_tag = s_tag.Substring(1);
					}
					else if (s_tag.EndsWith("/"))
					{
						found_tag_type = TagType.AutoClose;
						s_tag = s_tag.Remove(s_tag.Length - 1).Trim();
					}

					String s_av = null;
					ix = s_tag.IndexOfAny(tag_term);
					if (ix != -1)
					{
						s_av = s_tag.Substring(ix + 1).Trim();
						s_tag = s_tag.Remove(ix).Trim();
					}

					if (found_tag_type == TagType.Open || found_tag_type == TagType.AutoClose)
					{
						/// get attribute-value pairs if any
						Dictionary<String, String> attribute_values = null;
						Dictionary<String, String> styles = null;
						if (s_av != null)
						{
							var rgav = s_av.Split('=');
							if ((rgav.Length & 1) > 0)
								throw new Exception();

							attribute_values = rgav.PairOff().ToDictionary(av => av.x, av => av.y.Trim('\'', '\"'));

							String sp;
							if (attribute_values.TryGetValue("style", out sp))
							{
								var rgsp = sp.Split(style_split, StringSplitOptions.RemoveEmptyEntries);
								if ((rgsp.Length & 1) > 0)
									throw new Exception();

								styles = rgsp.PairOff().ToDictionary(av => av.x.Trim(), av => av.y.Trim());
							}
						}

						Func<Span, Dictionary<String, String>, Dictionary<String, String>, Span> open_func;
						if (!tag_dict.TryGetValue(s_tag, out open_func))
						{
							sp_cur.Inlines.Add(new Run(String.Format("HTML parse error, unrecognized tag '{0}'", s_tag)));
							break;
						}

						Span ns = open_func(sp_cur, attribute_values, styles);
						if (ns != null && found_tag_type != TagType.AutoClose)
						{
							stk.Push(new StackEntry(s_tag, sp_cur));
							sp_cur.Inlines.Add(ns);

							sp_cur = ns;
						}
					}
					else if (found_tag_type == TagType.Close)
					{
						if (stk.Count == 0)
						{
							sp_cur.Inlines.Add(new Run(String.Format("HTML parse error, can't close tag '{0}' because there are no tags open", s_tag)));
							break;
						}
						if (stk.Peek().tag != s_tag)
						{
							sp_cur.Inlines.Add(new Run(String.Format("HTML parse error, can't close tag '{0}' because it is not the pending tag", s_tag)));
							break;
						}
						sp_cur = stk.Pop().obj;
					}
				}

				/// Add the unformatted text
				if (part.Length > 0)
				{
					var xx = part.Replace("&lt;", "<").Replace("&gt;", ">").Replace("\r\n", "\n").Replace('\r', '\n');
					sp_cur.Inlines.Add(new Run(xx));
				}
			}
			sp_cur.Inlines.Add(new LineBreak());

			if (stk.Count != 0)
				throw new Exception();

			return sp_base;
		}
	};
}
