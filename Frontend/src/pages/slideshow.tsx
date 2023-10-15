import { useEffect, useRef, useState } from "react";
import { useSlideshow } from "../components/slideshow";
import { Tooltip } from "bootstrap";

export default function Slideshow() {
    const { expansion, setAutoShift, setBlured, setExpansion, setNavContent, autoShift, nextImage, prevImage, navContent } = useSlideshow();
    const [prevBlur, setPrevBlur] = useState<"default" | "blured" | "unblured">("default");
    const listRef = useRef<HTMLUListElement>(null);

    useEffect(() => {
        setExpansion("main");
        setBlured(blured => {
            setPrevBlur(blured);
            return "unblured";
        });
        localStorage.removeItem("unblured");
        return () => {
            setBlured(prevBlur);
            setExpansion("slideshow_main");
            setNavContent(null);
        }
    }, []);

    useEffect(() => {
        if (!listRef.current) return;
        var tooltips = Array.from(listRef.current.children).map((t) => {
            const tooltip = new Tooltip(t, {
                placement: "bottom",
                trigger: "hover"
            });
            return tooltip;
        });
        return () => {
            tooltips.forEach((t) => t.dispose());
        }
    }, [navContent]);

    useEffect(() => {
        setNavContent(<>
            <ul className="navbar-nav" ref={listRef}>
                <li className="navbar-item me-2" data-bs-title="A Realm Reborn"><button className={"btn " + (expansion === "main" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("main")}>ARR</button></li>
                <li className="navbar-item me-2" data-bs-title="Heavensward"><button className={"btn " + (expansion === "ex1" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex1")}>HW</button></li>
                <li className="navbar-item me-2" data-bs-title="Stormblood"><button className={"btn " + (expansion === "ex2" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex2")}>SB</button></li>
                <li className="navbar-item me-2" data-bs-title="Shadowbringers"><button className={"btn " + (expansion === "ex3" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex3")}>ShB</button></li>
                <li className="navbar-item me-2" data-bs-title="Endwalker"><button className={"btn " + (expansion === "ex4" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex4")}>EW</button></li>
                {/* <li className="navbar-item me-2" data-bs-title="Dawntrail"><button className={"btn " + (expansion === "ex5" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex5")}>DT</button></li> */}
            </ul>
            <ul className="navbar-nav">
                <li className="navbar-item"><button className={"btn btn-info me-2"} onClick={() => setAutoShift(!autoShift)}>{autoShift ? "Stop" : "Start"} auto shift</button></li>
            </ul>
        </>
        );
    }, [expansion, autoShift]);

    return (!autoShift && (<div style={{ position: "absolute", top: 0, left: 0, height: "100vh", width: "100vw", overflow: "hidden", zIndex: -1 }}>
        <i className="rounded-5 text-center hover-glow bi bi-arrow-left-short" style={{ position: "absolute", top: "calc(50vh - 23px)", left: 14, fontSize: 30, width: 46, height: 46, background: "#333333", cursor: "pointer" }} onClick={() => prevImage()}></i>
        <i className="rounded-5 text-center hover-glow bi bi-arrow-right-short" style={{ position: "absolute", top: "calc(50vh - 23px)", right: 14, fontSize: 30, width: 46, height: 46, background: "#333333", cursor: "pointer" }} onClick={() => nextImage()}></i>
    </div>
    )) || (<></>);
}