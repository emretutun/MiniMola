import {
    Application,
    Container,
    Graphics
} from "pixi.js";

const page = document.getElementById("aquarium-page");
let selectedFish = null;

if (page) {
    startAquarium().catch(showError);
}

async function startAquarium() {
    const apiUrl = page.dataset.apiUrl;

    const response = await fetch(apiUrl, {
        method: "GET",
        credentials: "same-origin",
        headers: {
            Accept: "application/json"
        }
    });

    if (!response.ok) {
        throw new Error(`Akvaryum verisi alınamadı: ${response.status}`);
    }

    const aquarium = await response.json();

    updatePageInformation(aquarium);
    initializeFishDetailDialog();
    initializeUpgradePanel().catch(showUpgradeError);

    const host = document.getElementById("aquarium-canvas-host");

    const app = new Application();

    await app.init({
        resizeTo: host,
        backgroundAlpha: 0,
        antialias: true,
        autoDensity: true,
        resolution: Math.min(window.devicePixelRatio || 1, 2)
    });

    host.appendChild(app.canvas);

    const environmentLayer = new Container();
    const decorationLayer = new Container();
    const fishLayer = new Container();
    const bubbleLayer = new Container();

    decorationLayer.sortableChildren = true;

    app.stage.addChild(environmentLayer);
    app.stage.addChild(decorationLayer);
    app.stage.addChild(fishLayer);
    app.stage.addChild(bubbleLayer);

    drawEnvironment(app, environmentLayer);

    const decorationStates = aquarium.decorations.map(
        decoration => createDecoration(
            app,
            decorationLayer,
            decoration));

    const fishStates = aquarium.fish.map((fish, index) =>
        createFish(
            app,
            fishLayer,
            bubbleLayer,
            fish,
            index));

    const bubbleStates = [];
    let bubbleTimer = 0;

    app.ticker.add((ticker) => {
        const delta = ticker.deltaTime;
        const width = app.screen.width;
        const height = app.screen.height;

        for (const state of fishStates) {
            state.angle += 0.025 * delta;
            state.container.x +=
                state.direction * state.speed * delta;

            state.container.y =
                state.baseY + Math.sin(state.angle) * 9;

            if (state.container.x > width - 65) {
                state.direction = -1;
                applyFishDirection(state);
            }

            if (state.container.x < 65) {
                state.direction = 1;
                applyFishDirection(state);
            }

            state.baseY = Math.min(
                state.baseY,
                height * 0.72);
        }

        bubbleTimer += delta;

        if (bubbleTimer > 28) {
            bubbleTimer = 0;

            bubbleStates.push(
                createBubble(
                    bubbleLayer,
                    Math.random() * width,
                    height + 10));
        }

        updateBubbles(
            bubbleLayer,
            bubbleStates,
            delta);
    });

    window.addEventListener("resize", () => {
        window.requestAnimationFrame(() => {
            drawEnvironment(app, environmentLayer);

            for (const state of fishStates) {
                state.baseY = Math.min(
                    state.baseY,
                    app.screen.height * 0.72);
            }

            for (const state of decorationStates) {
                positionDecoration(
                    app,
                    state);
            }

        });
    });

    document.getElementById("aquarium-loading")?.remove();
}

function updatePageInformation(aquarium) {
    setText("aquarium-name", aquarium.name);
    setText("aquarium-user-name", aquarium.userDisplayName);
    setText(
        "aquarium-point-balance",
        aquarium.pointBalance.toLocaleString("tr-TR"));
    setText("aquarium-level", aquarium.level);
    setText("aquarium-fish-count", aquarium.fish.length);
    setText("aquarium-capacity", aquarium.capacity);
}

