//#define ALGLIB
#define IMPLICIT_SHALLOW

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace alib.Vectors
{
	public static class vec_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static Double DotProduct(this double[] v, double[] v2)
		{
			Double sxy = 0;
			for (int i = 0; i < v.Length; i++)
				sxy += v[i] * v2[i];
			return sxy;
		}
		public static void Normalize(this double[] v)
		{
			Double l = VectorLength(v);
			if (l > 0.0)
				for (int i = 0; i < v.Length; i++)
					v[i] /= l;
		}
		public static Double VectorLength(this double[] v)
		{
			Double s = 0;
			foreach (Double n in v)
				s += n * n;
			return System.Math.Sqrt(s);
		}

		public static double[] MeanVector(this IEnumerable<double[]> seq)
		{
			var e = seq.GetEnumerator();
			double[] ret = null;
			int c = 0;
			while (e.MoveNext())
			{
				var cur = e.Current;
				if (ret == null)
				{
					ret = new double[cur.Length];
					cur.CopyTo(ret, 0);
				}
				else
				{
					for (int i = 0; i < ret.Length; i++)
						ret[i] += cur[i];

				}
				c++;
			}
			for (int i = 0; i < c; i++)
				ret[i] /= c;
			return ret;
		}
	}
}

namespace alib.geom
{
	using Math = System.Math;
	using String = System.String;
	using math = alib.Math.math;

	[DebuggerDisplay("{ToString(),nq}")]
	public struct PointD : IEquatable<PointD>
	{
		public PointD(Double x, Double y)
		{
			this.x = x;
			this.y = y;
		}

		internal Double x, y;
		public Double X { get { return this.x; } set { this.x = value; } }
		public Double Y { get { return this.y; } set { this.y = value; } }

		public void Offset(Double offsetX, Double offsetY)
		{
			this.x += offsetX;
			this.y += offsetY;
		}
		public void Offset(VectorD v)
		{
			this.x += v.x;
			this.y += v.y;
		}

		public static PointD operator +(PointD p, VectorD v) { return Add(p, v); }

		public static PointD Add(PointD p, VectorD v)
		{
			PointD pp;
			pp.x = p.x + v.x;
			pp.y = p.y + v.y;
			return pp;
		}

		public static PointD operator -(PointD p, VectorD v) { return Subtract(p, v); }

		public static PointD Subtract(PointD p, VectorD v)
		{
			PointD pp;
			pp.x = p.x - v.x;
			pp.y = p.y - v.y;
			return pp;
		}

		public static VectorD operator -(PointD p1, PointD p2) { return Subtract(p1, p2); }

		public static VectorD Subtract(PointD p1, PointD p2)
		{
			VectorD v;
			v.x = p1.x - p2.x;
			v.y = p1.y - p2.y;
			return v;
		}

		public static explicit operator SizeD(PointD p) { return new SizeD(Math.Abs(p.x), Math.Abs(p.y)); }

		public static explicit operator VectorD(PointD p) { return new VectorD(p.x, p.y); }

		public static bool Equals(PointD p1, PointD p2)
		{
			return math.IsZero(p1.X - p2.X) && math.IsZero(p1.Y - p2.Y);
		}

		public static bool operator ==(PointD p1, PointD p2) { return Equals(p1, p2); }

		public static bool operator !=(PointD p1, PointD p2) { return !Equals(p1, p2); }

		public bool Equals(PointD value) { return Equals(this, value); }

		public override bool Equals(Object o) { return o is PointD && Equals(this, (PointD)o); }

		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }

