import { useAuth } from "../components/auth";
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useHistory } from "../components/history";

export function Login() {
    const auth = useAuth();
    const navigate = useNavigate();
    const history = useHistory();

    async function asyncFunc() {
        if (window.location.hash) {
            var token = window.location.hash.substring(window.location.hash.indexOf('#') + 1);
            var stuff = token.split("&").reduce((acc: { [str: string]: string }, cur) => {
                var parts = cur.split("=");
                acc[parts[0]] = parts[1];
                return acc;
            }, {});
            if (stuff["error"]) {
                console.error(stuff["error"]);
                return;
            }
            var userId = (await (await fetch("https://discord.com/api/users/@me", {
                headers: {
                    "Authorization": "Bearer " + stuff["access_token"]
                }
            })).json() as { id: string }).id;
            auth?.login(userId);
            var redirect = localStorage.getItem("redirect");
            if (redirect) {
                localStorage.removeItem("redirect");
            }
            navigate(redirect ?? decodeURIComponent(stuff["state"]));
        }
        else {
            console.log(history);
            var last = history[history.length - 1] ?? "/";
            localStorage.setItem("redirect", last);
            setTimeout(() => {
                window.location.href = `https://discord.com/api/oauth2/authorize?client_id=1077847797954531388&redirect_uri=${encodeURIComponent(window.location.origin)}%2Flogin&response_type=token&scope=identify&state=${encodeURIComponent(last)}`;
            }, 10);
        }
    }

    useEffect(() => {
        asyncFunc();
    }, []);

    return (
        <div></div>
    );
}

export function Logout() {
    const auth = useAuth();
    const navigate = useNavigate();
    const history = useHistory();

    useEffect(() => {
        auth?.logout();
        var last = history[history.length - 1] ?? "/";
        navigate(last);
    });

    return (
        <div></div>
    );
}