using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NSProgram
{
	internal class CBuffer
	{
		int left = 0;
		ulong buffer = 0;
		BinaryWriter bw = null;
		BinaryReader br = null;

		public BinaryWriter Bw
		{
			get
			{
				return bw;
			}
			set
			{
				bw = value;
				left = 64;
				buffer = 0;
			}
		}

		public BinaryReader Br
		{
			get
			{
				return br;
			}
			set
			{
				br = value;
				left = 0;
				buffer = 0;
			}
		}

		void ReadBuffer()
		{
			left = 64;
			buffer = br.ReadUInt64();
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(buffer).Reverse().ToArray();
				buffer = BitConverter.ToUInt64(bytes, 0);
			}
		}

		public string ReadString()
		{
			string result = String.Empty;
			while (true)
			{
				char c = (char)Read(8);
				if (c == char.MinValue)
					break;
				result += c;

			}
			return result;
		}

		public byte ReadByte()
		{
			return (byte)Read(8);
		}

		public short ReadInt16()
		{
			return (short)Read(16);
		}

		public ulong ReadUInt64()
		{
			return Read(64);
		}

		void WriteBuffer()
		{
			if (left == 64)
				return;
			byte[] bytes = BitConverter.GetBytes(buffer);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			bw.Write(bytes);
			left = 64;
			buffer = 0;
		}

		public void Write()
		{
			buffer <<= left;
			WriteBuffer();
		}

		public void Write(string s)
		{
			foreach (char c in s)
				Write(c);
			Write('\0');
		}

		public void Write(byte b)
		{
			ulong ul = b;
			Write(ul, 8);
		}

		public void Write(char c)
		{
			ulong ul = c;
			Write(ul, 8);
		}

		public void Write(short s)
		{
			ulong ul = (ulong)s;
			Write(ul, 16);
		}

		public void Write(ulong ul)
		{
			Write(ul, 64);
		}

		public void Write(ulong v, int c)
		{
			if (c == 0)
				return;
			if (c <= left)
			{
				ulong mask = Constants.ul >> (64 - c);
				buffer = (buffer << c) | (v & mask);
				left -= c;
				if (left == 0)
					WriteBuffer();
			}
			else
			{
				int l2 = c - left;
				ulong v1 = v >> l2;
				Write(v1, left);
				left = 64;
				buffer = 0;
				Write(v, l2);
			}
		}

		public ulong Read(int c)
		{
			if (left >= c)
			{
				left -= c;
				ulong result = buffer >> (64 - c);
				buffer <<= c;
				return result;
			}
			int l2 = c - left;
			ulong b1 = buffer >> (64 - c);
			ulong mask = Constants.ul << l2;
			ReadBuffer();
			return (b1&mask) | Read(l2);
		}

	}
}
