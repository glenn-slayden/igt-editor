using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

using alib.Debugging;
using alib.Enumerable;
using alib.Wpf;

namespace xigt2
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
			dps.IsVisibleProperty.AddOwner(typeof(tier_base));
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

		[DefaultValue(true)]
		public Boolean IsVisible
		{
			get { return (Boolean)GetValue(dps.IsVisibleProperty); }
			set { SetValue(dps.IsVisibleProperty, value); }
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
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class TextTier : tier_base, ITextTier
	{
		protected TextTier(Color tier_color)
			: base(tier_color)
		{
			SetValue(dps.IsReadOnlyProperty, true);
		}
		public TextTier()
			: this("#E9C0C0".ToColor())
		{
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
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class CompoundTextTier : TextTier, ITextTiers
	{
		readonly static DependencyPropertyKey LinesPropertyKey;
		public static DependencyProperty LinesProperty { get { return LinesPropertyKey.DependencyProperty; } }

		static CompoundTextTier()
		{
			LinesPropertyKey = DependencyProperty.RegisterReadOnly("Lines", typeof(TextTierSet), typeof(CompoundTextTier),
				new PropertyMetadata(default(TextTierSet)));

			dps.TextProperty.AddOwner(typeof(CompoundTextTier), new PropertyMetadata(default(String),
				null,
				(d, o) => ((CompoundTextTier)d).coerce_text((String)o)));
		}

		String coerce_text(String s)
		{
			return Lines.Select(x => x.Text).StringJoin(" "/*Igt.IgtCorpus.Delimiter*/);
		}

		public TextTierSet Lines { get { return (TextTierSet)GetValue(LinesProperty); } }
		Iset<ITextTier> Iitems<ITextTier>.Items { get { return this.Lines; } }
		IList IListSource.GetList() { return this.Lines; }
		bool IListSource.ContainsListCollection { get { return true; } }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new String Text
		{
			get { return base.Text; }
			set { }
		}

		public ITextTier this[int index]
		{
			get { return Lines[index]; }
			set { Lines[index] = value; }
		}

		public int Count { get { return Lines.Count; } }

		public IEnumerator<ITextTier> GetEnumerator() { return Lines.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return Lines.GetEnumerator(); }

		public CompoundTextTier()
			: base("#D6E8B0".ToColor())
		{
			var _lines = new TextTierSet();
			_lines.CollectionChanged += (o, e) => CoerceValue(dps.TextProperty);
			SetValue(LinesPropertyKey, _lines);

			//	if (lines.Any(x => x.Igt != this.Igt || !this.Igt.Contains(x)))
			//		throw new Exception();
		}
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

			dps.TextProperty.AddOwner(typeof(parts_tier_base), new PropertyMetadata(default(String),
				(o, e) => { },
				(d, o) => ((parts_tier_base)d).coerce_text((String)o)));

			PartsPropertyKey = DependencyProperty.RegisterReadOnly("Parts", typeof(Iset<IPart>), typeof(parts_tier_base),
				new PropertyMetadata(default(Iset<IPart>),
					(o, e) =>
					{
						var _this = (parts_tier_base)o;
						_this.CoerceValue(dps.TextProperty);
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
		public parts_tier_base(Color tier_color, Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
			: base(tier_color)
		{
			SetValue(PartsPropertyKey, new PartsSet(this, src, f_newU, f_newT));
		}

		public void Promote(IPart p)
		{
			int ix = Parts.IndexOf(p);
			var _old = Parts[ix];
			var _new = new CopyPart
			{
				Source = _old,
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
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class SegTier : parts_tier_base
	{
		public SegTier()
			: base("#D8F8D8".ToColor())
		{
		}
		public SegTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
			: base("#D8F8D8".ToColor(), src, f_newU, f_newT)
		{
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class AlignmentTier : parts_tier_base
	{
		public static readonly DependencyProperty AlignWithProperty =
			DependencyProperty.Register("AlignWith", typeof(IParts), typeof(AlignmentTier),
				new PropertyMetadata(default(IParts), (o, e) => ((AlignmentTier)o).alignwith_change()));


		public AlignmentTier()
			: base("#FCD9CC".ToColor())
		{
		}
		public AlignmentTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
			: base("#FCD9CC".ToColor(), src, f_newU, f_newT)
		{
		}

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
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class PosTagTier : parts_tier_base
	{
		public PosTagTier()
			: base("#F8F8D8".ToColor())
		{
		}
		public PosTagTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
			: base("#F8F8D8".ToColor(), src, f_newU, f_newT)
		{
		}

		//[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//public override String Text
		//{
		//	get { return base.Text; }
		//	set { base.Text = value; }
		//}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[DebuggerDisplay("{ToString(),nq}")]
	public class DependenciesTier : parts_tier_base
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
		public DependenciesTier(Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
			: base("#FAFA9E".ToColor(), src, f_newU, f_newT)
		{
		}


	};
}
