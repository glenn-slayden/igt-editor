using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

using alib.Bits;

namespace alib.IO
{
	using String = System.String;

	public class CopyStream : Stream, IDisposable
	{
		Stream s_in;
		public CopyStream(Stream s_in) { this.s_in = s_in; }
		public override bool CanRead { get { return s_in.CanRead; } }
		public override bool CanSeek { get { return s_in.CanSeek; } }
		public override bool CanWrite { get { return s_in.CanWrite; } }
		public override void Flush() { s_in.Flush(); }
		public override long Length { get { return s_in.Length; } }
		public override long Position { get { return s_in.Position; } set { s_in.Position = value; } }
		public override int Read(byte[] buffer, int offset, int count) { return s_in.Read(buffer, offset, count); }
		public override long Seek(long offset, SeekOrigin origin) { return s_in.Seek(offset, origin); }
		public override void SetLength(long value) { SetLength(value); }
		public override void Write(byte[] buffer, int offset, int count) { Write(buffer, offset, count); }
		void IDisposable.Dispose() { s_in.Dispose(); s_in = null; }
	};

	public class _BinaryWriter : BinaryWriter
	{
		public _BinaryWriter(Stream str)
			: base(str)
		{
		}
		public new void Write7BitEncodedInt(int i)
		{
			base.Write7BitEncodedInt(i);
		}
	};

	public class _BinaryReader : BinaryReader
	{
		public _BinaryReader(Stream str)
			: base(str)
		{
		}
		public new int Read7BitEncodedInt()
		{
			return base.Read7BitEncodedInt();
		}
	};


	public static class _stream_ext
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe bool ContentsEquals(this Stream str, Stream str_other)
		{
			const int bufferSize = 2048 * 2;
			var buffer1 = new byte[bufferSize];
			var buffer2 = new byte[bufferSize];
			fixed (byte* pb1 = buffer1, pb2 = buffer2)
				while (true)
				{
					int c = str.Read(buffer1, 0, bufferSize);
					if (c != str_other.Read(buffer2, 0, bufferSize))
						return false;
					if (c == 0)
						return true;
					c >>= 6;
					for (int i = 0; i < c; i++)
						if (pb1[i] != pb2[i])
							return false;
				}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void AlignQword(this Stream str)
		{
			long pos = str.Position;
			long p2 = (pos + 7) & ~7;
			if (p2 != pos)
			{
				if (str.Length <= p2)
					str.Write(new byte[8 - (pos & 7)]);
				else
					str.Seek(p2, SeekOrigin.Begin);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Write the specified thin string to the memory stream
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void WriteStringAsByteArr(this Stream ms, String thin)
		{
			Byte[] buf = alib.String.Thin._string_thin_ext.ToByteArr(thin);
			ms.Write(buf);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Write(this Stream str, Byte[] buf)
		{
			str.Write(buf, 0, buf.Length);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Image a portion of the specified string to the memory stream in little-endian Unicode byte order,
		/// without creating an intermediate string object. However, Stringbuilder(String, int, int) is faster than this.
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe void Write(this MemoryStream ms, String s, int index, int length)
		{
			if (index < 0 || index + length >= s.Length)
				throw new ArgumentException();
			long idx = ms.Position;
			int cb = length * sizeof(Char);
			ms.Position = idx + cb;
			ms.SetLength(ms.Position);
			fixed (Char* p = s)
			{
				IntPtr ip = new IntPtr(p + index);
				Marshal.Copy(ip, ms.GetBuffer(), (int)idx, cb);
			}
		}

		///////////////////////////////////////////////////////
		/// 
		struct _save_stream_pos : IDisposable
		{
			Stream s;
			long p_cur;
			public _save_stream_pos(Stream s)
			{
				this.s = s;
				this.p_cur = s.Position;
			}
			public void Dispose()
			{
				s.Seek(p_cur, SeekOrigin.Begin);
				s = null;
			}
		};
		///
		///////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Write(this BinaryWriter bw, Guid guid)
		{
			bw.Write(guid.ToByteArray());
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Write(this BinaryWriter bw, DateTime dt)
		{
			bw.Write(dt.ToBinary());
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Write<T>(this BinaryWriter bw, ref T t) where T : struct
		{
			bw.BaseStream.Write(ref t);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static unsafe void Write<T>(this Stream s, ref T t) where T : struct
		{
			int c = Marshal.SizeOf(t);
			byte* pb = stackalloc byte[c];
			Marshal.StructureToPtr(t, (IntPtr)pb, false);
			for (int i = 0; i < c; i++)
				s.WriteByte(*pb++);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void WriteAtOffset(this BinaryWriter bw, long offset, int i)
		{
			using (var ssp = new _save_stream_pos(bw.BaseStream))
			{
				bw.Seek((int)offset, SeekOrigin.Begin);
				bw.Write(i);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void WriteAtOffset(this BinaryWriter bw, long offset, long l)
		{
			using (var ssp = new _save_stream_pos(bw.BaseStream))
			{
				bw.Seek((int)offset, SeekOrigin.Begin);
				bw.Write(l);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void WriteAtOffset(this BinaryWriter bw, long offset, String s)
		{
			using (var ssp = new _save_stream_pos(bw.BaseStream))
			{
				bw.Seek((int)offset, SeekOrigin.Begin);
				bw.Write(s);
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void WriteUnicodeString(this BinaryWriter bw, String s)
		{
			if (String.IsNullOrEmpty(s))
			{
				bw.Write((ushort)0);
				return;
			}
			bw.Write((ushort)s.Length);
			bw.Write(s.ToCharArray(), 0, s.Length);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static void Write67EncodedUint(this BinaryWriter bw, uint i)
		{
			if (_bitarray_ext.OnesCount(i) == 1)
			{
				bw.Write((byte)(0x80 | _bitarray_ext.OnlyBitPosition(i)));
			}
			else
			{
				byte b = (byte)(i & 0x3F);
				i >>= 6;
				if (i == 0)
					bw.Write(b);
				else
				{
					bw.Write((byte)(0x40 | b));
					while (true)
					{
						b = (byte)(i & 0x7F);
						i >>= 7;
						if (i == 0)
						{
							bw.Write(b);
							break;
						}
						bw.Write((byte)(0x80 | b));
					}
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static uint Read67EncodedUint(this BinaryReader br)
		{
			byte b = br.ReadByte();
			if ((b & 0x80) > 0)
				return (uint)1 << (b & 0x1F);
			uint i = (uint)(b & 0x3F);
			if ((b & 0x40) == 0)
				return i;
			int r = 6;
			while (true)
			{
				b = br.ReadByte();
				i |= (uint)(b & 0x7F) << r;
				if ((b & 0x80) == 0)
					return i;
				r += 7;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static Guid ReadGuid(this BinaryReader br)
		{
			return new Guid(br.ReadBytes(16));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Forces unicode encoding, as opposed to the BinaryReader default
		/// </summary>
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static String ReadUnicodeString(this BinaryReader br)
		{
			ushort cch = br.ReadUInt16();
			if (cch == 0)
				return String.Empty;
			return new String(br.ReadChars(cch));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public static DateTime ReadSystemTime(this BinaryReader br)
		{
			return DateTime.FromBinary(br.ReadInt64());
		}
	};
}
