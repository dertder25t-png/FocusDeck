# FocusDeck Phase 3: API Integration Setup Guide

This guide walks through setting up Google Calendar and Canvas API integrations for FocusDeck.

## Google Calendar Integration Setup

### Step 1: Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the project dropdown at the top
3. Click "NEW PROJECT"
4. Name it "FocusDeck" and click Create
5. Wait for the project to be created, then select it

### Step 2: Enable Google Calendar API

1. In the Google Cloud Console, go to **APIs & Services → Library**
2. Search for "Google Calendar API"
3. Click on it and press **ENABLE**
4. Wait for it to enable (should show "Manage" button)

### Step 3: Create OAuth2 Credentials

1. Go to **APIs & Services → Credentials**
2. Click **+ CREATE CREDENTIALS** → **OAuth client ID**
3. Select **Desktop application** as the application type
4. Give it the name "FocusDeck"
5. Click **CREATE**
6. You'll see a popup with your credentials. Click **DOWNLOAD JSON** or note:
   - **Client ID**: Copy this
   - **Client Secret**: Copy this

### Step 4: Configure FocusDeck

When you open FocusDeck and navigate to Settings → Calendar:

1. Paste your **Client ID** in the Google Calendar Client ID field
2. Paste your **Client Secret** in the Google Calendar Client Secret field
3. Check **Enable Google Calendar**
4. Click **Authorize with Google**
5. You'll be redirected to Google's login page
6. Sign in with your Google account
7. Grant FocusDeck permission to access your calendar
8. Return to FocusDeck - your token will be saved automatically

FocusDeck will now sync your calendar events every 15 minutes (configurable).

---

## Canvas LMS Integration Setup

### Step 1: Get Your Canvas API Token

1. Log into your Canvas instance (e.g., https://yourschool.instructure.com)
2. Click your **Profile** (user icon in top right)
3. Click **Settings**
4. Scroll down to **Approved Integrations**
5. Click **+ New Access Token**
6. Give it a name like "FocusDeck"
7. Leave the expiration date blank (or set it far in the future)
8. Click **Generate Token**
9. **Copy the token** - you won't be able to see it again!

### Step 2: Find Your Canvas Instance URL

Your Canvas instance URL is the base URL you use to access Canvas:
- Example: `https://myschool.instructure.com`
- Or: `https://canvas.company.com`

### Step 3: Configure FocusDeck

When you open FocusDeck and navigate to Settings → Canvas:

1. Paste your **Canvas Instance URL** (e.g., `https://myschool.instructure.com`)
2. Paste your **API Token** in the Canvas API Token field
3. Check **Enable Canvas**
4. Click **Test Connection** to verify it works
5. If successful, click **Save**

FocusDeck will now:
- Fetch all your Canvas courses
- Detect assignments from those courses
- Automatically create tasks for upcoming assignments
- Show assignment due dates in the calendar
- Sync every 15 minutes (configurable)

---

## What Happens After Setup

### Google Calendar
- **Automatic**: Every 15 minutes, FocusDeck fetches your Google Calendar events
- **Display**: Upcoming events appear in the Calendar view
- **Smart Layouts**: You can set a layout to automatically apply when specific calendar events start
- **Reminders**: Notifications 5 minutes (configurable) before events

### Canvas Assignments
- **Automatic**: Every 15 minutes, FocusDeck fetches your Canvas assignments
- **To-Do List**: Assignments automatically become tasks in your to-do list
- **Calendar**: Due dates appear on the calendar
- **Priority**: Overdue assignments appear first with visual indicators
- **Tracking**: Mark assignments as complete when submitted

### Combined Features
- Calendar events and assignments appear together in timeline view
- Study sessions automatically block out time before due dates
- Effectiveness ratings help identify your most productive times
- Workspaces can be triggered by specific calendar events

---

## Troubleshooting

### "Test Connection Failed" for Canvas
- Verify your instance URL is correct (no trailing slash)
- Confirm the API token hasn't expired
- Check that you copied the entire token without extra spaces
- Ensure your Canvas user has permission to access courses

### Google Calendar Not Syncing
- Verify your Client ID and Secret are correct
- Check that the Google Calendar API is enabled in Google Cloud Console
- Re-authorize: Click "Authorize with Google" again
- Check your Google account permissions at [myaccount.google.com/permissions](https://myaccount.google.com/permissions)

### Assignments Not Appearing
- Click "Sync Now" in the Tasks view to force an immediate sync
- Verify you're enrolled in at least one Canvas course
- Check that assignments have due dates set
- Ensure your Canvas user role can view assignments

### Can't Find Settings
In FocusDeck:
1. Look for a ⚙️ Settings icon or menu
2. Or click the FocusDeck icon in taskbar and select "Settings"
3. Look for "Calendar" or "API" tab

---

## Privacy & Security Notes

✅ **What FocusDeck does:**
- Stores tokens locally in `%LOCALAPPDATA%\FocusDeck\calendar_settings.json`
- Only syncs events/assignments you have access to
- Never stores your calendar data on external servers

❌ **What FocusDeck does NOT do:**
- Never sends your data to third-party servers
- Never modifies your calendar events (read-only)
- Never modifies Canvas assignments (read-only for status tracking)
- Never shares your tokens with anyone

---

## Revoking Access

### Google Calendar
1. Go to [myaccount.google.com/permissions](https://myaccount.google.com/permissions)
2. Find "FocusDeck" in your connected apps
3. Click it and select "Remove access"

### Canvas
1. Log into Canvas
2. Go to **Profile → Settings → Approved Integrations**
3. Find your "FocusDeck" token
4. Click the trash icon to delete it

---

## Next Steps

After setting up both integrations:

1. **Try the Study Session Tracker**: Start a study session and watch your effectiveness rating
2. **Set Up Automations**: Configure time-based rules in Settings → Automations
3. **Create Study Workspaces**: Design layouts for different subjects
4. **Explore Calendar Views**: Check out the calendar UI with your real events
5. **Fine-Tune Sync**: Adjust sync interval if you want more/less frequent updates

Questions? Check the main README.md for the full feature list and project status.
