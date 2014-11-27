using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

using System.Windows.Input;

using alib;
using alib.Debugging;
using alib.Enumerable;

namespace xie
{
#if false
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	[UsableDuringInitialization(true)]
	public abstract class cmd_base : FrameworkContentElement
	{
		public abstract void Execute();

		public abstract bool CanExecute { get; }

		public abstract String CommandText { get; }

		public override String ToString()
		{
			return CommandText;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_tokenize_tier : cmd_base
	{
		public static readonly DependencyProperty TextTierProperty =
			DependencyProperty.Register("TextTier", typeof(TextTier), typeof(cmd_tokenize_tier), new PropertyMetadata(default(TextTier)));

		public TextTier TextTier
		{
			get { return (TextTier)GetValue(TextTierProperty); }
			set { SetValue(TextTierProperty, value); }
		}

		public override void Execute()
		{
			TextTier.Segment();
		}

		public override bool CanExecute
		{
			get { return true; }
		}

		public override String CommandText { get { return "Tokenize Tier..."; } }
	};
#else
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	//[UsableDuringInitialization(true)]
	public abstract class cmd_base //: FrameworkContentElement
	{
		public void Handler(Object _, RoutedEventArgs __) { Execute(); }

		public abstract void Execute();

		public abstract String CommandText { get; }

		public override String ToString()
		{
			return CommandText;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_delete_tier : cmd_base
	{
		public cmd_delete_tier(ITier tier)
		{
			this.tier = tier;
		}

		readonly ITier tier;

		public override void Execute()
		{
			var th = tier.TiersHost;
			var tiers = th.Tiers;

			tiers.Remove(tier);

			IHostedItem hi;
			if (tiers.Count == 0 && (hi = th as IHostedItem) != null)
			{
				Debug.Print("Removing {0} from {1}", hi.GetType().Name, hi.Host.GetType().Name);
				hi.Host.GetList().Remove(hi);
			}
		}

		public override String CommandText { get { return "Delete tier"; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_hide_tier : cmd_base
	{
		public cmd_hide_tier(tier_base tier)
		{
			this.tier = tier;
		}

		readonly tier_base tier;

		public override void Execute()
		{
			tier.IsVisible = false;
		}

		public override String CommandText { get { return "Hide"; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_promote_tier : cmd_base
	{
		public cmd_promote_tier(ITier tier)
		{
			this.tier = tier;
		}

		readonly ITier tier;

		public override void Execute()
		{
			ITiers<ITier> thx;
			var thh = (ITier)(thx = tier.TiersHost);

			tier_base.MoveTier(thx, tier.OuterIndex, thh.TiersHost, thh.OuterIndex + 1);
		}

		public override String CommandText { get { return "Promote"; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_add_tier_to_new_group : cmd_base
	{
		public cmd_add_tier_to_new_group(ITier tier)
		{
			this.tier = tier;
		}

		readonly ITier tier;

		public override void Execute()
		{
			var thh = tier.TiersHost;
			if (thh == null)
				throw new Exception();

			int ix = tier.OuterIndex;

			var tg = new TierGroupTier
			{
				TierType = tier.TierType,
			};
			thh.Insert(ix, tg);

			tier_base.MoveTier(thh, ix + 1, tg, 0);
		}

		public override String CommandText { get { return "Create new group"; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_nest_tier : cmd_base
	{
		public cmd_nest_tier(ITier tier, ITiers<ITier> grp)
		{
			this.tier = tier;
			this.grp = grp;
		}

		readonly ITier tier;
		readonly ITiers<ITier> grp;

		public override void Execute()
		{
			tier_base.MoveTier(tier.TiersHost, tier.OuterIndex, grp, grp.Count);
		}

		public override String CommandText { get { return String.Format("Move to group {0}", grp.Name); } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_tokenize_text_tier : cmd_base
	{
		public cmd_tokenize_text_tier(TextTier tt)
		{
			this.tt = tt;
		}

		readonly TextTier tt;

		public override void Execute()
		{
			tt.Segment();
		}

		public override String CommandText { get { return "Tokenize"; } }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_join_text_tiers : cmd_base
	{
		public cmd_join_text_tiers(TextTier t1, TextTier t2)
		{
			this.t1 = t1;
			this.t2 = t2;
		}

		readonly TextTier t1, t2;

		public override void Execute()
		{
			var stt = (t1.TierType ?? t1.GetHashCode().ToString("X")) + ";" + (t2.TierType ?? t2.GetHashCode().ToString("X"));
			var mst = new CompoundTextTier
			{
				TierType = stt
			};
			t1.TiersHost.Add(mst);

			mst.Tiers.Add(t1);
			mst.Tiers.Add(t2);
		}

		public override String CommandText
		{
			get
			{
				return String.Format("Join with tier {0} {1}...", t2.TierType, t2.OuterIndex);
			}
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_align_tiers : cmd_base
	{
		public cmd_align_tiers(IPartsTier pt_from, IPartsTier pt_to)
		{
			this.pt_from = pt_from;
			this.pt_to = pt_to;
		}

		readonly IPartsTier pt_from, pt_to;

		public override void Execute()
		{
			var at = new AlignmentTier(pt_from.Parts)
			{
				AlignWith = pt_to,
				TierType = "Align",
			};
			pt_from.TiersHost.Add(at);
		}

		public override String CommandText
		{
			get
			{
				return String.Format("Align to {0} {1}", pt_to.TierType, pt_to.OuterIndex);
			}
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_new_pos_tier : cmd_base
	{
		public cmd_new_pos_tier(IPartsTier src_tier)
		{
			this.src_tier = src_tier;
		}

		readonly IPartsTier src_tier;

		public override void Execute()
		{
			var pos_tier = new PosTagTier(src_tier.Parts) { TierType = "POS", };

			src_tier.TiersHost.Add(pos_tier);
		}

		public override String CommandText { get { return "Create POS tagging tier"; } }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	public sealed class cmd_new_dep_tier : cmd_base
	{
		public cmd_new_dep_tier(IPartsTier src_tier)
		{
			this.src_tier = src_tier;
		}

		readonly IPartsTier src_tier;

		public override void Execute()
		{
			var pos_tier = new DependenciesTier(src_tier.Parts) { TierType = "Dep", };

			src_tier.TiersHost.Add(pos_tier);
		}

		public override String CommandText { get { return "Create dependencies tier"; } }
	};

#endif
}
