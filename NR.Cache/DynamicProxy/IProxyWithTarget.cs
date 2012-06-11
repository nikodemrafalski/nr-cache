using System;

namespace NR.Cache.DynamicProxy
{
    public interface IProxyWithTarget<T>
    {
        Type ProxyType{ get; }

        T CreateInstance(T targetObject, IInterceptor interceptor);
    }
}