using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using Blish_HUD;
using System.Globalization;

namespace GuildCalendar
{
    public class CalendarService
    {
        private static readonly HttpClient _http = new HttpClient();

        // For UTC → ET conversion when needed
        private readonly TimeZoneInfo _etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        public async Task<List<GuildEvent>> FetchEvents(string icalUrl)
        {
            if (string.IsNullOrWhiteSpace(icalUrl)) return new List<GuildEvent>();

            try
            {
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                string iCalData = await _http.GetStringAsync(icalUrl);

                if (iCalData.Contains("<!DOCTYPE html>") || iCalData.Contains("<html"))
                    throw new Exception("Link is a Login Page. Check permissions.");

                
                var rawLines = iCalData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                var unfoldedLines = new List<string>();
                StringBuilder sb = new StringBuilder();

                foreach (var rawLine in rawLines)
                {
                    if (rawLine.StartsWith(" ") || rawLine.StartsWith("\t"))
                    {
                        sb.Append(rawLine.Substring(1));
                    }
                    else
                    {
                        if (sb.Length > 0)
                        {
                            unfoldedLines.Add(sb.ToString());
                            sb.Clear();
                        }
                        sb.Append(rawLine);
                    }
                }
                if (sb.Length > 0) unfoldedLines.Add(sb.ToString());

                var events = new List<GuildEvent>();

                string currentSummary = null;
                string currentDesc = null;
                DateTime? currentDate = null;
                bool inEvent = false;

                // Recurring
                bool isRecurring = false;
                string repeatFreq = "";
                int repeatInterval = 1;

                foreach (var line in unfoldedLines)
                {
                    var l = line.Trim();

                    if (l == "BEGIN:VEVENT")
                    {
                        inEvent = true;
                        currentSummary = null;
                        currentDesc = null;
                        currentDate = null;
                        isRecurring = false;
                        repeatFreq = "";
                        repeatInterval = 1;
                        continue;
                    }

                    if (!inEvent) continue;

                    if (l == "END:VEVENT")
                    {
                        if (currentSummary != null && currentDate.HasValue)
                        {
                            
                            string cleanDesc = (currentDesc ?? "").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\,", ",").Replace("\\;", ";");

                            events.Add(new GuildEvent
                            {
                                Title = currentSummary,
                                Date = currentDate.Value,
                                Description = cleanDesc
                            });

                            // Recurring expansion
                            if (isRecurring && repeatFreq == "WEEKLY")
                            {
                                DateTime nextDate = currentDate.Value;
                                for (int i = 0; i < 52; i++)
                                {
                                    nextDate = nextDate.AddDays(7 * repeatInterval);
                                    events.Add(new GuildEvent { Title = currentSummary, Date = nextDate, Description = cleanDesc });
                                }
                            }
                            else if (isRecurring && repeatFreq == "DAILY")
                            {
                                DateTime nextDate = currentDate.Value;
                                for (int i = 0; i < 60; i++)
                                {
                                    nextDate = nextDate.AddDays(1 * repeatInterval);
                                    events.Add(new GuildEvent { Title = currentSummary, Date = nextDate, Description = cleanDesc });
                                }
                            }
                        }

                        inEvent = false;
                        continue;
                    }

                    if (l.StartsWith("SUMMARY:")) currentSummary = l.Substring(l.IndexOf(':') + 1).Trim();
                    if (l.StartsWith("DESCRIPTION:")) currentDesc = l.Substring(l.IndexOf(':') + 1);

                    
                    if (l.StartsWith("DTSTART"))
                    {
                        try
                        {
                            string datePart = l.Substring(l.IndexOf(':') + 1).Trim();
                            if (datePart.Length == 8 && !datePart.Contains("T"))
                            {
                                int year = int.Parse(datePart.Substring(0, 4));
                                int month = int.Parse(datePart.Substring(4, 2));
                                int day = int.Parse(datePart.Substring(6, 2));
                                currentDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
                            }
                            else if (datePart.Contains("T"))
                            {
                                bool isUtc = datePart.EndsWith("Z");
                                string cleanDate = datePart.Replace("Z", "");
                                int year = int.Parse(cleanDate.Substring(0, 4));
                                int month = int.Parse(cleanDate.Substring(4, 2));
                                int day = int.Parse(cleanDate.Substring(6, 2));
                                int hour = int.Parse(cleanDate.Substring(9, 2));
                                int min = int.Parse(cleanDate.Substring(11, 2));
                                int sec = cleanDate.Length > 13 ? int.Parse(cleanDate.Substring(13, 2)) : 0;

                                if (isUtc)
                                {
                                    
                                    var utcTime = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);
                                    var etTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, _etZone);
                                    currentDate = new DateTime(etTime.Year, etTime.Month, etTime.Day, etTime.Hour, etTime.Minute, etTime.Second, DateTimeKind.Unspecified);
                                }
                                else
                                {
                                    
                                    currentDate = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Unspecified);
                                }
                            }
                        }
                        catch { }
                    }

                    if (l.StartsWith("RRULE:"))
                    {
                        isRecurring = true;
                        if (l.Contains("FREQ=WEEKLY")) repeatFreq = "WEEKLY";
                        if (l.Contains("FREQ=DAILY")) repeatFreq = "DAILY";
                        var intervalMatch = Regex.Match(l, "INTERVAL=([0-9]+)");
                        if (intervalMatch.Success) int.TryParse(intervalMatch.Groups[1].Value, out repeatInterval);
                    }
                }

                var minDate = DateTime.Now.AddYears(-2);
                var maxDate = DateTime.Now.AddYears(2);
                return events.Where(e => e.Date > minDate && e.Date < maxDate).OrderBy(e => e.Date).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch/parse iCal: " + ex.Message);
            }
        }
    }

    public class GuildEvent
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}