using System.Diagnostics;
using System.Text;

namespace GitTrayNotifier;

record ScanResult(
    RepositoryConfig Repo,
    string? LocalCommit,
    string? RemoteCommit,
    bool HasUpdates,
    string? Error
);

static class Scanner
{
    public static async Task<List<ScanResult>> ScanAllAsync(AppConfig config)
    {
        var results = new List<ScanResult>();

        bool? wslRunning = null; // lazily checked once if any WSL2 repos exist

        foreach (var repo in config.Repositories)
        {
            if (repo.Type == RepositoryType.Wsl2)
            {
                wslRunning ??= await IsDefaultWslDistroRunningAsync();
                if (!wslRunning.Value)
                {
                    results.Add(new ScanResult(repo, null, null, false,
                        "Skipped: default WSL2 distro is not running."));
                    continue;
                }
                results.Add(await ScanWsl2RepoAsync(repo));
            }
            else
            {
                results.Add(await ScanWindowsRepoAsync(repo));
            }
        }

        return results;
    }

    // Returns true if the default WSL2 distro is in the Running state.
    static async Task<bool> IsDefaultWslDistroRunningAsync()
    {
        try
        {
            // wsl.exe --list --verbose outputs a UTF-16 table with STATE column.
            // The default distro is marked with '*'.
            var output = await RunProcessAsync("wsl.exe", null,
                encoding: Encoding.Unicode,
                "--list", "--verbose");

            if (output is null) return false;

            foreach (var line in output.Split('\n'))
            {
                // Line format: "* Ubuntu   Running   2"  or  "  Ubuntu   Stopped   2"
                if (line.TrimStart().StartsWith('*') && line.Contains("Running", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    static async Task<ScanResult> ScanWsl2RepoAsync(RepositoryConfig repo)
    {
        try
        {
            var localCommit = await RunWslGitAsync(repo.Path, "rev-parse", repo.Branch);
            var remoteCommit = await RunWslGitAsync(repo.Path, "ls-remote", "origin", repo.Branch);

            remoteCommit = remoteCommit?.Split('\t', 2)[0].Trim();
            localCommit = localCommit?.Trim();

            if (localCommit is null || remoteCommit is null)
                return new ScanResult(repo, localCommit, remoteCommit, false,
                    "Could not determine one or both commits.");

            bool hasUpdates = !string.Equals(localCommit, remoteCommit, StringComparison.OrdinalIgnoreCase);
            return new ScanResult(repo, localCommit, remoteCommit, hasUpdates, null);
        }
        catch (Exception ex)
        {
            return new ScanResult(repo, null, null, false, ex.Message);
        }
    }

    static Task<string?> RunWslGitAsync(string wslPath, params string[] gitArguments)
    {
        // wsl.exe --cd <path> -- git <args>
        var args = new[] { "--cd", wslPath, "--", "git" }.Concat(gitArguments).ToArray();
        return RunProcessAsync("wsl.exe", workingDir: null, encoding: Encoding.UTF8, args);
    }

    static async Task<ScanResult> ScanWindowsRepoAsync(RepositoryConfig repo)
    {
        try
        {
            var localCommit = await RunGitAsync(repo.Path, "rev-parse", repo.Branch);
            var remoteCommit = await RunGitAsync(repo.Path, "ls-remote", "origin", repo.Branch);

            remoteCommit = remoteCommit?.Split('\t', 2)[0].Trim();
            localCommit = localCommit?.Trim();

            if (localCommit is null || remoteCommit is null)
                return new ScanResult(repo, localCommit, remoteCommit, false,
                    "Could not determine one or both commits.");

            bool hasUpdates = !string.Equals(localCommit, remoteCommit, StringComparison.OrdinalIgnoreCase);
            return new ScanResult(repo, localCommit, remoteCommit, hasUpdates, null);
        }
        catch (Exception ex)
        {
            return new ScanResult(repo, null, null, false, ex.Message);
        }
    }

    static Task<string?> RunGitAsync(string workingDir, params string[] arguments)
        => RunProcessAsync("git", workingDir, Encoding.UTF8, arguments);

    static async Task<string?> RunProcessAsync(string executable, string? workingDir, Encoding encoding, params string[] arguments)
    {
        var psi = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding,
        };
        if (workingDir is not null)
            psi.WorkingDirectory = workingDir;
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {executable}.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(
                $"{executable} {string.Join(' ', arguments)} exited {process.ExitCode}: {stderr.Trim()}");
        }

        return string.IsNullOrWhiteSpace(stdout) ? null : stdout;
    }
}
