const test = require("node:test");
const assert = require("node:assert/strict");
const {
    applyFormUpdates,
    applyScalarUpdates,
    getEditableSchemaFields,
    parseBatchArrayValue,
    parseSchemaContent,
    parseTopLevelYaml,
    validateParsedConfig
} = require("../src/configValidation");

test("parseSchemaContent should capture scalar and array property metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["id", "name"],
          "properties": {
            "id": {
              "type": "integer",
              "title": "Monster Id",
              "description": "Primary monster key.",
              "default": 1
            },
            "name": {
              "type": "string",
              "enum": ["Slime", "Goblin"]
            },
            "dropRates": {
              "type": "array",
              "description": "Drop rate list.",
              "items": {
                "type": "integer",
                "enum": [1, 2, 3]
              }
            }
          }
        }
    `);

    assert.deepEqual(schema.required, ["id", "name"]);
    assert.deepEqual(schema.properties, {
        id: {
            type: "integer",
            title: "Monster Id",
            description: "Primary monster key.",
            defaultValue: "1",
            enumValues: undefined,
            refTable: undefined
        },
        name: {
            type: "string",
            title: undefined,
            description: undefined,
            defaultValue: undefined,
            enumValues: ["Slime", "Goblin"],
            refTable: undefined
        },
        dropRates: {
            type: "array",
            itemType: "integer",
            title: undefined,
            description: "Drop rate list.",
            defaultValue: undefined,
            refTable: undefined,
            itemEnumValues: ["1", "2", "3"]
        }
    });
});

test("validateParsedConfig should report missing and unknown properties", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["id", "name"],
          "properties": {
            "id": { "type": "integer" },
            "name": { "type": "string" }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
id: 1
title: Slime
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.equal(diagnostics[0].severity, "error");
    assert.match(diagnostics[0].message, /name/u);
    assert.equal(diagnostics[1].severity, "error");
    assert.match(diagnostics[1].message, /title/u);
});

test("validateParsedConfig should report array item type mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "items": { "type": "integer" }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRates:
  - 1
  - potion
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.equal(diagnostics[0].severity, "error");
    assert.match(diagnostics[0].message, /dropRates/u);
});

test("validateParsedConfig should report scalar enum mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "rarity": {
              "type": "string",
              "enum": ["common", "rare"]
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
rarity: epic
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /common, rare/u);
});

test("validateParsedConfig should report array item enum mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "tags": {
              "type": "array",
              "items": {
                "type": "string",
                "enum": ["fire", "ice"]
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
tags:
  - fire
  - poison
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /fire, ice/u);
});

test("parseTopLevelYaml should classify nested mappings as object entries", () => {
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
name: Slime
`);

    assert.equal(yaml.entries.get("reward").kind, "object");
    assert.equal(yaml.entries.get("name").kind, "scalar");
});

test("applyScalarUpdates should update top-level scalars and append new keys", () => {
    const updated = applyScalarUpdates(
        [
            "id: 1",
            "name: Slime",
            "dropRates:",
            "  - 1"
        ].join("\n"),
        {
            name: "Goblin",
            hp: "25"
        });

    assert.match(updated, /^name: Goblin$/mu);
    assert.match(updated, /^hp: 25$/mu);
    assert.match(updated, /^  - 1$/mu);
});

test("applyFormUpdates should replace top-level scalar arrays and preserve unrelated content", () => {
    const updated = applyFormUpdates(
        [
            "id: 1",
            "name: Slime",
            "dropItems:",
            "  - potion",
            "  - slime_gel",
            "reward:",
            "  gold: 10"
        ].join("\n"),
        {
            scalars: {
                name: "Goblin"
            },
            arrays: {
                dropItems: ["bomb", "hi potion"]
            }
        });

    assert.match(updated, /^name: Goblin$/mu);
    assert.match(updated, /^dropItems:$/mu);
    assert.match(updated, /^  - bomb$/mu);
    assert.match(updated, /^  - hi potion$/mu);
    assert.match(updated, /^reward:$/mu);
    assert.match(updated, /^  gold: 10$/mu);
});

test("getEditableSchemaFields should expose only scalar and scalar-array properties", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["id", "dropItems"],
          "properties": {
            "id": { "type": "integer" },
            "name": {
              "type": "string",
              "title": "Monster Name",
              "description": "Display name."
            },
            "reward": { "type": "object" },
            "dropItems": {
              "type": "array",
              "description": "Drop ids.",
              "items": {
                "type": "string",
                "enum": ["potion", "bomb"]
              }
            },
            "waypoints": {
              "type": "array",
              "items": { "type": "object" }
            }
          }
        }
    `);

    assert.deepEqual(getEditableSchemaFields(schema), [
        {
            key: "dropItems",
            type: "array",
            itemType: "string",
            title: undefined,
            description: "Drop ids.",
            defaultValue: undefined,
            itemEnumValues: ["potion", "bomb"],
            refTable: undefined,
            inputKind: "array",
            required: true
        },
        {
            key: "id",
            type: "integer",
            title: undefined,
            description: undefined,
            defaultValue: undefined,
            enumValues: undefined,
            refTable: undefined,
            inputKind: "scalar",
            required: true
        },
        {
            key: "name",
            type: "string",
            title: "Monster Name",
            description: "Display name.",
            defaultValue: undefined,
            enumValues: undefined,
            refTable: undefined,
            inputKind: "scalar",
            required: false
        }
    ]);
});

test("parseBatchArrayValue should split comma-separated items and drop empty segments", () => {
    assert.deepEqual(parseBatchArrayValue(" potion, hi potion , ,bomb "), [
        "potion",
        "hi potion",
        "bomb"
    ]);
    assert.deepEqual(parseBatchArrayValue(""), []);
});
