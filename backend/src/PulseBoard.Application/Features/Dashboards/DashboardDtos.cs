using PulseBoard.Application.Features.Widgets;

namespace PulseBoard.Application.Features.Dashboards;

public record DashboardSummaryDto(
    Guid Id, string Name, string Slug, string Description,
    Guid DatasetId, string DatasetName, string Role, int WidgetCount, DateTimeOffset UpdatedAt);

public record DashboardMemberDto(Guid UserId, string Email, string DisplayName, string Role);

public record DashboardDetailDto(
    Guid Id, string Name, string Slug, string Description,
    Guid DatasetId, string DatasetName, string Role,
    IReadOnlyList<WidgetWithDataDto> Widgets,
    IReadOnlyList<DashboardMemberDto> Members);

public record SaveDashboardRequest(string Name, string Description, Guid DatasetId);
