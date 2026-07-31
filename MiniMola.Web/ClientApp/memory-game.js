const page = document.getElementById(
    "memory-game-page");

const cardDefinitions = [
    {
        key: "fish",
        label: "Balık",
        art: `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <polygon points="25,50 7,32 7,68"
                         fill="#ffb347"/>
                <ellipse cx="57" cy="50" rx="34" ry="23"
                         fill="#28aee4"/>
                <circle cx="76" cy="43" r="4"
                        fill="#063653"/>
                <path d="M38 50 Q55 63 72 50"
                      fill="none"
                      stroke="#087da7"
                      stroke-width="4"/>
            </svg>`
    },
    {
        key: "star",
        label: "Deniz yıldızı",
        art: `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <polygon
                    points="50,8 61,36 91,36 67,55 76,87 50,68 24,87 33,55 9,36 39,36"
                    fill="#ff806d"/>
                <circle cx="42" cy="45" r="3"
                        fill="#9d463d"/>
                <circle cx="58" cy="45" r="3"
                        fill="#9d463d"/>
            </svg>`
    },
    {
        key: "shell",
        label: "Deniz kabuğu",
        art: `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <path
                    d="M16 66 C14 33 31 16 50 16 C69 16 86 33 84 66 Z"
                    fill="#f4b6d2"/>
                <path d="M50 18 V66 M34 23 L40 66 M66 23 L60 66"
                      fill="none"
                      stroke="#c96f9a"
                      stroke-width="5"
                      stroke-linecap="round"/>
                <rect x="14" y="63" width="72" height="14"
                      rx="7"
                      fill="#de8eb3"/>
            </svg>`
    },
    {
        key: "jelly",
        label: "Denizanası",
        art: `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <path
                    d="M19 50 C19 25 32 12 50 12 C68 12 81 25 81 50 Z"
                    fill="#a77be8"/>
                <path
                    d="M27 51 C25 68 39 70 33 87
                       M43 51 C42 69 52 72 47 89
                       M58 51 C61 67 53 75 59 89
                       M73 51 C77 68 65 73 70 86"
                    fill="none"
                    stroke="#875ac9"
                    stroke-width="6"
                    stroke-linecap="round"/>
                <circle cx="40" cy="36" r="3"
                        fill="#49306b"/>
                <circle cx="60" cy="36" r="3"
                        fill="#49306b"/>
            </svg>`
    },
    {
        key: "coral",
        label: "Mercan",
        art: `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <path
                    d="M50 86 V30
                       M50 53 L30 35
                       M50 65 L72 43
                       M50 46 L67 24
                       M30 35 L25 19
                       M72 43 L82 28"
                    fill="none"
                    stroke="#ff745f"
                    stroke-width="12"
                    stroke-linecap="round"
                    stroke-linejoin="round"/>
                <ellipse cx="50" cy="88" rx="34" ry="8"
                         fill="#d9c28b"/>
            </svg>`
    },
    {
        key: "turtle",
        label: "Deniz kaplumbağası",
        art: `
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="48" cy="52" rx="29" ry="24"
                         fill="#39b98a"/>
                <path d="M28 35 Q48 52 68 35
                         M25 54 Q48 65 71 54"
                      fill="none"
                      stroke="#197d62"
                      stroke-width="5"/>
                <circle cx="80" cy="49" r="11"
                        fill="#64d2a5"/>
                <circle cx="84" cy="46" r="2.5"
                        fill="#063653"/>
                <ellipse cx="23" cy="25" rx="12" ry="6"
                         fill="#64d2a5"
                         transform="rotate(35 23 25)"/>
                <ellipse cx="25" cy="78" rx="12" ry="6"
                         fill="#64d2a5"
                         transform="rotate(-35 25 78)"/>
            </svg>`
    }
];

let gameStatus = null;
let firstCard = null;
let secondCard = null;
let boardLocked = false;
let gameRunning = false;
let matchedPairs = 0;
let moveCount = 0;
let elapsedSeconds = 0;
let elapsedTimer = null;

if (page) {
    initializeMemoryGame().catch(showFatalError);
}