function setText(elementId, value) {
    const element = document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

async function initializeUpgradePanel() {
    const button = document.getElementById(
        "aquarium-upgrade-button");

    if (!button) {
        return;
    }

    button.addEventListener(
        "click",
        upgradeAquarium);

    const status = await getUpgradeStatus();

    renderUpgradeStatus(status);
}

async function getUpgradeStatus() {
    const response = await fetch(
        page.dataset.upgradeUrl,
        {
            method: "GET",
            credentials: "same-origin",
            headers: {
                Accept: "application/json"
            }
        });

    if (!response.ok) {
        throw new Error(
            `Yükseltme bilgileri alınamadı: ${response.status}`);
    }

    return await response.json();
}

async function upgradeAquarium() {
    const button = document.getElementById(
        "aquarium-upgrade-button");

    const tokenInput = document.querySelector(
        'input[name="__RequestVerificationToken"]');

    if (!button) {
        return;
    }

    if (!tokenInput?.value) {
        showUpgradeMessage(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.",
            false);

        return;
    }

    button.disabled = true;
    button.classList.add("is-loading");
    button.textContent = "Yükseltiliyor...";

    try {
        const response = await fetch(
            page.dataset.upgradeUrl,
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
                result?.lockedReason
                ?? "Akvaryum yükseltilemedi.");
        }

        renderUpgradeStatus(result);

        setText(
            "aquarium-point-balance",
            result.pointBalance.toLocaleString("tr-TR"));

        setText(
            "aquarium-level",
            result.level);

        setText(
            "aquarium-capacity",
            result.capacity);

        showUpgradeMessage(
            `Harika! Akvaryumun seviye ${result.level} oldu.`,
            true);
    }
    catch (error) {
        showUpgradeError(error);
    }
    finally {
        button.classList.remove("is-loading");

        if (button.textContent === "Yükseltiliyor...") {
            button.textContent = "Akvaryumu yükselt";
            button.disabled = false;
        }
    }
}

function renderUpgradeStatus(status) {
    setText(
        "upgrade-current-level",
        status.level);

    setText(
        "upgrade-next-level",
        status.nextLevel ?? "Maks.");

    setText(
        "upgrade-current-capacity",
        status.capacity);

    setText(
        "upgrade-next-capacity",
        status.nextCapacity ?? "Maks.");

    setText(
        "upgrade-cost",
        status.upgradeCost?.toLocaleString("tr-TR") ?? "-");

    const button = document.getElementById(
        "aquarium-upgrade-button");

    if (button) {
        button.disabled = !status.canUpgrade;

        button.textContent =
            status.nextLevel === null
                ? "Maksimum seviyeye ulaştın"
                : "Akvaryumu yükselt";
    }

    if (status.lockedReason) {
        showUpgradeMessage(
            status.lockedReason,
            false,
            false);
    }
    else {
        showUpgradeMessage(
            `Seviye ${status.nextLevel} için hazırsın.`,
            true,
            false);
    }
}

function showUpgradeMessage(
    message,
    success,
    emphasize = true) {

    const messageElement = document.getElementById(
        "aquarium-upgrade-message");

    if (!messageElement) {
        return;
    }

    messageElement.textContent = message;

    messageElement.classList.remove(
        "is-success",
        "is-error");

    if (!emphasize) {
        return;
    }

    messageElement.classList.add(
        success ? "is-success" : "is-error");
}

function showUpgradeError(error) {
    console.error(error);

    showUpgradeMessage(
        error instanceof Error
            ? error.message
            : "Yükseltme sırasında bir sorun oluştu.",
        false);
}


function drawEnvironment(app, layer) {
    for (const child of layer.removeChildren()) {
        child.destroy();
    }

    const width = app.screen.width;
    const height = app.screen.height;
    const groundY = height * 0.82;

    const lightRay = new Graphics()
        .poly([
            width * 0.12, 0,
            width * 0.31, 0,
            width * 0.52, groundY
        ])
        .fill({
            color: 0xc9fff8,
            alpha: 0.08
        });

    const sand = new Graphics()
        .rect(0, groundY, width, height - groundY)
        .fill({
            color: 0xd8c48f,
            alpha: 0.92
        });

    const sandHighlight = new Graphics()
        .ellipse(
            width * 0.48,
            groundY + 12,
            width * 0.54,
            22)
        .fill({
            color: 0xf4e5b5,
            alpha: 0.28
        });

    layer.addChild(lightRay);
    layer.addChild(sand);
    layer.addChild(sandHighlight);

    addRock(
        layer,
        width * 0.12,
        groundY + 8,
        42,
        28);

    addRock(
        layer,
        width * 0.80,
        groundY + 16,
        56,
        34);

    addPlant(
        layer,
        width * 0.08,
        groundY,
        0x2fb487,
        0.9);

    addPlant(
        layer,
        width * 0.87,
        groundY,
        0x28a87c,
        1.15);

    addPlant(
        layer,
        width * 0.94,
        groundY,
        0x47c59a,
        0.75);
}

function addRock(layer, x, y, width, height) {
    const rock = new Graphics()
        .ellipse(x, y, width, height)
        .fill({
            color: 0x506d75,
            alpha: 0.88
        });

    layer.addChild(rock);
}

