// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

const test = require("node:test");
const assert = require("node:assert/strict");
const {
    createSampleConfigYaml,
    parseSchemaContent,
    parseTopLevelYaml,
    validateParsedConfig
} = require("../src/configValidation");

test("parseSchemaContent should capture object and array enum comparable metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" },
                "itemId": { "type": "string" }
              },
              "enum": [
                { "gold": 10, "itemId": "potion" }
              ]
            },
            "dropItemIds": {
              "type": "array",
              "items": { "type": "string" },
              "enum": [
                ["fire", "ice"]
              ]
            }
          }
        }
    `);

    assert.deepEqual(schema.properties.reward.enumDisplayValues, ["{\"gold\":10,\"itemId\":\"potion\"}"]);
    assert.match(schema.properties.reward.enumComparableValues[0], /^4:gold=/u);
    assert.deepEqual(schema.properties.dropItemIds.enumDisplayValues, ["[\"fire\",\"ice\"]"]);
    assert.equal(schema.properties.dropItemIds.enumComparableValues[0], "[13:string:4:fire,12:string:3:ice]");
});

test("validateParsedConfig should reject object values not declared in object enum", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["reward"],
          "properties": {
            "reward": {
              "type": "object",
              "required": ["gold", "itemId"],
              "properties": {
                "gold": { "type": "integer" },
                "itemId": { "type": "string" }
              },
              "enum": [
                { "gold": 10, "itemId": "potion" }
              ]
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
  itemId: elixir
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /reward/u);
    assert.match(diagnostics[0].message, /"itemId":"potion"/u);
});

test("validateParsedConfig should treat array enum candidates as order-sensitive", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["dropItemIds"],
          "properties": {
            "dropItemIds": {
              "type": "array",
              "items": { "type": "string" },
              "enum": [
                ["fire", "ice"]
              ]
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropItemIds:
  - ice
  - fire
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /dropItemIds/u);
    assert.match(diagnostics[0].message, /\["fire","ice"\]/u);
});

test("validateParsedConfig should not add parent object enumMismatch after child diagnostics", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["reward"],
          "properties": {
            "reward": {
              "type": "object",
              "required": ["gold", "itemId"],
              "properties": {
                "gold": { "type": "integer" },
                "itemId": { "type": "string" }
              },
              "enum": [
                { "gold": 10, "itemId": "potion" }
              ]
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: wrong
  itemId: potion
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /reward\.gold/u);
    assert.doesNotMatch(diagnostics[0].message, /must be one of|必须是以下值之一/u);
});

test("validateParsedConfig should not add parent array enumMismatch after item diagnostics", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["dropLevels"],
          "properties": {
            "dropLevels": {
              "type": "array",
              "items": { "type": "integer" },
              "enum": [
                [1, 2]
              ]
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropLevels:
  - 1
  - two
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /dropLevels\[1\]/u);
    assert.doesNotMatch(diagnostics[0].message, /must be one of|必须是以下值之一/u);
});

test("createSampleConfigYaml should reuse object and array enum payloads for valid samples", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["reward", "phases"],
          "properties": {
            "reward": {
              "type": "object",
              "required": ["gold", "itemId"],
              "properties": {
                "gold": { "type": "integer" },
                "itemId": { "type": "string" }
              },
              "enum": [
                { "gold": 10, "itemId": "potion" }
              ]
            },
            "phases": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["wave", "monsterId"],
                "properties": {
                  "wave": { "type": "integer" },
                  "monsterId": { "type": "string" }
                },
                "enum": [
                  { "wave": 1, "monsterId": "slime" }
                ]
              }
            }
          }
        }
    `);

    const sample = createSampleConfigYaml(schema);
    const yaml = parseTopLevelYaml(sample);
    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 0);
    assert.match(sample, /^reward:$/mu);
    assert.match(sample, /^  gold: 10$/mu);
    assert.match(sample, /^  itemId: potion$/mu);
    assert.match(sample, /^phases:$/mu);
    assert.match(sample, /^  -$/mu);
    assert.match(sample, /^    wave: 1$/mu);
    assert.match(sample, /^    monsterId: slime$/mu);
});

test("parseSchemaContent should reject empty enum arrays", () => {
    assert.throws(() => parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "rarity": {
              "type": "string",
              "enum": []
            }
          }
        }
    `), /must declare 'enum' with at least one value/u);
});
