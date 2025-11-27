# 🤖 FocusDeck Automation System

## Overview

The FocusDeck Automation System is a powerful, flexible framework that allows you to create custom workflows triggered by events from various sources. It connects your study sessions, tasks, calendar, smart home, and external services into a cohesive productivity ecosystem.

## Core Concepts

### Automations
An **Automation** is a rule that consists of:
- **Name**: A descriptive title (e.g., "Start Pomodoro when study event begins")
- **Trigger**: The event that starts the automation
- **Actions**: One or more actions to perform when triggered
- **Enabled/Disabled**: Whether the automation is active

### Triggers
**Triggers** are events that start an automation. They can come from:
- Time and scheduling
- App events (sessions, tasks, notes)
- System activity (applications, files, user behavior)
- External APIs (Google Calendar, Canvas, Home Assistant, etc.)
- Location and sensors
- Health and biometrics

### Actions
**Actions** are what happens when a trigger fires. Examples:
- Start a focus timer
- Create a task
- Send a notification
- Control smart lights
- Play music on Spotify
- Log activity

---

## Available Triggers

### ⏰ Time-Based Triggers

| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **At Specific Time** | Fires at an exact time and day | `days: "Mon,Tue,Wed,Thu,Fri"`, `time: "09:00"` |
| **Time of Day** | Fires within a time range | `period: "morning"` (6am-12pm) |
| **Sunrise / Sunset** | Fires at local sunrise/sunset | `event: "sunset"`, `offset_minutes: -30` |
| **On Date** | Fires on a specific calendar date | `date: "2025-12-01"`, `recurring: true` |
| **Recurring Interval** | Fires on a loop | `interval_minutes: 30` |
| **On App Launch** | When FocusDeck starts | - |
| **On App Close** | When FocusDeck exits | - |
| **On App Update** | First launch after update | - |

**Example Automation:**
```json
{
  "name": "Morning Focus Routine",
  "trigger": {
    "service": "FocusDeck",
    "triggerType": "time.specific",
    "settings": {
      "days": "Mon,Tue,Wed,Thu,Fri",
      "time": "09:00"
    }
  },
  "actions": [
    { "actionType": "notification.show", "settings": { "message": "Good morning! Ready to focus?" } },
    { "actionType": "timer.start_pomodoro", "settings": { "duration": "25" } }
  ]
}
```

---

### 📚 Study Session Triggers

| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Session Started** | When any focus session begins | `session_type: "Pomodoro"` (optional filter) |
| **Session Completed** | When a session finishes naturally | `min_duration: "20"` |
| **Session Stopped** | When manually stopped | - |
| **Session Paused / Resumed** | Timer paused or resumed | - |
| **Break Started** | When a break timer begins | - |
| **Break Completed** | When break finishes | - |

**Example Automation:**
```json
{
  "name": "Dim Lights During Focus",
  "trigger": {
    "service": "FocusDeck",
    "triggerType": "session.started",
    "settings": {}
  },
  "actions": [
    { "actionType": "lights.set_scene", "settings": { "scene": "focus", "brightness": "30" } },
    { "actionType": "spotify.play_playlist", "settings": { "playlist_id": "focus_music" } }
  ]
}
```

---

### ✅ Task & Todo Triggers

| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Task Created** | New task added | `tag: "urgent"` (optional) |
| **Task Completed** | Task checked off | `priority: "high"` |
| **Task Due** | At the moment a task is due | - |
| **Task Due Approaching** | Before a task is due | `minutes_before: 60` |
| **Task Priority Changed** | Priority set or changed | `new_priority: "high"` |
| **Task Moved** | Moved to different list | `to_list: "In Progress"` |
| **Task Tagged** | Specific tag added | `tag: "#Urgent"` |

**Example Automation:**
```json
{
  "name": "Urgent Task Alert",
  "trigger": {
    "service": "FocusDeck",
    "triggerType": "task.due_approaching",
    "settings": {
      "minutes_before": "60"
    }
  },
  "actions": [
    { "actionType": "notification.show", "settings": { "title": "Task Due Soon", "message": "{{task_title}} is due in 1 hour!" } },
    { "actionType": "notification.sound", "settings": { "sound": "alert" } }
  ]
}
```

---

### 💻 System Activity Triggers

| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Application Launched** | Specific app opened | `app_name: "photoshop.exe"` |
| **Application Closed** | Specific app closed | `app_name: "chrome.exe"` |
| **Application in Focus** | App becomes active window | `app_name: "code.exe"` |
| **Window Title** | Active window title contains text | `contains: "Figma - "` |
| **User Idle** | No mouse/keyboard activity | `idle_minutes: 5` |
| **User Returns from Idle** | Activity after being idle | - |
| **System Locked / Unlocked** | Win+L or unlock | `action: "locked"` |
| **System Shutdown / Sleep** | Before shutdown/sleep | - |
| **System Wake** | Computer wakes from sleep | - |
| **File Changed** | File created/modified/deleted | `folder: "~/Downloads"`, `pattern: "*.pdf"` |
| **Network Connected** | Wi-Fi connects | `ssid: "Office-WIFI"` |
| **Hotkey Pressed** | Global key combination | `hotkey: "Ctrl+Alt+S"` |
| **Clipboard Content** | Clipboard contains pattern | `pattern: "https://"` |

**Example Automation:**
```json
{
  "name": "Auto-pause when AFK",
  "trigger": {
    "service": "FocusDeck",
    "triggerType": "system.user_idle",
    "settings": {
      "idle_minutes": "5"
    }
  },
  "actions": [
    { "actionType": "timer.pause", "settings": {} }
  ]
}
```

---

### 🌐 External API Triggers

#### Google Calendar
| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Event Start** | Event begins (with warning time) | `calendar: "Work"`, `minutes_before: 5`, `event_contains: "Study"` |
| **Event End** | Event finishes | `calendar: "School"` |
| **Event Created** | New event added | `event_contains: "Meeting"` |

#### Canvas LMS
| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Assignment Due** | Assignment due today | `course_id: "12345"` |
| **New Grade** | Grade posted | `course_id: "12345"` |
| **New Announcement** | Announcement posted | `course_contains: "CS101"` |

#### Home Assistant
| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Webhook** | Receive webhook from HA | `entity_id: "binary_sensor.desk_occupied"`, `state: "on"` |

#### Others
- **Email Received**: `from: "professor@school.edu"`, `subject_contains: "Assignment"`
- **GitHub**: Issue assigned, PR review, build failed
- **Discord/Slack**: Mention or DM received
- **Spotify**: (Currently actions only)

**Example Automation:**
```json
{
  "name": "Start timer when study event begins",
  "trigger": {
    "service": "GoogleCalendar",
    "triggerType": "google_calendar.event_start",
    "settings": {
      "calendar": "School",
      "event_contains": "Study",
      "minutes_before": "5"
    }
  },
  "actions": [
    { "actionType": "notification.show", "settings": { "message": "Study session starting in 5 minutes!" } },
    { "actionType": "timer.start", "settings": { "duration": "60" } },
    { "actionType": "lights.set_scene", "settings": { "scene": "focus" } }
  ]
}
```

---

### 🏃 Health & Biometric Triggers

(Requires wearable integration: Oura, Fitbit, Apple Watch)

| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Sleep Logged** | Sleep session recorded | - |
| **Wake Up** | Morning wake-up detected | - |
| **Readiness Score** | Score below threshold | `below: 60` |
| **Heart Rate** | Heart rate above/below value | `above: 100`, `exclude_exercise: true` |
| **Inactivity** | Low step count | `max_steps: 50`, `time_window: 60` |
| **Mindfulness Completed** | Meditation session done | - |

---

### 🔗 Complex & Chained Triggers

| Trigger | Description | Example Settings |
|---------|-------------|------------------|
| **Conditional Time** | Time + condition | `time: "09:00"`, `condition: "has_calendar_event"` |
| **Consecutive Events** | Multiple events in a row | `event_type: "session.completed"`, `count: 3` |
| **Absence of Event** | Event hasn't happened | `event_type: "session.started"`, `by_time: "11:00"` |
| **Task List State** | All tasks in list completed | `list: "Today"` |
| **Streak** | Daily streak milestone | `event_type: "task_completed"`, `days: 7` |
| **Manual Trigger** | Manual "Run" button | - |
| **Webhook Received** | Generic webhook endpoint | `webhook_id: "custom_webhook_123"` |

---

## Available Actions

