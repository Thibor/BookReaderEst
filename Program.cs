using NSUci;
using RapIni;
using RapLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;

namespace NSProgram
{

    public class BookOptions
    {
        public int oblivion = 0;
        /// <summary>
        /// Random moves factor.
        /// </summary>
        public int random = 60;
    }

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
        public static BookOptions options = new BookOptions();

        public static bool Confirm(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{title} [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }

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
            string lastFen = String.Empty;
            string lastMoves = String.Empty;
            CUci uci = new CUci();
            string ax = "-bf";
            List<string> listBf = new List<string>();
            List<string> listEf = new List<string>();
            List<string> listEa = new List<string>();
            List<string> listTf = new List<string>();
            BookOptions optionsOrg = new BookOptions();
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
                                options.random = int.TryParse(ac, out int r) ? r : 0;
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
                bookFile = ini.Read("book",Constants.bookFile);
                engineFile = ini.Read("engine>file");
                engineArguments = ini.Read("engine>arguments");
                teacherFile = ini.Read("teacher>file");
            }
            Console.WriteLine($"idbook name {CHeader.name}");
            Console.WriteLine($"idbook version {CHeader.version}");
            Console.WriteLine($"idbook extension {CHeader.extension}");
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
            bool help = false;
            do
            {
                string msg = Console.ReadLine().Trim();
                if (help || String.IsNullOrEmpty(msg) || (msg == "help") || (msg == "book"))
                {
                    Console.WriteLine("book load [filename].[mem|pgn|uci] - clear and add moves from file");
                    Console.WriteLine("book save [filename].[mem|pgn|uci] - save book to the file");
                    Console.WriteLine("book delete [number x] - delete x moves from the book");
                    Console.WriteLine("book addfile [filename].[mem|png|uci] - add moves to the book from file");
                    Console.WriteLine("book adduci [uci] - add moves in uci format to the book");
                    Console.WriteLine("book addfen [fen] - add position in fen format");
                    Console.WriteLine("book clear - clear all moves from the book");
                    Console.WriteLine("book moves [uci] - make sequence of moves in uci format and shows possible continuations");
                    Console.WriteLine("book info - show extra informations of current book");
                    Console.WriteLine("book getoption - show options");
                    Console.WriteLine("book setoption name [option name] value [option value] - set option");
                    help = false;
                    continue;
                }
                uci.SetMsg(msg);
                int count = book.recList.Count;
                if (uci[0] == "book")
                {
                    switch (uci.tokens[1])
                    {
                        case "isready":
                            Console.WriteLine("book readyok");
                            break;
                        case "addfen":
                            if (book.AddFen(uci.GetValue("addfen")))
                                Console.WriteLine("Fen have been added");
                            else
                                Console.WriteLine("Wrong fen");
                            break;
                        case "addfile":
                            book.AddFileInfo(uci.GetValue("addfile"), true);
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
                            byte d = book.recList.DepthMin();
                            if (d < Constants.MIN_DEPTH)
                            {
                                int c = book.recList.CountDepth(d);
                                if (Confirm($"Delete {c} moves?"))
                                    Console.WriteLine($"{book.DeleteDepth(d):N0} moves was deleted");
                                break;
                            }
                            if (Confirm($"Delete {book.CountOldest():N0} moves?"))
                                Console.WriteLine($"{book.DeleteOldest():N0} moves was deleted");
                            break;
                        case "load":
                            book.LoadFromFile(uci.GetValue("load"), true);
                            break;
                        case "start":
                            teacher.SetTeacherFile(teacherFile);
                            while (teacher.enabled)
                                if (TeacherStart())
                                    book.SaveToFile(bookFile);
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
                        case "reset":
                            book.Reset();
                            break;
                        case "save":
                            if (book.SaveToFile(uci.GetValue("save")))
                                Console.WriteLine("The book has been saved");
                            else
                                Console.WriteLine("Writing to the file has failed");
                            break;
                        case "shallow":
                            Console.WriteLine(book.GetShallow());
                            break;
                        case "getoption":
                            Console.WriteLine($"option name book_file type string default book{CBook.defExt}");
                            Console.WriteLine($"option name teacher_file type string default");
                            Console.WriteLine($"option name Write type check default false");
                            Console.WriteLine($"option name Log type check default false");
                            Console.WriteLine($"option name limit_add_moves type spin default {bookLimitAdd} min 0 max 100");
                            Console.WriteLine($"option name limit_read_ply type spin default {bookLimitR} min 0 max 100");
                            Console.WriteLine($"option name limit_write_ply type spin default {bookLimitW} min 0 max 100");
                            Console.WriteLine($"option name random_moves type spin default {optionsOrg.random} min 0 max 201");
                            Console.WriteLine($"option name oblivion type spin default {optionsOrg.oblivion} min 0 max 255");
                            Console.WriteLine("optionend");
                            break;
                        case "setoption":
                            switch (uci.GetValue("name", "value").ToLower())
                            {
                                case "book_file":
                                    SetBookFile(uci.GetValue("value"));
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
                                    options.random = uci.GetInt("value");
                                    break;
                                case "oblivion":
                                    options.oblivion = uci.GetInt("value");
                                    break;
                            }
                            break;
                        case "help":
                            help = true;
                            break;
                        default:
                            Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
                            Console.WriteLine($"book help - show console commands");
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
                                added = 0;
                                updated = 0;
                                deleted = 0;
                                teacher.Stop();
                            }
                            else if ((book.chess.halfMove >> 1) == 1)
                                teacher.SetTeacherFile(teacherFile);
                            if (isW)
                                if (bookWrite && book.chess.Is2ToEnd(out string myMove, out string enMove))
                                {
                                    if (bookWrite)
                                    {
                                        string moves = $"{lastMoves} {myMove} {enMove}";
                                        string[] am = moves.Trim().Split();
                                        int added = book.AddUci(moves, true, bookLimitW, bookLimitAdd);
                                        CRecList rl = book.MovesToRecList(moves);
                                        CRec last = rl.Last();
                                        int del = am.Length - rl.Count;
                                        int score = Constants.CHECKMATE_MAX - 1 - (del >> 1);
                                        if ((del & 1) > 0)
                                            score = -score;
                                        last.score = (short)score;
                                        rl.UpdateTotal();
                                        bookChanged = true;
                                        if ((added > 0) && (++book.header.oblivion > options.oblivion) && (options.oblivion > 0))
                                        {
                                            book.Delete(added);
                                            book.header.oblivion = 0;
                                        }
                                    }
                                    teacher.Stop();
                                }
                        }
                        break;
                    case "go":
                        string move = String.Empty;
                        if ((bookLimitR == 0) || (bookLimitR > book.chess.halfMove))
                            move = book.GetMove(lastFen, lastMoves, options.random, ref bookWrite);
                        if (String.IsNullOrEmpty(move))
                        {
                            emptyRow++;
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
                if (isW)
                {
                    if (teacher.enabled)
                        TeacherStart();
                    if (!bookChanged)
                    {
                        CRec rec = book.recList.GetRec();
                        if (rec != null)
                            bookChanged = rec.UpdateBack() > 0;
                    }
                    if (bookChanged)
                    {
                        bookChanged = false;
                        book.SaveToFile(bookFile);
                    }
                }
            } while (uci.command != "quit");
            teacher.TeacherTerminate();

            bool TeacherStart()
            {
                CTData td = teacher.GetTData();
                if (td.empty)
                    BookToTeacher();
                else if (td.finished)
                {
                    TeacherToBook(td);
                    td = new CTData();
                    teacher.SetTData(td);
                    return true;
                }
                return false;
            }

            void BookToTeacher()
            {
                string moves = book.GetShallow();
                if (String.IsNullOrEmpty(moves))
                    return;
                CRecList rl = book.MovesToRecList(moves);
                CRec last = rl.Last();
                CChessExt chess = new CChessExt();
                chess.MakeMoves(moves);
                List<int> ml = chess.GenerateValidMoves(out bool mate);
                if (ml.Count == 0)
                {
                    last.depth = Constants.MAX_DEPTH;
                    last.score = (short)(mate ? Constants.CHECKMATE_MAX - 1 : 0);
                    rl.UpdateTotal();
                    bookChanged = true;
                }
                else
                    teacher.Start(moves, last.depth + 1);
            }

            void TeacherToBook(CTData td)
            {
                CRecList rl = book.MovesToRecList(td.moves);
                if (rl.Count == 0)
                    return;
                CRec last = rl.Last();
                last.depth = td.lastDepth;
                last.score = (short)-td.score;
                rl.UpdateTotal();
                bookChanged = true;
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
                if (teacherOn)
                    isW = true;
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
                else
                    isW = false;
                if (isW)
                {
                    bookLimitR = 0;
                    options.random = 0;
                }
                return bookLoaded;
            }

        }
    }
}
