const { readFileSync } = require('node:fs');
const { runInNewContext } = require('node:vm');
const assert = require('node:assert/strict');
const { test } = require('node:test');
const source = readFileSync('MiniMola.Web/ClientApp/market-detail.js', 'utf8');
const renderer = source.slice(source.indexOf('function renderFundPortfolio(result)'), source.indexOf('function renderFundPortfolioHoldings('));
test('available portfolio still displays failed-update warning', () => {
    const nodes = new Map();
    let message;
    runInNewContext(renderer + '\nrenderFundPortfolio(result);', {
        result: { isSupported: true, isAvailable: true, holdings: [{ marketAssetId: 1 }],
            message: 'Yeni rapor okunamadı; son geçerli rapor kullanılıyor.' },
        document: { getElementById(id) { if (!nodes.has(id)) nodes.set(id, {}); return nodes.get(id); } },
        renderFundPortfolioHoldings() {}, formatPercent: String, formatDate: String, formatDateTime: String,
        setFundPortfolioState(text) { message = text; }
    });
    assert.match(message, /1 hisse okundu/);
    assert.match(message, /son geçerli rapor/);
    assert.equal(nodes.get('fund-portfolio-panel').hidden, false);
});