### ⏲️ Timer & Session Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Start Timer** | Start a focus session | `duration: 25`, `label: "Deep Work"` |
| **Stop Timer** | Stop current session | - |
| **Pause Timer** | Pause active timer | - |
| **Start Break** | Start a break timer | `duration: 5` |
| **Start Pomodoro** | Start full Pomodoro cycle | `work: 25`, `break: 5`, `cycles: 4` |

### ✅ Task Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Create Task** | Add a new task | `title: "{{trigger_data}}"`, `priority: "high"`, `due_date: "today"` |
| **Complete Task** | Check off a task | `task_id: "..."` or `task_title: "..."` |
| **Set Priority** | Change task priority | `task_id: "..."`, `priority: "high"` |
| **Add Tag** | Add tag to task | `task_id: "..."`, `tag: "#urgent"` |
| **Move Task** | Move to different list | `task_id: "..."`, `to_list: "Done"` |

### 🔔 Notification Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Show Notification** | Display system notification | `title: "Break Time"`, `message: "Take a 5 minute break"` |
| **Play Sound** | Play audio alert | `sound: "bell"`, `volume: 70` |
| **Show Alert** | In-app modal alert | `message: "Don't forget to stretch!"` |
| **Send Email** | Send email notification | `to: "me@example.com"`, `subject: "..."`, `body: "..."` |

### 🏠 Home Assistant Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Turn On** | Turn on entity | `entity_id: "light.desk_lamp"` |
| **Turn Off** | Turn off entity | `entity_id: "switch.fan"` |
| **Set State** | Set entity state | `entity_id: "input_boolean.study_mode"`, `state: "on"` |
| **Call Service** | Call HA service | `service: "scene.turn_on"`, `data: {"entity_id": "scene.focus"}` |

### 🎵 Spotify Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Play** | Resume playback | - |
| **Pause** | Pause playback | - |
| **Play Playlist** | Start a playlist | `playlist_id: "37i9dQZF1DX..."` or `playlist_name: "Focus Music"` |
| **Set Volume** | Adjust volume | `volume: 50` |

### 💡 Smart Lighting Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Set Scene** | Activate lighting scene | `scene: "focus"`, `brightness: 60` |
| **Dim Lights** | Reduce brightness | `brightness: 30`, `lights: "desk,overhead"` |
| **Set Color** | Change light color | `color: "blue"` or `rgb: "0,100,255"` |

### 💻 System Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Run Command** | Execute shell command | `command: "shutdown /s /t 0"` (Windows) |
| **Open Application** | Launch an app | `app_path: "C:\\Program Files\\..."` |
| **Close Application** | Close an app | `app_name: "chrome.exe"` |
| **Open URL** | Open URL in browser | `url: "https://canvas.instructure.com"` |
| **Set Clipboard** | Copy text to clipboard | `text: "{{session_notes}}"` |
| **Press Hotkey** | Simulate key press | `keys: "Ctrl+Alt+P"` |

### 📊 Data Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Sync Data** | Trigger cloud sync | - |
| **Export Data** | Export to JSON/CSV | `format: "json"`, `include: "tasks,sessions"` |
| **Log Activity** | Log custom event | `event_type: "custom"`, `data: {...}` |

### 🧠 Advanced Actions

| Action | Description | Example Settings |
|--------|-------------|------------------|
| **Wait** | Pause before next action | `seconds: 10` |
| **HTTP Request** | Make custom API call | `url: "..."`, `method: "POST"`, `body: {...}` |
| **Conditional** | If-then-else logic | `condition: "{{time}} < 12:00"`, `then: [...]`, `else: [...]` |
| **Loop** | Repeat actions | `count: 3`, `delay: 5`, `actions: [...]` |
| **Trigger Automation** | Run another automation | `automation_id: "..."` |

---

## Setting Up Integrations

### Google Calendar

1. Go to **Settings → Services**
2. Click **Connect Google Calendar**
3. Sign in and grant permissions
4. Select which calendars to watch

### Canvas LMS

1. Get your Canvas API token: `Settings → Approved Integrations → + New Access Token`
2. In FocusDeck: **Settings → Services → Connect Canvas**
3. Enter your Canvas domain (e.g., `myschool.instructure.com`) and API token

### Home Assistant

1. In Home Assistant: `Settings → Automations & Scenes → Create Automation → Webhook`
2. Get the webhook URL (e.g., `/api/webhook/abc123`)
3. In FocusDeck: **Settings → Services → Connect Home Assistant**
4. Enter your HA URL and webhook token

