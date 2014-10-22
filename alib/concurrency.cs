using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using alib.Debugging;
using alib.Enumerable;

namespace alib.Concurrency
{
	public static class cmp
	{
		public static T xchg<T>(ref T location1, T value, T comparand)
			where T : class
		{
			return Interlocked.CompareExchange(ref location1, value, comparand);
		}

		public static T install<T>(ref T _ref)
			where T : class,new()
		{
			T _new;
			return _ref ?? Interlocked.CompareExchange(ref _ref, _new = new T(), null) ?? _new;
		}
	}

	public struct ObjSwapper
	{
		public Object swap;

		/// <summary> Does not install your object if the type of the swapped object is already type (T) </summary>
		public T SwitchTo<T>(T t, out Object was) where T : class
		{
			Debug.Assert(t != null);
			T _tmp;
			was = null;
			Object n, o = this.swap;
			if ((_tmp = o as T) != null)
				return _tmp;
			n = Interlocked.CompareExchange<Object>(ref swap, t, o);
			if (n != o)
				return (T)n;
			was = n;
			return t;
		}
		public Object SwitchIfNull<T>(T t) where T : class
		{
			Debug.Assert(t != null);
			Object o = this.swap;
			if (o != null || o == t)
				return o;
			return Interlocked.CompareExchange<Object>(ref swap, t, null) ?? t;
		}
		/// <summary> Does not install your object if the type of the swapped object is already type (T) </summary>
		public T SwitchToT<T, TContext>(Func<TContext, T> Tcreator, TContext ctx) where T : class
		{
			T _tmp;
			Object n, o = this.swap;
			if ((_tmp = o as T) != null)
				return _tmp;
			_tmp = Tcreator(ctx);
			return o != (n = Interlocked.CompareExchange<Object>(ref swap, _tmp, o)) ? (T)n : _tmp;
		}
		public bool Is<T>() where T : class { return swap is T; }
		public T As<T>() where T : class { return swap as T; }
		public bool IsNull { [DebuggerStepThrough] get { return this.swap == null; } }
	};

	public sealed class DisposeAction : IDisposable
	{
		public DisposeAction(Action a) { this.a = a; }
		Action a;
		public void Dispose()
		{
			Action _tmp = Interlocked.Exchange(ref a, null);
			if (_tmp != null)
				_tmp();
		}
	};

	public unsafe sealed class DisposalList : IDisposable
	{
		[StructLayout(LayoutKind.Explicit)]
		struct block
		{
			[FieldOffset(0)]
			GCHandle _next_block;
			[FieldOffset(0)]
			long _ul_nb;
			[FieldOffset(8)]
			public fixed ulong arr[BlockEntries];		// also, 0xF, below

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public byte[] next_block
			{
				[DebuggerStepThrough]
				get
				{
					GCHandle gch = _next_block;	// yes, copying value type on purpose
					return gch.IsAllocated ? (byte[])gch.Target : null;
				}
			}

			public byte[] try_set_next(byte[] arr)
			{
				GCHandle gch;
				long ul;
				if (arr == null)
				{
					//gch = default(GCHandle);
					ul = Interlocked.Exchange(ref _ul_nb, 0);//*(long*)&gch);
					gch = *(GCHandle*)&ul;
					if (gch.IsAllocated)
						gch.Free();
				}
				else
				{
#if DEBUG
					gch = _next_block;	// yes, copying value type on purpose
					//if (gch.IsAllocated && gch.Target == arr)
					//    return arr;
					if (gch.IsAllocated)
						return (byte[])gch.Target;
#endif
					GCHandle gch_new = GCHandle.Alloc(arr, GCHandleType.Normal);
					ul = Interlocked.CompareExchange(ref _ul_nb, *(long*)&gch_new, 0/* *(long*)&gch*/);
					if (ul != 0)// *(long*)&gch)
					{
						gch_new.Free();
						return (byte[])((GCHandle*)&ul)->Target;
					}
				}
				return arr;
			}
		};

		int m_c;
		block b;

		public const int BlockEntriesShift = 4;
		public const int BlockEntries = 1 << BlockEntriesShift;
		const int BlockEntriesMask = BlockEntries - 1;

		public void Add(IDisposable disp)
		{
			Debug.Assert(!(disp is DisposalList));
			if (disp == null)
				return;
			int bix, ix = Interlocked.Increment(ref m_c) - 1;
			if (ix == -1)
				throw new ObjectDisposedException("DisposalList");
			if ((bix = ix >> BlockEntriesShift) > 0)
			{
				byte[] rgb;
				if ((rgb = b.next_block) == null)
					rgb = b.try_set_next(new byte[sizeof(block)]);

				while (--bix > 0)
					fixed (byte* p = rgb)
						if ((rgb = ((block*)p)->next_block) == null)
							rgb = ((block*)p)->try_set_next(new byte[sizeof(block)]);

				fixed (byte* p = rgb)
					add((block*)p, ix, disp);
			}
			else
				fixed (block* pb = &b)
					add(pb, ix, disp);
		}

		static void add(block* pb, int ix, IDisposable disp)
		{
			*(GCHandle*)&pb->arr[ix & BlockEntriesMask] = GCHandle.Alloc(disp, GCHandleType.Normal);
		}

		public void Dispose()
		{
			int c = Interlocked.Exchange(ref m_c, -1);
			if (c > 0)
				fixed (block* pb = &b)
					release_block(pb, c);
		}

		static void release_block(block* pb, int c)
		{
			byte[] rgb = pb->next_block;
			if (rgb != null)
			{
				fixed (byte* px = rgb)
					release_block((block*)px, c);
				c = BlockEntries;
				if (pb->try_set_next(null) != null)
					throw new Exception();
			}
			else
				c = ((c - 1) & BlockEntriesMask) + 1;

			GCHandle* p = (GCHandle*)&pb->arr[0];
			for (int i = 0; i < c; i++)
			{
				((IDisposable)p->Target).Dispose();
				p->Free();
				p = (GCHandle*)((ulong*)p + 1);
			}
		}
	};

#if false
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public unsafe struct GroupGenerationCounter
	{
		[FieldOffset(0)]
		long atomic;
		[FieldOffset(0)]
		public int count;
		[FieldOffset(4)]
		public int gen;

		public GenerationState TryGetCount(int gen_in, out int count_out)
		{
			GroupGenerationCounter _this = this;
			count_out = _this.count;
			return gen_in < _this.gen ?
				GenerationState.GenerationExpired :
				gen_in > _this.gen ? GenerationState.CountExpired : GenerationState.Current;
		}

		public GenerationState TrySetCount(int gen_in, int count_in)
		{
			var _cur = this;
			int d = gen_in - _cur.gen;
			if (d < 0)
				return GenerationState.GenerationExpired;
			if (d > 0 || count_in != _cur.count)
			{
				GroupGenerationCounter _new;
				var _dummy = &_new;
				_new.gen = gen_in;
				_new.count = count_in;

				long _act = Interlocked.CompareExchange(ref this.atomic, _new.atomic, _cur.atomic);
				if (_act != _cur.atomic)
				{
//#if DEBUG

					if (_new.gen == _cur.gen && _new.count != _cur.count)
						throw new Exception("different count was already set for this generation");
					//if (_new.gen 
//#endif
					return GenerationState.GenerationExpired;
				}
			}
			return GenerationState.Current;

		}
	};

