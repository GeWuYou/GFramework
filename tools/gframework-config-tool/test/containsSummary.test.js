const test = require("node:test");
const assert = require("node:assert/strict");
const {describeContainsSchema} = require("../src/containsSummary");
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
