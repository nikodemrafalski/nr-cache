namespace NR.Cache.DynamicProxy
{
    public class ProxyFactory
    {
        private static ProxyFactory _instance;
        private readonly ProxyWithTargetFactory _proxyWithTargetFactory;
       
        private ProxyFactory(ProxyWithTargetFactory proxyWithTargetFactory)
        {
            _proxyWithTargetFactory = proxyWithTargetFactory;
        }

        public IProxyWithTarget<T> CreateProxyWithTarget<T>()
        {
            return _proxyWithTargetFactory.CreateProxy<T>();
        }

        public static ProxyFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProxyFactory(new ProxyWithTargetFactory());
                }

                return _instance;
            }
        }
    }
}