	public enum GenerationState { GenerationExpired, Current, CountExpired };
#endif

	public static class _concurrency_ext
	{
		//public static void LockAndPulseAll(this Object o)
		//{
		//    Monitor.Enter(o);
		//    Monitor.PulseAll(o);
		//    Monitor.Exit(o);
		//}

		public static int InterlockedMax(ref int max, int num)
		{
			int _tmp = max;
			while (_tmp < num && _tmp != (_tmp = Interlocked.CompareExchange(ref max, num, _tmp)))
				;
			return _tmp;
		}

		public static bool InterlockedTryClaim<T>(this T _my_claim, ref T target, out T claimed_by) where T : class
		{
			return (claimed_by = Interlocked.CompareExchange(ref target, _my_claim, null) ?? _my_claim) == _my_claim;
		}

		public static void InterlockedPublishTo<T>(this T _final, ref T target) where T : class
		{
			T _claim;
			Monitor.Enter(_claim = Interlocked.Exchange(ref target, _final));
			Monitor.PulseAll(_claim);
			Monitor.Exit(_claim);
		}
		public static T InterlockedWaitPulse<T>(this T _other_claim, ref T target) where T : class
		{
			T _new;
			if ((_new = target) == _other_claim)
			{
				Monitor.Enter(_other_claim);
				if ((_new = target) == _other_claim)
				{
					Monitor.Wait(_other_claim);
					_new = target;
				}
				Monitor.Exit(_other_claim);
			}
			Debug.Assert(_new != _other_claim);
			return _new;
		}
	};

#if false
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Concurrent producer/consumer buffered into a blocking ring buffer
	/// The IEnumerator interface on this object is consuming. All IEnumerators that are issued compete to consume
	/// the objects in the buffer.
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class RingBuf<T> : IReadWriteCollection<T>
	{
		public RingBuf(int c_ranges_to_add)
		{
			this.rb = new T[0x100];
			this.c_ranges_expected = c_ranges_to_add;
		}

		public RingBuf(int c_ranges_to_add, Action<RingBuf<T>> finished_callback)
			: this(c_ranges_to_add)
		{
			this.a_finished = finished_callback;
		}
		public RingBuf()
			: this(0)
		{
		}

		T[] rb;
		byte i_put, i_take;
		bool finished_adding;
		int c_ranges_expected, c_output_items;
		Action<RingBuf<T>> a_finished;

		/////////////////////////////////////////////////////////////////////////////////////
		bool empty { get { return i_put == i_take; } }
		bool full { get { return (byte)(i_put + 1) == i_take; } }
		byte c_cur { get { return (byte)(i_put - i_take); } }

		/////////////////////////////////////////////////////////////////////////////////////
		void _finished()
		{
			if (a_finished != null)
			{
				var a = Interlocked.Exchange(ref a_finished, null);
				if (a != null)
					a(this);
			}
			rb = null;
		}

		/////////////////////////////////////////////////////////////////////////////////////
		void _add(T[] _rb, T item)
		{
			/// begin critical section
			Monitor.Enter(_rb);

			/// block if the ring buffer is full. If you haven't arranged for concurrency, then this
			/// will freeze your program when the buffer contains more than 256 items. You must be
			/// removing items on another thread or task.
			while (full)
				Monitor.Wait(_rb);

			bool f_was_empty = empty;

			/// put the object into the ring buffer
			_rb[i_put] = item;
			i_put++;

			/// if the ring buffer was empty, notify the consumer that a result is now available
			if (f_was_empty)
				Monitor.PulseAll(_rb);

			/// end of critical section
			Monitor.Exit(_rb);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public int Count { get { return c_cur; } }
		public int OutputItemsCount { get { return c_output_items; } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public virtual void AddRange(IEnumerable<T> e)
		{
			if (finished_adding)
				throw new Exception("Adding to the ring buffer was closed.");
			var _rb = rb;
			if (_rb == null)
				throw new Exception();

			IEnumerator<T> ee = e.GetEnumerator();
			while (ee.MoveNext())
				_add(_rb, ee.Current);

			if (c_ranges_expected > 0 && Interlocked.Decrement(ref c_ranges_expected) == 0)
				CompleteAdding();
			else if (finished_adding)
			{
				Monitor.Enter(_rb);
				Monitor.PulseAll(_rb);
				Monitor.Exit(_rb);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Add(T item)
		{
			if (finished_adding)
				throw new Exception("Adding to the ring buffer was closed.");
			var _rb = rb;
			if (_rb == null)
				throw new Exception();

			_add(_rb, item);

			if (finished_adding)
			{
				Monitor.Enter(_rb);
				Monitor.PulseAll(_rb);
				Monitor.Exit(_rb);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void CompleteAdding()
		{
			Monitor.Enter(rb);
			if (finished_adding)
				throw new Exception("Adding to the ring buffer was already closed.");
			finished_adding = true;
			Monitor.PulseAll(rb);
			Monitor.Exit(rb);
			if (empty)
				_finished();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public T Take()
		{
			T cur = default(T);
			var _rb = rb;
			if (_rb != null)
			{
				Monitor.Enter(_rb);
				if (!finished_adding || !empty)
				{
					while (!finished_adding && empty)
						Monitor.Wait(_rb);

					if (!empty)
					{
						/// opportunistic lock piggybacking
						c_output_items++;

						bool f_was_full = full;

						/// get the object out of the ring buffer
						cur = _rb[i_take];
						i_take++;

						/// if the ring buffer was full, notify the adders that this may now no longer be the case
						if (f_was_full && !finished_adding)
							Monitor.PulseAll(_rb);
					}
				}
				Monitor.Exit(_rb);

				if (cur != null && finished_adding && empty)
					_finished();
			}
			return cur;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public bool TryTake(out T cur)
		{
			cur = default(T);
			bool f_ok = false;
			var _rb = rb;
			if (_rb != null)
			{
				Monitor.Enter(_rb);
				if (!empty)
				{
					/// opportunistic lock piggybacking
					c_output_items++;

					bool f_was_full = full;

					/// get the object out of the ring buffer
					cur = _rb[i_take];
					i_take++;
					f_ok = true;

					/// if the ring buffer was full, notify the adders that this may now no longer be the case
					if (f_was_full && !finished_adding)
						Monitor.PulseAll(_rb);
				}
				Monitor.Exit(_rb);

				if (cur != null && finished_adding && empty)
					_finished();
			}
			return f_ok;
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) { throw not.valid; }
		bool ICollection<T>.Contains(T item) { throw not.valid; }
		bool ICollection<T>.Remove(T item) { throw not.impl; }
		void ICollection<T>.Clear() { throw not.valid; }
		bool ICollection<T>.IsReadOnly { get { return false; } }
		void ICollection.CopyTo(System.Array array, int index) { throw not.valid; }
		Object ICollection.SyncRoot { get { return null; } }
		bool ICollection.IsSynchronized { get { return true; } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		struct _enum : IEnumerator<T>
		{
			public _enum(RingBuf<T> rb) { this.rb = rb; this.cur = default(T); }
			RingBuf<T> rb;
			T cur;
			public T Current { get { return cur; } }
			object IEnumerator.Current { get { return cur; } }
			public bool MoveNext() { cur = rb.Take(); return cur != null; }
			public void Reset() { cur = default(T); }
			public void Dispose() { rb = null; cur = default(T); }
		}

		public IEnumerator<T> GetEnumerator() { return new _enum(this); }
		IEnumerator IEnumerable.GetEnumerator() { return new _enum(this); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Similar to RingBuf(T), but allows you to install, upon construction, a utility (e.g. multithreading) wrapper 
	/// which is automatically placed around each of your subsequent AddRange(...) calls.
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class AddWrapperRingBuf<T, R> : RingBuf<T>
		where T : class
	{
		public AddWrapperRingBuf(
			int c_ranges_to_add,
			Func<Action<IEnumerable<T>>, IEnumerable<T>, R> ar_wrap,
			Action<RingBuf<T>> ar_finished)
			: base(c_ranges_to_add, ar_finished)
		{
			if (ar_wrap == null)
				throw new ArgumentException();
			this.ar_wrap = ar_wrap;
		}
		readonly Func<Action<IEnumerable<T>>, IEnumerable<T>, R> ar_wrap;
		public override void AddRange(IEnumerable<T> e) { ar_wrap(base.AddRange, e); }
	}

	public class AddWrapperRingBuf<T> : RingBuf<T>
		where T : class
	{
		public AddWrapperRingBuf(
			int c_ranges_to_add,
			Action<Action<IEnumerable<T>>, IEnumerable<T>> ar_wrap,
			Action<RingBuf<T>> ar_finished)
			: base(c_ranges_to_add, ar_finished)
		{
			if (ar_wrap == null)
				throw new ArgumentException();
			this.ar_wrap = ar_wrap;
		}
		readonly Action<Action<IEnumerable<T>>, IEnumerable<T>> ar_wrap;
		public override void AddRange(IEnumerable<T> e) { ar_wrap(base.AddRange, e); }
	}
#endif

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Manual signalling event which uses the managed Monitor mechanism. Can only be used (i.e. set) once.
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class ManualSetEvent : Object
	{
		bool f_expired = false;
		public void Wait()
		{
			if (!f_expired)
			{
				Monitor.Enter(this);
				while (!f_expired)
					Monitor.Wait(this, -1, false);
				Monitor.Exit(this);
			}
		}
		public void Set()
		{
			if (f_expired)
				throw new Exception("The event was previously set.");
			Monitor.Enter(this);
			f_expired = true;
			Monitor.PulseAll(this);
			Monitor.Exit(this);
		}
	}

	/// <summary>
	/// Ensures that some (expensive) work to construct an item of type T is only executed once. The point of this value 
	/// type is to avoid allocating any lock until one is actually needed (to wait on) by the arrival of a second requestor.
	/// </summary>
#if true
	[DebuggerDisplay("{ToString(),nq}")]
	public struct MemoizedItem<T>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		T t;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ManualSetEvent mre;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		const ManualSetEvent New = null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		static ManualSetEvent Busy = new ManualSetEvent();
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		static ManualSetEvent Done = new ManualSetEvent();

		[DebuggerStepThrough]
		public MemoizedItem(T t)
		{
			this.t = t;
			this.mre = Done;
		}

		[DebuggerStepThrough]
		public bool FirstClaim()
		{
			return mre == New && Interlocked.CompareExchange(ref mre, Busy, New) == New;
		}

		[DebuggerStepThrough]
		public void SetResult(T result)
		{
			Debug.Assert(mre != New && mre != Done);
			this.t = result;

			ManualSetEvent last = Interlocked.Exchange(ref mre, Done);
			if (last != Busy)
				last.Set();
		}

		public bool IsStarted { [DebuggerStepThrough] get { return mre != New; } }
		public bool IsNotStarted { [DebuggerStepThrough] get { return mre == New; } }
		public bool IsRendered { [DebuggerStepThrough] get { return mre == Done; } }
		public bool IsRendering
		{
			[DebuggerStepThrough]
			get
			{
				var _tmp = mre;
				return _tmp != New && _tmp != Done;
			}
		}

		[DebuggerStepThrough]
		public void Wait()
		{
			ManualSetEvent mx = mre;
			if (mx == New)
				throw new Exception();

			ManualSetEvent promote = null;
			while (mx != Done)
			{
				if (mx == Busy)
				{
					if (promote == null)
						promote = new ManualSetEvent();
					if ((mx = mre) != Busy)
						continue;
					if ((mx = Interlocked.CompareExchange(ref mre, promote, Busy)) != Busy)
						continue;
					mx = promote;
				}
				if (mx == mre)
					mx.Wait();
				return;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Result
		{
			[DebuggerStepThrough]
			get
			{
				Wait();
				return this.t;
			}
		}

		public override string ToString()
		{
			return IsNotStarted ? "(deferred)" : t.ToString();
		}
	};
#else
	static class _ols
	{
		public static readonly Object oBusy = new Object();
		public static readonly Object oDone = new Object();
	}
	public struct MemoizedItem<T>
	{
		T t;
		Object o_lock;

		public bool FirstClaim()
		{
			return o_lock == null && Interlocked.CompareExchange(ref o_lock, _ols.oBusy, null) == null;
		}

		public void SetResult(T result)
		{
			this.t = result;
			Object _tmp = Interlocked.Exchange(ref o_lock, _ols.oDone);
			if (_tmp == _ols.oDone)
				throw new Exception();
			if (_tmp != null && _tmp != _ols.oBusy)
			{
				Monitor.Enter(_tmp);
				Monitor.PulseAll(_tmp);
				Monitor.Exit(_tmp);
			}
		}

		public bool IsNotStarted { get { return o_lock == null; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		[DebuggerStepThrough]
		public T Result
		{
			get
			{
				Object _cur = this.o_lock;
				if (_cur != _ols.oDone)
				{
					if (_cur == _ols.oBusy)
					{
						Object _tmp;
						_cur = Interlocked.CompareExchange(ref o_lock, _tmp = new Object(), _ols.oBusy);
						if (_cur == _ols.oBusy)
							_cur = _tmp;
					}
					if (_cur == _ols.oBusy)
						throw new Exception();
					if (_cur != _ols.oDone)
					{
						Monitor.Enter(_cur);
						while (o_lock != _ols.oDone)
							Monitor.Wait(_cur);
						Monitor.Exit(_cur);
					}
				}
				return this.t;
			}
		}
	};
#endif

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class ConcurrentList<T> : List<T>//: ConcurrentContainer<T>
	{
		//readonly List<T> list = new List<T>();
		SpinLock m_lock = new SpinLock(false);	// do not make this 'readonly'

		public ConcurrentList(int capacity)
			: base(capacity)
		{
		}
		public ConcurrentList()
		{
		}

		public ConcurrentList(T t)
		{
			this.Add(t);
		}

		public new /*override*/ void Clear()
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				base.Clear();
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public new /*override*/ bool Add(T item)
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				base.Add(item);
				return true;
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public new void AddRange(IEnumerable<T> items)
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				base.AddRange(items);
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		/// <summary>
		/// (useless for concurrency)
		/// </summary>
		public new /*override*/ bool Contains(T item)
		{
#if DEBUG
			if (this is ISet<T>)
				throw not.expected;
#endif
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				return base.Contains(item);
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		/// <summary>
		/// (useless for concurrency, accordingly, not locked)
		/// </summary>
		public new /*override*/ int Count { get { return base.Count; } }

		public new /*override*/ IEnumerator<T> GetEnumerator()
		{
			while (true)
			{
				bool entered = false;
				try
				{
					m_lock.Enter(ref entered);
					return ((IEnumerable<T>)base.ToArray()).GetEnumerator();
				}
				finally
				{
					if (entered)
						m_lock.Exit(false);
				}
			}
		}

		public IEnumerable<T> GetEnumerableUnsafe()
		{
			return (IEnumerable<T>)base.ToArray();
		}

		public new T[] ToArray()
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				return base.ToArray();
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public new /*override*/ bool Remove(T item) { throw not.valid; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class ConcurrentContainer<T> : Collections.rw_set_base<T>//, _ISet<T>
	{
		public T this[int index]
		{
			get { throw not.impl; }
		}

		//int _ISet<T>.RemoveWhere(Predicate<T> match) { throw not.impl; }

		public void ExceptWith(IEnumerable<T> other) { throw not.valid; }

		public void IntersectWith(IEnumerable<T> other) { throw not.valid; }

		//public void UnionWith(IEnumerable<T> other) { throw not.valid; }

		public bool IsProperSubsetOf(IEnumerable<T> other) { throw not.valid; }

		public bool IsProperSupersetOf(IEnumerable<T> other) { throw not.valid; }

		public bool IsSubsetOf(IEnumerable<T> other) { throw not.valid; }

		public bool IsSupersetOf(IEnumerable<T> other) { throw not.valid; }

		public bool Overlaps(IEnumerable<T> other) { throw not.valid; }

		public bool SetEquals(IEnumerable<T> other) { throw not.valid; }

		public void SymmetricExceptWith(IEnumerable<T> other) { throw not.valid; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class ConcurrentHashSet<T> : ConcurrentContainer<T>
	{
		readonly HashSet<T> hs = new HashSet<T>();
		SpinLock m_lock = new SpinLock(false);	// do not make this 'readonly'
		T any = default(T);

		public T Peek() { return any; }

		public bool Add(T item)
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				bool b = hs.Add(item);
				if (b)
					any = item;
				return b;
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public bool Remove(T item)
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				bool b = hs.Remove(item);
				if (b && any.Equals(item))
				{
					if (hs.Count == 0)
						any = default(T);
					else
					{
						var e = hs.GetEnumerator();
						e.MoveNext();
						any = e.Current;
					}
				}
				return b;
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public void Clear()
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				hs.Clear();
				any = default(T);
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public override bool Contains(T item)
		{
#if DEBUG
			if (this is ISet<T>)
				throw not.expected;
#endif
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				return hs.Contains(item);
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public override T[] ToArray()
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				return hs.ToArray();
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return enum_ext.Enumerator(this.ToArray());
		}

		public int Count { get { return hs.Count; } }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class ConcurrentStringBuilder
	{
		readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();
		SpinLock m_lock = new SpinLock(false);	// do not make this 'readonly'

		public void Clear()
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				sb.Clear();
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public ConcurrentStringBuilder Append(System.String s)
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				sb.Append(s);
				return this;
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public ConcurrentStringBuilder AppendFormat(System.String fmt, params Object[] args)
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				sb.AppendFormat(fmt, args);
				return this;
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public override System.String ToString()
		{
			bool entered = false;
			try
			{
				m_lock.Enter(ref entered);
				return sb.ToString();
			}
			finally
			{
				if (entered)
					m_lock.Exit(false);
			}
		}

		public int Length
		{
			get
			{
				bool entered = false;
				try
				{
					m_lock.Enter(ref entered);
					return sb.Length;
				}
				finally
				{
					if (entered)
						m_lock.Exit(false);
				}
			}
			set
			{
				bool entered = false;
				try
				{
					m_lock.Enter(ref entered);
					sb.Length = value;
				}
				finally
				{
					if (entered)
						m_lock.Exit(false);
				}
			}
		}
	};



	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class ConcurrentSymmetricDictionary<K1, K2>
	{
		FalseSharing.Padding60 fsp1;
		protected SpinLock slock = new SpinLock(false);	// do not make this 'readonly'
		FalseSharing.Padding60 fsp2;

		public readonly Dictionary<K1, K2> forward;
		public readonly Dictionary<K2, K1> reverse;

		public ConcurrentSymmetricDictionary(IEqualityComparer<K1> c)
		{
			forward = new Dictionary<K1, K2>(c);
			reverse = new Dictionary<K2, K1>();
			Debugging.Nop.X(fsp1, fsp2);
		}

		public ConcurrentSymmetricDictionary(IEqualityComparer<K2> c)
		{
			forward = new Dictionary<K1, K2>();
			reverse = new Dictionary<K2, K1>(c);
			Debugging.Nop.X(fsp1, fsp2);
		}

		public ConcurrentSymmetricDictionary()
		{
			forward = new Dictionary<K1, K2>();
			reverse = new Dictionary<K2, K1>();
			Debugging.Nop.X(fsp1, fsp2);
		}

		public K2 this[K1 k1]
		{
			get
			{
				return forward[k1];
			}
			set
			{
				bool _b = false;
				try
				{
					slock.Enter(ref _b);
					forward[k1] = value;
					reverse[value] = k1;
				}
				finally
				{
					if (_b) slock.Exit();
				}
			}
		}

		public K1 this[K2 k2]
		{
			get
			{
				return reverse[k2];
			}
			set
			{
				bool _b = false;
				try
				{
					slock.Enter(ref _b);
					reverse[k2] = value;
					forward[value] = k2;
				}
				finally
				{
					if (_b) slock.Exit();
				}
			}
		}

		//public void Add(K1 k1, K2 k2)
		//{
		//    bool _b = false;
		//    try
		//    {
		//        slock.Enter(ref _b);
		//        forward.Add(k1, k2);
		//        reverse.Add(k2, k1);
		//    }
		//    finally
		//    {
		//        if (_b) slock.Exit();
		//    }
		//}


		public bool Remove(K1 k1)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				K2 k2;
				if (!forward.TryGetValue(k1, out k2))
					return false;
				forward.Remove(k1);
				reverse.Remove(k2);
				return true;
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}

		public bool Remove(K2 k2)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				K1 k1;
				if (!reverse.TryGetValue(k2, out k1))
					return false;
				reverse.Remove(k2);
				forward.Remove(k1);
			}
			finally
			{
				if (_b) slock.Exit();
			}
			return true;
		}

		public void Clear()
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				forward.Clear();
				reverse.Clear();
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}

		//public bool TryGetValue(K2 k2, out K1 k1)
		//{
		//    return reverse.TryGetValue(k2, out k1);
		//}
		//public bool TryGetValue(K1 k1, out K2 k2)
		//{
		//    return forward.TryGetValue(k1, out k2);
		//}

		public bool ContainsKey(K1 k1)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				return forward.ContainsKey(k1);
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}

		public bool ContainsKey(K2 k2)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				return reverse.ContainsKey(k2);
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}

		public bool TryGetKey(K1 k1, out K2 k2)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				return forward.TryGetValue(k1, out k2);
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}

		public K2 GetOrAdd(K1 k1, K2 k2)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				K2 v;
				if (!forward.TryGetValue(k1, out v))
				{
					v = k2;
					forward.Add(k1, v);
					reverse.Add(v, k1);
				}
				return v;
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}
		public K2 GetOrAdd(K1 k1, Func<K2> get_new_val)
		{
			bool _b = false;
			try
			{
				slock.Enter(ref _b);
				K2 k2;
				if (!forward.TryGetValue(k1, out k2))
				{
					k2 = get_new_val();
					forward.Add(k1, k2);
					reverse.Add(k2, k1);
				}
				return k2;
			}
			finally
			{
				if (_b) slock.Exit();
			}
		}

		public int Count { get { return forward.Count; } }

		public IEnumerable<KeyValuePair<K1, K2>> Enumerate()
		{
			return forward;
		}
	};

	public struct cache_line
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Object o1, o2, o3, o4, o5, o6, o7, o8;
	}


	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct fs_pad<T> where T : struct
	{
		public void _dummy(ref fs_pad<T> x)
		{
			x.o1a = null;
			x.o1b = null;
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Object o1a, o2a, o3a, o4a, o5a, o6a, o7a, o8a;
		public T Value;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Object o1b, o2b, o3b, o4b, o5b, o6b, o7b, o8b;

		public static implicit operator T(fs_pad<T> t) { return t.Value; }
	}
#if false
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct shared_ref<T> where T : class
	{
		public void _dummy(ref shared_ref<T> x)
		{
			x.o1a = null;
			x.o1b = null;
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T o1a, o2a, o3a, o4a, o5a, o6a, o7a, o8a, Value, o1b, o2b, o3b, o4b, o5b, o6b, o7b, o8b;

		public static implicit operator T(shared_ref<T> t) { return t.Value; }
		public static bool operator ==(T t1, shared_ref<T> t2) { return Object.Equals(t1, t2.Value); }
		public static bool operator !=(T t1, shared_ref<T> t2) { return !Object.Equals(t1, t2.Value); }
		public static bool operator ==(shared_ref<T> t1, T t2) { return Object.Equals(t1.Value, t2); }
		public static bool operator !=(shared_ref<T> t1, T t2) { return !Object.Equals(t1.Value, t2); }

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
#endif
	[StructLayout(LayoutKind.Sequential)]
	public struct fs_pad_int
	{
		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public cache_line cl0;

		public int i;

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public cache_line cl1;
	}

	public static class FalseSharing
	{
		[StructLayout(LayoutKind.Explicit, Size = 56)]
		public struct Padding56
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 60)]
		public struct Padding60
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 64)]
		public struct Padding64
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 120)]
		public struct Padding120
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 124)]
		public struct Padding124
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 128)]
		public struct Padding128
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 248)]
		public struct Padding248
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 252)]
		public struct Padding252
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 256)]
		public struct Padding256
		{
		};

		[StructLayout(LayoutKind.Explicit, Size = 2048)]
		public struct Padding2048
		{
		};
	};
}


