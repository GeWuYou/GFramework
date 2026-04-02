const test = require("node:test");
const assert = require("node:assert/strict");
const {
    applyFormUpdates,
    applyScalarUpdates,
    createSampleConfigYaml,
    extractYamlComments,
    getEditableSchemaFields,
    parseBatchArrayValue,
    parseSchemaContent,
    parseTopLevelYaml,
    validateParsedConfig
} = require("../src/configValidation");

test("parseSchemaContent should capture nested objects and object-array metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["id", "reward", "phases"],
          "properties": {
            "id": {
              "type": "integer",
              "title": "Monster Id",
              "description": "Primary monster key.",
              "default": 1
            },
            "reward": {
              "type": "object",
              "required": ["gold"],
              "properties": {
                "gold": {
                  "type": "integer",
                  "default": 10
                },
                "currency": {
                  "type": "string",
                  "enum": ["coin", "gem"]
                }
              }
            },
            "phases": {
              "type": "array",
              "description": "Encounter phases.",
              "items": {
                "type": "object",
                "required": ["wave"],
                "properties": {
                  "wave": { "type": "integer" },
                  "monsterId": { "type": "string" }
                }
              }
            }
          }
        }
    `);

    assert.equal(schema.type, "object");
    assert.deepEqual(schema.required, ["id", "reward", "phases"]);
    assert.equal(schema.properties.id.defaultValue, "1");
    assert.equal(schema.properties.reward.type, "object");
    assert.deepEqual(schema.properties.reward.required, ["gold"]);
    assert.equal(schema.properties.reward.properties.currency.enumValues[1], "gem");
    assert.equal(schema.properties.phases.type, "array");
    assert.equal(schema.properties.phases.items.type, "object");
    assert.equal(schema.properties.phases.items.properties.wave.type, "integer");
});

test("parseTopLevelYaml should parse nested mappings and object arrays", () => {
    const yaml = parseTopLevelYaml(`
id: 1
reward:
  gold: 10
  currency: coin
phases:
  -
    wave: 1
    monsterId: slime
`);

    assert.equal(yaml.kind, "object");
    assert.equal(yaml.map.get("reward").kind, "object");
    assert.equal(yaml.map.get("reward").map.get("currency").value, "coin");
    assert.equal(yaml.map.get("phases").kind, "array");
    assert.equal(yaml.map.get("phases").items[0].kind, "object");
    assert.equal(yaml.map.get("phases").items[0].map.get("wave").value, "1");
});

test("parseTopLevelYaml should keep complex mapping keys", () => {
    const yaml = parseTopLevelYaml(`
my-key: slime
"complex key": value
root:
  item.id: potion
`);

    assert.equal(yaml.kind, "object");
    assert.equal(yaml.map.get("my-key").value, "slime");
    assert.equal(yaml.map.get("complex key").value, "value");
    assert.equal(yaml.map.get("root").map.get("item.id").value, "potion");
});

test("validateParsedConfig should report missing and unknown nested properties", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["reward"],
          "properties": {
            "reward": {
              "type": "object",
              "required": ["gold", "currency"],
              "properties": {
                "gold": { "type": "integer" },
                "currency": { "type": "string" }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
  rarity: epic
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /reward\.currency/u);
    assert.match(diagnostics[1].message, /reward\.rarity/u);
});

test("validateParsedConfig should report object-array item issues", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "phases": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["wave", "monsterId"],
                "properties": {
                  "wave": { "type": "integer" },
                  "monsterId": { "type": "string" }
                }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
phases:
  -
    wave: 1
    hpScale: 1.5
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /phases\[0\]\.monsterId/u);
    assert.match(diagnostics[1].message, /phases\[0\]\.hpScale/u);
});

test("validateParsedConfig should report deep enum mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "properties": {
                "currency": {
                  "type": "string",
                  "enum": ["coin", "gem"]
                }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  currency: ticket
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /coin, gem/u);
});

test("validateParsedConfig should localize diagnostics when Chinese UI is requested", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["name"],
          "properties": {
            "name": { "type": "string" }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
id: 1
`);

    const diagnostics = validateParsedConfig(schema, yaml, {isChinese: true});

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /缺少必填属性/u);
    assert.match(diagnostics[1].message, /未在匹配的 schema 中声明/u);
});

test("applyFormUpdates should update nested scalar and scalar-array paths", () => {
    const updated = applyFormUpdates(
        [
            "id: 1",
            "reward:",
            "  gold: 10",
            "phases:",
            "  -",
            "    wave: 1"
        ].join("\n"),
        {
            scalars: {
                "reward.currency": "coin",
                name: "Slime"
            },
            arrays: {
                dropItems: ["potion", "hi potion"]
            }
        });

    assert.match(updated, /^name: Slime$/mu);
    assert.match(updated, /^reward:$/mu);
    assert.match(updated, /^  currency: coin$/mu);
    assert.match(updated, /^dropItems:$/mu);
    assert.match(updated, /^  - potion$/mu);
    assert.match(updated, /^  - hi potion$/mu);
    assert.match(updated, /^phases:$/mu);
});

