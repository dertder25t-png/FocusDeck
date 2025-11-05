# FocusDeck Linux - Quick Reference Card

##  Installation (One Command)

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | sudo bash
```

**Duration**: 5-10 minutes | **Requirements**: Ubuntu 20.04+ / Debian 11+

---

##  Service Management

| Task | Command |
|------|---------|
| **Check Status** | `sudo systemctl status focusdeck` |
| **Start Service** | `sudo systemctl start focusdeck` |
| **Stop Service** | `sudo systemctl stop focusdeck` |
| **Restart Service** | `sudo systemctl restart focusdeck` |
| **View Live Logs** | `sudo journalctl -u focusdeck -f` |
| **Last 50 Logs** | `sudo journalctl -u focusdeck -n 50` |
| **Enable Auto-Start** | `sudo systemctl enable focusdeck` |
| **Disable Auto-Start** | `sudo systemctl disable focusdeck` |

---

##  Testing & Verification

```bash
# Check if service is running
sudo systemctl is-active focusdeck

# Test health endpoint
curl http://localhost:5000/v1/system/health | jq

# Check which port it's listening on
sudo netstat -tlnp | grep dotnet

# Check resource usage
ps aux | grep focusdeck
```

---

##  Important Paths

| Item | Path |
|------|------|
| **Application** | `/home/focusdeck/FocusDeck` |
| **Database** | `/home/focusdeck/FocusDeck/focusdeck.db` |
| **Config** | `/home/focusdeck/FocusDeck/appsettings.json` |
| **Service File** | `/etc/systemd/system/focusdeck.service` |
| **Logs** | Via `journalctl` (systemd) |

---

##  Access

| Interface | URL |
|-----------|-----|
| **Local** | `http://localhost:5000` |
| **Network** | `http://YOUR_SERVER_IP:5000` |
| **Health Check** | `http://localhost:5000/v1/system/health` |
| **API Docs** | `http://localhost:5000/swagger` |

---

##  Quick Troubleshooting

### Service won't start?
```bash
sudo journalctl -u focusdeck -n 100
```

### Port already in use?
```bash
sudo lsof -i :5000
```

### Permission denied?
```bash
sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck
```

### .NET not found?
```bash
dotnet --version
export PATH=$PATH:/usr/local/dotnet
```

---

##  Documentation

- **Installation**: `LINUX_INSTALL.md`
- **Overview**: `README.md`
- **Advanced**: `docs/CLOUDFLARE_DEPLOYMENT.md`
- **GitHub**: https://github.com/dertder25t-png/FocusDeck

---

**Last Updated**: November 5, 2025
**Version**: 1.0
