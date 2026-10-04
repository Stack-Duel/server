using StackDuel.Application.Settings;

namespace StackDuel.Application.Configuration;

public sealed class MessageBusOptions : IOption
{
    public static string SectionName => "MessageBus";

    public string Transport { get; init; } = "RabbitMQ";

    public RabbitMqOptions RabbitMQ { get; init; } = new();

    public AzureServiceBusOptions AzureServiceBus { get; init; } = new();
}

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public int ConsumerConcurrency { get; init; } = 2;
}

public sealed class AzureServiceBusOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public int MaxConcurrentCalls { get; init; } = 2;
}