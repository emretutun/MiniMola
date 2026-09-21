const { readFileSync } = require('node:fs');
const assert = require('node:assert/strict');
const { test } = require('node:test');
const css = readFileSync('MiniMola.Web/wwwroot/css/theme.css', 'utf8');
const token = name => css.match(new RegExp(`--${name}:\\s*(#[0-9a-f]{6});`, 'i'))[1];
function luminance(hex) {
    const rgb = hex.slice(1).match(/../g).map(x => parseInt(x, 16) / 255)
        .map(x => x <= 0.04045 ? x / 12.92 : ((x + 0.055) / 1.055) ** 2.4);
    return rgb[0] * 0.2126 + rgb[1] * 0.7152 + rgb[2] * 0.0722;
}
function ratio(a, b) {
    const x = luminance(a), y = luminance(b);
    return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05);
}
for (const foreground of ['mm-ink', 'mm-ink-soft', 'mm-muted']) {
    test(`${foreground} is readable on dark surfaces`, () => {
        for (const background of ['mm-page', 'mm-surface', 'mm-surface-soft'])
            assert.ok(ratio(token(foreground), token(background)) >= 4.5);
    });
}
for (const [label, foreground, background] of [
    ['buy', '#77ddae', '#203c35'], ['sell/error', '#ffaaa0', '#492c30'],
    ['hold', '#e7cb8e', '#41371f'], ['holding weight', '#e7cb8e', '#222f3a']
]) {
    test(`${label} palette retains text contrast`, () => {
        assert.ok(css.includes(foreground) && css.includes(background));
        assert.ok(ratio(foreground, background) >= 4.5);
    });
}
