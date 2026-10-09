using Xunit;

// тесты меняют общие статические настройки (DisabledPacks), поэтому не запускаются параллельно
[assembly: CollectionBehavior(DisableTestParallelization = true)]
