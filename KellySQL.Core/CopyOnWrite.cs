using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;
using System.Threading;
using System.Collections.Concurrent;

using KellySQL.Core.Databases;
using KellySQL.Core.GlobalTaskQueue;

namespace KellySQL.Core.CopyOnWrite
{
	/// <summary>
	/// Shadow copy master array allows implementation of commit-rollback schemantics
	/// and increased efficency
	/// </summary>
	[Serializable] public sealed class ShadowCopyMasterArray : RowOrCollum
	{
		internal readonly IList collection;
		/// <summary>
		/// Creates an master copy of a collection
		/// </summary>
		/// <param name="collection">The collection you want to use for the master copy</param>
		public ShadowCopyMasterArray(IList c)
		{
			collection = c;
		}

		public override object this[int a] { get => collection[a]; set => throw new NotImplementedException(); }

		public override int Count => collection.Count;

		public override bool IsSynchronized => collection.IsSynchronized;

		public override bool IsReadOnly => collection.IsReadOnly;

		public override bool IsFixedSize => collection.IsFixedSize;

		public override int Add(object value)
		{
			throw new NotImplementedException();
		}

		public void ApplySlave(RowOrCollum slave) {
			if(slave is ShadowCopySlaveArrayHeavy shadow)
			{
				ApplySlave(shadow);
			}
			else
			{
				ApplySlave((ShadowCopySlaveArrayLight) slave);
			}
		}

		/// <summary>
		/// Applies the changes made to the slave array to the master array
		/// </summary>
		public void ApplySlave(ShadowCopySlaveArrayLight slave) {
			if(!ReferenceEquals(slave.master, this))
			{
				throw new InvalidOperationException("The slave array is not a copy of this master array");
			}
			GlobalTaskQueue.GlobalTaskQueue.ExecuteTransactionSYNC(new MethodTransaction(() => {
				Parallel.ForEach(slave.copies, ApplySlaveIMPL);
				return null;
			}));
		}

		/// <summary>
		/// Applies the changes made to the slave array to the master array
		/// </summary>
		public void ApplySlave(ShadowCopySlaveArrayHeavy slave)
		{
			if(!ReferenceEquals(slave.master, this))
			{
				throw new InvalidOperationException("The slave array is not a copy of this master array");
			}
			collection.Clear();
			foreach(object o in slave.clone) {
				collection.Add(o);
			}
		}

		public override void Clear() => collection.Clear();

		public override bool Contains(object value) => collection.Contains(value);

		public override void CopyTo(Array array, int index) => collection.CopyTo(array, index);

		public override int IndexOf(object value) => collection.IndexOf(value);

		public override void Insert(int index, object value) => collection.Insert(index, value);

		public override void Remove(object value) => collection.Remove(value);

		public override void RemoveAt(int index) => collection.RemoveAt(index);

		private void ApplySlaveIMPL(KeyValuePair<int, object> obj)
		{
			collection[obj.Key] = obj.Value;
		}
	}
	/// <summary>
	/// Shadow copy slave array, does not support adding and removing
	/// </summary>
	public sealed class ShadowCopySlaveArrayLight : RowOrCollum
	{
		internal readonly ConcurrentDictionary<int, object> copies = new ConcurrentDictionary<int, object>();
		internal readonly ShadowCopyMasterArray master;

		public override object this[int a]
		{
			get => copies.ContainsKey(a) ? copies[a] : master[a]; set
			{
				copies.AddOrUpdate(a, (int _) => { return value; }, (int _, object x2) => { return value; });
			}
		}

		public override int Count => master.Count;

		public override bool IsSynchronized => throw new NotImplementedException();

		public override bool IsReadOnly => throw new NotImplementedException();

		public override bool IsFixedSize => throw new NotImplementedException();

		/// <param name="m">The master array should not be modified until the slave array is destroyed or applied to the master array.</param>
		public ShadowCopySlaveArrayLight(ShadowCopyMasterArray m) {
			master = m;
		}

		public override int Add(object value)
		{
			throw new NotImplementedException();
		}

		public override void Clear()
		{
			throw new NotImplementedException();
		}

		public override bool Contains(object value)
		{
			throw new NotImplementedException();
		}

		public override void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public override int IndexOf(object value)
		{
			throw new NotImplementedException();
		}

		public override void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public override void Remove(object value)
		{
			throw new NotImplementedException();
		}

		public override void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}
	}
	/// <summary>
	/// Heavyweight shadow copy slave array, supporting complex operations
	/// </summary>
	public sealed class ShadowCopySlaveArrayHeavy : RowOrCollum
	{
		internal readonly ShadowCopyMasterArray master;

		internal readonly IList clone = new List<object>();

		private bool forked = false;

		private IList GetList2()
		{
			if(forked)
			{
				return clone;
			}
			else
			{
				return master.collection;
			}
		}

		private IList GetList3()
		{
			if(!forked)
			{
				foreach(object o in master.collection)
				{
					clone.Add(o);
				}
				forked = true;
			}
			return clone;
		}

		public override int Count => GetList2().Count;

		public override bool IsSynchronized => GetList2().IsSynchronized;

		public override bool IsReadOnly => GetList2().IsReadOnly;

		public override bool IsFixedSize => GetList2().IsFixedSize;

		public override object this[int a] { get => GetList2()[a]; set => GetList3()[a] = value; }

		/// <param name="m">The master array should not be modified until the slave array is destroyed or applied to the master array.</param>
		public ShadowCopySlaveArrayHeavy(ShadowCopyMasterArray m)
		{
			master = m;
		}

		public override int Add(object value) => GetList3().Add(value);

		public override void Clear() => GetList3().Clear();

		public override bool Contains(object value) => GetList2().Contains(value);

		public override void CopyTo(Array array, int index) => GetList2().CopyTo(array, index);

		public override int IndexOf(object value) => GetList2().IndexOf(value);

		public override void Insert(int index, object value) => GetList3().Insert(index, value);

		public override void Remove(object value) => GetList3().Remove(value);

		public override void RemoveAt(int index) => GetList3().RemoveAt(index);
	}
}
