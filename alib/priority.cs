using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace alib.priority
{
	using String = System.String;

	public class PriorityQueue<T> : IEnumerable<T>
	{
		public PriorityQueue(IComparer<T> comparer = null, int capacity = -1)
		{
			if (capacity <= 16)
				capacity = 16;
			if (comparer == null)
				comparer = Comparer<T>.Default;
			this.comparer = comparer;
			int length = 1;
			while (length < capacity)
				length <<= 1;
			heap = new Interval[length];
		}
		public PriorityQueue(int capacity)
			: this(null, -1)
		{
		}

		struct Interval
		{
			internal T first, last;
			internal int firsthandle, lasthandle;

			public override String ToString()
			{
				return System.String.Format("[{0}; {1}]", first, last);
			}
		};

		Interval[] heap;

		int size;
		public int Count { get { return size; } }

		IComparer<T> comparer;
		public IComparer<T> Comparer { get { return comparer; } }


		bool heapifyMin(int i)
		{
			bool swappedroot = false;
			int cell = i, currentmin = cell;
			T currentitem = heap[cell].first;
			int currenthandle = heap[cell].firsthandle;

			// bug20080222.txt
			{
				T other = heap[cell].last;
				if (2 * cell + 1 < size && comparer.Compare(currentitem, other) > 0)
				{
					swappedroot = true;
					int otherhandle = heap[cell].lasthandle;
					updateLast(cell, currentitem, ref currenthandle);
					currentitem = other;
					currenthandle = otherhandle;
				}
			}

			T minitem = currentitem;
			int minhandle = currenthandle;

			while (true)
			{
				int l = 2 * cell + 1, r = l + 1;
				T lv, rv;

				if (2 * l < size && comparer.Compare(lv = heap[l].first, minitem) < 0)
				{ currentmin = l; minitem = lv; }

				if (2 * r < size && comparer.Compare(rv = heap[r].first, minitem) < 0)
				{ currentmin = r; minitem = rv; }

				if (currentmin == cell)
					break;

				minhandle = heap[currentmin].firsthandle;
				updateFirst(cell, minitem, ref  minhandle);
				cell = currentmin;

				//Maybe swap first and last
				T other = heap[cell].last;
				if (2 * currentmin + 1 < size && comparer.Compare(currentitem, other) > 0)
				{
					int otherhandle = heap[cell].lasthandle;
					updateLast(cell, currentitem, ref  currenthandle);
					currentitem = other;
					currenthandle = otherhandle;
				}

				minitem = currentitem;
				minhandle = currenthandle;
			}

			if (cell != i || swappedroot)
				updateFirst(cell, minitem, ref  minhandle);
			return swappedroot;
		}


		bool heapifyMax(int i)
		{
			bool swappedroot = false;
			int cell = i, currentmax = cell;
			T currentitem = heap[cell].last;
			int currenthandle = heap[cell].lasthandle;

			// bug20080222.txt
			{
				T other = heap[cell].first;
				if (2 * cell + 1 < size && comparer.Compare(currentitem, other) < 0)
				{
					swappedroot = true;
					int otherhandle = heap[cell].firsthandle;
					updateFirst(cell, currentitem, ref currenthandle);
					currentitem = other;
					currenthandle = otherhandle;
				}
			}

			T maxitem = currentitem;
			int maxhandle = currenthandle;

			while (true)
			{
				int l = 2 * cell + 1, r = l + 1;
				T lv, rv;

				if (2 * l + 1 < size && comparer.Compare(lv = heap[l].last, maxitem) > 0)
				{ currentmax = l; maxitem = lv; }

				if (2 * r + 1 < size && comparer.Compare(rv = heap[r].last, maxitem) > 0)
				{ currentmax = r; maxitem = rv; }

				if (currentmax == cell)
					break;

				maxhandle = heap[currentmax].lasthandle;
				updateLast(cell, maxitem, ref  maxhandle);
				cell = currentmax;

				//Maybe swap first and last
				T other = heap[cell].first;
				if (comparer.Compare(currentitem, other) < 0)
				{
					int otherhandle = heap[cell].firsthandle;
					updateFirst(cell, currentitem, ref  currenthandle);
					currentitem = other;
					currenthandle = otherhandle;
				}

				maxitem = currentitem;
				maxhandle = currenthandle;
			}

			if (cell != i || swappedroot) //Check could be better?
				updateLast(cell, maxitem, ref  maxhandle);
			return swappedroot;
		}

		void bubbleUpMin(int i)
		{
			if (i > 0)
			{
				T min = heap[i].first, iv = min;
				int minhandle = heap[i].firsthandle;
				int p = (i + 1) / 2 - 1;

				while (i > 0)
				{
					if (comparer.Compare(iv, min = heap[p = (i + 1) / 2 - 1].first) < 0)
					{
						updateFirst(i, min, ref  heap[p].firsthandle);
						min = iv;
						i = p;
					}
					else
						break;
				}

				updateFirst(i, iv, ref minhandle);
			}
		}

		void bubbleUpMax(int i)
		{
			if (i > 0)
			{
				T max = heap[i].last, iv = max;
				int maxhandle = heap[i].lasthandle;
				int p = (i + 1) / 2 - 1;

				while (i > 0)
				{
					if (comparer.Compare(iv, max = heap[p = (i + 1) / 2 - 1].last) > 0)
					{
						updateLast(i, max, ref  heap[p].lasthandle);
						max = iv;
						i = p;
					}
					else
						break;
				}
				updateLast(i, iv, ref maxhandle);
			}
		}

		bool add(out int itemhandle, T item)
		{
			itemhandle = -1;
			if (size == 0)
			{
				size = 1;
				updateFirst(0, item, ref itemhandle);
				return true;
			}

			if (size == 2 * heap.Length)
			{
				Interval[] newheap = new Interval[2 * heap.Length];

				System.Array.Copy(heap, newheap, heap.Length);
				heap = newheap;
			}

			if (size % 2 == 0)
			{
				int i = size / 2, p = (i + 1) / 2 - 1;
				T tmp = heap[p].last;

				if (comparer.Compare(item, tmp) > 0)
				{
					updateFirst(i, tmp, ref heap[p].lasthandle);
					updateLast(p, item, ref  itemhandle);
					bubbleUpMax(p);
				}
				else
				{
					updateFirst(i, item, ref itemhandle);

					if (comparer.Compare(item, heap[p].first) < 0)
						bubbleUpMin(i);
				}
			}
			else
			{
				int i = size / 2;
				T other = heap[i].first;

				if (comparer.Compare(item, other) < 0)
				{
					updateLast(i, other, ref  heap[i].firsthandle);
					updateFirst(i, item, ref itemhandle);
					bubbleUpMin(i);
				}
				else
				{
					updateLast(i, item, ref  itemhandle);
					bubbleUpMax(i);
				}
			}
			size++;

			return true;
		}

		void updateLast(int cell, T item, ref int handle)
		{
			heap[cell].last = item;
			if (handle != -1)
				handle = 2 * cell + 1;
			heap[cell].lasthandle = handle;
		}

		void updateFirst(int cell, T item, ref int handle)
		{
			heap[cell].first = item;
			if (handle != -1)
				handle = 2 * cell;
			heap[cell].firsthandle = handle;
		}


		public T this[int handle]
		{
			get
			{
				bool isfirst;
				int cell = checkHandle(handle, out isfirst);

				return isfirst ? heap[cell].first : heap[cell].last;
			}
			set
			{
				Replace(handle, value);
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < size; i++)
				yield return i % 2 == 0 ? heap[i >> 1].first : heap[i >> 1].last;
		}

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public bool Add(out int handle, T item)
		{
			return add(out handle, item);
		}
		public bool Add(T item)
		{
			int _;
			return add(out _, item);
		}
		void AddRange(IEnumerable<T> items)
		{
			int oldsize = size;
			int _;
			foreach (T item in items)
				add(out _, item);
		}

		public T Replace(int handle, T item)
		{
			bool isfirst;
			int cell = checkHandle(handle, out isfirst);
			if (size == 0)
				throw new Exception();

			T retval;

			if (isfirst)
			{
				retval = heap[cell].first;
				heap[cell].first = item;
				if (size == 1)
				{
				}
				else if (size == 2 * cell + 1) // cell == lastcell
				{
					int p = (cell + 1) / 2 - 1;
					if (comparer.Compare(item, heap[p].last) > 0)
					{
						int thehandle = heap[cell].firsthandle;
						updateFirst(cell, heap[p].last, ref heap[p].lasthandle);
						updateLast(p, item, ref  thehandle);
						bubbleUpMax(p);
					}
					else
						bubbleUpMin(cell);
				}
				else if (heapifyMin(cell))
					bubbleUpMax(cell);
				else
					bubbleUpMin(cell);
			}
			else
			{
				retval = heap[cell].last;
				heap[cell].last = item;
				if (heapifyMax(cell))
					bubbleUpMin(cell);
				else
					bubbleUpMax(cell);
			}

			return retval;
		}

		public bool Find(int handle, out T item)
		{
			if (handle == -1)
			{
				item = default(T);
				return false;
			}
			int toremove = handle;
			int cell = toremove / 2;
			bool isfirst = toremove % 2 == 0;
			{
				if (toremove == -1 || toremove >= size)
				{
					item = default(T);
					return false;
				}
				int actualhandle = isfirst ? heap[cell].firsthandle : heap[cell].lasthandle;
				if (actualhandle != handle)
				{
					item = default(T);
					return false;
				}
			}
			item = isfirst ? heap[cell].first : heap[cell].last;
			return true;
		}

		public T FindMin(out int handle)
		{
			if (size == 0)
				throw new Exception();
			handle = heap[0].firsthandle;

			return heap[0].first;
		}
		public T FindMin()
		{
			if (size == 0)
				throw new Exception();

			return heap[0].first;
		}

		public T FindMax(out int handle)
		{
			if (size == 0)
				throw new Exception();
			else if (size == 1)
			{
				handle = heap[0].firsthandle;
				return heap[0].first;
			}
			else
			{
				handle = heap[0].lasthandle;
				return heap[0].last;
			}
		}
		public T FindMax()
		{
			if (size == 0)
				throw new Exception("Heap is empty");
			else if (size == 1)
				return heap[0].first;
			else
				return heap[0].last;
		}

		public T Remove(int handle)
		{
			bool isfirst;
			int cell = checkHandle(handle, out isfirst);

			T retval;
			int lastcell = (size - 1) / 2;

			if (cell == lastcell)
			{
				if (isfirst)
				{
					retval = heap[cell].first;
					if (size % 2 == 0)
					{
						updateFirst(cell, heap[cell].last, ref heap[cell].lasthandle);
						heap[cell].last = default(T);
						heap[cell].lasthandle = -1;
					}
					else
					{
						heap[cell].first = default(T);
						heap[cell].firsthandle = -1;
					}
				}
				else
				{
					retval = heap[cell].last;
					heap[cell].last = default(T);
					heap[cell].lasthandle = -1;
				}
				size--;
			}
			else if (isfirst)
			{
				retval = heap[cell].first;

				if (size % 2 == 0)
				{
					updateFirst(cell, heap[lastcell].last, ref  heap[lastcell].lasthandle);
					heap[lastcell].last = default(T);
					heap[lastcell].lasthandle = -1;
				}
				else
				{
					updateFirst(cell, heap[lastcell].first, ref  heap[lastcell].firsthandle);
					heap[lastcell].first = default(T);
					heap[lastcell].firsthandle = -1;
				}

				size--;
				if (heapifyMin(cell))
					bubbleUpMax(cell);
				else
					bubbleUpMin(cell);
			}
			else
			{
				retval = heap[cell].last;

				if (size % 2 == 0)
				{
					updateLast(cell, heap[lastcell].last, ref heap[lastcell].lasthandle);
					heap[lastcell].last = default(T);
					heap[lastcell].lasthandle = -1;
				}
				else
				{
					updateLast(cell, heap[lastcell].first, ref  heap[lastcell].firsthandle);
					heap[lastcell].first = default(T);
					heap[lastcell].firsthandle = -1;
				}

				size--;
				if (heapifyMax(cell))
					bubbleUpMin(cell);
				else
					bubbleUpMax(cell);
			}
			return retval;
		}

		public T RemoveMin()
		{
			int handle;
			return RemoveMin(out handle);
		}

		public T RemoveMin(out int handle)
		{
			if (size == 0)
				throw new Exception();

			T retval = heap[0].first;
			int myhandle = heap[0].firsthandle;
			handle = myhandle;
			if (myhandle != -1)
				myhandle = -1;

			if (size == 1)
			{
				size = 0;
				heap[0].first = default(T);
				heap[0].firsthandle = -1;
			}
			else
			{
				int lastcell = (size - 1) / 2;

				if (size % 2 == 0)
				{
					updateFirst(0, heap[lastcell].last, ref  heap[lastcell].lasthandle);
					heap[lastcell].last = default(T);
					heap[lastcell].lasthandle = -1;
				}
				else
				{
					updateFirst(0, heap[lastcell].first, ref heap[lastcell].firsthandle);
					heap[lastcell].first = default(T);
					heap[lastcell].firsthandle = -1;
				}

				size--;
				heapifyMin(0);
			}

			return retval;

		}

		public T RemoveMax()
		{
			int handle;
			return RemoveMax(out handle);
		}

		public T RemoveMax(out int handle)
		{
			if (size == 0)
				throw new Exception();

			T retval;
			int myhandle;

			if (size == 1)
			{
				size = 0;
				retval = heap[0].first;
				myhandle = heap[0].firsthandle;
				if (myhandle != -1)
					myhandle = -1;
				heap[0].first = default(T);
				heap[0].firsthandle = -1;
			}
			else
			{
				retval = heap[0].last;
				myhandle = heap[0].lasthandle;
				if (myhandle != -1)
					myhandle = -1;

				int lastcell = (size - 1) / 2;

				if (size % 2 == 0)
				{
					updateLast(0, heap[lastcell].last, ref  heap[lastcell].lasthandle);
					heap[lastcell].last = default(T);
					heap[lastcell].lasthandle = -1;
				}
				else
				{
					updateLast(0, heap[lastcell].first, ref heap[lastcell].firsthandle);
					heap[lastcell].first = default(T);
					heap[lastcell].firsthandle = -1;
				}

				size--;
				heapifyMax(0);
			}
			handle = myhandle;
			return retval;
		}

		int checkHandle(int handle, out bool isfirst)
		{
			int cell = handle / 2;
			isfirst = handle % 2 == 0;

			if ((uint)handle >= (uint)size)
				throw new Exception("Invalid handle, index out of range");

			int actualhandle = isfirst ? heap[cell].firsthandle : heap[cell].lasthandle;
			if (actualhandle != handle)
				throw new Exception("Invalid handle, doesn't match queue");
			return cell;
		}

#if CHECK
		public bool Check()
		{
			if (size == 0)
				return true;

			if (size == 1)
				return (object)(heap[0].first) != null;

			return check(0, heap[0].first, heap[0].last);
		}
		bool check(int i, T min, T max)
		{
			bool retval = true;
			Interval interval = heap[i];
			T first = interval.first, last = interval.last;

			if (2 * i + 1 == size)
			{
				if (comparer.Compare(min, first) > 0)
				{
					Console.WriteLine("Cell {0}: parent.first({1}) > first({2})  [size={3}]", i, min, first, size);
					retval = false;
				}

				if (comparer.Compare(first, max) > 0)
				{
					Console.WriteLine("Cell {0}: first({1}) > parent.last({2})  [size={3}]", i, first, max, size);
					retval = false;
				}
				if (interval.firsthandle != -1 && interval.firsthandle != 2 * i)
				{
					Console.WriteLine("Cell {0}: firsthandle({1}) != 2*cell({2})  [size={3}]", i, interval.firsthandle, 2 * i, size);
					retval = false;
				}
				return retval;
			}
			else
			{
				if (comparer.Compare(min, first) > 0)
				{
					Console.WriteLine("Cell {0}: parent.first({1}) > first({2})  [size={3}]", i, min, first, size);
					retval = false;
				}

				if (comparer.Compare(first, last) > 0)
				{
					Console.WriteLine("Cell {0}: first({1}) > last({2})  [size={3}]", i, first, last, size);
					retval = false;
				}

				if (comparer.Compare(last, max) > 0)
				{
					Console.WriteLine("Cell {0}: last({1}) > parent.last({2})  [size={3}]", i, last, max, size);
					retval = false;
				}
				if (interval.firsthandle != -1 && interval.firsthandle != 2 * i)
				{
					Console.WriteLine("Cell {0}: firsthandle({1}) != 2*cell({2})  [size={3}]", i, interval.firsthandle, 2 * i, size);
					retval = false;
				}
				if (interval.lasthandle != -1 && interval.lasthandle != 2 * i + 1)
				{
					Console.WriteLine("Cell {0}: lasthandle({1}) != 2*cell+1({2})  [size={3}]", i, interval.lasthandle, 2 * i + 1, size);
					retval = false;
				}

				int l = 2 * i + 1, r = l + 1;

				if (2 * l < size)
					retval = retval && check(l, first, last);

				if (2 * r < size)
					retval = retval && check(r, first, last);
			}

			return retval;
		}
#endif
	};
}
