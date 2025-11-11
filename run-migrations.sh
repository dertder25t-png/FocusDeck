#!/bin/bash
set -e

echo "🔧 FocusDeck Database Migration"
echo "================================"
echo ""

# Stop the service
echo "Stopping FocusDeck service..."
sudo systemctl stop focusdeck
sleep 2
echo "✅ Service stopped"
echo ""

# Backup the current database
echo "Backing up database..."
DB_BACKUP="/home/focusdeck/FocusDeck/data/focusdeck.db.backup-$(date +%Y%m%d-%H%M%S)"
sudo cp /home/focusDeck/FocusDeck/data/focusdeck.db "$DB_BACKUP"
echo "✅ Database backed up to: $DB_BACKUP"
echo ""

# Remove WAL files (they may be causing issues)
echo "Cleaning WAL files..."
sudo rm -f /home/focusdeck/FocusDeck/data/focusdeck.db-shm
sudo rm -f /home/focusdeck/FocusDeck/data/focusdeck.db-wal
echo "✅ WAL files cleaned"
echo ""

# Remove old migrations database if it exists and start fresh
echo "Creating fresh database with new schema..."
sudo rm -f /home/focusdeck/FocusDeck/data/focusdeck.db
echo "✅ Database recreated"
echo ""

# Start the service (it will run migrations automatically)
echo "Starting FocusDeck service (will run migrations)..."
sudo systemctl start focusdeck
echo "✅ Service started"
echo ""

# Wait for migrations to complete
echo "Waiting for migrations to complete..."
sleep 10

# Check if service is still running
if sudo systemctl is-active --quiet focusdeck; then
  echo "✅ Service is running"
  echo ""
  echo "Testing endpoints..."
  curl -s http://localhost:5000/healthz
  echo ""
  echo "✅ Migration successful!"
else
  echo "❌ Service failed to start after migration"
  echo ""
  echo "Checking logs..."
  sudo journalctl -u focusdeck -n 50 --no-pager
  exit 1
fi
