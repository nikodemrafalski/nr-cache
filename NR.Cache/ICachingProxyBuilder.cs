namespace NR.Cache
{
    public interface ICachingProxyBuilder
    {
        T BuildProxy<T>(ICachingProxyConfiguration<T> configuration) where T : class;
    }
}