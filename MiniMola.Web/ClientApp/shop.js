const shopPage = document.getElementById("fish-shop");

if (shopPage) {
    loadShop().catch(handleInitialError);
}

async function loadShop() {
    const response = await fetch(shopPage.dataset.apiUrl, {
        method: "GET",
        credentials: "same-origin",
        headers: {
            Accept: "application/json"
        }
    });

    if (!response.ok) {
        throw new Error(
            `Mağaza verisi alınamadı: ${response.status}`);
    }

    const shop = await response.json();

    updateSummary(shop);
    renderShopItems(shop.items);
}

function updateSummary(shop) {
    setText(
        "shop-point-balance",
        shop.pointBalance.toLocaleString("tr-TR"));

    setText(
        "shop-aquarium-level",
        shop.aquariumLevel);

    setText(
        "shop-placed-count",
        shop.placedFishCount);

    setText(
        "shop-capacity",
        shop.aquariumCapacity);
}

function renderShopItems(items) {
    const grid = document.getElementById("fish-shop-grid");
    grid.replaceChildren();

    for (const item of items) {
        grid.appendChild(createFishCard(item));
    }
}

function createFishCard(item) {
    const card = createElement("article", "fish-card");

    const preview = createElement("div", "fish-preview");
    const fish = createElement("div", "shop-fish");
    const body = createElement("div", "shop-fish-body");
    const tail = createElement("div", "shop-fish-tail");
    const eye = createElement("div", "shop-fish-eye");

    const palette = getFishPalette(item.assetKey);

    fish.style.setProperty(
        "--fish-body",
        palette.body);

    fish.style.setProperty(
        "--fish-tail",
        palette.tail);

    fish.append(tail, body, eye);
    preview.appendChild(fish);

    const content = createElement(
        "div",
        "fish-card-content");

    const topLine = createElement(
        "div",
        "fish-card-topline");

    const rarity = createElement(
        "span",
        "fish-rarity",
        translateRarity(item.rarity));

    const owned = createElement(
        "span",
        "fish-owned",
        `Sende: ${item.ownedCount}`);

    topLine.append(rarity, owned);

    const title = createElement(
        "h2",
        "",
        item.name);

    const description = createElement(
        "p",
        "",
        item.description);

    const price = createElement(
        "div",
        "fish-price");

    price.innerHTML = `
        <svg viewBox="0 0 18 24"
             width="14"
             height="19"
             aria-hidden="true">
            <path d="M9 1S2 11 2 16c0 4 3 7 7 7s7-3 7-7C16 11 9 1 9 1Z"
                  fill="currentColor"></path>
        </svg>
        <span>${item.price.toLocaleString("tr-TR")} Damla</span>`;

    const button = createElement(
        "button",
        "buy-fish-button");

    button.type = "button";
    button.disabled = !item.canPurchase;

    button.textContent = item.canPurchase
        ? "Satın al"
        : item.lockedReason ?? "Kilitli";

    button.addEventListener("click", () => {
        purchaseFish(item, button);
    });

    content.append(
        topLine,
        title,
        description,
        price,
        button);

    card.append(preview, content);

    return card;
}

async function purchaseFish(item, button) {
    const tokenInput = document.querySelector(
        '#antiforgery-form input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        showMessage(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.",
            false);
        return;
    }

    const originalText = button.textContent;

    button.disabled = true;
    button.textContent = "Akvaryuma ekleniyor...";

    try {
        const response = await fetch(
            `${shopPage.dataset.apiUrl}/${item.id}`,
            {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json",
                    "X-CSRF-TOKEN": tokenInput.value
                }
            });

        const result = await response
            .json()
            .catch(() => null);

        if (!response.ok) {
            throw new Error(
                result?.message
                ?? `Satın alma başarısız: ${response.status}`);
        }

        showMessage(result.message, true);

        await loadShop();
    } catch (error) {
        console.error(error);

        showMessage(
            error.message
            ?? "Satın alma sırasında bir sorun oluştu.",
            false);

        button.disabled = false;
        button.textContent = originalText;
    }
}

function showMessage(message, isSuccess) {
    const element =
        document.getElementById("shop-message");

    element.textContent = message;
    element.hidden = false;

    element.classList.toggle(
        "success",
        isSuccess);

    element.classList.toggle(
        "error",
        !isSuccess);

    element.scrollIntoView({
        behavior: "smooth",
        block: "nearest"
    });
}

function handleInitialError(error) {
    console.error(error);

    const grid =
        document.getElementById("fish-shop-grid");

    grid.replaceChildren(
        createElement(
            "div",
            "shop-loading",
            "Mağaza yüklenirken bir sorun oluştu."));

    showMessage(
        "Balık mağazasına ulaşılamadı.",
        false);
}

function createElement(tagName, className, text) {
    const element = document.createElement(tagName);

    if (className) {
        element.className = className;
    }

    if (text !== undefined) {
        element.textContent = text;
    }

    return element;
}

function setText(elementId, value) {
    const element = document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

function translateRarity(rarity) {
    const translations = {
        Common: "Yaygın",
        Uncommon: "Sıra dışı",
        Rare: "Nadir",
        Epic: "Destansı",
        Legendary: "Efsanevi"
    };

    return translations[rarity] ?? rarity;
}

function getFishPalette(assetKey) {
    const palettes = {
        "blue-tang": {
            body: "#2f9fe4",
            tail: "#ffd34e"
        },
        "clown-fish": {
            body: "#ff8738",
            tail: "#ffb15e"
        },
        "neon-tetra": {
            body: "#39d7d0",
            tail: "#f35f72"
        },
        "betta-fish": {
            body: "#a45be0",
            tail: "#e06eb9"
        },
        "goldfish": {
            body: "#ffb52e",
            tail: "#ffd166"
        }
    };

    return palettes[assetKey] ?? {
        body: "#55c7c2",
        tail: "#8ce3d8"
    };
}
