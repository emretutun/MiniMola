// Runs before stylesheets so a saved dark theme does not flash a light page.
(() => {
    "use strict";
    const key = "minimola.theme";
    const system = window.matchMedia("(prefers-color-scheme: dark)");
    const valid = value => ["light", "dark", "system"].includes(value);
    let preference = "system";
    try {
        const saved = localStorage.getItem(key);
        if (valid(saved)) preference = saved;
    } catch { /* Theme switching still works when browser storage is unavailable. */ }

    function apply() {
        const theme = preference === "system" ? (system.matches ? "dark" : "light") : preference;
        document.documentElement.dataset.bsTheme = theme;
        document.documentElement.style.colorScheme = theme;
        const meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.content = theme === "dark" ? "#11181f" : "#f6f7f9";
        const select = document.getElementById("theme-mode");
        if (select) select.value = preference;
        window.dispatchEvent(new CustomEvent("minimola:themechange", { detail: { theme } }));
    }

    apply();
    system.addEventListener("change", () => { if (preference === "system") apply(); });
    window.addEventListener("storage", event => {
        if (event.key !== key && event.key !== null) return;
        preference = valid(event.newValue) ? event.newValue : "system";
        apply();
    });
    document.addEventListener("DOMContentLoaded", () => {
        const select = document.getElementById("theme-mode");
        if (!select) return;
        select.value = preference;
        select.addEventListener("change", () => {
            preference = valid(select.value) ? select.value : "system";
            try { localStorage.setItem(key, preference); } catch { /* Optional persistence. */ }
            apply();
        });
    });
})();
