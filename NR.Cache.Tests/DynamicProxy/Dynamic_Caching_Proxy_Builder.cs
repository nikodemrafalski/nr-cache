using System.Collections.Generic;
using FluentAssertions;
using NR.Cache;
using NR.Cache.Dynamic;
using NR.Cache.Tests;
using NUnit.Framework;
using Rhino.Mocks;

namespace Dynamic_Caching_Proxy_Builder
{
    public class When_calling_a_method_returning_void_via_the_proxy : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        public interface IFoo
        {
            void Bar();
        }

        protected override void Arrange()
        {
            _targetObject = MockRepository.GenerateMock<IFoo>();
            _proxy = new CachingProxyConfiguration<IFoo>(_targetObject, new DynamicCacheBuilder()).Build();
        }

        protected override void Act()
        {
            _proxy.Bar();
        }

        [Test]
        public void Method_on_the_proxied_object_should_be_called()
        {
            _targetObject.AssertWasCalled(x => x.Bar());
        }
    }

    public class When_calling_a_method_returning_something_via_the_proxy : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        private int _someReturnValue;
        private int _resultFromProxy;

        public interface IFoo
        {
            int Bar();
        }

        protected override void Arrange()
        {
            _someReturnValue = 10;

            _targetObject = MockRepository.GenerateMock<IFoo>();
            _targetObject.Stub(x => x.Bar()).Return(_someReturnValue);
  
            _proxy = new CachingProxyConfiguration<IFoo>(_targetObject, new DynamicCacheBuilder()).Build();
        }

        protected override void Act()
        {
            _resultFromProxy = _proxy.Bar();
        }

        [Test]
        public void Method_on_the_proxied_object_should_be_called()
        {
            _targetObject.AssertWasCalled(x => x.Bar());
        }

        [Test]
        public void Proxy_should_return_the_result_produced_by_the_target_object()
        {
            _resultFromProxy.Should().Be(_someReturnValue);
        }
    }

    public class When_calling_a_method_via_the_proxy_passing_some_parameters : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        private int _someValueTypeParameter;
        private IList<int> _someReferenceTypeParameter;

        public interface IFoo
        {
            void Bar(int valueParameter, IList<int> referenceParameter);
        }

        protected override void Arrange()
        {
            _someValueTypeParameter = 11;
            _someReferenceTypeParameter = new List<int> { 200, 111 };

            _targetObject = MockRepository.GenerateStub<IFoo>();

            _proxy = new CachingProxyConfiguration<IFoo>(_targetObject, new DynamicCacheBuilder()).Build();
        }

        protected override void Act()
        {
            _proxy.Bar(_someValueTypeParameter, _someReferenceTypeParameter);
        }

        [Test]
        public void Method_on_the_proxied_object_should_be_called()
        {
            _targetObject.AssertWasCalled(x => x.Bar(Arg<int>.Is.Anything, Arg<IList<int>>.Is.Anything));

        }

        [Test]
        public void Method_on_the_target_object_should_be_invoked_with_exact_same_arguments()
        {
            _targetObject.AssertWasCalled(x => x.Bar(Arg.Is(_someValueTypeParameter), Arg.Is(_someReferenceTypeParameter)));
        }
    }
}
