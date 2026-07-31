const playerRoot =
    document.getElementById("spotify-web-player");

const playerStatus =
    document.getElementById("spotify-player-status");

const activateButton =
    document.getElementById("spotify-activate-button");

const playerContent =
    document.getElementById("spotify-player-content");

const coverImage =
    document.getElementById("spotify-player-cover");

const coverPlaceholder =
    document.getElementById(
        "spotify-player-cover-placeholder"
    );

const typeElement =
    document.getElementById("spotify-player-type");

const titleElement =
    document.getElementById("spotify-player-title");

const artistElement =
    document.getElementById("spotify-player-artist");

const previousButton =
    document.getElementById("spotify-previous-button");

const playButton =
    document.getElementById("spotify-play-button");

const nextButton =
    document.getElementById("spotify-next-button");

const volumeInput =
    document.getElementById("spotify-volume");

const tabButtons =
    document.querySelectorAll("[data-spotify-tab]");

const searchPanel =
    document.getElementById("spotify-search-panel");

const playlistsPanel =
    document.getElementById("spotify-playlists-panel");

const searchForm =
    document.getElementById("spotify-search-form");

const searchInput =
    document.getElementById("spotify-search-input");

const searchStatus =
    document.getElementById("spotify-search-status");

const searchResults =
    document.getElementById("spotify-search-results");

const refreshPlaylistsButton =
    document.getElementById(
        "spotify-refresh-playlists"
    );

const playlistsStatus =
    document.getElementById(
        "spotify-playlists-status"
    );

const playlistsList =
    document.getElementById(
        "spotify-playlists-list"
    );

let spotifyPlayer = null;
let spotifyDeviceId = null;
let playlistsLoaded = false;

if (playerRoot) {
    window.onSpotifyWebPlaybackSDKReady =
        initializeSpotifyPlayer;

    bindLibraryEvents();
}

function bindLibraryEvents() {
    for (const button of tabButtons) {
        button.addEventListener(
            "click",
            () => {
                selectLibraryTab(
                    button.dataset.spotifyTab
                );
            }
        );
    }

    searchForm?.addEventListener(
        "submit",
        searchSpotifyTracks
    );

    refreshPlaylistsButton?.addEventListener(
        "click",
        () => {
            loadPlaylists(true);
        }
    );

    activateButton?.addEventListener(
        "click",
        activateBrowserPlayback
    );

    previousButton?.addEventListener(
        "click",
        async () => {
            if (spotifyPlayer) {
                await spotifyPlayer.previousTrack();
            }
        }
    );

    playButton?.addEventListener(
        "click",
        async () => {
            if (spotifyPlayer) {
                await spotifyPlayer.togglePlay();
            }
        }
    );

    nextButton?.addEventListener(
        "click",
        async () => {
            if (spotifyPlayer) {
                await spotifyPlayer.nextTrack();
            }
        }
    );

    volumeInput?.addEventListener(
        "change",
        async event => {
            if (!spotifyPlayer) {
                return;
            }

            const volume =
                Number(event.target.value) / 100;

            await spotifyPlayer.setVolume(volume);
        }
    );
}

async function initializeSpotifyPlayer() {
    setPlayerStatus(
        "Spotify hesabına bağlanılıyor..."
    );

    spotifyPlayer = new Spotify.Player({
        name: "MiniMola Web Player",

        getOAuthToken: async callback => {
            try {
                callback(await getAccessToken());
            } catch (error) {
                console.error(
                    "Spotify token alınamadı:",
                    error
                );

                setPlayerStatus(
                    error.message
                    || "Spotify tokenı alınamadı.",
                    true
                );
            }
        },

        volume: 0.5,
        enableMediaSession: true
    });

    spotifyPlayer.addListener(
        "initialization_error",
        error => {
            setPlayerStatus(
                `Oynatıcı başlatılamadı: ${error.message}`,
                true
            );
        }
    );

    spotifyPlayer.addListener(
        "authentication_error",
        error => {
            setPlayerStatus(
                "Spotify yetkilendirmesi geçersiz. "
                + "Hesabını tekrar bağla. "
                + error.message,
                true
            );
        }
    );

    spotifyPlayer.addListener(
        "account_error",
        error => {
            setPlayerStatus(
                "Web Player için Spotify Premium gerekiyor. "
                + error.message,
                true
            );
        }
    );

    spotifyPlayer.addListener(
        "playback_error",
        error => {
            setPlayerStatus(
                `Oynatma hatası: ${error.message}`,
                true
            );
        }
    );

    spotifyPlayer.addListener(
        "ready",
        ({ device_id: deviceId }) => {
            spotifyDeviceId = deviceId;

            activateButton.disabled = false;
            activateButton.textContent =
                "Mevcut müziği buraya aktar";

            setPlayerStatus(
                "Oynatıcı hazır. Bir şarkı arayabilir "
                + "veya listelerinden birini açabilirsin."
            );
        }
    );

    spotifyPlayer.addListener(
        "not_ready",
        ({ device_id: deviceId }) => {
            if (spotifyDeviceId === deviceId) {
                spotifyDeviceId = null;
            }

            activateButton.disabled = true;
            activateButton.textContent =
                "Oynatıcı hazır değil";

            setControlsEnabled(false);

            setPlayerStatus(
                "Spotify oynatıcısının bağlantısı kesildi.",
                true
            );
        }
    );

    spotifyPlayer.addListener(
        "player_state_changed",
        state => {
            if (state) {
                renderPlayerState(state);
            }
        }
    );

    const connected =
        await spotifyPlayer.connect();

    if (!connected) {
        setPlayerStatus(
            "Spotify Web Player bağlantısı kurulamadı.",
            true
        );
    }
}

