namespace StackDuel.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class RequireUserAttribute : Attribute { }