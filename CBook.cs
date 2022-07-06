using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NSChess;
using RapLog;

namespace NSProgram
{

	class CBook
	{
		string path = String.Empty;
		public int errors = 0;
		public int maxRecords = 0;
		public const string name = "BookReaderEst";
		public const string version = "2022-07-04";
		public string fileShortName = String.Empty;
		string fileDirectory = String.Empty;
		public const string defExt = ".est";
		public CChessExt chess = new CChessExt();
		readonly int[] arrAge = new int[0x100];
		public CRecList recList = new CRecList();
		public CBranchList branchList = new CBranchList();
		public CRapLog log = new CRapLog();

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
			return (AgeAvg() >> 3) + 1;
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

		string GetHeader()
		{
			return $"{name} {version}";
		}

		string GetBookFile()
		{
			return $"{fileShortName}{defExt}";
		}

		string GetBookPath()
		{
			return $@"{fileDirectory}{GetBookFile()}";
		}

		public string GetFlat()
		{
			string result = String.Empty;
			chess.SetFen();
			recList.SetUsed(false);
			CEmoList el = GetEmoList(true);
			while (el.Count > 0)
			{
				el.SortFlat();
				CEmo emo = el[0];
				chess.MakeMove(emo.emo);
				string umo = chess.EmoToUmo(emo.emo);
				result += $" {umo}";
				el = GetEmoList(true);
			}
			return result.Trim();
		}

		public bool LoadFromFile(string p)
		{
			path = p;
			fileDirectory = Path.GetDirectoryName(p);
			if (fileDirectory != String.Empty)
				fileDirectory = $@"{fileDirectory}\";
			fileShortName = Path.GetFileNameWithoutExtension(p);
			recList.Clear();
			return AddFile(p);
		}

		public bool LoadFromFile()
		{
			return LoadFromFile(path);
		}

		public bool AddFile(string p)
		{
			bool result = true;
			if (!File.Exists(p) && (!File.Exists(p + ".tmp")))
				return true;
			if (String.IsNullOrEmpty(fileShortName))
				fileShortName = Path.GetFileNameWithoutExtension(p);
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				result = AddFileTnt(p);
			else if (ext == ".uci")
				AddFileUci(p);
			else if (ext == ".pgn")
				AddFilePgn(p);
			Console.WriteLine($"info string moves {recList.Count:N0}");
			return result;
		}

		ulong[] TntToHash(string tnt)
		{
			int z = 0;
			ulong[] hash = new ulong[4];
			for (int h = 0; h < 4; h++)
				for (int n = 0; n < 16; n++)
				{
					ulong p = 0;
					switch (tnt[z++])
					{
						case 'a':
							p = 1;
							break;
						case 'P':
							p = 2;
							break;
						case 'p':
							p = 3;
							break;
						case 'N':
							p = 4;
							break;
						case 'n':
							p = 5;
							break;
						case 'B':
							p = 6;
							break;
						case 'b':
							p = 7;
							break;
						case 'R':
							p = 8;
							break;
						case 'r':
							p = 9;
							break;
						case 'Q':
							p = 10;
							break;
						case 'q':
							p = 11;
							break;
						case 'K':
							p = 12;
							break;
						case 'k':
							p = 13;
							break;
						case 'T':
							p = 14;
							break;
						case 't':
							p = 15;
							break;
					}
					hash[h] <<= 4;
					hash[h] |= p;
				}
			return hash;
		}

		string HashToTnt(ulong[] hash)
		{
			string tnt = String.Empty;
			foreach (ulong h in hash)
				for (int n = 15; n >= 0; n--)
				{
					ulong p = (h >> (n << 2)) & 0xf;
					switch (p)
					{
						case 0:
							tnt += "-";
							break;
						case 1:
							tnt += "a";
							break;
						case 2:
							tnt += "P";
							break;
						case 3:
							tnt += "p";
							break;
						case 4:
							tnt += "N";
							break;
						case 5:
							tnt += "n";
							break;
						case 6:
							tnt += "B";
							break;
						case 7:
							tnt += "b";
							break;
						case 8:
							tnt += "R";
							break;
						case 9:
							tnt += "r";
							break;
						case 10:
							tnt += "Q";
							break;
						case 11:
							tnt += "q";
							break;
						case 12:
							tnt += "K";
							break;
						case 13:
							tnt += "k";
							break;
						case 14:
							tnt += "T";
							break;
						case 15:
							tnt += "t";
							break;
					}
				}
			return tnt;
		}