function addPlant(layer, x, groundY, color, scale) {
    const plant = new Container();

    const stemOne = new Graphics()
        .roundRect(-5, -95, 10, 100, 8)
        .fill(color);

    stemOne.rotation = -0.16;

    const stemTwo = new Graphics()
        .roundRect(8, -75, 9, 80, 8)
        .fill({
            color,
            alpha: 0.88
        });

    stemTwo.rotation = 0.18;

    const stemThree = new Graphics()
        .roundRect(-20, -60, 8, 65, 8)
        .fill({
            color,
            alpha: 0.78
        });

    stemThree.rotation = -0.28;

    plant.addChild(stemOne);
    plant.addChild(stemTwo);
    plant.addChild(stemThree);

    plant.position.set(x, groundY);
    plant.scale.set(scale);

    layer.addChild(plant);
}

function createDecoration(
    app,
    layer,
    decoration) {

    const container = new Container();

    drawDecoration(
        container,
        decoration.assetKey);

    const state = {
        container,
        decorationId: decoration.id,
        decorationName: decoration.name,
        positionX: clamp(
            decoration.positionX,
            0.05,
            0.95),
        positionY: clamp(
            decoration.positionY,
            0.18,
            0.88),
        originalRotation:
            decoration.rotation || 0,
        startPositionX: 0,
        startPositionY: 0,
        dragOffsetX: 0,
        dragOffsetY: 0,
        isDragging: false,
        dragMoved: false
    };

    container.zIndex =
        decoration.zIndex || 0;

    container.rotation =
        state.originalRotation;

    container.scale.set(
        decoration.scale || 1);

    container.eventMode = "static";
    container.cursor = "grab";

    app.stage.eventMode = "static";
    app.stage.hitArea = app.screen;

    container.on("pointerdown", event => {
        state.isDragging = true;
        state.dragMoved = false;

        state.startPositionX =
            state.positionX;

        state.startPositionY =
            state.positionY;

        state.dragOffsetX =
            event.global.x - container.x;

        state.dragOffsetY =
            event.global.y - container.y;

        container.cursor = "grabbing";
        container.alpha = 0.82;
    });

    app.stage.on("pointermove", event => {
        if (!state.isDragging) {
            return;
        }

        const nextX = clamp(
            event.global.x - state.dragOffsetX,
            app.screen.width * 0.05,
            app.screen.width * 0.95);

        const nextY = clamp(
            event.global.y - state.dragOffsetY,
            app.screen.height * 0.18,
            app.screen.height * 0.88);

        if (Math.abs(nextX - container.x) > 2
            || Math.abs(nextY - container.y) > 2) {
            state.dragMoved = true;
        }

        container.position.set(
            nextX,
            nextY);

        state.positionX =
            nextX / app.screen.width;

        state.positionY =
            nextY / app.screen.height;
    });

    const finishDragging = () => {
        if (!state.isDragging) {
            return;
        }

        state.isDragging = false;
        container.cursor = "grab";
        container.alpha = 1;

        if (!state.dragMoved) {
            return;
        }

        showAquariumStatus(
            `${state.decorationName} konumu kaydediliyor...`);

        saveDecorationPosition(
            app,
            state)
            .catch(error => {
                console.error(error);

                state.positionX =
                    state.startPositionX;

                state.positionY =
                    state.startPositionY;

                positionDecoration(
                    app,
                    state);

                showAquariumStatus(
                    error instanceof Error
                        ? error.message
                        : "Dekorasyon konumu kaydedilemedi.");
            });
    };

    app.stage.on(
        "pointerup",
        finishDragging);

    app.stage.on(
        "pointerupoutside",
        finishDragging);

    container.on("pointertap", () => {
        if (state.dragMoved) {
            state.dragMoved = false;
            return;
        }

        showAquariumStatus(
            `${decoration.name} akvaryumunu güzelleştiriyor.`);

        container.rotation =
            state.originalRotation + 0.08;

        window.setTimeout(() => {
            container.rotation =
                state.originalRotation;
        }, 350);
    });

    positionDecoration(
        app,
        state);

    layer.addChild(container);

    return state;
}

function positionDecoration(
    app,
    state) {

    state.container.position.set(
        app.screen.width * state.positionX,
        app.screen.height * state.positionY);
}

