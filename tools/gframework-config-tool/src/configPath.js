// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

/**
 * Join one object property onto a logical config path.
 *
 * @param {string} parentPath Parent logical path.
 * @param {string} propertyName Property name.
 * @returns {string} Combined logical path.
 */
function joinPropertyPath(parentPath, propertyName) {
    return parentPath ? `${parentPath}.${propertyName}` : propertyName;
}

/**
 * Join one indexed array item onto a logical config path.
 *
 * @param {string} arrayPath Array logical path.
 * @param {number} itemIndex Zero-based item index.
 * @returns {string} Indexed logical path.
 */
function joinArrayIndexPath(arrayPath, itemIndex) {
    return `${arrayPath}[${itemIndex}]`;
}

/**
 * Join one array-item template marker onto a logical config path.
 *
 * @param {string} arrayPath Array logical path.
 * @returns {string} Template logical path.
 */
function joinArrayTemplatePath(arrayPath) {
    return `${arrayPath}[]`;
}

/**
 * Check whether a logical path still contains one template array marker.
 *
 * @param {string} path Logical path.
 * @returns {boolean} True when the path contains a template array segment.
 */
function isTemplatePath(path) {
    return String(path).includes("[]");
}

/**
 * Split one logical object path into individual property segments.
 * The current form model only supports dotted object paths here and keeps
 * array indexing as part of other dedicated helpers.
 *
 * @param {string} path Logical path.
 * @returns {string[]} Property segments.
 */
function splitObjectPath(path) {
    return String(path)
        .split(".")
        .map((segment) => segment.trim())
        .filter((segment) => segment.length > 0);
}

module.exports = {
    isTemplatePath,
    joinArrayIndexPath,
    joinArrayTemplatePath,
    joinPropertyPath,
    splitObjectPath
};
