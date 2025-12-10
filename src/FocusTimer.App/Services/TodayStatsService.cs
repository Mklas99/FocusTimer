using System;
using System.Collections.Generic;
using System.Linq;
using FocusTimer.Core.Models;

namespace FocusTimer.App.Services
{
    public class TodayStatsService
    {
        private TimeSpan _todayTotal = TimeSpan.Zero;
        private DateTime _today = DateTime.Today;

        public void AddEntries(IEnumerable<TimeEntry> entries)
        {
            var today = DateTime.Today;
            if (_today != today)
            {
                _today = today;
                _todayTotal = TimeSpan.Zero;
            }

            foreach (var entry in entries)
            {
                var entryDate = entry.StartTime.Date;
                if (entryDate == today && entry.Duration.HasValue)
                    _todayTotal += entry.Duration.Value;
            }
        }

        public TimeSpan GetTodayTotal() => _todayTotal;
    }
}
