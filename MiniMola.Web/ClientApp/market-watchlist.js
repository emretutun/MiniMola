const page =
    document.getElementById("market-watchlist-page");

const state = {
    assetType: null,
    search: "",
    favoritesOnly: false,
    page: 1,
    pageSize: 30,
    totalCount: 0,
    totalPages: 0,
    isLoading: false
};

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

const priceKindNames = {
    1: "Canlı",
    2: "Gecikmeli",
    3: "Resmî",
    4: "Tahmini"
};

let requestVersion = 0;
let searchTimer = null;

if (page) {
    initializeMarketWatchlist()
        .catch(handleInitialError);
}

async function initializeMarketWatchlist() {
    bindSearch();
    bindAssetTypeFilters();
    bindFavoritesFilter();
    bindPagination();

    await loadAssets();
}

function bindSearch() {
    const searchInput =
        document.getElementById("market-watchlist-search");

    searchInput.addEventListener("input", () => {
        window.clearTimeout(searchTimer);

        state.search = searchInput.value.trim();
        resetPagination();
        requestVersion += 1;
        setLoadingState(true);

        searchTimer = window.setTimeout(() => {
            loadAssets();
        }, 300);
    });
}

function bindAssetTypeFilters() {
    const buttons =
        document.querySelectorAll(
            ".market-filter-button");

    for (const button of buttons) {
        button.addEventListener("click", () => {
            for (const item of buttons) {
                item.classList.remove("is-active");
            }

            button.classList.add("is-active");

            const value =
                button.dataset.assetType;

            state.assetType =
                value === ""
                    ? null
                    : value;

            resetPagination();
            loadAssets();
        });
    }
}

function bindFavoritesFilter() {
    const button =
        document.getElementById(
            "market-watchlist-favorites");

    button.addEventListener("click", () => {
        state.favoritesOnly =
            !state.favoritesOnly;

        button.classList.toggle(
            "is-active",
            state.favoritesOnly);

        button.setAttribute(
            "aria-pressed",
            state.favoritesOnly.toString());

        const icon =
            button.querySelector("span");

        icon.textContent =
            state.favoritesOnly ? "★" : "☆";

        resetPagination();
        loadAssets();
    });
}

function bindPagination() {
    document.getElementById("market-watchlist-previous")
        .addEventListener("click", () => changePage(-1));

    document.getElementById("market-watchlist-next")
        .addEventListener("click", () => changePage(1));

    document.getElementById("market-watchlist-page-size")
        .addEventListener("change", (event) => {
            state.pageSize = Number(event.target.value);
            resetPagination();
            loadAssets();
        });
}

function changePage(offset) {
    const requestedPage = state.page + offset;

    if (state.isLoading ||
        requestedPage < 1 ||
        requestedPage > state.totalPages) {
        return;
    }

    loadAssets(requestedPage);
}

function resetPagination() {
    state.page = 1;
    state.totalCount = 0;
    state.totalPages = 0;
    document.getElementById("market-watchlist-grid")
        .replaceChildren();
    document.getElementById("market-watchlist-empty")
        .hidden = true;
}

function updatePagination() {
    const pagination =
        document.getElementById("market-watchlist-pagination");

    pagination.hidden = state.totalCount === 0;
    pagination.setAttribute("aria-busy", state.isLoading.toString());

    document.getElementById("market-watchlist-page-info")
        .textContent = state.totalCount === 0
            ? ""
            : `Sayfa ${state.page} / ${state.totalPages}`;

    document.getElementById("market-watchlist-previous")
        .disabled = state.isLoading || state.totalCount === 0 || state.page <= 1;

    document.getElementById("market-watchlist-next")
        .disabled = state.isLoading || state.totalCount === 0 || state.page >= state.totalPages;

    document.getElementById("market-watchlist-page-size")
        .disabled = state.isLoading;
}