		public override String ToString() { return System.String.Format("X={0:N3},Y={1:N3}", x, y); }
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public struct SizeD : IEquatable<SizeD>
	{
		/// <summary> This does *not* behave the same as System.Windows.Size.Empty </summary>
		public static SizeD Empty { get { return default(SizeD); } }

		public SizeD(Double width, Double height)
		{
			if (width < 0.0 || height < 0.0)
				throw new ArgumentException();

			this.width = width;
			this.height = height;
		}

		internal Double width, height;

		public Double Width
		{
			get { return width; }
			set
			{
				if (value < 0.0)
					throw new ArgumentException();
				this.width = value;
			}
		}
		public Double Height
		{
			get { return height; }
			set
			{
				if (value < 0.0)
					throw new ArgumentException();
				this.height = value;
			}
		}

		public bool IsEmpty { get { return width < math.ε || height < math.ε; } }

		public static bool Equals(SizeD s1, SizeD s2)
		{
			if (s1.IsEmpty)
				return s2.IsEmpty;

			return math.IsZero(s1.width - s2.width) && math.IsZero(s1.height = s2.height);
		}

		public static bool operator ==(SizeD s1, SizeD s2) { return Equals(s1, s2); }

		public static bool operator !=(SizeD s1, SizeD s2) { return !Equals(s1, s2); }

		public bool Equals(SizeD value) { return Equals(this, value); }

		public override bool Equals(Object o) { return o is SizeD && Equals(this, (SizeD)o); }

		public override int GetHashCode()
		{
			if (IsEmpty)
				return 0;
			return width.GetHashCode() ^ height.GetHashCode();
		}

		public override String ToString() { return String.Format("width={0:N3},height={1:N3}", width, height); }

		public static explicit operator VectorD(SizeD size) { return new VectorD(size.width, size.height); }

		public static explicit operator PointD(SizeD size) { return new PointD(size.width, size.height); }
	};

	[DebuggerDisplay("{ToString(),nq}")]
	public struct RectD : IEquatable<RectD>
	{
		public static RectD Empty { get { return default(RectD); } }

		public RectD(Double x, Double y, Double width, Double height)
		{
			this.x = x;
			this.y = y;
			if ((this.width = width) < math.ε)
				this.width = 0.0;
			if ((this.height = height) < math.ε)
				this.height = 0.0;
		}

		public RectD(PointD pt, SizeD sz)
			: this(pt.x, pt.y, sz.width, sz.height)
		{
		}

		public RectD(PointD pt1, PointD pt2)
			: this(Math.Min(pt1.x, pt2.x), Math.Min(pt1.y, pt2.y),
				   Math.Abs(pt1.x - pt2.x), Math.Abs(pt1.y - pt2.y))
		{
		}

		public RectD(PointD pt, VectorD v)
			: this(pt, pt + v)
		{
		}

		public RectD(SizeD sz)
		{
			this = default(RectD);
			this.Size = sz;
		}

		internal Double x, y, width, height;

		public Double X
		{
			get { return this.x; }
			set { this.x = value; }
		}
		public Double Y
		{
			get { return this.y; }
			set { this.y = value; }
		}

		public PointD Location
		{
			get { PointD pt; pt.x = x; pt.y = y; return pt; }
			set { x = value.x; y = value.y; }
		}

		public bool IsEmpty { get { return width < math.ε || height < math.ε; } }

		public SizeD Size
		{
			get { return new SizeD(width, height); }
			set
			{
				if ((this.width = value.width) < math.ε)
					this.width = 0.0;
				if ((this.height = value.height) < math.ε)
					this.height = 0.0;
			}
		}

		public Double Width
		{
			get { return width; }
			set
			{
				if ((this.width = value) < math.ε)
					this.width = 0.0;
			}
		}
		public Double Height
		{
			get { return height; }
			set
			{
				if ((this.height = value) < math.ε)
					this.height = 0.0;
			}
		}
		public Double Left { get { return this.x; } }
		public Double Top { get { return this.y; } }
		public Double Right { get { return x + Math.Max(width, 0.0); } }
		public Double Bottom { get { return y + Math.Max(height, 0.0); } }

		public PointD TopLeft { get { return Location; } }
		public PointD TopRight { get { return new PointD(this.Right, this.Top); } }
		public PointD BottomLeft { get { return new PointD(this.Left, this.Bottom); } }
		public PointD BottomRight { get { return new PointD(this.Right, this.Bottom); } }

		public bool Contains(PointD pt) { return this.Contains(pt.x, pt.y); }

		public bool Contains(Double x, Double y)
		{
			if (this.IsEmpty)
				return false;

			return x >= this.x && x - this.width <= this.x && y >= this.y && y - this.height <= this.y;
		}

		public bool Contains(RectD rect)
		{
			if (this.IsEmpty || rect.IsEmpty)
				return false;

			return x <= rect.x && y <= rect.y && x + width >= rect.x + rect.width && y + height >= rect.y + rect.height;
		}

		public bool IntersectsWith(RectD rect)
		{
			if (this.IsEmpty || rect.IsEmpty)
				return false;

			return rect.Left <= this.Right && rect.Right >= this.Left && rect.Top <= this.Bottom && rect.Bottom >= this.Top;
		}

