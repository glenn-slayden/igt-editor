using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;

using alib.Collections;
using alib.Debugging;
using alib.Enumerable;

namespace xigt2.config
{
	public interface ISourceTextProvider
	{
		bool TryGetSourceValue(IReadOnlyList<_line_match> rglm, out Object value);
	};


	public class SourceDescriptors : List<Pattern>, IList
	{
		public SourceDescriptors(XigtConfig xc)
		{
			this.xc = xc;
		}
		readonly XigtConfig xc;

		//int IList.Add(Object value)
		//{
		//	var pat = (Pattern)value;
		//	pat.xc = this.xc;
		//	base.Add(pat);
		//	return base.Count - 1;
		//}

		//void IList.Insert(int index, Object value)
		//{
		//	var pat = (Pattern)value;
		//	pat.xc = this.xc;
		//	base.Insert(index, pat);
		//}
	};

	public class XigtConfig : ISupportInitialize//, IReadOnlyDictionary<String, name_base>
	{
		//class _xow : XamlObjectWriter
		//{
		//	public _xow(XamlSchemaContext ctx) : base(ctx) { }

		//	public override void WriteEndObject()
		//	{
		//		base.WriteEndObject();
		//	}
		//	protected override bool OnSetValue(object eventSender, XamlMember member, object value)
		//	{
		//		var ret = base.OnSetValue(eventSender, member, value);

		//		var pat = eventSender as Pattern;
		//		if (pat != null)
		//		{
		//			Nop.X();
		//		}

		//		return ret;
		//	}

		//	public override void WriteStartMember(XamlMember property)
		//	{
		//		base.WriteStartMember(property);
		//	}
		//};


		public static XigtConfig Load()
		{
			var rs = typeof(XigtConfig).Assembly.GetManifestResourceStream("xigt2.odin.xigt-config");

			var ctx = new XamlSchemaContext(new XamlSchemaContextSettings { SupportMarkupExtensionsWithDuplicateArity = true });

			var xr = new XamlXmlReader(rs, ctx);

			var xw = new XamlObjectWriter(ctx);

			XamlServices.Transform(xr, xw);

			var xc = (XigtConfig)xw.Result;

			//xc.xaml_ns = (INameScopeDictionary)xw.RootNameScope;

			return xc;
		}

		public XigtConfig()
		{
			this.IgnoreUnmatchedSourceLines = false;
			this.IgnoreEmptyIgts = true;
			this.resources = new List<Object>();
			this.rglpm = new SourceDescriptors(this);
			this.igt_attribs = new List<attr_base>();
			this.tier_attribs = new List<attr_base>();
		}

		public void BeginInit() { }

		//content_pattern_base[] required;
		public void EndInit()
		{
			var cd = rglpm.IndexOfType<IgtDelimiterPattern>();
			if (cd >= 0 && cd != rglpm.Count - 1)
				throw new Exception("can only specify one Igt delimiter and it must be last");

			//required = rglpm.OfType<content_pattern_base>().Where(x => x.required()).ToArray();
		}

		readonly List<Object> resources;
		public IList Resources { get { return resources; } }

		readonly SourceDescriptors rglpm;
		public SourceDescriptors SourceDescriptors { get { return rglpm; } }

		readonly List<attr_base> igt_attribs;
		public List<attr_base> IgtAttributes { get { return igt_attribs; } }

		readonly List<attr_base> tier_attribs;
		public List<attr_base> TierAttributes { get { return tier_attribs; } }

		public bool IgnoreUnmatchedSourceLines { get; set; }

		public bool IgnoreEmptyIgts { get; set; }