function selectLibraryTab(tabName) {
    for (const button of tabButtons) {
        const isSelected =
            button.dataset.spotifyTab === tabName;

        button.classList.toggle(
            "is-active",
            isSelected
        );

        button.setAttribute(
            "aria-selected",
            String(isSelected)
        );
    }

    searchPanel.hidden = tabName !== "search";
    playlistsPanel.hidden = tabName !== "playlists";

    if (tabName === "playlists"
        && !playlistsLoaded) {
        loadPlaylists();
    }
}

async function searchSpotifyTracks(event) {
    event.preventDefault();

    const query = searchInput.value.trim();

    if (query.length < 2) {
        setSearchStatus(
            "Arama için en az 2 karakter gir.",
            true
        );

        return;
    }

    searchResults.replaceChildren();

    setSearchStatus("Spotify’da aranıyor...");

    const submitButton =
        searchForm.querySelector(
            "button[type='submit']"
        );

    submitButton.disabled = true;

    try {
        const tracks = await apiGet(
            `/api/spotify/search?query=${encodeURIComponent(query)
            }`
        );

        renderTrackResults(tracks);
    } catch (error) {
        setSearchStatus(
            error.message
            || "Arama sırasında bir hata oluştu.",
            true
        );
    } finally {
        submitButton.disabled = false;
    }
}

function renderTrackResults(tracks) {
    searchResults.replaceChildren();

    if (!Array.isArray(tracks)
        || tracks.length === 0) {
        setSearchStatus(
            "Bu arama için şarkı bulunamadı."
        );

        return;
    }

    const fragment =
        document.createDocumentFragment();

    for (const track of tracks) {
        fragment.appendChild(
            createTrackResult(track)
        );
    }

    searchResults.appendChild(fragment);

    setSearchStatus(
        `${tracks.length} şarkı bulundu.`
    );
}

function createTrackResult(track) {
    const item = document.createElement("article");
    item.className = "spotify-track-item";

    const artwork =
        createArtwork(
            track.imageUrl,
            "spotify-result-cover"
        );

    const info = document.createElement("div");
    info.className = "spotify-result-info";

    const title = document.createElement("p");
    title.className = "spotify-result-title";
    title.textContent = track.name;

    const subtitle = document.createElement("p");
    subtitle.className = "spotify-result-subtitle";
    subtitle.textContent =
        track.albumName
            ? `${track.artistName} · ${track.albumName}`
            : track.artistName;

    info.append(title, subtitle);

    if (track.spotifyUrl) {
        info.appendChild(
            createSpotifySourceLink(
                track.spotifyUrl
            )
        );
    }

    const playTrackButton =
        document.createElement("button");

    playTrackButton.type = "button";
    playTrackButton.className =
        "spotify-result-action";
    playTrackButton.textContent = "Çal";

    playTrackButton.addEventListener(
        "click",
        () => {
            playSpotifyContent(
                {
                    uris: [track.uri]
                },
                `"${track.name}" çalıyor.`,
                searchStatus,
                playTrackButton
            );
        }
    );

    item.append(
        artwork,
        info,
        playTrackButton
    );

    return item;
}

async function loadPlaylists(forceRefresh = false) {
    if (playlistsLoaded && !forceRefresh) {
        return;
    }

    playlistsList.replaceChildren();
    refreshPlaylistsButton.disabled = true;

    setPlaylistsStatus(
        "Çalma listelerin yükleniyor..."
    );

    try {
        const playlists =
            await apiGet(
                "/api/spotify/playlists"
            );

        renderPlaylists(playlists);
        playlistsLoaded = true;
    } catch (error) {
        setPlaylistsStatus(
            error.message
            || "Çalma listeleri yüklenemedi.",
            true
        );
    } finally {
        refreshPlaylistsButton.disabled = false;
    }
}

