﻿using System;
using System.Collections.Generic;

namespace NSProgram
{
	class CRec
	{
		public bool used = false;
		public short score = 0;
		public byte age = 0;
		public byte depth = 0;
		public string tnt = String.Empty;

	}

	class CRecList : List<CRec>
	{
		readonly static Random rnd = new Random();

		public bool AddRec(CRec rec, bool age = false)
		{
			int index = FindTnt(rec.tnt);
			if (index == Count)
				Add(rec);
			else
			{
				CRec r = this[index];
				if (r.tnt == rec.tnt)
				{
					if (age)
						r.age = rec.age;
					return false;
				}
				else
					Insert(index, rec);
			}
			return true;
		}

		public int RecDelete(int count)
		{
			if (count <= 0)
				return 0;
			int c = Count;
			if (count >= Count)
				Clear();
			else
			{
				Shuffle();
				SortAge();
				RemoveRange(Count - count, count);
				SortTnt();
			}
			return c - Count;
		}

		public int DeleteNotUsed()
		{
			int del = 0;
			Shuffle();
			SortAge();
			for (int n = Count - 1; n >= 0; n--)
			{
				CRec rec = this[n];
				if (rec.age < 0xff)
					break;
				if (rec.used)
					continue;
				RemoveAt(n);
				del++;
			}
			SortTnt();
			return del;
		}

		public int FindTnt(string tnt)
		{
			int first = -1;
			int last = Count;
			while (true)
			{
				if (last - first == 1)
					return last;
				int middle = (first + last) >> 1;
				CRec rec = this[middle];
				int c = String.Compare(tnt, rec.tnt, StringComparison.Ordinal);
				if (c < 0)
					last = middle;
				else if (c > 0)
					first = middle;
				else
					return middle;
			}
		}

		public CRec RecRnd()
		{
			int index = rnd.Next(Count);
			if (index < Count)
				return this[index];
			return null;
		}

		public CRec RecBst()
		{
			CRec bst = null;
			int depth = 0xffff;
			foreach (CRec rec in this)
				if (depth > rec.depth)
				{
					depth = rec.depth;
					bst = rec;
					if (depth == 0)
						return bst;
				}
			return bst;
		}

		public CRec GetRec(string tnt)
		{
			int index = FindTnt(tnt);
			if (index < Count)
				if (this[index].tnt == tnt)
					return this[index];
			return null;
		}

		public void DelTnt(string tnt)
		{
			if (IsTnt(tnt, out int index))
				RemoveAt(index);
		}

		public bool IsTnt(string tnt, out int index)
		{
			index = FindTnt(tnt);
			if (index < Count)
				return this[index].tnt == tnt;
			return false;
		}

		public void SetUsed(bool u)
		{
			foreach (CRec rec in this)
				rec.used = u;
		}

		public int GetUsed()
		{
			int used = 0;
			foreach (CRec rec in this)
				if (rec.used)
					used++;
			return used;
		}

		public void Shuffle()
		{
			int n = Count;
			while (n > 1)
			{
				int k = rnd.Next(n--);
				CRec value = this[k];
				this[k] = this[n];
				this[n] = value;
			}
		}

		public void SortTnt()
		{
			Sort(delegate (CRec r1, CRec r2)
			{
				return String.Compare(r1.tnt, r2.tnt, StringComparison.Ordinal);
			});
		}

		public void SortAge()
		{
			Sort(delegate (CRec r1, CRec r2)
			{
				return r1.age - r2.age;
			});
		}


	}
}
