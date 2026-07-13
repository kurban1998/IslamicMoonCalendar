// ============================================================
// Настройки
// ============================================================

// Если backend развёрнут на другом домене, укажите его тут, например:
// const API_BASE = "https://your-api-domain.example/api";
const API_BASE = "/api";

const WEEKDAYS_SHORT = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"];

// ============================================================
// Telegram WebApp
// ============================================================

const tg = window.Telegram?.WebApp;
tg?.ready();
tg?.expand();
tg?.setHeaderColor?.("#0E3E7D");
tg?.setBackgroundColor?.("#EAF3FF");

let userLocation = null; // { lat, lon }

function initLocation() {
  return new Promise((resolve) => {
    let settled = false;
    const finish = () => {
      if (settled) return;
      settled = true;
      resolve();
    };

    // Страховочный таймаут: если ни LocationManager, ни navigator.geolocation
    // не ответят (например, приложение открыто просто ссылкой в браузере,
    // а не внутри настоящего Telegram — тогда колбэк LocationManager.init
    // может вообще никогда не вызваться), приложение всё равно должно
    // загрузиться — просто без геолокации, backend возьмёт город по умолчанию
    const safetyTimeout = setTimeout(finish, 4000);

    // Telegram Bot API 8.0+: встроенный LocationManager мини-приложений
    if (tg?.LocationManager) {
      try {
        tg.LocationManager.init(() => {
          try {
            tg.LocationManager.getLocation((data) => {
              if (data?.latitude) {
                userLocation = { lat: data.latitude, lon: data.longitude };
              }
              clearTimeout(safetyTimeout);
              finish();
            });
          } catch {
            clearTimeout(safetyTimeout);
            finish();
          }
        });
        return;
      } catch {
        // падаем в fallback ниже
      }
    }

    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          userLocation = { lat: pos.coords.latitude, lon: pos.coords.longitude };
          clearTimeout(safetyTimeout);
          finish();
        },
        () => {
          clearTimeout(safetyTimeout);
          finish();
        },
        { timeout: 4000 }
      );
      return;
    }

    clearTimeout(safetyTimeout);
    finish(); // геолокация недоступна — backend подставит город по умолчанию
  });
}

