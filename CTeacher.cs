using System;
using System.Diagnostics;
using System.IO;
using NSUci;

namespace NSProgram
{
	class CTData
	{
		public bool finished = true;
		public bool stop = false;
		public string moves = String.Empty;
		public byte depth = 0;
		public short score = 0;
		public string best = String.Empty;

		public void Assign(CTData td)
		{
			finished = td.finished;
			stop = td.stop;
			moves = td.moves;
			depth = td.depth;
			score = td.score;
			best = td.best;
		}
	}

	internal class CTeacher
	{
		public bool enabled = false;
		public bool stoped = false;
		int minDepth = 0xf;
		public int time = 0;
		public int games = 0;
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
					uci.SetMsg(e.Data);
					if (uci.command == "bestmove")
					{
						CTData td = GetTData();
						uci.GetValue("bestmove", out td.best);
						td.finished = true;
						SetTData(td);
						return;
					}
					if (uci.GetValue("cp", out string value))
					{
						CTData td = GetTData();
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
						CTData td = GetTData();
						int v = Convert.ToInt32(mate);
						if (v > 0)
						{
							v = Constants.CHECKMATE_MAX - (v<<1);
							if (v <= Constants.CHECKMATE_NEAR)
								v = Constants.CHECKMATE_NEAR + 1;
						}
						if (v < 0)
						{
							v = -Constants.CHECKMATE_MAX - (v<<1);
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
		}

		public void Start(string moves, int depth)
		{
			time = 0;
			games++;
			if (stoped || String.IsNullOrEmpty(moves) || (depth > 0xff))
				return;
			if ((time < 4) && (minDepth < 0xff))
				minDepth++;
			if ((time > 4) && (minDepth > 0xf))
				minDepth--;
			if (depth < minDepth)
				depth = minDepth;
			CTData td = new CTData
			{
				finished = false,
				moves = moves,
				depth = (byte)depth
			};
			SetTData(td);
			TeacherWriteLine($"position startpos moves {moves}");
			TeacherWriteLine($"go depth {depth}");
		}

		public void Stop()
		{
			stoped = true;
			TeacherWriteLine("stop");
		}

		public bool SetTeacherFile(string teacherFile)
		{
			enabled = false;
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
				TeacherWriteLine("isready");
				TeacherWriteLine("ucinewgame");
				enabled = true;
				stoped = false;
				games = 0;
			}
			return enabled;
		}

	}
}
