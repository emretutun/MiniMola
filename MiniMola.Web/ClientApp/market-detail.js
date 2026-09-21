import {
    CandlestickSeries,
    ColorType,
    CrosshairMode,
    HistogramSeries,
    LineSeries,
    createChart
} from "lightweight-charts";

const page =
    document.getElementById("market-detail-page");

const assetTypeNames = {
    1: "Fon",
    2: "BES",
    3: "Hisse",
    4: "Endeks",
    5: "Kripto",
    6: "Kıymetli maden",
    7: "Döviz",
    8: "ETF",
    9: "Emtia",
    10: "Tahvil"
};

const state = {
    range: "1m",
    requestController: null,
    analysisController: null,
    estimateController: null,
    portfolioController: null,
    chart: null,
    resizeObserver: null,
    currency: "TRY"
};

function applyChartTheme(chart = state.chart) {
    if (!chart) return;
    const dark = document.documentElement.dataset.bsTheme === "dark";
    const line = dark ? "#344450" : "rgba(8, 59, 73, 0.12)";
    chart.applyOptions({
        layout: { background: { type: ColorType.Solid, color: dark ? "#1a242e" : "#fbfdfd" }, textColor: dark ? "#a3b3c2" : "#6d8288" },
        grid: { vertLines: { color: dark ? "#283641" : "rgba(8, 59, 73, 0.055)" }, horzLines: { color: dark ? "#283641" : "rgba(8, 59, 73, 0.055)" } },
        rightPriceScale: { borderColor: line },
        timeScale: { borderColor: line }
    });
}
window.addEventListener("minimola:themechange", () => applyChartTheme());

if (page) {
    initializeMarketDetail()
        .catch(handleInitialError);
}

async function initializeMarketDetail() {
    bindRangeButtons();
    await Promise.all([
        loadHistory(),
        loadAnalysis(),
        loadFundEstimate(),
        loadFundPortfolio()
    ]);
}

function bindRangeButtons() {
    const buttons =
        document.querySelectorAll(
            ".market-range-buttons button");

    for (const button of buttons) {
        button.addEventListener("click", () => {
            const requestedRange =
                button.dataset.range;

            if (!requestedRange ||
                requestedRange === state.range) {
                return;
            }

            state.range = requestedRange;

            for (const item of buttons) {
                item.classList.toggle(
                    "is-active",
                    item === button);
            }

            loadHistory();
        });
    }
}

async function loadHistory() {
    state.requestController?.abort();

    const currentController =
        new AbortController();

    state.requestController =
        currentController;

    setLoadingState(true);

    try {
        const url =
            new URL(
                page.dataset.apiUrl,
                window.location.origin);

        url.searchParams.set(
            "range",
            state.range);

        const response =
            await fetch(url, {
                method: "GET",
                credentials: "same-origin",
                signal: currentController.signal,
                headers: {
                    Accept: "application/json"
                }
            });

        const result =
            await response.json()
                .catch(() => null);

        if (state.requestController
            !== currentController) {
            return;
        }

        if (!response.ok) {
            throw new Error(
                response.status === 404
                    ? "Varlık bulunamadı."
                    : getErrorMessage(
                        result,
                        `Grafik verisi alınamadı: ${response.status}`));
        }

        if (!result ||
            !Array.isArray(result.points)) {
            throw new Error(
                "Sunucudan geçersiz grafik verisi geldi.");
        }

        renderAssetSummary(result);

        if (!result.isSupported ||
            result.points.length === 0) {
            destroyChart();
            setChartState(
                result.message ||
                "Bu dönem için grafik verisi bulunamadı.",
                true);
            resetStats();
            return;
        }

        renderChart(result.points);
        renderStats(result.points);
        setChartState("", false);
    } catch (error) {
        if (error.name === "AbortError" ||
            state.requestController
                !== currentController) {
            return;
        }

        destroyChart();
        setChartState(
            error.message ||
            "Grafik yüklenemedi.",
            true);
        resetStats();
    } finally {
        if (state.requestController
            === currentController) {
            state.requestController = null;
            setLoadingState(false);
        }
    }
}

