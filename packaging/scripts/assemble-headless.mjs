#!/usr/bin/env node
import fs from 'fs';
import path from 'path';
import child from 'child_process';

const [,, artifactsDir, outDir, bundleJsonPath, version] = process.argv;
const b = JSON.parse(fs.readFileSync(bundleJsonPath,'utf8'));
fs.mkdirSync(outDir, { recursive: true });

function extract(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  if (src.endsWith('.zip')) child.execSync(`unzip -q "${src}" -d "${dest}"`);
  else child.execSync(`tar -xzf "${src}" -C "${dest}"`);
}

for (const [comp, cfg] of Object.entries(b.components)) {
  const compDir = path.join(outDir, comp);
  fs.mkdirSync(compDir, { recursive: true });
  for (const rt of cfg.runtime) {
    const name = `${comp}_v${version}_${rt}.tar.gz`;
    const found = child.execSync(`ls ${artifactsDir}/**/${name}`).toString().trim().split('\n')[0];
    extract(found, compDir);
  }
}

const pluginsDir = path.join(outDir, 'plugins');
fs.mkdirSync(pluginsDir, { recursive: true });
for (const p of (b.plugins || [])) {
  const name = `Plugin-${p.id}_v${version}_linux-arm64.tar.gz`;
  const found = child.execSync(`ls ${artifactsDir}/**/${name}`).toString().trim().split('\n')[0];
  extract(found, path.join(pluginsDir, p.id));
}

fs.writeFileSync(path.join(outDir, 'release.json'), JSON.stringify({ name: b.name, version }, null, 2));
