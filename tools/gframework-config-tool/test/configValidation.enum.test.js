const test = require("node:test");
const assert = require("node:assert/strict");
const {
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