async function initializeMemoryGame() {
    const response = await fetch(
        page.dataset.statusUrl,
        {
            method: "GET",
            credentials: "same-origin",
            headers: {
                Accept: "application/json"
            }
        });

    if (!response.ok) {
        throw new Error(
            `Oyun bilgileri alınamadı: ${response.status}`);
    }

    gameStatus = await response.json();

    setText(
        "memory-pair-count",
        gameStatus.pairCount);

    setText(
        "memory-reward-points",
        gameStatus.rewardPoints);

    setText(
        "memory-point-balance",
        gameStatus.pointBalance.toLocaleString("tr-TR"));

    const startButton = document.getElementById(
        "memory-start-button");

    startButton?.addEventListener(
        "click",
        startGame);

    if (startButton) {
        startButton.disabled = false;

        startButton.textContent =
            gameStatus.rewardClaimed
                ? "Eğlence için oyna"
                : "Oyunu başlat";
    }

    setText(
        "memory-overlay-title",
        gameStatus.rewardClaimed
            ? "Bugünkü ödülünü aldın"
            : "Hafızanı test et");

    setText(
        "memory-overlay-message",
        gameStatus.message);

    showGameMessage(
        gameStatus.message,
        gameStatus.rewardClaimed
            ? "success"
            : null);
}

function startGame() {
    if (!gameStatus || gameRunning) {
        return;
    }

    stopElapsedTimer();

    firstCard = null;
    secondCard = null;
    boardLocked = false;
    gameRunning = true;
    matchedPairs = 0;
    moveCount = 0;
    elapsedSeconds = 0;

    setText("memory-match-count", matchedPairs);
    setText("memory-move-count", moveCount);
    setText("memory-elapsed-time", elapsedSeconds);

    createBoard();

    document
        .getElementById("memory-game-overlay")
        ?.classList.add("is-hidden");

    showGameMessage(
        "İlk kartını seç.");

    elapsedTimer = window.setInterval(
        () => {
            elapsedSeconds++;
            setText(
                "memory-elapsed-time",
                elapsedSeconds);
        },
        1000);
}

function createBoard() {
    const board = document.getElementById(
        "memory-board");

    if (!board) {
        return;
    }

    board.replaceChildren();

    const selectedCards = cardDefinitions
        .slice(0, gameStatus.pairCount);

    const cards = shuffle([
        ...selectedCards,
        ...selectedCards
    ]);

    cards.forEach((definition, index) => {
        board.appendChild(
            createCard(definition, index));
    });
}

function createCard(definition, index) {
    const card = document.createElement("button");

    card.type = "button";
    card.className = "memory-card";
    card.dataset.cardKey = definition.key;
    card.setAttribute(
        "aria-label",
        `${index + 1}. kapalı kart`);

    card.innerHTML = `
        <span class="memory-card-inner">
            <span class="memory-card-face memory-card-front">
            </span>

            <span class="memory-card-face memory-card-back">
                <span class="memory-card-art">
                    ${definition.art}
                </span>
            </span>
        </span>`;

    card.addEventListener(
        "click",
        () => revealCard(card, definition));

    return card;
}

function revealCard(card, definition) {
    if (!gameRunning
        || boardLocked
        || card === firstCard
        || card.classList.contains("is-matched")
        || card.classList.contains("is-flipped")) {
        return;
    }

    card.classList.add("is-flipped");
    card.setAttribute(
        "aria-label",
        definition.label);

    if (!firstCard) {
        firstCard = card;

        showGameMessage(
            "Şimdi eşini bul.");

        return;
    }

    secondCard = card;
    moveCount++;

    setText(
        "memory-move-count",
        moveCount);

    boardLocked = true;

    if (firstCard.dataset.cardKey
        === secondCard.dataset.cardKey) {
        handleMatch();
    }
    else {
        handleMismatch();
    }
}

function handleMatch() {
    const matchedFirstCard = firstCard;
    const matchedSecondCard = secondCard;

    window.setTimeout(
        () => {
            matchedFirstCard.classList.add(
                "is-matched");

            matchedSecondCard.classList.add(
                "is-matched");

            matchedPairs++;

            setText(
                "memory-match-count",
                matchedPairs);

            resetTurn();

            if (matchedPairs >= gameStatus.pairCount) {
                void finishGame();
                return;
            }

            showGameMessage(
                "Eşleşme bulundu! Devam et.",
                "success");
        },
        380);
}

