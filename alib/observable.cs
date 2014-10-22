
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using alib.Enumerable;
using alib.Collections;
using alib.Reflection;
using alib.Array;

namespace alib.Observable
{
	public static class _obs_ext
	{
		public static T[] ToArrayWait<T>(this IObservable<T> obs)
		{
			return new _array_materializer<T>(obs).Result;
		}

		sealed class _array_materializer<T> : IObserver<T>
		{
			public _array_materializer(IObservable<T> obs)
			{
				this.mre = new ManualResetEvent(false);
				obs.Subscribe(this);
			}
			ManualResetEvent mre;
			RefList<T> list;
			Exception ex;
			T[] arr;

			void IObserver<T>.OnNext(T value)
			{
#if DEBUG
				ManualResetEvent _tmp;
				if ((_tmp = mre) == null || _tmp.WaitOne(0))
					throw new Exception();
#endif
				(list ?? (list = new RefList<T>())).Add(value);
			}

			void IObserver<T>.OnCompleted()
			{
				var _tmp = Interlocked.Exchange(ref list, null);
				arr = _tmp == null ? Collection<T>.None : _tmp.GetTrimmed();
				set();
			}

			void IObserver<T>.OnError(Exception ex)
			{
				this.ex = ex;
				set();
			}

			void set() { Interlocked.Exchange(ref mre, null).Set(); }