namespace alib.Collections.Concurrent
{
	using Array = System.Array;

	[DebuggerDisplay("Count = {this.m_c}  Type = {_item_type().Name,nq}")]
	public class ConcurrentRefList<T> : IList<T>, IList where T : class
	{
		const int _defaultCapacity = 4;
		public static readonly ConcurrentRefList<T> Empty = new ConcurrentRefList<T>();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		T[] m_arr;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int m_c;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// _ctor
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[DebuggerStepThrough]
		public ConcurrentRefList()
		{
			m_arr = alib.Collections.Collection<T>.None;
		}

		[DebuggerStepThrough]
		public ConcurrentRefList(T[] arr)
		{
			this.m_arr = arr;
			this.m_c = arr.Length;
		}

		public ConcurrentRefList(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException();

			var is2 = collection as ICollection;
			if (is2 != null)
			{
				int count = is2.Count;
				m_arr = new T[count];
				is2.CopyTo(m_arr, 0);
				m_c = count;
			}
			else
			{
				m_c = 0;
				m_arr = new T[0x4];
				using (IEnumerator<T> enumerator = collection.GetEnumerator())
				{
					while (enumerator.MoveNext())
						_unsafe_add(enumerator.Current);
				}
			}
		}

		public ConcurrentRefList(int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException();

			m_arr = new T[capacity];
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// private
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		void EnsureCapacity(int min)
		{
			if (m_arr.Length < min)
			{
				int i = (m_arr.Length == 0) ? 0x4 : (m_arr.Length * 0x2);
				if (i < min)
					i = min;

				if (i < m_c)
					throw new ArgumentException();

				if (i != m_arr.Length)
				{
					if (i > 0)
					{
						T[] destinationArray = new T[i];
						if (m_c > 0)
							Array.Copy(m_arr, 0, destinationArray, 0, m_c);

						m_arr = destinationArray;
					}
					else
						m_arr = alib.Collections.Collection<T>.None;
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// AList
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public T[] GetArray(out int c)
		{
			while (true)
			{
				var _1 = m_arr;
				int _c = m_c;
				Thread.MemoryBarrier();
				if (_1 == m_arr)
				{
					c = _c;
					return _1;
				}
				Nop.CodeCoverage("concurrent reflist");
			}
		}

		public T[] GetArray()
		{
			var _tmp = m_arr;
			return _tmp ?? alib.Collections.Collection<T>.None;
		}

		public int Capacity
		{
			get
			{
				var _tmp = m_arr;
				return _tmp == null ? 0 : _tmp.Length;
			}
		}

#if false
		public ObjectModel.ReadOnlyCollection<T> AsReadOnly()
		{
			return new ObjectModel.ReadOnlyCollection<T>(this);
		}

		public int BinarySearch(T item)
		{
			return BinarySearch(0, m_c, item, null);
		}

		public int BinarySearch(T item, IComparer<T> comparer)
		{
			return BinarySearch(0, m_c, item, comparer);
		}

		public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();

			return Array.BinarySearch<T>(m_arr, index, count, item, comparer);
		}

		public List<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
		{
			if (converter == null)
				throw new ArgumentNullException();

			List<TOutput> list = new List<TOutput>(m_c);
			for (int i = 0; i < m_c; i++)
				list.Add(converter(m_arr[i]));
			return list;
		}

		public void CopyTo(T[] array)
		{
			CopyTo(array, 0);
		}

		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if ((m_c - index) < count)
				throw new ArgumentException();

			Array.Copy(m_arr, index, array, arrayIndex, count);
		}


		public bool Exists(Predicate<T> match)
		{
			return m_c > 0 && FindIndex(0, m_c, match) != -1;
		}

		public T Find(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return m_arr[i];
			return null;
		}

		public RefList<T> FindAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			RefList<T> list = new RefList<T>();
			for (int i = 0; i < m_c; i++)
			{
				if (match(m_arr[i]))
				{
					list.Add(m_arr[i]);
				}
			}
			return list;
		}

		public bool Any(Predicate<T> match)
		{
			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return true;
			return false;
		}
		public int FindIndex(Predicate<T> match)
		{
			for (int i = 0; i < m_c; i++)
				if (match(m_arr[i]))
					return i;
			return -1;
		}

		public int FindIndex(int startIndex, Predicate<T> match)
		{
			return FindIndex(startIndex, m_c - startIndex, match);
		}

		public int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			if (startIndex > m_c || count < 0 || startIndex > m_c - count)
				throw new ArgumentOutOfRangeException();

			if (match == null)
				throw new ArgumentNullException();

			int num = startIndex + count;
			for (int i = startIndex; i < num; i++)
				if (match(m_arr[i]))
					return i;
			return -1;
		}

