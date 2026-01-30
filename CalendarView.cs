using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildCalendar
{
    public class CalendarView : View
    {
        private const int HEADER_HEIGHT = 50;
        private const int DAY_HEADER_HEIGHT = 30;
        private const int FIXED_CELL_HEIGHT = 90;
        private DateTime _currentMonth;
        private List<GuildEvent> _events;
        private Func<bool> _canEdit;
        private Label _monthLabel;
        private Label _statusLabel;
        private Panel _gridPanel;
        private string _currentStatusText = "";
        private Color _currentStatusColor = Color.Transparent;

        private readonly TimeZoneInfo _etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        public CalendarView(Func<bool> canEdit)
        {
            _canEdit = canEdit;
            _currentMonth = DateTime.Now;
            _events = new List<GuildEvent>();
        }

        public void UpdateStatus(string message, Color color)
        {
            _currentStatusText = (color == Color.Green) ? "" : message;
            _currentStatusColor = (color == Color.Green) ? Color.Transparent : color;
            if (_statusLabel != null) { _statusLabel.Text = _currentStatusText; _statusLabel.TextColor = _currentStatusColor; }
        }

        public void UpdateEvents(List<GuildEvent> events)
        {
            _events = events ?? new List<GuildEvent>();
            if (_gridPanel != null) RenderCalendar();
        }

        protected override void Build(Container buildPanel)
        {
            new Panel()
            {
                Parent = buildPanel,
                Size = buildPanel.Size,
                Location = new Point(0, 0),
                BackgroundColor = Color.Black * 0.8f,
                ZIndex = 0
            };

            var headerPanel = new Panel() { Parent = buildPanel, Size = new Point(buildPanel.Width, HEADER_HEIGHT), Location = new Point(0, 0), ZIndex = 1 };

            var prevButton = new StandardButton() { Parent = headerPanel, Text = "<", Width = 30, Location = new Point(20, 10) };
            prevButton.Click += (s, e) => ChangeMonth(-1);

            _monthLabel = new Label()
            {
                Parent = headerPanel,
                Text = _currentMonth.ToString("MMMM yyyy"),
                Font = GameService.Content.DefaultFont32,
                AutoSizeWidth = false,
                Width = 250,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Location = new Point(50, 0),
                Size = new Point(250, 40)
            };

            var nextButton = new StandardButton() { Parent = headerPanel, Text = ">", Width = 30, Location = new Point(300, 10) };
            nextButton.Click += (s, e) => ChangeMonth(1);

            _statusLabel = new Label() { Parent = headerPanel, Text = _currentStatusText, TextColor = _currentStatusColor, AutoSizeWidth = true, Location = new Point(400, 15) };

            _gridPanel = new Panel()
            {
                Parent = buildPanel,
                Location = new Point(0, HEADER_HEIGHT),
                Size = new Point(buildPanel.Width, buildPanel.Height - HEADER_HEIGHT),
                ZIndex = 1
            };

            RenderCalendar();
        }

        private void ChangeMonth(int offset)
        {
            _currentMonth = _currentMonth.AddMonths(offset);
            if (_monthLabel != null) _monthLabel.Text = _currentMonth.ToString("MMMM yyyy");
            RenderCalendar();
        }

        private DateTime GetLocalEventDate(GuildEvent evt)
        {
            return TimeZoneInfo.ConvertTime(evt.Date, _etZone, TimeZoneInfo.Local).Date;
        }

        private void RenderCalendar()
        {
            if (_gridPanel == null) return;
            _gridPanel.ClearChildren();

            int cellWidth = _gridPanel.Width / 7;
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            for (int i = 0; i < 7; i++)
            {
                new Label() { Parent = _gridPanel, Text = days[i], Size = new Point(cellWidth, DAY_HEADER_HEIGHT), Location = new Point(i * cellWidth, 0), HorizontalAlignment = HorizontalAlignment.Center, TextColor = Color.Yellow };
            }

            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int startOffset = (int)firstDayOfMonth.DayOfWeek - 1;
            if (startOffset < 0) startOffset = 6;
            var startDate = firstDayOfMonth.AddDays(-startOffset);

            for (int i = 0; i < 35; i++)
            {
                var cellDate = startDate.AddDays(i);
                int row = i / 7;
                int col = i % 7;

                var cell = new Panel()
                {
                    Parent = _gridPanel,
                    Size = new Point(cellWidth - 2, FIXED_CELL_HEIGHT - 2),
                    Location = new Point(col * cellWidth, (row * FIXED_CELL_HEIGHT) + DAY_HEADER_HEIGHT),
                    BackgroundColor = (cellDate.Month == _currentMonth.Month) ? Color.Black * 0.4f : Color.Black * 0.1f
                };

                new Label() { Parent = cell, Text = cellDate.Day.ToString(), Location = new Point(5, 5), TextColor = (cellDate.Date == DateTime.Now.Date) ? Color.Red : Color.White };

                var daysEvents = _events.Where(e => GetLocalEventDate(e) == cellDate.Date).ToList();

                int yPos = 25;
                foreach (var evt in daysEvents)
                {
                    if (yPos > FIXED_CELL_HEIGHT - 20) break;

                    var lbl = new Label()
                    {
                        Parent = cell,
                        Text = evt.Title,
                        Location = new Point(4, yPos),
                        AutoSizeWidth = false,
                        Width = cellWidth - 6,
                        TextColor = Color.LightBlue,
                        Font = GameService.Content.DefaultFont12,
                        BasicTooltipText = "Click for Details"
                    };
                    lbl.Click += (s, e) => ShowDetails(evt);
                    yPos += 16;
                }
            }
        }

        private void ShowDetails(GuildEvent evt)
        {
            int contentWidth = 720;

            var localTime = TimeZoneInfo.ConvertTime(evt.Date, _etZone, TimeZoneInfo.Local);

            string descText = string.IsNullOrEmpty(evt.Description)
                ? "No notes provided."
                : evt.Description
                    .Replace(@"\,", ",")
                    .Replace(@"\;", ";")
                    .Replace(@"\\", @"\")
                    .Replace(@"\n", "\n")
                    .Replace(@"\N", "\n")
                    .Replace("\r", "")
                    .Trim();

            var window = new StandardWindow(
                GameService.Content.DatAssetCache.GetTextureFromAssetId(155985),
                new Rectangle(40, 26, 913, 691),
                new Rectangle(70, 71, 839, 605))
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "Event Details",
                Emblem = GameService.Content.DatAssetCache.GetTextureFromAssetId(156022),
                Location = new Point(300, 200),
                SavesPosition = true,
                Size = new Point(850, 650)
            };

            var content = new Panel()
            {
                Parent = window,
                Size = new Point(window.ContentRegion.Width, window.ContentRegion.Height),
                Location = new Point(0, 0),
                BackgroundColor = Color.Black * 0.85f
            };

            var titleLbl = new Label()
            {
                Parent = content,
                Text = evt.Title,
                Font = GameService.Content.DefaultFont32,
                TextColor = Color.White,
                AutoSizeHeight = true,
                Width = contentWidth,
                Location = new Point(30, 40),
                WrapText = true
            };

            string timeStr = $"{localTime:dddd, MMMM dd • h:mm tt} local time ({evt.Date:h:mm tt} ET)";
            var timeLbl = new Label()
            {
                Parent = content,
                Text = timeStr,
                Font = GameService.Content.DefaultFont32,
                TextColor = Color.LightCyan,
                Location = new Point(30, titleLbl.Bottom + 30),
                AutoSizeHeight = true,
                Width = contentWidth
            };

            var sep = new Panel()
            {
                Parent = content,
                BackgroundColor = Color.Gray,
                Height = 2,
                Width = contentWidth,
                Location = new Point(30, timeLbl.Bottom + 40)
            };

            int remainingHeight = content.Height - sep.Bottom - 100;

            var descPanel = new Panel()
            {
                Parent = content,
                Location = new Point(30, sep.Bottom + 40),
                Size = new Point(contentWidth + 40, remainingHeight),
                CanScroll = true,
                BackgroundColor = Color.Black * 0.7f
            };

            new Label()
            {
                Parent = descPanel,
                Text = descText,
                Width = contentWidth,
                Location = new Point(20, 20),
                AutoSizeHeight = true,
                WrapText = true,
                TextColor = Color.Beige,
                Font = GameService.Content.DefaultFont18
            };

            window.Show();
        }
    }
}