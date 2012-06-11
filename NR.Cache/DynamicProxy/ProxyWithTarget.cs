using System;
using System.Linq.Expressions;

namespace NR.Cache.DynamicProxy
{
    internal class ProxyWithTarget<T> : IProxyWithTarget<T>
    {
        private readonly Func<T, IInterceptor, T> _createInstanceDelegate;

        public ProxyWithTarget(Type proxyType)
        {
            ProxyType = proxyType;
            _createInstanceDelegate = CreateConstructorCaller(proxyType);
        }

        public Type ProxyType { get; private set; }

        public T CreateInstance(T targetObject, IInterceptor interceptor)
        {
            return _createInstanceDelegate(targetObject, interceptor);
        }

        private static Func<T, IInterceptor, T> CreateConstructorCaller(Type proxyType)
        {
            var ctorInfo = proxyType.GetConstructor(new[] { typeof(T), typeof(IInterceptor) });
            ParameterExpression targetParameter = Expression.Parameter(typeof(T), "target");
            ParameterExpression interceptorParameter = Expression.Parameter(typeof(IInterceptor), "interceptor");
            NewExpression constructorExpression = Expression.New(ctorInfo, targetParameter, interceptorParameter);

            var expression = Expression.Lambda<Func<T, IInterceptor, T>>(constructorExpression,
                                                                         targetParameter, interceptorParameter);
            return expression.Compile();
        }
    }
}