async function saveDecorationPosition(
    app,
    state) {

    const tokenInput = document.querySelector(
        'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        throw new Error(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.");
    }

    const response = await fetch(
        `${page.dataset.apiUrl}`
        + `/decorations/${state.decorationId}/position`,
        {
            method: "PUT",
            credentials: "same-origin",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": tokenInput.value
            },
            body: JSON.stringify({
                positionX: state.positionX,
                positionY: state.positionY
            })
        });

    const result = await response
        .json()
        .catch(() => null);

    if (!response.ok) {
        throw new Error(
            result?.message
            ?? "Dekorasyon konumu kaydedilemedi.");
    }

    state.positionX =
        result.positionX;

    state.positionY =
        result.positionY;

    positionDecoration(
        app,
        state);

    showAquariumStatus(
        result.message);
}

function showAquariumStatus(message) {
    const element = document.querySelector(
        ".status-message");

    if (element) {
        element.textContent = message;
    }
}

function drawDecoration(
    container,
    assetKey) {

    switch (assetKey) {
        case "curved-water-plant":
            drawCurvedWaterPlant(container);
            break;

        case "pink-coral":
            drawPinkCoral(container);
            break;

        case "volcanic-rock":
            drawVolcanicRock(container);
            break;

        case "treasure-chest":
            drawTreasureChest(container);
            break;

        case "mini-lighthouse":
            drawMiniLighthouse(container);
            break;

        case "moon-light":
            drawMoonLight(container);
            break;

        default:
            drawVolcanicRock(container);
            break;
    }
}

function drawCurvedWaterPlant(container) {
    const shadow = new Graphics()
        .ellipse(0, 5, 38, 10)
        .fill({
            color: 0x173f43,
            alpha: 0.22
        });

    const stemOne = new Graphics()
        .roundRect(-5, -102, 10, 105, 8)
        .fill(0x39b98a);

    stemOne.rotation = -0.18;

    const stemTwo = new Graphics()
        .roundRect(-4, -84, 9, 88, 8)
        .fill(0x64d2a5);

    stemTwo.position.x = 16;
    stemTwo.rotation = 0.22;

    const stemThree = new Graphics()
        .roundRect(-4, -70, 8, 74, 8)
        .fill(0x248e70);

    stemThree.position.x = -17;
    stemThree.rotation = -0.25;

    const leafOne = new Graphics()
        .ellipse(-17, -63, 13, 6)
        .fill(0x55c999);

    leafOne.rotation = -0.35;

    const leafTwo = new Graphics()
        .ellipse(17, -52, 14, 6)
        .fill(0x74ddb4);

    leafTwo.rotation = 0.35;

    container.addChild(
        shadow,
        stemOne,
        stemTwo,
        stemThree,
        leafOne,
        leafTwo);
}

function drawPinkCoral(container) {
    const shadow = new Graphics()
        .ellipse(0, 5, 42, 10)
        .fill({
            color: 0x533b48,
            alpha: 0.2
        });

    const trunk = new Graphics()
        .roundRect(-7, -88, 14, 92, 9)
        .fill(0xff7f91);

    const leftBranch = new Graphics()
        .roundRect(-6, -56, 12, 58, 8)
        .fill(0xf26981);

    leftBranch.position.set(
        -19,
        -30);

    leftBranch.rotation = -0.72;

    const rightBranch = new Graphics()
        .roundRect(-6, -62, 12, 65, 8)
        .fill(0xff91a1);

    rightBranch.position.set(
        20,
        -25);

    rightBranch.rotation = 0.67;

    const topBranch = new Graphics()
        .roundRect(-5, -46, 10, 49, 7)
        .fill(0xff9aaa);

    topBranch.position.set(
        15,
        -65);

    topBranch.rotation = 0.46;

    container.addChild(
        shadow,
        trunk,
        leftBranch,
        rightBranch,
        topBranch);
}

function drawVolcanicRock(container) {
    const shadow = new Graphics()
        .ellipse(0, 7, 52, 13)
        .fill({
            color: 0x17353e,
            alpha: 0.25
        });

    const backRock = new Graphics()
        .poly([
            -48, 0,
            -35, -45,
            -12, -70,
            8, -58,
            30, -30,
            48, 0
        ])
        .fill(0x465b64);

    const frontRock = new Graphics()
        .poly([
            -43, 0,
            -20, -36,
            3, -24,
            17, -50,
            43, 0
        ])
        .fill(0x344951);

    const holeOne = new Graphics()
        .ellipse(-13, -22, 10, 7)
        .fill(0x20363e);

    const holeTwo = new Graphics()
        .circle(18, -17, 6)
        .fill(0x20363e);

    const highlight = new Graphics()
        .poly([
            -31, -39,
            -13, -62,
            2, -53,
            -11, -43
        ])
        .fill({
            color: 0x71838a,
            alpha: 0.55
        });

    container.addChild(
        shadow,
        backRock,
        frontRock,
        highlight,
        holeOne,
        holeTwo);
}