		public T FindLast(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			for (int i = m_c - 1; i >= 0; i--)
				if (match(m_arr[i]))
					return m_arr[i];
			return null;
		}

		public int FindLastIndex(Predicate<T> match)
		{
			return FindLastIndex(m_c - 1, m_c, match);
		}

		public int FindLastIndex(int startIndex, Predicate<T> match)
		{
			return FindLastIndex(startIndex, startIndex + 1, match);
		}

		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();
			if (m_c == 0)
			{
				if (startIndex != -1)
					throw new ArgumentOutOfRangeException();
			}
			else if (startIndex >= m_c)
				throw new ArgumentOutOfRangeException();

			if ((count < 0) || (((startIndex - count) + 1) < 0))
				throw new ArgumentOutOfRangeException();

			int num = startIndex - count;
			for (int i = startIndex; i > num; i--)
				if (match(m_arr[i]))
					return i;
			return -1;
		}

		public void ForEach(Action<T> action)
		{
			if (action == null)
				throw new ArgumentNullException();

			for (int i = 0; i < m_c; i++)
				action(m_arr[i]);
		}

		public RefList<T> GetRange(int index, int count)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();

			RefList<T> list = new RefList<T>(count);
			Array.Copy(m_arr, index, list.m_arr, 0, count);
			list.m_c = count;
			return list;
		}