test("applyFormUpdates should rewrite object-array items from structured form payloads", () => {
    const updated = applyFormUpdates(
        [
            "id: 1",
            "name: Slime",
            "phases:",
            "  -",
            "    wave: 1",
            "    monsterId: slime"
        ].join("\n"),
        {
            objectArrays: {
                phases: [
                    {
                        wave: "1",
                        monsterId: "slime",
                        tags: ["starter", "melee"],
                        reward: {
                            gold: "10",
                            currency: "coin"
                        }
                    },
                    {
                        wave: "2",
                        monsterId: "goblin"
                    }
                ]
            }
        });

    assert.match(updated, /^id: 1$/mu);
    assert.match(updated, /^name: Slime$/mu);
    assert.match(updated, /^phases:$/mu);
    assert.match(updated, /^  -$/mu);
    assert.match(updated, /^    wave: 1$/mu);
    assert.match(updated, /^    monsterId: slime$/mu);
    assert.match(updated, /^    tags:$/mu);
    assert.match(updated, /^      - starter$/mu);
    assert.match(updated, /^      - melee$/mu);
    assert.match(updated, /^    reward:$/mu);
    assert.match(updated, /^      gold: 10$/mu);
    assert.match(updated, /^      currency: coin$/mu);
    assert.match(updated, /^    monsterId: goblin$/mu);
});

test("applyFormUpdates should clear object arrays when the form removes all items", () => {
    const updated = applyFormUpdates(
        [
            "id: 1",
            "phases:",
            "  -",
            "    wave: 1",
            "    monsterId: slime"
        ].join("\n"),
        {
            objectArrays: {
                phases: []
            }
        });

    assert.equal(updated, [
        "id: 1",
        "phases: []"
    ].join("\n"));
});

test("extractYamlComments should map nested comments to logical paths", () => {
    const comments = extractYamlComments(`
# Monster display name
name: Slime
stats:
  # Current hp value
  hp: 10
skills:
  # First skill entry
  -
    # Skill id note
    id: jump
`);

    assert.equal(comments.name, "Monster display name");
    assert.equal(comments["stats.hp"], "Current hp value");
    assert.equal(comments["skills[0]"], "First skill entry");
    assert.equal(comments["skills[0].id"], "Skill id note");
});

test("extractYamlComments should keep comments for complex YAML keys", () => {
    const comments = extractYamlComments(`
# Dashed key comment
my-key: Slime
# Quoted key comment
"complex key": value
root:
  # Dotted key comment
  item.id: potion
`);

    assert.equal(comments["my-key"], "Dashed key comment");
    assert.equal(comments["complex key"], "Quoted key comment");
    assert.equal(comments["root.item.id"], "Dotted key comment");
});

test("applyFormUpdates should preserve and update YAML comments", () => {
    const updated = applyFormUpdates(
        [
            "# Monster display name",
            "name: Slime",
            "stats:",
            "  # Current hp value",
            "  hp: 10"
        ].join("\n"),
        {
            scalars: {
                name: "Slime King"
            },
            comments: {
                name: "Localized display name",
                "stats.hp": "Health points after rebalance"
            }
        });

    assert.match(updated, /^# Localized display name$/mu);
    assert.match(updated, /^name: Slime King$/mu);
    assert.match(updated, /^  # Health points after rebalance$/mu);
    assert.match(updated, /^  hp: 10$/mu);
});

test("createSampleConfigYaml should bootstrap comments and placeholder values from schema", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "description": "Monster display name."
            },
            "rarity": {
              "type": "string",
              "description": "Monster rarity.",
              "enum": ["common", "rare"]
            },
            "skills": {
              "type": "array",
              "description": "Skill entries.",
              "items": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "string",
                    "description": "Skill id."
                  }
                }
              }
            }
          }
        }
    `);

    const sample = createSampleConfigYaml(schema);

    assert.match(sample, /^# Monster display name\.$/mu);
    assert.match(sample, /^name: example$/mu);
    assert.match(sample, /^# Monster rarity\.$/mu);
    assert.match(sample, /^rarity: common$/mu);
    assert.match(sample, /^# Skill entries\.$/mu);
    assert.match(sample, /^skills:$/mu);
    assert.match(sample, /^  -$/mu);
    assert.match(sample, /^    # Skill id\.$/mu);
    assert.match(sample, /^    id: example$/mu);
});

test("applyScalarUpdates should preserve the scalar-only compatibility wrapper", () => {
    const updated = applyScalarUpdates(
        [
            "id: 1",
            "name: Slime"
        ].join("\n"),
        {
            name: "Goblin",
            hp: "25"
        });

    assert.match(updated, /^name: Goblin$/mu);
    assert.match(updated, /^hp: 25$/mu);
});

test("getEditableSchemaFields should keep batch editing limited to top-level scalar and scalar-array properties", () => {
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
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" }
              }
            },
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
              "items": {
                "type": "object",
                "properties": {
                  "x": { "type": "integer" }
                }
              }
            }
          }
        }
    `);

    assert.deepEqual(getEditableSchemaFields(schema), [
        {
            key: "dropItems",
            path: "dropItems",
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
            path: "id",
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
            path: "name",
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

test("parseBatchArrayValue should keep comma-separated batch editing behavior", () => {
    assert.deepEqual(parseBatchArrayValue(" potion, bomb ,  ,elixir "), ["potion", "bomb", "elixir"]);
});
