export function setCultureCookie(name, value) {
    const expires = new Date();
    expires.setFullYear(expires.getFullYear() + 1);

    let cookie = `${name}=${value}; expires=${expires.toUTCString()}; path=/; samesite=lax`;
    if (window.location.protocol === "https:") {
        cookie += "; secure";
    }

    document.cookie = cookie;
}