		public void Intersect(RectD rect)
		{
			if (this.IsEmpty || rect.IsEmpty)
				this = default(RectD);
			else
			{
				Double num2 = Math.Max(Left, rect.Left);
				Double num = Math.Max(Top, rect.Top);
				width = Math.Max((Double)(Math.Min(Right, rect.Right) - num2), (Double)0.0);
				height = Math.Max((Double)(Math.Min(Bottom, rect.Bottom) - num), (Double)0.0);
				x = num2;
				y = num;
			}
		}

		public void Union(RectD rect)
		{
			if (rect.IsEmpty)
				return;

			if (this.IsEmpty)
				this = rect;
			else
			{
				x = Math.Min(this.Left, rect.Left);
				y = Math.Min(this.Top, rect.Top);

				var x2 = Math.Max(this.Right, rect.Right);
				var y2 = Math.Max(this.Bottom, rect.Bottom);

				if ((width = x2 - x) < math.ε)
					width = 0.0;
				if ((height = y2 - y) < math.ε)
					height = 0.0;
			}
		}

		public void Union(PointD point) { Union(new RectD(point, point)); }

		public void Offset(VectorD v) { Offset(v.x, v.y); }

		public void Offset(Double offsetX, Double offsetY)
		{
			x += offsetX;
			y += offsetY;
		}

		public void Inflate(SizeD size) { Inflate(size.width, size.height); }

		public void Inflate(Double width, Double height)
		{
			if ((this.width += width) < math.ε)
				this.width = 0.0;
			else if ((this.width += width) < math.ε)
				this.width = 0.0;
			else
				this.x -= width;

			if ((this.height += height) < math.ε)
				this.height = 0.0;
			else if ((this.height += height) < math.ε)
				this.height = 0.0;
			else
				this.y -= height;
		}

		public void Scale(Double scaleX, Double scaleY)
		{
			if (!this.IsEmpty)
			{
				this.x *= scaleX;
				this.y *= scaleY;
				this.width *= scaleX;
				this.height *= scaleY;
				if (scaleX < 0.0)
				{
					this.x += this.width;
					this.width *= -1.0;
				}
				if (scaleY < 0.0)
				{
					this.y += this.height;
					this.height *= -1.0;
				}
			}
		}

		public static RectD Offset(RectD rect, VectorD v)
		{
			rect.Offset(v.x, v.y);
			return rect;
		}
		public static RectD Offset(RectD rect, Double offsetX, Double offsetY)
		{
			rect.Offset(offsetX, offsetY);
			return rect;
		}
		public static RectD Inflate(RectD rect, SizeD size)
		{
			rect.Inflate(size.width, size.height);
			return rect;
		}
		public static RectD Inflate(RectD rect, Double width, Double height)
		{
			rect.Inflate(width, height);
			return rect;
		}
		public static RectD Intersect(RectD rect1, RectD rect2)
		{
			rect1.Intersect(rect2);
			return rect1;
		}
		public static RectD Union(RectD rect1, RectD rect2)
		{
			rect1.Union(rect2);
			return rect1;
		}
		public static RectD Union(RectD rect, PointD pt)
		{
			rect.Union(new RectD(pt, pt));
			return rect;
		}

		public bool Equals(RectD value) { return Equals(this, value); }

		public static bool Equals(RectD r1, RectD r2)
		{
			if (r1.IsEmpty)
				return r2.IsEmpty;

			return math.IsZero(r1.x - r2.x) && math.IsZero(r1.y - r2.y) &&
				   math.IsZero(r1.width - r2.width) && math.IsZero(r1.height - r2.height);
		}

		public static bool operator ==(RectD rect1, RectD rect2) { return Equals(rect1, rect2); }

		public static bool operator !=(RectD rect1, RectD rect2) { return !Equals(rect1, rect2); }

		public override bool Equals(Object o) { return o is RectD && Equals(this, (RectD)o); }

		public override int GetHashCode()
		{
			if (this.IsEmpty)
				return 0;
			return x.GetHashCode() ^ y.GetHashCode() ^ width.GetHashCode() ^ height.GetHashCode();
		}

		public override String ToString()
		{
			return String.Format("X={0:N3},Y={1:N3},Width={2:N3},Height={3:N3}");
		}
	};


