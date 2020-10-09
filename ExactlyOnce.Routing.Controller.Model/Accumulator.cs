using System.Collections;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public struct Accumulator<T> : IEnumerable<T>
    {
        static readonly List<T> emptyValue = new List<T>();
        List<T> values;

        public void Add(T value)
        {
            if (values == null)
            {
                values = new List<T>();
            }
            values.Add(value);
        }

        public List<T> ToList()
        {
            return values ?? emptyValue;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (values ?? emptyValue).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}