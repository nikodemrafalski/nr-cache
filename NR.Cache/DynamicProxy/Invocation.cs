using System;
using System.Reflection;

namespace NR.Cache.DynamicProxy
{
    public abstract class Invocation : IInvocation
    {
        private readonly object _proxy;
        private readonly object[] _arguments;
        private readonly IInterceptor[] _interceptors;
        private readonly MethodInfo _proxiedMethod;
        private int _interceptorIndex = -1;

        protected Invocation(object proxy, MethodInfo proxiedMethod, object[] arguments, IInterceptor[] interceptors)
        {
            _proxy = proxy;
            _proxiedMethod = proxiedMethod;
            _arguments = arguments;
            _interceptors = interceptors ?? new IInterceptor[0];
        }

        public void Continue()
        {
            _interceptorIndex++;
            if (_interceptorIndex < _interceptors.Length)
            {
                _interceptors[_interceptorIndex].Intercept(this);
                return;
            }

            InvokeTargetMethod();
        }

        /// <summary>
        /// Invokes the <see cref="ProxiedMethod"/> with arguments stored in <see cref="Arguments"/> arrays 
        /// and stores the result in <see cref="ReturnValue"/> property.
        /// </summary>
        public abstract void InvokeTargetMethod();

        public object Proxy { get { return _proxy; } }

        public MethodInfo ProxiedMethod { get { return _proxiedMethod; } }

        public object[] Arguments { get { return _arguments; } }

        public object ReturnValue { get; set; }
    }
}