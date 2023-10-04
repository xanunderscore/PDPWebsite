import { useContext, createContext, useState, useEffect } from "react";
import { request } from "./request";
import { deleteCookie, getCookie, setCookie } from "./cookie";

export const AuthContext = createContext<{
    user: string | null;
    login: (uid: string) => Promise<void>;
    logout: () => Promise<void>;
} | null>(null);

export function useAuth() {
    return useContext(AuthContext);
}

export function AuthProvider(props: any) {
    const [user, setUser] = useState<string | null>(null);

    async function login(uid: string) {
        var resp = await request("/api/auth/login?userId=" + uid, {
            method: "GET"
        });
        if (resp.status !== 200) {
            throw new Error("Failed to login");
        }
        var token = await resp.text();
        setUser(token);
    }

    async function logout() {
        var resp = await request("/api/auth/logout", {
            method: "DELETE"
        });
        if (resp.status !== 200) {
            throw new Error("Failed to logout");
        }
        setUser(null);
    }

    useEffect(() => {
        var k = getCookie("token");
        if (k) {
            setUser(k.value);
        }
    }, [setUser]);

    useEffect(() => {
        if (user) {
            setCookie("token", user, 7);
        }
        else {
            deleteCookie("token");
        }
    }, [user]);

    return (
        <AuthContext.Provider value={{
            user: user,
            login,
            logout
        }}>
            {props.children}
        </AuthContext.Provider>
    );
}