function renderPlaylists(playlists) {
    playlistsList.replaceChildren();

    if (!Array.isArray(playlists)
        || playlists.length === 0) {
        setPlaylistsStatus(
            "Spotify hesabında gösterilecek "
            + "bir çalma listesi bulunamadı."
        );

        return;
    }

    const fragment =
        document.createDocumentFragment();

    for (const playlist of playlists) {
        fragment.appendChild(
            createPlaylistCard(playlist)
        );
    }

    playlistsList.appendChild(fragment);

    setPlaylistsStatus(
        `${playlists.length} çalma listesi gösteriliyor.`
    );
}

function createPlaylistCard(playlist) {
    const card = document.createElement("article");
    card.className = "spotify-playlist-card";

    const artwork =
        createArtwork(
            playlist.imageUrl,
            "spotify-playlist-image"
        );

    const body = document.createElement("div");
    body.className = "spotify-result-info";

    const title = document.createElement("h3");
    title.textContent = playlist.name;

    const detail = document.createElement("p");

    const ownerText =
        playlist.ownerName
            ? ` · ${playlist.ownerName}`
            : "";

    detail.textContent =
        `${playlist.totalItems} içerik${ownerText}`;

    const playPlaylistButton =
        document.createElement("button");

    playPlaylistButton.type = "button";
    playPlaylistButton.className =
        "spotify-result-action";
    playPlaylistButton.textContent = "Listeyi çal";

    playPlaylistButton.addEventListener(
        "click",
        () => {
            playSpotifyContent(
                {
                    context_uri: playlist.uri
                },
                `"${playlist.name}" çalıyor.`,
                playlistsStatus,
                playPlaylistButton
            );
        }
    );

    body.append(
        title,
        detail,
        playPlaylistButton
    );

    if (playlist.spotifyUrl) {
        body.appendChild(
            createSpotifySourceLink(
                playlist.spotifyUrl
            )
        );
    }

    card.append(artwork, body);

    return card;
}

function createArtwork(imageUrl, className) {
    const artwork = document.createElement("div");
    artwork.className = className;

    if (imageUrl) {
        const image = document.createElement("img");
        image.src = imageUrl;
        image.alt = "";
        artwork.appendChild(image);
    } else {
        artwork.textContent = "♪";
        artwork.setAttribute(
            "aria-hidden",
            "true"
        );
    }

    return artwork;
}

function createSpotifySourceLink(url) {
    const link = document.createElement("a");

    link.className = "spotify-source-link";
    link.href = url;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.textContent = "Spotify’da aç ↗";

    return link;
}

async function playSpotifyContent(
    playbackBody,
    successMessage,
    messageElement,
    actionButton) {
    if (!spotifyPlayer || !spotifyDeviceId) {
        setElementStatus(
            messageElement,
            "Oynatıcı henüz hazır değil. "
            + "Birkaç saniye sonra tekrar dene.",
            true
        );

        return;
    }

    actionButton.disabled = true;

    try {
        await spotifyPlayer.activateElement();

        const accessToken =
            await getAccessToken();

        const response = await fetch(
            "https://api.spotify.com/v1/me/player/play"
            + `?device_id=${encodeURIComponent(spotifyDeviceId)
            }`,
            {
                method: "PUT",

                headers: {
                    Authorization:
                        `Bearer ${accessToken}`,

                    "Content-Type":
                        "application/json"
                },

                body: JSON.stringify(playbackBody)
            }
        );

        if (!response.ok) {
            throw new Error(
                getPlaybackErrorMessage(
                    response.status
                )
            );
        }

        setElementStatus(
            messageElement,
            successMessage
        );

        setPlayerStatus(successMessage);
        setControlsEnabled(true);
    } catch (error) {
        console.error(
            "Spotify oynatma hatası:",
            error
        );

        setElementStatus(
            messageElement,
            error.message
            || "İçerik oynatılamadı.",
            true
        );
    } finally {
        actionButton.disabled = false;
    }
}

