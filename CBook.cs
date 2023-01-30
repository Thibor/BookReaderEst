using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NSProgram
{

	class CBook
	{
		public int errors = 0;
		public int maxRecords = 0;
		public const string defExt = ".est";
		public string path = string.Empty;
		public CChessExt chess = new CChessExt();
		readonly int[] arrAge = new int[0x100];
		readonly CHeader header = new CHeader();
		public CRecList recList = new CRecList();
		readonly CBuffer buffer = new CBuffer();
		readonly Stopwatch stopWatch = new Stopwatch();

		#region file est

		public bool SaveToEst(string p)
		{
			string pt = p + ".tmp";
			RefreshAge();
			int maxAge = GetMaxAge();
			double totalDepth = 0;
			Program.deleted = 0;
			if (maxRecords > 0)
				Program.deleted = recList.Count - maxRecords;
			else if (maxAge == 0xff)
				Program.deleted = AgeAvg() >> 5;
			if (Program.deleted > 0)
				Delete(Program.deleted);
			Program.LogMsg("doubled records", recList.IsDoubled());
			try
			{
				using (FileStream fs = File.Open(pt, FileMode.Create, FileAccess.Write, FileShare.None))
				using (buffer.Bw = new BinaryWriter(fs))
				{
					string lastTnt = new string('-', 64);
					buffer.Write(header.GetHeader());
					foreach (CRec rec in recList)
					{
						totalDepth += rec.depth;
						if (rec.tnt == lastTnt)
						{
							Program.deleted++;
							continue;
						}
						if (rec.age < maxAge)
							rec.age++;
						ulong zip = (ulong)lastTnt.Zip(rec.tnt, (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
						buffer.Write(zip, 6);
						for (int n = (int)zip; n < 64; n++)
						{
							ulong ul = (ulong)TntToInt(rec.tnt[n]);
							if (ul == 0)
								buffer.Write(1, 1);
							else
								buffer.Write(ul, 5);
						}
						buffer.Write(rec.score);
						buffer.Write(rec.age);
						buffer.Write(rec.depth);
						lastTnt = rec.tnt;
					}
					buffer.Write();
				}
			}
			catch
			{
				return false;
			}
			try
			{
				if (File.Exists(p) && File.Exists(pt))
					File.Delete(p);
			}
			catch
			{
				return false;
			}
			try
			{
				if (!File.Exists(p) && File.Exists(pt))
					File.Move(pt, p);
			}
			catch
			{
				return false;
			}
			double depth = totalDepth / recList.Count;
			if (maxAge > 0)
				Program.log.Add($"book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} teacher {Program.teacher.added} oldest {arrAge[0xff]} depth {depth:N2} max {maxAge}");
			return true;
		}

		bool AddFileTnt(string p)
		{
			path = p;
			string pt = p + ".tmp";
			try
			{
				if (!File.Exists(p) && File.Exists(pt))
					File.Move(pt, p);
			}
			catch
			{
				return false;
			}
			if (!File.Exists(p))
				return true;
			try
			{
				using (FileStream fs = File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (buffer.Br = new BinaryReader(fs))
				{
					string lastTnt = new string('-', 64);
					string headerBst = header.GetHeader();
					string headerCur = buffer.ReadString();
					if (Program.isVersion && (headerCur != headerBst))
						Console.WriteLine($"This program only supports version  [{headerBst}]");
					else
					{
						while (buffer.Br.BaseStream.Position != buffer.Br.BaseStream.Length)
						{
							ulong zip = buffer.Read(6);
							byte[] a = new byte[64];
							string tnt = lastTnt.Substring(0, (int)zip);
							for (int n = (int)zip; n < 64; n++)
								if (buffer.Read(1) == 0)
									tnt += IntToTnt((int)buffer.Read(4));
								else
									tnt += '-';
							CRec rec = new CRec
							{
								tnt = tnt,
								score = buffer.ReadInt16(),
								age = buffer.ReadByte(),
								depth = buffer.ReadByte()
							};
							recList.Add(rec);
							lastTnt = rec.tnt;
						}
					}
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		int TntToInt(char tnt)
		{
			return "-aPpNnBbRrQqKkTt".IndexOf(tnt);
		}

		char IntToTnt(int i)
		{
			return "-aPpNnBbRrQqKkTt"[i];
		}

		#endregion file est

		#region file uci

		void AddFileUci(string p)
		{
			string[] lines = File.ReadAllLines(p);
			foreach (string uci in lines)
				AddUci(uci);
		}

		#endregion file uci

		#region file txt

		public bool SaveToTxt(string p)
		{
			int line = 0;
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (CRec rec in recList)
				{
					string l = $"{rec.tnt} {rec.depth} {rec.score:+#;-#;+0}";
					sw.WriteLine(l);
					Console.Write($"\rRecord {++line}");
				}
			}
			Console.WriteLine();
			return true;
		}

		#endregion file txt

		public void ShowMoves(bool last = false)
		{
			Console.Write($"\r{recList.Count} moves");
			if (last)
			{
				Console.WriteLine();
				if (errors > 0)
					Console.WriteLine($"{errors} errors");
				errors = 0;
			}
		}

		int AgeAvg()
		{
			return (recList.Count >> 8) + 1;
		}

		int AgeDel()
		{
			return (AgeAvg() >> 1) + 1;
		}

		int AgeMax()
		{
			return AgeAvg() + AgeDel();
		}

		int AgeMin()
		{
			return AgeAvg() - AgeDel();
		}

		public void Clear()
		{
			recList.Clear();
		}

		public string GetShallow(out int depth)
		{
			depth = int.MaxValue;
			string result = String.Empty;
			chess.SetFen();
			CEmoList el = GetEmoList();
			while (el.Count > 0)
			{
				el.SortFlat();
				CEmo emo = el[0];
				chess.MakeMove(emo.emo);
				if (chess.IsRepetition(0))
					break;
				else
				{
					if (depth > emo.rec.depth)
						depth = emo.rec.depth;
					string umo = chess.EmoToUmo(emo.emo);
					result += $" {umo}";
					el = GetEmoList(emo.rec.score);
				}
			}
			if (result == String.Empty)
				depth = 0;
			return result.Trim();
		}

		public bool LoadFromFile()
		{
			return LoadFromFile(path);
		}

		public bool LoadFromFile(string p)
		{
			if (String.IsNullOrEmpty(p))
				return false;
			stopWatch.Restart();
			recList.Clear();
			bool result = AddFile(p);
			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			Console.WriteLine($"info string Loaded in {ts.Seconds}.{ts.Milliseconds} seconds");
			return result;
		}

		public bool AddFile(string p)
		{
			bool result = true;
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				result = AddFileTnt(p);
			else if (ext == ".uci")
				AddFileUci(p);
			else if (ext == ".pgn")
				AddFilePgn(p);
			return result;
		}

		void AddFilePgn(string p)
		{
			List<string> listPgn = File.ReadAllLines(p).ToList();
			string movesUci = String.Empty;
			chess.SetFen();
			foreach (string m in listPgn)
			{
				string cm = m.Trim();
				if (String.IsNullOrEmpty(cm))
					continue;
				if (cm[0] == '[')
					continue;
				cm = Regex.Replace(cm, @"\.(?! |$)", ". ");
				if (cm.StartsWith("1. "))
				{
					AddUci(movesUci);
					ShowMoves();
					movesUci = String.Empty;
					chess.SetFen();
				}
				string[] arrMoves = cm.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string san in arrMoves)
				{
					if (Char.IsDigit(san[0]))
						continue;
					string umo = chess.SanToUmo(san);
					if (umo == String.Empty)
					{
						errors++;
						break;
					}
					movesUci += $" {umo}";
					int emo = chess.UmoToEmo(umo);
					chess.MakeMove(emo);
				}
			}
			AddUci(movesUci);
			ShowMoves();
		}

		public bool AddFen(string fen)
		{
			if (chess.SetFen(fen))
			{
				CRec rec = new CRec
				{
					tnt = chess.GetTnt()
				};
				recList.AddRec(rec);
				return true;
			}
			return false;
		}

		public static int BitCount(ulong bitboard)
		{
			bitboard -= (bitboard >> 1) & 0x5555555555555555UL;
			bitboard = (bitboard & 0x3333333333333333UL) + ((bitboard >> 2) & 0x3333333333333333UL);
			return (int)(((bitboard + (bitboard >> 4) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
		}

		public bool UpdateBack(string moves, bool mate = false)
		{
			return UpdateBack(moves.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), mate);
		}

		public bool UpdateBack(List<string> moves, bool mate = false)
		{
			return UpdateBack(moves.ToArray(), mate);
		}

		public bool UpdateBack(string[] moves, bool mate = false)
		{
			int up = Program.updated;
			List<CRec> lr = new List<CRec>();
			chess.SetFen();
			foreach (string uci in moves)
				if (chess.MakeMove(uci, out _))
				{
					string tnt = chess.GetTnt();
					CRec rec = recList.GetRec(tnt);
					if (rec != null)
						lr.Add(rec);
					else break;
				}
				else break;

			if (mate)
			{
				int i = moves.Length - lr.Count;
				int m = i + 2;
				for (int n = lr.Count - 1; n >= 0; n--)
				{
					CRec rec = lr[n];
					if ((++i & 1) > 0)
						rec.score = (short)(Constants.CHECKMATE_MAX - m);
					else
						rec.score = (short)(-Constants.CHECKMATE_MAX + m);
					if (m < Constants.CHECKMATE_MAX)
						m++;
				}
			}

			for (int n = lr.Count - 2; n >= 0; n--)
				Program.updated += UpdateRec(lr[n], true);
			return up != Program.updated;
		}

		public int UpdateRec(CRec rec, bool upDepth = false)
		{
			if (rec == null)
				return 0;
			chess.SetTnt(rec.tnt);
			CEmoList emoList = GetEmoList();
			if (emoList.Count == 0)
				return 0;
			CRec bst = emoList[0].rec;
			int depth = bst.depth;
			int score = bst.score;
			if (upDepth)
				depth = emoList.GetMinDepth();
			score = -score;
			if (++depth > 0xff)
				depth = 0xff;
			if (score > 0)
				score--;
			if (score < 0)
				score++;
			if ((rec.depth != depth) || (rec.score != score))
			{
				rec.depth = (byte)depth;
				rec.score = (short)score;
				return 1;
			}
			return 0;
		}

		public bool AddUci(string moves, bool upAge = false, int limitPly = 0, int limitAdd = 0)
		{
			return AddUci(moves.Trim().Split(' '), upAge, limitPly, limitAdd);
		}

		public bool AddUci(List<string> moves, bool upAge = false, int limitPly = 0, int limitAdd = 0)
		{
			return AddUci(moves.ToArray(), upAge, limitPly, limitAdd);
		}

		public bool AddUci(string[] moves, bool upAge = false, int limitPly = 0, int limitAdd = 0)
		{
			int ca = 0;
			if ((limitPly == 0) || (limitPly > moves.Length))
				limitPly = moves.Length;
			chess.SetFen();
			for (int n = 0; n < limitPly; n++)
			{
				string m = moves[n];
				if (chess.MakeMove(m, out _))
				{
					CRec rec = new CRec();
					rec.tnt = chess.GetTnt();
					if (recList.AddRec(rec, upAge))
					{
						Program.added++;
						if ((limitAdd > 0) && (++ca >= limitAdd))
							break;
					}
				}
				else
					return false;
			}
			return true;
		}

		void RefreshAge()
		{
			for (int n = 0; n < 0x100; n++)
				arrAge[n] = 0;
			foreach (CRec rec in recList)
				arrAge[rec.age]++;
		}

		int GetMaxAge()
		{
			int max = AgeMax();
			int last = 0;
			for (int n = 0; n < 0xff; n++)
			{
				int cur = arrAge[n];
				if (last + cur < max)
					return n;
				last = cur;
			}
			return 0xfF;
		}

		public int Delete(int c)
		{
			return recList.RecDelete(c);
		}

		public bool IsWinner(int index, int count)
		{
			return (index & 1) != (count & 1);
		}

		public CEmoList GetNotUsedList(CEmoList el)
		{
			if (el.Count == 0)
				return el;
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _);
			foreach (int m in moves)
			{
				if (el.GetEmo(m) == null)
				{
					CEmo emo = new CEmo(m);
					emoList.Add(emo);
				}
			}
			if (emoList.Count > 0)
				return emoList;
			return el;
		}

		public CEmoList GetEmoList(short score = 0, int repetytion = -1)
		{
			score = Math.Abs(score);
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _, repetytion);
			foreach (int m in moves)
			{
				chess.MakeMove(m);
				string tnt = chess.GetTnt();
				CRec rec = recList.GetRec(tnt);
				if (rec != null)
					if ((Math.Abs(rec.score) >= score) || (chess.move50 == 0))
					{
						CEmo emo = new CEmo(m, rec);
						emoList.Add(emo);
					}
				chess.UnmakeMove(m);
			}
			emoList.SortMat();
			return emoList;
		}

		public string GetMove(string fen, string moves, int rnd, ref bool bookWrite)
		{
			chess.SetFen(fen);
			chess.MakeMoves(moves);
			CEmoList emoList = GetEmoList();
			if (rnd > 200)
			{
				rnd = 100;
				emoList = GetNotUsedList(emoList);
			}
			if (emoList.Count == 0)
				return String.Empty;
			CEmo bst = emoList.GetRnd(rnd);
			chess.MakeMove(bst.emo);
			if (chess.IsRepetition())
			{
				bookWrite = false;
				return String.Empty;
			}
			string umo = chess.EmoToUmo(bst.emo);
			if (bst.rec != null)
			{
				string scFm = bst.rec.score > Constants.CHECKMATE_NEAR ? $"mate {(Constants.CHECKMATE_MAX - bst.rec.score) >> 1}" : (bst.rec.score < -Constants.CHECKMATE_NEAR ? $"mate {(-Constants.CHECKMATE_MAX - bst.rec.score) >> 1}" : $"cp {bst.rec.score}");
				Console.WriteLine($"info score {scFm} depth {bst.rec.depth}");
				Console.WriteLine($"info string book {umo} {scFm} depth {bst.rec.depth} possible {emoList.Count} age {bst.rec.age}");
			}
			return umo;
		}

		public void ShowLevel(int lev)
		{
			int ageMax = AgeMax();
			int ageMin = AgeMin();
			int ageCou = arrAge[lev];
			int del = 0;
			if (ageCou < ageMin)
				del = ageCou - ageMin;
			if (ageCou > ageMax)
				del = ageCou - ageMax;
			Console.WriteLine("{0,4} {1,5} {2,5}", lev, arrAge[lev], del);
		}

		public void InfoStructure()
		{
			int ageAvg = AgeAvg();
			int ageMax = AgeMax();
			int ageMin = AgeMin();
			int ageDel = AgeDel();
			Console.WriteLine($"moves {recList.Count:N0} min {ageMin:N0} avg {ageAvg:N0} max {ageMax:N0} delta {ageDel:N0}");
			Console.WriteLine("{0,4} {1,5} {2,5}", "age", "count", "delta");
			Console.WriteLine();
			RefreshAge();
			ShowLevel(0);
			for (int n = 1; n < 0xff; n++)
			{
				if ((arrAge[n] > ageMax) || (arrAge[n] < ageMin))
					ShowLevel(n);
				if (arrAge[n] == 0)
					break;
			}
			ShowLevel(255);
		}

		public void InfoMoves(string moves = "")
		{
			chess.SetFen();
			if (!chess.MakeMoves(moves))
				Console.WriteLine("wrong moves");
			else
			{
				CEmoList el = GetEmoList();
				if (el.Count == 0)
					Console.WriteLine("no moves found");
				else
				{
					Console.WriteLine("id move  score depth age");
					Console.WriteLine();
					int i = 1;
					foreach (CEmo e in el)
					{
						string umo = chess.EmoToUmo(e.emo);
						Console.WriteLine(String.Format("{0,2} {1,-4} {2,6} {3,5} {4,3}", i++, umo, e.rec.score, e.rec.depth, e.rec.age));
					}
				}
			}
		}

		public void ShowInfo()
		{
			InfoMoves();
		}

		public void Update()
		{
			Program.added = 0;
			Program.updated = 0;
			Program.deleted = 0;
			int up = recList.Count;
			int max;
			CRecList rl = new CRecList();
			foreach (CRec rec in recList)
				rl.Add(rec);
			rl.SortDepth();
			do
			{
				int line = 0;
				max = up;
				up = 0;
				foreach (CRec rec in rl)
				{
					up += UpdateRec(rec);
					Console.Write($"\rupdate {(++line * 100.0 / recList.Count):N4}%");
				}
				Program.updated += up;
				Console.WriteLine();
				Console.WriteLine($"Updated {up:N0}");
				SaveToFile();
			} while ((max > up) && (up > 0));
			Console.WriteLine($"records {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0}");
		}

		#region save

		public bool SaveToFile(string p)
		{
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				return SaveToEst(p);
			if (ext == ".uci")
				return SaveToUci(p);
			if (ext == ".pgn")
				return SaveToPgn(p);
			if (ext == ".txt")
				return SaveToTxt(p);
			return false;
		}

		public void SaveToFile()
		{
			if (!string.IsNullOrEmpty(path))
				SaveToFile(path);
		}

		public bool SaveToUci(string p)
		{
			List<string> sl = GetGames();
			using (FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None))
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (string uci in sl)
					sw.WriteLine(uci);
			}
			return true;
		}

		public bool SaveToPgn(string p)
		{
			List<string> sl = GetGames();
			int line = 0;
			using (FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None))
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (String uci in sl)
				{
					string[] arrMoves = uci.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					chess.SetFen();
					string pgn = String.Empty;
					foreach (string umo in arrMoves)
					{
						string san = chess.UmoToSan(umo);
						if (san == String.Empty)
							break;
						int number = (chess.halfMove >> 1) + 1;
						if (chess.whiteTurn)
							pgn += $" {number}. {san}";
						else
							pgn += $" {san}";
						int emo = chess.UmoToEmo(umo);
						chess.MakeMove(emo);
					}
					sw.WriteLine();
					sw.WriteLine("[White \"White\"]");
					sw.WriteLine("[Black \"Black\"]");
					sw.WriteLine();
					sw.WriteLine(pgn.Trim());
					Console.Write($"\rgames {++line}");
				}
			}
			Console.WriteLine();
			return true;
		}

		List<string> GetGames()
		{
			List<string> sl = new List<string>();
			GetGames(string.Empty, 0, 0, 0, 1, ref sl);
			Console.WriteLine();
			Console.WriteLine("finish");
			Console.Beep();
			sl.Sort();
			return sl;
		}

		bool GetGames(string moves, int ply, short score, double proT, double proU, ref List<string> list)
		{
			bool add = true;
			if (ply < 12)
			{
				chess.SetFen();
				chess.MakeMoves(moves);
				if (chess.IsRepetition(0))
					return false;
				CEmoList el = GetEmoList(score);
				if (el.Count > 0)
				{
					proU /= el.Count;
					for (int n = 0; n < el.Count; n++)
					{
						CEmo emo = el[n];
						double p = proT + n * proU;
						if (GetGames($"{moves} {chess.EmoToUmo(emo.emo)}".Trim(), ply + 1, emo.rec.score, p, proU, ref list))
							add = false;
					}
				}
			}
			if (add)
			{
				list.Add(moves);
				double pro = (proT + proU) * 100.0;
				Console.Write($"\r{pro:N4} %");
			}
			return true;
		}

		#endregion save

	}
}