async function loadAnalysis() {
    state.analysisController?.abort();

    const currentController =
        new AbortController();

    state.analysisController =
        currentController;

    setAnalysisState(
        "Teknik görünüm hazırlanıyor...",
        false);

    try {
        const response =
            await fetch(
                page.dataset.analysisUrl,
                {
                    method: "GET",
                    credentials: "same-origin",
                    signal: currentController.signal,
                    headers: {
                        Accept: "application/json"
                    }
                });

        const result =
            await response.json()
                .catch(() => null);

        if (state.analysisController
            !== currentController) {
            return;
        }

        if (!response.ok) {
            throw new Error(
                response.status === 404
                    ? "Varlık bulunamadı."
                    : getErrorMessage(
                        result,
                        `Teknik görünüm alınamadı: ${response.status}`));
        }

        if (!result ||
            !Array.isArray(result.indicators)) {
            throw new Error(
                "Sunucudan geçersiz teknik analiz verisi geldi.");
        }

        renderAnalysis(result);
    } catch (error) {
        if (error.name === "AbortError" ||
            state.analysisController
                !== currentController) {
            return;
        }

        renderUnavailableAnalysis(
            error.message ||
            "Teknik görünüm yüklenemedi.");
    } finally {
        if (state.analysisController
            === currentController) {
            state.analysisController = null;
        }
    }
}

async function loadFundEstimate() {
    state.estimateController?.abort();

    const currentController =
        new AbortController();

    state.estimateController =
        currentController;

    try {
        const response =
            await fetch(
                page.dataset.estimateUrl,
                {
                    method: "GET",
                    credentials: "same-origin",
                    signal: currentController.signal,
                    headers: {
                        Accept: "application/json"
                    }
                });

        const result =
            await response.json()
                .catch(() => null);

        if (state.estimateController
            !== currentController) {
            return;
        }

        if (!response.ok) {
            throw new Error(
                response.status === 404
                    ? "Varlık bulunamadı."
                    : getErrorMessage(
                        result,
                        `Fon tahmini alınamadı: ${response.status}`));
        }

        if (!result ||
            !Array.isArray(result.contributions)) {
            throw new Error(
                "Sunucudan geçersiz fon tahmini geldi.");
        }

        renderFundEstimate(result);

        if (result.isSupported) {
            await loadFundEstimateHistory(
                currentController.signal);
        }
    } catch (error) {
        if (error.name === "AbortError" ||
            state.estimateController
                !== currentController) {
            return;
        }

        const panel =
            document.getElementById(
                "fund-estimate-panel");

        panel.hidden = false;
        setFundEstimateState(
            error.message ||
            "Fon tahmini hazırlanamadı.",
            true);
    } finally {
        if (state.estimateController
            === currentController) {
            state.estimateController = null;
        }
    }
}

function renderFundEstimate(result) {
    const panel =
        document.getElementById(
            "fund-estimate-panel");

    if (!result.isSupported) {
        panel.hidden = true;
        return;
    }

    panel.hidden = false;

    if (result.quoteCurrency) {
        state.currency = result.quoteCurrency;
    }

    document.getElementById(
        "fund-estimate-method")
        .textContent = result.methodology;

    document.getElementById(
        "fund-estimate-base-price")
        .textContent = result.basePrice === null
            ? "—"
            : formatPriceWithCurrency(
                result.basePrice);

    document.getElementById(
        "fund-estimate-base-date")
        .textContent = result.basePriceObservedAtUtc
            ? `Fiyat tarihi: ${formatDate(
                result.basePriceObservedAtUtc)}`
            : "Resmî fiyat tarihi yok";

    document.getElementById(
        "fund-estimate-coverage")
        .textContent =
            `%${formatPercent(
                result.coveragePercent || 0)}`;

    document.getElementById(
        "fund-estimate-distribution-date")
        .textContent = result.distributionDate
            ? `Dağılım: ${formatDate(
                result.distributionDate)}`
            : "Dağılım tarihi yok";

    const confidenceElement =
        document.getElementById(
            "fund-estimate-confidence");

    confidenceElement.className =
        `is-${result.confidenceCode || "insufficient"}`;
    confidenceElement.textContent =
        result.confidenceLabel || "Yetersiz";

    document.getElementById(
        "fund-estimate-calculated-at")
        .textContent = result.calculatedAtUtc
            ? `Hesaplama: ${formatDateTime(
                result.calculatedAtUtc)}`
            : "Hesaplama zamanı yok";

    const changeElement =
        document.getElementById(
            "fund-estimate-change");

    changeElement.className =
        "fund-estimate-change is-neutral";

    if (result.isAvailable &&
        Number.isFinite(Number(
            result.estimatedChangePercent))) {
        const change =
            Number(result.estimatedChangePercent);

        changeElement.className =
            "fund-estimate-change " +
            (change > 0
                ? "is-positive"
                : change < 0
                    ? "is-negative"
                    : "is-neutral");

        changeElement.textContent =
            `${change > 0 ? "+" : ""}` +
            `${formatPercent(change)}% tahmini`;

        document.getElementById(
            "fund-estimate-price")
            .textContent = formatPriceWithCurrency(
                result.estimatedPrice);

        setFundEstimateState(result.message || "", false);
    } else {
        changeElement.textContent =
            "Hesaplanamadı";

        document.getElementById(
            "fund-estimate-price")
            .textContent = "—";

        setFundEstimateState(
            result.message ||
            "Tahmin için yeterli veri bulunamadı.",
            true);
    }

    renderFundEstimateContributions(
        result.contributions);
}

