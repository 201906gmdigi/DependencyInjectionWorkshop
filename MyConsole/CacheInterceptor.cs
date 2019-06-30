using System;
using System.Linq;
using Castle.DynamicProxy;

namespace MyConsole
{
    public class CacheInterceptor : IInterceptor
    {
        private readonly ICacheProvider _cache;

        public CacheInterceptor(ICacheProvider cache)
        {
            _cache = cache;
        }

        public CacheResultAttribute GetCacheResultAttribute(IInvocation invocation)
        {
            return Attribute.GetCustomAttribute(
                    invocation.MethodInvocationTarget,
                    typeof(CacheResultAttribute)
                )
                as CacheResultAttribute;
        }

        public string GetInvocationSignature(IInvocation invocation)
        {
            return
                $"{invocation.TargetType.FullName}-{invocation.Method.Name}-{String.Join("-", invocation.Arguments.Select(a => (a ?? "").ToString()).ToArray())}";
        }

        public void Intercept(IInvocation invocation)
        {
            var cacheAttr = GetCacheResultAttribute(invocation);

            if (cacheAttr == null)
            {
                invocation.Proceed();
                return;
            }

            string key = GetInvocationSignature(invocation);

            if (_cache.Contains(key))
            {
                invocation.ReturnValue = _cache.Get(key);
                return;
            }

            invocation.Proceed();
            var result = invocation.ReturnValue;

            if (result != null)
            {
                _cache.Put(key, result, cacheAttr.Duration);
            }
        }
    }
}