const test = require("node:test");
const assert = require("node:assert/strict");
const {buildContainsHintLines, describeContainsSchema} = require("../src/containsSummary");
const {createLocalizer} = require("../src/localization");

test("describeContainsSchema should reuse localized Chinese hint strings", () => {
    const localizer = createLocalizer("zh-cn");

    const summary = describeContainsSchema(
        {
            type: "string",
            constValue: "\"potion\"",
            constDisplayValue: "\"potion\"",
            refTable: "item"
        },
        localizer);

    assert.equal(summary, "string, 固定值：\"potion\", 引用表：item");
});

test("describeContainsSchema should fall back to localized item label", () => {
    const localizer = createLocalizer("en");

    const summary = describeContainsSchema({}, localizer);

    assert.equal(summary, "Item");
});

test("buildContainsHintLines should include default minContains when schema omits it", () => {
    const localizer = createLocalizer("en");

    const lines = buildContainsHintLines(
        {
            contains: {
                type: "integer",
                constValue: "5",
                constDisplayValue: "5"
            }
        },
        localizer);

    assert.deepEqual(lines, [
        "Contains: integer, Const: 5",
        "Min contains: 1"
    ]);
});
