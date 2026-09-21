const { readFileSync } = require('node:fs');
const { runInNewContext } = require('node:vm');
const assert = require('node:assert/strict');
const { test } = require('node:test');
const source = readFileSync('MiniMola.Web/ClientApp/market-detail.js', 'utf8');
const renderer = source.slice(source.indexOf('function renderFundEstimateHistory('), source.indexOf('function formatSignedPercent('));
function render(items, evaluatedCount = 0) {
    function element() {
        return { textContent: '', children: [], classList: { toggle() {} },
            append(...nodes) { this.children.push(...nodes); },
            appendChild(node) { this.children.push(node); },
            replaceChildren() { this.children = []; } };
    }
    const nodes = new Map();
    const get = id => { if (!nodes.has(id)) nodes.set(id, element()); return nodes.get(id); };
    runInNewContext(renderer + '\nrenderFundEstimateHistory(result);', {
        result: { items, evaluatedCount, meanAbsoluteErrorPercent: evaluatedCount ? 1 : null, withinOnePercentRate: evaluatedCount ? 100 : null },
        document: { getElementById: get, createElement: element, createDocumentFragment: element },
        formatPercent: String, formatDate: String, formatDateTime: String,
        formatPriceWithCurrency: String, formatSignedPercent: String
    });
    return { get, rows: get('fund-estimate-history-list').children[0]?.children ?? [] };
}
const item = { targetDate: '2026-09-17', kind: 'intraday', status: 'pending', estimatedChangePercent: 2,
    estimatedPrice: 10.2, actualChangePercent: null, actualPrice: null, absoluteErrorPercent: null,
    calculatedAtUtc: '2026-09-16T10:00:00Z', coveragePercent: 80 };
test('pending intraday row visible without invented success rate', () => {
    const r = render([item]);
    assert.equal(r.rows.length, 1);
    assert.match(r.rows[0].children[3].textContent, /bekleniyor/);
    assert.match(r.rows[0].children[1].children[0].textContent, /kaydı yok/);
    assert.equal(r.get('fund-estimate-mean-error').textContent, '—');
});
test('intraday and closing values displayed independently', () => {
    const closing = { ...item, kind: 'closing', status: 'evaluated', actualChangePercent: 1, actualPrice: 10.1, absoluteErrorPercent: 1 };
    const r = render([item, closing], 1);
    assert.equal(r.rows.length, 2);
    assert.match(r.rows[1].children[1].textContent, /sabit/);
    assert.match(r.rows[1].children[4].textContent, /1 puan hata/);
    assert.equal(r.rows[0].children[1].children.length, 0);
});
test('empty history does not claim a prediction was recorded', () => {
    const r = render([]);
    assert.equal(r.rows.length, 0);
    assert.match(r.get('fund-estimate-history-state').textContent, /Henüz yeni türde kayıt yok/);
});
test('missing exact-date official price explained explicitly', () => {
    const r = render([{ ...item, status: 'missing-official' }]);
    assert.match(r.rows[0].children[3].textContent, /Hedef tarihin/);
});