#endif
		public int IndexOf(T item, int index)
		{
			Monitor.Enter(this);
			if (index > m_c)
				throw new ArgumentOutOfRangeException();

			int i = Array.IndexOf<T>(m_arr, item, index, m_c - index);
			Monitor.Exit(this);
			return i;
		}

		public int IndexOf(T item, int index, int count)
		{
			Monitor.Enter(this);
			if (index > m_c || (count < 0) || (index > (m_c - count)))
				throw new ArgumentOutOfRangeException();

			int i = Array.IndexOf<T>(m_arr, item, index, count);
			Monitor.Exit(this);
			return i;
		}

		public void AddRange(IEnumerable<T> collection)
		{
			InsertRange(m_c, collection);
		}

		public void InsertRange(int index, IEnumerable<T> collection)
		{
			Monitor.Enter(this);
			if (collection == null)
				throw new ArgumentNullException();

			if (index > m_c)
				throw new ArgumentOutOfRangeException();

			var is2 = collection as ICollection;
			if (is2 != null)
			{
				int count = is2.Count;
				if (count > 0)
				{
					EnsureCapacity(m_c + count);
					if (index < m_c)
					{
						Array.Copy(m_arr, index, m_arr, index + count, m_c - index);
					}
					if (this == is2)
					{
						Array.Copy(m_arr, 0, m_arr, index, index);
						Array.Copy(m_arr, (int)(index + count), m_arr, (int)(index * 0x2), (int)(m_c - index));
					}
					else
					{
						T[] array = new T[count];
						is2.CopyTo(array, 0);
						array.CopyTo(m_arr, index);
					}
					m_c += count;
				}
			}
			else
			{
				using (IEnumerator<T> enumerator = collection.GetEnumerator())
				{
					while (enumerator.MoveNext())
						_unsafe_insert(index++, enumerator.Current);
				}
			}
			Monitor.Exit(this);
		}

