using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KellySQL.Core.Databases;
using KellySQL.Core.GlobalTaskQueue;

namespace KellySQL.Testing
{
	class Program
	{
		static void Main(string[] args)
		{
			MemoryTableIMPL rows = new MemoryTableIMPL();
			rows.BeginHeavy();
			rows.Add(new MemoryRowIMPL
			{
				"Jessie Lesbian"
			});
			rows.Commit();
			Console.WriteLine(rows[0][0]);
			rows.BeginLight();
			rows[0] = new MemoryRowIMPL
			{
				"Kelly Lesbian"
			};
			Console.WriteLine(rows[0][0]);
			rows.Revert();
			Console.WriteLine(rows[0][0]);
			rows.BeginLight();
			rows[0] = new MemoryRowIMPL
			{
				"Flying Lesbian"
			};
			Console.WriteLine(rows[0][0]);
			rows.Commit();
			Console.WriteLine(rows[0][0]);
			GlobalTaskQueue.SafeKill();
		}
	}
}