		public IgtCorpus LoadTxtFile(String filename)
		{
			var corp = new IgtCorpus { Filename = filename };

			////////////////////////////

			foreach (var item in File.ReadAllLines(filename)
							.Select(s => String.IsNullOrWhiteSpace(s) ? String.Empty : s)
							.Partition(String.Empty)
							.Select(raw => new TextIgt(filename, ((RefList<String>)raw).GetTrimmed()))
							.Select(TextIgt.ToIgt))
			{
				corp.Add(item);
			}

			////////////////////////////
#if false
			foreach (var llm in ProcessSourceLines(filename))
			{
				Debug.Assert(llm.IsSorted(x => x.ix_igt));

				var ii = new Igt
				{
					IgtCorpus = corp
				};
#if false
					foreach (var q in llm)
					{
						//q.match

						Debug.WriteLine("{0} {1,5} {2,3} {3,3} {4}",
								q.pattern.Name.Substring(9),
								q.ix_file,
								q.ix_igt,
								q.GroupCount,
								q._matched_text_display);
						//foreach (var gg in q.match.Groups.OfType<Group>().Skip(1))
						//	Debug.WriteLine("\t" + gg.Value);
					}
					Debug.WriteLine("");
#endif

#if false
					foreach (var ta in igt_attribs)
					{
						Object v;
						if (ta.TryGetSourceValue(llm, out v))
						{
							ii[ta] = v;
						}
						else
						{
							throw new Exception();
						}
					}

					var tier_groups = llm
								.GroupBy(x => x.ix_igt)
								.Where(g => g.Any(y => y.pattern.IsTierInstance))
								.ToArray();

					//var qqq = llm.Select(qq => qq.pattern as ISourceTextProvider).ToArray();

					var nnn = llm.Select(qq => qq.pattern.Name).ToArray();

					if (llm.Length >= 14)
						Nop.X();

					Nop.X();

					foreach (var tt in llm.Where(x => x.pattern.IsTierInstance))
					{
						if (ii.ContainsTierSourceLineIndex(tt.ix_igt))
							throw new Exception("All of the TierInstancePattern(s) must jointly identify distinct source lines");

						var tier = new Tier(ii, tt.ix_igt);

						//var llm_tier = llm.Where(x=>x.pattern

						var xxx = llm[0].pattern._dbg_name;



			

						foreach (var xa in tier_attribs)
						{
							//Object v;
							//if (xa.TryGetSourceValue(llm, out v))
							//{
							//	Nop.X();
							//}
							//else
							//{
							//	Nop.X();
							//}
						}

						ii.Add(tier);
					}

					if (llm.Length >= 14)
						Nop.X();

					//corp.Add(ii);
#endif
			}
#endif
			return corp;
		}


		IEnumerable<_line_match[]> ProcessSourceLines(String filename)
		{
			var llm = new RefList<_line_match>();

			int ix_igt = 0, ix_line = 0;
			foreach (var _ss in File.ReadAllLines(filename))
			{
				ix_line++;
				ix_igt++;

				int c_line_matches = 0;
				//var consumed = _ss;
				int c_prev = llm.Count;

				//var descriptors = rglpm.SelectMany(x => x.Patterns.Prepend(x)).ToArray();

				foreach (var pat in rglpm)
				//foreach (var pat in descriptors)
				{
					if (!pat.allow_multiple() && llm.Any(_lm => pat.Name == _lm.pattern.Name))
						continue;

					//var ss = consumed;
					var ss = _ss;

					var spv = pat as ISourceTextProvider;
					if (llm.Count > 0 && spv != null)
					{
						Object sub;
						if (spv.TryGetSourceValue(llm.Skip(c_prev).ToArray(), out sub))
						{
							var ssub = sub as String ?? (String)Convert.ChangeType(sub, typeof(String));
							if (ssub == null)
								throw new Exception();
							ss = ssub;
						}
					}

					var __lm = pat.FindMatch(ss);
					if (__lm == null)
						continue;

					c_line_matches++;

					//if (pat.consume())
					//	consumed = ss.ZeroStringChars(__lm.match.Index, __lm.match.Length);

					if (pat is IgtDelimiterPattern)
					{
						if (llm.Count > 0 || !IgnoreEmptyIgts)
						{
							//if (required.Length > 0)
							//{
							//	var sld_absent = required.FirstOrDefault(_pat => llm.None(_lm => _pat.Name == _lm.pattern.Name));
							//	if (sld_absent != null)
							//		throw new Exception(String.Format("A source line matching pattern '{0}' is required", sld_absent));
							//}

							yield return llm.GetTrimmed();
							llm.Clear();
						}
						ix_igt = 0;
					}
					else
					{
						//if (ss != consumed)
						//	Debug.Print("[{0}] [{1}]", ss.Replace('\0', '_'), consumed.Replace('\0', '_'));

						__lm.filename = filename;
						__lm.ix_file = ix_line;
						__lm.ix_igt = ix_igt;

						llm.Add(__lm);
					}
					if (!pat.continue_matching())
						break;
				}
				if (c_line_matches == 0 && !IgnoreUnmatchedSourceLines)
					throw new Exception("XigtConfig: no LinePatternMatch was found for: " + _ss);
			}
		}

		//public bool ContainsKey(String key) { return xaml_ns.ContainsKey(key); }

		//public IEnumerable<String> Keys { get { return xaml_ns.Keys; } }

		//public bool TryGetValue(String key, out name_base value)
		//{
		//	Object v;
		//	bool b = xaml_ns.TryGetValue(key, out v);
		//	value = v as name_base;
		//	return !b || v == null;
		//}