#if false
		public int LastIndexOf(T item)
		{
			if (m_c == 0)
				return -1;
			return LastIndexOf(item, m_c - 1, m_c);
		}

		public int LastIndexOf(T item, int index)
		{
			if (index >= m_c)
				throw new ArgumentOutOfRangeException();

			return LastIndexOf(item, index, index + 1);
		}

		public int LastIndexOf(T item, int index, int count)
		{
			if (index < 0 || count < 0)
				throw new ArgumentOutOfRangeException();
			if (m_c == 0)
				return -1;
			if (index >= m_c || count > (index + 1))
				throw new ArgumentOutOfRangeException();

			return Array.LastIndexOf<T>(m_arr, item, index, count);
		}
#endif
		public int RemoveAll(int src, Predicate<T> _remove)
		{
			Monitor.Enter(this);
			int dst = 0;
			while (src < m_c)
			{
				T t;
				if (!_remove(t = m_arr[src]))
				{
					if (src != dst)
						m_arr[dst] = t;
					dst++;
				}
				src++;
			}
			if ((src -= dst) > 0)
				m_c = dst;
			Monitor.Exit(this);
			return src;
		}

		public T RemoveItem(T item)
		{
			Monitor.Enter(this);
			int ix = Array.IndexOf<T>(m_arr, item, 0, m_c);
			T t;
			if (ix == -1)
				t = null;
			else
			{
				t = m_arr[ix];

				m_c--;
				Array.Copy(m_arr, ix + 1, m_arr, ix, m_c - ix);
				m_arr[m_c] = null;
			}
			Monitor.Exit(this);
			return t;
		}

		public T RemoveFirst(Predicate<T> _remove)
		{
			T t;
			Monitor.Enter(this);
			for (int i = 0; i < m_c; i++)
			{
				if (_remove(t = m_arr[i]))
				{
					m_c--;
					Array.Copy(m_arr, i + 1, m_arr, i, m_c - i);
					m_arr[m_c] = null;
					goto exit;
				}
			}
			t = null;
		exit:
			Monitor.Exit(this);
			return t;
		}

		public int RemoveAll(Predicate<T> _remove)
		{
#if true
			Monitor.Enter(this);
			int src = 0, dst = 0;
			while (src < m_c)
			{
				T t;
				if (!_remove(t = m_arr[src]))
				{
					if (src != dst)
						m_arr[dst] = t;
					dst++;
				}
				src++;
			}
			if ((src -= dst) > 0)
				m_c = dst;
			Monitor.Exit(this);
			return src;
#else
			if (match == null)
				throw new ArgumentNullException();

			int index = 0;
			while ((index < m_c) && !match(m_arr[index]))
				index++;

			if (index >= m_c)
				return 0;

			int i = index + 1;
			while (i < m_c)
			{
				while ((i < m_c) && match(m_arr[i]))
					i++;

				if (i < m_c)
					m_arr[index++] = m_arr[i++];

			}
			Array.Clear(m_arr, index, m_c - index);
			int j = m_c - index;
			m_c = index;
			return j;
#endif
		}

