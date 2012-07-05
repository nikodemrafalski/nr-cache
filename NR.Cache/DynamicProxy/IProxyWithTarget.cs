using System;
using System.Collections.Generic;

namespace NR.Cache.DynamicProxy
{
    public interface IProxyWithTarget<T>
    {
        Type ProxyType{ get; }

        T CreateInstance(T targetObject, IEnumerable<IInterceptor> interceptors);
    }
}