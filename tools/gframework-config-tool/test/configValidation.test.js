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

test("validateParsedConfig should enforce supported string formats", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "releaseDate": {
              "type": "string",
              "format": "date"
            },
            "ancientReleaseDate": {
              "type": "string",
              "format": "date"
            },
            "publishedAt": {
              "type": "string",
              "format": "date-time"
            },
            "respawnDelay": {
              "type": "string",
              "format": "duration"
            },
            "contactEmail": {
              "type": "string",
              "format": "email"
            },
            "dailyResetAt": {
              "type": "string",
              "format": "time"
            },
            "catalogUri": {
              "type": "string",
              "format": "uri"
            },
            "configId": {
              "type": "string",
              "format": "uuid"
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
releaseDate: 2026-02-30
ancientReleaseDate: 0000-01-01
publishedAt: 2026-04-11T08:30:00
respawnDelay: P1Y
contactEmail: boss.example.com
dailyResetAt: 08:30:00
catalogUri: /loot-table
configId: 123e4567e89b12d3a456426614174000
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 8);
    assert.match(diagnostics[0].message, /format 'date'|字符串格式“date”/u);
    assert.match(diagnostics[1].message, /format 'date'|字符串格式“date”/u);
    assert.match(diagnostics[2].message, /format 'date-time'|字符串格式“date-time”/u);
    assert.match(diagnostics[3].message, /format 'duration'|字符串格式“duration”/u);
    assert.match(diagnostics[4].message, /format 'email'|字符串格式“email”/u);
    assert.match(diagnostics[5].message, /format 'time'|字符串格式“time”/u);
    assert.match(diagnostics[6].message, /format 'uri'|字符串格式“uri”/u);
    assert.match(diagnostics[7].message, /format 'uuid'|字符串格式“uuid”/u);
});

test("validateParsedConfig should accept supported string formats", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "releaseDate": {
              "type": "string",
              "format": "date"
            },
            "publishedAt": {
              "type": "string",
              "format": "date-time"
            },
            "respawnDelay": {
              "type": "string",
              "format": "duration"
            },
            "contactEmail": {
              "type": "string",
              "format": "email"
            },
            "dailyResetAt": {
              "type": "string",
              "format": "time"
            },
            "catalogUri": {
              "type": "string",
              "format": "uri"
            },
            "configId": {
              "type": "string",
              "format": "uuid"
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
releaseDate: 2026-04-11
publishedAt: 2026-04-11T08:30:00Z
respawnDelay: P2DT3H4M5.5S
contactEmail: boss@example.com
dailyResetAt: 08:30:00Z
catalogUri: https://example.com/loot-table
configId: 123e4567-e89b-12d3-a456-426614174000
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
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

test("validateParsedConfig should report contains match-count violations", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "minContains": 2,
              "contains": {
                "type": "integer",
                "const": 5
              },
              "items": {
                "type": "integer"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRates:
  - 5
  - 7
  - 9
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /at least 2 items matching the 'contains' schema|至少需要包含 2 个匹配 contains 条件的元素/u);
});

test("validateParsedConfig should skip contains match-count when items are structurally invalid", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "required": ["dropRates"],
          "properties": {
            "dropRates": {
              "type": "array",
              "minContains": 2,
              "contains": {
                "type": "object",
                "required": ["type"],
                "properties": {
                  "type": {
                    "type": "string",
                    "const": "RARE"
                  }
                }
              },
              "items": {
                "type": "object",
                "required": ["type", "value"],
                "properties": {
                  "type": {
                    "type": "string"
                  },
                  "value": {
                    "type": "integer"
                  }
                }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRates:
  -
    type: RARE
    value: "not-a-number"
  -
    type: RARE
    value: 10
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.ok(diagnostics.length > 0);
    assert.match(
        diagnostics[0].message,
        /dropRates\[0\]\.value/u);
    assert.match(
        diagnostics[0].message,
        /integer|整数/u);
    assert.equal(
        diagnostics.some((diagnostic) => /at least 2 items matching the 'contains' schema|至少需要包含 2 个匹配 contains 条件的元素/u.test(diagnostic.message)),
        false);
    assert.equal(
        diagnostics.some((diagnostic) => /at most \d+ items matching the 'contains' schema|最多只能包含 \d+ 个匹配 contains 条件的元素/u.test(diagnostic.message)),
        false);
});

test("validateParsedConfig should continue contains match-count when items only have value-level violations", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "minContains": 1,
              "contains": {
                "type": "integer",
                "const": 7
              },
              "items": {
                "type": "integer",
                "minimum": 10
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRates:
  - 5
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 2);
    assert.match(diagnostics[0].message, /greater than or equal to 10|大于或等于 10/u);
    assert.match(diagnostics[1].message, /at least 1 items matching the 'contains' schema|至少需要包含 1 个匹配 contains 条件的元素/u);
});

test("validateParsedConfig should report maxContains violations", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "maxContains": 1,
              "contains": {
                "type": "integer",
                "const": 5
              },
              "items": {
                "type": "integer"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
dropRates:
  - 5
  - 5
  - 7
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /at most 1 items matching the 'contains' schema|最多只能包含 1 个匹配 contains 条件的元素/u);
});

test("validateParsedConfig should accept satisfied contains constraints", () => {
    const schemaWithRange = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "minContains": 2,
              "maxContains": 3,
              "contains": {
                "type": "integer",
                "const": 5
              },
              "items": {
                "type": "integer"
              }
            }
          }
        }
    `);
    const yamlWithinRange = parseTopLevelYaml(`
dropRates:
  - 0
  - 5
  - 5
  - 10
