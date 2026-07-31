const wordGamePage =
    document.getElementById("word-game");

const TurkishAlphabet =
    "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";

const keyboardRows = [
    ["E", "R", "T", "Y", "U", "I", "O", "P", "Ğ", "Ü"],
    ["A", "S", "D", "F", "G", "H", "J", "K", "L", "Ş", "İ"],
    ["ENTER", "Z", "C", "V", "B", "N", "M", "Ö", "Ç", "BACKSPACE"]
];

let currentGame = null;

if (wordGamePage) {
    initializeWordGame().catch(handleInitialError);
}

async function initializeWordGame() {
    const form =
        document.getElementById("word-guess-form");

    const input =
        document.getElementById("word-guess-input");

    form.addEventListener("submit", submitGuess);

    input.addEventListener("input", () => {
        input.value = normalizeInput(input.value);
        renderBoard();
    });

    const response = await fetch(
        wordGamePage.dataset.apiUrl,
        {
            method: "GET",
            credentials: "same-origin",
            headers: {
                Accept: "application/json"
            }
        });

    const game = await response
        .json()
        .catch(() => null);

    if (!response.ok) {
        throw new Error(
            game?.message
            ?? `Oyun yüklenemedi: ${response.status}`);
    }

    currentGame = game;

    updateGameInterface();
    input.focus();
}

function updateGameInterface() {
    setText(
        "word-game-hint",
        currentGame.hint);

    setText(
        "word-game-reward",
        currentGame.rewardPoints);

    setText(
        "word-game-balance",
        currentGame.pointBalance.toLocaleString("tr-TR"));

    renderBoard();
    renderKeyboard();
    updateFormState();

    if (currentGame.status === "Won") {
        showMessage(
            `Bugünkü kelimeyi buldun: ${currentGame.revealedAnswer}`,
            "success");
    } else if (currentGame.status === "Lost") {
        showMessage(
            `Bugünkü kelime: ${currentGame.revealedAnswer}`,
            "error");
    }
}

function renderBoard() {
    if (!currentGame) {
        return;
    }

    const board =
        document.getElementById("word-game-board");

    const input =
        document.getElementById("word-guess-input");

    const inputCharacters =
        Array.from(input.value);

    board.replaceChildren();

    for (let row = 1;
        row <= currentGame.maxAttempts;
        row++) {

        const savedGuess =
            currentGame.guesses.find(
                item => item.attemptNumber === row);

        const isCurrentRow =
            currentGame.status === "InProgress"
            && row === currentGame.attemptCount + 1;

        for (let column = 0;
            column < currentGame.wordLength;
            column++) {

            const tile =
                document.createElement("div");

            tile.className = "word-tile";

            if (savedGuess) {
                tile.textContent =
                    savedGuess.guess[column] ?? "";

                tile.classList.add(
                    patternToClass(
                        savedGuess.resultPattern[column]));

                tile.style.animationDelay =
                    `${column * 70}ms`;
            } else if (isCurrentRow
                && inputCharacters[column]) {

                tile.textContent =
                    inputCharacters[column];

                tile.classList.add("filled");
            }

            board.appendChild(tile);
        }
    }
}

function renderKeyboard() {
    const keyboard =
        document.getElementById("word-keyboard");

    const letterStates =
        calculateKeyboardStates();

    keyboard.replaceChildren();

    for (const rowKeys of keyboardRows) {
        const row =
            document.createElement("div");

        row.className = "keyboard-row";

        for (const key of rowKeys) {
            const button =
                document.createElement("button");

            button.type = "button";
            button.className = "keyboard-key";

            if (key === "ENTER") {
                button.textContent = "GİR";
                button.classList.add("wide");
            } else if (key === "BACKSPACE") {
                button.textContent = "⌫";
                button.classList.add("wide");
            } else {
                button.textContent = key;

                const state = letterStates.get(key);

                if (state) {
                    button.classList.add(state);
                }
            }

            button.disabled =
                currentGame.status !== "InProgress";

            button.addEventListener("click", () => {
                handleKeyboardKey(key);
            });

            row.appendChild(button);
        }

        keyboard.appendChild(row);
    }
}

