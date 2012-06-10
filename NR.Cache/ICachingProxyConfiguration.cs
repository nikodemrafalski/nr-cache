namespace NR.Cache
{
    public interface ICachingProxyConfiguration<T>
        where T : class
    {
        T TargetObject { get;  }

        T Build();

        ICachingProxyConfiguration<T> ForInstance(T instance);
    }
}