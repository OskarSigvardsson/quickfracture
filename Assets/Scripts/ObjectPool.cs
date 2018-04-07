using System;
using System.Threading;
using System.Collections.Concurrent;

namespace GK {
	public class ObjectPool<T> {

		int borrowed;

		public int Borrowed {
			get {
				return borrowed;
			}
		}

		Func<T> objectGenerator;
		ConcurrentBag<T> pool;

		public ObjectPool(Func<T> objectGenerator) {
			borrowed = 0;

			pool = new ConcurrentBag<T>();
			this.objectGenerator = objectGenerator;
		}

		public T TakeOut() {
			Interlocked.Increment(ref borrowed);

			T obj;

			if (pool.TryTake(out obj)) {
				return obj;
			} else {
				return objectGenerator();
			}
		}

		public void PutBack(T obj) {
			Interlocked.Decrement(ref borrowed);

			pool.Add(obj);
		}
	}
}
