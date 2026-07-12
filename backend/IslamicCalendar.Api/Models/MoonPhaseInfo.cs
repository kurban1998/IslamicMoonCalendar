namespace IslamicCalendar.Api.Models;

public class MoonPhaseInfo
{
    /// <summary>Возраст Луны в сутках от последнего новолуния (0..~29.53).</summary>
    public double AgeDays { get; set; }

    /// <summary>Продолжительность синодического месяца, использованная для расчёта (дней).</summary>
    public double SynodicMonthDays { get; set; } = 29.530588861;

    public double IlluminationPercent { get; set; }

    /// <summary>Код фазы: new, waxing-crescent, first-quarter, waxing-gibbous, full, waning-gibbous, last-quarter, waning-crescent.</summary>
    public string PhaseCode { get; set; } = string.Empty;

    public string PhaseNameRu { get; set; } = string.Empty;
}
