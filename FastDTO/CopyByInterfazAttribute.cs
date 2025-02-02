using System;

namespace FastDTO
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class CopyByInterfazAttribute : Attribute
    {
        public CopyByInterfazAttribute()
        {
        }
    }
}
