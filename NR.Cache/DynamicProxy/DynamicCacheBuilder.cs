using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NR.Cache.Dynamic
{
    internal class DynamicCacheBuilder : ICachingProxyBuilder
    {
        public T BuildProxy<T>(ICachingProxyConfiguration<T> configuration) where T : class
        {
            return new Emitter().BuildProxy<T>(configuration.TargetObject);
        }
    }

    public class Emitter
    {
        public TProxy BuildProxy<TProxy>(TProxy targetObject)
        {
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("NRCacheGeneratedAssembly"),
                AssemblyBuilderAccess.Run);

            ModuleBuilder dynamicModule = assemblyBuilder.DefineDynamicModule("MainModule");

            Type proxyType = typeof(TProxy);
            Type targetImplementationType = targetObject.GetType();

            TypeBuilder dynamicType = dynamicModule.DefineType(proxyType.Name + "_CachingProxy",
                                                               TypeAttributes.Public | TypeAttributes.Class);
            dynamicType.AddInterfaceImplementation(proxyType);

            FieldBuilder interceptorField = dynamicType.DefineField("_interceptor", typeof(IInterceptor), FieldAttributes.Private);
            FieldBuilder targetObjectField = dynamicType.DefineField("_proxyTarget", targetImplementationType, FieldAttributes.Private);

            ConstructorInfo ctor = EmitProxyConstructor(dynamicType, interceptorField, targetObjectField);

            foreach (var interfaceMethod in CollectMethods(proxyType))
            {

                var map = targetImplementationType.GetInterfaceMap(proxyType);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                MethodInfo targetMethod = map.TargetMethods[index];

                MethodBuilder dynamicMethod = dynamicType.DefineMethod(
                    interfaceMethod.Name,
                    MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.HasThis,
                    interfaceMethod.ReturnType,
                    interfaceMethod.GetParameters().Select(x => x.ParameterType).ToArray());

                ILGenerator il = dynamicMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, targetObjectField);
                il.Emit(OpCodes.Call, targetMethod);
                il.Emit(OpCodes.Ret);
            }

            var generatedType = dynamicType.CreateType();
            var proxyInstance = Activator.CreateInstance(generatedType, new DummyInterceptor(), targetObject);
            return (TProxy)proxyInstance;
        }

        private IEnumerable<MethodInfo> CollectMethods(Type interfaceType)
        {
            return interfaceType.GetMethods();
        }

        private ConstructorInfo EmitProxyConstructor(TypeBuilder typeBuilder, FieldInfo interceptorField, FieldInfo targetField)
        {
            ConstructorInfo objectCtor = typeof(object).GetConstructor(new Type[0]);

            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new[] { interceptorField.FieldType, targetField.FieldType });

            ILGenerator il = ctorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, objectCtor);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, interceptorField);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, targetField);

            il.Emit(OpCodes.Ret);

            return ctorBuilder;
        }
    }
}