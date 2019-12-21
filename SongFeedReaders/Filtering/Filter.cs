using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Filtering
{
    public abstract class Filter
    {
        public string PropertyName { get; }
        public abstract Type Type { get; }
        public bool IsValid { get; protected set; }
        public virtual bool IsValidForObject(IFilterableObject obj)
        {
            return Type.IsAssignableFrom(obj.GetType());
        }
        public List<Filter> AndFilters { get; }
        public List<Filter> OrFilters { get; }
        public List<Filter> NotFilters { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public abstract bool InvokeForObject(IFilterableObject obj);
    }

    public abstract class Filter<T, TValue>
        : Filter where T : IFilterableObject
    {
        public TValue Value { get; }
        
        public override Type Type => typeof(T);
        public Predicate<T> GetPredicate()
        {
            var pred = new Predicate<T>(t => AndFilters.TrueForAll(m => m.InvokeForObject(t))
                            && OrFilters.Any(m => m.InvokeForObject(t))
                            && !NotFilters.Any(m => m.InvokeForObject(t)));
            return pred;
        }
        

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool InvokeForObject(IFilterableObject obj)
        {
            if (obj is T castObj)
                return InvokeForObject(castObj);
            else
                throw new ArgumentException($"Cannot invoke predicate for an object of type {Type.Name} on an object of type {obj.GetType().Name}.", nameof(obj));

        }

        public bool InvokeForObject(T obj)
        {
            return GetPredicate().Invoke(obj);
        }
    }

    public class BeatMapFilter<TValue>
        : Filter<BeatMap, TValue>
    {

    }

    public class BeatMap
        : IFilterableObject
    {
        public string Name;
        public bool Is360;
        public int BPM;
    }
}
