import { useAuth } from "../components/auth";
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

export function Login() {
    const auth = useAuth();
    const navigate = useNavigate();

    async function asyncFunc() {
        if (window.location.hash) {
            var token = window.location.hash.substring(window.location.hash.indexOf('#') + 1);
            var stuff = token.split("&").reduce((acc: { [str: string]: string }, cur) => {
                var parts = cur.split("=");
                acc[parts[0]] = parts[1];
                return acc;
            }, {});
            var userId = (await (await fetch("https://discord.com/api/users/@me", {
                headers: {
                    "Authorization": "Bearer " + stuff["access_token"]
                }
            })).json() as { id: string }).id;
            auth?.login(userId);
            navigate("/");
        }
        else {
            window.location.href = `https://discord.com/api/oauth2/authorize?client_id=1077847797954531388&redirect_uri=${encodeURIComponent(window.location.origin)}%2Flogin&response_type=token&scope=identify`;
        }
    }

    useEffect(() => {
        asyncFunc();
    });

    return (
        <div></div>
    );
}

export function Logout() {
    const auth = useAuth();
    const navigate = useNavigate();

    useEffect(() => {
        auth?.logout();
        navigate("/");
    });

    return (
        <div></div>
    );
}