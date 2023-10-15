import React, { createContext, useContext, useEffect, useState } from "react";
import "./slideshow.scss";

function shuffle<T>(array: T[]): T[] {
    let currentIndex = array.length, randomIndex;

    // While there remain elements to shuffle.
    while (currentIndex > 0) {

        // Pick a remaining element.
        randomIndex = Math.floor(Math.random() * currentIndex);
        currentIndex--;

        // And swap it with the current element.
        [array[currentIndex], array[randomIndex]] = [
            array[randomIndex], array[currentIndex]];
    }

    return array;
}

const ImagesContext = createContext<{ setExpansion: React.Dispatch<React.SetStateAction<string>>, expansion: string, setBlured: React.Dispatch<React.SetStateAction<"blured" | "default" | "unblured">>, setAutoShift: React.Dispatch<React.SetStateAction<boolean>>, setNavContent: React.Dispatch<React.SetStateAction<React.ReactNode>>, navContent: React.ReactNode, autoShift: boolean, nextImage: () => void, prevImage: () => void }>(null);

export function useSlideshow() {
    return useContext(ImagesContext);
}

const urlPath = "https://pdp.wildwolf.dev/files/victoryposes/";

async function getImages(path?: string) {
    const resp = await fetch(urlPath + (path ? path : ""));
    const data = await resp.text();
    const domData = new DOMParser().parseFromString(data, "text/html");
    return Array.from(domData.querySelectorAll("body > pre > a") as NodeListOf<HTMLAnchorElement>).map(t => t.href.replace(t.origin, "")).map((t) => t.substring(1)).slice(1);
}

async function getAllImages() {
    var data = await getImages();
    var expansions = data.filter((t) => t.endsWith("/"));
    var images = await Promise.all(expansions.map(async (expansion) => {
        var paths = await getImages(expansion);
        return shuffle(paths).map((path) => {
            return { expansion: expansion.substring(0, expansion.length - 1), path: expansion + path };
        });
    }));
    return images.flat();
}

function loadImage(path: string, callback: () => void) {
    var img = new Image();
    img.onload = callback;
    img.src = urlPath + path;
}

export default function Slideshow(props: { children: React.ReactNode }) {
    const [navContent, setNavContent] = useState<React.ReactNode>(null);
    const [expansion, setExpansion] = useState<string>("slideshow_main");
    const [imagePaths, setImagePaths] = useState<{ expansion: string, path: string }[]>([]);
    const [currentImage, setCurrentImage] = useState<string>("");
    const [nextImage, setNextImage] = useState<string>("");
    const [lastUpdate, setLastUpdate] = useState<{ time: Date, delta: number }>({ time: new Date(), delta: 0 });
    const [autoShift, setAutoShift] = useState<boolean>(true);
    const [state, setState] = useState<"shifting" | "shifted" | "shifted-prepared">("shifted");
    const [blured, setBlured] = useState<"blured" | "default" | "unblured">("default");
    const loadDelays = [300, 59700];

    useEffect(() => {
        getAllImages().then(
            (data) => {
                setImagePaths(data);
                var images = data.filter(t => t.expansion === expansion)
                setCurrentImage(images[0].path);
                setNextImage(images[1].path);
                setLastUpdate({ time: new Date(), delta: 0 });
            },
            (err) => console.error(err)
        );
    }, [setImagePaths]);

    useEffect(() => {
        if (state === "shifting" && ((autoShift && lastUpdate.delta > loadDelays[0]) || (!autoShift && lastUpdate.delta > 300))) {
            setState("shifted");
            setCurrentImage(nextImage);
            setLastUpdate({ time: new Date(), delta: 0 });
        }
        else if (state === "shifted" && lastUpdate.delta > loadDelays[1] && !autoShift) {
            changeImage(false);
        }
        else if (state === "shifted-prepared") {
            setState("shifting");
            setLastUpdate({ time: new Date(), delta: 0 });
        }
        const interval = setInterval(() => {
            setLastUpdate({ time: new Date(), delta: new Date().getTime() - lastUpdate.time.getTime() + lastUpdate.delta });
        }, 100);
        return () => clearInterval(interval);
    }, [lastUpdate]);

    useEffect(() => {
        console.log(state);
        if (state === "shifted") {
            changeImage(false);
        }
    }, [expansion]);

    function changeImage(prev: boolean) {
        var images = imagePaths.filter((t) => t.expansion === expansion).map((t) => t.path);
        var nextImage = !prev ?
            images.indexOf(currentImage) + 1 === images.length ? images[0] : images[images.indexOf(currentImage) + 1]
            :
            images.indexOf(currentImage) - 1 === -1 ? images[images.length - 1] : images[images.indexOf(currentImage) - 1];
        loadImage(nextImage, () => {
            setNextImage(nextImage);
            setState("shifted-prepared");
            setLastUpdate({ time: new Date(), delta: 0 });
        });
    }

    return (
        <ImagesContext.Provider value={{ setExpansion, expansion, setBlured, setAutoShift, setNavContent, navContent, autoShift, nextImage: () => changeImage(false), prevImage: () => changeImage(true) }}>
            {props.children}
            <div className={"slideshow-container" + (blured === "blured" ? " blur" : "") + (blured === "unblured" ? " unblur" : "")}>
                {currentImage && <div className="slideshow-image" style={{ backgroundImage: `url("${urlPath}${currentImage}")` }} />}
                {nextImage && <div className={"slideshow-image" + (state === "shifting" ? "" : " hidden")} style={{ backgroundImage: `url("${urlPath}${nextImage}")` }} />}
            </div>
        </ImagesContext.Provider>
    )
}