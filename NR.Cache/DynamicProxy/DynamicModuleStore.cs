using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NR.Cache.DynamicProxy
{
    internal class DynamicModuleStore
    {
        private AssemblyBuilder _assembly;
        private ModuleBuilder _module;

        public ModuleBuilder Module
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

        public AssemblyBuilder Assembly
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

        private void Init()
        {
            _assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NR.DynamicAssembly"),
                                                                                AssemblyBuilderAccess.Run);
            _module = _assembly.DefineDynamicModule("NR.DynamicAssembly.dll");
        }
    }
}