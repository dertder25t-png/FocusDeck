# FocusDeck UI Troubleshooting - Quick Reference

## You're trying to access: http://192.168.1.110:5000/

## Run on Linux Server (SSH):

### 1. Check Service Status
\\\ash
sudo systemctl status focusdeck
\\\
**Expected:** Should say "active (running)"
**If failed:** Run \sudo systemctl restart focusdeck\

### 2. Check Port 5000
\\\ash
sudo netstat -tlnp | grep 5000
\\\
**Expected:** Should show dotnet listening on 0.0.0.0:5000
**If nothing:** Service crashed. Check logs below.

### 3. Check Recent Logs (First Look)
\\\ash
sudo journalctl -u focusdeck -n 50 --no-pager
\\\
**Look for:** ERROR messages, crash info, or startup failures

### 4. Verify UI Files Exist
\\\ash
ls -la /home/focusdeck/FocusDeck/publish/wwwroot/
\\\
**Expected:** Should list index.html, app.js, styles.css
**If empty:** Files not published correctly

### 5. Test API Locally
\\\ash
curl http://localhost:5000/v1/system/health
\\\
**Expected:** JSON response like \{"ok":true,"time":"2025-11-05..."}\
**If fails:** Server crashed or not listening

### 6. Check Firewall
\\\ash
sudo ufw status
\\\
**If 5000 not listed:** Allow it with \sudo ufw allow 5000/tcp\

---

## Most Common Fixes

**Problem: Connection Refused (Cannot reach 192.168.1.110:5000)**
\\\ash
# 1. Check if service is running
sudo systemctl restart focusdeck
sleep 2

# 2. Verify it started
sudo systemctl status focusdeck

# 3. Allow firewall
sudo ufw allow 5000/tcp && sudo ufw reload

# 4. Test locally first
curl http://localhost:5000/v1/system/health
\\\

**Problem: Blank Page / 404 Error**
\\\ash
# 1. Check if UI files exist
ls -la /home/focusdeck/FocusDeck/publish/wwwroot/index.html

# 2. If missing, republish
cd /home/focusdeck/FocusDeck
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj \
  -c Release -o ./publish --no-build

# 3. Check permissions
sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck/publish

# 4. Restart service
sudo systemctl restart focusdeck
\\\

**Problem: Service Keeps Crashing**
\\\ash
# 1. Check detailed logs
sudo journalctl -u focusdeck -n 100 --no-pager

# 2. Check system logs
sudo tail -50 /var/log/syslog

# 3. Try manual start to see error
cd /home/focusdeck/FocusDeck
dotnet FocusDeck.Server.dll
\\\

---

## Still Not Working?

Share output of these commands:
1. \sudo systemctl status focusdeck\
2. \sudo netstat -tlnp | grep 5000\
3. \sudo journalctl -u focusdeck -n 50\
4. \curl http://localhost:5000/v1/system/health\
5. \ls -la /home/focusdeck/FocusDeck/publish/wwwroot/\

I'll diagnose the exact problem!
