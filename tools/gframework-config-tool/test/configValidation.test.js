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

test("parseSchemaContent should capture const metadata for scalar, object, array, integer, and boolean nodes", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "rarity": {
              "type": "string",
              "const": "common"
            },
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" },
                "items": {
                  "type": "array",
                  "items": {
                    "type": "string"
                  }
                }
              },
              "const": {
                "gold": 100,
                "items": [
                  "potion",
                  "sword"
                ]
              }
            },
            "tags": {
              "type": "array",
              "const": ["daily", "quest"],
              "items": {
                "type": "string"
              }
            },
            "maxAttempts": {
              "type": "integer",
              "const": 3
            },
            "allowRetry": {
              "type": "boolean",
              "const": true
            }
          }
        }
    `);
    const reorderedSchema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" },
                "items": {
                  "type": "array",
                  "items": {
                    "type": "string"
                  }
                }
              },
              "const": {
                "items": [
                  "potion",
                  "sword"
                ],
                "gold": 100
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.rarity.constValue, "common");
    assert.equal(schema.properties.rarity.constDisplayValue, "\"common\"");
    assert.ok(schema.properties.rarity.constComparableValue);

    assert.equal(schema.properties.reward.constValue, "{\"gold\":100,\"items\":[\"potion\",\"sword\"]}");
    assert.equal(schema.properties.reward.constDisplayValue, "{\"gold\":100,\"items\":[\"potion\",\"sword\"]}");
    assert.equal(
        schema.properties.reward.constComparableValue,
        reorderedSchema.properties.reward.constComparableValue);

    assert.equal(schema.properties.tags.constValue, "[\"daily\",\"quest\"]");
    assert.equal(schema.properties.tags.constDisplayValue, "[\"daily\",\"quest\"]");
    assert.ok(schema.properties.tags.constComparableValue);

    assert.equal(schema.properties.maxAttempts.constValue, "3");
    assert.equal(schema.properties.maxAttempts.constDisplayValue, "3");
    assert.equal(schema.properties.maxAttempts.constComparableValue, "integer:1:3");

    assert.equal(schema.properties.allowRetry.constValue, "true");
    assert.equal(schema.properties.allowRetry.constDisplayValue, "true");
    assert.equal(schema.properties.allowRetry.constComparableValue, "boolean:4:true");
});

test("parseSchemaContent should preserve empty-string const raw and display metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "const": ""
            }
          }
        }
    `);

    assert.equal(schema.properties.name.constValue, "");
    assert.equal(schema.properties.name.constDisplayValue, "\"\"");
});

test("parseSchemaContent should build object const comparable keys with ordinal property ordering", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "payload": {
              "type": "object",
              "properties": {
                "z": { "type": "integer" },
                "ä": { "type": "integer" }
              },
              "const": {
                "z": 1,
                "ä": 2
              }
            }
          }
        }
    `);

    assert.match(schema.properties.payload.constComparableValue, /^1:z=/u);
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

test("validateParsedConfig should report scalar const mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "rarity": {
              "type": "string",
              "const": "common"
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
rarity: rare
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /constant value "common"|固定值 "common"/u);
});

test("validateParsedConfig should accept scalar, object, array, integer, and boolean const matches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "rarity": {
              "type": "string",
              "const": "common"
            },
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" },
                "items": {
                  "type": "array",
                  "items": {
                    "type": "string"
                  }
                }
              },
              "const": {
                "gold": 100,
                "items": [
                  "potion",
                  "sword"
                ]
              }
            },
            "tags": {
              "type": "array",
              "items": {
                "type": "string"
              },
              "const": [
                "daily",
                "quest"
              ]
            },
            "maxAttempts": {
              "type": "integer",
              "const": 3
            },
            "allowRetry": {
              "type": "boolean",
              "const": true
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
rarity: common
reward:
  gold: 100
  items:
    - potion
    - sword
tags:
  - daily
  - quest
maxAttempts: 3
allowRetry: true
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
});

test("validateParsedConfig should normalize object const comparisons but keep array const order", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" },
                "items": {
                  "type": "array",
                  "items": {
                    "type": "string"
                  }
                }
              },
              "const": {
                "gold": 100,
                "items": [
                  "potion",
                  "sword"
                ]
              }
            },
            "tags": {
              "type": "array",
              "items": {
                "type": "string"
              },
              "const": [
                "daily",
                "quest"
              ]
            }
          }
        }
    `);
    const normalizedYaml = parseTopLevelYaml(`
reward:
  items:
    - potion
    - sword
  gold: 100
tags:
  - daily
  - quest
`);
    const arrayOrderMismatchYaml = parseTopLevelYaml(`
reward:
  items:
    - potion
    - sword
  gold: 100
tags:
  - quest
  - daily
`);

    assert.deepEqual(validateParsedConfig(schema, normalizedYaml), []);

    const diagnostics = validateParsedConfig(schema, arrayOrderMismatchYaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /tags/u);
    assert.match(diagnostics[0].message, /constant value \["daily","quest"\]|固定值 \["daily","quest"\]/u);
});