		bool AddFileTnt(string p)
		{
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
			try
			{
				using (FileStream fs = File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (BinaryReader reader = new BinaryReader(fs))
					{
						string header = GetHeader();
						if (reader.ReadString() != header)
							Console.WriteLine($"This program only supports version  [{header}]");
						else
						{
							while (reader.BaseStream.Position != reader.BaseStream.Length)
							{
								ulong[] hash = new ulong[4];
								for (int n = 0; n < hash.Length; n++)
									hash[n] = ReadUInt64(reader);
								CRec rec = new CRec
								{
									tnt = HashToTnt(hash),
									score = ReadInt16(reader),
									age = reader.ReadByte(),
									depth = reader.ReadByte()
								};
								recList.Add(rec);
							}
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

		void AddFileUci(string p)
		{
			string[] lines = File.ReadAllLines(p);
			foreach (string uci in lines)
				AddUci(uci);
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

		public int UpdateBack(string moves)
		{
			return UpdateBack(moves.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
		}

		public int UpdateBack(List<string> moves)
		{
			return UpdateBack(moves.ToArray());
		}

		public int UpdateBack(string[] moves)
		{

			int result = 0;
			List<CRec> lr = new List<CRec>();
			chess.SetFen();
			foreach (string uci in moves)
				if (chess.MakeMove(uci, out _))
				{
					string tnt = chess.GetTnt();
					CRec rec = recList.GetRec(tnt);
					if (rec != null)
						lr.Add(rec);
					else return 0;
				}
				else return 0;
			for (int n = lr.Count - 2; n >= 0; n--)
				result += UpdateRec(lr[n]);
			return result;
		}

		public int UpdateRec(CRec rec)
		{
			if (rec == null)
				return 0;
			chess.SetTnt(rec.tnt);
			CEmoList emoList = GetEmoList();
			if (emoList.Count > 0)
			{
				byte depth = emoList.GetDepth();
				if (depth > 0)
				{
					int score = -emoList.GetScore();
					if (depth < 0xff)
						depth++;
					if ((rec.score != score) || (rec.depth != depth))
					{
						rec.score = (short)score;
						rec.depth = depth;
						return 1;
					}
				}
			}
			return 0;
		}

		public int AddUci(string moves, bool age = false, int limitLen = 0, int limitAdd = 0)
		{
			return AddUci(moves.Trim().Split(' '), age, limitLen, limitAdd);
		}

		public int AddUci(List<string> moves, bool age = false, int limitLen = 0, int limitAdd = 0)
		{
			return AddUci(moves.ToArray(), age, limitLen, limitAdd);
		}

		public int AddUci(string[] moves, bool age = false, int limitLen = 0, int limitAdd = 0)
		{
			int ca = 0;
			if ((limitLen == 0) || (limitLen > moves.Length))
				limitLen = moves.Length;
			chess.SetFen();
			for (int n = 0; n < limitLen; n++)
			{
				string m = moves[n];
				if (chess.MakeMove(m, out _))
				{
					CRec rec = new CRec
					{
						tnt = chess.GetTnt()
					};
					if (recList.AddRec(rec, age))
						ca++;
					if ((limitAdd > 0) && (ca >= limitAdd))
						break;
				}
				else
					break;
			}
			return ca;
		}

		void RefreshAge()
		{
			for (int n = 0; n < 0x100; n++)
				arrAge[n] = 0;
			foreach (CRec rec in recList)
				arrAge[rec.age]++;
		}

		void WriteUInt64(BinaryWriter writer, ulong v)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			writer.Write(bytes);
		}

		ulong ReadUInt64(BinaryReader reader)
		{
			ulong v = reader.ReadUInt64();
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(v).Reverse().ToArray();
				return BitConverter.ToUInt64(bytes, 0);
			}
			return v;
		}

		void WriteInt16(BinaryWriter writer, short v)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			writer.Write(bytes);
		}

		short ReadInt16(BinaryReader reader)
		{
			short v = reader.ReadInt16();
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(v).Reverse().ToArray();
				return BitConverter.ToInt16(bytes, 0);
			}
			return v;
		}

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
			SaveToFile(GetBookPath());
		}

		public bool SaveToUci(string p)
		{
			List<string> sl = GetGames(Program.bookLimitW);
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (String uci in sl)
					sw.WriteLine(uci);
			}
			return true;
		}

		public bool SaveToTxt(string p)
		{
			int line = 0;
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (CRec rec in recList)
				{
					string l = $"{rec.tnt}{rec.score:+#;-#;+0}";
					sw.WriteLine(l);
					Console.Write($"\rRecord {++line}");
				}
			}
			Console.WriteLine();
			return true;
		}

