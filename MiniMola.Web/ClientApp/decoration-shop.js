const shopPage = document.getElementById(
    "decoration-shop");

if (shopPage) {
    loadDecorationShop().catch(handleInitialError);
}

async function loadDecorationShop() {
    const response = await fetch(
        shopPage.dataset.apiUrl,
        {
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
    renderItems(shop.items);
}

function updateSummary(shop) {
    setText(
        "decoration-point-balance",
        shop.pointBalance.toLocaleString("tr-TR"));

    setText(
        "decoration-aquarium-level",
        shop.aquariumLevel);

    setText(
        "decoration-placed-count",
        shop.placedDecorationCount);
}

function renderItems(items) {
    const grid = document.getElementById(
        "decoration-shop-grid");

    if (!grid) {
        return;
    }

    grid.replaceChildren();

    for (const item of items) {
        grid.appendChild(
            createDecorationCard(item));
    }
}

function createDecorationCard(item) {
    const card = createElement(
        "article",
        "decoration-card");

    const preview = createElement(
        "div",
        "decoration-preview");

    const art = createElement(
        "div",
        "decoration-art");

    art.innerHTML =
        getDecorationArt(item.assetKey);

    preview.appendChild(art);

    const content = createElement(
        "div",
        "decoration-card-content");

    const topLine = createElement(
        "div",
        "decoration-card-topline");

    const category = createElement(
        "span",
        "decoration-category",
        translateCategory(item.category));

    const owned = createElement(
        "span",
        "decoration-owned",
        `Sende: ${item.ownedCount}`);

    topLine.append(category, owned);

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
        "decoration-price");

    price.innerHTML = `
        <svg viewBox="0 0 20 26"
             width="16"
             height="21"
             aria-hidden="true">
            <path d="M10 1 C10 1 2 12 2 17
                     C2 22 5.5 25 10 25
                     C14.5 25 18 22 18 17
                     C18 12 10 1 10 1 Z"
                  fill="currentColor">
            </path>
        </svg>

        <span>
            ${item.price.toLocaleString("tr-TR")}
            Damla
        </span>`;

    const button = createElement(
        "button",
        "buy-decoration-button");

    button.type = "button";
    button.disabled = !item.canPurchase;

    button.textContent = item.canPurchase
        ? "Satın al ve yerleştir"
        : item.lockedReason ?? "Kilitli";

    button.addEventListener(
        "click",
        () => purchaseDecoration(item, button));

    content.append(
        topLine,
        title,
        description,
        price,
        button);

    card.append(
        preview,
        content);

    return card;
}

async function purchaseDecoration(
    item,
    button) {

    const tokenInput = document.querySelector(
        '#decoration-antiforgery-form '
        + 'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        showMessage(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.",
            false);

        return;
    }

    const originalText = button.textContent;

    button.disabled = true;
    button.classList.add("is-loading");
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

        showMessage(
            result.message,
            true);

        await loadDecorationShop();
    }
    catch (error) {
        console.error(error);

        showMessage(
            error instanceof Error
                ? error.message
                : "Satın alma sırasında bir sorun oluştu.",
            false);

        button.disabled = false;
        button.textContent = originalText;
    }
    finally {
        button.classList.remove("is-loading");
    }
}

function showMessage(message, isSuccess) {
    const element = document.getElementById(
        "decoration-shop-message");

    if (!element) {
        return;
    }

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

    const grid = document.getElementById(
        "decoration-shop-grid");

    grid?.replaceChildren(
        createElement(
            "div",
            "shop-loading",
            "Dekorasyon mağazası yüklenirken "
            + "bir sorun oluştu."));

    showMessage(
        "Dekorasyon mağazasına ulaşılamadı.",
        false);
}

function createElement(
    tagName,
    className,
    text) {

    const element =
        document.createElement(tagName);

    if (className) {
        element.className = className;
    }

    if (text !== undefined) {
        element.textContent = text;
    }

    return element;
}

