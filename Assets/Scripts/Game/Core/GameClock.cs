using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// In-game time. A full 24-hour day lasts Settings.DayLengthMinutes of real time (default 30).
    /// The clock stops while every player is in combat, dialogue, a dice roll or a menu.
    /// </summary>
    public class GameClock
    {
        public int Day = 1;            // day 1 = Dawnday
        public float Minutes = 7 * 60; // minutes since midnight

        public int Hour => (int)(Minutes / 60) % 24;
        public int Minute => (int)Minutes % 60;
        public int WeekdayIndex => (Day - 1) % 7;
        public string Weekday => WorldDb.Weekdays[WeekdayIndex];

        public event System.Action<int> HourChanged;
        public event System.Action DayChanged;

        int _lastHour = -1;

        public void Tick(float realSeconds, float dayLengthMinutes)
        {
            float gameMinutesPerRealSecond = 1440f / (dayLengthMinutes * 60f);
            Advance(realSeconds * gameMinutesPerRealSecond);
        }

        public void Advance(float gameMinutes)
        {
            Minutes += gameMinutes;
            while (Minutes >= 1440) { Minutes -= 1440; Day++; DayChanged?.Invoke(); }
            if (Hour != _lastHour) { _lastHour = Hour; HourChanged?.Invoke(Hour); }
        }

        /// <summary>Jump to a specific time on the following morning (sleeping).</summary>
        public void SleepUntil(int hour)
        {
            float target = hour * 60;
            float delta = target - Minutes;
            if (delta <= 0) delta += 1440;
            Advance(delta);
        }

        public string TimeText()
        {
            int h = Hour % 12; if (h == 0) h = 12;
            string ampm = Hour < 12 ? "am" : "pm";
            return $"{h}:{(Minute / 10) * 10:00} {ampm}";
        }

        /// <summary>Ambient tint for outdoor scenes: clear by day, warm at dusk, blue at night.</summary>
        public Color AmbientTint()
        {
            float h = Minutes / 60f;
            Color day = new Color(0, 0, 0, 0);
            Color dusk = new Color(0.55f, 0.25f, 0.1f, 0.22f);
            Color night = new Color(0.04f, 0.06f, 0.2f, 0.55f);
            if (h >= 6.5f && h < 17.5f) return day;
            if (h >= 17.5f && h < 19.5f) return Color.Lerp(day, dusk, (h - 17.5f) / 2f);
            if (h >= 19.5f && h < 21f) return Color.Lerp(dusk, night, (h - 19.5f) / 1.5f);
            if (h >= 5f && h < 6.5f) return Color.Lerp(night, day, (h - 5f) / 1.5f);
            return night;
        }

        public bool IsNight => Hour >= 20 || Hour < 6;
    }
}
