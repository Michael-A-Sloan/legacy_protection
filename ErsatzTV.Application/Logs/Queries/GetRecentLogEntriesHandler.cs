using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Logs.Mapper;

namespace ErsatzTV.Application.Logs;

public class GetRecentLogEntriesHandler(
    ILocalFileSystem localFileSystem,
    IConfigElementRepository configElementRepository)
    : IRequestHandler<GetRecentLogEntries, PagedLogEntriesViewModel>
{
    public async Task<PagedLogEntriesViewModel> Handle(
        GetRecentLogEntries request,
        CancellationToken cancellationToken)
    {
        bool showAdminSecurityLogs = await configElementRepository
            .GetValue<bool>(ConfigElementKey.AdminSecurityLogsInSupportSectionEnabled, cancellationToken)
            .IfNoneAsync(true);

        // get most recent file
        string logFileName = localFileSystem.ListFiles(FileSystemLayout.LogsFolder)
            .OrderDescending()
            .FirstOrDefault();

        if (logFileName is not null)
        {
            IQueryable<LogEntryViewModel> entries = ReadFrom(logFileName)
                .Bind(line => ProjectToViewModel(line))
                .AsQueryable();

            if (!showAdminSecurityLogs)
            {
                entries = entries.Where(le => !AdminSecurityLogMessages.IsAdminSecurityLogMessage(le.Message));
            }

            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                entries = entries.Filter(le =>
                    le.Level.ToString().Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                    le.Message.Contains(request.Filter, StringComparison.OrdinalIgnoreCase));
            }

            int count = entries.Count();

            IOrderedQueryable<LogEntryViewModel> ordered = request.SortDescending.Match(
                descending => descending
                    ? entries.OrderByDescending(request.SortExpression).ThenByDescending(le => le.Timestamp)
                    : entries.OrderBy(request.SortExpression).ThenByDescending(le => le.Timestamp),
                () => entries.OrderByDescending(le => le.Timestamp));

            var page = ordered
                .Skip(request.PageNum * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedLogEntriesViewModel(count, page);
        }

        return new PagedLogEntriesViewModel(0, new List<LogEntryViewModel>());
    }

    private static IEnumerable<string> ReadFrom(string file)
    {
        using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