		public bool SaveToEst(string p)
		{
			string pt = p + ".tmp";
			RefreshAge();
			int maxAge = GetMaxAge();
			int empty = 0;
			Program.deleted = 0;
			if (maxRecords > 0)
				Program.deleted = recList.Count - maxRecords;
			else if (maxAge == 0xff)
				Program.deleted = AgeAvg() >> 5;
			if (Program.deleted > 0)
				Delete(Program.deleted);
			try
			{
				using (FileStream fs = File.Open(pt, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					using (BinaryWriter writer = new BinaryWriter(fs))
					{
						string lastTnt = String.Empty;
						recList.SortTnt();
						writer.Write(GetHeader());
						foreach (CRec rec in recList)
						{
							if (rec.depth == 0)
								empty++;
							if (rec.tnt == lastTnt)
							{
								Program.deleted++;
								continue;
							}
							ulong[] hash = TntToHash(rec.tnt);
							if (rec.age < maxAge)
								rec.age++;
							foreach (ulong h in hash)
								WriteUInt64(writer, h);
							WriteInt16(writer, rec.score);
							writer.Write(rec.age);
							writer.Write(rec.depth);
							lastTnt = rec.tnt;
						}
					}
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
			if (Program.deleted > 0)
				Console.WriteLine($"log book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0}");
			if (Program.isLog && (maxAge > 0))
				log.Add($"book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} empty {empty} max {maxAge}");
			return true;
		}

		public bool SaveToPgn(string p)
		{
			List<string> sl = GetGames();
			int line = 0;
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
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
						int number = (chess.g_moveNumber >> 1) + 1;
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

		public CEmoList GetEmoList(bool onlyNotUsed = false, int repetytion = -1)
		{
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _, repetytion);
			foreach (int m in moves)
			{
				chess.MakeMove(m);
				string tnt = chess.GetTnt();
				CRec rec = recList.GetRec(tnt);
				if (rec != null)
					if (!onlyNotUsed || !rec.used)
					{
						if (onlyNotUsed)
							rec.used = true;
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
			Console.WriteLine($"info score cp {bst.rec.score} depth {bst.rec.depth}");
			Console.WriteLine($"info string book {umo} {bst.rec.score:+#;-#;0} depth {bst.rec.depth} possible {emoList.Count} age {bst.rec.age}");
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

		public void InfoMoves(string moves)
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

		List<string> GetGames(int limit = 0)
		{
			List<string> sl = new List<string>();
			if (branchList.Start(limit))
				do
				{
					string uci = branchList.GetUci();
					sl.Add(uci);
					Console.Write($"\rsearch {branchList.GetProcent():N4}%");
				} while (branchList.BlNext());
			double pro = (branchList.used * 100.0) / recList.Count;
			Console.WriteLine();
			Console.WriteLine($"games {sl.Count:N0} used {branchList.used:N0} ({pro:N2}%)");
			sl.Sort();
			return sl;
		}

		public void Update()
		{
			Program.added = 0;
			Program.updated = 0;
			Program.deleted = 0;
			int up = recList.Count;
			int max;
			int zero;
			do
			{
				int line = 0;
				max = up;
				up = 0;
				zero = 0;
				foreach (CRec rec in recList)
				{
					up += UpdateRec(rec);
					if (rec.depth == 0)
						zero++;
					Console.Write($"\rupdate {(++line * 100.0 / recList.Count):N4}%");
				}
				Program.updated += up;
				Console.WriteLine();
				Console.WriteLine($"Updated {up:N0}");
				SaveToFile();
			} while ((max > up) && (up > 0));
			Console.WriteLine($"records {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} zero {zero}");
		}

	}
}