function renderFundEstimateContributions(
    contributions) {
    const container =
        document.getElementById(
            "fund-estimate-contributions");

    container.replaceChildren();

    const fragment =
        document.createDocumentFragment();

    const more = document.createElement("details");
    const summary = document.createElement("summary");
    summary.textContent = `Diğer ${Math.max(0, contributions.length - 8)} kalemi göster`;
    more.appendChild(summary);
    let index = 0;
    for (const item of contributions) {
        const article =
            document.createElement("article");

        article.className =
            "fund-estimate-contribution";
        article.classList.toggle(
            "is-uncovered",
            !item.isCovered);

        const heading =
            document.createElement("div");

        const name =
            document.createElement("strong");

        name.textContent = item.categoryName;

        const weight =
            document.createElement("span");

        weight.textContent =
            `%${formatPercent(item.weightPercent)}`;

        heading.append(name, weight);

        const detail =
            document.createElement("small");

        if (item.isCovered) {
            const proxyChange =
                Number(item.proxyChangePercent);

            const contribution =
                Number(item.contributionPercent);

            detail.textContent =
                `${item.proxySymbol}: ` +
                `${proxyChange > 0 ? "+" : ""}` +
                `${formatPercent(proxyChange)}% · ` +
                `etki ${contribution > 0 ? "+" : ""}` +
                `${formatPercent(contribution)} puan`;
        } else {
            detail.textContent =
                "Güncel ve kullanılabilir fiyat yok; tahmine dahil edilmedi.";
        }

        article.append(heading, detail);
        if (index++ < 8) fragment.appendChild(article);
        else more.appendChild(article);
    }

    if (contributions.length > 8) fragment.appendChild(more);
    container.appendChild(fragment);
}

async function loadFundEstimateHistory(signal) {
    const section =
        document.getElementById(
            "fund-estimate-accuracy");

    section.hidden = false;

    try {
        const response =
            await fetch(
                page.dataset.estimateHistoryUrl,
                {
                    method: "GET",
                    credentials: "same-origin",
                    signal,
                    headers: {
                        Accept: "application/json"
                    }
                });

        const result =
            await response.json()
                .catch(() => null);

        if (!response.ok) {
            throw new Error(
                getErrorMessage(
                    result,
                    `Tahmin geçmişi alınamadı: ${response.status}`));
        }

        if (!result || !Array.isArray(result.items)) {
            throw new Error(
                "Sunucudan geçersiz tahmin geçmişi geldi.");
        }

        renderFundEstimateHistory(result);
    } catch (error) {
        if (error.name === "AbortError") {
            return;
        }

        setFundEstimateHistoryState(
            error.message ||
            "Tahmin geçmişi yüklenemedi.",
            true);
    }
}