async function activateBrowserPlayback() {
    if (!spotifyPlayer || !spotifyDeviceId) {
        setPlayerStatus(
            "Spotify oynatıcısı henüz hazır değil.",
            true
        );

        return;
    }

    activateButton.disabled = true;
    activateButton.textContent = "Aktarılıyor...";

    try {
        await spotifyPlayer.activateElement();

        const accessToken =
            await getAccessToken();

        const response = await fetch(
            "https://api.spotify.com/v1/me/player",
            {
                method: "PUT",

                headers: {
                    Authorization:
                        `Bearer ${accessToken}`,

                    "Content-Type":
                        "application/json"
                },

                body: JSON.stringify({
                    device_ids: [spotifyDeviceId],
                    play: true
                })
            }
        );

        if (!response.ok) {
            throw new Error(
                getPlaybackErrorMessage(
                    response.status
                )
            );
        }

        setPlayerStatus(
            "Mevcut oynatma MiniMola’ya aktarıldı."
        );

        setControlsEnabled(true);
    } catch (error) {
        setPlayerStatus(
            error.message
            || "Oynatma aktarılamadı.",
            true
        );
    } finally {
        activateButton.disabled = false;
        activateButton.textContent =
            "Mevcut müziği buraya aktar";
    }
}

function renderPlayerState(state) {
    const currentTrack =
        state.track_window?.current_track;

    if (!currentTrack) {
        return;
    }

    playerContent.hidden = false;

    typeElement.textContent =
        currentTrack.type === "episode"
            ? "Podcast"
            : "Müzik";

    titleElement.textContent =
        currentTrack.name
        || "Bilinmeyen içerik";

    const artists =
        Array.isArray(currentTrack.artists)
            ? currentTrack.artists
                .map(artist => artist.name)
                .filter(Boolean)
            : [];

    artistElement.textContent =
        artists.length > 0
            ? artists.join(", ")
            : currentTrack.album?.name
            || "Spotify";

    const images =
        currentTrack.album?.images
        || currentTrack.images
        || [];

    const imageUrl =
        images.find(image => image?.url)?.url;

    if (imageUrl) {
        coverImage.src = imageUrl;
        coverImage.hidden = false;
        coverPlaceholder.hidden = true;
    } else {
        coverImage.removeAttribute("src");
        coverImage.hidden = true;
        coverPlaceholder.hidden = false;
    }

    playButton.classList.toggle(
        "is-playing",
        !state.paused
    );

    playButton.setAttribute(
        "aria-label",
        state.paused
            ? "Oynat"
            : "Duraklat"
    );

    setControlsEnabled(true);

    setPlayerStatus(
        state.paused
            ? "Oynatma duraklatıldı."
            : "MiniMola’da çalıyor."
    );
}

async function apiGet(url) {
    const response = await fetch(
        url,
        {
            method: "GET",

            headers: {
                Accept: "application/json"
            },

            credentials: "same-origin",
            cache: "no-store"
        }
    );

    if (!response.ok) {
        let message =
            `Spotify isteği başarısız (${response.status}).`;

        try {
            const errorBody = await response.json();

            if (errorBody.message) {
                message = errorBody.message;
            }
        } catch {
            // Varsayılan hata mesajı kullanılır.
        }

        throw new Error(message);
    }

    return response.json();
}

async function getAccessToken() {
    const tokenResult =
        await apiGet("/api/spotify/token");

    if (!tokenResult.accessToken) {
        throw new Error(
            "Spotify access tokenı boş döndü."
        );
    }

    return tokenResult.accessToken;
}

function getPlaybackErrorMessage(statusCode) {
    switch (statusCode) {
        case 401:
            return "Spotify bağlantını yeniden yapman gerekiyor.";

        case 403:
            return "Bu işlem için Spotify Premium "
                + "ve oynatma izni gerekiyor.";

        case 404:
            return "Spotify oynatıcı cihazı bulunamadı.";

        case 429:
            return "Spotify istek sınırına ulaşıldı. "
                + "Biraz sonra tekrar dene.";

        default:
            return `İçerik oynatılamadı (${statusCode}).`;
    }
}

function setControlsEnabled(enabled) {
    previousButton.disabled = !enabled;
    playButton.disabled = !enabled;
    nextButton.disabled = !enabled;
    volumeInput.disabled = !enabled;
}

function setPlayerStatus(message, isError = false) {
    setElementStatus(
        playerStatus,
        message,
        isError
    );

    playerRoot.classList.toggle(
        "has-error",
        isError
    );
}

function setSearchStatus(message, isError = false) {
    setElementStatus(
        searchStatus,
        message,
        isError
    );
}

function setPlaylistsStatus(
    message,
    isError = false) {
    setElementStatus(
        playlistsStatus,
        message,
        isError
    );
}

function setElementStatus(
    element,
    message,
    isError = false) {
    element.textContent = message;

    element.classList.toggle(
        "is-error",
        isError
    );
}

window.addEventListener(
    "beforeunload",
    () => {
        spotifyPlayer?.disconnect();
    }
);