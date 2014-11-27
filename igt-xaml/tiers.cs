using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;

using alib;
using alib.Debugging;
using alib.Enumerable;
using alib.Wpf;

namespace xie
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	[UsableDuringInitialization(true)]
	public abstract class tier_base : text_dp_base, ITier
	{
		static tier_base()
		{
			dps.TiersHostProperty.AddOwner(typeof(tier_base));
			dps.TierTypeProperty.AddOwner(typeof(tier_base));
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITiers TiersHost
		{
			get { return (ITiers)this.GetValue(dps.TiersHostProperty); }
			set { this.SetValue(dps.TiersHostProperty, value); }
		}
		public sealed override IItems Host
		{
			get { return TiersHost; }
			set { TiersHost = (ITiers)value; }
		}

		public String TierType
		{
			get { return (String)GetValue(dps.TierTypeProperty); }
			set { SetValue(dps.TierTypeProperty, value); }
		}

		protected tier_base(Color tier_color)
		{
			this.br = SolidColorBrushCache.Get(tier_color);
		}

		SolidColorBrush br;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public SolidColorBrush TierBrush { get { return br; } }

		public static void MoveTier(tier_base told, tier_base tnew)
		{
			MoveTier(told.TiersHost, told.OuterIndex, tnew.TiersHost, tnew.OuterIndex);
			//if (told != tnew)
			//{
			//	ITiers h0, h1;
			//	if ((h0 = told.TiersHost) != (h1 = tnew.TiersHost))
			//	{
			//		h0.RemoveAt(told.OuterIndex);
			//		(told.TiersHost = h1).Insert(tnew.OuterIndex, told);

			//		IHostedItem hi;
			//		if (h0.Count == 0 && (hi = h0 as IHostedItem) != null)
			//		{
			//			Debug.Print("drag removing {0} from {1}", hi.GetType().Name, hi.Host.GetType().Name);
			//			hi.Host.GetList().Remove(hi);
			//		}
			//	}
			//	else
			//	{
			//		h0.Tiers.Move(told.OuterIndex, tnew.OuterIndex);
			//	}
			//}
		}
		public static void MoveTier(ITiers h0, int ix0, ITiers h1, int ix1)
		{
			if (h0 == null || h1 == null)
				throw new NullReferenceException();
			if (h0 == h1)
			{
				if (ix0 != ix1)
					h0.Tiers.Move(ix0, ix1);
			}
			else
			{
				var told = h0[ix0];
				h0.RemoveAt(ix0);
				(told.TiersHost = h1).Insert(ix1, told);

				IHostedItem hi;
				if (h0.Count == 0 && (hi = h0 as IHostedItem) != null)
				{
					Debug.Print("drag removing {0} from {1}", hi.GetType().Name, hi.Host.GetType().Name);
					hi.Host.GetList().Remove(hi);
				}
			}
		}

		/// <summary> Excluding self/'this' </summary>
		public ISet<ITier> AncestorTiers
		{
			get
			{
				var host = TiersHost;
				if (host == null)
					return new HashSet<ITier>();

				var rgt = new HashSet<ITier>(host.Tiers);
				ITier pt;
				while ((pt = host as ITier) != null)
				{
					host = pt.TiersHost;
					rgt.UnionWith(host.Tiers);
				}
				rgt.Remove(this);
				return rgt;
			}
		}

		public virtual IEnumerable<cmd_base> GetCommands()
		{
			var thh = TiersHost as ITier;
			if (thh != null)
				yield return new cmd_promote_tier(this, thh);

			yield return new cmd_add_tier_to_new_group(this);

			foreach (var grp in AncestorTiers.OfType<ITiers>())
				yield return new cmd_nest_tier(this, grp);

			yield return new cmd_hide_tier(this);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class TextTier : tier_base//, ITextTier
	{
		static TextTier()
		{
			dps.LineNumbersProperty.AddOwner(typeof(TextTier), new PropertyMetadata(default(int[])));
		}

		protected TextTier(Color tier_color)
			: base(tier_color)
		{
			SetValue(dps.IsReadOnlyProperty, true);
		}
		public TextTier()
			: this("#E9C0C0".ToColor())
		{
		}

		public int[] LineNumbers
		{
			get { return (int[])GetValue(dps.LineNumbersProperty); }
			set { SetValue(dps.LineNumbersProperty, value); }
		}

		public String s_LineNumbers
		{
			get { return LineNumbers == null ? "--" : LineNumbers.StringJoin(" "); }
		}


		public SegTier Segment()
		{
			var st = new SegTier { TierType = "Seg" };
			var s = this.Text;
			if (s.Length > 0)
			{
				var f_ws = Char.IsWhiteSpace(s[0]);
				var x = s.PartitionBy(ch => f_ws != (f_ws ^= Char.IsWhiteSpace(ch))).ToArray();

				int i_from = 0;
				for (int i = 0; i < x.Length; i++)
				{
					int i_to = i_from + x[i].Count;
					if (!x[i].Key)
					{
						var p = new SegPart
						{
							FromChar = i_from,
							ToChar = i_to,
							SourceTier = this,
						};
						st.Add(p);
					}
					i_from = i_to;
				}
			}
			TiersHost.Add(st);
			return st;
		}

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;

			yield return new cmd_tokenize_text_tier(this);

			foreach (var tt_other in AncestorTiers.OfType<TextTier>())
				yield return new cmd_join_text_tiers(this, tt_other);
		}

		public override string ToString()
		{
			return base.ToString() + " line:" + s_LineNumbers;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class CompoundTextTier : TextTier, ITiers<ITier>
	{
		readonly static DependencyPropertyKey LinesPropertyKey;
		public static DependencyProperty LinesProperty { get { return LinesPropertyKey.DependencyProperty; } }

		static CompoundTextTier()
		{
			LinesPropertyKey = DependencyProperty.RegisterReadOnly("Lines", typeof(TextTierSet), typeof(CompoundTextTier),
				new PropertyMetadata(default(TextTierSet)));

			TextProperty.AddOwner(typeof(CompoundTextTier), new PropertyMetadata(default(String),
				null,
				(d, o) => ((CompoundTextTier)d).coerce_text((String)o)));
		}

		String coerce_text(String s)
		{
			return Lines.Select(x => x.Text).StringJoin(" "/*Igt.IgtCorpus.Delimiter*/);
		}

		public TextTierSet Lines { get { return (TextTierSet)GetValue(LinesProperty); } }
		Iset<ITier> Iitems<ITier>.Items { get { return this.Lines; } }
		IList IListSource.GetList() { return this.Lines; }
		bool IListSource.ContainsListCollection { get { return true; } }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new String Text
		{
			get { return base.Text; }
			set { }
		}

		public ITier this[int index]
		{
			get { return Lines[index]; }
			set { Lines[index] = value; }
		}

		public int Count { get { return Lines.Count; } }

		public IEnumerator<ITier> GetEnumerator() { return Lines.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Lines.GetEnumerator(); }

		public CompoundTextTier()
			: base("#D6E8B0".ToColor())
		{
			var _lines = new TierSet(this);
			_lines.CollectionChanged += (o, e) => CoerceValue(TextProperty);
			SetValue(LinesPropertyKey, _lines);

			//	if (lines.Any(x => x.Igt != this.Igt || !this.Igt.Contains(x)))
			//		throw new Exception();
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class TextGroupTier : tier_base, ITiers
	{
		readonly static DependencyPropertyKey LinesPropertyKey;
		public static DependencyProperty LinesProperty { get { return LinesPropertyKey.DependencyProperty; } }

		static TextGroupTier()
		{
			LinesPropertyKey = DependencyProperty.RegisterReadOnly("Lines", typeof(TextTierSet), typeof(TextGroupTier),
				new PropertyMetadata(default(TextTierSet)));

			//TextProperty.AddOwner(typeof(TextGroupTier), new PropertyMetadata(default(String),
			//	null,
			//	(d, o) => ((TextGroupTier)d).coerce_text((String)o)));
		}

		//String coerce_text(String s)
		//{
		//	return Lines.Select(x => x.Text).StringJoin(" "/*Igt.IgtCorpus.Delimiter*/);
		//}

		public TextTierSet Lines { get { return (TextTierSet)GetValue(LinesProperty); } }
		public TierSet Tiers { get { return this.Lines; } }
		Iset<ITier> Iitems<ITier>.Items { get { return this.Lines; } }
		IList IListSource.GetList() { return this.Lines; }
		bool IListSource.ContainsListCollection { get { return true; } }

		//[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//public new String Text
		//{
		//	get { return base.Text; }
		//	set { }
		//}

		public ITier this[int index]
		{
			get { return Lines[index]; }
			set { Lines[index] = value; }
		}

		public int Count { get { return Lines.Count; } }

		public IEnumerator<ITier> GetEnumerator() { return Lines.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Lines.GetEnumerator(); }

		public TextGroupTier()
			: base("#D6E8F0".ToColor())
		{
			var _lines = new TextTierSet(this);
			//_lines.CollectionChanged += (o, e) => CoerceValue(TextProperty);
			SetValue(LinesPropertyKey, _lines);

			//	if (lines.Any(x => x.Igt != this.Igt || !this.Igt.Contains(x)))
			//		throw new Exception();
		}
		//TierSet _lines;

		//public TierSet Tiers
		//{
		//	get { return _lines; }
		//}

		//Iset<ITier> Iitems<ITier>.Items
		//{
		//	get { return _lines; }
		//}

		//ITier Iitems<ITier>.this[int index]
		//{
		//	get
		//	{
		//		return _lines[index];
		//	}
		//	set
		//	{
		//		_lines[index] = value;
		//	}
		//}

		//ITier IReadOnlyList<ITier>.this[int index]
		//{
		//	get { return _lines[index]; }
		//}

		//IEnumerator<ITier> IEnumerable<ITier>.GetEnumerator()
		//{
		//	return _lines.GetEnumerator();
		//}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class TierGroupTier : tier_base, ITiersTier
	{
		static TierGroupTier()
		{
			dps.TiersProperty.AddOwner(typeof(TierGroupTier));
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

		public TierGroupTier()
			: base("#E0F7F7".ToColor())
		{
			SetValue(dps.TiersPropertyKey, new TierSet(this));
		}

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class parts_tier_base : tier_base, IPartsTier
	{
		readonly static DependencyPropertyKey PartsPropertyKey;
		public static DependencyProperty PartsProperty { get { return PartsPropertyKey.DependencyProperty; } }

		static parts_tier_base()
		{
			//Selector.SelectedIndexProperty.AddOwner(typeof(parts_tier_base), new PropertyMetadata());
			//Selector.SelectedItemProperty.AddOwner(typeof(parts_tier_base), new PropertyMetadata());

			TextProperty.AddOwner(typeof(parts_tier_base), new PropertyMetadata(default(String),
				(o, e) => { },
				(d, o) => ((parts_tier_base)d).coerce_text((String)o)));

			PartsPropertyKey = DependencyProperty.RegisterReadOnly("Parts", typeof(Iset<IPart>), typeof(parts_tier_base),
				new PropertyMetadata(default(Iset<IPart>),
					(o, e) =>
					{
						var _this = (parts_tier_base)o;
						_this.CoerceValue(TextProperty);
					}));
		}

		String coerce_text(String s)
		{
			return Parts.Select(x => x.Text).StringJoin(" "/*Igt.IgtCorpus.Delimiter*/);
		}

		//public int SelectedIndex
		//{
		//	get { return (int)GetValue(Selector.SelectedIndexProperty); }
		//	set { SetValue(Selector.SelectedIndexProperty, value); }
		//}

		//public IPart SelectedItem
		//{
		//	get { return (IPart)GetValue(Selector.SelectedItemProperty); }
		//	set { SetValue(Selector.SelectedItemProperty, value); }
		//}

		public Iset<IPart> Parts { get { return (Iset<IPart>)GetValue(PartsProperty); } }
		Iset<IPart> Iitems<IPart>.Items { get { return Parts; } }

		IList IListSource.GetList() { return Parts; }
		bool IListSource.ContainsListCollection { get { return false; } }

		public IPart this[int index]
		{
			get { return Parts[index]; }
			set { Parts[index] = value; }
		}

		public int Count { get { return Parts.Count; } }

		public IEnumerator<IPart> GetEnumerator() { return Parts.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Parts.GetEnumerator(); }

		public parts_tier_base(Color tier_color)
			: base(tier_color)
		{
			SetValue(PartsPropertyKey, new OwnerPartsSet(this));
		}
		public parts_tier_base(Color tier_color, Iset<IPart> src)
			: base(tier_color)
		{
			SetValue(PartsPropertyKey, new _parts_set_proxy(this, src));
		}

		sealed class _parts_set_proxy : PartsSet
		{
			public _parts_set_proxy(parts_tier_base ptb, Iset<IPart> src)
				: base(ptb, src)
			{
				this.ptb = ptb;
			}
			readonly parts_tier_base ptb;
			protected override IPart f_newU(IPart t) { return ptb.f_newU(t); }
			protected override IPart f_newT(IPart u) { return ptb.f_newT(u); }
		};

		protected abstract IPart f_newU(IPart t)
			//{
			//	throw new NotImplementedException();
			//}
			;
		protected abstract IPart f_newT(IPart u)
			//{
			//	throw new NotImplementedException();
			//}
			;

		//public parts_tier_base(Color tier_color, Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
		//	: base(tier_color)
		//{
		//	SetValue(PartsPropertyKey, new PartsSet(this, src, f_newU, f_newT));
		//}

		public void Promote(IPart p)
		{
			int ix = Parts.IndexOf(p);
			var _old = Parts[ix];
			var _new = new CopyPart
			{
				SourcePart = _old,
				//Target = new TextPart
				//{
				//	//PartsHost = _old.PartsHost,
				//	Text = _old.Text
				//},
			};
			Parts[ix] = _new;
		}

		public void Merge(IPart p)
		{
			MergePart mp;

			int ix = Parts.IndexOf(p);
			if (ix + 1 >= Parts.Count)
				return;
			var pnew = Parts[ix + 1];

			if ((mp = p as MergePart) == null)
			{
				mp = new MergePart { PartsHost = this };

				p.Host = mp;
				mp.Parts.Add(p);
				Parts[ix] = mp;
			}
			pnew.Host = mp;
			mp.Parts.Add(pnew);

			Parts.RemoveAt(ix + 1);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		IPart[] _dbg { get { return Parts.ToArray(); } }

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;

			foreach (var pt_other in AncestorTiers.OfType<IPartsTier>())
				yield return new cmd_align_tiers(this, pt_other);

			yield return new cmd_new_pos_tier(this);

			yield return new cmd_new_dep_tier(this);
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class SegTier : parts_tier_base
	{
		public SegTier()
			: base("#D8F8D8".ToColor())
		{
		}
		//public SegTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
		//	: base("#D8F8D8".ToColor(), src, f_newU, f_newT)
		//{
		//}

		protected override IPart f_newU(IPart t)
		{
			throw new NotImplementedException();
		}
		protected override IPart f_newT(IPart u)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class PosTagTier : parts_tier_base
	{
		public PosTagTier()
			: base("#F8F8D8".ToColor())
		{
		}
		//public PosTagTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
		//	: base("#F8F8D8".ToColor(), src, f_newU, f_newT)
		//{
		//}
		public PosTagTier(Iset<IPart> src)
			: base("#F8F8D8".ToColor(), src)
		{
		}

		//[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//public override String Text
		//{
		//	get { return base.Text; }
		//	set { base.Text = value; }
		//}

		protected override IPart f_newU(IPart t)
		{
			return new TagPart { SourcePart = t };
		}
		protected override IPart f_newT(IPart u)
		{
			throw new Exception("slave tier should not add new parts");
			//return null;
		}

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class DependenciesTier : parts_tier_base
	{
		public static readonly DependencyProperty SelectingHeadProperty;

		static DependenciesTier()
		{
			SelectingHeadProperty = DependencyProperty.Register("SelectingHead", typeof(DepPart), typeof(DependenciesTier),
				new PropertyMetadata(default(DepPart), (o, e) => ((DependenciesTier)o)._selecting_change()));
		}

		public DepPart SelectingHead
		{
			get { return (DepPart)GetValue(SelectingHeadProperty); }
			set { SetValue(SelectingHeadProperty, value); }
		}

		void _selecting_change()
		{
			var _cur = SelectingHead;
			foreach (var dpo in Parts.OfType<DepPart>())
			{
				dpo.IsSelected = dpo == _cur;
			}
		}

		public DependenciesTier()
			: base("#FAFA9E".ToColor())
		{
		}
		public DependenciesTier(Iset<IPart> src)
			: base("#FAFA9E".ToColor(), src)
		{
		}
		//public DependenciesTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
		//	: base("#FAFA9E".ToColor(), src, f_newU, f_newT)
		//{
		//}

		protected override IPart f_newU(IPart t)
		{
			return new DepPart { SourcePart = t };
		}
		protected override IPart f_newT(IPart u)
		{
			throw new Exception("slave tier (Dep) should not add new parts");
		}

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class AlignmentTier : parts_tier_base
	{
		public static readonly DependencyProperty AlignWithProperty =
			DependencyProperty.Register("AlignWith", typeof(IParts), typeof(AlignmentTier),
				new PropertyMetadata(default(IParts), (o, e) => ((AlignmentTier)o).alignwith_change()));

		public AlignmentTier()
			: base("#FCD9CC".ToColor())
		{
		}
		public AlignmentTier(Iset<IPart> src)
			: base("#FCD9CC".ToColor(), src)
		{
		}
		//public AlignmentTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
		//	: base("#FCD9CC".ToColor(), src, f_newU, f_newT)
		//{
		//}

		public IParts AlignWith
		{
			get { return (IParts)GetValue(AlignWithProperty); }
			set { SetValue(AlignWithProperty, value); }
		}

		void alignwith_change()
		{
			var rgp = this.AlignWith;
			if (rgp == null)
				return;
		}

		//public void AddParts(IParts src_tier, IParts tgt_tier)
		//{
		//	var rgt = tgt_tier;
		//	if (rgt.Count > 0)
		//	{
		//		int i = 0;
		//		foreach (var p in src_tier)
		//		{
		//			Parts.Add(new AlignPart
		//			{
		//				Source = p,
		//				Target = rgt[i++],
		//			});
		//			if (i == rgt.Count)
		//				i = 0;
		//		}
		//	}
		//}

		protected override IPart f_newU(IPart t)
		{
			return new AlignPart { SourcePart = t };
		}
		protected override IPart f_newT(IPart u)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<cmd_base> GetCommands()
		{
			foreach (var cmd in base.GetCommands())
				yield return cmd;
		}
	};
}
