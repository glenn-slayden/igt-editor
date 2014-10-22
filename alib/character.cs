using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using alib.Array;

namespace alib.Character
{
	public static class Charset
	{
		// according to 'Char.IsWhiteSpace':
		public static readonly Char[] ws =
		{ '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u0020', '\u0085', '\u00A0', '\u1680', 
			'\u180E', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', 
			'\u2008', '\u2009', '\u200A', '\u2028', '\u2029', '\u202F', '\u205F', '\u3000' };

		public static readonly Char[] comma_ws = new[] { ',' }.Concat(ws);

		public static readonly Char[] semi_ws = new[] { ';' }.Concat(ws);

		public static readonly Char[] comma_semi_ws = new[] { ',', ';' }.Concat(ws);

		public static readonly Char[] allquotes_ws = new[] { '\'', '\"', '“', '”', '‘', '’', '»', '«', '„', '‟', '‚', '‛' }.Concat(ws);

		public static readonly Char[] dot_ws = new[] { '.' }.Concat(ws);

		public static readonly Char[] sp = { ' ' };

		public static readonly Char[] tab = { '\t' };

		public static readonly Char[] semi = { ';' };

		public static readonly Char[] dq = { '\"' };

		public static readonly Char[] cr_lf = { '\r', '\n' };

		public static readonly Char[] colon = { ':' };

		public static readonly Char[] dot = { '.' };

		public static readonly Char[] dollar = { '$' };

		public static readonly Char[] _ = { '_' };

		public static readonly Char[] hyphen = { '-' };

		public static readonly Char[] cl_brace = { '}' };

		public static readonly Char[] braces = { '{', '}' };

		public static readonly Char[] emdash_semi = { '—', ';' };

		public static readonly Char[] sq_dq = { '\'', '\"' };

		public static readonly Char[] dqdq = { '\"', '“', '”' };

		public static readonly Char[] outer_trim = { '\"', '\'', '(', ')', '[', ']' };

		public static readonly Char[] parens = { '(', ')' };

		public static readonly Char[] op_space = { '(', ' ' };

		public static readonly Char[] parens_space = { '(', ')', ' ' };
	};

	public sealed class CharComparison : IEqualityComparer<Char>
	{
		static CharComparison() { IgnoreCase = new CharComparison(); }
		CharComparison() { }

		public static IEqualityComparer<Char> IgnoreCase;
		public bool Equals(char x, char y)
		{
			return x == y || Char.ToLowerInvariant(x) == Char.ToLowerInvariant(y);
		}
		public int GetHashCode(char obj) { return (int)obj; }
	};

