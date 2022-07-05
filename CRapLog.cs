using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

namespace RapLog
{
	public class CRapLog
	{
		bool addDate;
		int max;
		string path;

		public CRapLog(string p = "",int m = 100,bool a = true)
		{
			path = p;
			max = m;
			addDate = a;
			if(path == String.Empty) {
				string name = Assembly.GetExecutingAssembly().GetName().Name + ".log";
				path = new FileInfo(name).FullName.ToString();
			}
		}

		public void Add(string m)
		{
			List<string> list = new List<string>();
			if (File.Exists(path))
				list = File.ReadAllLines(path).ToList();
			if (addDate)
				list.Insert(0, $"{DateTime.Now} {m}");
			else
				list.Add(m);
			int count = list.Count - max;
			if ((count > 0) && (max > 0))
				list.RemoveRange(100, count);
			File.WriteAllLines(path, list);
		}

	}
}
