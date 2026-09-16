using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.WorkSchedules;

public interface IWorkScheduleService
{
    Task<WorkScheduleDto?> GetAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<UpdateWorkScheduleResultDto> UpdateAsync(
        string identityUserId,
        UpdateWorkScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkCountdownDto?> GetCountdownAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);
}