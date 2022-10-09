using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NSUci;

namespace NSProgram
{
	class Program
	{
		/// <summary>
		/// Book can write log file.
		/// </summary>
		public static bool isLog = false;
		public static int added = 0;
		public static int updated = 0;
		public static int deleted = 0;
		/// <summary>
		/// Moves added to book per game.
		/// </summary>
		public static int bookAdd = 5;
		/// <summary>
		/// Limit ply to wrtie.
		/// </summary>
		public static int bookLimitW = 32;
		/// <summary>
		/// Limit ply to read.
		/// </summary>
		public static int bookLimitR = 0xf;
		public static bool isIv = false;
		public static CBook book = new CBook();

		static void Main(string[] args)
		{
			bool bookChanged = false;
			bool bookWrite = false;
			bool isInfo = false;
			/// <summary>
			/// Book can update moves.
			/// </summary>
			bool isW = false;
			/// <summary>
			/// Number of moves not found in a row.
			/// </summary>
			int emptyRow = 0;
			int emptyTotal = 0;
			/// <summary>
			/// Random moves factor.
			/// </summary>
			int bookRandom = 60;
			string lastFen = String.Empty;
			string lastMoves = String.Empty;
			CUci Uci = new CUci();
			CTeacher teacher = new CTeacher();
			string ax = "-bn";
			List<string> listBn = new List<string>();
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			List<string> listTf = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-add"://moves add to book
					case "-bn"://book name
					case "-ef"://engine file
					case "-ea"://engine arguments
					case "-rnd"://random moves
					case "-lr"://limit read in half moves
					case "-lw"://limit write in half moves
					case "-tf"://teacher file
						ax = ac;
						break;
					case "-log"://add log
						ax = ac;
						isLog = true;
						break;
					case "-w"://writable
						ax = ac;
						isW = true;
						break;
					case "-info":
						ax = ac;
						isInfo = true;
						break;
					case "-iv":
						ax = ac;
						isIv = true;
						break;
					default:
						switch (ax)
						{
							case "-bn":
								listBn.Add(ac);
								break;
							case "-ef":
								listEf.Add(ac);
								break;
							case "-tf":
								listTf.Add(ac);
								break;
							case "-ea":
								listEa.Add(ac);
								break;
							case "-w":
								ac = ac.Replace("K", "000").Replace("M", "000000");
								book.maxRecords = int.TryParse(ac, out int m) ? m : 0;
								break;
							case "-add":
								bookAdd = int.TryParse(ac, out int a) ? a : bookAdd;
								break;
							case "-rnd":
								bookRandom = int.TryParse(ac, out int r) ? r : 0;
								break;
							case "-lr":
								bookLimitR = int.TryParse(ac, out int lr) ? lr : 0;
								break;
							case "-lw":
								bookLimitW = int.TryParse(ac, out int lw) ? lw : 0;
								break;
						}
						break;
				}
			}
			string bookName = String.Join(" ", listBn);
			string engineFile = String.Join(" ", listEf);
			string teacherFile = String.Join(" ", listTf);
			string arguments = String.Join(" ", listEa);
			string ext = Path.GetExtension(bookName);
			Console.WriteLine($"info string book {CBook.name} ver {CBook.version}");
			if (String.IsNullOrEmpty(ext))
				bookName = $"{bookName}{CBook.defExt}";
			bool bookLoaded = book.LoadFromFile(bookName);
			if (bookLoaded)
			{
				Console.WriteLine($"info string book on");
				if (isW)
					Console.WriteLine($"info string write on");
			}
			else
				isW = false;
			Process engineProcess = null;
			if (File.Exists(engineFile))
			{
				engineProcess = new Process();
				engineProcess.StartInfo.FileName = engineFile;
				engineProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
				engineProcess.StartInfo.UseShellExecute = false;
				engineProcess.StartInfo.RedirectStandardInput = true;
				engineProcess.StartInfo.Arguments = arguments;
				engineProcess.Start();
				Console.WriteLine($"info string engine on");
			}
			else if (engineFile != String.Empty)
				Console.WriteLine($"info string missing engine  [{engineFile}]");


			if (teacher.SetTeacher(teacherFile))
				Console.WriteLine($"info string teacher on");