function renderFundEstimateHistory(result) {
    document.getElementById(
        "fund-estimate-evaluated-count")
        .textContent = String(
            result.evaluatedCount || 0);

    document.getElementById(
        "fund-estimate-mean-error")
        .textContent = result.meanAbsoluteErrorPercent === null
            ? "—"
            : `${formatPercent(
                result.meanAbsoluteErrorPercent)} puan`;

    document.getElementById(
        "fund-estimate-hit-rate")
        .textContent = result.withinOnePercentRate === null
            ? "—"
            : `%${formatPercent(
                result.withinOnePercentRate)}`;

    const list =
        document.getElementById(
            "fund-estimate-history-list");

    list.replaceChildren();

    if (result.items.length === 0) {
        setFundEstimateHistoryState(
            "Henüz yeni türde kayıt yok. Eski, türü bilinmeyen tahminler kapanış başarısına dahil edilmez.",
            false);
        return;
    }

    const fragment =
        document.createDocumentFragment();

    for (const item of result.items) {
        const row = document.createElement("article");
        const date = document.createElement("time");
        const kind = document.createElement("span");
        const estimate = document.createElement("span");
        const actual = document.createElement("span");
        const error = document.createElement("strong");

        date.textContent = formatDate(item.targetDate);
        date.dateTime = item.targetDate;
        kind.textContent = item.kind === "closing" ? "Seans sonu · sabit" : "Gün içi";
        if (item.kind !== "closing" && !result.items.some(other =>
            other.targetDate === item.targetDate && other.kind === "closing")) {
            const missing = document.createElement("small");
            missing.textContent = "Seans sonu kaydı yok";
            kind.appendChild(missing);
        }
        row.classList.toggle("is-closing", item.kind === "closing");
        estimate.textContent =
            `Tahmin ${formatSignedPercent(
                item.estimatedChangePercent)} · ${formatPriceWithCurrency(item.estimatedPrice)}`;
        const detail = document.createElement("small");
        detail.textContent = `Kayıt: ${formatDateTime(item.calculatedAtUtc)} · Kapsam %${formatPercent(item.coveragePercent)}`;
        estimate.appendChild(detail);
        actual.textContent = item.actualChangePercent === null
            ? (item.status === "missing-official" ? "Hedef tarihin resmî fiyatı bulunamadı" : "Resmî fiyat bekleniyor")
            : `Gerçekleşen ${formatSignedPercent(item.actualChangePercent)} · ${formatPriceWithCurrency(item.actualPrice)}`;
        error.textContent = item.absoluteErrorPercent === null ? "—"
            : `${formatPercent(item.absoluteErrorPercent)} puan hata`;

        row.append(date, kind, estimate, actual, error);
        fragment.appendChild(row);
    }

    list.appendChild(fragment);
    setFundEstimateHistoryState(result.evaluatedCount === 0
        ? "Henüz ölçülmüş seans sonu kaydı yok; başarı oranı hesaplanmadı." : "", false);
}

function setFundEstimateHistoryState(message, isError) {
    const element =
        document.getElementById(
            "fund-estimate-history-state");

    element.hidden = message === "";
    element.textContent = message;
    element.classList.toggle("is-error", isError);
}

function formatSignedPercent(value) {
    const numberValue = Number(value);

    return `${numberValue > 0 ? "+" : ""}` +
        `${formatPercent(numberValue)}%`;
}

function setFundEstimateState(message, isError) {
    const element =
        document.getElementById(
            "fund-estimate-state");

    element.hidden = message === "";
    element.textContent = message;
    element.classList.toggle(
        "is-error",
        isError);
}

async function loadFundPortfolio() {
    state.portfolioController?.abort();

    const currentController =
        new AbortController();

    state.portfolioController =
        currentController;

    try {
        const response =
            await fetch(
                page.dataset.fundPortfolioUrl,
                {
                    method: "GET",
                    credentials: "same-origin",
                    signal: currentController.signal,
                    headers: {
                        Accept: "application/json"
                    }
                });

        const result =
            await response.json()
                .catch(() => null);

        if (state.portfolioController
            !== currentController) {
            return;
        }

        if (!response.ok) {
            throw new Error(
                response.status === 404
                    ? "Varlık bulunamadı."
                    : getErrorMessage(
                        result,
                        `KAP portföyü alınamadı: ${response.status}`));
        }

        if (!result ||
            !Array.isArray(result.holdings)) {
            throw new Error(
                "Sunucudan geçersiz KAP portföy verisi geldi.");
        }

        renderFundPortfolio(result);
    } catch (error) {
        if (error.name === "AbortError" ||
            state.portfolioController
                !== currentController) {
            return;
        }

        const panel =
            document.getElementById(
                "fund-portfolio-panel");

        panel.hidden = false;
        setFundPortfolioState(
            error.message ||
            "KAP portföyü yüklenemedi.",
            true);
    } finally {
        if (state.portfolioController
            === currentController) {
            state.portfolioController = null;
        }
    }
}

