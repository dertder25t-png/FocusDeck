# Service Capabilities & Integration Guide

## Overview

FocusDeck supports 16 different service integrations, each offering unique triggers and actions for your study automation workflows. This document outlines what each service can do and how to use them effectively.

---

## 🎯 FocusDeck (Built-in)

**Authentication:** None required (built-in)

### Triggers
- ⏰ **At Specific Time** - Execute automations at exact times
- 🔁 **Recurring Schedule** - Daily, weekly, or custom recurring triggers
- ▶️ **Session Started** - When a Pomodoro/study session begins
- ✅ **Session Completed** - When a study session finishes
- ⏸️ **Session Paused** - When you pause a session
- ☕ **Break Started** - When a break period begins
- 🔄 **Break Ended** - When returning from break
- 📝 **Task Created** - New task added to planner
- ✓ **Task Completed** - Task marked as done
- ⚠️ **Task Due Soon** - Task approaching deadline
- 🚨 **Task Overdue** - Task past due date
- 🔴 **High Priority Task Added** - Urgent task created
- 🃏 **Deck Created** - New flashcard deck created
- 🎯 **Study Milestone Reached** - Study goals achieved

### Actions
- Start/stop/pause timer
- Create/update/complete tasks
- Show notification
- Play sound alert
- Update statistics

---

## 📅 Google Calendar

**Authentication:** OAuth 2.0  
**Scopes:** `calendar.readonly`

### Triggers
- 📆 **Event Starting Soon** - Calendar event about to begin (5/15/30 min before)
- 🎯 **Event Started** - Calendar event just started
- ✅ **Event Ended** - Calendar event completed
- 🆕 **New Event Created** - New calendar event added
- 📝 **Event Updated** - Existing event modified

### Actions
- Create calendar event
- Update event
- Send meeting reminder
- Block focus time
- Check schedule availability

### Use Cases
- Start study session when "Study Time" event begins
- Block distractions during meetings
- Auto-create focus blocks based on assignment due dates
- Send reminders before class/study sessions

---

## 🎓 Canvas LMS

**Authentication:** API Token

### Triggers
- 📚 **New Assignment** - Assignment posted in course
- ⚠️ **Assignment Due Soon** - Deadline approaching
- 📊 **Grade Posted** - New grade available
- 📣 **Announcement Posted** - Course announcement
- 📝 **Assignment Submitted** - You submitted work
- 💬 **New Discussion Post** - Forum activity

### Actions
- Check upcoming assignments
- Fetch assignment details
- Download course materials
- Get grade summary
- Mark assignment as reviewed

### Use Cases
- Auto-create tasks for new assignments
- Send notifications when grades are posted
- Daily digest of upcoming deadlines
- Sync Canvas deadlines to Google Calendar

---

## 📁 Google Drive

**Authentication:** OAuth 2.0  
**Scopes:** `drive.readonly`

### Triggers
- 📄 **File Created** - New file in monitored folder
- ✏️ **File Modified** - File updated
- 📁 **Folder Changes** - Folder content changed
- 🔗 **File Shared** - File shared with you

### Actions
- List files in folder
- Get file metadata
- Download file
- Check sharing permissions
- Search files by name/type

### Use Cases
- Auto-organize study materials
- Backup created flashcard decks
- Notification when professor shares notes
- Track collaborative doc changes

---

## 🎵 Spotify

**Authentication:** OAuth 2.0  
**Scopes:** `user-read-playback-state`, `user-modify-playback-state`

### Triggers
- ▶️ **Playback Started** - Music started playing
- ⏸️ **Playback Paused** - Music paused
- ⏭️ **Track Changed** - New song started
- 📻 **Playlist Changed** - Switched playlists

### Actions
- Play/pause playback
- Next/previous track
- Set volume
- Play specific playlist
- Start focus music
- Resume playback

### Use Cases
- Auto-play focus playlist when study session starts
- Pause music during breaks
- Play energizing music for tough subjects
- Silence Spotify during virtual classes

---

## 🏠 Home Assistant

**Authentication:** Long-Lived Access Token

