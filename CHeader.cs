using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	internal class CHeader
	{
		public const string name = "BookReaderEst";
		public const string version = "2022-11-17";

		public string GetHeader()
		{
			return $"{name} {version}";
		}

	}
}