function drawTreasureChest(container) {
    const shadow = new Graphics()
        .ellipse(0, 6, 50, 12)
        .fill({
            color: 0x513c28,
            alpha: 0.24
        });

    const body = new Graphics()
        .roundRect(-42, -48, 84, 50, 7)
        .fill(0x995d31);

    const lid = new Graphics()
        .roundRect(-42, -76, 84, 34, 14)
        .fill(0x85502d);

    const lowerBand = new Graphics()
        .rect(-42, -43, 84, 8)
        .fill(0xd5a63f);

    const centerBand = new Graphics()
        .rect(-5, -76, 10, 78)
        .fill(0xe0b34f);

    const lock = new Graphics()
        .roundRect(-10, -38, 20, 23, 4)
        .fill(0xf2ce67);

    const keyHole = new Graphics()
        .circle(0, -28, 3)
        .fill(0x62451e);

    container.addChild(
        shadow,
        body,
        lid,
        lowerBand,
        centerBand,
        lock,
        keyHole);
}

function drawMiniLighthouse(container) {
    const shadow = new Graphics()
        .ellipse(0, 6, 45, 11)
        .fill({
            color: 0x3a4650,
            alpha: 0.22
        });

    const tower = new Graphics()
        .poly([
            -28, 0,
            -20, -92,
            20, -92,
            28, 0
        ])
        .fill(0xf5eee3);

    const stripeOne = new Graphics()
        .poly([
            -23, -66,
            23, -66,
            25, -49,
            -25, -49
        ])
        .fill(0xe7655d);

    const stripeTwo = new Graphics()
        .poly([
            -26, -32,
            26, -32,
            27, -15,
            -27, -15
        ])
        .fill(0xe7655d);

    const lightRoom = new Graphics()
        .roundRect(-24, -113, 48, 24, 5)
        .fill(0xf4cc62);

    const roof = new Graphics()
        .poly([
            -31, -112,
            0, -133,
            31, -112
        ])
        .fill(0xca4f49);

    const door = new Graphics()
        .roundRect(-8, -23, 16, 23, 5)
        .fill(0x31566a);

    const glow = new Graphics()
        .circle(0, -102, 39)
        .fill({
            color: 0xffef9a,
            alpha: 0.12
        });

    container.addChild(
        shadow,
        glow,
        tower,
        stripeOne,
        stripeTwo,
        lightRoom,
        roof,
        door);
}

function drawMoonLight(container) {
    const shadow = new Graphics()
        .ellipse(0, 6, 38, 10)
        .fill({
            color: 0x314451,
            alpha: 0.22
        });

    const glow = new Graphics()
        .circle(0, -84, 48)
        .fill({
            color: 0xfff2a8,
            alpha: 0.13
        });

    const stand = new Graphics()
        .roundRect(-4, -64, 8, 67, 5)
        .fill(0x425f72);

    const base = new Graphics()
        .roundRect(-31, -6, 62, 10, 6)
        .fill(0x425f72);

    const moon = new Graphics()
        .circle(0, -91, 28)
        .fill(0xfff1a8);

    const moonCutout = new Graphics()
        .circle(12, -101, 25)
        .fill({
            color: 0x087ca7,
            alpha: 0.94
        });

    container.addChild(
        shadow,
        glow,
        stand,
        base,
        moon,
        moonCutout);
}

function clamp(
    value,
    minimum,
    maximum) {

    return Math.min(
        Math.max(value, minimum),
        maximum);
}



