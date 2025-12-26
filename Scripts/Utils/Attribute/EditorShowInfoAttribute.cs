using System;
using UnityEditor;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class EditorShowInfoAttribute : Attribute
{
    public readonly string Message;
    public readonly MessageType MessageType;

    public EditorShowInfoAttribute(
        string message,
        MessageType messageType = MessageType.Info)
    {
        Message = message;
        MessageType = messageType;
    }
}