function renderFundPortfolio(result) {
    const panel =
        document.getElementById(
            "fund-portfolio-panel");

    if (!result.isSupported) {
        panel.hidden = true;
        return;
    }

    panel.hidden = false;

    const sourceLink =
        document.getElementById(
            "fund-portfolio-source-link");

    sourceLink.hidden = !result.notificationUrl;

    if (result.notificationUrl) {
        sourceLink.href = result.notificationUrl;
    }

    document.getElementById(
        "fund-portfolio-report-date")
        .textContent = result.reportDate
            ? formatDate(result.reportDate)
            : "—";

    document.getElementById(
        "fund-portfolio-matched-weight")
        .textContent = Number.isFinite(
            Number(result.parsedWeightPercent))
            ? `%${formatPercent(
                result.parsedWeightPercent)}`
            : "—";

    document.getElementById(
        "fund-portfolio-age")
        .textContent = Number.isFinite(
            Number(result.reportAgeDays))
            ? `${Number(result.reportAgeDays)} gün`
            : "—";

    renderFundPortfolioHoldings(
        result.holdings);

    if (!result.isAvailable ||
        result.holdings.length === 0) {
        setFundPortfolioState(
            result.message ||
            "KAP raporunda eşleşen hisse bulunamadı.",
            true);
        return;
    }

    const publishedText =
        result.publishedAtUtc
            ? ` · KAP yayını ${formatDateTime(
                result.publishedAtUtc)}`
            : "";
    const linkedHoldingCount =
        result.holdings.filter(holding =>
            holding.marketAssetId).length;
    const linkedText = linkedHoldingCount
        < result.holdings.length
        ? ` · ${linkedHoldingCount} tanesi ` +
          "MiniMola varlığına bağlandı"
        : "";

    setFundPortfolioState(
        `${result.holdings.length} hisse okundu` +
        linkedText +
        publishedText + (result.message ? ` · ${result.message}` : ""),
        false);
}

function renderFundPortfolioHoldings(holdings) {
    const container =
        document.getElementById(
            "fund-portfolio-holdings");

    container.replaceChildren();

    if (holdings.length === 0) {
        return;
    }

    const fragment =
        document.createDocumentFragment();

    for (const holding of holdings) {
        const row = document.createElement("article");
        const identity = document.createElement("div");
        const symbol = holding.marketAssetId
            ? document.createElement("a")
            : document.createElement("strong");
        const name = document.createElement("small");
        const weight = document.createElement("span");
        const meter = document.createElement("i");

        row.className = "fund-portfolio-holding";
        identity.className = "fund-portfolio-holding-identity";
        symbol.textContent = holding.symbol;

        if (holding.marketAssetId) {
            symbol.href =
                `${page.dataset.marketDetailBaseUrl}` +
                `${holding.marketAssetId}`;
        }

        name.textContent = holding.name;
        weight.textContent =
            `%${formatPercent(holding.weightPercent)}`;
        meter.style.setProperty(
            "--holding-weight",
            `${Math.min(
                Math.max(Number(holding.weightPercent), 0),
                100)}%`);

        identity.append(symbol, name);
        row.append(identity, weight, meter);
        fragment.appendChild(row);
    }

    container.appendChild(fragment);
}

function setFundPortfolioState(message, isError) {
    const element =
        document.getElementById(
            "fund-portfolio-state");

    element.hidden = message === "";
    element.textContent = message;
    element.classList.toggle("is-error", isError);
}

