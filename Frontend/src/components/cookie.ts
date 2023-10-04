/**
 * Gets a cookie by name
 * @param name The name of the cookie
 * @returns The value and ttl of the cookie
 */
export function getCookie(name: string) {
    let cookieString = document.cookie.split(";");
    let cookies: { [name: string]: { value: string, ttl: Date } } = {};
    for (let cookie of cookieString.filter(t => t && !t[0].includes("_expire"))) {
        let [name, value] = cookie.split("=");
        let expire = cookieString.find(t => t.includes(name.trim() + "_expire"));
        if (expire) {
            let [, expireDate] = expire.split("=");
            cookies[name.trim()] = { value, ttl: new Date(expireDate) };
        }
    }
    return cookies[name];
}

/**
 * Sets a cookie
 * @param name The name of the cookie
 * @param value The value of the cookie
 * @param ttl The time to live of the cookie
 */
export function setCookie(name: string, value: string, ttl: number) {
    let date = new Date();
    date.setSeconds(date.getSeconds() + ttl);
    document.cookie = name.trim() + "=" + value + "; expires=" + date.toUTCString();
    document.cookie = name.trim() + "_expire=" + date.toUTCString() + "; expires=" + date.toUTCString();
}

/**
 * Deletes a cookie by name
 * @param name The name of the cookie
 */
export function deleteCookie(name: string) {
    document.cookie = name.trim() + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC";
    document.cookie = name.trim() + "_expire=; expires=Thu, 01 Jan 1970 00:00:00 UTC";
}

/**
 * Deletes all cookies
 */
export function deleteAll() {
    let cookies = document.cookie.split(";");
    for (let cookie of cookies) {
        let name = cookie.split("=")[0];
        deleteCookie(name);
    }
}