import { useState } from "react";
import { useAuth } from "./auth";
import { getCookie } from "./cookie";

export function useRequest() {
    const auth = useAuth();
    async function request(url: string, options: RequestInit = {}): Promise<Response> {
        options = {
            ...options,
            headers: {
                ...options.headers,
                "Authorization": auth && auth.user ? "Bearer " + auth.user.token : undefined
            }
        }
        if (process.env.REACT_APP_API_URL) {
            url = process.env.REACT_APP_API_URL + url;
        }
        const response = await fetch(url, options);
        if (!response.ok) {
            throw new Error(response.statusText);
        }
        return response;
    }
    return request;
}
