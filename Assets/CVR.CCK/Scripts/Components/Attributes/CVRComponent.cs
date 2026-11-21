using System;

namespace ABI.CCK.Components
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CVRComponentAttribute : Attribute
    {
        public ComponentStatus Status { get; }

        public CVRComponentAttribute(ComponentStatus status = ComponentStatus.None)
        {
            Status = status;
        }
    }
    
    public enum ComponentStatus
    {
        None,
        InDevelopment,
        Deprecated
    }
}