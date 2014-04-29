using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace alib.Int
{
	using Math = System.Math;
	using String = System.String;

	public static class _int_ext
	{
		public const long Megabyte = (long)1 << 20;
		public const long Gigabyte = (long)1 << 30;
		public const Double MegabyteD = Megabyte;
		public const Double GigabyteD = Gigabyte;

		public static bool IsOdd(this int i)
		{
			return (i & 1) == 1;
		}

		public static bool IsEven(this int i)
		{
			return (i & 1) == 0;
		}

		public static int Base10DigitCount(this int i)
		{
			if (i == 0)
				return 1;
			return (int)Math.Log10(i) + 1;
		}

		public static unsafe int Atoi(this string s)
		{
			if (String.IsNullOrWhiteSpace(s))
				return 0;
			int i = 0;
			fixed (Char* pin = s)
			{
				Char* p = pin;
				while ('0' <= *p && *p <= '9')
				{
					i *= 10;
					i += *p++ - '0';
				}
				return i;
			}
		}
		public static unsafe uint Atoui(this string s)
		{
			if (String.IsNullOrWhiteSpace(s))
				return 0;
			uint i = 0;
			fixed (Char* pin = s)
			{
				Char* p = pin;
				while ('0' <= *p && *p <= '9')
				{
					i *= 10;
					i += (uint)(*p++ - '0');
				}
				return i;
			}
		}
	};
}

namespace alib.Math
{
	using alib.Collections;
	using Math = System.Math;
	using String = System.String;

