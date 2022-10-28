using System;
using System.Collections.Generic;
using NSChess;

namespace NSProgram
{
	class CEmo
	{
		public int emo = 0;
		public CRec rec = null;

		public CEmo(int e)
		{
			emo = e;
		}

		public CEmo(int e, CRec r)
		{
			emo = e;
			rec = r;
		}

	}

	class CEmoList : List<CEmo>
	{
		readonly static Random rnd = new Random();

		public int GetDepth(int minDepth)
		{
			int depth = 0xffff;
			foreach (CEmo e in this)
				if (depth > e.rec.depth)
					depth = e.rec.depth;
			return depth < 0xffff ? depth:minDepth;
		}

		public bool GetDepth(int mindDepth,out int depth)
		{
			depth = 0xffff;
			foreach (CEmo e in this)
				if ((e.rec.depth >= mindDepth) && (depth > e.rec.depth))
					depth = e.rec.depth;
			return depth < 0xffff;
		}

		public bool GetScore(int minDepth,out int score)
		{
			score = 0;
			if (Count == 0)
				return false;
			CRec rec = this[0].rec;
			if (rec.depth < minDepth)
				return false;
			score = rec.score;
			return true;
		}

		public CEmo GetEmo(int emo)
		{
			foreach (CEmo e in this)
				if (e.emo == emo)
					return e;
			return null;
		}

		public CEmo GetRnd(int rnd = 0)
		{
			if (Count == 0)
				return null;
			if (rnd < 0)
				rnd = 0;
			int i1 = 0;
			int i2 = Count;
			if (rnd <= 100)
				i2 = (Count * rnd) / 100;
			else
				i1 = ((Count - 1) * (rnd - 100)) / 100;
			return this[CChess.random.Next(i1, i2)];
		}

		public void SetUsed(bool used = true)
		{
			foreach (CEmo e in this)
				e.rec.used = used;
		}

		public void Shuffle()
		{
			int n = Count;
			while (n > 1)
			{
				int k = rnd.Next(n--);
				(this[n], this[k]) = (this[k], this[n]);
			}
		}

		public void SortMat()
		{
			Shuffle();
			Sort(delegate (CEmo e1, CEmo e2)
			{
				int del = e2.rec.score - e1.rec.score;
				if (del != 0)
					return del;
				return e2.rec.age - e1.rec.age;
			});
		}

		public void SortFlat()
		{
			Shuffle();
			Sort(delegate (CEmo e1, CEmo e2)
			{
				return e1.rec.depth - e2.rec.depth;
			});
		}

	}
}
