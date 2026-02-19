using Aspire.Hosting.JavaScript;

namespace MyNetatmo24.AppHost;

internal static class CustomPnpmExtensions
{
    public static IResourceBuilder<TResource> WithPnpmWithWorkspaceRoot<TResource>(this IResourceBuilder<TResource> resource, bool install = true, string[]? installArgs = null, string workspaceRootPathRelativeToProject = "..") where TResource : JavaScriptAppResource
    {
        ArgumentNullException.ThrowIfNull(resource);

        var workingDirectory = resource.Resource.WorkingDirectory;
        var workspaceRootDirectory = Path.GetFullPath(Path.Combine(workingDirectory, workspaceRootPathRelativeToProject));
        var hasPnpmLock = File.Exists(Path.Combine(workspaceRootDirectory, "pnpm-lock.yaml"));

        installArgs ??= GetDefaultPnpmInstallArgs(resource, hasPnpmLock);

        var packageFilesSourcePattern = "package.json";
        if (hasPnpmLock)
        {
            packageFilesSourcePattern += " pnpm-lock.yaml";
        }

        resource
            .WithAnnotation(new JavaScriptPackageManagerAnnotation("pnpm", runScriptCommand: "run", cacheMount: "/pnpm/store")
            {
                PackageFilesPatterns = { new CopyFilePattern(packageFilesSourcePattern, "./") },
                // pnpm does not strip the -- separator and passes it to the script, causing Vite to ignore subsequent arguments.
                CommandSeparator = null,
                // pnpm is not included in the Node.js Docker image by default, so we need to enable it via corepack
#pragma warning disable ASPIREDOCKERFILEBUILDER001
                InitializeDockerBuildStage = stage => stage.Run("corepack enable pnpm")
#pragma warning restore ASPIREDOCKERFILEBUILDER001
            })
            .WithAnnotation(new JavaScriptInstallCommandWithWorkingDirAnnotation(["install", .. installArgs], workspaceRootDirectory));

        AddInstaller(resource, install);
        return resource;
    }

    private static string[] GetDefaultPnpmInstallArgs(IResourceBuilder<JavaScriptAppResource> resource, bool hasPnpmLock) =>
        resource.ApplicationBuilder.ExecutionContext.IsPublishMode && hasPnpmLock
            ? ["--frozen-lockfile"]
            : [];

    private static void AddInstaller<TResource>(IResourceBuilder<TResource> resource, bool install) where TResource : JavaScriptAppResource
    {
        // Only install packages if in run mode
        if (resource.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Check if the installer resource already exists
            var installerName = $"{resource.Resource.Name}-installer";
            resource.ApplicationBuilder.TryCreateResourceBuilder<JavaScriptInstallerResource>(installerName, out var existingResource);

            if (existingResource is not null)
            {
                // Installer already exists, update its configuration based on install parameter
                if (!install)
                {
                    // Remove wait annotation if install is false
                    resource.Resource.Annotations.OfType<WaitAnnotation>()
                        .Where(w => w.Resource == existingResource.Resource)
                        .ToList()
                        .ForEach(w => resource.Resource.Annotations.Remove(w));

                    // Add WithExplicitStart to the existing installer
                    existingResource.WithExplicitStart();
                }
                return;
            }

            var installer = new JavaScriptInstallerResource(installerName, resource.Resource.WorkingDirectory);
            var installerBuilder = resource.ApplicationBuilder.AddResource(installer)
                .WithParentRelationship(resource.Resource)
                .ExcludeFromManifest()
                .WithCertificateTrustScope(CertificateTrustScope.None);

            resource.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
            {
                // set the installer's working directory to match the resource's working directory
                // and set the install command and args based on the resource's annotations
                if (!resource.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager) ||
                    !resource.Resource.TryGetLastAnnotation<JavaScriptInstallCommandWithWorkingDirAnnotation>(out var installCommand))
                {
                    throw new InvalidOperationException("JavaScriptPackageManagerAnnotation and JavaScriptInstallCommandWithWorkingDirAnnotation are required when installing packages.");
                }

                installerBuilder
                    .WithCommand(packageManager.ExecutableName)
                    .WithWorkingDirectory(installCommand.WorkingDirectory)
                    .WithArgs(installCommand.Args);

                return Task.CompletedTask;
            });

            if (install)
            {
                // Make the parent resource wait for the installer to complete
                resource.WaitForCompletion(installerBuilder);
            }
            else
            {
                // Add WithExplicitStart when install is false
                // Note: No need to remove wait annotations here since WaitForCompletion was never called
                installerBuilder.WithExplicitStart();
            }

            resource.WithAnnotation(new JavaScriptPackageInstallerAnnotation(installer));
        }
    }

    private sealed class JavaScriptInstallCommandWithWorkingDirAnnotation(string[] args, string workingDirectory): IResourceAnnotation
    {
        public string[] Args { get; } = args;
        public string WorkingDirectory { get; } = workingDirectory;
    }
}
