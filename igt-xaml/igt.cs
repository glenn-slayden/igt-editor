using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using System.Windows;
using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class Igt : name_dp_base, IIgt
	{
		static Igt()
		{
			dps.TiersProperty.AddOwner(typeof(Igt));
			dps.IgtCorpusProperty.AddOwner(typeof(Igt));
			dps.DocIdProperty.AddOwner(typeof(Igt));
			dps.DocInfoProperty.AddOwner(typeof(Igt));
			dps.LanguageProperty.AddOwner(typeof(Igt));
			dps.FromLineProperty.AddOwner(typeof(Igt));
			dps.ToLineProperty.AddOwner(typeof(Igt));
		}

		public Igt()
		{
			SetValue(dps.TiersPropertyKey, new TierSet(this));
		}

		public TierSet Tiers { get { return (TierSet)GetValue(dps.TiersProperty); } }

		Iset<ITier> Iitems<ITier>.Items { get { return this.Tiers; } }

		IList IListSource.GetList() { return this.Tiers; }

		bool IListSource.ContainsListCollection { get { return true; } }

		public ITier this[int index]
		{
			get { return Tiers[index]; }
			set { Tiers[index] = value; }
		}

		public int Count { get { return Tiers.Count; } }

		public IEnumerator<ITier> GetEnumerator() { return Tiers.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Tiers.GetEnumerator(); }

		Iitems<Igt> host;
		public override IItems Host
		{
			get { return host; }
			set { host = (Iitems<Igt>)value; }
		}

		//public bool ContainsTierSourceLineIndex(int ix_src_line)
		//{
		//	return _tiers.OfType<Tier>().Any(tier => tier.ix_src_line == ix_src_line);
		//}


		public String SourceLineRange
		{
			get { return String.Format("{0}-{1}", FromLine, ToLine); }
		}

		//public IgtCorpus IgtCorpus
		//{
		//	get { return (IgtCorpus)this.GetValue(dps.IgtCorpusProperty); }
		//	set { this.SetValue(dps.IgtCorpusProperty, value); }
		//}
		[DefaultValue(default(String))]
		public String DocId
		{
			get { return (String)this.GetValue(dps.DocIdProperty); }
			set { this.SetValue(dps.DocIdProperty, value); }
		}
		[DefaultValue(default(String))]
		public String DocInfo
		{
			get { return (String)this.GetValue(dps.DocInfoProperty); }
			set { this.SetValue(dps.DocInfoProperty, value); }
		}
		[DefaultValue(default(String))]
		public String Language
		{
			get { return (String)this.GetValue(dps.LanguageProperty); }
			set { this.SetValue(dps.LanguageProperty, value); }
		}
		public int FromLine
		{
			get { return (int)this.GetValue(dps.FromLineProperty); }
			set { this.SetValue(dps.FromLineProperty, value); }
		}
		public int ToLine
		{
			get { return (int)this.GetValue(dps.ToLineProperty); }
			set { this.SetValue(dps.ToLineProperty, value); }
		}
	};
}
