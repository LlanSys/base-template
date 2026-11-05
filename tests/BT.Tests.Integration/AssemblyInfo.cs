using Xunit;

// Each collection can own a database container. Avoid parallel SQL Server instances
// exhausting workstation resources while the local application stacks are running.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
