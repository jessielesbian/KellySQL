using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;

using KellySQL.Core.CopyOnWrite;
using KellySQL.Core.GlobalTaskQueue;

namespace KellySQL.Core.Databases
{
	public abstract class RowOrCollum : IList, IEnumerator
	{
		public abstract object this[int a] { get; set; }
		public int Index { get; private set; }

		public object Current => this[Index];

		public object SyncRoot { get; } = new object();
		public abstract int Count
		{
			get;
		}
		public abstract bool IsSynchronized
		{
			get;
		}
		public abstract bool IsReadOnly
		{
			get;
		}
		public abstract bool IsFixedSize
		{
			get;
		}

		public abstract int Add(object value);
		public abstract void Clear();
		public abstract bool Contains(object value);
		public abstract void CopyTo(Array array, int index);

		public virtual IEnumerator GetEnumerator()
		{
			return this;
		}

		public abstract int IndexOf(object value);
		public abstract void Insert(int index, object value);

		public virtual bool MoveNext()
		{
			return Index++ < Count;
		}

		public abstract void Remove(object value);
		public abstract void RemoveAt(int index);

		public virtual void Reset()
		{
			Index = 0;
		}
	}
	public sealed class Collum : RowOrCollum
	{
		private readonly Table table;

		private readonly int id;
		public Collum(Table t, int i)
		{
			table = t;
			id = i;
		}

		public override object this[int a] { get => table[a][id]; set => table[a][id] = value; }

		public override int Count => table.Count;

		public override bool IsSynchronized => false;

		public override bool IsReadOnly => false;

		public override bool IsFixedSize => false;

		public override int Add(object value) => throw new NotImplementedException();

		public override void Clear() => throw new NotImplementedException();

		public override bool Contains(object value) {
			foreach(Row r in table)
			{
				if(r[id] == value)
				{
					return true;
				}
			}
			return false;
		}

		private sealed class ParallelContains : Transaction
		{
			private readonly Collum col;
			private readonly object find;
			protected override object ExecuteIMPL()
			{
				bool found = false;
				Parallel.ForEach(col.table, (IList list) => {
					if(found)
					{
						return;
					}
					if(list[col.id] == find)
					{
						found = true;
					}
				});
				return found;
			}
			public ParallelContains(Collum c, object f)
			{
				col = c;
				find = f;
			}
		}

		public override void CopyTo(Array array, int index) => table[id].CopyTo(array, index);

		public override int IndexOf(object value) => table[id].IndexOf(value);

		public override void Insert(int index, object value) => throw new NotImplementedException();

		public override void Remove(object value) => throw new NotImplementedException();

		public override void RemoveAt(int index) => throw new NotImplementedException();
	}
	public abstract class Row : RowOrCollum
	{
		
	}
	public abstract class Table : IList<Row>
	{
		public abstract Row this[int index] { get; set; }

		public abstract int Count
		{
			get;
		}
		public abstract bool IsReadOnly
		{
			get;
		}

		public abstract void Add(Row item);
		public abstract void Clear();
		public abstract bool Contains(Row item);
		public abstract void CopyTo(Row[] array, int arrayIndex);
		public abstract IEnumerator<Row> GetEnumerator();
		public abstract int IndexOf(Row item);
		public abstract void Insert(int index, Row item);
		public abstract bool Remove(Row item);
		public abstract void RemoveAt(int index);

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
	public sealed class MemoryRowIMPL : Row
	{
		private readonly List<object> underlying = new List<object>();

		public override object this[int a] { get => underlying[a]; set => underlying[a] = value; }

		public override int Count => underlying.Count;

		public override bool IsSynchronized => true;

		public override bool IsReadOnly => false;

		public override bool IsFixedSize => false;

		public override int Add(object value) {
			//VERY UNSAFE CODE!
			underlying.Add(value);
			return underlying.Count - 1;
		}

		public override void Clear() => underlying.Clear();

		public override bool Contains(object value) => underlying.Contains(value);

		public override void CopyTo(Array array, int index) => underlying.CopyTo((object[]) array, index);

		public override int IndexOf(object value) => underlying.IndexOf(value);

		public override void Insert(int index, object value) => underlying.Insert(index, value);

		public override void Remove(object value) => underlying.Remove(value);

		public override void RemoveAt(int index) => underlying.RemoveAt(index);
	}

	/// <summary>
	/// An in-memory database table with commit-rollback shadow copying
	/// </summary>
	public sealed class MemoryTableIMPL : Table
	{
		private readonly ShadowCopyMasterArray lst2;

		private RowOrCollum shadowCopy;

		private RowOrCollum GetLst()
		{
			if(shadowCopy == null)
			{
				return lst2;
			}
			else
			{
				return shadowCopy;
			}
		}

		public MemoryTableIMPL(ShadowCopySlaveArrayHeavy lst)
		{
			shadowCopy = lst;
		}

		public override Row this[int index] { get => (Row)((IList)GetLst())[index]; set => ((IList)GetLst())[index] = value; }

		public override bool IsReadOnly => ((IList)GetLst()).IsReadOnly;

		public override int Count => ((ICollection)GetLst()).Count;

		public override void Add(Row item)
		{
			if(GetLst().Add(item) < 0)
			{
				throw new InvalidOperationException("Unable to add row to database");
			}
		}

		public override void Clear()
		{
			((IList)GetLst()).Clear();
		}

		public override bool Contains(Row item) => GetLst().Contains(item);

		public void CopyTo(Array array, int index)
		{
			((ICollection)GetLst()).CopyTo(array, index);
		}

		public override void CopyTo(Row[] array, int arrayIndex)
		{
			GetLst().CopyTo(array, arrayIndex);
		}


		public override IEnumerator<Row> GetEnumerator()
		{
			return (IEnumerator<Row>)GetLst().GetEnumerator();
		}

		public int IndexOf(object value)
		{
			return ((IList)GetLst()).IndexOf(value);
		}

		public override int IndexOf(Row item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, object value)
		{
			((IList)GetLst()).Insert(index, value);
		}

		public override void Insert(int index, Row item)
		{
			throw new NotImplementedException();
		}

		public void Remove(object value)
		{
			((IList)GetLst()).Remove(value);
		}

		public override bool Remove(Row item)
		{
			int temp = GetLst().Count;
			GetLst().Remove((object)item);
			return GetLst().Count + 1 == temp;
		}

		public override void RemoveAt(int index)
		{
			((IList)GetLst()).RemoveAt(index);
		}

		public MemoryTableIMPL()
		{
			lst2 = new ShadowCopyMasterArray(new List<object>());
		}

		public MemoryTableIMPL(ShadowCopyMasterArray shadow)
		{
			lst2 = shadow;
		}

		public void BeginHeavy()
		{
			shadowCopy = new ShadowCopySlaveArrayHeavy(lst2);
		}

		public void BeginLight()
		{
			shadowCopy = new ShadowCopySlaveArrayLight(lst2);
		}

		public void Revert()
		{
			shadowCopy = null;
		}

		public void Commit()
		{
			lst2.ApplySlave(GetLst());
		}
	}
}