	public static class math
	{
		public const Double _ε = -2.2204460492503131E-16;
		public const Double ε = 2.2204460492503131E-16;
		public const Double PiRad = 180 / Math.PI;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Double Maximize(ref Double x, Double y)
		{
			if (y > x)
				x = y;
			return x;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Maximize(ref int x, int y)
		{
			if (y > x)
				x = y;
			return x;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Double Minimize(ref Double x, Double y)
		{
			if (y < x)
				x = y;
			return x;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Minimize(ref int x, int y)
		{
			if (y < x)
				x = y;
			return x;
		}

		public static Double Min(params Double[] arr)
		{
			if (arr.Length == 0)
				throw new Exception();
			Double min = arr[0];
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] < min)
					min = arr[i];
			return min;
		}
		public static Double Max(params Double[] arr)
		{
			if (arr.Length == 0)
				throw new Exception();
			Double max = arr[0];
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] > max)
					max = arr[i];
			return max;
		}
		public static Double Maximize(ref Double max, params Double[] arr)
		{
			if (arr.Length == 0)
				throw new Exception();
			max = arr[0];
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] > max)
					max = arr[i];
			return max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero(this Double value)
		{
			return _ε < value && value < ε;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFinite(this Double d)
		{
			return !Double.IsNaN(d) && !Double.IsInfinity(d);
		}

		readonly static Double[] pow10 = { 1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10 };
		public static Double Truncate(this Double x, int digits)
		{
			if (digits < 0)
				throw new ArgumentException();
			if (digits == 0)
				return Math.Truncate(x);
			Double m = digits >= pow10.Length ? Math.Pow(10, digits) : pow10[digits];
			return Math.Truncate(x * m) / m;
		}

		public static Double TruncatePrecision(this Double x, int precision)
		{
			if (precision < 0)
				throw new ArgumentException();
			Double z = Math.Truncate(x);
			if (z != 0)
				precision -= (int)Math.Log10(Math.Abs(z)) + 1;
			if (precision == 0)
				return z;
			Double m = Math.Pow(10, precision);
			return Math.Truncate(x * m) / m;
		}

		public static Double RoundPrecision(this Double x, int precision)
		{
			if (precision < 0)
				throw new ArgumentException();
			Double z = Math.Round(x);
			if (z != 0)
				precision -= (int)Math.Log10(Math.Abs(z)) + 1;
			if (precision == 0)
				return z;
			Double m = Math.Pow(10, precision);
			return Math.Round(x * m) / m;
		}

		public static int GreatestCommonDivisor(this IEnumerable<int> x)
		{
			IEnumerator<int> z = x.GetEnumerator();
			if (!z.MoveNext())
				throw new ArgumentException("The sequence is empty");
			int tmp = z.Current;
			while (z.MoveNext())
				tmp = gcd(tmp, z.Current);
			return tmp;
		}

		public static int LeastCommonMultiple(this IEnumerable<int> x)
		{
			IEnumerator<int> z = x.GetEnumerator();
			if (!z.MoveNext())
				throw new ArgumentException("The sequence is empty");
			int tmp = z.Current;
			while (z.MoveNext())
				tmp = lcm(tmp, z.Current);
			return tmp;
		}

		public static int Product<T>(this IEnumerable<T> seq, Func<T, int> fn)
		{
			IEnumerator<T> z = seq.GetEnumerator();
			if (!z.MoveNext())
				throw new ArgumentException("The sequence is empty");
			int tmp = fn(z.Current);
			while (z.MoveNext())
				tmp *= fn(z.Current);
			return tmp;
		}

		/// <summary>
		/// Arranges the integers in 'arr' so that no integer below the supplied index position 'k' is 
		/// greater than any integer above index 'k'
		/// </summary>
		public static unsafe void SelectMedian(int[] arr, int k, IComparer<int> cmp)
		{
			fixed (int* a = &arr[0])
				select_median(a, arr.Length, k, cmp.Compare);
		}

		/// <summary>
		/// Arranges the integers in 'arr' so that no integer below the supplied index position 'k' is 
		/// greater than any integer above index 'k'
		/// </summary>
		public static unsafe void SelectMedian(int[] arr, int k)
		{
			fixed (int* a = &arr[0])
				select_median(a, arr.Length, k, null);
		}
		static unsafe void select_median(int* a, int c, int k, Func<int, int, int> cmp)
		{
			int* pi = a, pj, pL = a, pR;
			pj = pR = a + (c - 1);
			a += k;
			while (pL < pR)
			{
				k = *a;
				do
				{
					if (cmp == null)
					{
						while (*pi < k)
							pi++;
						while (k < *pj)
							pj--;
					}
					else
					{
						while (cmp(*pi, k) < 0)
							pi++;
						while (cmp(k, *pj) < 0)
							pj--;
					}
					if (pi > pj)
						break;

					if (pi < pj)
						swap(pi, pj);

					pi++;
					pj--;
				}
				while (pi <= pj);

				if (pj < a)
				{
					pL = pi;
					pj = pR;
				}
				else
				{
					pR = pj;
					pi = pL;
				}
			}
		}
		public static unsafe void SelectMedian<T>(int[] arr, int k, T context, Func<T, int, int, int> cmp)
		{
			fixed (int* a = &arr[0])
				select_median(a, arr.Length, k, context, cmp);
		}
		static unsafe void select_median<T>(int* a, int c, int k, T context, Func<T, int, int, int> cmp)
		{
			int* pi = a, pj, pL = a, pR;
			pj = pR = a + (c - 1);
			a += k;
			while (pL < pR)
			{
				k = *a;
				do
				{
					if (cmp == null)
					{
						while (*pi < k)
							pi++;
						while (k < *pj)
							pj--;
					}
					else
					{
						while (cmp(context, *pi, k) < 0)
							pi++;
						while (cmp(context, k, *pj) < 0)
							pj--;
					}
					if (pi > pj)
						break;

					if (pi < pj)
						swap(pi, pj);

					pi++;
					pj--;
				}
				while (pi <= pj);

				if (pj < a)
				{
					pL = pi;
					pj = pR;
				}
				else
				{
					pR = pj;
					pi = pL;
				}
			}
		}
#if false
		public static unsafe int SelectMedians(int[] arr, IComparer<int> cmp)
		{
			int c = arr.Length;
			fixed (int* a = &arr[0])
				//if ((c & 1) != 0 || c == 2)
				//	select_median(a, c, c >>= 1, cmp.Compare);
				//else
				c = select_medians(a, c, cmp.Compare);
			return c;
		}
		public static unsafe int SelectMedians(int[] arr)
		{
			int c = arr.Length;
			fixed (int* a = &arr[0])
				//if ((c & 1) != 0 || c == 2)
				//	select_median(a, c, c >>= 1, null);
				//else
				c = select_medians(a, c, null);
			return c;
		}


		static unsafe int select_medians(int* a, int c, Func<int, int, int> cmp)
		{
			if (cmp != null)
				throw new Exception();
			int* pi, pj, pL, pR;
			pj = pR = (pi = pL = a) + (c - 1);
			c >>= 1;
			a += c;
			while (pL < pR)
			{
				int x = *a;
				do
				{
					while (*pi < x)
						pi++;
					while (x < *pj)
						pj--;

					if (pi > pj)
						break;

					if (pi < pj)
						swap(pi, pj);

					pi++;
					pj--;
				}
				while (pi < pj);

				if (pj < a)
				{
					pL = pi;
					pj = pR;
				}
				else
				{
					pR = pj;
					pi = pL;
				}
			}
			return c;
		}
#endif
		static unsafe void swap(int* p1, int* p2)
		{
			int t = *p1;
			*p1 = *p2;
			*p2 = t;
		}

		static int gcd(int a, int b)
		{
			int t;
			while (b != 0)
			{
				t = b;
				b = a % b;
				a = t;
			}
			return a;
		}

		static int lcm(int a, int b)
		{
			int temp = gcd(a, b);
			return temp == 0 ? 0 : b > a ? (b / temp) * a : (a / temp) * b;
		}

		public static Double StandardDeviation(this IEnumerable<Double> rg)
		{
			return Math.Sqrt(Variance(rg));
		}

		public static Double StandardDeviation<T>(this IEnumerable<T> rg, Converter<T, Double> fn)
		{
			int n = 0;
			Double mx = 0, mean = 0;
			foreach (T t in rg)
			{
				Double d = fn(t);
				n++;
				Double delta = d - mean;
				mean += delta / n;
				mx += delta * (d - mean);
			}
			if (n <= 1)
				return Double.NaN;
			return Math.Sqrt(mx / (n - 1));
		}

		public static Double Variance(this IEnumerable<Double> rg)
		{
			int n = 0;
			Double mx = 0, mean = 0;
			foreach (Double d in rg)
			{
				n++;
				Double delta = d - mean;
				mean += delta / n;
				mx += delta * (d - mean);
			}
			if (n <= 1)
				return Double.NaN;
			//variance_n = M2 / n;
			return mx / (n - 1);
		}

		/// <summary> Returns the entropy of the strings </summary>
		public static Double Entropy(this IEnumerable<String> ie)
		{
			int c = alib.Enumerable._enumerable_ext._Count(ie);
			return -ie.GroupBy(_identity<String>.func).Sum(e =>
			{
				var p = (Double)e.Count() / c;
				return p * Math.Log(p, 2.0);
			});
		}

#if false
		public static Double[] GatherUnityMass(this IEnumerable<Double> rg)
		{
			var max = Double.MinValue, min = Double.MaxValue, incr;
			int c = 0;
			foreach (Double d in rg)
			{
				if (d > max) max = d;
				if (d < min) min = d;
				c++;
			}
			if (c <= 1)
				return new[] { 1.0 };

			if ((incr = (max - min) / c) == 0)
				return Enumerable.Repeat(max / c, c).ToArray();

			var rv = new Double[c];
			//if (f_mono)
			//{
			//    foreach (var grp in rg.MyGroupBy(e => (int)((max - e) / incr)))
			//        rv[Math.Min(grp.Key, c - 1)] = grp.Count;
			//    return rv;
			//}

			foreach (var grp in rg.GroupBy(e => (int)((max - e) / incr)))
				rv[Math.Min(grp.Key, c - 1)] = grp.Count();
			return rv;
		}
#endif
		public static readonly ulong[] factorials = 
		{
			1, 
			1, 
			2, 
			6, 
			24, 
			120, 
			720, 
			5040, 
			40320,
			362880,
			3628800, 
			39916800,
			479001600,
			6227020800,
			87178291200, 
			1307674368000,
			20922789888000,
			355687428096000, 
			6402373705728000, 
			121645100408832000,
			2432902008176640000,
		};

		public static ulong BinomialCoefficient(uint n, uint k)
		{
			if (k > n - k)
				k = n - k;

			ulong c = 1;
			for (uint i = 0; i < k; i++)
			{
				c = c * (n - i);
				c = c / (i + 1);
			}
			return c;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class primes
	{
		static primes()
		{
			SievePrimes(out cache);
		}

		static readonly public ushort[] cache;

		static unsafe void SievePrimes(out ushort[] primes)
		{
			bool* sieve = stackalloc bool[65536];
			int j, c_nonprime = -2 /* 0, 1 */ + 65536;

			for (j = 2; j <= 256; j++)
				if (!sieve[j])
					for (int nonPrime = 2 * j; nonPrime < 65536; nonPrime += j)
						if (!sieve[nonPrime])
						{
							sieve[nonPrime] = true;
							c_nonprime--;
						}

			primes = new ushort[c_nonprime];
			j = 0;
			for (ushort i = 2; j < c_nonprime; i++)
				if (!sieve[i])
					primes[j++] = i;
		}

		public static List<ushort> Factor(uint num)
		{
			int ix = 0;
			ushort prime = cache[ix];
			var factors = new List<ushort>();
			while (num > 1)
			{
				if (num % prime == 0)
				{
					factors.Add(prime);
					num /= prime;
				}
				else
				{
					ix++;
					prime = cache[ix];
				}
			}
			return factors;
		}

		public static IReadOnlyList<uint> MultiplyPrimeFactors(IEnumerable<uint> lhs, IEnumerable<uint> rhs)
		{
			var product = new RefList<uint>();
			foreach (var prime in lhs)
				product.Add(prime);

			foreach (var prime in rhs)
				product.Add(prime);

			product.Sort();
			return product;
		}

		public static IReadOnlyList<uint> DividePrimeFactors(IEnumerable<ushort> numerator, IEnumerable<ushort> denominator)
		{
			var product = new RefList<uint>();
			foreach (var prime in numerator)
				product.Add(prime);

			foreach (var prime in denominator)
				product.Remove(prime);

			return product;
		}

		public static ulong EvaluatePrimeFactors(IEnumerable<uint> value)
		{
			ulong accumulator = 1;
			foreach (var prime in value)
			{
				ulong tmp = accumulator * prime;
				if (tmp < accumulator)
					throw new OverflowException();
				accumulator = tmp;
			}
			return accumulator;
		}

		public static int HashFriendly(int c)
		{
			int i;
			if (c <= 65521)
			{
				if ((i = System.Array.BinarySearch<ushort>(cache, (ushort)c)) < 0)
					i = ~i;
				return cache[i];
			}
			int stop = (int)Math.Sqrt(c |= 1);
			i = 3;
			do
				if (c % i != 0 || c > 0x3fffffff)
					i += 2;
				else
				{
					stop = (int)Math.Sqrt(c += 2);
					i = 3;
				}
			while (i <= stop);
			return c;
		}
	};
}
