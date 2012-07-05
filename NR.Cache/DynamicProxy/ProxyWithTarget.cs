using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace NR.Cache.DynamicProxy
{
    internal class ProxyWithTarget<T> : IProxyWithTarget<T>
    {
        private readonly Func<T, IInterceptor[], T> _createInstanceDelegate;

        public ProxyWithTarget(Type proxyType)
        {
            ProxyType = proxyType;
            _createInstanceDelegate = CreateConstructorCaller(proxyType);
        }

        public Type ProxyType { get; private set; }

        public T CreateInstance(T targetObject, IEnumerable<IInterceptor> interceptors)
        {
            return _createInstanceDelegate(targetObject, interceptors.ToArray());
        }

        private static Func<T, IInterceptor[], T> CreateConstructorCaller(Type proxyType)
        {
            var ctorInfo = proxyType.GetConstructor(new[] { typeof(T), typeof(IInterceptor[]) });
            Debug.Assert(ctorInfo != null, "No valid proxy constructor found.");

            ParameterExpression targetParameter = Expression.Parameter(typeof(T), "target");
            ParameterExpression interceptorsParameter = Expression.Parameter(typeof(IInterceptor[]), "interceptors");
            NewExpression constructorExpression = Expression.New(ctorInfo, targetParameter, interceptorsParameter);

            var expression = Expression.Lambda<Func<T, IInterceptor[], T>>(constructorExpression,
                                                                         targetParameter, interceptorsParameter);
            return expression.Compile();
        }
    }
}