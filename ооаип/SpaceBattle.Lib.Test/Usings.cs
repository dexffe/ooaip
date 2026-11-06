global using Xunit;
global using Moq;

// IoC-контейнер Hwdtech хранит состояние статически (скоупы, стратегии),
// а тесты пересоздают его через InitScopeBasedIoCImplementationCommand.
// Параллельный запуск тестовых классов приводит к гонкам, поэтому он отключён.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
