# Guild Calendar Module for Blish HUD

A Guild Wars 2 module for Blish HUD that displays your guild's events in-game by syncing with a Google Calendar.

LONG LIVE MY GUILD (DEAD)! The guild in which inspired me to make this! To all of you who were here when I
decided to make this for us, I thank you!

## Features
* **Live Sync:** Automatically pulls events from your guild's public Google Calendar.
* **Multi-Guild Support:** Configure up to 6 different guild calendars with unique tabs.
* **Customization:** Assign specific class icons (Guardian, Warrior, etc.) to each guild tab.
* **Local Time Conversion:** Automatically converts event times from the calendar's timezone to your local time.

## 🛠️ Setup Guide

### Step 1: Get Your Google Calendar Link
To make the calendar visible in-game, you must use the **Public iCal address**.

1.  Open **Google Calendar** in your browser.
2.  In the sidebar, find the guild calendar you want to share.
3.  Click the **three dots** next to it and select **Settings and sharing**.
4.  Under **Access permissions for events**, ensure **"Make available to public"** is checked.
    * *Note: Without this, the module cannot read the events.*
5.  Scroll down to the **Integrate calendar** section.
6.  Copy the URL labeled **"Public address in iCal format"**.
    * *It should end in `.ics`.*

### Step 2: Configure the Module
1.  Open **Blish HUD** and launch Guild Wars 2.
2.  Go to Module Settings
3.  **[Guild 1] Name:** Enter your guild's name (e.g., "DEAD" Thats my actual guild! :D ).
4.  **[Guild 1] Icon:** Choose a class icon to represent this guild tab.
5.  **[Guild 1] iCal Link:** Paste the `.ics` link you copied in Step 1 and press enter.
6.  **[Guild 1] Show in Window:** Check this box to make the tab visible.

Uncheck any Show in Window for a guild you dont want to show, if all are checked every guild icon will be on the calendar.

## ⚠️ Officer Tools & Permissions

The "Enable Officer Tools" setting allows you to edit the calendar view locally, but please note:

> **Important:** This module is primarily a **viewer**. 
> * You **cannot** add events to the official Google Calendar from within the game.
> * Any events created using the in-game tools are saved to your **PERSONAL local calendar only** and will not be seen by other guild members.
> * To add official guild events, you must add them directly to the Google Calendar via a web browser.

## Troubleshooting

* **Error: "No Link"** You haven't pasted a URL into the settings yet, or the "Show in Window" box is unchecked.
* **Error: "404 Not Found"** The calendar link is incorrect or the calendar is **Private**. Go back to Google Calendar settings and make sure "Make available to public" is checked.

* **Events not showing up?** The module refreshes periodically. If you just added an event to Google Calendar, give it a few minutes to sync or toggle the module off and on.
