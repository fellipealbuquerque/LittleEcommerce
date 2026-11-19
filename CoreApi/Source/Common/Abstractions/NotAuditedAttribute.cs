namespace LittleEcommerce.Source.Common.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class NotAuditedAttribute : Attribute;
