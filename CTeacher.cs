using System;
using System.Diagnostics;
using System.IO;
using NSUci;

namespace NSProgram
{
    class CTData
    {
        public bool empty = true;
        public bool finished = false;
        public byte depth = 0;
        public byte lastDepth = 0;
        public short score = 0;
        public string moves = string.Empty;
        public string best = string.Empty;

        public void Assign(CTData td)
        {
            empty = td.empty;
            finished = td.finished;
            depth = td.depth;
            lastDepth = td.lastDepth;
            score = td.score;
            moves = td.moves;
            best = td.best;
        }

    }

    internal class CTeacher
    {
        public bool enabled = false;
        public bool stoped = true;
        public int added = 0;
        readonly object locker = new object();
        readonly CTData tData = new CTData();
        Process teacherProcess = null;
        readonly CUci uci = new CUci();

        public CTData GetTData()
        {
            CTData td = new CTData();
            lock (locker)
            {
                td.Assign(tData);
            }
            return td;
        }

        public void SetTData(CTData td)
        {
            lock (locker)
            {
                tData.Assign(td);
            }
        }

        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    CTData td = GetTData();
                    uci.SetMsg(e.Data);
                    if (uci.command == "bestmove")
                    {
                        td.lastDepth = Math.Abs(td.score) > Constants.CHECKMATE_NEAR ? (byte)0xff : td.depth;
                        uci.GetValue("bestmove", out td.best);
                        td.finished = true;
                        SetTData(td);
                        Console.WriteLine($"info string teacher depth {td.depth} moves {td.moves}");
                        return;
                    }
                    if (uci.GetValue("cp", out string value))
                    {
                        int v = Convert.ToInt32(value);
                        if (v > Constants.CHECKMATE_NEAR)
                            v = Constants.CHECKMATE_NEAR;
                        if (v < -Constants.CHECKMATE_NEAR)
                            v = -Constants.CHECKMATE_NEAR;
                        td.score = (short)v;
                        SetTData(td);
                        return;
                    }
                    if (uci.GetValue("mate", out string mate))
                    {
                        int v = Convert.ToInt32(mate);
                        if (v > 0)
                        {
                            v = Constants.CHECKMATE_MAX - v;
                            if (v <= Constants.CHECKMATE_NEAR)
                                v = Constants.CHECKMATE_NEAR + 1;
                        }
                        if (v < 0)
                        {
                            v = -Constants.CHECKMATE_MAX - v;
                            if (v >= -Constants.CHECKMATE_NEAR)
                                v = -Constants.CHECKMATE_NEAR - 1;
                        }
                        td.score = (short)v;
                        SetTData(td);
                        return;
                    };
                }
            }
            catch { }
        }

        void TeacherWriteLine(string c)
        {
            if (teacherProcess != null)
                teacherProcess.StandardInput.WriteLine(c);
        }

        public void TeacherTerminate()
        {
            if (teacherProcess != null)
            {
                teacherProcess.OutputDataReceived -= OnDataReceived;
                teacherProcess.Kill();
                teacherProcess = null;
            }
            enabled = false;
            CTData td = new CTData();
            SetTData(td);
        }

        public bool Start(string moves, int depth)
        {
            if (stoped)
                return false;
            if (depth > Constants.MAX_DEPTH)
                return false;
            if (depth < Constants.MIN_DEPTH)
                depth = Constants.MIN_DEPTH;
            CTData td = new CTData() { empty = false, moves = moves, depth = (byte)depth };
            SetTData(td);
            TeacherWriteLine("ucinewgame");
            TeacherWriteLine($"position startpos moves {moves}");
            TeacherWriteLine($"go depth {depth}");
            return true;
        }
        public void Stop()
        {
            stoped = true;
            TeacherWriteLine("stop");
        }

        public bool SetTeacherFile(string teacherFile)
        {
            TeacherTerminate();
            if (File.Exists(teacherFile))
            {
                teacherProcess = new Process();
                teacherProcess.StartInfo.FileName = teacherFile;
                teacherProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(teacherFile);
                teacherProcess.StartInfo.CreateNoWindow = true;
                teacherProcess.StartInfo.RedirectStandardInput = true;
                teacherProcess.StartInfo.RedirectStandardOutput = true;
                teacherProcess.StartInfo.RedirectStandardError = true;
                teacherProcess.StartInfo.UseShellExecute = false;
                teacherProcess.OutputDataReceived += OnDataReceived;
                teacherProcess.Start();
                teacherProcess.BeginOutputReadLine();
                teacherProcess.PriorityClass = ProcessPriorityClass.Idle;
                TeacherWriteLine("uci");
                enabled = true;
            }
            stoped = !enabled;
            return enabled;
        }

    }
}