### Triggers
- 💡 **Light State Changed** - Smart light on/off
- 🌡️ **Sensor Updated** - Temperature, motion, etc.
- 🔔 **Automation Triggered** - HA automation ran
- 📍 **Location Changed** - Home/away status
- 🔐 **Door Opened/Closed** - Door sensor triggered

### Actions
- Turn lights on/off
- Set light brightness/color
- Control switches
- Trigger scenes
- Read sensor values
- Run HA automations

### Use Cases
- Dim lights when study session starts
- Red lights for focus mode, green for breaks
- Turn off distractions (TV) during study time
- Restore normal lighting after session
- Motion-activated study room lighting

---

## 📓 Notion

**Authentication:** Internal Integration Token

### Triggers
- 📝 **Page Created** - New page in database
- ✏️ **Page Updated** - Page content changed
- ✅ **Task Completed** - Checkbox checked
- 🏷️ **Tag Added** - Page tagged

### Actions
- Create page
- Update page properties
- Add content to page
- Query database
- Create task
- Update task status

### Use Cases
- Two-way sync: Notion tasks ↔ FocusDeck tasks
- Create study notes page after session
- Track study hours in Notion dashboard
- Auto-tag pages with session data

---

> Note: External task managers (e.g., Todoist) were originally considered here, but since FocusDeck is intended to replace them as the primary task system, deep Todoist integration is not a current priority.

## 💬 Slack

**Authentication:** Bot User OAuth Token

### Triggers
- 💬 **Message Received** - Message in monitored channel
- 📣 **Mention** - You were mentioned
- ⭐ **Reaction Added** - Someone reacted to message

### Actions
- Send message to channel
- Send DM
- Update status
- Set custom status
- Post to webhook
- Upload file

### Use Cases
- Send "Do Not Disturb - Studying" status
- Post study session summary to team channel
- Get notified of urgent mentions
- Share completed tasks with study group
- Daily standup automation

---

## 🎮 Discord

**Authentication:** Webhook URL

### Actions
- Send message to channel
- Send embed (rich message)
- Post notification
- Update webhook

### Use Cases
- Study session start/end notifications
- Task completion announcements
- Study stats summary
- Assignment reminders to server
- Study group coordination

---

## 🤖 Google Generative AI (Gemini)

**Authentication:** API Key

### Actions
- Generate text
- Summarize content
- Answer questions
- Analyze text
- Create study aids
- Generate flashcards
- Explain concepts

### Use Cases
- Auto-generate flashcards from notes
- Summarize lecture recordings
- Create study guides
- Explain difficult concepts
- Quiz generation
- Essay outline assistance

---

## 🔗 IFTTT

**Authentication:** Webhook Key

### Actions
- Trigger applet
- Send custom event
- Pass data to workflows

### Use Cases
- Connect to 600+ services
- Automate smart home beyond HA
- Social media posting
- Email notifications
- SMS alerts
- Cross-platform automation

---

## ⚡ Zapier

**Authentication:** Webhook URL

### Actions
- Trigger Zap
- Send workflow data
- Multi-step automation

### Use Cases
- Connect to 5,000+ apps
- CRM integration
- Email automation
- Database updates
- Advanced workflows
- Business tools integration

---

## 💡 Philips Hue

**Authentication:** Bridge IP + Auto-pairing

### Triggers
- 💡 **Light State Changed**
- 🌈 **Scene Activated**
- 🔆 **Brightness Changed**

### Actions
- Turn lights on/off
- Set brightness (0-100%)
- Set color (RGB)
- Activate scene
- Flash lights (alert)
- Transition effects

### Use Cases
- Focus lighting during study sessions
- Pomodoro visual timer (color changes)
- Red light = deep focus, blue = break
- Flash lights for task deadlines
- Circadian rhythm lighting

---

## 🎧 Apple Music

**Authentication:** Developer Token (requires Apple Developer Program)

### Status
⚠️ **In Development** - Requires $99/year Apple Developer membership

### Planned Actions
- Play/pause
- Next/previous track
- Play playlist
- Control volume
- Get now playing

### Recommendation
Use Spotify integration as alternative (same features, easier setup)

---

## Automation Examples

