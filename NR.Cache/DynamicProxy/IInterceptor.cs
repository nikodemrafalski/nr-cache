using System.Reflection;

namespace NR.Cache.Dynamic
{
    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }

    public interface IInvocation
    {
        MethodInfo Method { get; }

        object[] Parameters { get; }

        void SetReturnValue(object returnValue);
    }
}