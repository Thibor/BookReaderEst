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
        public readonly CHeader header = new CHeader();
        public CRecList recList = new CRecList();
        readonly CBuffer buffer = new CBuffer();

        #region file est

        bool AddFileEst(string p)
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
                    string curHeader = buffer.ReadString();
                    if (Program.isVersion && !header.FromStr(curHeader))
                        Console.WriteLine($"This program only supports version [{header.Title()}]");
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

        public bool SaveToEst(string p)
        {
            string pt = p + ".tmp";
            int maxAge = GetMaxAge();
            Program.deleted = 0;
            if (maxRecords > 0)
                Program.deleted = recList.Count - maxRecords;
            if (Program.deleted > 0)
                Delete(Program.deleted);
            recList.SortTnt();
            try
            {
                using (FileStream fs = File.Open(pt, FileMode.Create, FileAccess.Write, FileShare.None))
                using (buffer.Bw = new BinaryWriter(fs))
                {
                    string lastTnt = new string('-', 64);
                    buffer.Write(header.ToStr());
                    foreach (CRec rec in recList)
                    {
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
            if (maxAge > 0)
                AddLog();
            return true;
        }

        public void AddLog()
        {
            double proZS = ProZeroScore();
            double proZD = ProZeroDepth(out ulong totalDepth);
            double avgDepth = (double)totalDepth / recList.Count;
            Program.log.Add($"book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} (zero score {proZS:N2}%) (zero depth {proZD:N2}%) (avg depth {avgDepth:N2})");
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

        bool AddFileUci(string p)
        {
            if (!File.Exists(p))
                return true;
            string[] lines = File.ReadAllLines(p);
            foreach (string uci in lines)
                AddUci(uci);
            return true;
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
            Console.WriteLine("finish");
            Console.Beep();
            return true;
        }

        #endregion file uci

        #region file pgn

        bool AddFilePgn(string p, bool show = false)
        {
            if (!File.Exists(p))
                return true;
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
                    if (show)
                        Console.Write($"\rAdded {recList.Count} moves");
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
            if (show)
            {
                Console.WriteLine();
                Console.WriteLine($"Found {errors:N2} errors");
                Console.WriteLine("Finish");
                Console.Beep();
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
                foreach (string uci in sl)
                {
                    string[] arrMoves = uci.Split();
                    chess.SetFen();
                    string pgn = String.Empty;
                    foreach (string umo in arrMoves)
                    {
                        string san = chess.UmoToSan(umo);
                        if (san == String.Empty)
                            break;
                        int number = (chess.halfMove >> 1) + 1;
                        if (chess.WhiteTurn)
                            pgn += $"{number}. {san} ";
                        else
                            pgn += $"{san} ";
                        int emo = chess.UmoToEmo(umo);
                        chess.MakeMove(emo);
                    }
                    pgn += "1/2-1/2";
                    sw.WriteLine();
                    sw.WriteLine("[White \"White\"]");
                    sw.WriteLine("[Black \"Black\"]");
                    sw.WriteLine("[Result \"1/2-1/2\"]");
                    sw.WriteLine();
                    sw.WriteLine(pgn.Trim());
                    Console.Write($"\rgames {++line}");
                }
            }
            Console.WriteLine();
            Console.WriteLine("finish");
            Console.Beep();
            return true;
        }

        #endregion file pgn

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

        public string GetShallow()
        {
            byte depth = byte.MaxValue;
            string result = String.Empty;
            chess.SetFen();
            while (true)
            {
                CEmoList el = GetEmoList();
                if (el.Count == 0)
                    break;
                el.SortShallow();
                CEmo emo = el[0];
                if (emo.rec.depth > depth)
                    break;
                depth = emo.rec.depth;
                chess.MakeMove(emo.emo);
                string umo = chess.EmoToUmo(emo.emo);
                result += $" {umo}";
            }
            return result.Trim();
        }

        public bool LoadFromFile(string p = "", bool show = false)
        {
            if (String.IsNullOrEmpty(p))
                if (String.IsNullOrEmpty(path))
                    return false;
                else
                    return LoadFromFile(path);
            recList.Clear();
            return AddFileInfo(p, show);
        }

        public bool AddFile(string p, bool show = false)
        {
            string ext = Path.GetExtension(p).ToLower();
            if (ext == defExt)
                return AddFileEst(p);
            else if (ext == ".uci")
                return AddFileUci(p);
            else if (ext == ".pgn")
                return AddFilePgn(p, show);
            return false;
        }

        public bool AddFileInfo(string p, bool show = false)
        {
            if (!File.Exists(p))
            {
                Console.WriteLine($"info string file {Path.GetFileName(p)} not found");
                return true;
            }
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int count = recList.Count;
            bool result = AddFile(p, show);
            count = recList.Count - count;
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine($"info string {count:N0} moves added in {ts.TotalSeconds:N2} seconds");
            return result;
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

        public CRecList MovesToRecList(string moves)
        {
            return MovesToRecList(moves.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public CRecList MovesToRecList(string[] moves)
        {
            CChessExt chess = new CChessExt();
            chess.SetFen();
            CRecList rl = new CRecList();
            foreach (string uci in moves)
                if (chess.MakeMove(uci, out _))
                {
                    string tnt = chess.GetTnt();
                    CRec rec = recList.GetRec(tnt);
                    if (rec == null)
                        break;
                    rl.Add(rec);
                }
                else break;
            return rl;
        }

        public int AddUci(string moves, bool upAge = false, int limitPly = 0, int limitAdd = 0)
        {
            return AddUci(moves.Trim().Split(), upAge, limitPly, limitAdd);
        }

        public int AddUci(List<string> moves, bool upAge = false, int limitPly = 0, int limitAdd = 0)
        {
            return AddUci(moves.ToArray(), upAge, limitPly, limitAdd);
        }

        public int AddUci(string[] moves, bool upAge = false, int limitPly = 0, int limitAdd = 0)
        {
            int ca = 0;
            if ((limitPly == 0) || (limitPly > moves.Length))
                limitPly = moves.Length;
            CChessExt chess = new CChessExt();
            for (int n = 0; n < limitPly; n++)
            {
                string m = moves[n];
                if (chess.MakeMove(m, out _))
                {
                    CRec rec = new CRec() { tnt = chess.GetTnt() };
                    if (recList.AddRec(rec, upAge))
                    {
                        ca++;
                        Program.added++;
                        if ((limitAdd > 0) && (ca >= limitAdd))
                            break;
                    }
                }
                else
                    break;
            }
            return ca;
        }

        void RefreshAge()
        {
            for (int n = 0; n <= 0xff; n++)
                arrAge[n] = 0;
            foreach (CRec rec in recList)
                arrAge[rec.age]++;
        }

        int GetMaxAge()
        {
            RefreshAge();
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

        public int DeleteDepth(int d = 0)
        {
            int result = 0;
            for (int n = recList.Count - 1; n >= 0; n--)
                if (recList[n].depth == d)
                {
                    recList.RemoveAt(n);
                    result++;
                }
            RefreshAge();
            SaveToEst(path);
            return result;
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

        public CEmoList GetEmoList(bool backMove = true)
        {
            CEmoList emoList = new CEmoList();
            List<int> moves = chess.GenerateValidMoves(out _, 0);
            foreach (int m in moves)
            {
                chess.MakeMove(m);
                string tnt = chess.GetTnt();
                CRec rec = recList.GetRec(tnt);
                if (rec != null)
                    if (backMove || chess.MoveIsForth(m))
                    {
                        CEmo emo = new CEmo(m, rec);
                        emoList.Add(emo);
                    }
                chess.UnmakeMove(m);
            }
            emoList.SortScore();
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
                string scFm = bst.rec.score > Constants.CHECKMATE_NEAR ? $"mate {Constants.CHECKMATE_MAX - bst.rec.score}" : (bst.rec.score < -Constants.CHECKMATE_NEAR ? $"mate {-Constants.CHECKMATE_MAX - bst.rec.score}" : $"cp {bst.rec.score}");
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
            Console.WriteLine("{0,8} {1,8:N0} {2,8}", lev, arrAge[lev], del);
        }

        public void InfoStructure()
        {
            int ageMax = AgeMax();
            int ageMin = AgeMin();
            Console.WriteLine();
            Console.WriteLine("{0,8} {1,8} {2,8}", "age", "count", "delta");
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
                    Console.WriteLine();
                    Console.WriteLine("{0,8} {1,8} {2,8} {3,8} {4,8}", "id", "move", "score", "depth", "age");
                    int i = 1;
                    foreach (CEmo e in el)
                    {
                        string umo = chess.EmoToUmo(e.emo);
                        string scFm = e.rec.score > Constants.CHECKMATE_NEAR ? $"+{Constants.CHECKMATE_MAX - e.rec.score}M" : (e.rec.score < -Constants.CHECKMATE_NEAR ? $"{-Constants.CHECKMATE_MAX - e.rec.score}M" : $"{e.rec.score}");
                        Console.WriteLine("{0,8} {1,8} {2,8} {3,8} {4,8}", i++, umo, scFm, e.rec.depth, e.rec.age);
                    }
                }
            }
        }

        double ProZeroDepth(out ulong totalDepth)
        {
            totalDepth = 0;
            int result = 0;
            foreach (CRec rec in recList)
                if (rec.depth == 0)
                    result++;
                else
                    totalDepth += rec.depth;
            return (result * 100.0) / recList.Count;
        }

        int GetOldestAge()
        {
            for (int n = 0xff; n > 0; n--)
                if (arrAge[n] > 0)
                    return n;
            return 0;
        }

        public int CountOldest()
        {
            int oa = GetOldestAge();
            return arrAge[oa];
        }

        public int DeleteOldest()
        {
            int result = 0;
            int oa = GetOldestAge();
            for (int n = recList.Count - 1; n >= 0; n--)
                if (recList[n].age == oa)
                {
                    recList.RemoveAt(n);
                    result++;
                }
            RefreshAge();
            SaveToEst(path);
            return result;
        }

        int CountZeroScore()
        {
            int result = 0;
            foreach (CRec rec in recList)
                if (rec.score == 0)
                    result++;
            return result;
        }

        double ProZeroScore()
        {
            return (CountZeroScore() * 100.0) / recList.Count;
        }

        public void ShowInfo()
        {
            if (recList.Count == 0)
            {
                Console.WriteLine("no records");
                return;
            }
            Console.WriteLine();
            Console.WriteLine($"Records    {recList.Count:N0}");
            Console.WriteLine($"Depth avg  {recList.DepthAvg():N4}");
            Console.WriteLine($"Depth low  {recList.ProDepthLow():N4} %");
            Console.WriteLine($"Score zero {ProZeroScore():N2} %");
            string frm = "{0,8} {1,8:N0} {2,8:P4} %";
            Console.WriteLine();
            Console.WriteLine(frm, "depth", "count", "procent");
            int l = 0;
            for (byte d = 0; d <= 0xff; d++)
            {
                int c = recList.CountDepth(d);
                if (c > 0)
                {
                    Console.WriteLine(frm, d, c, (c * 100.0) / recList.Count);
                    if (++l > 1)
                        break;
                }
            }
            for (byte d = 0xff; d >= 0; d--)
            {
                int c = recList.CountDepth(d);
                if (c > 0)
                {
                    Console.WriteLine(frm, d, c, (c * 100.0) / recList.Count);
                    break;
                }
            }
            InfoStructure();
            InfoMoves();
        }

        public void Update()
        {
            int line = 0;
            int up = 0;
            recList.SortDepth();
            foreach (CRec rec in recList)
            {
                up += rec.UpdateBack();
                Console.Write($"\rupdate {++line * 100.0 / recList.Count:N4}%");
            }
            SaveToFile();
            Console.WriteLine();
            Console.WriteLine($"updated {up:N0}");
            Console.Beep();
        }

        public void Reset()
        {
            foreach (CRec rec in recList)
                rec.depth = 0;
            SaveToFile();
        }

        public bool SaveToFile(string p = "")
        {
            if (string.IsNullOrEmpty(p))
                if (string.IsNullOrEmpty(path))
                    return false;
                else
                    SaveToFile(path);
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

        /// <summary>
        /// Return all games from book in uci format
        /// <summary>
        List<string> GetGames()
        {
            List<string> sl = new List<string>();
            GetGames(string.Empty, 0, 0, 1, ref sl);
            Console.WriteLine();
            Console.WriteLine($"{sl.Count:N0} games");
            sl.Sort();
            return sl;
        }

        bool GetGames(string moves, int ply, double proT, double proU, ref List<string> list)
        {
            bool add = true;
            if (ply < 12)
            {
                chess.SetFen();
                chess.MakeMoves(moves);
                CEmoList el = GetEmoList(false);
                if (el.Count > 0)
                {
                    proU /= el.Count;
                    for (int n = 0; n < el.Count; n++)
                    {
                        CEmo emo = el[n];
                        string umo = chess.EmoToUmo(emo.emo);
                        double p = proT + n * proU;
                        if (GetGames($"{moves} {umo}".Trim(), ply + 1, p, proU, ref list))
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

    }
}