	[DebuggerDisplay("{ToString(),nq}")]
	public struct VectorD : IEquatable<VectorD>
	{
		public VectorD(Double x, Double y)
		{
			this.x = x;
			this.y = y;
		}

		internal Double x, y;

		public Double X
		{
			get { return this.x; }
			set { this.x = value; }
		}
		public Double Y
		{
			get { return this.y; }
			set { this.y = value; }
		}

		public Double LengthSquared { get { return x * x + y * y; } }

		public Double Length { get { return Math.Sqrt(LengthSquared); } }

		public void Normalize()
		{
			this = (VectorD)(this / Math.Max(Math.Abs(this.x), Math.Abs(this.y)));
			this = (VectorD)(this / this.Length);
		}

		public static Double CrossProduct(VectorD v1, VectorD v2)
		{
			return (v1.x * v2.y) - (v1.y * v2.x);
		}

		public static Double AngleBetween(VectorD v1, VectorD v2)
		{
			Double y = (v1.x * v2.y) - (v2.x * v1.y);
			Double x = (v1.x * v2.x) + (v1.y * v2.y);
			return (Math.Atan2(y, x) * 57.295779513082323);
		}

		public void Negate() { this.x = -this.x; this.y = -this.y; }

		public static VectorD operator -(VectorD v) { v.Negate(); return v; }

		public static VectorD operator +(VectorD v1, VectorD v2) { return new VectorD(v1.x + v2.x, v1.y + v2.y); }

		public static VectorD Add(VectorD v1, VectorD v2) { return new VectorD(v1.x + v2.x, v1.y + v2.y); }

		public static VectorD operator -(VectorD v1, VectorD v2) { return new VectorD(v1.x - v2.x, v1.y - v2.y); }

		public static VectorD Subtract(VectorD v1, VectorD v2) { return new VectorD(v1.x - v2.x, v1.y - v2.y); }

		public static PointD operator +(VectorD v, PointD pt) { return new PointD(pt.x + v.x, pt.y + v.y); }

		public static PointD Add(VectorD v, PointD pt) { return new PointD(pt.x + v.x, pt.y + v.y); }

		public static VectorD operator *(VectorD v, Double scalar) { return new VectorD(v.x * scalar, v.y * scalar); }

		public static VectorD Multiply(VectorD v, Double scalar) { return new VectorD(v.x * scalar, v.y * scalar); }

		public static VectorD operator *(Double scalar, VectorD v) { return new VectorD(v.x * scalar, v.y * scalar); }

		public static VectorD Multiply(Double scalar, VectorD v) { return new VectorD(v.x * scalar, v.y * scalar); }

		public static VectorD operator /(VectorD v, Double scalar) { return (VectorD)(v * (1.0 / scalar)); }

		public static VectorD Divide(VectorD v, Double scalar) { return (VectorD)(v * (1.0 / scalar)); }

		public static Double operator *(VectorD v1, VectorD v2) { return ((v1.x * v2.x) + (v1.y * v2.y)); }

		public static Double Multiply(VectorD v1, VectorD v2) { return ((v1.x * v2.x) + (v1.y * v2.y)); }

		public static Double Determinant(VectorD v1, VectorD v2) { return ((v1.x * v2.y) - (v1.y * v2.x)); }

		public static bool Equals(VectorD v1, VectorD v2) { return math.IsZero(v1.x - v2.x) && math.IsZero(v1.y - v2.y); }

		public bool Equals(VectorD value) { return Equals(this, value); }

		public override bool Equals(object o) { return o is VectorD && Equals(this, (VectorD)o); }

		public static bool operator ==(VectorD v1, VectorD v2) { return Equals(v1, v2); }

		public static bool operator !=(VectorD v1, VectorD v2) { return !Equals(v1, v2); }

		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }

		public override String ToString()
		{
			return String.Format("X={0:N3},Y={0:N3}");
		}


		public static explicit operator SizeD(VectorD v)
		{
			return new SizeD(Math.Abs(v.x), Math.Abs(v.y));
		}

		public static explicit operator PointD(VectorD v)
		{
			return new PointD(v.x, v.y);
		}
	};
}

