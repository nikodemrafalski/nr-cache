using System;
using NR.Cache.Dynamic;

namespace NR.Cache
{
    public class CacheFactory
    {
        internal ICachingProxyBuilder Builder { get; set; }

        public CacheFactory()
        {
            // TODO: expose configuration of this building block
            Builder = new DynamicCacheBuilder();
        }

        public ICachingProxyConfiguration<T> CreateCachingProxy<T>() where T : class
        {
            if (!typeof(T).IsInterface)
            {
                throw new NotSupportedException("Only interfaces are supported.");
            }

            return new CachingProxyConfiguration<T>(Builder);
        }
    }
}