async function loadAssets(requestedPage = state.page) {
    window.clearTimeout(searchTimer);

    const currentRequestVersion =
        ++requestVersion;

    setLoadingState(true);

    try {
        const url =
            new URL(
                page.dataset.apiUrl,
                window.location.origin);

        if (state.assetType !== null) {
            url.searchParams.set(
                "assetType",
                state.assetType);
        }

        if (state.search !== "") {
            url.searchParams.set(
                "search",
                state.search);
        }

        url.searchParams.set(
            "favoritesOnly",
            state.favoritesOnly.toString());

        url.searchParams.set(
            "page",
            requestedPage.toString());

        url.searchParams.set(
            "pageSize",
            state.pageSize.toString());

        const response =
            await fetch(url, {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json"
                }
            });

        const result =
            await response
                .json()
                .catch(() => null);

        if (requestVersion !== currentRequestVersion) {
            return;
        }

        if (!response.ok) {
            throw new Error(
                getErrorMessage(
                    result,
                    `Piyasa listesi alınamadı: ${response.status}`));
        }

        if (!result ||
            !Array.isArray(result.items)) {
            throw new Error(
                "Sunucudan geçersiz piyasa verisi geldi.");
        }

        state.totalCount =
            Number(result.totalCount) || 0;

        state.totalPages =
            Number(result.totalPages) || 0;

        state.page = state.totalCount === 0
            ? 1
            : Number(result.page) || 1;

        renderAssets(result.items);

        if (state.totalCount === 0) {
            setStatus("Varlık bulunamadı.");
        } else {
            const firstItem = (state.page - 1) * state.pageSize + 1;
            const lastItem = firstItem + result.items.length - 1;

            setStatus(
                `${state.totalCount} varlık bulundu. ` +
                `${firstItem}–${lastItem} arası gösteriliyor. ` +
                `Sayfa ${state.page}/${state.totalPages}.`);
        }
    } catch (error) {
        if (requestVersion !== currentRequestVersion) {
            return;
        }

        setStatus(
            error.message ||
            "Piyasa listesi yüklenemedi.",
            true);
    } finally {
        if (requestVersion === currentRequestVersion) {
            setLoadingState(false);
        }
    }
}

function renderAssets(assets) {
    const grid =
        document.getElementById(
            "market-watchlist-grid");

    const emptyState =
        document.getElementById(
            "market-watchlist-empty");

    grid.replaceChildren();

    emptyState.hidden =
        assets.length !== 0;

    if (assets.length === 0) {
        return;
    }

    const fragment =
        document.createDocumentFragment();

    for (const asset of assets) {
        fragment.appendChild(
            createAssetCard(asset));
    }

    grid.appendChild(fragment);
}

function createAssetCard(asset) {
    const article =
        document.createElement("article");

    article.className = "market-card";

    const header =
        document.createElement("div");

    header.className =
        "market-card-header";

    const titleContainer =
        document.createElement("div");

    const symbol =
        document.createElement("h2");

    symbol.className =
        "market-card-symbol";

    symbol.textContent =
        asset.symbol;

    const name =
        document.createElement("span");

    name.className =
        "market-card-name";

    name.textContent =
        asset.name;

    titleContainer.append(
        symbol,
        name);

    const favoriteButton =
        createFavoriteButton(asset);

    header.append(
        titleContainer,
        favoriteButton);

    article.appendChild(header);
    article.appendChild(
        createPriceElement(asset));

    if (asset.dailyChangePercent !== null) {
        article.appendChild(
            createChangeElement(
                asset.dailyChangePercent));
    }

    article.appendChild(
        createDetailLink(asset));

    article.appendChild(
        createMetadataElement(asset));

    return article;
}

function createDetailLink(asset) {
    const link =
        document.createElement("a");

    const baseUrl =
        page.dataset.detailBaseUrl ||
        "/Markets/Detail";

    link.className = "market-card-detail-link";
    link.href =
        `${baseUrl.replace(/\/$/, "")}/${asset.id}`;
    link.textContent = "Grafiği ve detayı aç";

    const arrow =
        document.createElement("span");

    arrow.setAttribute("aria-hidden", "true");
    arrow.textContent = "→";
    link.appendChild(arrow);

    return link;
}

function createFavoriteButton(asset) {
    const button =
        document.createElement("button");

    button.type = "button";

    button.className =
        "market-favorite-button";

    button.classList.toggle(
        "is-active",
        asset.isFavorite);

    button.textContent =
        asset.isFavorite ? "★" : "☆";

    button.setAttribute(
        "aria-pressed",
        asset.isFavorite.toString());

    button.setAttribute(
        "aria-label",
        asset.isFavorite
            ? `${asset.symbol} favorilerden çıkar`
            : `${asset.symbol} favorilere ekle`);

    button.title =
        asset.isFavorite
            ? "Favorilerden çıkar"
            : "Favorilere ekle";

    button.addEventListener(
        "click",
        () => toggleFavorite(
            asset,
            button));

    return button;
}

