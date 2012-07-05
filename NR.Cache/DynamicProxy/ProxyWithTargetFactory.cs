using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            : this(false)
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

            if (interfaceType.GetMethods().Any(x => x.IsGenericMethod))
            {
                throw new NotSupportedException("Generic methods are not supported yet.");
            }

            if (!_cachingEnabled)
            {
                return new ProxyWithTarget<T>(EmitProxyType(interfaceType));
            }

            int cacheKey = interfaceType.GetHashCode();
            Type proxyType;
            lock (typeof(ProxyWithTargetFactory))
            {
                if (!_proxiesCache.TryGetValue(cacheKey, out proxyType))
                {
                    proxyType = EmitProxyType(interfaceType);
                    _proxiesCache.Add(cacheKey, proxyType);
                }
            }

            return new ProxyWithTarget<T>(proxyType);
        }

        private Type EmitProxyType(Type proxyInterface)
        {
            TypeBuilder typeBuilder = DynamicModuleStore.Module.DefineType(proxyInterface.FullName + "_Proxy" + DateTime.Now.Ticks,
                TypeAttributes.Public | TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(proxyInterface);

            FieldBuilder targetField = typeBuilder.DefineField("_target", proxyInterface, FieldAttributes.Private);
            FieldBuilder interceptorsField = typeBuilder.DefineField("_interceptors", typeof(IInterceptor[]), FieldAttributes.Private);

            foreach (var interfaceMethod in proxyInterface.GetMethods())
            {
                var methodParameters = interfaceMethod.GetParameters();
                var parameterTypes = methodParameters.Select(x => x.ParameterType).ToArray();

                Type invocationType = EmitInvocationImpl(proxyInterface, interfaceMethod);
                ConstructorInfo invocationTypeCtor = invocationType.GetConstructor(
                    new[] { proxyInterface, proxyInterface, typeof(MethodInfo), typeof(object[]), typeof(IInterceptor[]) });

                Debug.Assert(invocationTypeCtor != null, "invocationTypeCtor != null");

                MethodAttributes attributes = MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual;
                if (interfaceMethod.IsSpecialName)
                {
                    attributes = attributes | MethodAttributes.SpecialName;
                }

                MethodBuilder dynamicMethod = typeBuilder.DefineMethod(
                    interfaceMethod.Name,
                    attributes,
                    CallingConventions.HasThis,
                    interfaceMethod.ReturnType,
                    interfaceMethod.GetParameters().Select(x => x.ParameterType).ToArray());

                ILGenerator il = dynamicMethod.GetILGenerator();

                // creating invocation object - pushing parameters onto the stack
                // parameter: target object
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, targetField);

                // parameter: proxy - self reference
                il.Emit(OpCodes.Ldarg_0);

                // parameter: proxied proxiedMethod
                il.Emit(OpCodes.Ldtoken, interfaceMethod);
                il.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) }));
                il.Emit(OpCodes.Castclass, typeof(MethodInfo));

                // parameter: arguments
                LocalBuilder args = il.DeclareLocal(typeof(object[]));
                il.Emit(OpCodes.Ldc_I4_S, parameterTypes.Length);
                il.Emit(OpCodes.Newarr, typeof(object));
                il.Emit(OpCodes.Stloc, args);

                if (parameterTypes.Length > 0)
                {
                    il.Emit(OpCodes.Ldloc, args);
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, i);
                        il.Emit(OpCodes.Ldarg, i + 1);

                        Type parameterType = parameterTypes[i];
                        if (parameterType.IsValueType)
                        {
                            il.Emit(OpCodes.Box, parameterType);
                        }

                        il.Emit(OpCodes.Stelem_Ref);
                        il.Emit(OpCodes.Ldloc, args);
                    }

                    il.Emit(OpCodes.Stloc, args);
                }

                il.Emit(OpCodes.Ldloc, args);

                // parameter: interceptors
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, interceptorsField);

                // creating invocation object - call invocation constructor with parameter waiting on the stack
                il.Emit(OpCodes.Newobj, invocationTypeCtor);

                // store invocation object in local variable
                LocalBuilder invocationLocal = il.DeclareLocal(invocationType);

                il.Emit(OpCodes.Stloc, invocationLocal);
                il.Emit(OpCodes.Ldloc, invocationLocal);

                // let the interceptors in - method on target objat MAY be invoked (if every interceptor continues)
                il.Emit(OpCodes.Callvirt, typeof(Invocation).GetMethod("Continue"));

                // interceptors have finished, if required put return value onto stack and return
                if (interfaceMethod.ReturnType != typeof(void))
                {
                    il.Emit(OpCodes.Ldloc, invocationLocal);
                    il.Emit(OpCodes.Callvirt, typeof(Invocation).GetMethod("get_ReturnValue"));
                    if (interfaceMethod.ReturnType.IsValueType)
                    {
                        Label isNull = il.DefineLabel();
                        Label notNull = il.DefineLabel();
                        Label end = il.DefineLabel();
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Beq_S, isNull);
                        il.Emit(OpCodes.Br_S, notNull);

                        il.MarkLabel(isNull);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ldloca, 0);
                        il.Emit(OpCodes.Initobj, interfaceMethod.ReturnType);
                        il.Emit(OpCodes.Ldloc_0);

                        il.Emit(OpCodes.Br_S, end);

                        il.MarkLabel(notNull);
                        il.Emit(OpCodes.Unbox, interfaceMethod.ReturnType);
                        il.Emit(OpCodes.Ldobj, interfaceMethod.ReturnType);
                        il.MarkLabel(end);
                    }
                }

                il.Emit(OpCodes.Ret);
            }

            EmitProxyConstructor(typeBuilder, targetField, interceptorsField);
            Type proxyType = typeBuilder.CreateType();
            return proxyType;
        }

        private static void EmitProxyConstructor(TypeBuilder typeBuilder, FieldInfo targetField, FieldInfo interceptorsField)
        {
            ConstructorInfo objectCtor = typeof(object).GetConstructor(Type.EmptyTypes);
            Debug.Assert(objectCtor != null, "objectCtor != null");
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new[] { targetField.FieldType, interceptorsField.FieldType });

            ILGenerator il = ctorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, objectCtor);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, targetField);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, interceptorsField);

            il.Emit(OpCodes.Ret);
        }

        private Type EmitInvocationImpl(Type proxyInterface, MethodInfo proxiedMethod)
        {
            TypeBuilder typeBuilder = DynamicModuleStore.Module.DefineType("InvocationImpl" + Guid.NewGuid().ToString("N"),
                TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.BeforeFieldInit, typeof(Invocation));

            FieldInfo targetField = typeBuilder.DefineField("_target", proxyInterface, FieldAttributes.Private);

            var baseCtorParams = new[] { typeof(object), typeof(MethodInfo), typeof(object[]), typeof(IInterceptor[]) };
            ConstructorInfo baseCtor = typeof(Invocation).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.HasThis, baseCtorParams, null);

            Debug.Assert(baseCtor != null, "baseCtor != null");

            // ctor
            // emit the constructor which calls the base one and sets target object field
            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard,
                new[] { proxyInterface, proxyInterface, typeof(MethodInfo), typeof(object[]), typeof(IInterceptor[]) });

            ILGenerator ctorIL = ctor.GetILGenerator();

            // invoke base ctor with: proxy, proxyMethod, arguments, interceptors
            ctorIL.Emit(OpCodes.Ldarg, 0);
            ctorIL.Emit(OpCodes.Ldarg, 2);
            ctorIL.Emit(OpCodes.Ldarg, 3);
            ctorIL.Emit(OpCodes.Ldarg, 4);
            ctorIL.Emit(OpCodes.Ldarg, 5);
            ctorIL.Emit(OpCodes.Call, baseCtor);

            // store the object which is proxy target in a field
            ctorIL.Emit(OpCodes.Ldarg, 0);
            ctorIL.Emit(OpCodes.Ldarg, 1);
            ctorIL.Emit(OpCodes.Stfld, targetField);

            ctorIL.Emit(OpCodes.Ret);
            var parameterTypes = proxiedMethod.GetParameters().Select(x => x.ParameterType).ToArray();

            // proxiedMethod
            // emit proxiedMethod which implements abstract Invocation.InvokeTargetMethod
            MethodBuilder invokeTarget = typeBuilder.DefineMethod("InvokeTargetMethod",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.ReuseSlot,
                CallingConventions.HasThis, typeof(void), Type.EmptyTypes);

            ILGenerator methodIL = invokeTarget.GetILGenerator();
            LocalBuilder args = methodIL.DeclareLocal(typeof(object[]));

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, targetField);

            // get target proxiedMethod arguments, store them in local variable
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Callvirt, typeof(Invocation).GetMethod("get_Arguments"));
            methodIL.Emit(OpCodes.Stloc, args);

            // push every argument onto the stack and unbox in case it's valuetype 
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                methodIL.Emit(OpCodes.Ldloc, args);
                methodIL.Emit(OpCodes.Ldc_I4, i);
                methodIL.Emit(OpCodes.Ldelem_Ref);
                if (parameterTypes[i].IsValueType)
                {
                    methodIL.Emit(OpCodes.Unbox, parameterTypes[i]);
                    methodIL.Emit(OpCodes.Ldobj, parameterTypes[i]);
                }
            }

            methodIL.Emit(OpCodes.Callvirt, proxiedMethod);

            // store the proxiedMethod result into the ResultValue property
            if (proxiedMethod.ReturnType != typeof(void))
            {
                if (proxiedMethod.ReturnType.IsValueType)
                {
                    methodIL.Emit(OpCodes.Box, proxiedMethod.ReturnType);
                }

                LocalBuilder returnValue = methodIL.DeclareLocal(typeof(object));

                methodIL.Emit(OpCodes.Stloc, returnValue);
                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Ldloc, returnValue);
                methodIL.Emit(OpCodes.Callvirt, typeof(Invocation).GetMethod("set_ReturnValue"));
            }

            methodIL.Emit(OpCodes.Ret);

            return typeBuilder.CreateType();
        }
    }
}