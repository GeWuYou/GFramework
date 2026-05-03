// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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

test("buildContainsHintLines should use explicit minContains when provided", () => {
    const localizer = createLocalizer("en");

    const lines = buildContainsHintLines(
        {
            minContains: 2,
            maxContains: 3,
            contains: {
                type: "string",
                constValue: "\"potion\"",
                constDisplayValue: "\"potion\"",
                refTable: "item"
            }
        },
        localizer);

    assert.deepEqual(lines, [
        "Contains: string, Const: \"potion\", Ref table: item",
        "Min contains: 2",
        "Max contains: 3"
    ]);
});

test("describeContainsSchema should format enum-based contains schema in English", () => {
    const localizer = createLocalizer("en");

    const summary = describeContainsSchema(
        {
            type: "string",
            enumValues: ["potion", "elixir"],
            refTable: "item"
        },
        localizer);

    assert.equal(summary, "string, Allowed: potion, elixir, Ref table: item");
});

test("describeContainsSchema should format pattern-based contains schema in Chinese", () => {
    const localizer = createLocalizer("zh-cn");

    const summary = describeContainsSchema(
        {
            type: "string",
            pattern: "^potion-",
            refTable: "item"
        },
        localizer);

    assert.equal(summary, "string, 正则模式：^potion-, 引用表：item");
});

test("buildContainsHintLines should use updated Chinese contains hint wording", () => {
    const localizer = createLocalizer("zh-cn");

    const lines = buildContainsHintLines(
        {
            minContains: 1,
            maxContains: 2,
            contains: {
                type: "string",
                enumValues: ["potion", "elixir"]
            }
        },
        localizer);

    assert.deepEqual(lines, [
        "contains 条件：string, 允许值：potion, elixir",
        "最少匹配数：1",
        "最多匹配数：2"
    ]);
});
