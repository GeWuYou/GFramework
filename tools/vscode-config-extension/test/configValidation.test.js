const test = require("node:test");
const assert = require("node:assert/strict");
const {
    applyFormUpdates,
    applyScalarUpdates,
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
            "id": { "type": "integer" },
            "name": { "type": "string" },
            "dropRates": {
              "type": "array",
              "items": { "type": "integer" }
            }
          }
        }
    `);

    assert.deepEqual(schema.required, ["id", "name"]);
    assert.deepEqual(schema.properties, {
        id: {type: "integer"},
        name: {type: "string"},
        dropRates: {type: "array", itemType: "integer"}
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