function setText(elementId, value) {
    const element =
        document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

function translateCategory(category) {
    const translations = {
        Plant: "Bitki",
        Coral: "Mercan",
        Rock: "Kaya",
        Ornament: "Süs",
        Structure: "Yapı",
        Lighting: "Aydınlatma"
    };

    return translations[category] ?? category;
}

function getDecorationArt(assetKey) {
    const artwork = {
        "curved-water-plant": `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="91" rx="35" ry="7"
                         fill="#b79d67"/>
                <path d="M47 90 C40 72 56 56 43 38
                         C34 26 41 14 48 8"
                      fill="none"
                      stroke="#39b98a"
                      stroke-width="10"
                      stroke-linecap="round"/>
                <path d="M59 90 C66 70 51 57 66 42
                         C75 33 72 21 68 14"
                      fill="none"
                      stroke="#64d2a5"
                      stroke-width="9"
                      stroke-linecap="round"/>
                <path d="M34 90 C28 75 38 65 30 53
                         C25 44 27 34 32 27"
                      fill="none"
                      stroke="#248e70"
                      stroke-width="8"
                      stroke-linecap="round"/>
            </svg>`,

        "pink-coral": `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="90" rx="36" ry="7"
                         fill="#b79d67"/>
                <path d="M50 88 V31
                         M50 55 L30 37
                         M50 66 L73 44
                         M50 47 L67 24
                         M30 37 L25 20
                         M73 44 L83 29"
                      fill="none"
                      stroke="#ff7f91"
                      stroke-width="11"
                      stroke-linecap="round"
                      stroke-linejoin="round"/>
            </svg>`,

        "volcanic-rock": `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="90" rx="40" ry="7"
                         fill="#b79d67"/>
                <path d="M16 86 L28 43 L45 25
                         L63 34 L84 86 Z"
                      fill="#465b64"/>
                <path d="M28 43 L49 54 L63 34
                         L72 63 L84 86 L16 86 Z"
                      fill="#344951"/>
                <circle cx="43" cy="64" r="7"
                        fill="#243941"/>
                <circle cx="66" cy="73" r="5"
                        fill="#243941"/>
            </svg>`,

        "treasure-chest": `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="90" rx="39" ry="7"
                         fill="#b79d67"/>
                <path d="M20 43 C20 27 31 19 50 19
                         C69 19 80 27 80 43 Z"
                      fill="#8c552d"/>
                <rect x="18" y="42"
                      width="64" height="43"
                      rx="5"
                      fill="#9f6232"/>
                <path d="M18 52 H82 M50 19 V85"
                      fill="none"
                      stroke="#e2b34f"
                      stroke-width="7"/>
                <rect x="43" y="54"
                      width="14" height="16"
                      rx="3"
                      fill="#f2ce67"/>
            </svg>`,

        "mini-lighthouse": `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="92" rx="36" ry="6"
                         fill="#b79d67"/>
                <path d="M32 88 L39 31 H61 L68 88 Z"
                      fill="#f4eee4"/>
                <path d="M37 50 H63 M35 67 H65"
                      stroke="#e7655d"
                      stroke-width="11"/>
                <rect x="34" y="22"
                      width="32" height="15"
                      rx="3"
                      fill="#f3c85c"/>
                <path d="M29 23 L50 10 L71 23 Z"
                      fill="#d9524b"/>
                <rect x="46" y="73"
                      width="10" height="15"
                      fill="#31566a"/>
            </svg>`,

        "moon-light": `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="91" rx="31" ry="6"
                         fill="#b79d67"/>
                <path d="M50 84 V53"
                      stroke="#425f72"
                      stroke-width="7"
                      stroke-linecap="round"/>
                <path d="M30 81 H70"
                      stroke="#425f72"
                      stroke-width="9"
                      stroke-linecap="round"/>
                <path d="M65 15 C47 18 39 34 45 49
                         C50 62 64 67 77 61
                         C67 60 59 52 57 43
                         C54 31 58 22 65 15 Z"
                      fill="#fff1a8"/>
                <circle cx="65" cy="38" r="30"
                        fill="#fff1a8"
                        opacity="0.16"/>
            </svg>`
    };

    return artwork[assetKey]
        ?? `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <circle cx="50" cy="50" r="30"
                        fill="#65d6d0"/>
            </svg>`;
}