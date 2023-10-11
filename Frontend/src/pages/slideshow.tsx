import { useEffect } from "react";

export default function Slideshow() {
    useEffect(() => {
        localStorage.setItem("unblured", "true");
        return () => localStorage.setItem("unblured", "false");
    }, []);

    return (<></>);
}