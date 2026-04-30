/**
 * Build a compact contains-schema summary for array field hints.
 * The summary reuses existing localized hint strings so Chinese UI surfaces
 * do not fall back to mixed English tokens such as const/enum/pattern/ref.
 *
 * @param {{type?: string, enumValues?: string[], constValue?: string, constDisplayValue?: string, pattern?: string, refTable?: string}} containsSchema Parsed contains schema metadata.
 * @param {{t: (key: string, params?: Record<string, string | number>) => string}} localizer Runtime localizer.
 * @returns {string} Human-facing summary.
 */
function describeContainsSchema(containsSchema, localizer) {
    const parts = [];
    if (containsSchema.type) {
        parts.push(containsSchema.type);
    }

    if (containsSchema.constValue !== undefined) {
        parts.push(localizer.t("webview.hint.const", {
            value: containsSchema.constDisplayValue ?? containsSchema.constValue
        }));
    } else if (Array.isArray(containsSchema.enumValues) && containsSchema.enumValues.length > 0) {
        parts.push(localizer.t("webview.hint.allowed", {
            values: containsSchema.enumValues.join(", ")
        }));
    } else if (containsSchema.pattern) {
        parts.push(localizer.t("webview.hint.pattern", {
            value: containsSchema.pattern
        }));
    }

    if (containsSchema.refTable) {
        parts.push(localizer.t("webview.hint.refTable", {
            refTable: containsSchema.refTable
        }));
    }

    return parts.join(", ") || localizer.t("webview.objectArray.item");
}

/**
 * Build localized contains-related hint lines for array fields.
 *
 * @param {{contains?: {type?: string, enumValues?: string[], constValue?: string, constDisplayValue?: string, pattern?: string, refTable?: string}, minContains?: number, maxContains?: number}} propertySchema Array property schema metadata.
 * @param {{t: (key: string, params?: Record<string, string | number>) => string}} localizer Runtime localizer.
 * @returns {string[]} Localized contains hint lines.
 */
function buildContainsHintLines(propertySchema, localizer) {
    if (!propertySchema.contains) {
        return [];
    }

    const effectiveMinContains = typeof propertySchema.minContains === "number"
        ? propertySchema.minContains
        : 1;
    const lines = [
        localizer.t("webview.hint.contains", {
            summary: describeContainsSchema(propertySchema.contains, localizer)
        }),
        localizer.t("webview.hint.minContains", {
            value: effectiveMinContains
        })
    ];

    if (typeof propertySchema.maxContains === "number") {
        lines.push(localizer.t("webview.hint.maxContains", {
            value: propertySchema.maxContains
        }));
    }

    return lines;
}

module.exports = {
    describeContainsSchema,
    buildContainsHintLines
};
