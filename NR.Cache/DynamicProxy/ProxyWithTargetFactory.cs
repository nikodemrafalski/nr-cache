using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NR.Cache.DynamicProxy
{
    internal class ProxyWithTargetFactory
    {
        private readonly bool _cachingEnabled;
        private readonly Dictionary<int, Type> _proxiesCache = new Dictionary<int, Type>();

        public ProxyWithTargetFactory()
            : this(true)
        {
        }

        public ProxyWithTargetFactory(bool cachingEnabled)
        {
            _cachingEnabled = cachingEnabled;
        }

        public IProxyWithTarget<T> CreateProxy<T>()
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new NotSupportedException("Only interface proxies are supported");
            }

            if (!_cachingEnabled)
            {
                return new ProxyWithTarget<T>(CreateProxyType(interfaceType));
            }

            int cacheKey = interfaceType.GetHashCode();
            Type proxyType;
            lock (typeof(ProxyWithTargetFactory))
            {
                if (!_proxiesCache.TryGetValue(cacheKey, out proxyType))
                {
                    proxyType = CreateProxyType(interfaceType);
                    _proxiesCache.Add(cacheKey, proxyType);
                }
            }

            return new ProxyWithTarget<T>(proxyType);
        }

        private Type CreateProxyType(Type proxyInterface)
        {
            //TODO: Implement caching of the Assembly and Module builders
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ProxiesAssembly"),
                AssemblyBuilderAccess.Run);

            ModuleBuilder dynamicModule = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder dynamicType = dynamicModule.DefineType(proxyInterface.Name + "_Proxy", TypeAttributes.Public | TypeAttributes.Class);
            dynamicType.AddInterfaceImplementation(proxyInterface);

            FieldBuilder targetField = dynamicType.DefineField("_target", proxyInterface, FieldAttributes.Private);
            FieldBuilder interceptorField = dynamicType.DefineField("_interceptor", typeof(IInterceptor), FieldAttributes.Private);

            foreach (var interfaceMethod in proxyInterface.GetMethods())
            {
                var methodParameters = interfaceMethod.GetParameters();

                MethodBuilder dynamicMethod = dynamicType.DefineMethod(
                    interfaceMethod.Name,
                    MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.HasThis,
                    interfaceMethod.ReturnType,
                    interfaceMethod.GetParameters().Select(x => x.ParameterType).ToArray());

                ILGenerator il = dynamicMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, targetField);
                for (int argIndex = 1; argIndex <= methodParameters.Length; argIndex++)
                {
                    il.Emit(OpCodes.Ldarg, argIndex);
                }

                il.Emit(OpCodes.Callvirt, interfaceMethod);
                il.Emit(OpCodes.Ret);
            }

            EmitProxyConstructor(dynamicType, interceptorField, targetField);
            Type proxyType = dynamicType.CreateType();
            return proxyType;
        }

        private static void EmitProxyConstructor(TypeBuilder typeBuilder, FieldInfo interceptorField, FieldInfo targetField)
        {
            ConstructorInfo objectCtor = typeof(object).GetConstructor(new Type[0]);

            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public,
                                                                           CallingConventions.Standard, new[] { targetField.FieldType, interceptorField.FieldType });

            ILGenerator il = ctorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, objectCtor);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, targetField);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, interceptorField);

            il.Emit(OpCodes.Ret);
        }
    }
}