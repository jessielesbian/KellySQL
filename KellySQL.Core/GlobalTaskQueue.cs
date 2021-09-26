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
		private static readonly ConcurrentQueue<Transaction> queue;
		private static readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
		static GlobalTaskQueue()
		{
			new Thread(TransactionSequencerThread)
			{
				Name = "KellySQL Transaction Sequencer Thread"
			}.Start();
		}
		public static void PushTransaction(Transaction tx)
		{
			queue.Enqueue(tx);
			autoResetEvent.Set();
		}
		public static object ExecuteTransactionSYNC(Transaction tx)
		{
			queue.Enqueue(tx);
			autoResetEvent.Set();
			Exception e = tx.Error;
			if(e == null)
			{
				return tx.Result;
			}
			else
			{
				throw e;
			}
		}
		private static void TransactionSequencerThread()
		{
			while(true)
			{
				if(queue.TryDequeue(out Transaction pending))
				{
					pending.Execute();
				}
				else
				{
					autoResetEvent.WaitOne();
				}
			}
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
			}
			completionWaiter.Set();
		}

		protected abstract object ExecuteIMPL();
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
}