function formatDateLocal(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

// ============================================================
// DOM-элементы
// ============================================================

const el = {
  homeScreen: document.getElementById("homeScreen"),
  calendarScreen: document.getElementById("calendarScreen"),
  openCalendarBtn: document.getElementById("openCalendarBtn"),
  backToHomeBtn: document.getElementById("backToHome"),

  // Карточка дня
  todayCard: document.getElementById("todayCard"),
  spinner: document.getElementById("cardSpinner"),
  moonIcon: document.getElementById("moonIcon"),
  moonCaption: document.getElementById("moonCaption"),
  cardWeekday: document.getElementById("cardWeekday"),
  cardHijriDay: document.getElementById("cardHijriDay"),
  cardHijriMonth: document.getElementById("cardHijriMonth"),
  sacredBadge: document.getElementById("sacredBadge"),
  cardGregDate: document.getElementById("cardGregDate"),
  cardLocation: document.getElementById("cardLocation"),
  prayerList: document.getElementById("prayerTimesList"),
  fridaySection: document.getElementById("fridaySection"),
  fridayList: document.getElementById("fridayList"),
  quoteTypeLabel: document.getElementById("quoteTypeLabel"),
  quoteText: document.getElementById("quoteText"),
  quoteSource: document.getElementById("quoteSource"),

  // Календарь
  weekdayRow: document.getElementById("weekdayRow"),
  grid: document.getElementById("calendarGrid"),
  loading: document.getElementById("loadingIndicator"),
  hijriTitle: document.getElementById("hijriMonthTitle"),
  gregTitle: document.getElementById("gregMonthTitle"),
  prevBtn: document.getElementById("prevMonth"),
  nextBtn: document.getElementById("nextMonth"),
};

// ============================================================
// Инициализация
// ============================================================

WEEKDAYS_SHORT.forEach((w) => {
  const span = document.createElement("span");
  span.textContent = w;
  el.weekdayRow.appendChild(span);
});

el.prevBtn.addEventListener("click", () => changeMonth(-1));
el.nextBtn.addEventListener("click", () => changeMonth(1));
el.openCalendarBtn.addEventListener("click", showCalendarScreen);
el.backToHomeBtn.addEventListener("click", showHomeScreen);

(async function start() {
  try {
    await initLocation();
    await loadTodayCard();
  } catch (err) {
    console.error("Не удалось запустить приложение:", err);
    el.spinner.classList.add("hidden");
    el.quoteText.textContent = "Произошла ошибка при запуске приложения. Проверьте консоль браузера.";
  }
})();

// ============================================================
// Переключение экранов
// ============================================================

let todayHijri = null; // { year, month } по Хиджре — заполняется после загрузки карточки дня

function showCalendarScreen() {
  el.homeScreen.classList.add("hidden");
  el.calendarScreen.classList.remove("hidden");

  // Календарь открывается на ТЕКУЩЕМ ЛУННОМ месяце (не григорианском!)
  if (todayHijri) {
    viewHijriYear = todayHijri.year;
    viewHijriMonth = todayHijri.month;
  }
  loadMonth();
}

function showHomeScreen() {
  el.calendarScreen.classList.add("hidden");
  el.homeScreen.classList.remove("hidden");
}

// ============================================================
// Карточка сегодняшнего дня
// ============================================================

async function loadTodayCard() {
  el.spinner.classList.remove("hidden");

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 15000);

  try {
    const dateStr = formatDateLocal(new Date());
    const params = new URLSearchParams({ date: dateStr });
    if (userLocation) {
      params.set("lat", userLocation.lat);
      params.set("lon", userLocation.lon);
    }

    const res = await fetch(`${API_BASE}/day?${params.toString()}`, { signal: controller.signal });
    if (!res.ok) throw new Error(`Backend ответил статусом ${res.status}`);
    const card = await res.json();
    renderTodayCard(card);
  } catch (err) {
    console.error(err);
    const reason = err?.name === "AbortError" ? "истекло время ожидания ответа" : (err?.message || "неизвестная ошибка");
    el.quoteText.textContent = `Не удалось загрузить данные дня (${reason}). Проверьте, что backend запущен и доступен по адресу ${API_BASE}.`;
  } finally {
    clearTimeout(timeoutId);
    el.spinner.classList.add("hidden");
  }
}

function renderTodayCard(card) {
  const gregDate = new Date(card.gregorianDate);

  el.cardWeekday.textContent = card.hijri.weekdayRu;
  el.cardHijriDay.textContent = card.hijri.day;
  el.cardHijriMonth.textContent = `${card.hijri.monthNameRu} ${card.hijri.year} г.х.`;
  el.cardGregDate.textContent = gregDate.toLocaleDateString("ru-RU", {
    day: "numeric", month: "long", year: "numeric"
  });

  // Запоминаем текущий лунный месяц/год — именно на нём будет открываться календарь
  todayHijri = { year: card.hijri.year, month: card.hijri.month };

  // Запретные (священные) месяцы — розовая палитра карточки вместо голубой
  el.todayCard.classList.toggle("sacred-month", !!card.hijri.isSacredMonth);
  el.sacredBadge.classList.toggle("hidden", !card.hijri.isSacredMonth);

  // Фаза Луны
  const phaseFraction = card.moon.ageDays / card.moon.synodicMonthDays;
  el.moonIcon.innerHTML = buildMoonPhaseSvg(phaseFraction);
  el.moonCaption.textContent = `${card.moon.phaseNameRu} · освещённость ${Math.round(card.moon.illuminationPercent)}%`;

  // Город/место, по которому рассчитано время намазов
  el.cardLocation.textContent = card.locationName || "—";

  // Время намазов
  const prayers = [
    ["Фаджр", card.prayers.fajr],
    ["Восход", card.prayers.sunrise],
    ["Зухр", card.prayers.dhuhr],
    ["Аср", card.prayers.asr],
    ["Магриб", card.prayers.maghrib],
    ["Иша", card.prayers.isha],
  ];
  el.prayerList.innerHTML = prayers.map(([name, time]) => `
    <div class="prayer-list__item">
      <div class="prayer-list__name">${name}</div>
      <div class="prayer-list__time">${time || "—"}</div>
    </div>
  `).join("");

  // Пятничный блок
  if (card.friday?.isFriday) {
    el.fridaySection.classList.remove("hidden");
    el.fridayList.innerHTML = card.friday.reminders.map((r) => `<li>${r}</li>`).join("");
  } else {
    el.fridaySection.classList.add("hidden");
  }

  // Хадис / аят
  el.quoteTypeLabel.textContent = card.quote.type === "ayah" ? "Аят из Корана" : "Хадис";
  el.quoteText.textContent = card.quote.textRu;
  el.quoteSource.textContent = card.quote.source;
}

