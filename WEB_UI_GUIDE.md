# FocusDeck Server Web UI Guide

## Overview

The FocusDeck server now includes a beautiful, user-friendly web-based admin panel! No more command-line tools or API calls - manage everything from your browser.

## Accessing the Web UI

Once your server is running, simply open a web browser and navigate to:

```
http://your-server-ip:5000
```

Or if you've set up a domain:

```
http://your-domain.com
```

## Features

### 🎯 Dashboard Tab
- **Real-time Statistics**: See total decks, cards, and users at a glance
- **Server Status**: Live indicator showing server is online
- **Quick Overview**: Get started with helpful tips

### 📚 Decks Tab
Manage all your flashcard decks:
- ➕ **Create New Deck**: Add decks with custom names and cards
- ✏️ **Edit Decks**: Modify existing decks and their cards
- 🗑️ **Delete Decks**: Remove decks you no longer need
- 👁️ **View Cards**: Expandable card lists for each deck
- 🔄 **Refresh**: Update the deck list

#### Creating a Deck
1. Click "➕ Create New Deck"
2. Enter a deck name
3. Add cards (one per line)
4. Click "💾 Save Deck"

#### Editing a Deck
1. Find the deck you want to edit
2. Click "✏️ Edit"
3. Modify the name or cards
4. Click "💾 Save Deck"

### ⚙️ Configuration Tab
Server settings and data management:

**Server Settings:**
- Port configuration
- Environment selection (Development/Production)
- Maximum decks per user limit

**Data Management:**
- 📥 **Export All Data**: Download a JSON backup of all your decks
- 🗑️ **Clear All Data**: Remove all decks (with double confirmation)

**Security:**
- Enable/disable API authentication
- View security warnings and recommendations

### 📖 API Info Tab
Complete API documentation with examples:
- **GET /api/decks** - Retrieve all decks
- **GET /api/decks/{id}** - Get specific deck
- **POST /api/decks** - Create new deck
- **PUT /api/decks/{id}** - Update deck
- **DELETE /api/decks/{id}** - Delete deck

Each endpoint includes:
- HTTP method badge
- Description
- Copy-paste curl examples

## UI Features

### Modern Design
- 🎨 Beautiful gradient purple theme
- 📱 Fully responsive (works on mobile, tablet, desktop)
- 🌊 Smooth animations and transitions
- 💫 Glass-morphism effects

### User Experience
- ✅ Success/error notifications
- 🔔 Confirmation dialogs for destructive actions
- 📊 Visual statistics cards
- 🎯 Intuitive navigation tabs

### Accessibility
- Clean, readable fonts
- High contrast colors
- Clear button labels with emojis
- Keyboard-friendly navigation

## Keyboard Shortcuts

- **Esc**: Close any open modal
- **Tab**: Navigate between form fields
- **Enter**: Submit forms

## Mobile Support

The web UI is fully responsive and works great on:
- 📱 Smartphones (iOS, Android)
- 📱 Tablets (iPad, Android tablets)
- 💻 Laptops and desktops
- 🖥️ Large displays

## Browser Compatibility

Works in all modern browsers:
- ✅ Chrome/Edge (Chromium)
- ✅ Firefox
- ✅ Safari
- ✅ Opera

## Tips & Tricks

### Quick Deck Creation
Type or paste multiple cards at once, one per line:
```
What is the capital of France?|Paris
What is 2+2?|4
Who wrote Hamlet?|Shakespeare
```

### Export Regular Backups
Use the "📥 Export All Data" button to download JSON backups of your data. This is especially important since the server currently uses in-memory storage.

### Use the API Info Tab
Copy the curl examples to test endpoints or integrate with other tools.

### Bulk Delete
The configuration tab lets you clear all data at once - but be careful! This action is permanent (with double confirmation).

## Technical Details

### Stack
- **Frontend**: Pure HTML, CSS, and JavaScript (no frameworks)
- **API**: ASP.NET Core REST API
- **Data Format**: JSON

### Zero Dependencies
The web UI has:
- ❌ No npm packages
- ❌ No build process
- ❌ No external libraries
- ✅ Just vanilla JavaScript!

This means:
- ⚡ Lightning fast load times
- 🔒 No security vulnerabilities from dependencies
- 🎯 Simple to understand and modify

### Storage
Currently uses in-memory storage. Data is lost when the server restarts. Consider adding a database for production use (see the main setup guide).

## Future Enhancements

Potential features for future versions:
- 🔐 User authentication and login
- 👥 Multi-user support
- 📊 Analytics and charts
- 🗄️ Database integration
- 🔍 Search and filter
- 📤 Import from file
- 🎨 Theme customization
- 📱 Progressive Web App (PWA) support
- 🌙 Dark mode

## Security Notice

⚠️ **IMPORTANT**: The current setup has NO authentication. Anyone who can access your server can view and modify data. For production use:

1. Enable authentication (see Configuration tab)
2. Use HTTPS (see setup guide)
3. Set up a firewall
4. Use strong passwords
5. Consider adding rate limiting

## Troubleshooting

### UI Not Loading
```bash
# Check if server is running
sudo systemctl status focusdeck

# Check if files exist
ls ~/focusdeck-server/wwwroot/

# Check server logs
sudo journalctl -u focusdeck -n 50
```

### Can't Create Decks
1. Check browser console for errors (F12)
2. Verify API is accessible: `http://your-server:5000/api/decks`
3. Check CORS settings in server logs

### Styles Not Loading
1. Hard refresh: Ctrl+F5 (Windows) or Cmd+Shift+R (Mac)
2. Clear browser cache
3. Check network tab in browser DevTools

## Support

For issues or questions:
1. Check the main [LINUX_SERVER_SETUP.md](LINUX_SERVER_SETUP.md) guide
2. Review server logs: `sudo journalctl -u focusdeck -f`
3. Open an issue on GitHub

## Screenshots

### Dashboard
Beautiful overview with statistics and status indicators.

### Decks Management
Intuitive card-based interface for managing flashcard decks.

### Configuration
Easy-to-use settings page for server configuration.

### API Documentation
Complete API reference with copy-paste examples.

---

Enjoy your new web UI! 🎉
