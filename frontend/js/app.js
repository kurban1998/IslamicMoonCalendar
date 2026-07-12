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
    // Telegram Bot API 8.0+: встроенный LocationManager мини-приложений
    if (tg?.LocationManager) {
      try {
        tg.LocationManager.init(() => {
          tg.LocationManager.getLocation((data) => {
            if (data?.latitude) {
              userLocation = { lat: data.latitude, lon: data.longitude };
            }
            resolve();
          });
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
          resolve();
        },
        () => resolve(),
        { timeout: 5000 }
      );
      return;
    }

    resolve(); // геолокация недоступна — backend подставит город по умолчанию
  });
}

// ============================================================
// Состояние календаря
// ============================================================

const today = new Date();
let viewYear = today.getFullYear();
let viewMonth = today.getMonth() + 1; // 1..12

// ============================================================
// DOM-элементы
// ============================================================

const el = {
  weekdayRow: document.getElementById("weekdayRow"),
  grid: document.getElementById("calendarGrid"),
  loading: document.getElementById("loadingIndicator"),
  hijriTitle: document.getElementById("hijriMonthTitle"),
  gregTitle: document.getElementById("gregMonthTitle"),
  prevBtn: document.getElementById("prevMonth"),
  nextBtn: document.getElementById("nextMonth"),

  overlay: document.getElementById("dayCardOverlay"),
  sheet: document.getElementById("dayCardSheet"),
  closeBtn: document.getElementById("closeCard"),
  moonIcon: document.getElementById("moonIcon"),
  moonCaption: document.getElementById("moonCaption"),
  cardWeekday: document.getElementById("cardWeekday"),
  cardHijriDay: document.getElementById("cardHijriDay"),
  cardHijriMonth: document.getElementById("cardHijriMonth"),
  cardGregDate: document.getElementById("cardGregDate"),
  prayerList: document.getElementById("prayerTimesList"),
  fridaySection: document.getElementById("fridaySection"),
  fridayList: document.getElementById("fridayList"),
  quoteTypeLabel: document.getElementById("quoteTypeLabel"),
  quoteText: document.getElementById("quoteText"),
  quoteSource: document.getElementById("quoteSource"),
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
el.closeBtn.addEventListener("click", closeDayCard);
el.overlay.addEventListener("click", (e) => {
  if (e.target === el.overlay) closeDayCard();
});

(async function start() {
  await initLocation();
  await loadMonth();
})();

// ============================================================
// Загрузка и рендер месяца
// ============================================================

function changeMonth(delta) {
  viewMonth += delta;
  if (viewMonth > 12) { viewMonth = 1; viewYear++; }
  if (viewMonth < 1) { viewMonth = 12; viewYear--; }
  loadMonth();
}

async function loadMonth() {
  setLoading(true);
  try {
    const res = await fetch(`${API_BASE}/calendar/month/${viewYear}/${viewMonth}`);
    if (!res.ok) throw new Error("Не удалось загрузить календарь");
    const days = await res.json();
    renderMonth(days);
  } catch (err) {
    console.error(err);
    el.grid.innerHTML = `<div class="loading-indicator">Не удалось загрузить календарь. Проверьте подключение к API.</div>`;
  } finally {
    setLoading(false);
  }
}

function renderMonth(days) {
  el.grid.innerHTML = "";

  if (days.length === 0) return;

  // Заголовок: месяц Хиджры (берём из первого дня месяца — обычно совпадает
  // на большей части месяца, переход виден по числу/названию у самих ячеек)
  const midDay = days[Math.floor(days.length / 2)];
  el.hijriTitle.textContent = `${midDay.hijri.monthNameRu} ${midDay.hijri.year}`;
  const gregDate = new Date(days[0].gregorianDate);
  el.gregTitle.textContent = gregDate.toLocaleDateString("ru-RU", { month: "long", year: "numeric" });

  // Пустые ячейки перед первым днём месяца (неделя начинается с понедельника)
  const firstDate = new Date(days[0].gregorianDate);
  let firstWeekday = firstDate.getDay(); // 0 = вс
  firstWeekday = firstWeekday === 0 ? 6 : firstWeekday - 1; // 0 = пн

  for (let i = 0; i < firstWeekday; i++) {
    const empty = document.createElement("div");
    empty.className = "day-cell empty";
    el.grid.appendChild(empty);
  }

  days.forEach((day) => {
    const cell = document.createElement("div");
    cell.className = "day-cell";
    if (day.isFriday) cell.classList.add("is-friday");
    if (day.isToday) cell.classList.add("is-today");

    const gregSpan = document.createElement("span");
    gregSpan.className = "day-cell__greg";
    gregSpan.textContent = new Date(day.gregorianDate).getDate();

    const hijriSpan = document.createElement("span");
    hijriSpan.className = "day-cell__hijri";
    hijriSpan.textContent = day.hijri.day;

    cell.appendChild(gregSpan);
    cell.appendChild(hijriSpan);

    cell.addEventListener("click", () => openDayCard(day.gregorianDate));
    el.grid.appendChild(cell);
  });
}

function setLoading(isLoading) {
  el.loading.classList.toggle("hidden", !isLoading);
}

// ============================================================
// Карточка дня
// ============================================================

async function openDayCard(dateStr) {
  el.overlay.classList.remove("hidden");
  el.sheet.classList.remove("hidden");
  tg?.HapticFeedback?.impactOccurred?.("light");

  try {
    const params = new URLSearchParams({ date: dateStr });
    if (userLocation) {
      params.set("lat", userLocation.lat);
      params.set("lon", userLocation.lon);
    }

    const res = await fetch(`${API_BASE}/day?${params.toString()}`);
    if (!res.ok) throw new Error("Не удалось загрузить карточку дня");
    const card = await res.json();
    renderDayCard(card);
  } catch (err) {
    console.error(err);
    el.quoteText.textContent = "Не удалось загрузить данные дня. Проверьте подключение к API.";
  }
}

function closeDayCard() {
  el.overlay.classList.add("hidden");
  el.sheet.classList.add("hidden");
}

function renderDayCard(card) {
  const gregDate = new Date(card.gregorianDate);

  el.cardWeekday.textContent = card.hijri.weekdayRu;
  el.cardHijriDay.textContent = card.hijri.day;
  el.cardHijriMonth.textContent = `${card.hijri.monthNameRu} ${card.hijri.year} г.х.`;
  el.cardGregDate.textContent = gregDate.toLocaleDateString("ru-RU", {
    day: "numeric", month: "long", year: "numeric"
  });

  // Фаза Луны
  const phaseFraction = card.moon.ageDays / card.moon.synodicMonthDays;
  el.moonIcon.innerHTML = buildMoonPhaseSvg(phaseFraction);
  el.moonCaption.textContent = `${card.moon.phaseNameRu} · освещённость ${Math.round(card.moon.illuminationPercent)}%`;

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
