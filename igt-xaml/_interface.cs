using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using System.Windows;
using alib.Debugging;
using alib.Enumerable;

namespace xie
{
	public interface Iset<T> : System.Collections.IList, IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
		where T : class, IHostedItem
	{
		void Move(int oldIndex, int newIndex);
		new void RemoveAt(int index);
		new T this[int index] { get; }
		void replace_item(int index, IHostedItem item);
		new int Count { get; }
		new void Clear();
	};

	/////////////////////////////////////////////////////////////////

	public interface IItem
	{
		Object GetValue(DependencyProperty dp);
		void SetValue(DependencyProperty dp, Object value);
		String Name { get; set; }
	};
	public interface IHostedItem : IItem
	{
		IItems Host { get; set; }
		int OuterIndex { get; }
	};
	public interface ITextItem : IItem
	{
		String Text { get; set; }
	};
	public interface IEditText : ITextItem
	{
	};
	public interface IHostedTextItem : IHostedItem, ITextItem
	{
	};
	public interface ITier : IHostedTextItem
	{
		ITiers TiersHost { get; set; }
		String TierType { get; set; }
	};
	public interface IPart : IHostedTextItem
	{
		IParts PartsHost { get; set; }
	};
	public interface IItems : IItem, IListSource
	{
		int Count { get; }
	};
	public interface Iitems<T> : IItems, IReadOnlyList<T>
		where T : class, IHostedItem
	{
		Iset<T> Items { get; }
		new T this[int index] { get; }
		new int Count { get; }
		//int SelectedIndex { get; set; }
		//T SelectedItem { get; set; }
	};
	public interface IParts : Iitems<IPart>
	{
		Iset<IPart> Parts { get; }
	};
	public interface ITiers<T> : Iitems<T>
		where T : class, ITier
	{
	};
	public interface ITiers : ITiers<ITier>
	{
		TierSet Tiers { get; }
	};
	public interface IPartsTier : IParts, ITier
	{
	};
	public interface ITiersTier : ITiers, ITier
	{
	};
	public interface IIgt : ITiers, IHostedItem
	{
	};



	public static class _isb_ext
	{
		public static void Add<T>(this Iitems<T> _this, T p)
			where T : class,IHostedItem
		{
			_this.Items.Add(p);
		}
		public static bool Remove<T>(this Iitems<T> _this, T p)
			where T : class,IHostedItem
		{
			int ix;
			if ((ix = _this.Items.IndexOf(p)) == -1)
				return false;
			_this.Items.RemoveAt(ix);
			return true;
		}
		public static void RemoveAt<T>(this Iitems<T> _this, int ix)
			where T : class,IHostedItem
		{
			_this.Items.RemoveAt(ix);
		}
		public static void Insert<T>(this Iitems<T> _this, int ix, T p)
			where T : class,IHostedItem
		{
			_this.Items.Insert(ix, p);
		}
		public static void Move<T>(this Iitems<T> _this, int ix_from, int ix_to)
			where T : class,IHostedItem
		{
			_this.Items.Move(ix_from, ix_to);
		}
		public static int IndexOf<T>(this Iitems<T> _this, T p)
			where T : class,IHostedItem
		{
			return _this.Items.IndexOf(p);
		}

		public static int OuterIndex(this IItems host)
		{
			var hi = host as IHostedItem;
			return hi != null ? hi.OuterIndex : -1;
		}


		public static IEnumerable<ITier> AllDescendants(this ITiers _this)
		{
			ITiers tt;

			foreach (var t in _this.Tiers)
			{
				if ((tt = t as ITiers) != null)
					foreach (var ttt in AllDescendants(tt))
						yield return ttt;

				yield return t;
			}
		}
	};
}