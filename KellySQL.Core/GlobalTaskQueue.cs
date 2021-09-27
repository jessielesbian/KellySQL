using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;
using System.Threading;

namespace KellySQL.Core.GlobalTaskQueue
{
	public static class GlobalTaskQueue
	{
		private static readonly ConcurrentQueue<Transaction> queue = new ConcurrentQueue<Transaction>();
		private static readonly Thread thr;
		static GlobalTaskQueue()
		{
			thr = new Thread(TransactionSequencerThread)
			{
				Name = "KellySQL Transaction Sequencer Thread"
			};
			thr.Start();
		}
		public static void PushTransaction(Transaction tx)
		{
			queue.Enqueue(tx);
		}
		public static object ExecuteTransactionSYNC(Transaction tx)
		{
			queue.Enqueue(tx);
			Exception e = tx.Error;
			if(e == null)
			{
				return tx.Result;
			}
			else if(e is SequencerKiller)
			{
				return null;
			}
			else
			{
				throw e;
			}
		}
		private static void TransactionSequencerThread()
		{
			try
			{
				while(true)
				{
					if(queue.TryDequeue(out Transaction pending))
					{
						pending.Execute();
					}
					else
					{
						Thread.Sleep(1);
					}
				}
			} catch(SequencerKiller)
			{
				return;
			}
			
		}
		/// <summary>
		/// Waits for all pending transactions to finish, and kill the sequencer.
		/// Once the sequencer is killed, the're no way to restart it for now.
		/// </summary>
		public static void SafeKill()
		{
			ExecuteTransactionSYNC(new MethodTransaction(() =>
			{
				throw new SequencerKiller();
			}));
		}
	}
	public abstract class Transaction
	{
		private object result;
		private Exception error;
		/// <summary>
		/// Blocks until conpletion, and returns the result
		/// </summary>
		public object Result
		{
			get {
				completionWaiter.Wait();
				return result;
			}
		}
		/// <summary>
		/// Blocks until completion and returns the error
		/// </summary>
		public Exception Error
		{
			get
			{
				completionWaiter.Wait();
				return error;
			}
		}
		internal readonly ManualResetEventSlim completionWaiter = new ManualResetEventSlim(false);

		internal void Execute()
		{
			try
			{
				result = ExecuteIMPL();
			} catch(Exception e){
				error = e;
				if(e is SequencerKiller)
				{
					completionWaiter.Set();
					throw e;
				}
			}
			completionWaiter.Set();
		}

		protected abstract object ExecuteIMPL();

		~Transaction()
		{
			completionWaiter.Dispose();
		}
	}
	public sealed class MethodTransaction : Transaction
	{
		private readonly Func<object> func;
		public MethodTransaction(Func<object> f)
		{
			func = f;
		}
		protected override object ExecuteIMPL()
		{
			return func.Invoke();
		}
	}
	internal sealed class SequencerKiller : Exception {
		
	}
}