function renderAssetSummary(result) {
    state.currency =
        result.quoteCurrency || "TRY";

    document.getElementById("market-detail-symbol")
        .textContent = result.symbol;

    document.getElementById("market-detail-type")
        .textContent =
            assetTypeNames[result.assetType] || "Piyasa";

    document.getElementById("market-detail-name")
        .textContent = result.name;

    document.getElementById("market-detail-market")
        .textContent =
            `${result.marketCode} · ` +
            `${result.quoteCurrency}`;

    document.title =
        `${result.symbol} | MiniMola`;

    document.getElementById("market-detail-price")
        .textContent = result.price === null
            ? "Fiyat bekleniyor"
            : `${formatPrice(result.price)} ` +
              `${result.quoteCurrency}`;

    renderDailyChange(
        result.dailyChangePercent);

    const observedElement =
        document.getElementById(
            "market-detail-observed");

    observedElement.textContent =
        result.priceObservedAtUtc
            ? `${result.priceSource || "Piyasa"} · ` +
              `${formatDateTime(result.priceObservedAtUtc)}`
            : "Fiyat zamanı bekleniyor";

    document.getElementById("market-history-source")
        .textContent = result.historySource
            ? `Grafik kaynağı: ${result.historySource}`
            : "Grafik kaynağı bulunamadı";

    document.getElementById("market-history-interval")
        .textContent =
            `Veri aralığı: ${result.interval}`;
}

function renderDailyChange(value) {
    const element =
        document.getElementById(
            "market-detail-change");

    element.classList.remove(
        "is-positive",
        "is-negative",
        "is-neutral");

    if (value === null ||
        !Number.isFinite(Number(value))) {
        element.classList.add("is-neutral");
        element.textContent = "Günlük değişim yok";
        return;
    }

    const numberValue = Number(value);

    element.classList.add(
        numberValue > 0
            ? "is-positive"
            : numberValue < 0
                ? "is-negative"
                : "is-neutral");

    element.textContent =
        `${numberValue > 0 ? "+" : ""}` +
        `${formatPercent(numberValue)}% bugün`;
}

function renderChart(points) {
    destroyChart();

    const container =
        document.getElementById(
            "market-price-chart");

    const chart =
        createChart(container, {
            width: Math.max(
                container.clientWidth,
                300),
            height: container.clientHeight || 430,
            layout: {
                background: {
                    type: ColorType.Solid,
                    color: "#fbfdfd"
                },
                textColor: "#6d8288",
                fontFamily: "inherit"
            },
            grid: {
                vertLines: {
                    color: "rgba(8, 59, 73, 0.055)"
                },
                horzLines: {
                    color: "rgba(8, 59, 73, 0.055)"
                }
            },
            crosshair: {
                mode: CrosshairMode.Normal
            },
            rightPriceScale: {
                borderColor: "rgba(8, 59, 73, 0.12)"
            },
            timeScale: {
                borderColor: "rgba(8, 59, 73, 0.12)",
                timeVisible: state.range === "1w",
                secondsVisible: false
            },
            localization: {
                locale: "tr-TR",
                priceFormatter: value =>
                    formatChartPrice(value)
            }
        });

    const hasCandles =
        points.every(point =>
            point.open !== null &&
            point.high !== null &&
            point.low !== null);

    if (hasCandles) {
        const series =
            chart.addSeries(
                CandlestickSeries,
                {
                    upColor: "#18a477",
                    downColor: "#d8675b",
                    wickUpColor: "#18a477",
                    wickDownColor: "#d8675b",
                    borderVisible: false
                });

        series.setData(
            points.map(point => ({
                time: Number(point.time),
                open: Number(point.open),
                high: Number(point.high),
                low: Number(point.low),
                close: Number(point.close)
            })));
    } else {
        const series =
            chart.addSeries(
                LineSeries,
                {
                    color: "#0aa39a",
                    lineWidth: 3,
                    crosshairMarkerRadius: 4
                });

        series.setData(
            points.map(point => ({
                time: Number(point.time),
                value: Number(point.close)
            })));
    }

    addMovingAverageSeries(
        chart,
        points,
        20,
        "#e59a38");

    addMovingAverageSeries(
        chart,
        points,
        50,
        "#7668c8");

    const volumeData =
        points
            .filter(point =>
                point.volume !== null &&
                Number(point.volume) > 0)
            .map(point => ({
                time: Number(point.time),
                value: Number(point.volume),
                color: "rgba(10, 163, 154, 0.24)"
            }));

    if (volumeData.length > 0) {
        const volumeSeries =
            chart.addSeries(
                HistogramSeries,
                {
                    priceFormat: {
                        type: "volume"
                    },
                    priceScaleId: "",
                    lastValueVisible: false,
                    priceLineVisible: false
                });

        volumeSeries.priceScale()
            .applyOptions({
                scaleMargins: {
                    top: 0.82,
                    bottom: 0
                }
            });

        volumeSeries.setData(volumeData);
    }

    chart.timeScale().fitContent();

    const resizeObserver =
        new ResizeObserver(entries => {
            const entry = entries[0];

            if (!entry) {
                return;
            }

            chart.applyOptions({
                width: Math.max(
                    Math.floor(
                        entry.contentRect.width),
                    300)
            });
        });

    resizeObserver.observe(container);

    state.chart = chart;
    applyChartTheme(chart);
    state.resizeObserver = resizeObserver;
}