test("validateParsedConfig should report object and array const mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" },
                "currency": { "type": "string" }
              },
              "const": {
                "gold": 10,
                "currency": "coin"
              }
            },
            "dropItemIds": {
              "type": "array",
              "const": ["potion", "gem"],
              "items": {
                "type": "string"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
  currency: gem
dropItemIds:
  - gem
  - potion
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /reward/u);
    assert.match(diagnostics[1].message, /dropItemIds/u);
});

test("validateParsedConfig should cover integer and boolean const scalar normalization and mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "maxAttempts": {
              "type": "integer",
              "const": 3
            },
            "allowRetry": {
              "type": "boolean",
              "const": true
            }
          }
        }
    `);
    const normalizedYaml = parseTopLevelYaml(`
maxAttempts: "3"
allowRetry: "true"
`);
    const integerMismatchYaml = parseTopLevelYaml(`
maxAttempts: 3.5
allowRetry: true
`);
    const booleanConstMismatchYaml = parseTopLevelYaml(`
maxAttempts: 3
allowRetry: false
`);
    const booleanTypeMismatchYaml = parseTopLevelYaml(`
maxAttempts: 3
allowRetry: 0
`);

    assert.deepEqual(validateParsedConfig(schema, normalizedYaml), []);

    const integerDiagnostics = validateParsedConfig(schema, integerMismatchYaml);

    assert.equal(integerDiagnostics.length, 1);
    assert.match(integerDiagnostics[0].message, /maxAttempts/u);
    assert.match(integerDiagnostics[0].message, /integer|整数/u);

    const booleanConstDiagnostics = validateParsedConfig(schema, booleanConstMismatchYaml);

    assert.equal(booleanConstDiagnostics.length, 1);
    assert.match(booleanConstDiagnostics[0].message, /allowRetry/u);
    assert.match(booleanConstDiagnostics[0].message, /constant value true|固定值 true/u);

    const booleanTypeDiagnostics = validateParsedConfig(schema, booleanTypeMismatchYaml);

    assert.equal(booleanTypeDiagnostics.length, 1);
    assert.match(booleanTypeDiagnostics[0].message, /allowRetry/u);
    assert.match(booleanTypeDiagnostics[0].message, /boolean|布尔/u);
});

test("validateParsedConfig should report numeric range and string length mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "minLength": 3,
              "maxLength": 8
            },
            "hp": {
              "type": "integer",
              "minimum": 1,
              "maximum": 10
            },
            "tags": {
              "type": "array",
              "items": {
                "type": "string",
                "maxLength": 4
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
name: Sl
hp: 12
tags:
  - safe
  - shield
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 3);
    assert.match(diagnostics[0].message, /at least 3 characters|至少为 3 个字符/u);
    assert.match(diagnostics[1].message, /less than or equal to 10|小于或等于 10/u);
    assert.match(diagnostics[2].message, /tags\[1\]|shield/u);
});

test("validateParsedConfig should report exclusive bounds, pattern, and array item-count mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "pattern": "^[A-Z][a-z]+$"
            },
            "hp": {
              "type": "integer",
              "exclusiveMinimum": 10,
              "exclusiveMaximum": 20
            },
            "tags": {
              "type": "array",
              "minItems": 2,
              "maxItems": 3,
              "items": {
                "type": "string"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
name: slime
hp: 10
tags:
  - onlyOne
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 3);
    assert.match(diagnostics[0].message, /pattern|正则模式/u);
    assert.match(diagnostics[1].message, /greater than 10|大于 10/u);
    assert.match(diagnostics[2].message, /at least 2 items|至少需要包含 2 个元素/u);
});

test("validateParsedConfig should report exclusive maximum and maxItems violations", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "hp": {
              "type": "integer",
              "exclusiveMinimum": 10,
              "exclusiveMaximum": 20
            },
            "tags": {
              "type": "array",
              "minItems": 1,
              "maxItems": 3,
              "items": {
                "type": "string"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
hp: 20
tags:
  - a
  - b
  - c
  - d
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /less than 20|小于 20/u);
    assert.match(diagnostics[1].message, /at most 3 items|最多只能包含 3 个元素/u);
});

test("validateParsedConfig should report object property-count mismatches", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "minProperties": 2,
          "maxProperties": 3,
          "properties": {
            "reward": {
              "type": "object",
              "minProperties": 2,
              "maxProperties": 2,
              "properties": {
                "gold": { "type": "integer" },
                "currency": { "type": "string" },
                "tier": { "type": "string" }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
  currency: coin
  tier: epic
`);

    const diagnostics = validateParsedConfig(schema, yaml);
    const messages = diagnostics.map((diagnostic) => diagnostic.message);

    assert.equal(diagnostics.length, 2);
    assert.ok(messages.some((message) => /at least 2 properties|至少需要包含 2 个属性/u.test(message)));
    assert.ok(messages.some((message) => /reward.*at most 2 properties|reward.*最多只能包含 2 个子属性/u.test(message)));
});

