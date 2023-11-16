import { lazy } from "react";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Header } from "./components/partials";
import Modal from "./components/modal";
import History from "./components/history";
import SlideshowPage from "./pages/slideshow";
import { Login, Logout } from "./pages/loginout";
import Toast from "./components/toast";
var Schedule = lazy(() => import("./pages/schedule"));
var Home = lazy(() => import("./pages/home"));
var About = lazy(() => import("./pages/about"));
var Editor = lazy(() => import("./pages/editor"));

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");

export default function App() {
    return (
        <BrowserRouter basename={baseUrl}>
            <History>
                <Toast>
                    <Modal>
                        <Header />
                        <div className="fill-page">
                            <Routes>
                                <Route path="/" element={<Home />} />
                                <Route path="/login" element={<Login />} />
                                <Route path="/logout" element={<Logout />} />
                                <Route path="/slideshow" element={<SlideshowPage />} />
                                <Route path="/about" element={<About />} />
                                <Route path="/schedule" element={<Schedule />} />
                                <Route path="/editor" element={<Editor />} />
                            </Routes>
                        </div>
                    </Modal>
                </Toast>
            </History>
        </BrowserRouter>);
}