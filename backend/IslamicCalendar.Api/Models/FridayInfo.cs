namespace IslamicCalendar.Api.Models;

public class FridayInfo
{
    public bool IsFriday { get; set; }
    public List<string> Reminders { get; set; } = [];
}
