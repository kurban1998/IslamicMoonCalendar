using IslamicCalendar.Api.Models;

namespace IslamicCalendar.Api.Services;

public interface IFridayReminderService
{
    FridayInfo GetFridayInfo(DateOnly date);
}

public class FridayReminderService : IFridayReminderService
{
    public FridayInfo GetFridayInfo(DateOnly date)
    {
        if (date.DayOfWeek != DayOfWeek.Friday)
            return new FridayInfo { IsFriday = false };

        return new FridayInfo
        {
            IsFriday = true,
            Reminders =
            [
                "Совершите полное омовение (гусль) перед пятничной молитвой",
                "Прочитайте суру «Аль-Кахф» (Пещера) — день и ночь пятницы",
                "Как можно больше произносите салават Пророку ﷺ",
                "Постарайтесь прийти в мечеть пораньше, к началу проповеди (хутбы)",
                "Ищите час принятия мольбы (дуа) — в последние часы перед закатом",
                "Наденьте чистую, лучшую одежду и используйте благовония"
            ]
        };
    }
}
