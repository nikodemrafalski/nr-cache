namespace NR.Cache
{
    internal class CachingProxyConfiguration<T> : ICachingProxyConfiguration<T> where T : class
    {
        private readonly ICachingProxyBuilder _builder;
        private readonly T _instance;

        public CachingProxyConfiguration(ICachingProxyBuilder builder)
        {
            _builder = builder;
        }

        internal CachingProxyConfiguration(T instance, ICachingProxyBuilder builder)
            : this(builder)
        {
            _instance = instance;
        }

        public T TargetObject
        {
            get { return _instance; }
        }

        public T Build()
        {
            return _builder.BuildProxy(this);
        }

        public ICachingProxyConfiguration<T> ForInstance(T instance)
        {
            return new CachingProxyConfiguration<T>(instance, _builder);
        }
    }
}