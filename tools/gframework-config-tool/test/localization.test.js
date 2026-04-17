const test = require("node:test");
const assert = require("node:assert/strict");
const {createLocalizer} = require("../src/localization");
const {ValidationMessageKeys} = require("../src/localizationKeys");

test("createLocalizer should default to English strings", () => {
    const localizer = createLocalizer("en");

    assert.equal(localizer.languageTag, "en");
    assert.equal(localizer.isChinese, false);
    assert.equal(localizer.t("webview.button.save"), "Save Form");
    assert.equal(
        localizer.t("message.batchEditUpdated", {count: 2, domain: "monster"}),
        "Batch updated 2 config file(s) in 'monster'.");
});

test("createLocalizer should switch to Simplified Chinese for zh languages", () => {
    const localizer = createLocalizer("zh-cn");

    assert.equal(localizer.languageTag, "zh-CN");
    assert.equal(localizer.isChinese, true);
    assert.equal(localizer.t("webview.button.save"), "保存表单");
    assert.equal(
        localizer.t("message.batchEditUpdated", {count: 2, domain: "monster"}),
        "已在“monster”中批量更新 2 个配置文件。");
});

test("createLocalizer should fall back to English for Traditional Chinese locales", () => {
    const localizer = createLocalizer("zh-TW");

    assert.equal(localizer.languageTag, "zh-tw");
    assert.equal(localizer.isChinese, false);
    assert.equal(localizer.t("webview.button.save"), "Save Form");
    assert.equal(
        localizer.t("message.batchEditUpdated", {count: 2, domain: "monster"}),
        "Batch updated 2 config file(s) in 'monster'.");
});

test("createLocalizer should expose object property-count validation keys in English", () => {
    const localizer = createLocalizer("en");

    assert.equal(
        localizer.t(ValidationMessageKeys.minPropertiesViolation, {displayPath: "reward", value: 2}),
        "Property 'reward' must contain at least 2 properties.");
    assert.equal(
        localizer.t(ValidationMessageKeys.maxPropertiesViolation, {displayPath: "reward", value: 3}),
        "Property 'reward' must contain at most 3 properties.");
});

test("createLocalizer should expose object property-count validation keys in Simplified Chinese", () => {
    const localizer = createLocalizer("zh-cn");

    assert.equal(
        localizer.t(ValidationMessageKeys.minPropertiesViolation, {displayPath: "reward", value: 2}),
        "对象属性“reward”至少需要包含 2 个子属性。");
    assert.equal(
        localizer.t(ValidationMessageKeys.maxPropertiesViolation, {displayPath: "reward", value: 3}),
        "对象属性“reward”最多只能包含 3 个子属性。");
});

test("createLocalizer should expose contains-count validation keys", () => {
    const englishLocalizer = createLocalizer("en");
    const chineseLocalizer = createLocalizer("zh-cn");

    assert.equal(
        englishLocalizer.t(ValidationMessageKeys.minContainsViolation, {displayPath: "dropRates", value: 2}),
        "Property 'dropRates' must contain at least 2 items matching the 'contains' schema.");
    assert.equal(
        chineseLocalizer.t(ValidationMessageKeys.maxContainsViolation, {displayPath: "dropRates", value: 1}),
        "属性“dropRates”最多只能包含 1 个匹配 contains 条件的元素。");
});

test("createLocalizer should expose not validation keys", () => {
    const englishLocalizer = createLocalizer("en");
    const chineseLocalizer = createLocalizer("zh-cn");

    assert.equal(
        englishLocalizer.t(ValidationMessageKeys.notViolation, {displayPath: "name"}),
        "Property 'name' must not match the forbidden 'not' schema.");
    assert.equal(
        chineseLocalizer.t(ValidationMessageKeys.notViolation, {displayPath: "name"}),
        "属性“name”不能匹配被 `not` 禁止的 schema。");
});

test("createLocalizer should expose dependentRequired validation keys", () => {
    const englishLocalizer = createLocalizer("en");
    const chineseLocalizer = createLocalizer("zh-cn");

    assert.equal(
        englishLocalizer.t("webview.hint.dependentRequired", {
            trigger: "reward.itemId",
            dependencies: "reward.itemCount, reward.bonusCount"
        }),
        "When reward.itemId is set: require reward.itemCount, reward.bonusCount");
    assert.equal(
        chineseLocalizer.t("webview.hint.dependentRequired", {
            trigger: "reward.itemId",
            dependencies: "reward.itemCount, reward.bonusCount"
        }),
        "当 reward.itemId 出现时：还必须声明 reward.itemCount, reward.bonusCount");
    assert.equal(
        englishLocalizer.t(ValidationMessageKeys.dependentRequiredViolation, {
            displayPath: "reward.itemCount",
            triggerProperty: "reward.itemId"
        }),
        "Property 'reward.itemCount' is required when sibling property 'reward.itemId' is present.");
    assert.equal(
        chineseLocalizer.t(ValidationMessageKeys.dependentRequiredViolation, {
            displayPath: "reward.itemCount",
            triggerProperty: "reward.itemId"
        }),
        "属性“reward.itemId”存在时，必须同时声明属性“reward.itemCount”。");
});

test("createLocalizer should expose dependentSchemas validation keys", () => {
    const englishLocalizer = createLocalizer("en");
    const chineseLocalizer = createLocalizer("zh-cn");

    assert.equal(
        englishLocalizer.t("webview.hint.required", {
            properties: "itemCount, bonusCount"
        }),
        "Required: itemCount, bonusCount");
    assert.equal(
        englishLocalizer.t("webview.hint.dependentSchemas", {
            trigger: "reward.itemId",
            schema: "object, Required: itemCount"
        }),
        "When reward.itemId is set: satisfy object, Required: itemCount");
    assert.equal(
        chineseLocalizer.t("webview.hint.dependentSchemas", {
            trigger: "reward.itemId",
            schema: "object, 必填字段：itemCount"
        }),
        "当 reward.itemId 出现时：还必须满足 object, 必填字段：itemCount");
    assert.equal(
        englishLocalizer.t(ValidationMessageKeys.dependentSchemasViolation, {
            displayPath: "reward",
            triggerProperty: "reward.itemId"
        }),
        "Object 'reward' must satisfy the dependent schema triggered by sibling property 'reward.itemId'.");
    assert.equal(
        chineseLocalizer.t(ValidationMessageKeys.dependentSchemasViolation, {
            displayPath: "reward",
            triggerProperty: "reward.itemId"
        }),
        "对象“reward”在属性“reward.itemId”存在时，必须满足对应的 dependent schema。");
});
