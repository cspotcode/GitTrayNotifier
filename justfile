set dotenv-load
set positional-arguments

@default:
  just --list --unsorted

project := "GitTrayNotifier/GitTrayNotifier.csproj"

# Build all projects
build:
    dotnet build

# Run the app, forwarding any extra arguments as subcommands
# Usage: just run [subcommand]
run *args:
    dotnet run --project {{project}} -- {{args}}

# Publish a self-contained release executable
publish:
    dotnet publish {{project}} -c Release

# Run tests
test:
    dotnet test

# Add a NuGet package to the main project
# Usage: just add-package <PackageName>
add-package name:
    dotnet add {{project}} package {{name}}

# Format source code
format:
    dotnet format

# Delete build artifacts
clean:
    rm -r GitTrayNotifier/bin
    rm -r GitTrayNotifier/obj
    rm -r GitTrayNotifier.Tests/bin
    rm -r GitTrayNotifier.Tests/obj

# Regenerate Icons/app.ico from Icons/Git_icon.svg
generate-icon:
    dotnet script Icons/GenerateIcon.csx

# Open a PowerShell window navigated to the GitTrayNotifier registry key
regedit:
    pwsh -NoExit -Command "Set-Location 'HKCU:\Software\GitTrayNotifier'"
