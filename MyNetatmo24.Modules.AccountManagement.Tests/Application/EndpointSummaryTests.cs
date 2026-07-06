using MyNetatmo24.Modules.AccountManagement.Application;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

/// <summary>
/// The endpoint summaries carry the OpenAPI documentation. These tests verify the summary
/// metadata is constructed without error and exposes a non-empty description.
/// </summary>
public class EndpointSummaryTests
{
    [Test]
    public async Task EnsureAccountSummary_IsPopulated()
    {
        var summary = new EnsureAccount.EndpointSummary();

        await Assert.That(summary.Summary).IsNotNull().And.IsNotEmpty();
        await Assert.That(summary.Description).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task MyAccountSummary_IsPopulated()
    {
        var summary = new MyAccount.EndpointSummary();

        await Assert.That(summary.Summary).IsNotNull().And.IsNotEmpty();
        await Assert.That(summary.Description).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task RestoreAccountSummary_IsPopulated()
    {
        var summary = new RestoreAccount.EndpointSummary();

        await Assert.That(summary.Summary).IsNotNull().And.IsNotEmpty();
        await Assert.That(summary.Description).IsNotNull().And.IsNotEmpty();
    }
}
