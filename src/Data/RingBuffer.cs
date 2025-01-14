using System;
using System.Collections;
using System.Collections.Generic;

namespace MVirus.Data
{
    public class RingBuffer<T> : ICollection<T>
    {
        private int size;
        private readonly T[] buffer;
        private long tail;
        private long head;

        public int FreeSize => buffer.Length - size;
        public int Count => size;
        public int MaxCount => buffer.Length;
        public bool IsReadOnly => false;

        public RingBuffer(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            tail = 0;
            head = 0;
            size = 0;
            buffer = new T[bufferSize];
        }

        public void Add(T value)
        {
            if (size == buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(size));

            buffer[head] = value;

            head++;
            size++;
            if (head >= buffer.Length)
                head = 0;
        }

        public void Add(T[] items)
        {
            ThrowWhenNoSpace(items.Length);

            var nextHead = head + items.Length;

            if (nextHead >= buffer.Length)
            {
                nextHead = nextHead - buffer.Length;
                long firtsCopyCount = buffer.Length - head;

                Array.Copy(items, 0, buffer, head, firtsCopyCount);
                Array.Copy(items, firtsCopyCount, buffer, 0, items.Length - firtsCopyCount);
            } else
            {
                Array.Copy(items, 0, buffer, head, items.Length);
            }

            head = nextHead;
            size += items.Length;
        }

        public void Clear()
        {
            tail = 0;
            head = 0;
            size = 0;
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, size);
        }

        public void CopyTo(T[] array, int arrayIndex, long count)
        {
            if (count < 0 || count > size) throw new ArgumentOutOfRangeException("count");

            if (head > tail)
            {
                Array.Copy(buffer, tail, array, arrayIndex, count);
            } else
            {
                long firstSize = buffer.Length - tail;
                Array.Copy(buffer, tail, array, arrayIndex, Math.Min(firstSize, count));

                count -= firstSize;

                if (count <= 0)
                    return;

                Array.Copy(buffer, 0, array, arrayIndex + firstSize, count);
            }
        }

        public void Discard(int count)
        {
            if (count < 0 || count > size) throw new ArgumentOutOfRangeException("count");

            var newTail = tail + count;
            if (newTail >= buffer.Length)
                tail = newTail - buffer.Length;
            else
                tail = newTail;

            size -= count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public T Get(int index) {
            if (index < 0 || index >= size) throw new ArgumentOutOfRangeException();

            throw new NotImplementedException();
        }

        private void ThrowWhenNoSpace(int reqSpace)
        {
            if (FreeSize < reqSpace)
                throw new ArgumentException("No free size");
        }
    }

}
