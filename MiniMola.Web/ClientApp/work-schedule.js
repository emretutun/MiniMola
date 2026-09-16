const page =
    document.getElementById("work-schedule-page");

if (page) {
    initializeWorkSchedule()
        .catch(handleInitialError);
}

async function initializeWorkSchedule() {
    const form =
        document.getElementById("work-schedule-form");

    form.addEventListener(
        "submit",
        saveSchedule);

    await loadSchedule();
}

async function loadSchedule() {
    setSubmitState(true, "Program yükleniyor...");

    const response = await fetch(
        page.dataset.apiUrl,
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
            getErrorMessage(
                result,
                `Program yüklenemedi: ${response.status}`));
    }

    renderDays(result.days);
    setSubmitState(false);
}

function renderDays(days) {
    const container =
        document.getElementById("work-schedule-days");

    container.replaceChildren();

    for (const day of days) {
        container.appendChild(
            createDayElement(day));
    }
}

function createDayElement(day) {
    const article =
        document.createElement("article");

    article.className = "work-schedule-day";
    article.dataset.dayOfWeek = day.dayOfWeek;

    const toggleLabel =
        document.createElement("label");

    toggleLabel.className = "work-day-toggle";

    const checkbox =
        document.createElement("input");

    checkbox.type = "checkbox";
    checkbox.checked = day.isWorkingDay;
    checkbox.dataset.role = "working-day";
    checkbox.setAttribute(
        "aria-label",
        `${day.dayName} çalışma günü`);

    const switchElement =
        document.createElement("span");

    switchElement.className = "work-day-switch";
    switchElement.setAttribute("aria-hidden", "true");

    const nameContainer =
        document.createElement("span");

    nameContainer.className = "work-day-name";

    const dayName =
        document.createElement("strong");

    dayName.textContent = day.dayName;

    const dayStatus =
        document.createElement("small");

    dayStatus.dataset.role = "day-status";

    nameContainer.append(
        dayName,
        dayStatus);

    toggleLabel.append(
        checkbox,
        switchElement,
        nameContainer);

    const startField = createTimeField(
        day,
        "start",
        "Başlangıç",
        day.startTime);

    const endField = createTimeField(
        day,
        "end",
        "Bitiş",
        day.endTime);

    article.append(
        toggleLabel,
        startField,
        endField);

    checkbox.addEventListener(
        "change",
        () => updateDayState(article));

    updateDayState(article);

    return article;
}

function createTimeField(
    day,
    fieldName,
    labelText,
    value) {

    const field =
        document.createElement("div");

    field.className = "work-time-field";

    const inputId =
        `work-${fieldName}-${day.dayOfWeek}`;

    const label =
        document.createElement("label");

    label.htmlFor = inputId;
    label.textContent = labelText;

    const input =
        document.createElement("input");

    input.type = "time";
    input.id = inputId;
    input.step = "60";
    input.value = normalizeTime(value);
    input.dataset.role = `${fieldName}-time`;

    field.append(
        label,
        input);

    return field;
}

function updateDayState(article) {
    const checkbox =
        article.querySelector(
            '[data-role="working-day"]');

    const status =
        article.querySelector(
            '[data-role="day-status"]');

    const timeInputs =
        article.querySelectorAll(
            'input[type="time"]');

    const isWorkingDay =
        checkbox.checked;

    article.classList.toggle(
        "is-rest-day",
        !isWorkingDay);

    status.textContent =
        isWorkingDay
            ? "Çalışma günü"
            : "İzin günü";

    for (const input of timeInputs) {
        input.disabled = !isWorkingDay;
    }
}

async function saveSchedule(event) {
    event.preventDefault();
    hideMessage();

    const tokenInput =
        document.querySelector(
            '#work-schedule-antiforgery '
            + 'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        showMessage(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.",
            "error");

        return;
    }

    const days =
        Array.from(
            document.querySelectorAll(
                ".work-schedule-day"))
            .map(readDay);

    if (!days.some(day => day.isWorkingDay)) {
        showMessage(
            "En az bir çalışma günü seçmelisin.",
            "error");

        return;
    }

    const invalidDay =
        days.find(day =>
            day.isWorkingDay
            && day.startTime === day.endTime);

    if (invalidDay) {
        showMessage(
            "Çalışma günlerinde başlangıç ve bitiş saatleri aynı olamaz.",
            "error");

        return;
    }

    setSubmitState(
        true,
        "Kaydediliyor...");

    try {
        const response = await fetch(
            page.dataset.apiUrl,
            {
                method: "PUT",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json",
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": tokenInput.value
                },
                body: JSON.stringify({
                    days
                })
            });

        const result = await response
            .json()
            .catch(() => null);

        if (!response.ok) {
            throw new Error(
                getErrorMessage(
                    result,
                    `Program kaydedilemedi: ${response.status}`));
        }

        if (result.schedule?.days) {
            renderDays(
                result.schedule.days);
        }

        showMessage(
            result.message
            ?? "Çalışma saatlerin kaydedildi.",
            "success");
    } catch (error) {
        console.error(error);

        showMessage(
            error.message
            ?? "Çalışma saatleri kaydedilirken bir hata oluştu.",
            "error");
    } finally {
        setSubmitState(false);
    }
}

function readDay(article) {
    const checkbox =
        article.querySelector(
            '[data-role="working-day"]');

    const startInput =
        article.querySelector(
            '[data-role="start-time"]');

    const endInput =
        article.querySelector(
            '[data-role="end-time"]');

    return {
        dayOfWeek:
            Number(article.dataset.dayOfWeek),

        isWorkingDay:
            checkbox.checked,

        startTime:
            toApiTime(startInput.value),

        endTime:
            toApiTime(endInput.value)
    };
}

function normalizeTime(value) {
    if (!value) {
        return "09:00";
    }

    return value.substring(0, 5);
}

function toApiTime(value) {
    return `${value}:00`;
}

function getErrorMessage(
    result,
    fallbackMessage) {

    if (result?.errors) {
        const messages =
            Object.values(result.errors)
                .flat()
                .filter(Boolean);

        if (messages.length > 0) {
            return messages.join(" ");
        }
    }

    return result?.message
        ?? result?.detail
        ?? fallbackMessage;
}

function setSubmitState(
    isBusy,
    busyText = "Kaydediliyor...") {

    const button =
        document.getElementById(
            "work-schedule-submit");

    button.disabled = isBusy;

    button.textContent =
        isBusy
            ? busyText
            : "Çalışma saatlerimi kaydet";
}

function showMessage(message, type) {
    const element =
        document.getElementById(
            "work-schedule-message");

    element.textContent = message;
    element.hidden = false;

    element.classList.remove(
        "is-success",
        "is-error");

    element.classList.add(
        type === "success"
            ? "is-success"
            : "is-error");
}

function hideMessage() {
    const element =
        document.getElementById(
            "work-schedule-message");

    element.hidden = true;
    element.textContent = "";

    element.classList.remove(
        "is-success",
        "is-error");
}

function handleInitialError(error) {
    console.error(error);

    const container =
        document.getElementById(
            "work-schedule-days");

    container.textContent =
        "Çalışma programı yüklenemedi.";

    showMessage(
        error.message
        ?? "Çalışma programın yüklenirken bir hata oluştu.",
        "error");

    setSubmitState(true, "Program yüklenemedi");
}