// ============================================================
// Календарь — только просмотр, листание месяцев, без карточек по клику
// ============================================================

// viewHijriYear/viewHijriMonth — год/месяц ПО ХИДЖРЕ, который сейчас просматривается.
// Стартовые значения — заглушка на случай открытия календаря до того, как
// подгрузится сегодняшняя карточка (в норме showCalendarScreen их сразу
// перезапишет реальными данными из todayHijri)
let viewHijriYear = 1447;
let viewHijriMonth = 1;

function changeMonth(delta) {
  const shifted = shiftHijriMonth(viewHijriYear, viewHijriMonth, delta);
  viewHijriYear = shifted.year;
  viewHijriMonth = shifted.month;
  loadMonth();
}

function shiftHijriMonth(year, month, delta) {
  let m = month + delta;
  let y = year;
  if (m > 12) { m = 1; y++; }
  if (m < 1) { m = 12; y--; }
  return { year: y, month: m };
}

async function fetchHijriMonth(hijriYear, hijriMonth) {
  const res = await fetch(`${API_BASE}/calendar/hijri-month/${hijriYear}/${hijriMonth}`);
  if (!res.ok) throw new Error("Не удалось загрузить лунный месяц");
  return res.json();
}

async function loadMonth() {
  setLoading(true);
  try {
    const prev = shiftHijriMonth(viewHijriYear, viewHijriMonth, -1);
    const next = shiftHijriMonth(viewHijriYear, viewHijriMonth, 1);

    // Подгружаем соседние ЛУННЫЕ месяцы, чтобы показать реальные дни (не
    // пустые ячейки) на границах сетки — приглушённые, чтобы визуально
    // отличались от просматриваемого лунного месяца
    const [prevDays, currentDays, nextDays] = await Promise.all([
      fetchHijriMonth(prev.year, prev.month),
      fetchHijriMonth(viewHijriYear, viewHijriMonth),
      fetchHijriMonth(next.year, next.month),
    ]);

    renderMonth(currentDays, prevDays, nextDays);
  } catch (err) {
    console.error(err);
    el.grid.innerHTML = `<div class="loading-indicator">Не удалось загрузить календарь. Проверьте подключение к API.</div>`;
  } finally {
    setLoading(false);
  }
}

