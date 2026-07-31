const newsList = document.getElementById("news-list");
const newsStatus = document.getElementById("news-status");
const refreshButton = document.getElementById("refresh-news-button");

let activeRequestController = null;

if (newsList && newsStatus && refreshButton) {
    refreshButton.addEventListener("click", loadNews);
    loadNews();
}

async function loadNews() {
    activeRequestController?.abort();

    const requestController = new AbortController();
    activeRequestController = requestController;

    setLoadingState(true);
    newsStatus.classList.remove("is-error");
    newsStatus.textContent = "Haberler yükleniyor...";
    newsList.replaceChildren();

    try {
        const response = await fetch("/api/news/headlines", {
            method: "GET",
            headers: {
                Accept: "application/json"
            },
            cache: "no-store",
            signal: requestController.signal
        });

        if (!response.ok) {
            throw new Error(
                `Haber servisi ${response.status} koduyla yanıt verdi.`
            );
        }

        const feed = await response.json();
        const items = Array.isArray(feed.items) ? feed.items : [];

        renderNews(items, feed.sourceName);

        if (items.length === 0) {
            newsStatus.textContent = "";
            return;
        }

        const retrievedAt = formatDate(feed.retrievedAtUtc);

        newsStatus.textContent =
            `${items.length} haber gösteriliyor` +
            (retrievedAt ? ` · Son güncelleme: ${retrievedAt}` : "");
    } catch (error) {
        if (error.name === "AbortError") {
            return;
        }

        console.error("Haberler yüklenemedi:", error);

        newsStatus.classList.add("is-error");
        newsStatus.textContent =
            "Haberler şu anda yüklenemedi. Biraz sonra tekrar deneyebilirsin.";

        renderEmptyState();
    } finally {
        if (activeRequestController === requestController) {
            activeRequestController = null;
            setLoadingState(false);
        }
    }
}

function renderNews(items, sourceName) {
    if (items.length === 0) {
        renderEmptyState();
        return;
    }

    const fragment = document.createDocumentFragment();

    for (const item of items) {
        fragment.appendChild(createNewsCard(item, sourceName));
    }

    newsList.appendChild(fragment);
}

function createNewsCard(item, sourceName) {
    const article = document.createElement("article");
    article.className = "news-card";

    const meta = document.createElement("div");
    meta.className = "news-card__meta";

    const source = document.createElement("span");
    source.className = "news-card__source";
    source.textContent = sourceName || "Haber";

    const date = document.createElement("time");
    date.className = "news-card__date";
    date.textContent = formatDate(item.publishedAt) || "Yeni";

    if (item.publishedAt) {
        date.dateTime = item.publishedAt;
    }

    const title = document.createElement("h2");
    title.className = "news-card__title";
    title.textContent = item.title || "Başlıksız haber";

    const summary = document.createElement("p");
    summary.className = "news-card__summary";
    summary.textContent =
        item.summary || "Bu haber için kısa bir açıklama bulunmuyor.";

    const link = document.createElement("a");
    link.className = "news-card__link";
    link.href = item.url;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.textContent = "Haberi kaynağında oku →";

    meta.append(source, date);
    article.append(meta, title, summary, link);

    return article;
}

function renderEmptyState() {
    newsList.replaceChildren();

    const emptyState = document.createElement("div");
    emptyState.className = "news-empty";
    emptyState.textContent = "Şimdilik gösterilecek bir haber bulunamadı.";

    newsList.appendChild(emptyState);
}

function setLoadingState(isLoading) {
    refreshButton.disabled = isLoading;

    const label = refreshButton.querySelector("span");

    if (label) {
        label.textContent = isLoading
            ? "Yükleniyor..."
            : "Haberleri yenile";
    }

    refreshButton.classList.toggle("is-loading", isLoading);
}

function formatDate(value) {
    if (!value) {
        return "";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "";
    }

    return new Intl.DateTimeFormat("tr-TR", {
        day: "numeric",
        month: "short",
        hour: "2-digit",
        minute: "2-digit"
    }).format(date);
}
