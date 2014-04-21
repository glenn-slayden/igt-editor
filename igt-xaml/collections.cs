using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	public abstract class _set_of<T> : ObservableCollection<T>, Iset<T>
		where T : class, IHostedItem
	{
	};


	public abstract class _take_ownership_set<T> : _set_of<T>
		where T : class, IHostedItem
	{
		protected _take_ownership_set(Iitems<T> owner)
		{
			this.owner = owner ?? (Iitems<T>)this;
		}

		readonly Iitems<T> owner;

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				if (owner == null)
					throw new Exception();

				foreach (IHostedItem hi in e.NewItems)
				{
					//if (hi.Host == null)
					hi.Host = owner;
				}
			}

			base.OnCollectionChanged(e);
		}
	};

	public class TextTierSet : _set_of<ITextTier>
	{
	};

	public abstract class owner_tier_set<T> : _take_ownership_set<T>
		where T : class, ITier
	{
		protected owner_tier_set(Iitems<T> owner)
			: base(owner)
		{
		}
	};

	public class TierSet : owner_tier_set<ITier>
	{
		public TierSet(Iitems<ITier> owner)
			: base(owner)
		{
		}
	};

	public class PartsTierSet : owner_tier_set<IPartsTier>
	{
		public PartsTierSet(Iitems<IPartsTier> owner)
			: base(owner)
		{
		}
	};

	public class PartRefSet : _set_of<IPart>
	{
		public PartRefSet()
			: base()
		{
		}
	};

	public class OwnerPartsSet : _take_ownership_set<IPart>
	{
		public OwnerPartsSet(Iitems<IPart> owner)
			: base(owner)
		{
		}
	};

	public class OwnerIgtsSet : _take_ownership_set<Igt>
	{
		public OwnerIgtsSet(Iitems<Igt> owner)
			: base(owner)
		{
		}
	};

	public class OwnerCorpusSet : _take_ownership_set<IgtCorpus>
	{
		public OwnerCorpusSet(Iitems<IgtCorpus> owner)
			: base(owner)
		{
		}
	};

	public class PartsSet : _promoter<IPart, IPart>
	{
		public PartsSet(Iitems<IPart> owner, Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
			: base(owner, src, f_newU, f_newT)
		{
		}
	};
}