### Example 1: Complete Study Workflow
**Trigger:** Study session starts (FocusDeck)  
**Actions:**
1. Set Slack status to "🎯 Deep Focus"
2. Play focus playlist (Spotify)
3. Dim lights to 40%, warm white (Philips Hue)
4. Block calendar for next 25 minutes (Google Calendar)
5. Pause all notifications

### Example 2: Assignment Alert System
**Trigger:** New assignment posted (Canvas)  
**Actions:**
1. Create task in FocusDeck
2. Add to Todoist with Canvas link
3. Create calendar event for due date
4. Send Discord notification to study group
5. Log to Notion study tracker

### Example 3: Smart Break Time
**Trigger:** Break started (FocusDeck)  
**Actions:**
1. Pause Spotify
2. Set lights to bright, cool white (Hue)
3. Update Slack status to "☕ On Break"
4. Show stretch reminder notification
5. Start 5-minute break timer

### Example 4: Daily Study Summary
**Trigger:** 9:00 PM every day (FocusDeck)  
**Actions:**
1. Calculate total study time
2. List completed tasks
3. Generate summary with Gemini AI
4. Post to Discord channel
5. Create Notion journal entry
6. Send Slack DM to yourself

---

## Best Practices

### 🎯 Start Simple
Begin with 1-2 service integrations and gradually expand. Master basic automations before building complex workflows.

### 🔒 Security
- Never share API keys or tokens
- Use separate tokens for different purposes
- Regularly rotate credentials
- Review connected services periodically

### ⚡ Performance
- Avoid creating infinite loops
- Use rate limiting for API-heavy automations
- Test automations before enabling
- Monitor automation execution logs

### 📊 Effectiveness
- Track which automations you actually use
- Remove unused integrations
- Iterate based on what works
- Keep automations focused and specific

---

## Service Comparison

| Service | Auth Type | Difficulty | Features | Best For |
|---------|-----------|------------|----------|----------|
| FocusDeck | Built-in | ⭐ Easy | Core features | Everyone |
| Home Assistant | Token | ⭐⭐ Moderate | Smart home | HA users |
| Canvas | Token | ⭐ Easy | LMS integration | Students |
| Todoist | Token | ⭐ Easy | Task management | Task tracking |
| Slack | OAuth/Token | ⭐⭐ Moderate | Team communication | Groups |
| Discord | Webhook | ⭐ Easy | Notifications | Communities |
| Notion | Token | ⭐⭐ Moderate | Note-taking | Organization |
| Google Calendar | OAuth | ⭐⭐⭐ Advanced | Schedule management | Time blocking |
| Google Drive | OAuth | ⭐⭐⭐ Advanced | File management | Material organization |
| Spotify | OAuth | ⭐⭐⭐ Advanced | Music control | Focus music |
| Gemini AI | API Key | ⭐⭐ Moderate | AI assistance | Content generation |
| Philips Hue | Bridge IP | ⭐⭐ Moderate | Lighting | Ambiance |
| IFTTT | Webhook | ⭐⭐ Moderate | 600+ services | Broad integration |
| Zapier | Webhook | ⭐⭐ Moderate | 5000+ apps | Business tools |

---

## Getting Help

### Setup Issues
1. Check the setup guide for your service
2. Verify credentials are correct
3. Test with service's official tools first
4. Check service status/API limits

### Automation Not Working
1. Check automation logs
2. Verify service is connected
3. Test trigger manually
4. Check action permissions

### Common Problems

**"OAuth not configured"** → Enter Client ID/Secret in service setup  
**"Token expired"** → Reconnect the service  
**"Rate limited"** → Reduce automation frequency  
**"Permission denied"** → Check service scopes/permissions  
**"Webhook failed"** → Verify webhook URL is correct

---

## Roadmap

### Coming Soon
- [ ] Microsoft To Do integration
- [ ] Trello board automation
- [ ] GitHub commit tracking
- [ ] Google Tasks sync
- [ ] Apple Calendar support
- [ ] Amazon Alexa routines
- [ ] YouTube study music control

### Under Consideration
- [ ] Focus@Will integration
- [ ] Forest app integration
- [ ] RescueTime tracking
- [ ] Habitica gamification
- [ ] Anki flashcard sync
- [ ] Obsidian vault integration

---

*Last Updated: November 1, 2025*
