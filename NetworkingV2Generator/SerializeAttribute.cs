using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class SerializeDataAttribute : Attribute {
    public SerializeDataAttribute(){
    }
}