function addMovingAverageSeries(
    chart,
    points,
    period,
    color) {
    const data =
        calculateSmaSeries(
            points,
            period);

    if (data.length === 0) {
        return;
    }

    const series =
        chart.addSeries(
            LineSeries,
            {
                title: `SMA${period}`,
                color,
                lineWidth: 2,
                priceLineVisible: false,
                lastValueVisible: true,
                crosshairMarkerVisible: false
            });

    series.setData(data);
}

function calculateSmaSeries(points, period) {
    if (points.length < period) {
        return [];
    }

    const result = [];
    let sum = 0;

    for (let index = 0;
         index < points.length;
         index++) {
        sum += Number(points[index].close);

        if (index >= period) {
            sum -= Number(
                points[index - period].close);
        }

        if (index >= period - 1) {
            result.push({
                time: Number(points[index].time),
                value: sum / period
            });
        }
    }

    return result;
}

function renderAnalysis(result) {
    if (result.quoteCurrency) {
        state.currency = result.quoteCurrency;
    }

    const signalElement =
        document.getElementById(
            "market-analysis-signal");

    signalElement.className =
        "market-signal " +
        `is-${result.signalCode || "unavailable"}`;

    signalElement.textContent =
        result.signalLabel || "Hesaplanamadı";

    document.getElementById(
        "market-analysis-confidence")
        .textContent = result.isAvailable
            ? `Güven seviyesi: %${result.confidence}`
            : "Güven seviyesi hesaplanamadı";

    document.getElementById(
        "market-analysis-score")
        .textContent = result.isAvailable
            ? `${result.score}/100`
            : "—";

    document.getElementById(
        "market-analysis-meter-fill")
        .style.width = result.isAvailable
            ? `${Math.max(0, Math.min(100, result.score))}%`
            : "0%";

    document.getElementById(
        "market-analysis-summary")
        .textContent = result.summary;

    const container =
        document.getElementById(
            "market-analysis-indicators");

    container.replaceChildren();

    if (!result.isAvailable) {
        setAnalysisState(result.summary, true);
        return;
    }

    const fragment =
        document.createDocumentFragment();

    for (const indicator of result.indicators) {
        fragment.appendChild(
            createIndicatorCard(indicator));
    }

    container.appendChild(fragment);
    setAnalysisState("", false);
}

function renderUnavailableAnalysis(message) {
    renderAnalysis({
        isAvailable: false,
        quoteCurrency: state.currency,
        signalCode: "unavailable",
        signalLabel: "Hesaplanamadı",
        score: 50,
        confidence: 0,
        summary: message,
        indicators: []
    });
}

function createIndicatorCard(indicator) {
    const article =
        document.createElement("article");

    article.className =
        "market-indicator-card";

    const header =
        document.createElement("header");

    const title =
        document.createElement("h3");

    title.textContent = indicator.name;

    const status =
        document.createElement("span");

    status.className =
        "market-indicator-status";

    const normalizedStatus =
        String(indicator.status || "")
            .toLocaleLowerCase("tr-TR");

    if (normalizedStatus.includes("olumlu")) {
        status.classList.add("is-positive");
    } else if (normalizedStatus.includes("olumsuz") ||
               normalizedStatus.includes("zayıf") ||
               normalizedStatus.includes("aşırı alım")) {
        status.classList.add("is-negative");
    }

    status.textContent = indicator.status;
    header.append(title, status);

    const value =
        document.createElement("strong");

    value.textContent =
        formatIndicatorValue(indicator);

    const description =
        document.createElement("p");

    description.textContent =
        indicator.description;

    article.append(
        header,
        value,
        description);

    return article;
}

