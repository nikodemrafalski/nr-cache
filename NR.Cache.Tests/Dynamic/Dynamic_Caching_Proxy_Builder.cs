using Moq;
using NR.Cache;
using NR.Cache.Dynamic;
using NR.Cache.Tests;
using NUnit.Framework;

namespace Dynamic_Caching_Proxy_Builder
{
    public class When_calling_a_method_via_the_proxy : TestFlow
    {
        private Mock<IFoo> _targetObject;
        private IFoo _proxy;

        public interface IFoo
        {
            void Bar();
        }

        protected override void Arrange()
        {
            _targetObject = new Mock<IFoo>();
            _proxy = new CachingProxyConfiguration<IFoo>(_targetObject.Object, new DynamicCacheBuilder()).Build();
        }

        protected override void Act()
        {
            _proxy.Bar();
        }

        [Test]
        public void Method_on_the_proxied_object_should_be_called()
        {
            _targetObject.Verify(x => x.Bar());
        }
    }
}
