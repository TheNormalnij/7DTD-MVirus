using System;
using System.Collections;
using System.Collections.Generic;

namespace MVirus.Client
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
            if (head > buffer.Length)
                head = 0;
        }

        public void Add(T[] items)
        {
            ThrowWhenNoSpace(items.Length);

            var nextHead = head + items.Length;

            if (nextHead > buffer.Length)
            {
                nextHead = nextHead - buffer.Length;

                for (long i = head; i <  buffer.Length; i++)
                    buffer[i] = items[i - head];

                var writted = buffer.Length - head;
                for (long i = 0; i < nextHead; i++)
                    buffer[i] = items[writted + i];

                head = nextHead;
            } else
            {
                items.CopyTo(buffer, head);
            }

            size = size + items.Length;
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

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (count < 0 || count > size) throw new ArgumentOutOfRangeException("count");

            if (head > tail)
            {
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = buffer[tail + i];
                }
            } else
            {
                throw new NotImplementedException();
            }
        }

        public void Discard(int count)
        {
            var newTail = tail + count;
            if (newTail > buffer.Length)
                tail = buffer.Length - newTail;
            else
                tail = newTail;
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
