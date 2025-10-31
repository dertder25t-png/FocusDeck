# FocusDeck Linux Server Setup Guide

This guide will help you deploy the FocusDeck.Server API on your Linux machine so your Windows app can sync data to it.

## Prerequisites

- A Linux server (Ubuntu 20.04+ recommended)
- SSH access to your server
- A domain name (optional, but recommended for HTTPS)
- .NET 9.0 SDK installed on your server

## Step 1: Install .NET 9.0 on Your Linux Server

SSH into your Linux server and run:

```bash
# Download and install .NET 9.0
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Add .NET to your PATH
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc
source ~/.bashrc

# Verify installation
dotnet --version
```

## Step 2: Clone Your Repository on the Server

```bash
# Install git if not already installed
sudo apt update
sudo apt install git -y

# Clone your FocusDeck repository
cd ~
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck
```

## Step 3: Publish the Server Application

```bash
# Navigate to the server project
cd src/FocusDeck.Server

# Publish ONLY the server project (not the entire solution)
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
```

This creates a self-contained deployment in `~/focusdeck-server` that includes everything needed to run.

## Step 4: Test the Server Locally

```bash
# Navigate to the published directory
cd ~/focusdeck-server

# Run the server
./FocusDeck.Server

# You should see output like:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
```

Press `Ctrl+C` to stop the server. If it works, proceed to the next step.

## Step 5: Create a Systemd Service (Run on Boot)

Create a service file so the server starts automatically:

```bash
sudo nano /etc/systemd/system/focusdeck.service
```

Paste this content (replace `yourusername` with your actual username):

```ini
[Unit]
Description=FocusDeck API Server
After=network.target

[Service]
Type=notify
WorkingDirectory=/home/yourusername/focusdeck-server
ExecStart=/home/yourusername/focusdeck-server/FocusDeck.Server
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=focusdeck
User=yourusername
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

Save and exit (Ctrl+X, then Y, then Enter).

Enable and start the service:

```bash
sudo systemctl daemon-reload
sudo systemctl enable focusdeck
sudo systemctl start focusdeck

# Check status
sudo systemctl status focusdeck
```

## Step 6: Install and Configure Nginx (Reverse Proxy)

Install Nginx:

```bash
sudo apt install nginx -y
```

Create an Nginx configuration:

```bash
sudo nano /etc/nginx/sites-available/focusdeck
```

Paste this content (replace `your-domain.com` with your actual domain or server IP):

```nginx
server {
    listen 80;
    server_name your-domain.com;  # Replace with your domain or IP address

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/focusdeck /etc/nginx/sites-enabled/
sudo nginx -t  # Test configuration
sudo systemctl reload nginx
```

## Step 7: Set Up HTTPS with Let's Encrypt (Recommended)

Install Certbot:

```bash
sudo apt install certbot python3-certbot-nginx -y
```

Get an SSL certificate (replace with your domain):

```bash
sudo certbot --nginx -d your-domain.com
```

Follow the prompts. Certbot will automatically configure Nginx for HTTPS.

## Step 8: Configure Firewall

```bash
# Allow HTTP and HTTPS
sudo ufw allow 'Nginx Full'

# Enable firewall if not already enabled
sudo ufw enable
```

## Step 9: Test the API

From any computer, open a web browser and navigate to:

```
http://your-domain.com/api/decks
```

You should see an empty JSON array: `[]`

## Step 10: Configure Your Windows App

1. Open your FocusDeck Windows app
2. Click the **Settings** button (gear icon)
3. Go to the **Sync** tab
4. Enter your server URL:
   - `http://your-domain.com` (if no HTTPS)
   - `https://your-domain.com` (if you set up SSL)
5. Click **Save Sync Settings**
6. Click the **Test Server** button (cloud icon) on the main dock

You should see a message showing that decks were successfully synced!

## Maintenance Commands

```bash
# View server logs
sudo journalctl -u focusdeck -f

# Restart the server
sudo systemctl restart focusdeck

# Stop the server
sudo systemctl stop focusdeck

# Check server status
sudo systemctl status focusdeck

# Update the server (after pushing new code to GitHub)
cd ~/FocusDeck
git pull
cd src/FocusDeck.Server
dotnet publish -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
sudo systemctl restart focusdeck
```

## Troubleshooting

### Server won't start
```bash
# Check logs for errors
sudo journalctl -u focusdeck -n 50
```

### Can't connect from Windows app
- Check firewall: `sudo ufw status`
- Verify Nginx is running: `sudo systemctl status nginx`
- Test locally on server: `curl http://localhost:5000/api/decks`

### HTTPS certificate issues
```bash
# Renew certificate manually
sudo certbot renew

# Check certificate status
sudo certbot certificates
```

## Security Recommendations

1. **Add Authentication**: The current setup has no authentication. For production, implement API keys or JWT tokens.
2. **Use HTTPS**: Always use HTTPS in production (covered in Step 7).
3. **Keep Updated**: Regularly update your server:
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```
4. **Backup Data**: When you add a database, regularly back it up.

## Next Steps

This setup uses in-memory storage (data is lost on restart). To persist data:

1. Add a database (SQLite, PostgreSQL, or MySQL)
2. Configure Entity Framework Core in the server project
3. Update the `DecksController` to use the database instead of the in-memory list

Would you like help implementing persistent storage with a database?
