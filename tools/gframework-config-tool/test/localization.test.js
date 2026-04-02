const test = require("node:test");
const assert = require("node:assert/strict");
const {createLocalizer} = require("../src/localization");

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
