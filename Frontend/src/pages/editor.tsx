import MarkdownEditor from "@uiw/react-markdown-editor";
import DOMPurify from "dompurify";
import { parse } from "marked";

import { useEffect, useRef, useState } from "react";

export default function Editor() {
    const [markdown, setMarkdown] = useState<string>('');
    const [prevMarkdown, setPrevMarkdown] = useState<string>('');
    const divRef = useRef<HTMLDivElement>(null);
    const scrollRefs = useRef<Record<string, HTMLElement>>({});

    useEffect(() => {
        try {
            const html = parse(markdown);
            const clean = DOMPurify.sanitize(html);

            setPrevMarkdown(clean);
        }
        catch (err) {
            console.log(err);
        }
    }, [markdown]);

    useEffect(() => {
        if (divRef.current) {
            scrollRefs.current = {};
            divRef.current.innerHTML = prevMarkdown;
            divRef.current.querySelectorAll("h1").forEach((el) => {
                const el2 = el as HTMLElement;
                if (el2.id)
                    scrollRefs.current[el2.id] = el2;
            });
            divRef.current.querySelectorAll("button[data-navigate]").forEach((el) => {
                el.addEventListener("click", (e) => {
                    e.preventDefault();
                    const target = e.target as HTMLButtonElement;
                    const href = target.dataset.navigate;
                    if (href) {
                        const el = scrollRefs.current[href];
                        if (el) {
                            el.scrollIntoView({ behavior: "smooth" });
                        }
                    }
                });
            });
        }
    }, [prevMarkdown]);

    useEffect(() => {
        console.log("test");
    }, [divRef])

    return (
        <div className="mt-5 d-flex flex-grow-1">
            <div className="col-6" style={{ borderRight: "solid 1px #333333AA" }}>
                <MarkdownEditor
                    style={{ height: "100%" }}
                    value={markdown}
                    onChange={(value, viewUpdate) => {
                        setMarkdown(value);
                    }}
                    enablePreview={false}
                />
            </div>
            <div className="col-6">
                <div className="d-flex justify-content-between align-items-center p-2">
                    <h5 className="m-0">Preview</h5>
                </div>
                <div className="p-2" ref={divRef} />
            </div>
        </div >);
}