namespace alib.Matrix
{
	using Math = System.Math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class _matrix_ext
	{
		public static T[] Row<T>(this T[,] v, int i)
		{
			T[] row = new T[v.GetLength(1)];
			for (int j = 0; j < row.Length; j++)
				row[j] = v[i, j];
			return row;
		}
		public static T[] Column<T>(this T[,] v, int j)
		{
			T[] col = new T[v.GetLength(0)];
			for (int i = 0; i < col.Length; i++)
				col[j] = v[i, j];
			return col;
		}

		public static T[,] Trim<T>(this T[,] m, int r0, int c0)
		{
			if (m.GetLength(0) <= r0 && m.GetLength(1) <= c0)
				return m;
			T[,] _new = new T[r0, c0];
			for (int i = 0; i < r0; i++)
				for (int j = 0; j < c0; j++)
					_new[i, j] = m[i, j];
			return _new;
		}

		public static T[,] TakeKRows<T>(this T[,] m, int k) { return Trim(m, k, m.GetLength(1)); }
		public static T[,] TakeKColumns<T>(this T[,] m, int k) { return Trim(m, m.GetLength(0), k); }
		public static T[,] TakeKRowsAndColumns<T>(this T[,] m, int k) { return Trim(m, k, k); }

		public static void NormalizeColumns(this double[,] m)
		{
			int c_rows = m.GetLength(0);
			int c_cols = m.GetLength(1);
			double[] lengths = new double[c_cols];
			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					lengths[j] += Math.Pow(m[i, j], 2);
			for (int j = 0; j < c_cols; j++)
				lengths[j] = Math.Sqrt(lengths[j]);

			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					m[i, j] /= lengths[j];
		}

		public static T[,] Copy<T>(this T[,] m)
		{
			int c_rows = m.GetLength(0);
			int c_cols = m.GetLength(1);
			T[,] ret = new T[c_rows, c_cols];
			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					ret[i, j] = m[i, j];
			return ret;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{_DebugString()}")]
	public class Matrix
	{
		/// <summary> shallow (usurping) copy </summary>
		public Matrix(double[,] m)
		{
			this._values = m;
			this.c_rows = m.GetLength(0);
			this.c_cols = m.GetLength(1);
		}

		public Matrix(int c_rows, int c_cols)
			: this(new double[c_rows, c_cols])
		{
		}

		/// <summary> deep copy constructor </summary>
		public Matrix(Matrix m)
			: this(new double[m.c_rows, m.c_cols])
		{
			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					_values[i, j] = m[i, j];
		}

		public Matrix(double[] v)
			: this(v.Length, v.Length)
		{
			for (int i = 0; i < v.Length; i++)
				_values[i, i] = v[i];
		}

		public Matrix(double[] v, int c_take)
			: this(c_take, c_take)
		{
			c_take = Math.Min(c_take, v.Length);
			for (int i = 0; i < c_take; i++)
				_values[i, i] = v[i];
		}

		protected readonly double[,] _values;
		protected readonly int c_rows;
		protected readonly int c_cols;

		public double this[int row, int column]
		{
			get { return _values[row, column]; }
			set { _values[row, column] = value; }
		}

		public int RowCount { get { return c_rows; } }
		public int ColumnCount { get { return c_cols; } }

		public double[] Row(int i)
		{
			double[] v = new double[c_cols];
			for (int j = 0; j < c_cols; j++)
				v[j] = _values[i, j];
			return v;
		}

		public double[] Column(int j)
		{
			double[] v = new double[c_rows];
			for (int i = 0; i < c_rows; i++)
				v[i] = _values[i, j];
			return v;
		}

		public IEnumerable<double[]> Rows
		{
			get
			{
				//int x = _values.Cast<Double>().Count();	// can access all in [,] this way
				for (int i = 0; i < c_rows; i++)
					yield return Row(i);
			}
		}

		public IEnumerable<double[]> Columns
		{
			get
			{
				for (int i = 0; i < c_cols; i++)
					yield return Column(i);
			}
		}

		public void CleaveVertical(int left_cols, out Matrix left, out Matrix right)
		{
			if (left_cols == c_cols)
			{
				left = new Matrix(this);
				right = new Matrix(c_rows, 0);
			}
			else if (left_cols == 0)
			{
				left = new Matrix(c_rows, 0);
				right = new Matrix(this);
			}
			else if (left_cols > c_cols)
				throw new Exception();
			else
			{
				left = new double[c_rows, left_cols];
				right = new double[c_rows, c_cols - left_cols];
				for (int i = 0; i < c_rows; i++)
					for (int j = 0; j < c_cols; j++)
						if (j < left_cols)
							left[i, j] = _values[i, j];
						else
							right[i, j - left_cols] = _values[i, j];
			}
		}

		public void CleaveHorizontal(int top_rows, out Matrix top, out Matrix bottom)
		{
			if (top_rows == c_rows)
			{
				top = new Matrix(this);
				bottom = new Matrix(0, c_cols);
			}
			else if (top_rows == 0)
			{
				top = new Matrix(0, c_cols);
				bottom = new Matrix(this);
			}
			else if (top_rows > c_rows)
				throw new Exception();
			else
			{
				top = new double[top_rows, c_cols];
				bottom = new double[c_rows - top_rows, c_cols];
				for (int i = 0; i < c_rows; i++)
					for (int j = 0; j < c_cols; j++)
						if (i < top_rows)
							top[i, j] = _values[i, j];
						else
							bottom[i - top_rows, j] = _values[i, j];
			}
		}

		public void SetColumnValues(int j, double[] v)
		{
			if (v.Length != c_rows)
				throw new Exception();
			for (int i = 0; i < c_rows; i++)
				_values[i, j] = v[i];
		}

		public static Matrix Identity(int size)
		{
			Matrix m = new Matrix(size, size);
			for (int i = 0; i < size; i++)
				for (int j = 0; j < size; j++)
					m[i, j] = (i == j) ? 1.0 : 0.0;
			return m;
		}

		public Matrix Transpose()
		{
			Matrix m = new Matrix(c_cols, c_rows);
			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					m[j, i] = this[i, j];
			return m;
		}

		public static Matrix Add(Matrix m_l, Matrix m_r)
		{
			Debug.Assert(m_l.c_cols == m_r.c_cols);
			Debug.Assert(m_l.c_rows == m_r.c_rows);

			Matrix m = new Matrix(m_l.c_rows, m_r.c_cols);
			for (int i = 0; i < m_l.c_rows; i++)
				for (int j = 0; j < m_l.c_cols; j++)
					m[i, j] = m_l[i, j] + m_r[i, j];
			return m;
		}

		public static Matrix Subtract(Matrix m_l, Matrix m_r)
		{
			Debug.Assert(m_l.c_cols == m_r.c_cols);
			Debug.Assert(m_l.c_rows == m_r.c_rows);
			Matrix m = new Matrix(m_l.c_rows, m_r.c_cols);
			for (int i = 0; i < m_l.c_rows; i++)
				for (int j = 0; j < m_l.c_cols; j++)
					m[i, j] = m_l[i, j] - m_r[i, j];
			return m;
		}

		public static Matrix Multiply(Matrix m_l, Matrix m_r)
		{
			if (m_l.c_cols != m_r.c_rows)
				throw new Exception();
			Matrix m = new Matrix(m_l.c_rows, m_r.c_cols);
			for (int i = 0; i < m.c_cols; i++)
			{
				for (int j = 0; j < m_l.c_rows; j++)
				{
					double value = 0.0;
					for (int k = 0; k < m_r.c_rows; k++)
						value += m_l[j, k] * m_r[k, i];
					m[j, i] = value;
				}
			}
			return m;
		}

		public static Matrix Multiply(double left, Matrix m_r)
		{
			Matrix m = new Matrix(m_r.c_rows, m_r.c_cols);
			for (int i = 0; i < m.c_rows; i++)
				for (int j = 0; j < m_r.c_cols; j++)
					m[i, j] = left * m_r[i, j];
			return m;
		}

		public static Matrix Multiply(Matrix m_l, double right)
		{
			Matrix m = new Matrix(m_l.c_rows, m_l.c_cols);
			for (int i = 0; i < m_l.c_rows; i++)
				for (int j = 0; j < m_l.c_cols; j++)
					m[i, j] = m_l[i, j] * right;
			return m;
		}

		public static Matrix Divide(Matrix m_l, double right)
		{
			Matrix m = new Matrix(m_l.c_rows, m_l.c_cols);
			for (int i = 0; i < m_l.c_rows; i++)
				for (int j = 0; j < m_l.c_cols; j++)
					m[i, j] = m_l[i, j] / right;
			return m;
		}

		public static double[] DiagonalVector(Matrix m)
		{
			Debug.Assert(m.c_rows == m.c_cols);

			double[] v = new double[m.c_rows];
			for (int i = 0; i < m.c_rows; i++)
				v[i] = m[i, i];
			return v;
		}

		public Matrix Trim(int r0, int c0)
		{
			if (c_rows <= r0 && c_cols <= c0)
				return new Matrix(this);
			double[,] _new = new double[r0, c0];
			for (int i = 0; i < r0; i++)
				for (int j = 0; j < c0; j++)
					_new[i, j] = _values[i, j];
			return new Matrix(_new);
		}

		public Matrix TakeKRows(int k) { return Trim(k, c_cols); }
		public Matrix TakeKColumns(int k) { return Trim(c_rows, k); }
		public Matrix TakeKRowsAndColumns(int k) { return Trim(k, k); }

		public void NormalizeColumns()
		{
			double[] lengths = new double[c_cols];
			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					lengths[j] += Math.Pow(_values[i, j], 2);
			for (int j = 0; j < c_cols; j++)
				lengths[j] = Math.Sqrt(lengths[j]);

			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					_values[i, j] /= lengths[j];
		}
		public void NormalizeRows()
		{
			double[] lengths = new double[c_rows];
			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					lengths[i] += Math.Pow(_values[i, j], 2);
			for (int i = 0; i < c_rows; i++)
				lengths[i] = Math.Sqrt(lengths[i]);

			for (int i = 0; i < c_rows; i++)
				for (int j = 0; j < c_cols; j++)
					_values[i, j] /= lengths[i];
		}

		public static Matrix operator +(Matrix m_l, Matrix m_r) { return Matrix.Add(m_l, m_r); }
		public static Matrix operator -(Matrix m_l, Matrix m_r) { return Matrix.Subtract(m_l, m_r); }
		public static Matrix operator *(Matrix m_l, Matrix m_r) { return Matrix.Multiply(m_l, m_r); }
		public static Matrix operator *(double left, Matrix m_r) { return Matrix.Multiply(left, m_r); }
		public static Matrix operator *(Matrix m_l, double right) { return Matrix.Multiply(m_l, right); }
		public static Matrix operator /(Matrix m_l, double right) { return Matrix.Divide(m_l, right); }

#if IMPLICIT_SHALLOW
		public static implicit operator Matrix(double[,] m) { return new Matrix(m); }
		public static implicit operator double[,](Matrix m) { return m._values; }
#else
		public static implicit operator Matrix(double[,] m) { throw new NotImplementedException(); }
		public static implicit operator double[,](Matrix m)
		{
			double[,] result = new double[m.c_rows, m.c_cols];
			for (int i = 0; i < m.c_rows; i++)
				for (int j = 0; j < m.c_cols; j++)
					result[i, j] = m[i, j];
			return result;
		}
#endif

#if ALGLIB
		/// <summary>
		/// Make alglib's SVD nicer by using the above type
		/// </summary>
		public static bool SingularValueDecomposition(Matrix A, out double[] w, out Matrix V)
		{
			w = null;
			V = null;
			double[,] u = null;
			double[,] v = null;

			if (!alglib.svd.rmatrixsvd(A, A.c_rows, A.c_cols, 0, 1, 2, ref w, ref u, ref v))
				return false;
			V = new Matrix(v);
			return true;
		}
#endif

		public String TextDisplay()
		{
			StringBuilder sb = new StringBuilder();
			for (int j = 0; j < c_cols; j++)
				sb.AppendFormat("{0,6}", j);
			sb.AppendLine();
			for (int j = 0; j < c_cols; j++)
				sb.AppendFormat(" -----");
			sb.AppendLine();
			for (int i = 0; i < c_rows; i++)
			{
				for (int j = 0; j < c_cols; j++)
				{
					//String s = String.Format("{0,6:0.00}", _values[i, j]);
					//if (s.EndsWith("0.00"))
					//    s = "      ";
					//int x = (int)(_values[i, j] * 100.0);
					//String s = String.Format("{0,6}", x == 0 ? "" : x.ToString());
					//sb.Append(s);
					var d = _values[i, j];
					String s;
					if (alib.Math.math.IsZero(d))
					{
						s = "";
					}
					else
					{
						s = String.Format("{0:N}", d);
					}
					sb.Append(alib.String._string_ext.TrimEndOrPadLeft(s, 6));
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		private String _DebugString() { return String.Format("{0} rows by {1} columns", c_rows, c_cols); }

		public override String ToString()
		{
			return TextDisplay();
		}
	};
}

