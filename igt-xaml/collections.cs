﻿using System;
using System.Threading;
using System.Threading.Tasks;
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
	public abstract class _take_ownership_set<T> : ObservableCollection<T>, Iset<T>
		where T : class, IHostedItem
	{
		protected _take_ownership_set(IItem owner)
		{
			this.owner = owner as Iitems<T>;

			if (this.owner == null)
				Nop.X();
		}

		readonly Iitems<T> owner;

		public void replace_item(int index, IHostedItem item)
		{
			base[index] = (T)item;
		}

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

			if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				foreach (IHostedItem hi in e.OldItems)
				{
					hi.Host = null;
				}
			}
#if false
			if (e.Action == NotifyCollectionChangedAction.Replace)
			{
			}
#endif
		}
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

	public class PartRefSet : ObservableCollection<IPart>
	{
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
		public OwnerCorpusSet(IgtCorpora owner)
			: base(owner)
		{
		}
	};

	public abstract class PartsSet : _promoter<IPart, IPart>
	{
		public PartsSet(Iitems<IPart> owner, Iset<IPart> src)
			: base(owner, src)
		{
		}
		//public PartsSet(Iitems<IPart> owner, Iset<IPart> src, Func<IPart, IPart> f_newU, Func<IPart, IPart> f_newT)
		//	: base(owner, src, f_newU, f_newT)
		//{
		//}
	};
}