	public static class _character_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool IsWhiteSpace(this Char ch)
		{
			switch (ch)
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
					return true;
			}
			return false;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool IsSpaceChar(this Char ch)
		{
			switch (ch)
			{
				case '\u0020':
				case '\u00A0':	// nbsp
				case '\u1680':	// 'ogham'
				case '\u180E':	// mongolian
				case '\u2000':	// en quad
				case '\u2001':	// em quad
				case '\u2002':	// en space
				case '\u2003':	// em space
				case '\u2004':	// 3
				case '\u2005':	// 4
				case '\u2006':	// 6
				case '\u2007':	// figure space
				case '\u2008':	// punctuation space
				case '\u2009':	// thin space
				case '\u200A':	// hair space
				case '\u202F':	// narrow nbsp
				case '\u205F':	// medium math sp
				case '\u3000':	// ideographic sp
					return true;
			}
			return false;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool IsLineControl(this Char ch)
		{
			switch (ch)
			{
				case '\u000A':	// lf
				case '\u000C':	// form feed
				case '\u000D':	// cr
				case '\u0085':	// next line
				case '\u2028':	// line sep
				case '\u2029':	// para sep
					return true;
			}
			return false;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public static bool IsDigit(this Char ch)
		{
			return (uint)(ch - '0') <= 9;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsVowel(this Char ch)
		{
			switch (ch | ' ')
			{
				case 'a':
				case 'e':
				case 'i':
				case 'o':
				case 'u':
					return true;
			}
			return false;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static bool IsHex(this char ch)
		{
			return (uint)(ch - '0') <= 9 || (uint)((ch | ' ') - 'a') <= 5;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static byte HexToByte(this Char ch)
		{
			int ich = ch - '0';
			if ((uint)ich <= 9)
				return (byte)ich;

			ich = (ch | ' ') - 'a';
			if ((uint)ich <= 5)
				return (byte)(ich + 10);

			return 0;	// legacy behavior for t-l site
			//throw new FormatException();
		}
	};

#if false
	/// <summary>
	/// Case-insensitive (ASCII, not culture-aware)
	/// </summary>
	[DebuggerDisplay("{ToString(),nq}")]
	public class CharMap<T>
	{
		public CharMap(String s)
		{
			if (s.Length == 0)
				throw new Exception();

			var r = new Range(s);
			this._offs = r.Min - 1;
			this._map = new T[r.Extent + 1];
			this.c = 0;
		}
		public CharMap(Char[] iech)
			: this(new String(iech))
		{
		}

		public void SetDefaultValue(T default_value)
		{
			//if (default_value.Equals(default(T)))
			//	return;
			for (int i = 0; i < _map.Length; i++)
				_map[i] = default_value;
			this.c = 0;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly T[] _map;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly int _offs;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int c;

		public T this[Char ch]
		{
			get { return _map[get_index(ch)]; }
			set
			{
				int ix = get_index(ch);
				if (ix > 0 && !_map[ix].Equals(value))
				{
					if (!_map[ix].Equals(DefaultValue))
						c--;
					if (!(_map[ix] = value).Equals(DefaultValue))
						c++;
				}
			}
		}

		int get_index(Char ch) { return clamp((ch & 0xFFDF) - _offs); }

		int clamp(int ix1) { return ix1 < 0 || ix1 >= _map.Length ? 0 : ix1; }

		Char reverse(int ix1) { return (Char)(ix1 + _offs); }

		public T DefaultValue { get { return _map[0]; } }
		public int MapSize { get { return _map.Length - 1; } }
		public int Count { get { return c; } }
		public MapEntry First { get { return new MapEntry(this, 1); } }
		public MapEntry Last { get { return new MapEntry(this, _map.Length - 1); } }

		[DebuggerDisplay("{ToString(),nq}")]
		public struct MapEntry
		{
			public MapEntry(CharMap<T> charmap, int ix)
			{
				this.Index = (this.charmap = charmap).clamp(ix);
			}
			readonly CharMap<T> charmap;
			public readonly int Index;

			public Char Key { get { return charmap.reverse(Index); } }
			public T Value { get { return charmap._map[Index]; } }

			public override String ToString()
			{
				return String.Format("{0,4} '{1}'  [{2}]", Index, Key, Value);
			}
		};

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		MapEntry[] _dbg_used { get { return _dbg_used_fn().ToArray(); } }
		IEnumerable<MapEntry> _dbg_used_fn()
		{
			var d = this.DefaultValue;
			for (int ix = 1; ix < _map.Length; ix++)
				if (!_map[ix].Equals(d))
					yield return new MapEntry(this, ix);
		}
		//IEnumerable<MapEntry> _dbg_all_fn
		//{
		//	get
		//	{
		//		for (int ix = 1; ix < _map.Length; ix++)
		//			yield return new MapEntry(this, ix);
		//	}
		//}

		public override String ToString()
		{
			if (_map == null)
				return "(null)";
			return String.Format("Size={0} (/{1}/ - /{2}/)  Count={3}",
				MapSize,
				First.ToString().Trim(),
				Last.ToString().Trim(),
				Count);
		}
	};
#endif

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	namespace Thai
	{
		public static class _character_thai_ext
		{
			public static bool IsThai(this Char c)
			{
				return 'ก' <= c && c <= '๛';
			}
			public static bool IsThaiDigit(this Char c)
			{
				return '๐' <= c && c <= '๙';
			}
			/// <summary> excludes Thai numeric digits </summary>
			public static bool IsThaiAlpha(this Char c)
			{
				return 'ก' <= c && c <= '๏';
			}

			public static bool IsThaiTone(this Char ch)
			{
				return 0x0e48 <= ch && ch <= 0x0e4b;
			}
			public static bool ISDELETE(this Char c)
			{
				return c == '์';
			}
			public static bool ISPHINTHU(this Char c)
			{
				return c == 'ฺ';
			}

			public static bool IsThaiCons(this Char c)
			{
				return 'ก' <= c && c <= 'ฮ';
			}
			public static bool INVALIDFINAL(this Char c)
			{
				return c == 'ฉ' || c == 'ผ' || c == 'ฝ' || c == 'ย' || c == 'ว' || c == 'ห' || c == 'อ';
			}
			public static bool ISSONORANT(this Char c)
			{
				return c == 'ง' || c == 'น' || c == 'ม' || c == 'ย' || c == 'ร' || c == 'ล' || c == 'ว' || c == 'ญ' || c == 'ณ' || c == 'ฬ';
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			/// 
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			public static bool ISSUPERVOWEL(this Char c)
			{
				return c == 'ิ' || c == 'ี' || c == 'ึ' || c == 'ื';
			}
			public static bool ISSUBVOWEL(this Char c)
			{
				return c == 'ู' || c == 'ฺ';
			}
			public static bool ISPREVOWEL(this Char c)
			{
				return 'เ' <= c && c <= 'ไ';
			}
			public static bool ISPOSTVOWEL(this Char c)
			{
				return 'ะ' <= c && c <= 'ู';
			}
			public static bool ISVOWEL(this Char c)
			{
				return ISPREVOWEL(c) || ISPOSTVOWEL(c);
			}
			public static bool IsThaiVowel(this Char c)
			{
				return 'ะ' <= c && c <= 'ๅ' && c != 'ฺ' && c != '฿';
			}
			public static bool IsThaiPreGlyph(this Char c)
			{
				return c == 'ั' || ISPREVOWEL(c);
			}
			public static bool IsThaiPostGlyph(this Char c)
			{
				return (0x0e2f <= c && c <= 0x0e3a) || (0x0e45 <= c && c <= 0x0e4e);
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			/// 
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			public static bool IsThaiNameChar(this Char ch)
			{
				return 'ก' <= ch && ch <= '์' && ch != '฿' && ch != 'ๆ'; // note: ฯ  is allowed
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			/// 
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			public static Char Reinterpret(this Char ch, bool f_bullet)
			{
				if (0x00A1 <= ch && ch <= 0x00F9)
					return (Char)(ch + 3424);
				if (ch == 0x2022)
					return f_bullet ? '•' : 'ท';
				return ch;
			}

			static bool ISWHITE(Char c)
			{
				return c == ' ' || c == ',' || c == '.' || c == '!' || c == '?';
			}
			public static unsafe bool IsSyllableStart(Char* p)
			{
				if (*p == 0 || ISWHITE(p[0]))		// 0x95? no: bullet 'belongs' to this syllable
					return true;
				if (p[0].ISPREVOWEL())
					return true;
				p++;
				return p[0].ISPOSTVOWEL() || p[0].IsThaiTone();
			}

			public static bool IsThaiArabicOrDot(Char ch)
			{
				return Char.IsNumber(ch) || ch == '.';
			}

			public static System.String ToArabicNumString(this System.String s)
			{
				return new System.String(s.Select(e =>
				{
					if (alib.Character._character_ext.IsDigit(e))
						return e;
					else if (e.IsThaiDigit())
						return (Char)(e - '๐' + '0');
					else if (e == '.')
						return '.';
					else
						return default(Char);
				})
				.Where(e => e != default(Char)).ToArray());
			}

			public static bool IsSEAsian(this Char ch)
			{
				// Thai, lao, and Khmer
				return (0x0e00 <= ch && ch <= 0x0eff) || (0x1780 <= ch && ch <= 0x17ff);
			}
		};
	};
}
