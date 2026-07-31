const page = document.getElementById(
    "bubble-game-page");

let gameStatus = null;
let score = 0;
let remainingSeconds = 0;
let gameRunning = false;
let spawnTimer = null;
let countdownTimer = null;

if (page) {
    initializeGame().catch(showFatalError);
}

async function initializeGame() {
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

    updateStatusInformation();

    const startButton = document.getElementById(
        "bubble-start-button");

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
        "bubble-overlay-title",
        gameStatus.rewardClaimed
            ? "Bugünkü ödülünü aldın"
            : "Hazır mısın?");

    setText(
        "bubble-overlay-message",
        gameStatus.message);

    showGameMessage(
        gameStatus.message,
        gameStatus.rewardClaimed ? "success" : null);
}

function updateStatusInformation() {
    setText(
        "bubble-time",
        gameStatus.durationSeconds);

    setText(
        "bubble-score",
        0);

    setText(
        "bubble-target",
        gameStatus.targetScore);

    setText(
        "bubble-reward-points",
        gameStatus.rewardPoints);

    setText(
        "bubble-point-balance",
        gameStatus.pointBalance.toLocaleString("tr-TR"));
}

function startGame() {
    if (!gameStatus || gameRunning) {
        return;
    }

    clearGameTimers();
    removeAllBubbles();

    score = 0;
    remainingSeconds = gameStatus.durationSeconds;
    gameRunning = true;

    setText("bubble-score", score);
    setText("bubble-time", remainingSeconds);

    document
        .getElementById("bubble-game-overlay")
        ?.classList.add("is-hidden");

    showGameMessage(
        `${gameStatus.targetScore} baloncuğa ulaş!`);

    spawnBubble();

    spawnTimer = window.setInterval(
        spawnBubble,
        430);

    countdownTimer = window.setInterval(
        updateCountdown,
        1000);
}

function updateCountdown() {
    if (!gameRunning) {
        return;
    }

    remainingSeconds--;
    setText("bubble-time", remainingSeconds);

    if (remainingSeconds <= 0) {
        void finishGame(
            score >= gameStatus.targetScore);
    }
}

function spawnBubble() {
    if (!gameRunning) {
        return;
    }

    const board = document.getElementById(
        "bubble-game-board");

    if (!board) {
        return;
    }

    const bubble = document.createElement("button");
    const size = randomNumber(48, 92);
    const left = randomNumber(3, 91);
    const duration = randomNumber(4800, 7600);

    bubble.type = "button";
    bubble.className = "game-bubble";
    bubble.setAttribute(
        "aria-label",
        "Baloncuğu patlat");

    bubble.style.setProperty(
        "--bubble-size",
        `${size}px`);

    bubble.style.setProperty(
        "--bubble-left",
        `${left}%`);

    bubble.style.setProperty(
        "--bubble-duration",
        `${duration}ms`);

    bubble.addEventListener(
        "click",
        () => popBubble(bubble),
        { once: true });

    bubble.addEventListener(
        "animationend",
        () => bubble.remove(),
        { once: true });

    board.appendChild(bubble);
}

function popBubble(bubble) {
    if (!gameRunning
        || bubble.classList.contains("is-popped")) {
        return;
    }

    bubble.classList.add("is-popped");

    score++;
    setText("bubble-score", score);

    showGameMessage(
        `${score} / ${gameStatus.targetScore} baloncuk`);

    if (score >= gameStatus.targetScore) {
        void finishGame(true);
    }
}

async function finishGame(targetReached) {
    if (!gameRunning) {
        return;
    }

    gameRunning = false;

    clearGameTimers();
    removeAllBubbles();

    const overlay = document.getElementById(
        "bubble-game-overlay");

    const startButton = document.getElementById(
        "bubble-start-button");

    overlay?.classList.remove("is-hidden");

    if (startButton) {
        startButton.disabled = true;
    }

    if (!targetReached) {
        const missing =
            gameStatus.targetScore - score;

        setText(
            "bubble-overlay-title",
            "Süre doldu");

        setText(
            "bubble-overlay-message",
            `${missing} baloncuk daha patlatman gerekiyordu.`);

        showGameMessage(
            "Hedefe ulaşamadın, tekrar deneyebilirsin.",
            "error");

        prepareReplayButton();
        return;
    }

    setText(
        "bubble-overlay-title",
        "Harika!");

    if (gameStatus.rewardClaimed) {
        setText(
            "bubble-overlay-message",
            `Hedefe ulaştın! Bugünkü ödülünü daha önce almıştın.`);

        showGameMessage(
            `Skorun: ${score}. Yarın yeniden ödül kazanabilirsin.`,
            "success");

        prepareReplayButton();
        return;
    }

    setText(
        "bubble-overlay-message",
        "Ödülün hesabına aktarılıyor...");

    await claimReward();

    prepareReplayButton();
}

async function claimReward() {
    const tokenInput = document.querySelector(
        'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        throw new Error(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.");
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
                    score
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
            "bubble-point-balance",
            result.pointBalance.toLocaleString("tr-TR"));

        setText(
            "bubble-overlay-message",
            result.message);

        showGameMessage(
            result.message,
            "success");
    }
    catch (error) {
        const message =
            error instanceof Error
                ? error.message
                : "Ödül alınırken bir sorun oluştu.";

        setText(
            "bubble-overlay-message",
            message);

        showGameMessage(
            message,
            "error");
    }
}

function prepareReplayButton() {
    const startButton = document.getElementById(
        "bubble-start-button");

    if (!startButton) {
        return;
    }

    startButton.disabled = false;
    startButton.textContent = "Tekrar oyna";
}

function clearGameTimers() {
    if (spawnTimer !== null) {
        window.clearInterval(spawnTimer);
        spawnTimer = null;
    }

    if (countdownTimer !== null) {
        window.clearInterval(countdownTimer);
        countdownTimer = null;
    }
}

function removeAllBubbles() {
    document
        .querySelectorAll(".game-bubble")
        .forEach(bubble => bubble.remove());
}

function showGameMessage(
    message,
    type = null) {

    const element = document.getElementById(
        "bubble-game-message");

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

function randomNumber(minimum, maximum) {
    return Math.floor(
        Math.random() * (maximum - minimum + 1))
        + minimum;
}

function showFatalError(error) {
    console.error(error);

    const message =
        error instanceof Error
            ? error.message
            : "Oyun yüklenirken bir sorun oluştu.";

    setText(
        "bubble-overlay-title",
        "Bir sorun oluştu");

    setText(
        "bubble-overlay-message",
        message);

    showGameMessage(
        message,
        "error");
}