test("validateParsedConfig should count unique object properties for property-count constraints", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "minProperties": 2,
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
  gold: 20
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /reward.*at least 2 properties|reward.*至少需要包含 2 个子属性/u);
});

test("validateParsedConfig should report multipleOf and uniqueItems violations", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "hp": {
              "type": "integer",
              "multipleOf": 5
            },
            "phases": {
              "type": "array",
              "uniqueItems": true,
              "items": {
                "type": "object",
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
hp: 12
phases:
  -
    wave: 1
    monsterId: slime
  -
    monsterId: slime
    wave: 1
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /multiple of 5|5 的整数倍/u);
    assert.match(diagnostics[1].message, /phases\[1\]|uniqueItems|元素唯一/u);
});

test("validateParsedConfig should accept large decimal multiples without floating-point drift", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRate": {
              "type": "number",
              "multipleOf": 0.1
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRate: 10000000.2
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
});

test("validateParsedConfig should reject large numbers that are not actually multiples", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRate": {
              "type": "number",
              "multipleOf": 1
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRate: 1000000000000.4
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /multiple of 1|1 的整数倍/u);
});

test("validateParsedConfig should accept scientific-notation numbers", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRate": {
              "type": "number"
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRate: 1.5e10
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
});

test("validateParsedConfig should apply schema patterns with Unicode semantics", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "pattern": "^\\\\p{L}+$"
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
name: 测试
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
});

test("validateParsedConfig should skip uniqueItems checks for invalid array items", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "values": {
              "type": "array",
              "uniqueItems": true,
              "items": {
                "type": "integer"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
values:
  -
    id: 1
  -
    id: 2
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /values\[0\]/u);
    assert.match(diagnostics[1].message, /values\[1\]/u);
    assert.ok(diagnostics.every((diagnostic) => !/uniqueItems|元素唯一/u.test(diagnostic.message)));
});

test("validateParsedConfig should report every uniqueItems duplicate in one pass", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "tags": {
              "type": "array",
              "uniqueItems": true,
              "items": {
                "type": "string"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
tags:
  - alpha
  - beta
  - alpha
  - beta
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /tags\[2\]/u);
    assert.match(diagnostics[1].message, /tags\[3\]/u);
});

test("validateParsedConfig should avoid uniqueItems comparable-key collisions for distinct objects", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "entries": {
              "type": "array",
              "uniqueItems": true,
              "items": {
                "type": "object",
                "properties": {
                  "a": { "type": "string" },
                  "b": { "type": "string" }
                }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
entries:
  -
    a: "x|1:b=string:yz"
  -
    a: x
    b: yz
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
});

test("parseSchemaContent should capture scalar range and length metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "minLength": 3,
              "maxLength": 12
            },
            "hp": {
              "type": "integer",
              "minimum": 1,
              "maximum": 99
            },
            "tags": {
              "type": "array",
              "items": {
                "type": "string",
                "minLength": 2,
                "maxLength": 6
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.name.minLength, 3);
    assert.equal(schema.properties.name.maxLength, 12);
    assert.equal(schema.properties.hp.minimum, 1);
    assert.equal(schema.properties.hp.maximum, 99);
    assert.equal(schema.properties.tags.items.minLength, 2);
    assert.equal(schema.properties.tags.items.maxLength, 6);
});