function handleMismatch() {
    const unmatchedFirstCard = firstCard;
    const unmatchedSecondCard = secondCard;

    window.setTimeout(
        () => {
            unmatchedFirstCard.classList.remove(
                "is-flipped");

            unmatchedSecondCard.classList.remove(
                "is-flipped");

            unmatchedFirstCard.setAttribute(
                "aria-label",
                "Kapalı kart");

            unmatchedSecondCard.setAttribute(
                "aria-label",
                "Kapalı kart");

            resetTurn();

            showGameMessage(
                "Eşleşmedi, yeniden dene.");
        },
        850);
}

function resetTurn() {
    firstCard = null;
    secondCard = null;
    boardLocked = false;
}

async function finishGame() {
    if (!gameRunning) {
        return;
    }

    gameRunning = false;
    boardLocked = true;

    stopElapsedTimer();

    const overlay = document.getElementById(
        "memory-game-overlay");

    const startButton = document.getElementById(
        "memory-start-button");

    overlay?.classList.remove("is-hidden");

    if (startButton) {
        startButton.disabled = true;
    }

    setText(
        "memory-overlay-title",
        "Tüm eşleşmeleri buldun!");

    if (gameStatus.rewardClaimed) {
        setText(
            "memory-overlay-message",
            `${moveCount} hamlede ve `
            + `${elapsedSeconds} saniyede tamamladın. `
            + "Bugünkü ödülünü daha önce almıştın.");

        showGameMessage(
            "Yarın yeniden günlük ödül kazanabilirsin.",
            "success");

        prepareReplayButton();
        return;
    }

    setText(
        "memory-overlay-message",
        "Ödülün hesabına aktarılıyor...");

    await claimReward();

    prepareReplayButton();
}

async function claimReward() {
    const tokenInput = document.querySelector(
        'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        showRewardError(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.");
        return;
    }

    try {
        const response = await fetch(
            page.dataset.completeUrl,
            {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json",
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": tokenInput.value
                },
                body: JSON.stringify({
                    matchedPairs,
                    moveCount
                })
            });

        const result = await response
            .json()
            .catch(() => null);

        if (!response.ok) {
            throw new Error(
                result?.message
                ?? "Oyun ödülü alınamadı.");
        }

        gameStatus.rewardClaimed =
            result.rewardClaimed;

        gameStatus.pointBalance =
            result.pointBalance;

        setText(
            "memory-point-balance",
            result.pointBalance.toLocaleString("tr-TR"));

        setText(
            "memory-overlay-message",
            result.message);

        showGameMessage(
            result.message,
            "success");
    }
    catch (error) {
        showRewardError(
            error instanceof Error
                ? error.message
                : "Ödül alınırken bir sorun oluştu.");
    }
}

function showRewardError(message) {
    setText(
        "memory-overlay-message",
        message);

    showGameMessage(
        message,
        "error");
}

function prepareReplayButton() {
    const startButton = document.getElementById(
        "memory-start-button");

    if (!startButton) {
        return;
    }

    startButton.disabled = false;
    startButton.textContent = "Tekrar oyna";
}

function stopElapsedTimer() {
    if (elapsedTimer !== null) {
        window.clearInterval(elapsedTimer);
        elapsedTimer = null;
    }
}

function shuffle(items) {
    const shuffledItems = [...items];

    for (let index = shuffledItems.length - 1;
        index > 0;
        index--) {
        const randomIndex = Math.floor(
            Math.random() * (index + 1));

        [
            shuffledItems[index],
            shuffledItems[randomIndex]
        ] = [
                shuffledItems[randomIndex],
                shuffledItems[index]
            ];
    }

    return shuffledItems;
}

function showGameMessage(
    message,
    type = null) {

    const element = document.getElementById(
        "memory-game-message");

    if (!element) {
        return;
    }

    element.textContent = message;

    element.classList.remove(
        "is-success",
        "is-error");

    if (type === "success") {
        element.classList.add("is-success");
    }

    if (type === "error") {
        element.classList.add("is-error");
    }
}

function setText(elementId, value) {
    const element = document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

function showFatalError(error) {
    console.error(error);

    const message =
        error instanceof Error
            ? error.message
            : "Oyun yüklenirken bir sorun oluştu.";

    setText(
        "memory-overlay-title",
        "Bir sorun oluştu");

    setText(
        "memory-overlay-message",
        message);

    showGameMessage(
        message,
        "error");
}