#if false
		public void RemoveRange(int index, int count)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();

			if (count > 0)
			{
				m_c -= count;
				if (index < m_c)
					Array.Copy(m_arr, index + count, m_arr, index, m_c - index);
				Array.Clear(m_arr, m_c, count);
			}
		}

		public void Reverse()
		{
			Array.Reverse(m_arr, 0, m_c);
		}

		public void Reverse(int index, int count)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();
			Array.Reverse(m_arr, index, count);
		}

		public bool IsSorted(IComparer<T> cmp)
		{
			if (m_c >= 2)
			{
				T item = m_arr[0];
				for (int i = 1; i < m_c; i++)
					if (cmp.Compare(item, item = m_arr[i]) > 0)
						return false;
			}
			return true;
		}

		public void Sort()
		{
			//Sort(0, c, null);
			alib.Array._array_ext.qsort(m_arr, 0, m_c - 1, Comparer<T>.Default);
		}

		public void Sort(IComparer<T> comparer)
		{
			//Sort(0, m_c, comparer);
			alib.Array._array_ext.qsort(m_arr, 0, m_c - 1, comparer);
		}

		//public void Sort(Comparison<T> comparison)
		//{
		//    if (comparison == null)
		//        throw new ArgumentNullException();

		//    if (_size > 0)
		//    {
		//        IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
		//        Array.Sort<T>(_items, 0, _size, comparer);
		//    }
		//}

		public void Sort(int index, int count, IComparer<T> comparer)
		{
			if (index < 0 || count < 0 || (m_c - index) < count)
				throw new ArgumentOutOfRangeException();
			//Array.Sort<T>(m_arr, index, count, comparer);
			alib.Array._array_ext.qsort(m_arr, index, count - 1, comparer);
		}

		public T[] ToArray()
		{
			T[] tmp = new T[m_c];
			Array.Copy(m_arr, 0, tmp, 0, m_c);
			return tmp;
		}

		public void TrimExcess()
		{
			int num = (int)(m_arr.Length * 0.9);
			if (m_c < num)
				Capacity = m_c;
		}

		public bool TrueForAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException();

			for (int i = 0; i < m_c; i++)
				if (!match(m_arr[i]))
					return false;
			return true;
		}
