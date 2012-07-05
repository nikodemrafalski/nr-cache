using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NR.Cache.DynamicProxy
{
    internal class DynamicModuleStore
    {
        private static AssemblyBuilder _assembly;
        private static ModuleBuilder _module;

        public static ModuleBuilder Module
        {
            get
            {
                if (_module == null)
                {
                    Init();
                }

                return _module;
            }
        }

        public static AssemblyBuilder Assembly
        {
            get
            {
                if (_assembly == null)
                {
                    Init();
                }

                return _assembly;
            }
        }

        private static void Init()
        {
            _assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NR.DynamicAssembly"),
                                                                                AssemblyBuilderAccess.Run);
            _module = _assembly.DefineDynamicModule("MainModule");
        }
    }
}