function createFish(
    app,
    fishLayer,
    bubbleLayer,
    fish,
    index) {

    const container = new Container();
    const palette = getFishPalette(fish.assetKey);

    const shadow = new Graphics()
        .ellipse(3, 5, 48, 23)
        .fill({
            color: 0x042f47,
            alpha: 0.22
        });

    const tail = new Graphics()
        .poly([
            -39, 0,
            -67, -24,
            -61, 0,
            -67, 24
        ])
        .fill(palette.tail);

    const body = new Graphics()
        .ellipse(0, 0, 49, 25)
        .fill(palette.body);

    const lowerFin = new Graphics()
        .poly([
            -2, 17,
            -17, 34,
            14, 21
        ])
        .fill({
            color: palette.fin,
            alpha: 0.85
        });

    const eyeWhite = new Graphics()
        .circle(29, -7, 6)
        .fill(0xffffff);

    const eye = new Graphics()
        .circle(31, -7, 2.7)
        .fill(0x082b3a);

    const shine = new Graphics()
        .ellipse(8, -11, 20, 5)
        .fill({
            color: 0xffffff,
            alpha: 0.16
        });

    container.addChild(
        shadow,
        tail,
        lowerFin,
        body,
        shine,
        eyeWhite,
        eye);

    const direction = index % 2 === 0 ? 1 : -1;
    const scale = fish.displayScale || 1;

    const state = {
        container,
        direction,
        scale,
        speed: 0.65 + fish.baseSpeed * 0.75,
        baseY:
            app.screen.height *
            (0.30 + pseudoRandom(fish.id) * 0.38),
        angle: pseudoRandom(fish.id + 10) * Math.PI * 2
    };

    container.position.set(
        app.screen.width *
        (0.22 + pseudoRandom(fish.id + 20) * 0.56),
        state.baseY);

    applyFishDirection(state);

    container.eventMode = "static";
    container.cursor = "pointer";

    container.on("pointertap", () => {
        openFishDetail(fish);

        const message =
            document.querySelector(".status-message");

        if (message) {
            message.textContent =
                `${fish.nickname || fish.speciesName} seni fark etti!`;
        }

        for (let i = 0; i < 7; i++) {
            const bubble = createBubble(
                bubbleLayer,
                container.x + Math.random() * 30 - 15,
                container.y + Math.random() * 15);

            bubble.speed += Math.random() * 0.8;
        }

        state.speed += 0.45;

        window.setTimeout(() => {
            state.speed =
                0.65 + fish.baseSpeed * 0.75;
        }, 700);
    });

    fishLayer.addChild(container);

    return state;
}

function applyFishDirection(state) {
    state.container.scale.set(
        state.direction * state.scale,
        state.scale);
}

function getFishPalette(assetKey) {
    const palettes = {
        "blue-tang": {
            body: 0x2f9fe4,
            tail: 0xffd34e,
            fin: 0x1769aa
        },
        "clown-fish": {
            body: 0xff8738,
            tail: 0xffb15e,
            fin: 0xe45b24
        },
        "neon-tetra": {
            body: 0x39d7d0,
            tail: 0xf35f72,
            fin: 0x168b9c
        },
        "betta-fish": {
            body: 0xa45be0,
            tail: 0xe06eb9,
            fin: 0x7041aa
        },
        "goldfish": {
            body: 0xffb52e,
            tail: 0xffd166,
            fin: 0xe58119
        }
    };

    return palettes[assetKey] ?? {
        body: 0x55c7c2,
        tail: 0x8ce3d8,
        fin: 0x289c9a
    };
}

function createBubble(layer, x, y) {
    const radius = 2 + Math.random() * 5;

    const bubble = new Graphics()
        .circle(0, 0, radius)
        .stroke({
            width: 1.2,
            color: 0xe8fffc,
            alpha: 0.72
        });

    bubble.position.set(x, y);
    layer.addChild(bubble);

    return {
        graphic: bubble,
        speed: 0.45 + Math.random() * 0.8,
        drift: Math.random() * 0.04 + 0.015,
        phase: Math.random() * Math.PI * 2
    };
}

function updateBubbles(layer, states, delta) {
    for (let index = states.length - 1; index >= 0; index--) {
        const state = states[index];

        state.phase += state.drift * delta;
        state.graphic.y -= state.speed * delta;
        state.graphic.x += Math.sin(state.phase) * 0.25 * delta;

        if (state.graphic.y < -15) {
            layer.removeChild(state.graphic);
            state.graphic.destroy();
            states.splice(index, 1);
        }
    }
}

function pseudoRandom(seed) {
    const value = Math.sin(seed * 999) * 10000;
    return value - Math.floor(value);
}

