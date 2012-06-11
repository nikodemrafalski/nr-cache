using System.Collections.Generic;
using FluentAssertions;
using NR.Cache.DynamicProxy;
using NR.Cache.Tests;
using NUnit.Framework;
using Rhino.Mocks;

namespace Dynamic_Proxy_With_Target
{
    public class When_calling_an_indexer_getter_via_the_proxy : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        private string _someReturnValue;
        private string _resultFromProxy;

        public interface IFoo
        {
            string this[int index] { get; set; }
        }

        protected override void Arrange()
        {
            _someReturnValue = "Indexer get";

            _targetObject = MockRepository.GenerateMock<IFoo>();
            _targetObject.Stub(x => x[0]).Return(_someReturnValue);

            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
        }

        protected override void Act()
        {
            _resultFromProxy = _proxy[0];
        }

        [Test]
        public void Proxy_should_return_value_from_target_objects_indexer()
        {
            _resultFromProxy.Should().Be(_someReturnValue);
        }
    }

    public class When_calling_an_indexer_setter_via_the_proxy : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        private decimal _someValueToSet;

        public interface IFoo
        {
            decimal this[string index] { get; set; }
        }

        protected override void Arrange()
        {
            _someValueToSet = 122M;

            _targetObject = A.Mock<IFoo>();

            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
        }

        protected override void Act()
        {
            _proxy["index"] = _someValueToSet;
        }

        [Test]
        public void Setter_on_the_proxied_object_should_be_called_with_value_passed_to_proxy()
        {
            _targetObject.AssertWasCalled(x => x["index"] = _someValueToSet);
        }
    }

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
            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
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

    public class When_calling_a_property_getter_via_the_proxy : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        private int _someReturnValue;
        private int _resultFromProxy;

        public interface IFoo
        {
            int Bar { get; set; }
        }

        protected override void Arrange()
        {
            _someReturnValue = 200;
            _targetObject = A.Mock<IFoo>();
            _targetObject.Stub(x => x.Bar).Return(_someReturnValue);

            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
        }

        protected override void Act()
        {
            _resultFromProxy = _proxy.Bar;
        }

        [Test]
        public void Getter_on_the_proxied_object_should_be_called()
        {
            _targetObject.AssertWasCalled(x => x.Bar);
        }

        [Test]
        public void Proxy_should_return_value_from_target_objects_property()
        {
            _resultFromProxy.Should().Be(_someReturnValue);
        }
    }

    public class When_calling_a_property_setter_via_the_proxy : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;

        private string _someValueToSet;

        public interface IFoo
        {
            string Bar { get; set; }
        }

        protected override void Arrange()
        {
            _someValueToSet = "Hello";
            _targetObject = A.Mock<IFoo>();

            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
        }

        protected override void Act()
        {
            _proxy.Bar = _someValueToSet;
        }

        [Test]
        public void Setter_on_the_proxied_object_should_be_called_with_value_passed_to_proxy()
        {
            _targetObject.AssertWasCalled(x => x.Bar = _someValueToSet);
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

            _targetObject = A.Mock<IFoo>();
            _targetObject.Stub(x => x.Bar()).Return(_someReturnValue);

            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
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

            _targetObject = A.Stub<IFoo>();

            var interceptor = A.Mock<IInterceptor>();
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, interceptor);
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
