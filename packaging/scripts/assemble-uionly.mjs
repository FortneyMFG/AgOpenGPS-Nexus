#!/usr/bin/env node
import fs from 'fs';
import path from 'path';
import child from 'child_process';

const [,, artifactsDir, outDir, version] = process.argv;
fs.mkdirSync(outDir, { recursive: true });

function extract(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  if (src.endsWith('.zip')) child.execSync(`unzip -q "${src}" -d "${dest}"`);
  else child.execSync(`tar -xzf "${src}" -C "${dest}"`);
}

const tryFind = (pattern) => {
  try { return child.execSync(`ls ${artifactsDir}/**/${pattern}`).toString().trim().split('\n')[0]; }
  catch { return null; }
};

// prefer linux-x64 content
let found = tryFind(`ui_v${version}_linux-x64.tar.gz`) || tryFind(`ui_v${version}_win-x64.zip`);
if (!found) throw new Error('UI artifact not found for UI-only bundle.');
extract(found, outDir);

// write release.json
fs.writeFileSync(path.join(outDir, 'release.json'), JSON.stringify({ name: 'Nexus UI', version }, null, 2));
