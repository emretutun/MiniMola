const { readFileSync } = require('node:fs');
const { runInNewContext } = require('node:vm');
const assert = require('node:assert/strict');
const { test } = require('node:test');
const source = readFileSync('MiniMola.Web/wwwroot/js/theme.js', 'utf8');
function setup(saved, dark = false, blocked = false) {
    const events = {}, windowEvents = {}, changes = [];
    const select = { value: '', addEventListener: (k, f) => events[k] = f };
    const root = { dataset: {}, style: {} };
    const media = { matches: dark, addEventListener: (_, f) => events.system = f };
    const store = new Map(saved ? [['minimola.theme', saved]] : []);
    const document = { documentElement: root, querySelector: () => ({}),
        getElementById: () => select, addEventListener: (k, f) => events[k] = f };
    runInNewContext(source, { document,
        window: { matchMedia: () => media, addEventListener: (k, f) => windowEvents[k] = f,
            dispatchEvent: e => changes.push(e) },
        localStorage: { getItem: k => { if (blocked) throw Error(); return store.get(k); },
            setItem: (k, v) => { if (blocked) throw Error(); store.set(k, v); } },
        CustomEvent: class { constructor(type, options) { this.type = type; this.detail = options.detail; } }
    });
    events.DOMContentLoaded();
    return { root, media, events, windowEvents, select, store, changes };
}
test('initial system theme, saved preference and invalid preference', () => {
    assert.equal(setup(null, true).root.dataset.bsTheme, 'dark');
    assert.equal(setup('light', true).root.dataset.bsTheme, 'light');
    assert.equal(setup('invalid', true).select.value, 'system');
});
test('selection persists and emits theme event', () => {
    const t = setup(); t.select.value = 'dark'; t.events.change();
    assert.equal(t.store.get('minimola.theme'), 'dark');
    assert.equal(t.root.dataset.bsTheme, 'dark');
    assert.equal(t.changes.at(-1).detail.theme, 'dark');
    assert.equal(setup(t.store.get('minimola.theme')).root.dataset.bsTheme, 'dark');
});
test('system changes only affect system preference', () => {
    const t = setup(); t.media.matches = true; t.events.system();
    assert.equal(t.root.dataset.bsTheme, 'dark');
    t.select.value = 'light'; t.events.change(); t.events.system();
    assert.equal(t.root.dataset.bsTheme, 'light');
});
test('storage denial does not break switching', () => {
    const t = setup(null, false, true); t.select.value = 'dark'; t.events.change();
    assert.equal(t.root.dataset.bsTheme, 'dark');
});
test('other tabs synchronize, cleared preference follows system', () => {
    const t = setup(); t.windowEvents.storage({ key: 'minimola.theme', newValue: 'dark' });
    assert.equal(t.select.value, 'dark');
    t.windowEvents.storage({ key: null, newValue: null });
    assert.equal(t.root.dataset.bsTheme, 'light');
    assert.equal(t.select.value, 'system');
});