		//public IEnumerable<name_base> Values { get { return xaml_ns.Values.Cast<name_base>(); } }

		//public name_base this[String key] { get { return (name_base)xaml_ns[key]; } }

		//public int Count { get { return xaml_ns.Count; } }

		//public IEnumerator<KeyValuePair<String, name_base>> GetEnumerator() { return xaml_ns.Cast<KeyValuePair<String, name_base>>().GetEnumerator(); }

		//IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	};


	public class SourceContext
	{

	};


	[ContentProperty("Mappings")]
	public class ValueMap
	{
		public ValueMap()
		{
			this.m = new List<Mapping>();
		}
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly List<Mapping> m;
		public List<Mapping> Mappings { get { return m; } }
	};


	[DebuggerDisplay("{ToString(),nq}")]
	[ContentProperty("Value")]
	public class Mapping
	{
		public Object Key { get; set; }
		public Object Value { get; set; }

		public override String ToString()
		{
			return (this.Key ?? (Object)"(null)").ToString() + " -> " + (this.Value ?? (Object)"(null)").ToString();
		}
	};


	[RuntimeNameProperty("Name")]
	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class name_base : ISupportInitialize
	{
		public name_base()
		{
		}

		public String Name { get; set; }

		public void BeginInit() { }

		public virtual void EndInit()
		{
			if (String.IsNullOrEmpty(this.Name))
				throw new Exception(this.GetType().Name + " must specify a 'Name'");
		}

		public override String ToString()
		{
			return this.GetType().Name + " \"" + (this.Name ?? "(no name)") + "\"";
		}
	};


	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class attr_base : name_base, ISourceTextProvider
	{
		public attr_base()
		{
			this.IsPersistent = true;
		}

		public String ShortName { get; set; }

		public bool IsRequired { get; set; }

		public bool IsHidden { get; set; }

		public bool IsPersistent { get; set; }

		public bool IsReadOnly { get; set; }

		public bool IsWriteOnce { get; set; }

		public ISourceTextProvider ValueSource { get; set; }

		public virtual bool TryGetSourceValue(IReadOnlyList<_line_match> rglm, out Object value)
		{
			if (ValueSource == null)
			{
				value = null;
				return false;
			}
			return ValueSource.TryGetSourceValue(rglm, out value);
		}

		//public override void EndInit()
		//{
		//	base.EndInit();

		//	if (IsRequired && !f_default_set)
		//		throw new Exception((Name ?? "") + ": IsRequired requires a DefaultValue");
		//}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class attr_base<T> : attr_base
	{
		bool f_default_set;
		T default_value;

		public bool HasDefaultValue { get { return f_default_set; } }

		public void ClearDefaultValue() { default_value = default(T); }

		public T DefaultValue
		{
			get
			{
				if (!f_default_set)
					throw new Exception("Default value not set");
				return default_value;
			}
			set
			{
				f_default_set = (default_value = value) != null;
			}
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class BooleanAttribute : attr_base<Boolean>
	{
		public override bool TryGetSourceValue(IReadOnlyList<_line_match> rglm, out object value)
		{
			if (!base.TryGetSourceValue(rglm, out value))
				return false;
			if (!(value is bool))
				value = Convert.ChangeType(value, typeof(bool));
			return value != null;
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class IntegerAttribute : attr_base<int>
	{
		public override bool TryGetSourceValue(IReadOnlyList<_line_match> rglm, out object value)
		{
			if (!base.TryGetSourceValue(rglm, out value))
				return false;
			if (!(value is int))
				value = Convert.ChangeType(value, typeof(int));
			return value != null;
		}
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class attr_string : attr_base<String>
	{
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public class TextAttribute : attr_string
	{
	};

	[DebuggerDisplay("{ToString(),nq}")]
	[ContentProperty("Categories")]
	public class CategoricalAttribute : attr_string
	{
		public CategoricalAttribute()
		{
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IList<String> cats;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IList<String> Categories
		{
			get
			{
				if (cats == null)
					this.cats = new List<String>();
				return cats;
			}
			set
			{
				if (value != null && value.Count > 0)
				{
					if (cats == null || cats.Count == 0)
						cats = value;
					else
						cats.AddRange(value);

					Debug.Assert(cats.IsDistinct());
				}
			}
		}
	};


	[DebuggerDisplay("{ToString(),nq}")]
	public class MultiCategoryAttribute : CategoricalAttribute
	{
		public MultiCategoryAttribute()
		{
			this.IsDistinct = true;
			this.IsOrderSignificant = false;
		}

		public bool IsDistinct { get; set; }

		public bool IsOrderSignificant { get; set; }
	};
}
