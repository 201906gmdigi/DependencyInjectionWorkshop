using System;

namespace MyConsole
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheResultAttribute : Attribute
    {
        public int Duration { get; set; }
    }
}