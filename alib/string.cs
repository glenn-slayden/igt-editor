using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
#if ! __MOBILE__
using System.Web;
using System.Security.Cryptography;
#endif

using alib.Enumerable;

namespace alib.String
{
	using Array = System.Array;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public struct RegexPair : IEquatable<RegexPair>
	{
		public System.Text.RegularExpressions.Regex regex;
		public String replace;

		public System.Text.RegularExpressions.Regex rev_regex;
		public String rev_replace;

		public override String ToString()
		{
			return String.Format("{0} {1} ↔ {2} {3}", regex, replace, rev_regex, rev_replace);
		}

		public override int GetHashCode()
		{
			return regex.GetHashCode() ^ replace.GetHashCode();
		}

		public bool Equals(RegexPair other)
		{
			return this.regex == other.regex && this.replace == other.replace;
		}

		public override bool Equals(Object obj)
		{
			return obj is RegexPair && Equals((RegexPair)obj);
		}

		public static bool operator ==(RegexPair x, RegexPair y) { return x.Equals(y); }
		public static bool operator !=(RegexPair x, RegexPair y) { return !x.Equals(y); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class ImmutableStringArray : IList<String>, IEquatable<ImmutableStringArray>
	{
		readonly String[] arr;
		readonly int hc = 0;
		readonly IEqualityComparer<String> comparer;

		public ImmutableStringArray(IEnumerable<String> rgs, IEqualityComparer<String> comparer)
		{
			this.arr = rgs as String[] ?? rgs.ToArray();
			this.comparer = comparer ?? StringComparer.Ordinal;

			hc = arr.Length;
			for (int i = 0; i < arr.Length; i++)
				hc ^= i + comparer.GetHashCode(arr[i]);
		}

		public String this[int index]
		{
			get { return arr[index]; }
			set { throw new InvalidOperationException(); }
		}

		public int IndexOf(String item)
		{
			for (int i = 0; i < arr.Length; i++)
				if (comparer.Equals(item, arr[i]))
					return i;
			return -1;
		}
		public bool Contains(String item) { return IndexOf(item) != -1; }
		public void CopyTo(String[] array, int arrayIndex) { Array.Copy(arr, array, arr.Length); }
		public int Count { get { return arr.Length; } }
		public bool IsReadOnly { get { return true; } }

		public IEnumerator<String> GetEnumerator() { return ((IEnumerable<String>)arr).GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return arr.GetEnumerator(); }
		public void Insert(int index, String item) { throw new InvalidOperationException(); }
		public void RemoveAt(int index) { throw new InvalidOperationException(); }
		public void Add(String item) { throw new InvalidOperationException(); }
		public void Clear() { throw new InvalidOperationException(); }
		public bool Remove(String item) { throw new InvalidOperationException(); }

		public bool Equals(ImmutableStringArray other)
		{
			return hc == other.hc && arr.SequenceEqual(other.arr, comparer);
		}

		public override bool Equals(Object obj)
		{
			ImmutableStringArray o = obj as ImmutableStringArray;
			return o != null && hc == o.hc && arr.SequenceEqual(o.arr, comparer);
		}

		public override int GetHashCode() { return hc; }

		public override String ToString()
		{
			return String.Join(" ", arr.Select(s => s.SQRB()));
		}
	};

	/// <summary>
	/// note: This will never intern a string, that is the responsibility of the user of this comparer
	/// </summary>
	public sealed class StringInternEqualityComparer : IEqualityComparer<String>
	{
		public const StringComparison Comparison = (StringComparison)99;

		public static readonly IEqualityComparer<String> Instance;

		static StringInternEqualityComparer()
		{
			Instance = new StringInternEqualityComparer();
		}

		StringInternEqualityComparer() { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(String x, String y)
		{
			return Object.ReferenceEquals(x, y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetHashCode(String s)
		{
			return RuntimeHelpers.GetHashCode(s);
		}
	};

	public static class _string_ext
	{
		public static readonly String[] nl_sep;

		static _string_ext()
		{
			nl_sep = new String[] { Environment.NewLine };
		}

		public static IEqualityComparer<String> GetComparer(this StringComparison sc)
		{
			switch (sc)
			{
				case StringComparison.CurrentCulture:
					return StringComparer.CurrentCulture;
				case StringComparison.CurrentCultureIgnoreCase:
					return StringComparer.CurrentCultureIgnoreCase;
				case StringComparison.InvariantCulture:
					return StringComparer.InvariantCulture;
				case StringComparison.InvariantCultureIgnoreCase:
					return StringComparer.InvariantCultureIgnoreCase;
				case StringComparison.Ordinal:
					return StringComparer.Ordinal;
				case StringComparison.OrdinalIgnoreCase:
					return StringComparer.OrdinalIgnoreCase;
				case StringInternEqualityComparer.Comparison:
					return StringInternEqualityComparer.Instance;
			}
			throw new Exception();
		}
		public static StringComparison GetComparison(this IEqualityComparer<String> cmp)
		{
			if (cmp == StringComparer.CurrentCulture)
				return StringComparison.CurrentCulture;
			if (cmp == StringComparer.CurrentCultureIgnoreCase)
				return StringComparison.CurrentCultureIgnoreCase;
			if (cmp == StringComparer.InvariantCulture)
				return StringComparison.InvariantCulture;
			if (cmp == StringComparer.InvariantCultureIgnoreCase)
				return StringComparison.InvariantCultureIgnoreCase;
			if (cmp == StringComparer.Ordinal)
				return StringComparison.Ordinal;
			if (cmp == StringComparer.OrdinalIgnoreCase)
				return StringComparison.OrdinalIgnoreCase;
			if (cmp == StringInternEqualityComparer.Instance)
				return StringInternEqualityComparer.Comparison;
			throw new Exception();
		}

		public static int _Length(this String s)
		{
			return StringInfo.ParseCombiningCharacters(s).Length;
		}

		public static void SetStringChar(ref String s, int ix, Char ch)
		{
			var arr = s.ToCharArray();
			arr[ix] = ch;
			s = new String(arr);
		}

		public static String ZeroStringChars(this String s, int index, int count)
		{
			if ((uint)(count += index) >= (uint)s.Length)
				count = s.Length;
			if ((uint)index < (uint)count)
			{
				var rgch = s.ToCharArray();
				do
					rgch[index] = '\0';
				while (++index < count);
				s = new String(rgch);
			}
			return s;
		}

		static String s_subs = "₀₁₂₃₄₅₆₇₈₉";
		public static String SubscriptNum(this int num)
		{
			String sn = num.ToString();
			int c = sn.Length;
			Char[] rgch = new Char[c];
			for (int i = 0; i < c; i++)
			{
				Char ch;
				switch (ch = sn[i])
				{
					case '-':
						rgch[i] = '₋';
						break;
					case '+':
						rgch[i] = '₊';
						break;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						rgch[i] = s_subs[ch - '0'];
						break;
					default:
						rgch[i] = ch;
						break;
				}
			}
			return new String(rgch);
		}

		static String s_sups = "⁰¹²³⁴⁵⁶⁷⁸⁹";
		public static String SuperscriptNum(int num)
		{
			String sn = num.ToString();
			int c = sn.Length;
			Char[] rgch = new Char[c];
			for (int i = 0; i < c; i++)
			{
				Char ch;
				switch (ch = sn[i])
				{
					case '-':
						rgch[i] = '⁻';
						break;
					case '+':
						rgch[i] = '⁺';
						break;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						rgch[i] = s_sups[ch - '0'];
						break;
					default:
						rgch[i] = ch;
						break;
				}
			}
			return new String(rgch);
		}

		public static String SmallExp(this String fmt_G)
		{
			int c = fmt_G.Length - 1;
			for (int i = 0; i < c; i++)
				if (fmt_G[i] == 'E')
					return fmt_G.Remove(i++) + SuperscriptNum(int.Parse(fmt_G.Substring(i)));
			return fmt_G;
		}

		public static String PadRightComb(this String s, int cch)
		{
			return s.PadRight(cch + s.Count(ch => ch == '\u035F'));
		}

		public static String NewString(this IEnumerable<Char> iech)
		{
			return new String(iech as Char[] ?? iech.ToArray());
		}

		public static String Quotes(this String s)
		{
			return "\"" + s + "\"";
		}

		public static String[] SplitNoEmpty(this String s, params Char[] sep)
		{
			return s.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		}

		public static String RightEllipses(this String s, int c)
		{
			if (s.Length < c)
				return s;
			return s.Remove(c - 3) + "...";
		}
		public static String LeftEllipses(this String s, int c)
		{
			if (s.Length < c)
				return s;
			return "..." + s.Substring(3);
		}

		public static IEnumerable<String> Lines(this String s)
		{
			var tr = new StringReader(s);
			String l;
			while ((l = tr.ReadLine()) != null)
				yield return l;
		}

		public static String NormalizeLinefeeds(this String s)
		{
			if (s.Length == 0)
				return String.Empty;
			int i;
			Char ch, ch_prv = default(Char);
			for (i = 0; i < s.Length; i++)
			{
				if ((ch = s[i]) == '\n' && ch_prv != '\r')
					goto full;
				ch_prv = ch;
			}
			return s;
		full:
			var sb = new StringBuilder(s);
			sb.Insert(i++, '\r');

			while (++i < sb.Length)
			{
				if ((ch = sb[i]) == '\n' && ch_prv != '\r')
					sb.Insert(i++, '\r');
				ch_prv = ch;
			}
			return sb.ToString();
		}

		public static String Indent(this String s, int c)
		{
			return Indent(s, new String(' ', c));
		}
		public static String Indent(this String s, String ind)
		{
			int i;
			for (i = 0; i < s.Length; i++)
				if (s[i] == '\r' || s[i] == '\n')
					goto do_it;
			return ind + s;
		do_it:
			var lx = s.Split(nl_sep, StringSplitOptions.RemoveEmptyEntries);

			var sb = new StringBuilder(s.Length + lx.Length * (ind.Length + 1));

			for (i = 0; i < lx.Length; i++)
			{
				sb.Append(ind);
				sb.AppendLine(lx[i]);
			}
			return sb.ToString();
		}

		public static int IndexOfEnd(this String s, String value)
		{
			int ix = s.IndexOf(value);
			if (ix != 1)
				ix += value.Length;
			return ix;
		}

		public static int GetByteCount(this Encoding enc, String s, int count)
		{
			if (s.Length < count)
				throw new Exception();
			if (count < s.Length)
				s = s.Substring(0, count);
			return enc.GetByteCount(s);
		}

		public static String RemoveMatchedParentheses(this String s, out bool f_all_matched)
		{
			f_all_matched = true;
			int nest = 0, i_start = 0;
			Char prev = default(Char);
			for (int i = 0; i < s.Length; i++)
			{
				Char ch = s[i];
				if (ch == '(' && prev != '\\')
				{
					if (nest == 0)
						i_start = i;
					nest++;
				}
				if (ch == ')' && prev != '\\')
				{
					nest--;
					if (nest < 0)
						break;
					if (nest == 0)
					{
						s = s.Remove(i, 1).Remove(i_start, 1);
						i = i_start;
					}
				}
				prev = ch;
			}
			f_all_matched = nest == 0;
			return s;
		}

		public static String ExtractParenthesized(this String s)
		{
			int j, k;
			if ((j = s.IndexOf('(')) >= 0)
				if ((k = s.IndexOf(')', j)) > 0)
					s = s.Substring(j + 1, k - j - 1);
			return s;
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String RemoveParenthesized(this String s)
		{
			int j, k = 0;
			while ((j = s.IndexOf('(', k)) >= 0)
			{
				if ((k = s.IndexOf(')', j)) == -1)
					break;
				s = s.Remove(j, k + 1 - j);
				k = j;
			}
			return s;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String RemoveBracketed(this String s)
		{
			int j, k = 0;
			while ((j = s.IndexOf('[', k)) >= 0)
			{
				if ((k = s.IndexOf(']', j)) == -1)
					break;
				s = s.Remove(j, k + 1 - j);
				k = j;
			}
			return s;
		}
		public static String ExtractFirstBracket(this String s)
		{
			int j, k;
			return (j = s.IndexOf('[')) != -1 && (k = s.IndexOf(']', ++j)) != -1 ?
				s = s.Substring(j, k - j) :
				null;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String RemoveAngleTagged(this String s)
		{
			int j, k = 0;
			while ((j = s.IndexOf('<', k)) >= 0)
			{
				if ((k = s.IndexOf('>', j)) == -1)
					break;
				s = s.Remove(j, k + 1 - j);
				k = j;
			}
			return s;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String ExtractAngleTagged(this String s)
		{
			int j, k;
			return (j = s.IndexOf('<')) >= 0 && (k = s.IndexOf('>', j)) > 0 ? s.Substring(j + 1, k - j - 1) : String.Empty;
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe int CopyToNativeW(this String s, Char* pbuf, int cwch_max)
		{
			int c = s.Length;
			if (c + 1 > cwch_max)
				return 0;
			fixed (Char* p = s)
				pbuf[Encoding.Unicode.GetBytes(p, c, (byte*)pbuf, cwch_max * sizeof(Char)) >> 1] = '\0';
			return c + 1;
		}

		/// <summary>
		/// Probably not for use with LISP
		/// </summary>
		public static int QuoteInsulatedIndexOf(this String s, Char ch_find)
		{
			Stack<Char> stk = new Stack<Char>();
			for (int i = 0; i < s.Length; i++)
			{
				Char ch = s[i];
				if ((ch == '\'' || ch == '\"') && (i < 1 || s[i - 1] != '\\' || i < 2 || s[i - 2] != '\\'))
				{
					if (stk.Count > 0 && ch == stk.Peek())
						stk.Pop();
					else
						stk.Push(ch);
				}
				else if (stk.Count == 0 && ch == ch_find)
					return i;
			}
			return -1;
		}

		public static String RemoveComments(this String s)
		{
			StringBuilder sb = new StringBuilder();
			using (StringReader sr = new StringReader(s))
			{
				String _l;
				while ((_l = sr.ReadLine()) != null)
				{
					String L = _l.Trim();
					if (L.Length == 0 || L[0] == ';')
						continue;
					int ix = QuoteInsulatedIndexOf(L, ';');
					if (ix != -1)
						sb.AppendLine(L.Substring(0, ix).TrimEnd());//yield return L.Substring(ix).TrimEnd();
					else
						sb.AppendLine(L);
				}
			}
			return sb.ToString();
		}

		public static String RemoveCommentsCondense(this String s)
		{
			String q = s.RemoveComments();
			Char[] src = q.ToCharArray();
			Char[] tgt = new Char[src.Length];
			bool f_spc = false;
			int j = 0;
			for (int i = 0; i < src.Length; i++)
			{
				Char ch = src[i];
				if (ch < 32)
					ch = ' ';
				if (ch == ' ')
				{
					if (f_spc)
						continue;
					f_spc = true;
				}
				else
					f_spc = false;
				tgt[j++] = ch;
			}
			return new String(tgt, 0, j);
		}

		public static String Replace(this String s, String match, String repl, StringComparison cmp)
		{
			int cm = match.Length;
			if (s.Length == 0 || cm == 0 || cm > s.Length || String.Compare(match, repl, cmp) == 0)
				return s;
			int ix = 0;
			while ((ix = s.IndexOf(match, ix, cmp)) != -1)
				s = s.Remove(ix, cm).Insert(ix, repl);
			return s;
		}

		public static String RemoveSuffix(this String s, String suffix, StringComparison sc = StringComparison.Ordinal)
		{
			if (s.EndsWith(suffix, sc))
				s = s.Remove(s.Length - suffix.Length);
			return s;
		}
		public static String RemovePrefix(this String s, String prefix, StringComparison sc = StringComparison.Ordinal)
		{
			if (s.StartsWith(prefix, sc))
				s = s.Substring(prefix.Length);
			return s;
		}

		/// <summary>
		/// if i_start > 0, this does not check the skipped character positions. And
		/// also, even in this case, the function returns the length from the 
		/// start of both strings
		/// </summary>
		public static int FindMutualPrefix(this String s1, String s2, int i_start = 0)
		{
			int i, cc;
			if ((cc = s1.Length) == 0)
				return 0;
			if (cc > (i = s2.Length))
				cc = i;
			for (i = i_start; i < cc; i++)
				if (s1[i] != s2[i])
					break;
			return i;
		}

		public static IEnumerable<String> Split(this String s, String sep, StringComparison cmp, StringSplitOptions sso = StringSplitOptions.None)
		{
			if (s.Length == 0)
				yield break;
			if (sep.Length == 0)
			{
				yield return s;
				yield break;
			}

			int ix;
			int ix_prev = 0;
			while ((ix = s.IndexOf(sep, ix_prev, cmp)) != -1)
			{
				int c = ix - ix_prev;
				if (c > 0 || sso != StringSplitOptions.RemoveEmptyEntries)
					yield return s.Substring(ix_prev, c);

				ix_prev = ix + sep.Length;
			}
			if (ix_prev < s.Length || sso != StringSplitOptions.RemoveEmptyEntries)
				yield return s.Substring(ix_prev);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String EscapeDq(this String s)
		{
			return s.Replace("\"", "\\\"");
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if true	// agree now uses version below
		public static IEnumerable<String> QuoteInsulatedSplit(this String s, Char ch_sep)
		{
			int cb, i, i_last = 0;
			Stack<Char> stk = new Stack<Char>();
			for (i = 0; i < s.Length; i++)
			{
				Char ch = s[i];
				if (ch == '\'' || ch == '\"')
				{
					if (stk.Count > 0 && ch == stk.Peek())
						stk.Pop();
					else
						stk.Push(ch);
				}
				else if (stk.Count == 0 && ch == ch_sep)
				{
					if ((cb = i - i_last) > 0)
						yield return s.Substring(i_last, cb);
					i_last = i + 1;
				}
			}
			if ((cb = i - i_last) > 0)
				yield return s.Substring(i_last, cb);
#if DEBUG
			if (stk.Count != 0)
				throw new Exception();
#endif
		}
#endif
		public static IEnumerable<String> QuoteInsulatedSplit(this StringBuilder s, Char ch_sep)
		{
			int cb, i, i_last = 0;
			Char ch_cur = default(Char);
			for (i = 0; i < s.Length; i++)
			{
				Char ch = s[i];
				if (ch_cur == 0 && (ch == '\'' || ch == '\"'))
				{
					ch_cur = ch;
				}
				else if (ch == ch_cur && (i == 0 || s[i - 1] != '\\'))
				{
					ch_cur = default(Char);
				}
				else if (ch_cur == 0 && ch == ch_sep)
				{
					if ((cb = i - i_last) > 0)
						yield return Builder._string_builder_ext.Substring(s, i_last, cb);
					i_last = i + 1;
				}
			}
			if ((cb = i - i_last) > 0)
				yield return Builder._string_builder_ext.Substring(s, i_last, cb);
		}

#if false
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<String> QuoteInsulatedSplit(this String s, params Char[] rgch)
		{
			int cb, i, i_last = 0;
			Stack<Char> stk = new Stack<Char>();
			for (i = 0; i < s.Length; i++)
			{
				Char ch = s[i];
				if (ch == '\'' || ch == '\"')
				{
					if (stk.Count > 0 && ch == stk.Peek())
						stk.Pop();
					else
						stk.Push(ch);
				}
				else if (stk.Count == 0 && Array.IndexOf<Char>(rgch, ch) != -1)
				{
					if ((cb = i - i_last) > 0)
						yield return s.Substring(i_last, cb);
					i_last = i + 1;
				}
			}
			if ((cb = i - i_last) > 0)
				yield return s.Substring(i_last, cb);
#if DEBUG
			if (stk.Count != 0)
				throw new Exception();
#endif
		}
#endif


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<String> InsulatedSplit(this String s, Char ins, params Char[] split_chars)
		{
			if (split_chars.Length == 0)
				split_chars = alib.Character.Charset.ws;
			bool f_enable = true;
			int cb, i, i_last = 0;
			for (i = 0; i < s.Length; i++)
			{
				if (s[i] == ins)
					f_enable = !f_enable;
				else if (f_enable && Array.IndexOf<Char>(split_chars, s[i]) != -1)
				{
					if ((cb = i - i_last) > 0)
						yield return s.Substring(i_last, cb);
					i_last = i + 1;
				}
			}
			if ((cb = i - i_last) > 0)
				yield return s.Substring(i_last, cb);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<String> InsulatedSplitWithEscape(this String s, Char ins, params Char[] split_chars)
		{
			bool f_enable = true;
			int cb, i, i_last = 0;
			for (i = 0; i < s.Length; i++)
			{
				if (s[i] == ins && (i == 0 || s[i - 1] != '\\'))
					f_enable = !f_enable;
				else if (f_enable && Array.IndexOf<Char>(split_chars, s[i]) != -1)
				{
					if ((cb = i - i_last) > 0)
						yield return s.Substring(i_last, cb);
					i_last = i + 1;
				}
			}
			if ((cb = i - i_last) > 0)
				yield return s.Substring(i_last, cb);
		}

#if false
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<String> InsulatedSplit(this String s, Char[] ins, params Char[] split_chars)
		{
			Char wait_for = default(Char);
			int cb, i, i_last = 0;
			for (i = 0; i < s.Length; i++)
			{
				Char ch = s[i];
				int v = Array.IndexOf<Char>(ins, ch);
				if (v != -1)
				{
					if (ch == wait_for)
						wait_for = default(Char);
					else
						wait_for = ch;
				}
				else if (wait_for == default(Char) && Array.IndexOf<Char>(split_chars, ch) != -1)
				{
					if ((cb = i - i_last) > 0)
						yield return s.Substring(i_last, cb);
					i_last = i + 1;
				}
			}
			if ((cb = i - i_last) > 0)
				yield return s.Substring(i_last, cb);
		}
#endif


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Left(this String s, int cch)
		{
			if (s == null || s.Length == 0)
				return String.Empty;
			return s.Length <= cch ? s : s.Substring(0, cch);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String LeftOf(this String s, Char ch)
		{
			if (s == null || s.Length == 0)
				return String.Empty;
			int ix;
			return (ix = s.IndexOf(ch)) == -1 ? s : s.Substring(0, ix);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String RemoveOuterQuotationMarks(this String s)
		{
			if (s == null)
				return String.Empty;

			int c;
			if ((c = s.Length - 2) >= 0)
			{
				Char ch0 = s[0], ch1 = s[c + 1];
				if ((ch0 == '\"' && ch1 == '\"') || (ch0 == '“' && ch1 == '”'))
					s = s.Substring(1, c);
			}
			return s;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String RemoveOuterSquareBackets(this String s)
		{
			if (s == null)
				return String.Empty;

			int c;
			if ((c = s.Length - 2) >= 0 && s[0] == '[' && s[c + 1] == ']')
				s = s.Substring(1, c);
			return s;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsTrimmedNonEmpty(this String s)
		{
			int c;
			if ((c = s.Length) == 0 || Char.IsWhiteSpace(s[0]))
				return false;
			return c == 1 || !Char.IsWhiteSpace(s[c - 1]);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsNullOrTrimmedNonEmpty(this String s)
		{
			return s == null || IsTrimmedNonEmpty(s);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsTrimmed(this String s)
		{
			int c;
			return (c = s.Length) == 0 || (!Char.IsWhiteSpace(s[0]) && (c == 1 || !Char.IsWhiteSpace(s[c - 1])));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsTrimmedOrNull(this String s)
		{
			return s == null || IsTrimmed(s);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String TrimToNull(this String s)
		{
			return s == null || s.Length == 0 || (s = s.Trim()).Length == 0 ? null : s;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String TrimEndOrPadRight(this String s, int cch)
		{
			int c;
			if (cch == 0)
				return String.Empty;
			if (s == null)
				goto full_empty;
			if ((c = s.Length) == cch)
				return s;
			if (c > 0)
				return c < cch ? s.PadRight(cch) : s.Substring(0, cch);
		full_empty:
			return new String(' ', cch);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String TrimEndOrPadLeft(this String s, int cch)
		{
			if (s == null)
				return cch == 0 ? String.Empty : new String(' ', cch);
			int c = s.Length;
			if (c == cch)
				return s;
			if (c == 0)
				return new String(' ', cch);
			if (c < cch)
				return s.PadLeft(cch);
			return s.Remove(cch);
		}

		/// <summary>
		/// allows zero or negative length
		/// </summary>
		public static String TryPadLeft(this String s, int c)
		{
			if (c <= s.Length)
				return s;
			return s.PadLeft(c);
		}
		public static String LeftRight(this String left, String right, int width)
		{
			left = left.TrimEnd();
			right = right.TrimStart();
			if ((width -= (left.Length + right.Length + 1)) <= 0)
				return left + " " + right;
			return left + new String(' ', width) + right;
		}
		public static String LeftRightComb(this String left, String right, int width)
		{
			left = left.TrimEnd();
			right = right.TrimStart();
			width += left.Count(ch => ch == '\u035F');

			if ((width -= (left.Length + right.Length + 1)) <= 0)
				return left + " " + right;
			return left + new String(' ', width) + right;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Right(this String s, int cch)
		{
			return (s.Length <= cch) ? s : s.Substring(s.Length - cch);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String PadCenter(this String s, int width, char pad_char)
		{
			if (s == null || width <= s.Length)
				return s;
			return s.PadLeft(s.Length + (width - s.Length) / 2, pad_char).PadRight(width, pad_char);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// null or empty 's' never throws (always returns String.Empty).
		/// Otherwise, idx==s.Length is permitted (always returns String.Empty), but other out-of-range indexes 
		/// will throw ArgumentOutOfRangeException. cch must be non-negative.
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String SubstringOrLess(this String s, int idx, int cch)
		{
			int c;
			if (s == null || (c = s.Length) == 0)
				return String.Empty;

			if (cch < 0 || (uint)idx > (uint)c)
				throw new ArgumentOutOfRangeException();

			if (idx + cch >= c)
				return idx == 0 ? s : s.Substring(idx);

			return s.Substring(idx, cch);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// It's important to not spuriously create new strings
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool SubstringStartsWith(this String s, int i, String s_compare)
		{
			int c, j;
			if ((c = s_compare.Length) > s.Length - i)
				return false;
			for (j = 0; j < c; j++)
				if (s_compare[j] != s[i++])
					return false;
			return true;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// It's important to not spuriously create new strings
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool ToLowerSubstringStartsWith(this String s, int i, String s_compare)
		{
			int c, j;
			if ((c = s_compare.Length) > s.Length - i)
				return false;
			for (j = 0; j < c; j++)
				if (s_compare[j] != Char.ToLower(s[i++]))
					return false;
			return true;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool HasNonSpaceChars(this String s)
		{
			foreach (Char ch in s)
				if (ch != ' ')
					return true;
			return false;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool IsAllWhitespace(this String s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				switch (s[i])
				{
					case '\u0009':
					case '\u000A':
					case '\u000B':
					case '\u000C':
					case '\u000D':
					case '\u0020':
					case '\u0085':
					case '\u00A0':
					case '\u1680':
					case '\u180E':
					case '\u2000':
					case '\u2001':
					case '\u2002':
					case '\u2003':
					case '\u2004':
					case '\u2005':
					case '\u2006':
					case '\u2007':
					case '\u2008':
					case '\u2009':
					case '\u200A':
					case '\u2028':
					case '\u2029':
					case '\u202F':
					case '\u205F':
					case '\u3000':
						continue;
				}
				return false;
			}
			return true;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static String RemoveSpaces(this String s)
		{
			return new String(s.Where(ch => ch != ' ').ToArray());
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static String WhitespaceToSpace(this String s)
		{
			return new String(s.Select(e => Char.IsWhiteSpace(e) ? ' ' : e).ToArray());
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// String.Replace does not replace adjacent sequences /AxAxA/AxA/zA/ ... /zAxA/
		/// </summary>
		public static String ReplaceAll(this String s_in, String txt, String repl)
		{
			int c = s_in.Length;
			do
				s_in = s_in.Replace(txt, repl);
			while (c != (c = s_in.Length));
			return s_in;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static String CondenseSpaces(this String s_in)
		{
			int i, j, c;
			if ((j = s_in.Length) == 0)
				return s_in;

			for (i = 0; i < j && s_in[i] == ' '; i++)
				;
			for (c = j - 1; c > i && s_in[c] == ' '; c--)
				;

			if ((c -= i - 1) < j)
				s_in = s_in.Substring(i, c);

			for (i = 0; (i = s_in.IndexOf(' ', i)) != -1; )
			{
				j = ++i;
				while (j <= c && s_in[j] == ' ')
					j++;
				if (j > i)
					s_in = s_in.Remove(i, j - i);
			}
			return s_in;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static String DoubleQuote(this String s)
		{
			var rgch = s.ToCharArray();
			for (int i = 0; i < s.Length; i++)
				if (Character.Thai._character_thai_ext.IsThai(rgch[i]))
					return s;
			return "“" + s + "”";
		}
		public static String SmartQuotes(this String s)
		{
			var rgch = s.ToCharArray();
			int f = 0;
			for (int i = 0; i < rgch.Length; i++)
				if (rgch[i] == '\"')
					rgch[i] = (f++ & 0x01) == 0 ? '“' : '”';
			return f == 0 ? s : new String(rgch);
		}
		public static String SmartQuotes(this String s, bool f_span)
		{
			int i = 0;
			Char[] rgch = s.ToCharArray();
			StringBuilder sb = new StringBuilder();
			foreach (Char ch in rgch)
			{
				if (ch == '\"')
				{
					if ((i++ & 0x01) == 0)
						sb.Append("<span style='font-family:cambria;'>“</span>");
					else
						sb.Append("<span style='font-family:cambria;'>”</span>");
				}
				else
					sb.Append(ch);
			}
			return sb.ToString();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool IsAllDigits(this String s)
		{
			int i;
			for (i = 0; i < s.Length; i++)
				if (!alib.Character._character_ext.IsDigit(s[i]))
					return false;
			return (i > 0);
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static String RemoveZWSP(this String s)
		{
			return new String(s.Where(e => e != '\x200b').ToArray());
		}

		////////////////////////////////////////////////////////////////////////////////////////
		///
		public static String DQ(this String s)
		{
			return "\"" + s + "\"";
		}

		////////////////////////////////////////////////////////////////////////////////////////
		///
		public static String SQ(this String s)
		{
			return "'" + s + "'";
		}

		////////////////////////////////////////////////////////////////////////////////////////
		///
		public static String SQRB(this Object obj)
		{
			String s;
			if (obj != null && (s = obj as String ?? obj.ToString()).Length > 0)
				return "[" + s + "]";
			return String.Empty;
		}
		public static String SQRB(this int i) { return SQRB(i.ToString()); }

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		public static unsafe bool StartsWith(Char* psz, String s)
		{
			foreach (Char ch in s)
				if (ch != *psz++)
					return false;
			return true;
		}
	};

#if ! __MOBILE__
	public static class _encrypt_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Encrypt(this String s, Byte[] des_key, Byte[] des_iv)
		{
			Byte[] rgb = _encrypt(Encoding.UTF8.GetBytes(s), des_key, des_iv);
			return String.Join(String.Empty, rgb.Select(e => e.ToString("x2")).ToArray());
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Decrypt(this String s, Byte[] des_key, Byte[] des_iv)
		{
			if ((s.Length & 1) > 0)
				return string.Empty;
			Byte[] rgb;
			try
			{
				rgb = s.ToCharArray().Where((e, ix) => (ix & 1) == 0).Select((e, ix) => Convert.ToByte(s.Substring(ix * 2, 2), 16)).ToArray();
			}
			catch
			{
				return String.Empty;
			}
			return Encoding.UTF8.GetString(_decrypt(rgb, des_key, des_iv));
		}

		// Encrypt a byte array into a byte array using a key and an IV 
		public static Byte[] _encrypt(Byte[] clearData, Byte[] des_key, Byte[] des_iv)
		{
			using (MemoryStream ms = new MemoryStream())
			using (TripleDES alg = TripleDES.Create())
			{
				alg.Key = des_key;
				alg.IV = des_iv;

				using (CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
					cs.Write(clearData, 0, clearData.Length);

				return ms.ToArray();
			}
		}

		// Decrypt a byte array into a byte array using a key and an IV 
		public static Byte[] _decrypt(Byte[] cipherData, Byte[] des_key, Byte[] des_iv)
		{
			using (MemoryStream ms = new MemoryStream())
			using (TripleDES alg = TripleDES.Create())
			{
				alg.Key = des_key;
				alg.IV = des_iv;

				using (CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
					cs.Write(cipherData, 0, cipherData.Length);

				return ms.ToArray();
			}
		}
	};
#endif
}

namespace alib.String.Thai
{
	using String = System.String;
	using alib.Character.Thai;

	public static class _string_thai_ext
	{
		static _string_thai_ext()
		{
			digits = "๐๑๒๓๔๕๖๗๘๙".ToCharArray();

			trimchars_thai = new[] { ' ', '(', '[', '‘', ']', ')', '\"', '“', '”' };

			DotPhinthu = new[] { '•', 'ฺ' };

			enc_874 = Encoding.GetEncoding(874);

			var rgb = new byte[256];
			for (int i = 0; i < rgb.Length; i++)
				rgb[i] = (byte)i;

			ok_874_chars = new HashSet<Char>(enc_874.GetString(rgb));
			ok_874_chars.Remove(default(Char));
		}

		static readonly Char[] DotPhinthu;

		static readonly Char[] digits;

		static readonly HashSet<Char> ok_874_chars;

		static readonly Encoding enc_874;

		public static readonly Char[] trimchars_thai;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String NormalizeThai(this String s)
		{
			// I tested Trim() experimentally: when it does nothing, it returns a reference to the same string
			return s.Trim(trimchars_thai).NormalizeYamok();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String ToThaiDigits(this String s)
		{
			return new String(s.Select(ch => '0' <= ch && ch <= '9' ? digits[ch - '0'] : ch).ToArray());
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsTIS620(this String s)
		{
			for (int i = 0; i < s.Length; i++)
				if (s[i] < 161)
					return false;
			return true;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String PadRightThai(this String s, int cch)
		{
			cch -= new StringInfo(s).LengthInTextElements;
			if (cch <= 0)
				return s;
			return s + new String('\u2002', cch);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String ToThin874(this String s)
		{
			return Encoding.GetEncoding(1252).GetString(enc_874.GetBytes(s));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe int EncodeToNative874(this String s, byte* pbuf, int cch_max)
		{
			int c = s.Length;
			if (c + 1 > cch_max)
				return 0;
			fixed (Char* p = s)
				pbuf[enc_874.GetBytes(p, c, (byte*)pbuf, cch_max)] = 0;
			return c + 1;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static String EscapeNonTis620(this String s)
		{
			String[] rgs = s.Select(e =>
			{
				if (ok_874_chars.Contains(e))
					return e.ToString();
				else
					return String.Format("&#{0};", (int)e);
			}).ToArray();
			return String.Join(String.Empty, rgs);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static Char FirstThaiCons(this String s)
		{
			foreach (Char c in s.ToCharArray())
				if (c.IsThaiCons())
					return c;
			return default(Char);
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String NormalizeYamok(this String s)
		{
			int j = 0;
			while ((j = s.IndexOf(" ๆ", j)) >= 0)
				s = s.Remove(j, 1);
			return s;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool ContainsThai(this String s)
		{
			return s.Any(ch => ch.IsThai());
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String StripDotAndPhinthu(this String s_in)
		{
			return new String(s_in.Where(e => e != 'ฺ' && e != '•').ToArray());
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool ContainsDotOrPhinthu(this String s_in)
		{
			return s_in.IndexOfAny(DotPhinthu) != -1;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe String RemoveDiacritics(this String s_in)
		{
			String norm = s_in.Normalize(NormalizationForm.FormD);
			fixed (Char* pin_s = norm)
			{
				Char* p_end = pin_s + norm.Length;
				Char* p = pin_s;
				while (p < p_end)
				{
					if (!p[0].IsThai() && CharUnicodeInfo.GetUnicodeCategory(*p) == UnicodeCategory.NonSpacingMark)
						goto yes_convert;
					p++;
				}
				return s_in;
			yes_convert:
				StringBuilder sb = new StringBuilder(norm.Substring(0, (int)(p - pin_s)), norm.Length);
				p++;	// skip the one detected above
				while (p < p_end)
				{
					if (p[0].IsThai() || CharUnicodeInfo.GetUnicodeCategory(*p) != UnicodeCategory.NonSpacingMark)
						sb.Append(*p);
					p++;
				}
				return sb.ToString();
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String Reinterpret(this String s_in, bool f_bullet)
		{
			return new String(s_in.Select(e => e.Reinterpret(f_bullet)).ToArray());
		}
	};
};

namespace alib.String.Builder
{
	using String = System.String;

	public static class _string_builder_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static StringBuilder AppendFormatLine(this StringBuilder sb, String format, params Object[] args)
		{
			return sb.AppendLine(String.Format(format, args));
		}

		public static unsafe String Substring(this StringBuilder sb, int startIndex, int length)
		{
			if (startIndex >= sb.Length)
				throw new ArgumentException(String.Empty, "startIndex");
			if (startIndex + length > sb.Length)
				throw new ArgumentException(String.Empty, "length");

			Char[] rgch = new Char[length];
			sb.CopyTo(startIndex, rgch, 0, length);
			return new String(rgch);
		}

		public static void AppendLines(this StringBuilder sb, IEnumerable<String> lines)
		{
			sb.AppendLine(lines.StringJoin(Environment.NewLine));
		}
	};

#if false
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static unsafe void Append(this StringBuilder sb, Char* p, int cch)
	{
#if true
	// this tests much faster than what follows
	sb.Append(new String(p, 0, cch));
#else
	// seems to be no way to blt the internal contents
	if (cch == 0 || p == null)
		return;
	int idx = sb.Length;
	sb.Length += cch;
	Char* p_end = p + cch;
	while (p < p_end)
		sb[idx++] = *p++;
#endif
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static int IndexOf(this StringBuilder sb, String s)
	{
		return IndexOf(sb, s, 0);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static int IndexOf(this StringBuilder sb, String s, int startIndex)
	{
		if (s == null)
			s = String.Empty;

		for (int i = startIndex; i < sb.Length; i++)
		{
			int j;
			for (j = 0; j < s.Length && i + j < sb.Length && sb[i + j] == s[j]; j++)
				;
			if (j == s.Length)
				return i;
		}
		return -1;
	}
#endif
}

namespace alib.String.Thin
{
	using alib.Array;
	using String = System.String;

	public static class _string_thin_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Convert byte array to its thin string 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe String ToThin(this Byte[] rgb)
		{
			fixed (Byte* pb = rgb)
				return Marshal.PtrToStringAnsi(new IntPtr(pb), rgb.Length);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Split the array of bytes into portions delimited by multibyte separator 'sep'
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<Byte[]> Split(this Byte[] rgb, Byte[] sep)
		{
			int cb, i, i_prev = 0;
			for (i = 0; i < rgb.Length; )
			{
				if (rgb.ValueCompare<Byte>(i, sep))
				{
					if ((cb = i - i_prev) != 0)
					{
						Byte[] ret = new Byte[cb];
						Buffer.BlockCopy(rgb, i_prev, ret, 0, cb);
						yield return ret;
					}
					i += sep.Length;
					i_prev = i;
				}
				else
					i++;
			}
			if ((cb = i - i_prev) > 0)
			{
				Byte[] ret = new Byte[cb];
				Buffer.BlockCopy(rgb, i_prev, ret, 0, cb);
				yield return ret;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Split the array of bytes into portions delimited by thin string 'thin'
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static IEnumerable<Byte[]> Split(this Byte[] src, String thin)
		{
			foreach (Byte[] ret in src.Split(thin.ToByteArr()))
				yield return ret;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int IndexOf(this Byte[] a, Byte[] b)
		{
			if (b.Length > a.Length)
				return -1;
			int term = a.Length - b.Length + 1;
			for (int i = 0; i < term; i++)
			{
				if (a.ValueCompare<Byte>(i, b))
					return i;
			}
			return -1;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Convert a thin string to its byte array
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe Byte[] ToByteArr(this String thin)
		{
			Byte[] rgb = new Byte[thin.Length];
			fixed (Char* _p_src = thin)
			fixed (Byte* _p_dst = rgb)
			{
				Char* p = _p_src;
				Char* p_end = p + thin.Length;
				Byte* p_dst = _p_dst;
				while (p < p_end)
					*p_dst++ = (Byte)(*p++);
			}
			return rgb;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Append the thin string's bytes to the list of bytes
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Append(this List<Byte> l, String thin)
		{
			l.AddRange(thin.ToByteArr());
		}
	};
}
