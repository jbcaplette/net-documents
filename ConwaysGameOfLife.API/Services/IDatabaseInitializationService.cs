namespace ConwaysGameOfLife.API.Services;

public interface IDatabaseInitializationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
