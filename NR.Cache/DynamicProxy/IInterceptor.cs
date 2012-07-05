using System.Reflection;

namespace NR.Cache.DynamicProxy
{
    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }

    public interface IInvocation
    {
        object Proxy { get; }

        MethodInfo ProxiedMethod { get; }

        object[] Arguments { get; }

        object ReturnValue { get; set; }

        void Continue();
    }
}