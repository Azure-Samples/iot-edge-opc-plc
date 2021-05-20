// As client-side tests are passive and mostly sleep, we can use run many tests in parallel
// regardless of the number of cores.
[assembly: NUnit.Framework.LevelOfParallelism(16)]