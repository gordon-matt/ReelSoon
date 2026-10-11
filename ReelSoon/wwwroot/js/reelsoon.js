window.reelsoon = {
    setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        try { localStorage.setItem('reelsoon.theme', theme); } catch (e) { }
    },
    getTheme() {
        return document.documentElement.getAttribute('data-theme') || 'light';
    }
};
