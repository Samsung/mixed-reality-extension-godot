using System;
using System.Linq;
using Godot;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ExportEnumAttribute : ExportAttribute
{
    public ExportEnumAttribute(Type enumType) : base(PropertyHint.Flags, enumType.IsEnum ?
        string.Join(",", Enum.GetValues(enumType).OfType<Enum>().Where(e => (int)(object)e != 0)) : "Invalid Type")
    {
    }
}