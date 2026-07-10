using System.Runtime.CompilerServices;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.ScrubMember("Id");
    }
}
