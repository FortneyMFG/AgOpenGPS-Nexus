#!/usr/bin/env node
import fs from 'fs';

const [,, bundleJsonPath] = process.argv;
const b = JSON.parse(fs.readFileSync(bundleJsonPath, 'utf8'));
const ver = process.env.GITHUB_REF_NAME?.replace(/^v/,'') || process.env.NEXUS_VERSION || '0.0.0';

const names = [];

// components
for (const [comp, cfg] of Object.entries(b.components || {})) {
  for (const rt of cfg.runtime) {
    const ext = rt.startsWith('win-') ? 'zip' : 'tar.gz';
    names.push(`${comp}_v${ver}_${rt}.${ext}`);
  }
}

// plugins
for (const p of (b.plugins || [])) {
  for (const rt of ['win-x64','linux-x64','linux-arm64']) {
    const ext = rt.startsWith('win-') ? 'zip' : 'tar.gz';
    names.push(`plugin:${p.id}_v${ver}_${rt}.${ext}`.replace('plugin:','Plugin-'));
  }
}

console.log(names.sort().join('\n'));
