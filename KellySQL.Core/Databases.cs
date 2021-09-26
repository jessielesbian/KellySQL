using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;

using KellySQL.Core.CopyOnWrite;

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

		public override object this[int a] { get => table[id][a]; set => table[id][a] = value; }

		public override int Count => table[id].Count;

		public override bool IsSynchronized => table[id].IsSynchronized;

		public override bool IsReadOnly => table[id].IsReadOnly;

		public override bool IsFixedSize => table[id].IsFixedSize;

		public override int Add(object value) => throw new NotImplementedException();

		public override void Clear() => throw new NotImplementedException();

		public override bool Contains(object value) => table[id].Contains(value);

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
		private readonly List<object> underlying;

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
}
