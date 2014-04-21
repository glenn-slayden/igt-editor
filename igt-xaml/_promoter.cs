using System;
using System.Collections;
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
	public class _promoter<T, U> : Iset<U>
		where T : class, IHostedItem
		where U : class, IHostedItem
	{
		public _promoter(Iitems<T> owner, Iset<T> src, Func<T, U> f_newU, Func<U, T> f_newT)
		{
			this.owner = owner;
			this.src = src;
			this.f_newT = f_newT;
			this.f_newU = f_newU;
			this.t_u = new Dictionary<T, U>(src.Count);
			this.u_t = new Dictionary<U, T>(src.Count);

			src.CollectionChanged += _src_collchange;
			((INotifyPropertyChanged)src).PropertyChanged += _src_propchange;
		}

		void _src_collchange(Object sender, NotifyCollectionChangedEventArgs e)
		{
			var _tmp = this.CollectionChanged;
			if (_tmp != null)
			{
#if true
				var a = e.Action;
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
						e = new NotifyCollectionChangedEventArgs(a, map_all(e.NewItems), e.NewStartingIndex);
						break;
					case NotifyCollectionChangedAction.Remove:
						e = new NotifyCollectionChangedEventArgs(a, map_all(e.OldItems), e.OldStartingIndex);
						break;
					case NotifyCollectionChangedAction.Move:
						e = new NotifyCollectionChangedEventArgs(a, map_all(e.OldItems), e.NewStartingIndex, e.OldStartingIndex);
						break;
					case NotifyCollectionChangedAction.Replace:
						if (e.OldStartingIndex != e.NewStartingIndex)
							throw new Exception();
						e = new NotifyCollectionChangedEventArgs(a, map_all(e.NewItems), map_all(e.OldItems), e.OldStartingIndex);
						break;
					default:
						throw new Exception();
				}
#else
				IList rgT;
				U[] rgU_old, rgU_new;
				if ((rgT = e.OldItems) != null)
				{
					rgU_old = new U[rgT.Count];
					for (int i = 0; i < rgT.Count; i++)
						rgU_old[i] = GetOrAddU((T)rgT[i]);
				}

				if ((rgT = e.NewItems) != null)
				{
					rgU_new = new U[rgT.Count];
					for (int i = 0; i < rgT.Count; i++)
						rgU_new[i] = GetOrAddU((T)rgT[i]);
				}
#endif
				_tmp(this, e);
			}
		}

		U[] map_all(IList rgT)
		{
			if (owner == null)
				throw new Exception();
			var rgU = new U[rgT.Count];
			for (int i = 0; i < rgT.Count; i++)
			{
				(rgU[i] = GetOrAddU((T)rgT[i])).Host = owner;
			}
			return rgU;
		}

		void _src_propchange(Object sender, PropertyChangedEventArgs e)
		{
			var _tmp = this.PropertyChanged;
			if (_tmp != null)
				_tmp(this, e);
		}

		readonly Iitems<T> owner;
		readonly Iset<T> src;
		readonly Func<U, T> f_newT;
		readonly Func<T, U> f_newU;
		readonly Dictionary<T, U> t_u;
		readonly Dictionary<U, T> u_t;

		U GetOrAddU(T t)
		{
			U u;
			if (!t_u.TryGetValue(t, out u))
			{
				u = f_newU(t);
				if (u.Host == null)
					u.Host = owner;
				t_u.Add(t, u);
				u_t.Add(u, t);
			}
			return u;
		}

		T GetOrAddT(U u)
		{
			T t;
			if (!u_t.TryGetValue(u, out t))
			{
				t = f_newT(u);
				t_u.Add(t, u);
				u_t.Add(u, t);
			}
			return t;
		}

		public int Count { get { return src.Count; } }

		public U this[int index]
		{
			get { return GetOrAddU(src[index]); }
			set { src[index] = GetOrAddT(value); }
		}
		Object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (U)value; }
		}

		public void Add(U u)
		{
			src.Add(GetOrAddT(u));
		}
		int IList.Add(Object value)
		{
			return ((IList)src).Add(GetOrAddT((U)value));
		}

		public void Insert(int index, U u)
		{
			src.Insert(index, GetOrAddT(u));
		}
		void IList.Insert(int index, Object value)
		{
			Insert(index, (U)value);
		}

		public bool Remove(U u)
		{
			T t;
			return u_t.TryGetValue(u, out t) ? src.Remove(t) : false;
		}
		void IList.Remove(Object value)
		{
			Remove((U)value);
		}

		public void RemoveAt(int index) { src.RemoveAt(index); }

		public void Move(int oldIndex, int newIndex)
		{
			src.Move(oldIndex, newIndex);
		}

		public void Clear() { src.Clear(); }

		public int IndexOf(U u)
		{
			T t;
			return u_t.TryGetValue(u, out t) ? src.IndexOf(t) : -1;
		}
		int IList.IndexOf(Object value)
		{
			return IndexOf((U)value);
		}

		public bool Contains(U u)
		{
			T t;
			return u_t.TryGetValue(u, out t) && src.Contains(t);
		}
		bool IList.Contains(Object value)
		{
			return Contains((U)value);
		}

		public IEnumerator<U> GetEnumerator()
		{
			var e = src.GetEnumerator();
			while (e.MoveNext())
				yield return GetOrAddU(e.Current);
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public void CopyTo(U[] array, int index)
		{
			var e = src.GetEnumerator();
			while (e.MoveNext())
				array[index++] = GetOrAddU(e.Current);
		}
		void ICollection.CopyTo(Array array, int index)
		{
			var e = src.GetEnumerator();
			while (e.MoveNext())
				array.SetValue(GetOrAddU(e.Current), index++);
		}

		public bool IsReadOnly { get { return ((IList)src).IsReadOnly; } }

		bool ICollection.IsSynchronized { get { return ((ICollection)src).IsSynchronized; } }

		Object ICollection.SyncRoot { get { return ((ICollection)src).SyncRoot; } }

		bool IList.IsFixedSize { get { return ((IList)src).IsFixedSize; } }

		/////////////////

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public event PropertyChangedEventHandler PropertyChanged;
	};
}