`);

    assert.deepEqual(validateParsedConfig(schemaWithRange, yamlWithinRange), []);

    const schemaWithDefaultMinContains = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "contains": {
                "type": "integer",
                "const": 5
              },
              "items": {
                "type": "integer"
              }
            }
          }
        }
    `);
    const yamlSatisfyingDefaultMinContains = parseTopLevelYaml(`
dropRates:
  - 1
  - 2
  - 5
  - 3
`);

    assert.deepEqual(validateParsedConfig(schemaWithDefaultMinContains, yamlSatisfyingDefaultMinContains), []);
});

test("validateParsedConfig should allow object contains matches with additional declared item fields", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "entries": {
              "type": "array",
              "minContains": 1,
              "contains": {
                "type": "object",
                "required": ["id"],
                "properties": {
                  "id": {
                    "type": "string",
                    "const": "boss"
                  }
                }
              },
              "items": {
                "type": "object",
                "required": ["id", "weight"],
                "properties": {
                  "id": {
                    "type": "string"
                  },
                  "weight": {
                    "type": "integer"
                  }
                }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
entries:
  -
    id: boss
    weight: 10
  -
    id: slime
    weight: 3
`);

    assert.deepEqual(validateParsedConfig(schema, yaml), []);
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

test("parseSchemaContent should capture supported string format metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "contactEmail": {
              "type": "string",
              "format": "email"
            },
            "aliases": {
              "type": "array",
              "items": {
                "type": "string",
                "format": "duration"
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.contactEmail.format, "email");
    assert.equal(schema.properties.aliases.items.format, "duration");
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

test("parseSchemaContent should capture contains metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "dropRates": {
              "type": "array",
              "minContains": 1,
              "maxContains": 2,
              "contains": {
                "type": "integer",
                "const": 5
              },
              "items": {
                "type": "integer"
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.dropRates.minContains, 1);
    assert.equal(schema.properties.dropRates.maxContains, 2);
    assert.equal(schema.properties.dropRates.contains.type, "integer");
    assert.equal(schema.properties.dropRates.contains.constDisplayValue, "5");
});

test("parseSchemaContent should reject nested-array contains schemas", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "dropRates": {
                  "type": "array",
                  "contains": {
                    "type": "array",
                    "items": {
                      "type": "integer"
                    }
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
        `),
        /unsupported nested array 'contains' schemas/u);
});

test("parseSchemaContent should reject minContains and maxContains without contains", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "dropRates": {
                  "type": "array",
                  "minContains": 1,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
        `),
        /'minContains' or 'maxContains' without 'contains'/u);

    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "dropRates": {
                  "type": "array",
                  "maxContains": 1,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
        `),
        /'minContains' or 'maxContains' without 'contains'/u);
});

test("parseSchemaContent should reject contains schemas where default minContains exceeds maxContains", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "dropRates": {
                  "type": "array",
                  "maxContains": 0,
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
        `),
        /'minContains' greater than 'maxContains'/u);
});

test("parseSchemaContent should reject contains schemas where minContains is greater than maxContains", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "dropRates": {
                  "type": "array",
                  "minContains": 3,
                  "maxContains": 1,
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
        `),
        /'minContains' greater than 'maxContains'/u);
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

test("parseSchemaContent should capture not sub-schema metadata", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "not": {
                "type": "string",
                "const": "Deprecated"
              }
            }
          }
        }
    `);

    assert.equal(schema.properties.name.not.type, "string");
    assert.equal(schema.properties.name.not.constDisplayValue, "\"Deprecated\"");
});

test("parseSchemaContent should reject non-object not declarations", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string",
                  "not": "deprecated"
                }
              }
            }
        `),
        /must declare 'not' as an object-valued schema/u
    );
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

test("parseSchemaContent should reject unsupported string format declarations", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string",
                  "format": "ipv4"
                }
              }
            }
        `),
        /unsupported string format 'ipv4'.*'duration'.*'time'/u
    );
});

test("parseSchemaContent should reject format declarations on non-string schema nodes", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "hp": {
                  "type": "integer",
                  "format": "uuid"
                }
              }
            }
        `),
        /can only declare 'format' on type 'string'/u
    );
});

test("parseSchemaContent should reject non-string format metadata values", () => {
    assert.throws(
        () => parseSchemaContent(`
            {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string",
                  "format": 123
                }
              }
            }
        `),
        /must declare 'format' as a string/u
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

test("validateParsedConfig should reject values that match a forbidden not schema", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "not": {
                "type": "string",
                "const": "Deprecated"
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
name: Deprecated
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.equal(
        diagnostics[0].message,
        "Property 'name' must not match the forbidden 'not' schema.");
});

test("validateParsedConfig should keep not object matching strict instead of contains-style subset matching", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "not": {
                "type": "object",
                "required": ["gold"],
                "properties": {
                  "gold": { "type": "integer" }
                }
              },
              "properties": {
                "gold": { "type": "integer" },
                "bonus": { "type": "integer" }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
  bonus: 5
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.deepEqual(diagnostics, []);
});

test("validateParsedConfig should reject objects that fully match a forbidden not schema", () => {
    const schema = parseSchemaContent(`
        {
          "type": "object",
          "properties": {
            "reward": {
              "type": "object",
              "not": {
                "type": "object",
                "required": ["gold"],
                "properties": {
                  "gold": { "type": "integer" }
                }
              },
              "properties": {
                "gold": { "type": "integer" },
                "bonus": { "type": "integer" }
              }
            }
          }
        }
    `);
    const yaml = parseTopLevelYaml(`
reward:
  gold: 10
`);

    const diagnostics = validateParsedConfig(schema, yaml);

    assert.equal(diagnostics.length, 1);
    assert.match(diagnostics[0].message, /not/u);
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