			public T[] Result
			{
				get
				{
					ManualResetEvent _tmp;
					if ((_tmp = mre) != null)
						_tmp.WaitOne();

					if (ex != null)
						throw ex;

					return arr;
				}
			}
		};
	};

#if false
	/// <summary>
	/// Thread-safe state machine:
	/// 
	/// → ➀ open.idle.waiting
	///		→ ➁ open.idle.active.{empty,single,duple,multi}
	///			→ ➁
	///			→ ➂ open.busy
	///				→ ➁
	///			→ ➍ closed.complete
	///			→ ➎ closed.error
	///				
	/// </summary>
	public abstract class atoms<Q>
		where Q : class
	{
		protected interface I_items { IReadWriteList<Q> List { get; } }

		public abstract class atom { };

		///////////////////////////////////////////////////////////////////////
		/// 
		public abstract class closed : atom
		{
			public sealed class error : closed
			{
				public error(Exception ex) { this.ex = ex; }
				Exception ex;
				public Exception Exception { get { return ex; } }
			};

			public sealed class complete : closed, I_items
			{
				public complete(open.idle final) { this.final = final; }
				open.idle final;
				public IReadWriteList<Q> List { get { return final; } }
			};
		};
		///
		///////////////////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////////////////
		/// 
		public abstract class open : atom
		{
			///////////////////////////////////////////////////////////////////////
			/// 
			public sealed class busy : open, I_items
			{
				public busy(open.idle previous) { this._prv = previous; }
				open.idle _prv;
				public IReadWriteList<Q> List { get { return _prv; } }
			};
			/// 
			///////////////////////////////////////////////////////////////////////

			public abstract class idle : open, I_items, IReadWriteList<Q>
			{
				///////////////////////////////////////////////////////////////////////
				/// 
				public IReadWriteList<Q> List { get { return this; } }

				public virtual Q this[int ix] { get { throw not.valid; } set { throw not.valid; } }
				public virtual int Count { get { throw not.valid; } }
				public virtual void CopyTo(System.Array array, int index) { throw not.valid; }
				public virtual IEnumerator<Q> GetEnumerator() { throw not.valid; }

				IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				public bool IsSynchronized { get { return true; } }
				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				public Object SyncRoot { get { return this; } }
				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				public bool IsReadOnly { get { return false; } }
				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				public bool IsFixedSize { get { return true; } }

				public int IndexOf(Q item) { throw not.impl; }
				public void Insert(int index, Q item) { throw not.impl; }
				public void RemoveAt(int index) { throw not.impl; }
				public void Clear() { throw not.impl; }
				public bool Contains(Q item) { throw not.impl; }
				public void CopyTo(Q[] array, int arrayIndex) { throw not.impl; }
				public void AddRange(IEnumerable<Q> rg) { throw not.impl; }

				bool IList.Contains(Object item) { throw not.impl; }
				int IList.IndexOf(Object item) { throw not.impl; }
				void IList.Insert(int index, Object value) { throw not.impl; }

				Object IList.this[int index] { get { throw not.impl; } set { throw not.impl; } }

				int IList.Add(Object item) { Add((Q)item); return Count - 1; }
				void ICollection<Q>.Add(Q item) { Add(item); }
				public abstract active Add(Q item);

				void IList.Remove(Object item) { Remove((Q)item); }
				bool ICollection<Q>.Remove(Q item) { Remove(item); return true; }
				public abstract active Remove(Q item);

				public sealed class waiting : idle
				{
					public static readonly waiting Instance = new waiting();
					waiting() { }
					public override int Count { get { return 0; } }
					public override void CopyTo(System.Array array, int index) { }
					public override IEnumerator<Q> GetEnumerator() { return Collection<Q>.NoneEnumerator; }
					public override active Add(Q item) { return new active.single(item); }
					public override active Remove(Q item) { throw not.valid; }
				};
				///
				///////////////////////////////////////////////////////////////////////


				///////////////////////////////////////////////////////////////////////
				///  
				public abstract class active : idle
				{
					///////////////////////////////////////////////////////////////////////
					///
					public sealed class empty : active
					{
						public static readonly empty Instance = new empty();
						empty() { }
						public override int Count { get { return 0; } }
						public override void CopyTo(System.Array array, int index) { }
						public override IEnumerator<Q> GetEnumerator() { return Collection<Q>.NoneEnumerator; }
						public override active Add(Q item) { return new single(item); }
						public override active Remove(Q item) { return default(active); }
					};
					///
					///////////////////////////////////////////////////////////////////////


					///////////////////////////////////////////////////////////////////////
					///
					public sealed class single : active
					{
						public single(Q item)
						{
							this.item = item;
						}
						readonly Q item;
						public override int Count { get { return 1; } }
						public override IEnumerator<Q> GetEnumerator() { return new _enum(item); }
						public override Q this[int ix] { get { return item; } }
						public override void CopyTo(System.Array array, int index) { array.SetValue(item, index); }
						public override active Add(Q item) { return new duple(this, item); }
						public override active Remove(Q item) { return empty.Instance; }
						public sealed class _enum : IEnumerator<Q>
						{
							readonly Q elem;
							int i;
							public _enum(Q elem) { this.elem = elem; i = -1; }
							public bool MoveNext() { return ++i == 0; }
							public Q Current { get { return elem; } }
							Object IEnumerator.Current { get { return elem; } }
							public void Reset() { i = -1; }
							public void Dispose() { }
						};
					};
					///
					///////////////////////////////////////////////////////////////////////


					///////////////////////////////////////////////////////////////////////
					///
					public sealed class duple : active
					{
						public duple(single s, Q item)
						{
							this._0 = s[0];
							this._1 = item;
						}
						public duple(Q[] arr, int ix_remove)
						{
							Debug.Assert((ix_remove & ~3) == 0);
							this._0 = arr[((ix_remove ^ 3) + 2) >> 1];
							this._1 = arr[((ix_remove + 1) >> 1) ^ 1];	// :-P
						}
						readonly Q _0, _1;
						public override int Count { get { return 2; } }
						public override Q this[int ix] { get { return ix == 0 ? _0 : _1; } }
						public override void CopyTo(System.Array array, int index)
						{
							array.SetValue(_0, index++);
							array.SetValue(_1, index);
						}
						public override active Add(Q item) { return new multi(this, item); }
						public override active Remove(Q item)
						{
							return _0 == item ? new single(_1) : _1 == item ? new single(_0) : (active)this;
						}
						public override IEnumerator<Q> GetEnumerator() { return new _enum(this); }
						public sealed class _enum : IEnumerator<Q>
						{
							readonly duple d;
							int i;
							public _enum(duple d) { this.d = d; i = -1; }
							public bool MoveNext() { return ++i <= 1; }
							public Q Current { get { return i == 0 ? d._0 : d._1; } }
							Object IEnumerator.Current { get { return d; } }
							public void Reset() { i = -1; }
							public void Dispose() { }
						};
					};
					///
					///////////////////////////////////////////////////////////////////////


					///////////////////////////////////////////////////////////////////////
					///
					public sealed class multi : active
					{
						public multi(Q[] arr) { this.arr = arr; }
						public multi(active prev, Q item)
						{
							int c = prev.Count;
							Debug.Assert(c > 0, "Should use single.");
							Debug.Assert(c > 1, "Should use duple.");
							(this.arr = new Q[c + 1])[0] = item;
							prev.CopyTo(arr, 1);
						}
						Q[] arr;
						public override int Count { get { return arr.Length; } }
						public override Q this[int ix] { get { return arr[ix]; } }
						public override void CopyTo(System.Array array, int index) { arr.CopyTo(array, index); }
						public override active Add(Q item) { return new multi(this, item); }
						public override active Remove(Q item)
						{
							int ix = System.Array.IndexOf(arr, item);
							if (ix == -1)
								return this;
							if (arr.Length == 3)
								return new duple(this.arr, ix);
							return new multi(arr.RemoveAt(ix));
						}
						public override IEnumerator<Q> GetEnumerator() { return ((IEnumerable<Q>)arr).GetEnumerator(); }
					};
					///
					///////////////////////////////////////////////////////////////////////
				};
			};
		};

		protected struct atomic_action<ACur, ANew, TArg>
			where ACur : open.idle
			where ANew : atom
		{
			public atomic_action(ANew state) : this() { _new = state; }

			public TArg _arg;
			public ANew _new;
			public Action<ACur, TArg> a_success;
			public Func<ACur, TArg, ANew> a_reset;

			/// <summary>
			/// If successful, returns the previous atom (which was replaced). Re-tries the operation as 
			/// long as the current atom is consistent with ACur but note that ACur is not compatible with
			/// open.busy. If the fetched current atom is not consistent with ACur, returns that atom
			/// (which will be either open.busy or closed; the latter case guarantees a final atom).
			/// </summary>
			public atom _async(ref atom _list)
			{
				var _cur = _list;
				ACur _prv;
				while ((_prv = _cur as ACur) != null)
				{
					if (a_reset != null)
						_new = a_reset(_prv, _arg) ?? _new;
					if (_cur == (_cur = Interlocked.CompareExchange(ref _list, _new, _cur)))
					{
						if (a_success != null)
							a_success(_prv, _arg);
						break;
					}
				}
				return _cur;
			}

			/// <summary>
			/// Blocks until either:
			/// 1. the atom of type ACur is stored. The previous atom (which was replaced) is returned. Since
			/// your item can only have been stored if that list was open.idle, a returned object of type 
			/// 'open.idle' indicates success.
			/// 2. the list was closed or has become closed. A permanently closed atom is returned.
			/// </summary>
			public atom _sync(ref atom _list)
			{
				atom _cur;
				while ((_cur = _list) is open && (_cur = _async(ref _list)) is open)
					if (_cur is open.idle)
						break;
					else
						Thread.SpinWait(1);
				return _cur;
			}
		};
	};

