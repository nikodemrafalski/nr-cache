using System;

namespace NR.Cache.Remoting
{
    internal class RemotingCacheBuilder : ICachingProxyBuilder
    {
        public T BuildProxy<T>(ICachingProxyConfiguration<T> configuration) where T : class
        {
            throw new NotImplementedException();
        }
    }
}