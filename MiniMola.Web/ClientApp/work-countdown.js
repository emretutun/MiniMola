const countdownPage =
    document.getElementById("work-countdown-page");

let countdown = null;
let remainingSeconds = 0;
let currentTime = null;
let timerId = null;
let secondsSinceSync = 0;
let isRefreshing = false;

if (countdownPage) {
    loadCountdown()
        .catch(handleInitialError);

    window.addEventListener(
        "pagehide",
        stopTimer);
}

async function loadCountdown() {
    if (isRefreshing) {
        return;
    }

    isRefreshing = true;
    stopTimer();

    try {
        const response = await fetch(
            countdownPage.dataset.apiUrl,
            {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json"
                }
            });

        const result = await response
            .json()
            .catch(() => null);

        if (!response.ok) {
            throw new Error(
                result?.detail
                ?? result?.message
                ?? `Mesai bilgisi alınamadı: ${response.status}`);
        }

        countdown = result;

        remainingSeconds = Math.max(
            0,
            Number(result.remainingSeconds) || 0);

        currentTime =
            result.currentTime
                ? new Date(result.currentTime)
                : new Date();

        secondsSinceSync = 0;

        hideMessage();
        renderCountdown();
    } finally {
        isRefreshing = false;

        if (countdown && currentTime) {
            startTimer();
        }
    }
}

function startTimer() {
    stopTimer();

    timerId = window.setInterval(
        tick,
        1000);
}

function stopTimer() {
    if (timerId !== null) {
        window.clearInterval(timerId);
        timerId = null;
    }
}

function tick() {
    if (!countdown || !currentTime) {
        return;
    }

    currentTime =
        new Date(currentTime.getTime() + 1000);

    secondsSinceSync++;

    if (countdown.isWorkingNow
        && remainingSeconds > 0) {

        remainingSeconds--;
    }

    renderLiveValues();

    const countdownFinished =
        countdown.isWorkingNow
        && remainingSeconds <= 0;

    if (countdownFinished
        || secondsSinceSync >= 60) {

        loadCountdown()
            .catch(handleRefreshError);
    }
}

function renderCountdown() {
    countdownPage.classList.toggle(
        "is-completed",
        countdown.isWorkCompleted);

    countdownPage.classList.toggle(
        "is-rest-day",
        !countdown.isWorkingDay);

    setText(
        "countdown-status",
        countdown.statusMessage);

    setText(
        "countdown-start-time",
        formatTime(countdown.workStartTime));

    setText(
        "countdown-end-time",
        formatTime(countdown.workEndTime));

    renderLiveValues();
}

function renderLiveValues() {
    const secondsToDisplay =
        countdown.isWorkingNow
            ? remainingSeconds
            : 0;

    const hours =
        Math.floor(secondsToDisplay / 3600);

    const minutes =
        Math.floor(
            (secondsToDisplay % 3600) / 60);

    const seconds =
        secondsToDisplay % 60;

    setText(
        "countdown-hours",
        padNumber(hours));

    setText(
        "countdown-minutes",
        padNumber(minutes));

    setText(
        "countdown-seconds",
        padNumber(seconds));

    setText(
        "countdown-current-time",
        formatTime(currentTime));

    const progress =
        calculateProgressPercentage();

    setText(
        "countdown-progress-text",
        `${progress}%`);

    const progressBar =
        document.getElementById(
            "countdown-progress-bar");

    progressBar.style.width =
        `${progress}%`;
}

function calculateProgressPercentage() {
    if (countdown.isWorkCompleted) {
        return 100;
    }

    if (!countdown.isWorkingNow
        || !countdown.workStartTime
        || !countdown.workEndTime) {

        return Math.max(
            0,
            Math.min(
                100,
                Number(
                    countdown.progressPercentage)
                || 0));
    }

    const startTime =
        new Date(countdown.workStartTime);

    const endTime =
        new Date(countdown.workEndTime);

    const totalMilliseconds =
        endTime.getTime()
        - startTime.getTime();

    const elapsedMilliseconds =
        currentTime.getTime()
        - startTime.getTime();

    if (totalMilliseconds <= 0) {
        return 0;
    }

    return Math.max(
        0,
        Math.min(
            100,
            Math.floor(
                elapsedMilliseconds
                / totalMilliseconds
                * 100)));
}

function formatTime(value) {
    if (!value) {
        return "--:--";
    }

    const showSeconds =
        value instanceof Date;

    const date =
        showSeconds
            ? value
            : new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "--:--";
    }

    const options = {
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
        timeZone: "Europe/Istanbul"
    };

    if (showSeconds) {
        options.second = "2-digit";
    }

    return new Intl.DateTimeFormat(
        "tr-TR",
        options)
        .format(date);
}
function padNumber(value) {
    return String(value)
        .padStart(2, "0");
}

function setText(elementId, value) {
    const element =
        document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

function showMessage(message) {
    const element =
        document.getElementById(
            "countdown-message");

    element.textContent = message;
    element.hidden = false;
}

function hideMessage() {
    const element =
        document.getElementById(
            "countdown-message");

    element.textContent = "";
    element.hidden = true;
}

function handleRefreshError(error) {
    console.error(error);

    showMessage(
        "Sayaç güncellenemedi. Birazdan yeniden denenecek.");
}

function handleInitialError(error) {
    console.error(error);

    showMessage(
        error.message
        ?? "Mesai sayacı yüklenirken bir hata oluştu.");

    setText(
        "countdown-status",
        "Mesai bilgisi yüklenemedi.");
}