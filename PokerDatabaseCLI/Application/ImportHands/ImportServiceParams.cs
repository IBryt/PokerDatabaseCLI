using PokerDatabaseCLI.Application.Import.ImportHands;
using PokerDatabaseCLI.Core.Dependencies;

namespace PokerDatabaseCLI.Application.Import;

public sealed record ImportServiceParams(
    string Path,
    IProgress<ImportProgress> Progress,
    CancellationToken CancellationToken,
    ImportDependencies ImportDependencies
);