#endif
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// IList(T)
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public int Count { get { return m_c; } }

		public T this[int index]
		{
			get
			{
				Monitor.Enter(this);
				if (index >= m_c)
					throw new ArgumentOutOfRangeException();
				T t = m_arr[index];
				Monitor.Exit(this);
				return t;
			}
			set
			{
				Monitor.Enter(this);
				if (index >= m_c)
					throw new ArgumentOutOfRangeException();
				m_arr[index] = value;
				Monitor.Exit(this);
			}
		}

		public int IndexOf(T item)
		{
			Monitor.Enter(this);
			int i = Array.IndexOf<T>(m_arr, item, 0, m_c);
			Monitor.Exit(this);
			return i;
		}

		public bool Contains(T item)
		{
#if DEBUG
			if (this is ISet<T>)
				throw not.expected;
#endif
			bool b_ret = false;
			Monitor.Enter(this);
			for (int i = 0; i < m_c; i++)
				if (Object.Equals(m_arr[i], item))
				{
					b_ret = true;
					break;
				}
			Monitor.Exit(this);
			return b_ret;
		}

		public void Add(T item)
		{
			_add(item);
		}
		int _add(T item)
		{
			Monitor.Enter(this);
			int c = _unsafe_add(item);
			Monitor.Exit(this);
			return c;
		}
		int _unsafe_add(T item)
		{
			if (m_c == m_arr.Length)
				EnsureCapacity(m_c + 1);

			m_arr[m_c++] = item;
			return m_c;
		}

		void _unsafe_insert(int index, T item)
		{
			if (index > m_c)
				throw new ArgumentOutOfRangeException();

			if (m_c == m_arr.Length)
				EnsureCapacity(m_c + 1);

			if (index < m_c)
				Array.Copy(m_arr, index, m_arr, index + 1, m_c - index);

			m_arr[index] = item;
			m_c++;
		}
		public void Insert(int index, T item)
		{
			throw not.valid;
			//Monitor.Enter(this);
			//_unsafe_insert(index, item);
			//Monitor.Exit(this);
		}

		public bool Remove(T item)
		{
			bool b_ret = false;
			Monitor.Enter(this);
			int index = Array.IndexOf<T>(m_arr, item, 0, m_c);
			if (index >= 0)
			{
				m_c--;
				if (index < m_c)
					Array.Copy(m_arr, index + 1, m_arr, index, m_c - index);

				m_arr[m_c] = null;
				b_ret = true;
			}
			Monitor.Exit(this);
			return b_ret;
		}

		public void RemoveAt(int index)
		{
			throw not.valid;
			//Monitor.Enter(this);
			//if (index >= m_c)
			//    throw new ArgumentOutOfRangeException();

			//m_c--;
			//if (index < m_c)
			//    Array.Copy(m_arr, index + 1, m_arr, index, m_c - index);

			//m_arr[m_c] = null;
			//Monitor.Exit(this);
		}

		public void Clear()
		{
			Monitor.Enter(this);
			if (m_c > 0)
			{
				Array.Clear(m_arr, 0, m_c);
				m_c = 0;
			}
			Monitor.Exit(this);
		}

		public void SetCount(int count)
		{
			Monitor.Enter(this);
			if (count > m_arr.Length)
				throw new ArgumentException();
			m_c = count;
			Monitor.Exit(this);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			int c;
			T[] _tmp = GetArray(out c);

			Array.Copy(_tmp, 0, array, arrayIndex, m_c);
		}

		public IEnumerator<T> GetEnumerator()
		{
			int c;
			T[] _tmp = GetArray(out c);
			if (c == _tmp.Length)
				return ((IEnumerable<T>)_tmp).GetEnumerator();
			return _tmp.Take(c).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsReadOnly { get { return false; } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// IList
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (T)value; }
		}

		public int IndexOf(object item)
		{
			return item is T ? IndexOf((T)item) : -1;
		}

		public bool Contains(object item)
		{
			return item is T && Contains((T)item);
		}

		public int Add(object item)
		{
			return _add((T)item);
		}

		public void Insert(int index, object item)
		{
			Insert(index, (T)item);
		}

		public void Remove(object item)
		{
			if (item is T)
				Remove((T)item);
		}

		public void CopyTo(Array array, int index)
		{
			Monitor.Enter(this);
			if (array != null && array.Rank != 1)
				throw new ArgumentException();
			Array.Copy(m_arr, 0, array, index, m_c);
			Monitor.Exit(this);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsFixedSize { get { return false; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsSynchronized { get { return true; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public object SyncRoot { get { return this; } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		/// Debug
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		T[] _dbg_display { get { return this.ToArray(); } }

		Type _item_type() { return typeof(T); }
	};

#if UNIT_TEST
void test_reflist()
	{
		rlc = 0;
		rl = new RefList<String>();
		mre = new ManualResetEvent(true);

		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);
		Task.Factory.StartNew(test_action);

		sync_action();
	}
	RefList<String> rl;
	int rlc;
	ManualResetEvent mre;

	void test_action()
	{
		Random r = new Random();
		List<String> mine = new List<string>();
		while (true)
		{
			String s;
			switch (r.Next(9))
			{
				case 0:
				case 1:
				case 2:
				case 3:
					if (rl.Count < 2000)
					{
						s = Guid.NewGuid().ToString();
						mine.Add(s);
						rl.Add(s);
						Interlocked.Increment(ref rlc);
					}
					break;
				case 5:
				case 6:
				case 7:
				case 8:
					if (mine.Count > 0)
					{
						int ix = r.Next(mine.Count);
						s = mine[ix];
						mine.RemoveAt(ix);
						if (!rl.Remove(s))
							throw new Exception();
						Interlocked.Decrement(ref rlc);
					}
					break;
			}
			//if (rl.Count == 0)
			//    Console.Write("!");
			//Console.Write("{0,4} ", rl.Count);
			mre.WaitOne();
		}
	}
	void sync_action()
	{
		while (true)
		{
			Thread.Sleep(4000);
			mre.Reset();
			Thread.Sleep(200);
			Console.WriteLine();
			if (rl.Count != rlc)
				throw new Exception();
			Console.WriteLine("{0} {1}", rl.Count, rlc);
			mre.Set();
		}
	}
#endif

	//public class synclist<T>
	//    where T : class
	//{
	//    blk m_first;
	//    int m_c;

	//    public class blk
	//    {
	//        public blk()
	//        {
	//            this.arr = new T[32];
	//        }
	//        T[] arr;
	//        blk next;
	//    };

	//};
}