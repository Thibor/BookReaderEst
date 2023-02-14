﻿using NSUci;
using RapIni;
using RapLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NSProgram
{

	class Program
	{
		public static int added = 0;
		public static int updated = 0;
		public static int deleted = 0;
		/// <summary>
		/// Moves added to book per game.
		/// </summary>
		public static int bookLimitAdd = 8;
		/// <summary>
		/// Limit ply to wrtie.
		/// </summary>
		public static int bookLimitW = 8;
		/// <summary>
		/// Limit ply to read.
		/// </summary>
		public static int bookLimitR = 8;
		public static bool isVersion = true;
		public static CBook book = new CBook();
		public static CTeacher teacher = new CTeacher();
		public static CRapIni ini = new CRapIni();
		public static CRapLog log = new CRapLog(false);

		public static void LogMsg(string msg, bool condition = true)
		{
			if (condition)
				log.Add(msg);
		}

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
			CUci uci = new CUci();
			string ax = "-bf";
			List<string> listBf = new List<string>();
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			List<string> listTf = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-bf"://book file
					case "-ef"://engine file
					case "-ea"://engine arguments
					case "-rnd"://random moves
					case "-lr"://limit read in half moves
					case "-lw"://limit write in half moves
					case "-tf"://teacher file
					case "-add"://moves add to book
						ax = ac;
						break;
					case "-log"://add log
						ax = ac;
						log.enabled = true;
						break;
					case "-w"://writable
						ax = ac;
						isW = true;
						break;
					case "-info":
						ax = ac;
						isInfo = true;
						break;
					case "-ver":
						ax = ac;
						isVersion = false;
						break;
					default:
						switch (ax)
						{
							case "-bf":
								listBf.Add(ac);
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
								ac = ac.ToLower().Replace("k", "000").Replace("m", "000000");
								book.maxRecords = int.TryParse(ac, out int m) ? m : 0;
								break;
							case "-add":
								bookLimitAdd = int.TryParse(ac, out int a) ? a : bookLimitAdd;
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
			string bookFile = String.Join(" ", listBf);
			string engineFile = String.Join(" ", listEf);
			string engineArguments = String.Join(" ", listEa);
			string teacherFile = String.Join(" ", listTf);
			if (args.Length == 0)
			{
				bookFile = ini.Read("book>file");
				engineFile = ini.Read("engine>file");
				engineArguments = ini.Read("engine>arguments");
				teacherFile = ini.Read("teacher>file");
			}
			Console.WriteLine($"info string {CHeader.name} ver {CHeader.version}");
			Process engineProcess;
			if (SetEngineFile(engineFile))
				Console.WriteLine($"info string engine on");
			else if (engineFile != string.Empty)
				Console.WriteLine($"info string missing file [{engineFile}]");
			bool teacherOn = File.Exists(teacherFile);
			if (teacherOn)
				Console.WriteLine($"info string teacher on");
			else if (teacherFile != string.Empty)
				Console.WriteLine($"info string missing file [{teacherFile}]");
			bool bookLoaded = SetBookFile(bookFile);
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
					Console.WriteLine("book info - show extra informations of current book");
					Console.WriteLine("book getoption - show options");
					Console.WriteLine("book setoption name [option name] value [option value] - set option");
					continue;
				}
				uci.SetMsg(msg);
				int count = book.recList.Count;
				if (uci.command == "book")
				{
					switch (uci.tokens[1])
					{
						case "addfen":
							if (book.AddFen(uci.GetValue("addfen")))
								Console.WriteLine("Fen have been added");
							else
								Console.WriteLine("Wrong fen");
							break;
						case "addfile":
							book.AddFileInfo(uci.GetValue("addfile"),true);
							break;
						case "adduci":
							book.AddUci(uci.GetValue("adduci"));
							Console.WriteLine($"{(book.recList.Count - count):N0} moves have been added");
							break;
						case "clear":
							book.Clear();
							Console.WriteLine("Book is empty");
							break;
						case "delete":
							int c = book.Delete(uci.GetInt("delete"));
							Console.WriteLine($"{c:N0} moves was deleted");
							break;
						case "load":
							book.LoadFromFile(uci.GetValue("load"),true);
							break;
						case "moves":
							string moves = uci.GetValue("moves");
							if (moves == "shallow")
							{
								moves = book.GetShallow();
								Console.WriteLine($"moves {moves.Split().Length}");
								Console.WriteLine(moves);
								int i = moves.LastIndexOf(' ');
								if (i > 0)
									moves = moves.Substring(0, i);
							}
							book.InfoMoves(moves);
							break;
						case "info":
							book.ShowInfo();
							break;
						case "update":
							book.Update();
							break;
						case "save":
							if (book.SaveToFile(uci.GetValue("save")))
								Console.WriteLine("The book has been saved");
							else
								Console.WriteLine("Writing to the file has failed");
							break;
						case "getoption":
							Console.WriteLine($"option name book_file type string default book{CBook.defExt}");
							Console.WriteLine($"option name teacher_file type string default");
							Console.WriteLine($"option name Write type check default false");
							Console.WriteLine($"option name Log type check default false");
							Console.WriteLine($"option name limit_add_moves type spin default {bookLimitAdd} min 0 max 100");
							Console.WriteLine($"option name limit_read_ply type spin default {bookLimitR} min 0 max 100");
							Console.WriteLine($"option name limit_write_ply type spin default {bookLimitW} min 0 max 100");
							Console.WriteLine($"option name random_moves type spin default {bookRandom} min 0 max 201");
							Console.WriteLine("optionend");
							break;
						case "setoption":
							switch (uci.GetValue("name", "value").ToLower())
							{
								case "book_file":
									bookFile = uci.GetValue("value");
									break;
								case "teacher_file":
									teacherFile = uci.GetValue("value");
									break;
								case "write":
									isW = uci.GetValue("value") == "true";
									break;
								case "log":
									log.enabled = uci.GetValue("value") == "true";
									break;
								case "limit_add_moves":
									bookLimitAdd = uci.GetInt("value");
									break;
								case "limit_read_ply":
									bookLimitR = uci.GetInt("value");
									break;
								case "limit_write_ply":
									bookLimitW = uci.GetInt("value");
									break;
								case "random_moves":
									bookRandom = uci.GetInt("value");
									break;
							}
							break;
						case "optionend":
							SetBookFile(bookFile);
							break;
						default:
							Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
							break;
					}
					continue;
				}
				if ((uci.command != "go") && (engineProcess != null))
					engineProcess.StandardInput.WriteLine(msg);

				switch (uci.command)
				{
					case "position":
						lastFen = uci.GetValue("fen", "moves");
						lastMoves = uci.GetValue("moves", "fen");
						book.chess.SetFen(lastFen);
						book.chess.MakeMoves(lastMoves);
						if (String.IsNullOrEmpty(lastFen))
						{
							if ((book.chess.halfMove >> 1) == 0)
							{
								bookChanged = false;
								bookWrite = isW;
								emptyRow = 0;
								emptyTotal = 0;
								added = 0;
								updated = 0;
								deleted = 0;
								teacher.Stop();
							}
							else if ((book.chess.halfMove >> 1) == 1)
								teacher.SetTeacherFile(teacherFile);
							if (bookLoaded && isW)
								if (book.chess.Is2ToEnd(out string myMove, out string enMove))
								{
									if (bookWrite)
									{
										string moves = $"{lastMoves} {myMove} {enMove}";
										string[] am = moves.Trim().Split();
										book.AddUci(moves, true, bookLimitW, bookLimitAdd);
										CRecList rl = book.MovesToRecList(moves);
										CRec last = rl.Last();
										int del = am.Length - rl.Count;
										int score = Constants.CHECKMATE_MAX - 1 - (del >> 1);
										if ((del & 1) > 0)
											score = -score;
										last.score = (short)score;
										rl.UpdateTotal();
										bookChanged = true;
									}
									teacher.Stop();
								}
						}
						break;
					case "go":
						string move = String.Empty;
						if ((bookLimitR == 0) || (bookLimitR > book.chess.halfMove))
							move = book.GetMove(lastFen, lastMoves, bookRandom, ref bookWrite);
						if (String.IsNullOrEmpty(move))
						{
							if ((!teacher.enabled) && (emptyTotal == 0))
								log.Add(lastMoves);
							emptyRow++;
							emptyTotal++;
							if (engineProcess == null)
								Console.WriteLine("enginemove");
							else
								engineProcess.StandardInput.WriteLine(msg);
						}
						else
						{
							Console.WriteLine($"bestmove {move}");
							if (bookLoaded && isW && String.IsNullOrEmpty(lastFen) && (emptyRow > 0) && (emptyRow < bookLimitAdd))
							{
								bookChanged = true;
								book.AddUci(lastMoves);
								book.MovesToRecList(lastMoves).UpdateTotal();
							}
							emptyRow = 0;

						}
						break;
				}
				if (bookLoaded && isW)
				{
					if (teacher.enabled)
					{
						teacher.time++;
						CTData td = teacher.GetTData();
						if (td.empty)
							BookToTeacher();
						else if (td.finished)
						{
							TeacherToBook(td);
							td = new CTData();
							teacher.SetTData(td);
						}
					}
					if (!bookChanged)
						bookChanged = book.recList.GetRec().UpdateBack() > 0;
					if (bookChanged)
					{
						bookChanged = false;
						book.SaveToFile();
					}
				}
			} while (uci.command != "quit");
			teacher.TeacherTerminate();

			void BookToTeacher()
			{
				string moves = book.GetShallow();
				if (String.IsNullOrEmpty(moves))
					return;
				CRecList rl = book.MovesToRecList(moves);
				CRec last = rl.Last();
				CChessExt ch = new CChessExt();
				ch.MakeMoves(moves);
				List<int> ml = ch.GenerateValidMoves(out bool mate);
				if (ml.Count == 0)
				{
					last.depth = 0xff;
					last.score = (short)(mate ? Constants.CHECKMATE_MAX - 1 : 0);
					rl.UpdateTotal();
					bookChanged = true;
				}
				else
					teacher.Start(moves, last.depth + 1);
			}

			void TeacherToBook(CTData td)
			{
				if (string.IsNullOrEmpty(td.best))
					return;
				string moves = $"{td.moves} {td.best}";
				bool loop = book.AddUci(moves) == 0;
				CRecList rl = book.MovesToRecList(moves);
				if (rl.Count == 0)
					return;
				CRec last = rl.Last();
				last.depth = td.lastDepth;
				last.score = td.score;
				rl.UpdateTotal();
				bookChanged = true;
				string[] am = moves.Split();
				teacher.added++;
				log.Add($"moves {book.recList.Count:N0} first {am[0]} {rl.First().depth} last {td.best} {td.depth} moves {am.Length} loop {loop}");
			}

			bool SetEngineFile(string ef)
			{
				engineFile = ef;
				engineProcess = null;
				if (File.Exists(engineFile))
				{
					engineProcess = new Process();
					engineProcess.StartInfo.FileName = engineFile;
					engineProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
					engineProcess.StartInfo.UseShellExecute = false;
					engineProcess.StartInfo.RedirectStandardInput = true;
					engineProcess.StartInfo.Arguments = engineArguments;
					engineProcess.Start();
					return true;
				}
				return false;
			}

			bool SetBookFile(string bf)
			{
				bookFile = bf;
				bookLoaded = book.LoadFromFile(bookFile);
				if (bookLoaded)
				{
					if ((book.recList.Count > 0) && File.Exists(book.path))
					{
						FileInfo fi = new FileInfo(book.path);
						long bpm = (fi.Length << 3) / book.recList.Count;
						Console.WriteLine($"info string book on {book.recList.Count:N0} moves {bpm} bpm");
					}
					if (isW)
						Console.WriteLine($"info string write on");
					if (isInfo)
						book.ShowInfo();
				}
				if (teacherOn)
					isW = true;
				if (isW)
				{
					bookLimitR = 0;
					bookRandom = 0;
				}
				return bookLoaded;
			}

		}
	}
}