### Spotify

1. Go to **Settings → Services**
2. Click **Connect Spotify**
3. Sign in and grant playback control permissions

---

## Example Workflows

### "Perfect Study Setup"

**Trigger:** Google Calendar event starting (5 min before)  
**Conditions:** Event title contains "Study"

**Actions:**
1. Show notification: "Study session starting soon!"
2. Dim office lights to 40%
3. Start Spotify playlist: "Lo-fi Beats"
4. Start 25-minute Pomodoro timer
5. Set Home Assistant input_boolean.study_mode to ON

---

### "Assignment Deadline Reminder"

**Trigger:** Canvas assignment due (1 day before)

**Actions:**
1. Create high-priority task: "Submit {{assignment_name}}"
2. Show alert: "Assignment due tomorrow!"
3. Send email reminder to self

---

### "Auto-pause Timer When Away"

**Trigger:** User idle for 5 minutes

**Actions:**
1. Pause current timer
2. Show notification: "Timer paused (you've been away)"

---

### "End of Day Routine"

**Trigger:** Specific time (6:00 PM on weekdays)

**Actions:**
1. Stop any active timers
2. Export day's data to Google Drive
3. Show summary notification: "You completed {{completed_tasks}} tasks today!"
4. Turn off office lights

---

## API Reference

### Automations Endpoints

```
GET    /api/automations          # List all automations
GET    /api/automations/{id}     # Get specific automation
POST   /api/automations          # Create new automation
PUT    /api/automations/{id}     # Update automation
DELETE /api/automations/{id}     # Delete automation
POST   /api/automations/{id}/toggle  # Enable/disable
POST   /api/automations/{id}/run     # Trigger manually
```

### Services Endpoints

```
GET    /api/services                    # List connected services
POST   /api/services/connect/{service}  # Connect service
DELETE /api/services/{id}               # Disconnect service
GET    /api/services/oauth/{service}/url # Get OAuth URL
```

---

## Variables in Actions

You can use variables from triggers in your actions:

| Variable | Description | Example |
|----------|-------------|---------|
| `{{trigger_time}}` | When the trigger fired | `2025-10-31 14:30:00` |
| `{{task_title}}` | Title of task (task triggers) | `"Finish assignment"` |
| `{{task_priority}}` | Task priority | `"high"` |
| `{{event_title}}` | Calendar event title | `"Study Session"` |
| `{{event_location}}` | Event location | `"Library"` |
| `{{session_duration}}` | Session length (minutes) | `25` |
| `{{completed_tasks}}` | Count of completed tasks | `8` |
| `{{app_name}}` | Application name (system triggers) | `"photoshop.exe"` |
| `{{file_path}}` | File path (file triggers) | `"C:\\Downloads\\notes.pdf"` |
| `{{clipboard_content}}` | Clipboard text | `"https://..."` |

**Example:**
```json
{
  "actionType": "notification.show",
  "settings": {
    "message": "Task '{{task_title}}' is due in 1 hour!"
  }
}
```

---

## Security & Privacy

- **API Tokens**: Stored encrypted in local database
- **OAuth**: Uses industry-standard OAuth 2.0 flows
- **Webhooks**: Unique, secret URLs per automation
- **Permissions**: Each integration requests only necessary scopes
- **Data**: All automation data stored locally by default

---

## Troubleshooting

### Automation Not Firing

1. Check that it's **Enabled** (green toggle)
2. Verify the service is **Connected** (Settings → Services)
3. Check trigger settings are correct
4. View automation logs: **Settings → Automation Logs**

### OAuth Connection Failed

1. Clear browser cookies
2. Try connecting in incognito/private mode
3. Check that redirect URI is correct in service settings

### Home Assistant Not Responding

1. Verify HA is accessible from your network
2. Check webhook token is correct
3. Test webhook manually: `curl -X POST https://your-ha.com/api/webhook/YOUR_TOKEN`

---

## Coming Soon

- [ ] Visual automation builder (drag-and-drop)
- [ ] Automation templates marketplace
- [ ] Advanced debugging/testing tools
- [ ] AI-suggested automations based on usage
- [ ] Mobile app support
- [ ] Voice command triggers via Alexa/Google Home

---

**Version:** 1.1.0  
**Last Updated:** October 31, 2025
