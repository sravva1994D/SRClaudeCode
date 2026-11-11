using Xunit;

// The tests create and drop LocalDB databases and clear the shared connection pool, so they run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
