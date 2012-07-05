namespace NR.Cache.DynamicProxy
{
    public class ProxyFactory
    {
        private static ProxyFactory _instance;
        private readonly ProxyWithTargetFactory _proxyWithTargetFactory;
        private readonly DynamicModuleStore _dynamicModuleStore = new DynamicModuleStore();
       
        private ProxyFactory()
        {
            _dynamicModuleStore = new DynamicModuleStore();
            _proxyWithTargetFactory = new ProxyWithTargetFactory(true, _dynamicModuleStore);
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
                    _instance = new ProxyFactory();
                }

                return _instance;
            }
        }
    }
}