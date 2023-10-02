import React from "react";
import { BrowserRouter } from "react-router-dom";
import { createRoot } from "react-dom/client";
import "./index.scss";

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");

function Index() {
    return (
        <BrowserRouter basename={baseUrl}>
            <div>hello</div>
        </BrowserRouter>
    );
}

createRoot(document.getElementById("root")).render(<Index />);