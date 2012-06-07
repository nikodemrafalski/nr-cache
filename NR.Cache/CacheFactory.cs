using System;

namespace NR.Cache
{
    public class CacheFactory
    {
        internal ICachingProxyBuilder Builder { get; set; }

        public CacheFactory()
        {
            // TODO: expose configuration of this building block
            Builder = new Remoting.RemotingCacheBuilder();
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