test("parseSchemaContent should capture exclusive bounds, pattern, and array item-count metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "pattern": "^[A-Z][a-z]+$"
            },
            "hp": {
              "type": "integer",
              "exclusiveMinimum": 1,
              "exclusiveMaximum": 99
            },
            "tags": {
              "type": "array",
              "minItems": 2,
              "maxItems": 4,
              "items": {
                "type": "string",
                "pattern": "^[a-z]+$"
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.name.pattern, "^[A-Z][a-z]+$");
    assert.equal(schema.properties.hp.exclusiveMinimum, 1);
    assert.equal(schema.properties.hp.exclusiveMaximum, 99);
    assert.equal(schema.properties.tags.minItems, 2);
    assert.equal(schema.properties.tags.maxItems, 4);
    assert.equal(schema.properties.tags.items.pattern, "^[a-z]+$");
});

test("parseSchemaContent should capture multipleOf and uniqueItems metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "hp": {
              "type": "integer",
              "multipleOf": 5
            },
            "dropRates": {
              "type": "array",
              "uniqueItems": true,
              "items": {
                "type": "number",
                "multipleOf": 0.5
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.hp.multipleOf, 5);
    assert.equal(schema.properties.dropRates.uniqueItems, true);
    assert.equal(schema.properties.dropRates.items.multipleOf, 0.5);
});

test("parseSchemaContent should capture object property-count metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "minProperties": 2,
          "maxProperties": 4,
          "properties": {
            "reward": {
              "type": "object",
              "minProperties": 1,
              "maxProperties": 2,
              "properties": {
                "gold": { "type": "integer" }
              }
            }
          }
        }
    `);

    assert.equal(schema.minProperties, 2);
    assert.equal(schema.maxProperties, 4);
    assert.equal(schema.properties.reward.minProperties, 1);
    assert.equal(schema.properties.reward.maxProperties, 2);
});

test("parseSchemaContent should reject invalid pattern declarations instead of dropping them", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string",
                  "pattern": "["
                }
              }
            }
        `),
        /invalid 'pattern' regular expression/u
    );
});

test("parseSchemaContent should ignore mismatched constraint metadata on unsupported scalar types", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "enabled": {
              "type": "boolean",
              "minimum": 1,
              "minLength": 3
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
enabled: true
`);

    assert.equal(schema.properties.enabled.minimum, undefined);
    assert.equal(schema.properties.enabled.minLength, undefined);
    assert.deepEqual(validateParsedConfig(schema, yaml), []);
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

test("validateParsedConfig should localize expected object diagnostics when Chinese UI is requested", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "properties": {
                "gold": { "type": "integer" }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward: 1
`);

    const diagnostics = validateParsedConfig(schema, yaml, {isChinese: true});

    assert.equal(diagnostics.length, 1);
    assert.equal(diagnostics[0].message, "属性“reward”应为对象。");
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

test("getEditableSchemaFields should sort keys with ordinal semantics", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "a": { "type": "string" },
            "A": { "type": "string" },
            "ä": { "type": "string" },
            "z": { "type": "string" }
          }
        }
    `);

    assert.deepEqual(
        getEditableSchemaFields(schema).map((field) => field.key),
        ["A", "a", "z", "ä"]);
});

test("createSampleConfigYaml should preserve empty-string scalar const values", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "const": ""
            }
          }
        }
    `);

    const sample = createSampleConfigYaml(schema);

    assert.match(sample, /^name: ""$/mu);
});

test("createSampleConfigYaml should prefer scalar const values over defaults", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "rarity": {
              "type": "string",
              "const": "common",
              "default": "rare"
            }
          }
        }
    `);

    const sample = createSampleConfigYaml(schema);

    assert.match(sample, /^rarity: common$/mu);
    assert.ok(!/^rarity: rare$/mu.test(sample));
});

test("parseBatchArrayValue should keep comma-separated batch editing behavior", () => {
    assert.deepEqual(parseBatchArrayValue(" potion, bomb ,  ,elixir "), ["potion", "bomb", "elixir"]);
});
