using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using alib.Debugging;
using alib.Enumerable;

namespace xigt2
{
	public abstract class _set_of<T> : ObservableCollection<T>, Iset<T>
		where T : class, IHostedItem
	{
		protected _set_of(Iitems<T> owner)
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


	public abstract class tier_set_base<T> : _set_of<T>
		where T : class, ITier
	{
		protected tier_set_base(Iitems<T> owner)
			: base(owner)
		{
		}
	};

	public class TierSet : tier_set_base<ITier>
	{
		public TierSet(Iitems<ITier> owner)
			: base(owner)
		{
		}
	};

	public class TextTierSet : tier_set_base<TextTier>
	{
		public TextTierSet(Iitems<TextTier> owner)
			: base(owner)
		{
		}
	};

	public class PartsTierSet : tier_set_base<IPartsTier>
	{
		public PartsTierSet(Iitems<IPartsTier> owner)
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

	public class _PartsSet : _set_of<IPart>
	{
		public _PartsSet(Iitems<IPart> owner)
			: base(owner)
		{
		}
	};

	public class IgtsSet : _set_of<Igt>
	{
		public IgtsSet(Iitems<Igt> owner)
			: base(owner)
		{
		}
	};

	public class CorpusSet : _set_of<IgtCorpus>
	{
		public CorpusSet(Iitems<IgtCorpus> owner)
			: base(owner)
		{
		}
	};
}