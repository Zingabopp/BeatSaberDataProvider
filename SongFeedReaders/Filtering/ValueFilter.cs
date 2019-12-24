using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Filtering
{
    public abstract class ValueFilter
    {
        public abstract Type Type { get; }

        public abstract Predicate<object> Match(object other);
    }

    public abstract class ValueFilter<T>
        : ValueFilter
    {
        public override Type Type => typeof(T);
        public T Value { get; }
        public override Predicate<object> Match(object other)
        {
            if (!(other is T casted))
                throw new ArgumentException($"{other.GetType().Name} does not match ValueFilter Type {Type.Name}");
            throw new NotImplementedException();
            //return Match(casted);
        }
        public abstract Predicate<T> Match(T other);
    }
}