function createPriceElement(asset) {
    const container =
        document.createElement("div");

    container.className =
        "market-card-price";

    if (asset.price === null) {
        const noPrice =
            document.createElement("span");

        noPrice.className =
            "market-card-no-price";

        noPrice.textContent =
            "Fiyat bekleniyor";

        container.appendChild(noPrice);

        return container;
    }

    const price =
        document.createElement("span");

    price.textContent =
        formatPrice(asset.price);

    const currency =
        document.createElement("small");

    currency.textContent =
        asset.quoteCurrency;

    container.append(
        price,
        currency);

    return container;
}

function createChangeElement(value) {
    const change =
        document.createElement("div");

    const numberValue =
        Number(value);

    change.className =
        "market-card-change";

    if (numberValue > 0) {
        change.classList.add("is-positive");
    } else if (numberValue < 0) {
        change.classList.add("is-negative");
    } else {
        change.classList.add("is-neutral");
    }

    const prefix =
        numberValue > 0 ? "+" : "";

    change.textContent =
        `${prefix}${formatPercent(numberValue)}%`;

    return change;
}

function createMetadataElement(asset) {
    const container =
        document.createElement("div");

    container.className =
        "market-card-meta";

    appendMetadata(
        container,
        assetTypeNames[asset.assetType] ||
        "Diğer");

    appendMetadata(
        container,
        asset.priceKind === null
            ? "Henüz veri yok"
            : priceKindNames[asset.priceKind] ||
            "Fiyat");

    if (asset.source) {
        appendMetadata(
            container,
            `Kaynak: ${asset.source}`);
    }

    if (asset.observedAtUtc) {
        appendMetadata(
            container,
            `Güncelleme: ${formatObservedAt(
                asset.observedAtUtc)}`);
    }

    return container;
}

function appendMetadata(
    container,
    text) {

    const item =
        document.createElement("span");

    item.textContent = text;

    container.appendChild(item);
}

async function toggleFavorite(
    asset,
    button) {

    const token =
        document.querySelector(
            "#market-watchlist-antiforgery " +
            "input[name='__RequestVerificationToken']")
            ?.value;

    if (!token) {
        setStatus(
            "Güvenlik anahtarı bulunamadı.",
            true);

        return;
    }

    button.disabled = true;

    try {
        const endpoint =
            `${page.dataset.apiUrl}/` +
            `${asset.id}/favorite`;

        const response =
            await fetch(endpoint, {
                method:
                    asset.isFavorite
                        ? "DELETE"
                        : "POST",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json",
                    "X-CSRF-TOKEN": token
                }
            });

        if (!response.ok) {
            const result =
                await response
                    .json()
                    .catch(() => null);

            throw new Error(
                getErrorMessage(
                    result,
                    `Favori işlemi başarısız: ${response.status}`));
        }

        await loadAssets();
    } catch (error) {
        setStatus(
            error.message ||
            "Favori işlemi tamamlanamadı.",
            true);
    } finally {
        button.disabled = false;
    }
}

function formatPrice(value) {
    const numberValue =
        Number(value);

    if (!Number.isFinite(numberValue)) {
        return "-";
    }

    const absoluteValue =
        Math.abs(numberValue);

    let maximumFractionDigits = 8;

    if (absoluteValue >= 100) {
        maximumFractionDigits = 2;
    } else if (absoluteValue >= 1) {
        maximumFractionDigits = 4;
    }

    return new Intl.NumberFormat(
        "tr-TR",
        {
            maximumFractionDigits
        })
        .format(numberValue);
}

function formatPercent(value) {
    return new Intl.NumberFormat(
        "tr-TR",
        {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        })
        .format(value);
}

function formatObservedAt(value) {
    const date =
        new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "-";
    }

    return new Intl.DateTimeFormat(
        "tr-TR",
        {
            dateStyle: "short",
            timeStyle: "short"
        })
        .format(date);
}

function setLoadingState(isLoading) {
    state.isLoading = isLoading;
    updatePagination();

    const grid =
        document.getElementById(
            "market-watchlist-grid");

    grid.setAttribute(
        "aria-busy",
        isLoading.toString());

    if (isLoading) {
        setStatus(
            "Piyasa listesi güncelleniyor...");
    }
}

function setStatus(
    message,
    isError = false) {

    const status =
        document.getElementById(
            "market-watchlist-status");

    status.textContent = message;

    status.classList.toggle(
        "is-error",
        isError);
}

function getErrorMessage(
    result,
    fallback) {

    if (typeof result === "string" &&
        result.trim() !== "") {
        return result;
    }

    if (result?.detail) {
        return result.detail;
    }

    if (result?.title) {
        return result.title;
    }

    return fallback;
}

function handleInitialError(error) {
    setStatus(
        error.message ||
        "Piyasa ekranı başlatılamadı.",
        true);
}
