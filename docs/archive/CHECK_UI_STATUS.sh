#!/bin/bash
echo ""
echo "FocusDeck UI Troubleshooting - Run this on Linux server"
echo ""
echo ""
echo "1  SERVICE STATUS"
sudo systemctl status focusdeck --no-pager
echo ""
echo "2  PORT LISTENING CHECK"
sudo netstat -tlnp | grep 5000 || echo " Port 5000 not listening!"
echo ""
echo "3  RECENT LOGS (last 20 lines)"
sudo journalctl -u focusdeck -n 20 --no-pager
echo ""
echo "4  wwwROOT FILES"
ls -lah /home/focusdeck/FocusDeck/publish/wwwroot/ 2>/dev/null || echo " wwwroot not found!"
echo ""
echo "5  HEALTH ENDPOINT TEST"
curl -s http://localhost:5000/v1/system/health
echo ""
echo ""
echo "6  ROOT UI TEST (first 50 lines)"
curl -s http://localhost:5000/ | head -50
echo ""
echo "7  FIREWALL CHECK"
sudo ufw status | grep 5000 || echo "  Port 5000 may not be open"
echo ""
echo ""
