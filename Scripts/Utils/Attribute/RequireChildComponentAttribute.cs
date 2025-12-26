using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RequireChildComponentAttribute : Attribute
{
    public Type[] RequiredTypes { get; }

    public RequireChildComponentAttribute(params Type[] requiredTypes)
    {
        RequiredTypes = requiredTypes;
    }
}
