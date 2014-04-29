using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace alib.Concurrency
{
	public interface ITask<out T>
	{
		T Result { get; }
		bool IsCompleted { get; }
	}

#if ASYNC_TASK
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class AsyncTask<T> : TaskCompletionSource<T>, IAsyncResult
	{
		IAsyncResult ar { get { return (IAsyncResult)base.Task; } }

		Object IAsyncResult.AsyncState { get { return ar.AsyncState; } }
		WaitHandle IAsyncResult.AsyncWaitHandle { get { return ar.AsyncWaitHandle; } }
		bool IAsyncResult.CompletedSynchronously { get { return ar.CompletedSynchronously; } }
		public bool IsCompleted { get { return ar.IsCompleted; } }
		public void Wait() { base.Task.Wait(); }
	};
#endif


	public static class Tasks<K>
		where K : class
	{
		static Tasks()
		{
			var tcs = new TaskCompletionSource<K>();
			tcs.SetResult(null);
			CompletedNullResult = tcs.Task;
		}
		public static readonly Task<K> CompletedNullResult;
	}
	public static class Tasks
	{
		static Tasks()
		{
			CompletedTask = Tasks<Object>.CompletedNullResult;
			Completed = ((IAsyncResult)CompletedTask).AsyncWaitHandle;
			Signaled = new ManualResetEvent(true);
			Unclaimed = new ManualResetEvent(false);
		}

		public static readonly Task CompletedTask;

		public static readonly WaitHandle Completed;

		public static readonly ManualResetEvent Signaled;

		public static readonly ManualResetEvent Unclaimed;

		public static Task<T> FromError<T>(Exception ex)
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.AttachedToParent);
			tcs.SetException(ex);
			return tcs.Task;
		}

		public static Task<T> FromResult<T>(T result)
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.AttachedToParent);
			tcs.SetResult(result);
			return tcs.Task;
		}

		/// <summary>
		/// Caution: elements in the enumeration might be iterated more than once; beware of side-effects
		/// </summary>
		//public static Task WhenAllXX(IEnumerable<Task> iet)
		//{
		//    var c = iet as ICollection<Task>;
		//    if ((c != null && c.Count > 0) || iet.GetEnumerator().MoveNext())
		//        return TaskEx.WhenAll(iet);
		//    return CompletedTask;
		//}

		//public static void RgtaskNop(Task[] rgt) { }

		//public static Task Self
		//{
		//	get
		//	{
		//		return Task.Factory.StartNew(Nop, CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.Default).Parent();
		//	}
		//}
		//static void Nop() { }
	};

	public class TimedTask<T> : Task<T>
	{
		public TimedTask(Func<T> f)
			: base(f)
		{
		}
		public long Milliseconds { get { return ms; } }
		protected long ms;
		public new void Start()
		{
			Stopwatch sw = Stopwatch.StartNew();
			base.Start();
			base.ContinueWith(t => ms = sw.ElapsedMilliseconds, TaskContinuationOptions.ExecuteSynchronously);
		}
	};

	public static class _tasks_ext
	{
		public static Task Parent(this Task t)
		{
			FieldInfo info = typeof(Task).GetField("m_parent", BindingFlags.NonPublic | BindingFlags.Instance);
			return info != null ? (Task)info.GetValue(t) : null;
		}

		public static bool HasAsAncestor(this Task t, Task parent)
		{
			Task walk = t;
			while ((walk = walk.Parent()) != null)
				if (walk == t)
					return true;
			return false;
		}
	};
}