function showError(error) {
    console.error(error);

    const loading =
        document.getElementById("aquarium-loading");

    if (loading) {
        loading.textContent =
            "Akvaryum yüklenirken bir sorun oluştu.";
    }
}
function initializeFishDetailDialog() {
    const dialog =
        document.getElementById("fish-detail-dialog");

    const closeButton =
        document.getElementById("fish-detail-close");

    const form =
        document.getElementById("fish-nickname-form");
    const feedButton =
        document.getElementById("fish-feed-button");

    if (!dialog || !closeButton || !form) {
        return;
    }

    closeButton.addEventListener("click", () => {
        dialog.close();
    });

    dialog.addEventListener("click", event => {
        if (event.target === dialog) {
            dialog.close();
        }
    });

    dialog.addEventListener("close", () => {
        selectedFish = null;
        showFishDetailMessage("", null);
    });
    feedButton?.addEventListener(
        "click",
        feedSelectedFish);

    form.addEventListener(
        "submit",
        updateSelectedFishNickname);
}

function openFishDetail(fish) {
    const dialog =
        document.getElementById("fish-detail-dialog");

    if (!dialog) {
        return;
    }

    selectedFish = fish;

    const nickname =
        fish.nickname?.trim() || fish.speciesName;

    setText("fish-detail-name", nickname);
    setText("fish-detail-species", fish.speciesName);

    setText(
        "fish-detail-description",
        fish.speciesDescription
        || "Akvaryumunun sevimli sakinlerinden biri.");

    setText(
        "fish-detail-rarity",
        getFishRarityText(fish.rarity));

    setText(
        "fish-detail-acquired",
        formatFishAcquiredDate(fish.acquiredAtUtc));
    updateFishCarePanel(fish);

    const nicknameInput =
        document.getElementById("fish-nickname-input");

    if (nicknameInput) {
        nicknameInput.value = nickname;
    }

    const avatar =
        document.getElementById("fish-detail-avatar");

    if (avatar) {
        avatar.dataset.fish = fish.assetKey || "";
    }

    showFishDetailMessage("", null);

    if (!dialog.open) {
        dialog.showModal();
    }

    window.setTimeout(() => {
        nicknameInput?.focus();
        nicknameInput?.select();
    }, 100);
}
function updateFishCarePanel(fish) {
    const happinessPercent = Math.max(
        0,
        Math.min(
            100,
            Number(fish.happinessPercent) || 0));

    setText(
        "fish-care-status",
        fish.careStatus || "Bilinmiyor");

    setText(
        "fish-happiness-value",
        `%${happinessPercent} mutluluk`);

    const happinessBar =
        document.getElementById(
            "fish-happiness-bar");

    if (happinessBar) {
        happinessBar.style.width =
            `${happinessPercent}%`;
    }

    const happinessTrack =
        document.querySelector(
            ".fish-happiness-track");

    happinessTrack?.setAttribute(
        "aria-valuenow",
        happinessPercent.toString());

    const feedButton =
        document.getElementById(
            "fish-feed-button");

    if (feedButton) {
        feedButton.disabled = !fish.canFeed;

        feedButton.textContent =
            fish.canFeed
                ? "Balığı besle"
                : "Balık tok";
    }

    const totalFeedings =
        Number(fish.totalFeedings) || 0;

    let feedingMessage =
        `Toplam ${totalFeedings} kez beslendi.`;

    if (!fish.lastFedAtUtc) {
        feedingMessage =
            "Bu balık henüz beslenmedi.";
    }
    else if (fish.canFeed) {
        feedingMessage +=
            " Yeniden beslenmeye hazır.";
    }
    else if (fish.nextFeedAtUtc) {
        const nextFeedDate =
            new Date(fish.nextFeedAtUtc);

        if (!Number.isNaN(nextFeedDate.getTime())) {
            const formattedDate =
                nextFeedDate.toLocaleString(
                    "tr-TR",
                    {
                        dateStyle: "short",
                        timeStyle: "short"
                    });

            feedingMessage +=
                ` ${formattedDate} tarihinde `
                + "yeniden beslenebilir.";
        }
    }

    setText(
        "fish-feeding-info",
        feedingMessage);
}