function renderMonth(currentDays, prevDays, nextDays) {
  el.grid.innerHTML = "";
  if (currentDays.length === 0) return;

  // Все дни currentDays принадлежат ровно одному лунному месяцу — можно
  // безопасно брать название/год с первого дня, midpoint-хак не нужен
  el.hijriTitle.textContent = `${currentDays[0].hijri.monthNameRu} ${currentDays[0].hijri.year}`;

  // Лунный месяц почти всегда захватывает два григорианских — показываем
  // диапазон дат вместо одного "месяц год"
  const firstG = new Date(currentDays[0].gregorianDate);
  const lastG = new Date(currentDays[currentDays.length - 1].gregorianDate);
  const firstLabel = firstG.toLocaleDateString("ru-RU", { day: "numeric", month: "long" });
  const lastLabel = lastG.toLocaleDateString("ru-RU", { day: "numeric", month: "long", year: "numeric" });
  el.gregTitle.textContent = `${firstLabel} — ${lastLabel}`;

  // Неделя начинается с понедельника
  let firstWeekday = new Date(currentDays[0].gregorianDate).getDay(); // 0 = вс
  firstWeekday = firstWeekday === 0 ? 6 : firstWeekday - 1; // 0 = пн

  // leadingDays/trailingDays — хвосты СОСЕДНИХ ЛУННЫХ месяцев, а не
  // григорианских: именно это и делает подсветку "по лунному календарю"
  const leadingDays = firstWeekday > 0 ? prevDays.slice(prevDays.length - firstWeekday) : [];

  const totalSoFar = leadingDays.length + currentDays.length;
  const trailingCount = (7 - (totalSoFar % 7)) % 7;
  const trailingDays = nextDays.slice(0, trailingCount);

  const cells = [
    ...leadingDays.map((day) => ({ day, otherMonth: true })),
    ...currentDays.map((day) => ({ day, otherMonth: false })),
    ...trailingDays.map((day) => ({ day, otherMonth: true })),
  ];

  cells.forEach(({ day, otherMonth }) => {
    const cell = document.createElement("div");
    cell.className = "day-cell";
    if (day.isFriday) cell.classList.add("is-friday");
    if (day.hijri.isSacredMonth) cell.classList.add("is-sacred");
    if (day.isToday) cell.classList.add("is-today");
    if (otherMonth) cell.classList.add("other-month"); // день из соседнего ЛУННОГО месяца

    const gregSpan = document.createElement("span");
    gregSpan.className = "day-cell__greg";
    gregSpan.textContent = new Date(day.gregorianDate).getDate();

    const hijriSpan = document.createElement("span");
    hijriSpan.className = "day-cell__hijri";
    hijriSpan.textContent = day.hijri.day;

    cell.appendChild(gregSpan);
    cell.appendChild(hijriSpan);
    el.grid.appendChild(cell);
  });
}

function setLoading(isLoading) {
  el.loading.classList.toggle("hidden", !isLoading);
}

// ============================================================
// SVG-иконка фазы Луны
// (классический приём: базовый диск + путь-«терминатор» из двух дуг)
// ============================================================

function buildMoonPhaseSvg(phaseFraction, size = 56) {
  const r = size / 2;
  const theta = phaseFraction * 2 * Math.PI;
  const rx = Math.abs(r * Math.cos(theta));
  const waxing = phaseFraction < 0.5;

  const sweep1 = waxing ? 1 : 0;
  const sweep2 = (phaseFraction < 0.25 || phaseFraction > 0.75) ? sweep1 : 1 - sweep1;

  const path = `M ${r},1 A ${r - 1},${r - 1} 0 0,${sweep1} ${r},${size - 1} A ${rx},${r - 1} 0 0,${sweep2} ${r},1 Z`;

  return `
    <svg viewBox="0 0 ${size} ${size}" width="${size}" height="${size}" xmlns="http://www.w3.org/2000/svg">
      <circle cx="${r}" cy="${r}" r="${r - 1}" fill="#DCE9FA" />
      <path d="${path}" fill="#0E3E7D" />
      <circle cx="${r}" cy="${r}" r="${r - 1}" fill="none" stroke="#0E3E7D" stroke-width="1" opacity="0.5"/>
    </svg>
  `;
}
