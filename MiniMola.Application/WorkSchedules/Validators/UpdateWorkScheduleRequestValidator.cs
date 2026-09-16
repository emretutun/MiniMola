using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace MiniMola.Application.WorkSchedules.Validators;

public sealed class UpdateWorkScheduleRequestValidator
    : AbstractValidator<UpdateWorkScheduleRequest>
{
    public UpdateWorkScheduleRequestValidator()
    {
        RuleFor(x => x.Days)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Çalışma günleri gönderilmelidir.")
            .Must(days => days.Count == 7)
            .WithMessage(
                "Haftanın yedi günü için bilgi gönderilmelidir.")
            .Must(days =>
                days.Select(x => x.DayOfWeek)
                    .Distinct()
                    .Count() == 7)
            .WithMessage(
                "Her çalışma günü yalnızca bir kez gönderilmelidir.")
            .Must(days =>
                days.Any(x => x.IsWorkingDay))
            .WithMessage(
                "En az bir çalışma günü seçmelisin.");

        RuleForEach(x => x.Days)
            .SetValidator(
                new UpdateWorkScheduleDayRequestValidator());
    }
}

public sealed class UpdateWorkScheduleDayRequestValidator
    : AbstractValidator<UpdateWorkScheduleDayRequest>
{
    public UpdateWorkScheduleDayRequestValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum()
            .WithMessage("Geçersiz çalışma günü.");

        RuleFor(x => x)
            .Must(day =>
                !day.IsWorkingDay
                || day.StartTime != day.EndTime)
            .WithMessage(
                "Çalışılan günlerde başlangıç ve bitiş "
                + "saatleri aynı olamaz.");
    }
}