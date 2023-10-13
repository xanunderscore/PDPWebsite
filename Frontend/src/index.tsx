import { StrictMode, Suspense, lazy } from "react";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { createRoot } from "react-dom/client";
import "./index.scss";
import { Header } from "./components/partials";
import Loader from "./components/loader";
import { AuthProvider } from "./components/auth";
import Slideshow from "./components/slideshow";
import Modal from "./components/modal";
import History from "./components/history";
import SignalRProvider from "./components/signalr";
import RequestProvider from "./components/request";
import SlideshowPage from "./pages/slideshow";
import { Login, Logout } from "./pages/loginout";
var Schedule = lazy(() => import("./pages/schedule"));
var Home = lazy(() => import("./pages/home"));
var About = lazy(() => import("./pages/about"));

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");

function Index() {
    return (
        <StrictMode>
            <SignalRProvider>
                <BrowserRouter basename={baseUrl}>
                    <History>
                        <RequestProvider>
                            <AuthProvider>
                                <Modal>
                                    <Header />
                                    <Suspense fallback={<Loader />}>
                                        <div className="fill-page">
                                            <Routes>
                                                <Route path="/" element={<Home />} />
                                                <Route path="/login" element={<Login />} />
                                                <Route path="/logout" element={<Logout />} />
                                                <Route path="/slideshow" element={<SlideshowPage />} />
                                                <Route path="/about" element={<About />} />
                                                <Route path="/schedule" element={<Schedule />} />
                                            </Routes>
                                        </div>
                                    </Suspense>
                                </Modal>
                            </AuthProvider>
                        </RequestProvider>
                    </History>
                </BrowserRouter>
            </SignalRProvider>
            <Slideshow />
        </StrictMode>
    );
}

createRoot(document.getElementById("root")).render(<Index />);