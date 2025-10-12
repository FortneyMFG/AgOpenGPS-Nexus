# Appendix — DFU Catalog Schema (Status: draft)

This appendix documents machine-readable schemas for DFU catalogs and offline bundles. Schemas follow [JSON Schema Draft 2020-12](https://json-schema.org/) so vendors and community tooling can validate catalogs before distribution.

## 1. Catalog index (`catalog.schema.json`)
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://agopengps.org/schemas/dfu/catalog.json",
  "title": "AgOpenGPS DFU Catalog",
  "type": "object",
  "required": ["version", "publisher", "updated", "devices", "signing"],
  "properties": {
    "version": { "type": "integer", "const": 1 },
    "publisher": { "type": "string", "minLength": 1 },
    "updated": { "type": "string", "format": "date-time" },
    "defaultChannel": { "type": "string" },
    "devices": {
      "type": "array",
      "items": { "$ref": "#/$defs/device" },
      "minItems": 1
    },
    "mirrors": {
      "type": "array",
      "items": { "type": "string", "format": "uri" }
    },
    "signing": { "$ref": "#/$defs/signing" }
  },
  "$defs": {
    "signing": {
      "type": "object",
      "required": ["alg", "catalogSig"],
      "properties": {
        "alg": { "type": "string", "enum": ["ed25519"] },
        "catalogSig": { "type": "string", "pattern": "^[A-Za-z0-9+/=]{64,}$" },
        "publicKey": { "type": "string", "pattern": "^[A-Za-z0-9+/=]{32,}$" }
      },
      "additionalProperties": false
    },
    "device": {
      "type": "object",
      "required": ["vendor", "product", "variant", "mcu", "latest", "channels"],
      "properties": {
        "vendor": { "type": "string", "minLength": 1 },
        "product": { "type": "string", "minLength": 1 },
        "variant": { "type": "string", "minLength": 1 },
        "mcu": { "type": "string", "minLength": 1 },
        "bootloader": { "type": "string" },
        "latest": { "$ref": "#/$defs/channelPointer" },
        "channels": {
          "type": "object",
          "minProperties": 1,
          "additionalProperties": { "$ref": "#/$defs/channel" }
        },
        "compat": { "$ref": "#/$defs/compat" },
        "notes": { "type": "string" }
      },
      "additionalProperties": false
    },
    "channelPointer": {
      "type": "object",
      "required": ["channel", "version"],
      "properties": {
        "channel": { "type": "string", "minLength": 1 },
        "version": { "$ref": "#/$defs/semver" }
      },
      "additionalProperties": false
    },
    "channel": {
      "type": "object",
      "required": ["version", "assets"],
      "properties": {
        "version": { "$ref": "#/$defs/semver" },
        "assets": {
          "type": "array",
          "items": { "$ref": "#/$defs/asset" },
          "minItems": 1
        },
        "instructions": {
          "type": "array",
          "items": { "type": "string", "minLength": 1 }
        },
        "minCoreApi": { "type": "string", "pattern": "^\d+\.\d+(\.\d+)?$" },
        "notes": { "type": "string" },
        "published": { "type": "string", "format": "date-time" }
      },
      "additionalProperties": false
    },
    "asset": {
      "type": "object",
      "required": ["type", "url", "sha256"],
      "properties": {
        "type": { "type": "string", "enum": ["bin", "hex", "elf", "zip"] },
        "url": { "type": "string", "format": "uri" },
        "size": { "type": "integer", "minimum": 0 },
        "sha256": { "type": "string", "pattern": "^[a-fA-F0-9]{64}$" },
        "sig": { "type": "string", "pattern": "^[A-Za-z0-9+/=]{64,}$" },
        "contentType": { "type": "string" }
      },
      "additionalProperties": false
    },
    "compat": {
      "type": "object",
      "properties": {
        "hwMin": { "type": "string" },
        "hwMax": { "type": "string" },
        "requires": {
          "type": "array",
          "items": { "type": "string" }
        },
        "blocked": {
          "type": "array",
          "items": { "type": "string" }
        }
      },
      "additionalProperties": false
    },
    "semver": {
      "type": "string",
      "pattern": "^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"
    }
  },
  "additionalProperties": false
}
```

## 2. Offline bundle manifest (`bundle.schema.json`)
Offline bundles wrap a catalog subset plus firmware assets inside a portable archive.
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://agopengps.org/schemas/dfu/bundle.json",
  "title": "AgOpenGPS DFU Offline Bundle",
  "type": "object",
  "required": ["catalog", "assets"],
  "properties": {
    "catalog": { "$ref": "catalog.json" },
    "assets": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["path", "sha256"],
        "properties": {
          "path": { "type": "string", "pattern": "^[A-Za-z0-9._\\/-]+$" },
          "sha256": { "type": "string", "pattern": "^[a-fA-F0-9]{64}$" },
          "size": { "type": "integer", "minimum": 0 }
        },
        "additionalProperties": false
      }
    },
    "metadata": {
      "type": "object",
      "properties": {
        "created": { "type": "string", "format": "date-time" },
        "createdBy": { "type": "string" },
        "notes": { "type": "string" }
      },
      "additionalProperties": false
    }
  },
  "additionalProperties": false
}
```

## 3. Capability bitfield reference
Catalog `requires` entries can reference identity capabilities. Suggested bit assignments:

| Bit | Flag | Description |
|---|---|---|
| 0 | `ota` | Device supports authenticated OTA updates. |
| 1 | `canBoot` | Device can enter CAN bootloader mode. |
| 2 | `usbDfu` | Device exposes USB DFU interface. |
| 3 | `dualBank` | Device supports dual-bank firmware and rollback. |
| 4 | `powerTelemetry` | Device broadcasts voltage/current telemetry. |
| 5 | `rollback` | Device firmware advertises rollback command support. |
| 6 | `offlineBundle` | Device can ingest offline bundle assets. |
| 7 | reserved | Future expansion. |

Implementations should treat unknown bits as reserved and ignore them.