	public class atoms_list<Q> : atoms<Q>
		where Q : class
	{
		static atomic_action<open.idle.waiting, open.idle.active.empty, TVoid> aa_activate;
		static atoms_list()
		{
			aa_activate = new atomic_action<open.idle.waiting, open.idle.active.empty, TVoid>(open.idle.active.empty.Instance);
		}

		public atoms_list()
		{
			this.m_list = open.idle.waiting.Instance;
		}

		protected atom m_list;

		/// <summary> atomically change state from open.idle.waiting to open.idle.active.empty. </summary>
		public bool TryActivate()
		{
			return aa_activate._sync(ref m_list) is open.idle.waiting;
		}

		public bool Add(Q item)
		{
			return new atomic_action<open.idle, open.idle, Q>
			{
				_arg = item,
				_new = null,
				a_reset = (o, x) => o.Add(x),
				a_success = null,
			}
			._sync(ref m_list) is open;
		}

		public bool Remove(Q item)
		{
			return new atomic_action<open.idle.active, open.idle, Q>
			{
				_arg = item,
				_new = null,
				a_reset = (o, x) => o.Remove(x),
				a_success = null,
			}
			._sync(ref m_list) is open;
		}

		public IReadWriteList<Q> Snapshot
		{
			get
			{
				var ii = m_list as I_items;
				return ii != null ? ii.List : open.idle.waiting.Instance;
			}
		}
	};
#if false
	[DebuggerDisplay("{ToString(),nq}")]
	public class AtomsListExposer<T> : atoms_list<T>, IReadWriteList<T>
		where T : class
	{
		public T this[int ix] { get { return base.Snapshot[ix]; } }
		public int Count { get { return base.Snapshot.Count; } }

		public override System.String ToString()
		{
			return System.String.Format("{0} state={1} count={2}",
				typeof(T)._Name(),
				m_list.GetType().Name,
				base.Snapshot.Count);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		T[] _dbg { get { return this.ToArray(); } }

		public void CopyTo(System.Array array, int index) { base.Snapshot.CopyTo(array, index); }
		public IEnumerator<T> GetEnumerator() { return base.Snapshot.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return base.Snapshot.GetEnumerator(); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsSynchronized { get { return true; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public object SyncRoot { get { return base.Snapshot; } }
	};
#endif
	public class observer_list<T> : atoms_list<IObserver<T>>
	{
		void _send_item(open.idle _prv, T item)
		{
			foreach (var obs in _prv)
				obs.OnNext(item);
			base.m_list = _prv;
		}
		static void _send_complete(open.idle _prv, TVoid _)
		{
			foreach (var obs in _prv)
				obs.OnCompleted();
		}
		static void _send_error(open.idle _prv, Exception _err)
		{
			foreach (var obs in _prv)
				obs.OnError(_err);
		}

		public atom _item(T item)
		{
			return new atomic_action<open.idle, open.busy, T>
			{
				_arg = item,
				_new = null,
				a_success = _send_item,
				a_reset = (i, x) => new open.busy(i),
			}
			._async(ref m_list);
		}
		public atom _complete()
		{
			return new atomic_action<open.idle, closed.complete, TVoid>
			{
				_arg = null,
				_new = null,
				a_success = _send_complete,
				a_reset = (i, x) => new closed.complete(i),
			}
			._async(ref m_list);
		}
		public atom _error(Exception ex)
		{
			return new atomic_action<open.idle, closed.error, Exception>
			{
				_arg = ex,
				_new = new closed.error(ex),
				a_success = _send_error,
				a_reset = null,
			}
			._async(ref m_list);
		}
	};
#endif

	public class ObservableStack<T> : Stack<T>, INotifyCollectionChanged//, INotifyPropertyChanged
	{
		public ObservableStack()
		{
		}

		public ObservableStack(IEnumerable<T> collection)
		{
			foreach (var item in collection)
				base.Push(item);
		}

		public ObservableStack(List<T> list)
		{
			foreach (var item in list)
				base.Push(item);
		}

		//public event PropertyChangedEventHandler PropertyChanged;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public new void Clear()
		{
			base.Clear();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public new T Pop()
		{
			var item = base.Pop();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
			return item;
		}

		public new void Push(T item)
		{
			base.Push(item);
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			this.RaiseCollectionChanged(e);
		}

		//protected void OnPropertyChanged(PropertyChangedEventArgs e)
		//{
		//    this.RaisePropertyChanged(e);
		//}

		void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (this.CollectionChanged != null)
				this.CollectionChanged(this, e);
		}

		//void RaisePropertyChanged(PropertyChangedEventArgs e)
		//{
		//    if (this.PropertyChanged != null)
		//        this.PropertyChanged(this, e);
		//}

		//event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		//{
		//    add { this.PropertyChanged += value; }
		//    remove { this.PropertyChanged -= value; }
		//}
	};

	public class ObservableQueue<T> : Queue<T>, INotifyCollectionChanged//, INotifyPropertyChanged
	{
		public ObservableQueue()
		{
		}

		public ObservableQueue(IEnumerable<T> collection)
		{
			foreach (var item in collection)
				base.Enqueue(item);
		}

		public ObservableQueue(List<T> list)
		{
			foreach (var item in list)
				base.Enqueue(item);
		}

		//public event PropertyChangedEventHandler PropertyChanged;

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public new void Clear()
		{
			base.Clear();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public new void Enqueue(T item)
		{
			base.Enqueue(item);
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		public new T Dequeue()
		{
			var item = base.Dequeue();
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
			return item;
		}

		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			this.RaiseCollectionChanged(e);
		}

		//protected void OnPropertyChanged(PropertyChangedEventArgs e)
		//{
		//    this.RaisePropertyChanged(e);
		//}

		void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (this.CollectionChanged != null)
				this.CollectionChanged(this, e);
		}

		//void RaisePropertyChanged(PropertyChangedEventArgs e)
		//{
		//    if (this.PropertyChanged != null)
		//        this.PropertyChanged(this, e);
		//}

		//event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		//{
		//    add { this.PropertyChanged += value; }
		//    remove { this.PropertyChanged -= value; }
		//}
	};

}
