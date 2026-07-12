using IslamicCalendar.Api.Models;

namespace IslamicCalendar.Api.Services;

public interface IMoonPhaseService
{
    MoonPhaseInfo GetMoonPhase(DateOnly date);
}

/// <summary>
/// Рассчитывает фазу Луны по классической астрономической формуле (возраст Луны
/// от известного новолуния делённый по модулю на длину синодического месяца).
///
/// Публичные бесплатные API фаз Луны либо платные, либо нестабильны, а расчёт
/// по формуле детерминирован, не требует интернета и достаточно точен (±1 день)
/// для отображения иконки в карточке дня. При желании этот сервис легко заменить
/// на обёртку над внешним API — интерфейс IMoonPhaseService для этого и существует.
/// </summary>
public class MoonPhaseService : IMoonPhaseService
{
    // Известное новолуние: 6 января 2000, 18:14 UTC
    private static readonly DateTime ReferenceNewMoon = new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
    private const double SynodicMonthDays = 29.530588861;

    public MoonPhaseInfo GetMoonPhase(DateOnly date)
    {
        var target = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), DateTimeKind.Utc);
        var daysSinceReference = (target - ReferenceNewMoon).TotalDays;

        var age = daysSinceReference % SynodicMonthDays;
        if (age < 0) age += SynodicMonthDays;

        var phaseFraction = age / SynodicMonthDays; // 0..1
        var illumination = (1 - Math.Cos(2 * Math.PI * phaseFraction)) / 2 * 100;

        var (code, nameRu) = ClassifyPhase(phaseFraction);

        return new MoonPhaseInfo
        {
            AgeDays = Math.Round(age, 1),
            SynodicMonthDays = SynodicMonthDays,
            IlluminationPercent = Math.Round(illumination, 1),
            PhaseCode = code,
            PhaseNameRu = nameRu
        };
    }

    private static (string Code, string NameRu) ClassifyPhase(double p) => p switch
    {
        < 0.02 or >= 0.98 => ("new", "Новолуние"),
        < 0.24 => ("waxing-crescent", "Растущий серп"),
        < 0.26 => ("first-quarter", "Первая четверть"),
        < 0.49 => ("waxing-gibbous", "Растущая Луна"),
        < 0.51 => ("full", "Полнолуние"),
        < 0.74 => ("waning-gibbous", "Убывающая Луна"),
        < 0.76 => ("last-quarter", "Последняя четверть"),
        _ => ("waning-crescent", "Убывающий серп")
    };
}
