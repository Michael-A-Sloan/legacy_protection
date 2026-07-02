namespace ErsatzTV.Services;

internal static class BackgroundServiceExceptionHelper
{
    public static bool IsShutdownCancellation(Exception exception, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested && exception is OperationCanceledException;
}
