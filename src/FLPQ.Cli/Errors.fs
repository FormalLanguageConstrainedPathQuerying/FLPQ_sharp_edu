namespace FLPQ.Cli

/// User-facing CLI error: malformed input file, missing file, invalid algorithm/semantics
/// combination. Caught by `Program.runCli`, printed to stderr without usage, exit code 1.
exception CliErrorException of string
