*Note: phases may be implemented out of order, and the numbering may not be sequential. It is ok to implement phases out-of-order even if prior phases have not been implemented.*

# [x] Phase 1

csproj and justfile boilerplate
stubbed entrypoint C# file
I should be able to `just run [subcommand]` to run the app
Justfile commands:
    build
    run
    publish
    test
    add-package
    format

# [x] Phase 2

Ability to read config file
JSON schema of the config file in a `config.schema.json`

`just run show-config` to log both the configured config path and the config file contents

# [x] Phase 3

Scan windows repos
Skip WSL2

Implement subcommand so that we can `just run scan-and-log` to test this.

# [x] Phase 4

Scan WSL2 repos

# [x] Phase 5

UI for configuration: to set the config file path, and to modify the config file.

# [x] Phase 6

Add testing project so I can `just test` to run tests
Stubbed testing project, does not need to have tests, just a single `hello world` test case to show me how testing code looks in a C# project

# Phase 7

Write some tests

# [x] Phase 8

Proper icon

# [x] Phase 9

create github actions that will generate a release .exe. The release should be tied to a git version tag that I will manually create for each version

# Phase 10

Button accessible from notification to launch the repo in Code, so I can manually update.
