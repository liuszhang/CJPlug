using System.Collections.Generic;

namespace DllLoaderPlug.Services
{
    public interface IDllMethodCacheService
    {
        List<MethodDescriptor> GetMethods();
        void SetMethods(List<MethodDescriptor> methods);
        void Clear();
    }

    public class DllMethodCacheService : IDllMethodCacheService
    {
        private List<MethodDescriptor> _methods = new List<MethodDescriptor>();

        public List<MethodDescriptor> GetMethods() => _methods ??= new List<MethodDescriptor>();

        public void SetMethods(List<MethodDescriptor> methods)
        {
            _methods = methods ?? new List<MethodDescriptor>();
        }

        public void Clear() => _methods = new List<MethodDescriptor>();
    }
}
