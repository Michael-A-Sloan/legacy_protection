using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Application.Security;

public class RefreshPublicBlocklistsHandler(IPublicBlocklistService publicBlocklistService)
    : IRequestHandler<RefreshPublicBlocklists, Unit>
{
    public async Task<Unit> Handle(RefreshPublicBlocklists request, CancellationToken cancellationToken)
    {
        foreach (string listId in publicBlocklistService.GetDefinitions().Select(d => d.Id))
        {
            await publicBlocklistService.RefreshListAsync(listId, cancellationToken);
        }

        return Unit.Default;
    }
}
