using JetBrains.Annotations;

namespace MyNetatmo24.Gateway.TestModule;

[UsedImplicitly]
internal sealed record TestLoginRequest(string Username, string Password);
