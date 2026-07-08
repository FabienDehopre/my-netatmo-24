using System.Runtime.CompilerServices;

namespace MyNetatmo24.ApiService.IntegrationTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.ScrubMember("Id");
    }
}
