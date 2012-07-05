using System;
using System.Collections.Generic;
using FluentAssertions;
using NR.Cache.DynamicProxy;
using NR.Cache.Tests;
using NUnit.Framework;
using Rhino.Mocks;

namespace Dynamic_Proxy_With_Target
{
    public class When_interceptor_modifies_invocation_arguments : TestFlow
    {
        private class Interceptor : IInterceptor
        {
            private readonly string _parameterModification;

            public Interceptor(string parameterModification)
            {
                _parameterModification = parameterModification;
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.Arguments[0] = _parameterModification;
                invocation.Continue();
            }
        }

        public interface IFoo
        {
            void Bar(string argument);
        }

        private IFoo _targetObject;
        private IFoo _proxy;

        private string _originalParameter;
        private string _modifiedParameter;

        protected override void Arrange()
        {
            _targetObject = A.Mock<IFoo>();

            _originalParameter = "Freddie";
            _modifiedParameter = "Jason";

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { new Interceptor(_modifiedParameter) });
        }

        protected override void Act()
        {
            _proxy.Bar(_originalParameter);
        }

        [Test]
        public void Target_method_should_be_invoced_with_modified_argument()
        {
            _targetObject.AssertWasCalled(x => x.Bar(_modifiedParameter));
        }
    }

    public class When_at_least_one_interceptor_does_not_call_Continue_on_invocation : TestFlow
    {
        private IService _service;
        private IService _serviceProxy;

        public interface IService
        {
            string[] ServiceMethod(IList<int> param);
        }

        private class Interceptor1 : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                //do something
                invocation.Continue();
            }
        }

        private class Interceptor2 : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                //do something and forget about invocation
            }
        }

        protected override void Arrange()
        {
            _service = MockRepository.GenerateMock<IService>();

            _serviceProxy = ProxyFactory.Instance.CreateProxyWithTarget<IService>().CreateInstance(
                _service, new List<IInterceptor> { new Interceptor1(), new Interceptor2() });
        }

        protected override void Act()
        {
            _serviceProxy.ServiceMethod(new List<int>());
        }

        [Test]
        public void Method_on_proxied_object_should__not_be_invoked_at_all()
        {
            _service.AssertWasNotCalled(x => x.ServiceMethod(Arg<IList<int>>.Is.Anything));
        }
    }

    public class When_every_interceptor_calls_Continue_on_invocation : TestFlow
    {
        private IService _service;
        private IService _serviceProxy;

        public interface IService
        {
            string[] ServiceMethod(IList<int> param);
        }

        private class Interceptor1 : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                //do something
                invocation.Continue();
            }
        }

        private class Interceptor2 : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                //do something else
                invocation.Continue();
            }
        }

        protected override void Arrange()
        {
            _service = MockRepository.GenerateMock<IService>();

            _serviceProxy = ProxyFactory.Instance.CreateProxyWithTarget<IService>().CreateInstance(
                _service, new List<IInterceptor> { new Interceptor1(), new Interceptor2() });
        }

        protected override void Act()
        {
            _serviceProxy.ServiceMethod(new List<int>());
        }

        [Test]
        public void Finally_the_method_on_proxied_object_should_be_invoked()
        {
            _service.AssertWasCalled(x => x.ServiceMethod(Arg<IList<int>>.Is.Anything));
        }
    }

    public class When_intercepting_nonvoid_method_call : TestFlow
    {
        private class Interceptor : IInterceptor
        {
            private readonly int _value;

            public Interceptor(int value)
            {
                _value = value;
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.ReturnValue = _value;
            }
        }

        private IFoo _targetObject;
        private IFoo _proxy;

        private int _newReturnValue;
        private int _someReturnValue;
        private int _resultFromProxy;

        public interface IFoo
        {
            int Bar();
        }

        protected override void Arrange()
        {
            _someReturnValue = 10;
            _newReturnValue = 20000;

            _targetObject = A.Mock<IFoo>();
            _targetObject.Stub(x => x.Bar()).Return(_someReturnValue);

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { new Interceptor(_newReturnValue) });
        }

        protected override void Act()
        {
            _resultFromProxy = _proxy.Bar();
        }

        [Test]
        public void Interceptor_should_be_able_to_alter_return_value()
        {
            _resultFromProxy.Should().Be(_newReturnValue);
        }
    }

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


            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor>());
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

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor>());
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
            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { });
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

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { });
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

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { });
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

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { });
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

    public class When_calling_a_generic_method_returning_something_via_the_proxy_without : TestFlow
    {
        private IFoo _targetObject;
        private IFoo _proxy;


        private DateTime _someParameter;
        private string _someReturnValue;
        private string _resultFromProxy;

        public interface IFoo
        {
            TResult Bar<TParam, TResult>(TParam param);
        }

        protected override void Arrange()
        {
            _someParameter = DateTime.Today;
            _someReturnValue = "some return value";
            

            _targetObject = A.Mock<IFoo>();
            _targetObject.Stub(x => x.Bar<DateTime, string>(Arg<DateTime>.Is.Anything)).Return(_someReturnValue);

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor>());
        }

        protected override void Act()
        {
            _resultFromProxy = _proxy.Bar<DateTime, string>(_someParameter);
        }

        [Test]
        public void Method_on_the_proxied_object_should_be_called_with_same_as_proxy()
        {
            _targetObject.AssertWasCalled(x => x.Bar<DateTime,string>(_someParameter));
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

            _proxy = new ProxyWithTargetFactory().CreateProxy<IFoo>().CreateInstance(_targetObject, new List<IInterceptor> { });
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
