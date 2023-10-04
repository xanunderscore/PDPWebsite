import { getCookie } from "./cookie";

export async function request(url: string, options: RequestInit = {}): Promise<Response> {
    const token = getCookie("token");
    console.log(token);
    options = {
        ...options,
        headers: {
            ...options.headers,
            "Authorization": token ? "Bearer " + token.value : undefined
        }
    }
    console.log(options);
    if (process.env.REACT_APP_API_URL) {
        url = process.env.REACT_APP_API_URL + url;
    }
    const response = await fetch(url, options);
    if (!response.ok) {
        throw new Error(response.statusText);
    }
    return response;
}