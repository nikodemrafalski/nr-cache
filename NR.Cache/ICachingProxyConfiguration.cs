namespace NR.Cache
{
    public interface ICachingProxyConfiguration<T>
        where T : class
    {
        T Build();

        ICachingProxyConfiguration<T> ForInstance(T instance);
    }
}