using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Interfaces;

public interface ISessionRepository
{
    Task SaveSessionAsync(IEnumerable<TimeEntry> entries);
    Task<IEnumerable<TimeEntry>> GetSessionsByDateAsync(DateTime date);
}