function formatIndicatorValue(indicator) {
    if (indicator.value === null ||
        !Number.isFinite(Number(indicator.value))) {
        return "—";
    }

    if (indicator.code === "rsi-14") {
        return Number(indicator.value)
            .toFixed(2);
    }

    if (indicator.code === "macd") {
        return new Intl.NumberFormat(
            "tr-TR",
            {
                maximumFractionDigits: 4
            }).format(Number(indicator.value));
    }

    return formatPriceWithCurrency(
        indicator.value);
}

function setAnalysisState(message, isError) {
    const element =
        document.getElementById(
            "market-analysis-state");

    element.hidden = message === "";
    element.textContent = message;
    element.classList.toggle(
        "is-error",
        isError);
}

function renderStats(points) {
    const closeValues =
        points.map(point =>
            Number(point.close));

    const highValues =
        points.map(point =>
            Number(point.high ?? point.close));

    const lowValues =
        points.map(point =>
            Number(point.low ?? point.close));

    const first = closeValues[0];
    const last = closeValues[closeValues.length - 1];
    const change = first === 0
        ? null
        : ((last - first) / first) * 100;

    document.getElementById("market-stat-first")
        .textContent = formatPriceWithCurrency(first);

    document.getElementById("market-stat-high")
        .textContent =
            formatPriceWithCurrency(
                Math.max(...highValues));

    document.getElementById("market-stat-low")
        .textContent =
            formatPriceWithCurrency(
                Math.min(...lowValues));

    const changeElement =
        document.getElementById(
            "market-stat-change");

    changeElement.classList.remove(
        "is-positive",
        "is-negative");

    if (change === null ||
        !Number.isFinite(change)) {
        changeElement.textContent = "—";
        return;
    }

    if (change > 0) {
        changeElement.classList.add("is-positive");
    } else if (change < 0) {
        changeElement.classList.add("is-negative");
    }

    changeElement.textContent =
        `${change > 0 ? "+" : ""}` +
        `${formatPercent(change)}%`;
}

function resetStats() {
    for (const id of [
        "market-stat-first",
        "market-stat-high",
        "market-stat-low",
        "market-stat-change"
    ]) {
        const element =
            document.getElementById(id);

        element.textContent = "—";
        element.classList.remove(
            "is-positive",
            "is-negative");
    }
}

function destroyChart() {
    state.resizeObserver?.disconnect();
    state.resizeObserver = null;

    state.chart?.remove();
    state.chart = null;

    document.getElementById("market-price-chart")
        .replaceChildren();
}

function setLoadingState(isLoading) {
    const buttons =
        document.querySelectorAll(
            ".market-range-buttons button");

    for (const button of buttons) {
        button.disabled = isLoading;
    }

    if (isLoading) {
        setChartState(
            "Grafik hazırlanıyor...",
            false);
    }
}

function setChartState(message, isError) {
    const element =
        document.getElementById(
            "market-chart-state");

    element.hidden = message === "";
    element.textContent = message;
    element.classList.toggle(
        "is-error",
        isError);
}

function formatPriceWithCurrency(value) {
    return `${formatPrice(value)} ${state.currency}`;
}

function formatPrice(value) {
    const numberValue = Number(value);

    if (!Number.isFinite(numberValue)) {
        return "—";
    }

    return new Intl.NumberFormat(
        "tr-TR",
        {
            minimumFractionDigits:
                numberValue < 1 ? 4 : 2,
            maximumFractionDigits:
                numberValue < 1 ? 8 : 4
        }).format(numberValue);
}

function formatChartPrice(value) {
    return formatPrice(value);
}

function formatPercent(value) {
    return new Intl.NumberFormat(
        "tr-TR",
        {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(value);
}

function formatDateTime(value) {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "Zaman bilinmiyor";
    }

    return new Intl.DateTimeFormat(
        "tr-TR",
        {
            dateStyle: "short",
            timeStyle: "short"
        }).format(date);
}

function formatDate(value) {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "Tarih bilinmiyor";
    }

    return new Intl.DateTimeFormat(
        "tr-TR",
        {
            dateStyle: "short"
        }).format(date);
}

function getErrorMessage(result, fallback) {
    if (result &&
        typeof result.detail === "string") {
        return result.detail;
    }

    if (result &&
        typeof result.title === "string") {
        return result.title;
    }

    return fallback;
}

function handleInitialError(error) {
    setChartState(
        error.message ||
        "Varlık detay ekranı başlatılamadı.",
        true);
}
