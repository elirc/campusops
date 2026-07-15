namespace CampusOps.Contracts.Integrations;

public sealed record CreateWebhookSubscription(string EventType, string Endpoint, string SecretName);
public sealed record PublishWebhookEvent(string EventId, string EventType, string Payload);