function calculateKeyboardStates() {
    const states = new Map();

    const priorities = {
        absent: 1,
        present: 2,
        correct: 3
    };

    for (const guess of currentGame.guesses) {
        for (let index = 0;
            index < guess.guess.length;
            index++) {

            const letter = guess.guess[index];

            const state = patternToClass(
                guess.resultPattern[index]);

            const existingState =
                states.get(letter);

            if (!existingState
                || priorities[state]
                > priorities[existingState]) {

                states.set(letter, state);
            }
        }
    }

    return states;
}

function handleKeyboardKey(key) {
    if (!currentGame
        || currentGame.status !== "InProgress") {
        return;
    }

    const input =
        document.getElementById("word-guess-input");

    if (key === "ENTER") {
        document
            .getElementById("word-guess-form")
            .requestSubmit();

        return;
    }

    if (key === "BACKSPACE") {
        input.value = Array
            .from(input.value)
            .slice(0, -1)
            .join("");

        renderBoard();
        input.focus();
        return;
    }

    if (Array.from(input.value).length >= 5) {
        return;
    }

    input.value += key;
    renderBoard();
    input.focus();
}

async function submitGuess(event) {
    event.preventDefault();

    if (!currentGame
        || currentGame.status !== "InProgress") {
        return;
    }

    const input =
        document.getElementById("word-guess-input");

    const button =
        document.getElementById("word-guess-button");

    const guess = normalizeInput(input.value);

    if (Array.from(guess).length !== 5) {
        showMessage(
            "Tahminin beş harften oluşmalı.",
            "error");

        input.focus();
        return;
    }

    const tokenInput = document.querySelector(
        '#word-antiforgery-form '
        + 'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        showMessage(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.",
            "error");

        return;
    }

    button.disabled = true;
    button.textContent = "Kontrol ediliyor...";

    try {
        const response = await fetch(
            `${wordGamePage.dataset.apiUrl}/guess`,
            {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json",
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": tokenInput.value
                },
                body: JSON.stringify({
                    guess
                })
            });

        const result = await response
            .json()
            .catch(() => null);

        if (result?.game) {
            currentGame = result.game;
            input.value = "";
            updateGameInterface();
        }

        if (!response.ok) {
            showMessage(
                result?.message
                ?? `Tahmin gönderilemedi: ${response.status}`,
                "error");

            return;
        }

        setText(
            "word-game-balance",
            result.pointBalance.toLocaleString("tr-TR"));

        showMessage(
            result.message,
            currentGame.status === "Won"
                ? "success"
                : "info");
    } catch (error) {
        console.error(error);

        showMessage(
            "Tahmin gönderilirken bir bağlantı hatası oluştu.",
            "error");
    } finally {
        button.textContent = "Tahmin et";
        updateFormState();
        input.focus();
    }
}

function updateFormState() {
    const isFinished =
        currentGame.status !== "InProgress";

    const input =
        document.getElementById("word-guess-input");

    const button =
        document.getElementById("word-guess-button");

    input.disabled = isFinished;
    button.disabled = isFinished;

    if (isFinished) {
        input.placeholder = "Oyun tamamlandı";
    }
}

function normalizeInput(value) {
    return Array
        .from(
            value
                .normalize("NFC")
                .toLocaleUpperCase("tr-TR"))
        .filter(character =>
            TurkishAlphabet.includes(character))
        .slice(0, 5)
        .join("");
}

function patternToClass(patternCharacter) {
    if (patternCharacter === "2") {
        return "correct";
    }

    if (patternCharacter === "1") {
        return "present";
    }

    return "absent";
}

function showMessage(message, type) {
    const element =
        document.getElementById("word-game-message");

    element.textContent = message;
    element.hidden = false;

    element.classList.remove(
        "success",
        "info",
        "error");

    element.classList.add(type);
}

function setText(elementId, value) {
    const element = document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

function handleInitialError(error) {
    console.error(error);

    showMessage(
        error.message
        ?? "Kelime oyunu yüklenirken bir sorun oluştu.",
        "error");

    const input =
        document.getElementById("word-guess-input");

    const button =
        document.getElementById("word-guess-button");

    input.disabled = true;
    button.disabled = true;
}