using System;
using System.Collections.Generic;

namespace NSProgram
{
	class CRec
	{
		public short score = 0;
		public byte age = 0;
		public byte depth = 0;
		public string tnt = String.Empty;

	}

	class CRecList : List<CRec>
	{
		readonly static Random rnd = new Random();

		public bool AddRec(CRec rec, bool upAge = false)
		{
			int index = FindTnt(rec.tnt);
			if (index == Count)
				Add(rec);
			else
			{
				CRec r = this[index];
				if (r.tnt == rec.tnt)
				{
					if (upAge)
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
				SortAge();
				RemoveRange(Count - count, count);
				SortTnt();
			}
			return c - Count;
		}

		public int DeleteDoubled()
		{
			int result = 0;
			string last = String.Empty;
			for(int n=Count-1;n>=0;n--)
			{
				CRec rec = this[n];
				if (rec.tnt == last)
				{
					result++;
					RemoveAt(n);
				}
				last = rec.tnt;
			}
			return result;
		}

		public bool IsDoubled()
		{
			return DeleteDoubled() > 0;
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

		public CRec RecFlat()
		{
			if (Count == 0)
				return null;
			CRec bst = this[0];
			foreach (CRec rec in this)
				if ((bst.depth > rec.depth) || ((bst.depth == rec.depth) && (bst.age > rec.age)))
					bst = rec;
			return bst;
		}

		public CRec GetRec()
		{
			int index = rnd.Next(Count);
			if (index < Count)
				return this[index];
			return null;
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

		public bool IsSorted()
		{
			for (int n = 0; n < Count - 1; n++)
			{
				string t1 = this[n].tnt;
				string t2 = this[n+1].tnt;
				if (String.Compare(t1,t2, StringComparison.Ordinal) > 0)
				{
					Console.WriteLine($"sort fail record {n} count {SortFail()}");
					Console.WriteLine(t1);
					Console.WriteLine(t2);
					return false;
				}
			}
			Console.WriteLine("sort ok");
			return true;
		}

		public int SortFail()
		{
			int result = 0;
			for (int n = 0; n < Count - 1; n++)
			{
				string t1 = this[n].tnt;
				string t2 = this[n + 1].tnt;
				if (String.Compare(t1, t2, StringComparison.Ordinal) > 0)
					result++;
			}
			return result;
		}

		public void SortRnd()
		{
			int n = Count;
			while (n > 1)
			{
				int k = rnd.Next(n--);
				(this[n], this[k]) = (this[k], this[n]);
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
			SortRnd();
			Sort(delegate (CRec r1, CRec r2)
			{
				return r1.age - r2.age;
			});
		}

		public void SortDepth()
		{
			SortRnd();
			Sort(delegate (CRec r1, CRec r2)
			{
				return r1.depth - r2.depth;
			});
		}


	}
}
