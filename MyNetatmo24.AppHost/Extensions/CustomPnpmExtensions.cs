using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Hosting.JavaScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyNetatmo24.AppHost.Extensions;

internal static partial class CustomPnpmExtensions
{
    public static IResourceBuilder<JavaScriptAppResource> WithPlaywrightRepeatCommand(this IResourceBuilder<JavaScriptAppResource> resource, int repeatCount = 25)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var commandOptions = new CommandOptions
        {
            IconName = "ArrowRepeatAll",
            IsHighlighted = true,
        };

        resource.WithCommand(
            name: "repeat-playwright-tests",
            displayName: "Repeat Playwright Tests",
            executeCommand: async (context) =>
            {
#pragma warning disable ASPIREINTERACTION001
                var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
                var prompt = await interactionService.PromptInputAsync("Repetition", "How many times do you want to repeat the Playwright tests?", new InteractionInput
                {
                    Name = "RepetitionCount",
                    Label = "Repetition Count",
                    Description = "Enter the number of times to repeat the Playwright tests.",
                    InputType = InputType.Number,
                    Required = true,
                    Placeholder = $"{repeatCount}",
                });
#pragma warning restore ASPIREINTERACTION001
                if (prompt.Canceled)
                {
                    return CommandResults.Success();
                }
                return await OnRunCommand(resource, context, $"pnpm run test --repeat-each={prompt.Data.Value}");
            },
            commandOptions: commandOptions);

        return resource;
    }

    public static IResourceBuilder<JavaScriptAppResource> WithPnpmWithWorkspaceRoot(this IResourceBuilder<JavaScriptAppResource> resource, bool install = true, string[]? installArgs = null, string workspaceRootPathRelativeToProject = "..")
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

    private static void AddInstaller(IResourceBuilder<JavaScriptAppResource> resource, bool install)
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

    private static async Task<ExecuteCommandResult> OnRunCommand(IResourceBuilder<JavaScriptAppResource> builder, ExecuteCommandContext context, string command)
    {
        var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var logger = loggerService.GetLogger(context.ResourceName);

        var processStartInfo = new ProcessStartInfo()
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/bash",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            WorkingDirectory = builder.Resource.WorkingDirectory
        };

        var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start process");
        await process.StandardInput.WriteLineAsync($"{command} & exit");

// #pragma warning disable CA2024 // Do not use 'StreamReader.EndOfStream' in async methods
//         while (!process.StandardOutput.EndOfStream)
//         {
//             var line = await process.StandardOutput.ReadLineAsync() ?? string.Empty;
//             LogCommandOutput(logger, line);
//         }
// #pragma warning restore CA2024 // Do not use 'StreamReader.EndOfStream' in async methods
        while (await process.StandardOutput.ReadLineAsync() is { } line)
        {
            logger.LogCommandOutput(line);
        }

        return CommandResults.Success();
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Line}")]
    private static partial void LogCommandOutput(
        this ILogger logger,
        string line);

    private sealed class JavaScriptInstallCommandWithWorkingDirAnnotation(string[] args, string workingDirectory): IResourceAnnotation
    {
        public string[] Args { get; } = args;
        public string WorkingDirectory { get; } = workingDirectory;
    }
}
