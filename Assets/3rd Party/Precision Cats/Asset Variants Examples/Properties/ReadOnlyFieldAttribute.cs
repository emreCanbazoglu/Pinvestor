using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class ReadOnlyFieldAttribute : PropertyAttribute { }