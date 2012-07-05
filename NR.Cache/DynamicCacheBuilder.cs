using System.Collections.Generic;
using NR.Cache.DynamicProxy;

namespace NR.Cache.Dynamic
{
    internal class DynamicCacheBuilder : ICachingProxyBuilder
    {
        public T BuildProxy<T>(ICachingProxyConfiguration<T> configuration) where T : class
        {
            var proxy = ProxyFactory.Instance.CreateProxyWithTarget<T>();
            return proxy.CreateInstance(configuration.TargetObject, new List<IInterceptor>{new DummyInterceptor()});
        }
    }

    internal class DummyInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Continue();
        }
    }
}