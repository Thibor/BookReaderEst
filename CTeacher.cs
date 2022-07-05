using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using NSUci;

namespace NSProgram
{
	class CTData
	{
		public bool finished = true;
		public string moves = String.Empty;
		public byte depth = 0;
		public short score = 0;

		public void Assign(CTData td)
		{
			finished = td.finished;
			moves = td.moves;
			depth = td.depth;
			score = td.score;
		}
	}

	internal class CTeacher
	{
		short valMax = short.MaxValue - 0xff;
		short valMin = short.MinValue + 0xff;
		public bool ready = false;
		object locker = new object();
		CTData tData = new CTData();
		Process teacherProcess = null;
		CUci uci = new CUci();

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
						td.finished = true;
						SetTData(td);
						return;
					}
					if (uci.GetValue("cp", out string value))
					{
						CTData td = GetTData();
						int v = Convert.ToInt32(value);
						if (v > valMax)
							v = valMax;
						if (v < valMin)
							v = valMin;
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
							v = short.MaxValue - v;
							if (v <= valMax)
								v = valMax + 1;
						}
						if(v < 0)
						{
							v = short.MinValue - v;
							if (v >= valMin)
								v = valMin - 1;
						}
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
				if (!teacherProcess.HasExited)
				{
					teacherProcess.StandardInput.WriteLine(c);
					teacherProcess.StandardInput.Flush();
				}
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
			if (depth < 0xf)
				depth = 0xf;
			CTData td = new CTData();
			td.finished = false;
			td.moves = moves;
			td.depth = (byte)depth;
			SetTData(td);
			TeacherWriteLine($"position startpos moves {moves}");
			TeacherWriteLine($"go depth {depth}");
		}

		public bool SetTeacher(string teacherFile)
		{
			ready = false;
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
				ready = true;
			}
			return ready;
		}

	}
}
