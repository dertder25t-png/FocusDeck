#!/usr/bin/env node
/**
 * Post-build script to copy built SPA assets from dist/ to the .NET server's wwwroot/
 * This ensures the C# server always serves the latest build
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const sourceDir = path.join(__dirname, '../dist');

// Primary targets to update. The running server may serve files from the working-directory wwwroot
// (e.g. /home/focusdeck/FocusDeck/wwwroot) while the repo's server folder is at
// src/FocusDeck.Server/wwwroot. We'll update both if they exist.
const possibleTargets = [
  path.join(__dirname, '../../FocusDeck.Server/wwwroot'),
  '/home/focusdeck/FocusDeck/wwwroot',
  '/home/focusdeck/FocusDeck/publish/wwwroot'
].map(p => path.normalize(p));

console.log(`📦 Copying SPA assets from ${sourceDir} to targets:`);
possibleTargets.forEach(t => console.log(`  - ${t}`));

function ensureDirectoryExists(dirPath) {
  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
  }
}

function copyRecursiveSync(src, dest) {
  ensureDirectoryExists(dest);
  const files = fs.readdirSync(src, { withFileTypes: true });

  files.forEach(file => {
    const srcPath = path.join(src, file.name);
    const destPath = path.join(dest, file.name);

    if (file.isDirectory()) {
      copyRecursiveSync(srcPath, destPath);
    } else {
      fs.copyFileSync(srcPath, destPath);
      console.log(`  ✓ Copied: ${path.relative(sourceDir, srcPath)} -> ${destPath}`);
    }
  });
}

try {
  if (!fs.existsSync(sourceDir)) {
    throw new Error(`Source directory not found: ${sourceDir}`);
  }

  const timestamp = Date.now();

  for (const targetDir of possibleTargets) {
    if (!targetDir) continue;

    try {
      if (fs.existsSync(targetDir)) {
        // Backup the existing target before replacing it (safe rollback)
        const backupDir = `${targetDir}.bak.${timestamp}`;
        fs.renameSync(targetDir, backupDir);
        console.log(`  ⚠ Backed up existing target: ${targetDir} -> ${backupDir}`);
      }

      // Recreate target and copy files
      ensureDirectoryExists(targetDir);
      copyRecursiveSync(sourceDir, targetDir);
      console.log(`  ✅ Updated target: ${targetDir}`);
    } catch (err) {
      console.warn(`  ❌ Skipping target ${targetDir}: ${err.message}`);
    }
  }

  console.log(`✅ Successfully copied SPA assets to available targets.`);
  
  // Cleanup old backups (retention)
  const retentionMs = 14 * 24 * 60 * 60 * 1000; // 14 days
  const now = Date.now();

  for (const targetDir of possibleTargets) {
    try {
      const parent = path.dirname(targetDir);
      const base = path.basename(targetDir);
      if (!fs.existsSync(parent)) continue;
      const children = fs.readdirSync(parent);
      for (const child of children) {
        if (!child.startsWith(base + '.bak.')) continue;
        const tsPart = child.substring((base + '.bak.').length);
        const ts = Number(tsPart);
        if (Number.isFinite(ts) && ts > 0 && now - ts > retentionMs) {
          const full = path.join(parent, child);
          // recursive rm
          fs.rmSync(full, { recursive: true, force: true });
          console.log(`  🧹 Removed old backup: ${full}`);
        }
      }
    } catch (err) {
      // non-fatal
    }
  }
} catch (error) {
  console.error(`❌ Error copying SPA assets: ${error.message}`);
  process.exit(1);
}