async function feedSelectedFish() {
    if (!selectedFish) {
        return;
    }

    const feedButton =
        document.getElementById(
            "fish-feed-button");

    const tokenInput =
        document.querySelector(
            'input[name="__RequestVerificationToken"]');

    if (!tokenInput?.value) {
        showFishDetailMessage(
            "Güvenlik anahtarı bulunamadı. "
            + "Sayfayı yenileyip tekrar dene.",
            false);

        return;
    }

    if (feedButton) {
        feedButton.disabled = true;
        feedButton.textContent = "Besleniyor...";
    }

    try {
        const response = await fetch(
            `${page.dataset.apiUrl}/fish/`
            + `${selectedFish.id}/feed`,
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

        if (!response.ok || !result?.success) {
            throw new Error(
                result?.message
                || result?.detail
                || "Balık beslenemedi.");
        }

        selectedFish.lastFedAtUtc =
            result.lastFedAtUtc;

        selectedFish.totalFeedings =
            result.totalFeedings;

        selectedFish.happinessPercent =
            result.happinessPercent;

        selectedFish.careStatus =
            result.careStatus;

        selectedFish.canFeed =
            result.canFeed;

        selectedFish.nextFeedAtUtc =
            result.nextFeedAtUtc;

        updateFishCarePanel(selectedFish);

        showFishDetailMessage(
            result.message,
            true);

        const statusMessage =
            document.querySelector(
                ".status-message");

        if (statusMessage) {
            statusMessage.textContent =
                `${selectedFish.nickname} `
                + "yemeğini afiyetle yedi!";
        }
    }
    catch (error) {
        showFishDetailMessage(
            error instanceof Error
                ? error.message
                : "Balık beslenemedi.",
            false);
    }
    finally {
        updateFishCarePanel(selectedFish);
    }
}


async function updateSelectedFishNickname(event) {
    event.preventDefault();

    if (!selectedFish) {
        return;
    }

    const input =
        document.getElementById("fish-nickname-input");

    const button =
        document.getElementById("fish-nickname-submit");

    const tokenInput =
        document.querySelector(
            'input[name="__RequestVerificationToken"]');

    const nickname = input?.value.trim() ?? "";

    if (!nickname) {
        showFishDetailMessage(
            "Balığının adı boş bırakılamaz.",
            false);

        input?.focus();
        return;
    }

    if (!tokenInput?.value) {
        showFishDetailMessage(
            "Güvenlik anahtarı bulunamadı. Sayfayı yenile.",
            false);

        return;
    }

    if (button) {
        button.disabled = true;
        button.textContent = "Kaydediliyor...";
    }

    try {
        const response = await fetch(
            `${page.dataset.apiUrl}/fish/${selectedFish.id}/nickname`,
            {
                method: "PUT",
                credentials: "same-origin",
                headers: {
                    Accept: "application/json",
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": tokenInput.value
                },
                body: JSON.stringify({
                    nickname
                })
            });

        const result = await response
            .json()
            .catch(() => null);

        if (!response.ok || !result?.success) {
            const validationMessage =
                result?.errors
                    ? Object.values(result.errors)
                        .flat()
                        .find(message =>
                            typeof message === "string"
                            && message.trim().length > 0)
                    : null;

            throw new Error(
                validationMessage
                || result?.message
                || result?.detail
                || "Balığının adı değiştirilemedi.");
        }

        selectedFish.nickname = result.nickname;

        setText(
            "fish-detail-name",
            result.nickname);

        if (input) {
            input.value = result.nickname;
        }

        const statusMessage =
            document.querySelector(".status-message");

        if (statusMessage) {
            statusMessage.textContent =
                `${result.nickname} yeni adını çok sevdi!`;
        }

        showFishDetailMessage(
            result.message,
            true);
    }
    catch (error) {
        showFishDetailMessage(
            error instanceof Error
                ? error.message
                : "Balığının adı değiştirilemedi.",
            false);
    }
    finally {
        if (button) {
            button.disabled = false;
            button.textContent = "Adı kaydet";
        }
    }
}

function showFishDetailMessage(message, success) {
    const element =
        document.getElementById("fish-detail-message");

    if (!element) {
        return;
    }

    element.textContent = message;
    element.classList.remove(
        "is-success",
        "is-error");

    if (success === true) {
        element.classList.add("is-success");
    }

    if (success === false) {
        element.classList.add("is-error");
    }
}

function getFishRarityText(rarity) {
    const values = {
        Common: "Yaygın",
        Uncommon: "Nadir olmayan",
        Rare: "Nadir",
        Epic: "Destansı",
        Legendary: "Efsanevi"
    };

    return values[rarity] || rarity || "-";
}

function formatFishAcquiredDate(value) {
    if (!value) {
        return "-";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "-";
    }

    return new Intl.DateTimeFormat(
        "tr-TR",
        {
            day: "numeric",
            month: "long",
            year: "numeric"
        })
        .format(date);
} 