			if (bookLoaded && teacher.enabled)
			{
				isW = true;
				bookRandom = 0;
				bookLimitR = 0;
				bookLimitW = 0;
			}
			if (isInfo)
				book.InfoMoves();
			do
			{
				string msg = Console.ReadLine().Trim();
				if (String.IsNullOrEmpty(msg) || (msg == "help") || (msg == "book"))
				{
					Console.WriteLine("book load [filename].[mem|pgn|uci|fen] - clear and add moves from file");
					Console.WriteLine("book save [filename].[mem] - save book to the file");
					Console.WriteLine("book delete [number x] - delete x moves from the book");
					Console.WriteLine("book addfile [filename].[mem|png|uci|fen] - add moves to the book from file");
					Console.WriteLine("book adduci [uci] - add moves in uci format to the book");
					Console.WriteLine("book addfen [fen] - add position in fen format");
					Console.WriteLine("book clear - clear all moves from the book");
					Console.WriteLine("book moves [uci] - make sequence of moves in uci format and shows possible continuations");
					Console.WriteLine("book structure - show structure of current book");
					continue;
				}
				Uci.SetMsg(msg);
				int count = book.recList.Count;
				if (Uci.command == "book")
				{
					switch (Uci.tokens[1])
					{
						case "addfen":
							if (book.AddFen(Uci.GetValue(2, 0)))
								Console.WriteLine("Fen have been added");
							else
								Console.WriteLine("Wrong fen");
							break;
						case "addfile":
							string fn = Uci.GetValue(2, 0);
							if (File.Exists(fn))
							{
								book.AddFile(fn);
								book.ShowMoves(true);
							}
							else Console.WriteLine("File not found");
							break;
						case "adduci":
							book.AddUci(Uci.GetValue(2, 0));
							Console.WriteLine($"{(book.recList.Count - count):N0} moves have been added");
							break;
						case "clear":
							book.Clear();
							Console.WriteLine("Book is empty");
							break;
						case "delete":
							int c = book.Delete(Uci.GetInt(2));
							Console.WriteLine($"{c:N0} moves was deleted");
							break;
						case "load":
							book.LoadFromFile(Uci.GetValue(2, 0));
							book.ShowMoves(true);
							break;
						case "moves":
							book.InfoMoves(Uci.GetValue(2, 0));
							break;
						case "structure":
							book.InfoStructure();
							break;
						case "update":
							book.Update();
							break;
						case "save":
							if (book.SaveToFile(Uci.GetValue(2, 0)))
								Console.WriteLine("The book has been saved");
							else
								Console.WriteLine("Writing to the file has failed");
							break;
						default:
							Console.WriteLine($"Unknown command [{Uci.tokens[1]}]");
							break;
					}
					continue;
				}
				if ((Uci.command != "go") && (engineProcess != null))
					engineProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "position":
						lastFen = Uci.GetValue("fen", "moves");
						lastMoves = Uci.GetValue("moves", "fen");
						book.chess.SetFen(lastFen);
						book.chess.MakeMoves(lastMoves);
						if (String.IsNullOrEmpty(lastFen))
						{
							if (book.chess.g_moveNumber < 2)
							{
								bookChanged = false;
								bookWrite = isW;
								emptyRow = 0;
								emptyTotal = 0;
								added = 0;
								updated = 0;
								deleted = 0;
							}
							if (bookLoaded && isW)
								if (book.chess.Is2ToEnd(out string myMove, out string enMove))
								{
									string movesUci = $"{lastMoves} {myMove} {enMove}";
									if (bookWrite)
										book.AddUci(movesUci, true, 0, bookAdd);
									book.UpdateBack(movesUci, true);
									bookChanged = true;
									teacher.Stop();
								}
								else if (book.chess.Is1ToEnd())
								{
									if (book.UpdateBack(lastMoves))
										bookChanged = true;
									teacher.Stop();
								}
						}
						break;
					case "go":
						string move = String.Empty;
						if ((bookLimitR == 0) || (bookLimitR > book.chess.g_moveNumber))
							move = book.GetMove(lastFen, lastMoves, bookRandom, ref bookWrite);
						if (move != String.Empty)
						{
							Console.WriteLine($"bestmove {move}");
							if (bookLoaded && isW && String.IsNullOrEmpty(lastFen) && (emptyRow > 0) && (emptyRow < bookAdd))
							{
								bookChanged = true;
								book.AddUci(lastMoves);
								book.UpdateBack(lastMoves);
							}
							emptyRow = 0;
						}
						else
						{
							emptyRow++;
							emptyTotal++;
							if (engineProcess == null)
								Console.WriteLine("enginemove");
							else
								engineProcess.StandardInput.WriteLine(msg);
						}
						break;
				}
				if (bookLoaded && isW)
				{
					if (teacher.enabled)
					{
						teacher.time++;
						CTData td = teacher.GetTData();
						if (td.finished)
						{
							if (!String.IsNullOrEmpty(td.moves))
							{
								if (teacher.stoped)
								{
									if (teacher.games == 1)
									{
										string uci = $"{td.moves} {td.best}";
										book.AddUci(uci);
										book.UpdateBack(uci);
									}
								}
								else
								{
									book.chess.SetFen();
									book.chess.MakeMoves(td.moves);
									string tnt = book.chess.GetTnt();
									CRec rec = book.recList.GetRec(tnt);
									rec.score = (short)-td.score;
									rec.depth = td.depth;
									book.UpdateBack(td.moves);
								}
							}
							td.finished = false;
							td.moves = book.GetFlat();
							teacher.SetTData(td);
							if (td.moves != String.Empty)
							{
								book.UpdateBack(td.moves);
								book.chess.SetFen();
								book.chess.MakeMoves(td.moves);
								string tnt = book.chess.GetTnt();
								CRec bst = book.recList.GetRec(tnt);
								List<int> moves = book.chess.GenerateValidMoves(out bool mate);
								if (moves.Count == 0)
								{
									if (mate)
										bst.score = short.MaxValue - 1;
									else
										bst.score = 0;
									bst.depth++;
									book.UpdateBack(td.moves);
								}
								else
									teacher.Start(td.moves, bst.depth + 1);
							}
							bookChanged = true;
						}
					}
					if (!bookChanged)
					{
						CRec rec = book.recList.GetRec();
						bookChanged = book.UpdateRec(rec) > 0;
					}
					if (bookChanged)
					{
						bookChanged = false;
						book.SaveToFile();
					}
				}
			} while (Uci.command != "quit");
			teacher.TeacherTerminate();
		}
	}
}
