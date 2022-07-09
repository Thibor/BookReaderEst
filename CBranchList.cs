using System;
using System.Collections.Generic;
using System.Linq;

namespace NSProgram
{
	class CBranch
	{
		int index = 0;
		public CEmoList emoList = new CEmoList();

		public bool Fill()
		{
			index = 0;
			emoList = Program.book.GetEmoList(true);
			emoList.Shuffle();
			return emoList.Count > 0;
		}

		public CEmo GetEmo()
		{
			return emoList[index];
		}

		public double GetBit()
		{
			return 1.0 / emoList.Count;
		}

		public double GetProcent()
		{
			return (index * 1.0) / emoList.Count;
		}

		public bool Next()
		{
			if (index < emoList.Count - 1)
			{
				index++;
				return true;
			}
			return false;
		}

	}

	internal class CBranchList : List<CBranch>
	{
		public int used = 0;

		public bool Start()
		{
			used = 0;
			Program.book.chess.SetFen();
			Clear();
			BlFill();
			return Count > 0;
		}

		public void BlFill()
		{
			CBranch branch = new CBranch();
			if (branch.Fill())
			{
				used += branch.emoList.Count;
				Add(branch);
				Program.book.chess.MakeMove(branch.GetEmo().emo);
				if((Program.bookLimitW == 0) || (Program.bookLimitW < Count))
					BlFill();
			}
		}

		public bool BlNext()
		{
			if (Count == 0)
				return false;
			CBranch lastBranch = this.Last();
			CEmo lastEmo = lastBranch.GetEmo();
			Program.book.chess.UnmakeMove(lastEmo.emo);
			if (!lastBranch.Next())
			{
				RemoveAt(Count - 1);
				return BlNext();
			}
			CEmo newEmo = lastBranch.GetEmo();
			Program.book.chess.MakeMove(newEmo.emo);
			BlFill();
			return true;
		}

		public double GetProcent()
		{
			double b1 = Count > 0 ? this[0].GetBit() : 1.0;
			double b2 = Count > 1 ? this[1].GetBit() * b1 : b1;
			double p1 = Count > 0 ? this[0].GetProcent() : 1.0;
			double p2 = Count > 1 ? this[1].GetProcent() * b1 : b1;
			double p3 = Count > 2 ? this[2].GetProcent() * b2 : b2;
			return (p1 + p2 + p3) * 100.0;
		}

		public string GetUci()
		{
			string uci = String.Empty;
			foreach (CBranch branch in this)
			{
				CEmo emo = branch.GetEmo();
				if (emo != null)
				{
					string umo = Program.book.chess.EmoToUmo(emo.emo);
					uci = $"{uci} {umo}";
				}
			}
			return uci.Trim();
		}

		public void SetUsed(bool used = true)
		{
			foreach (CBranch b in this)
				b.emoList.SetUsed(used);
		}


	}

}
