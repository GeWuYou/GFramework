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
