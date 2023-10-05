import { StrictMode, Suspense, lazy } from "react";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { createRoot } from "react-dom/client";
import "./index.scss";
import { Header } from "./components/partials";
import Loader from "./components/loader";
import { AuthProvider } from "./components/auth";
import { Login, Logout } from "./pages/loginout";
import Slideshow from "./components/slideshow";
import About from "./pages/about";
import Modal from "./components/modal";
import History from "./components/history";
var Home = lazy(() => import("./pages/home"));

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");

function Index() {
    return (
        <StrictMode>
            <BrowserRouter basename={baseUrl}>
                <History>
                    <AuthProvider>
                        <Modal>
                            <Header />
                            <Suspense fallback={<Loader />}>
                                <div className="fill-page">
                                    <Routes>
                                        <Route path="/" element={<Home />} />
                                        <Route path="/login" element={<Login />} />
                                        <Route path="/logout" element={<Logout />} />
                                        <Route path="/about" element={<About />} />
                                    </Routes>
                                </div>
                            </Suspense>
                        </Modal>
                    </AuthProvider>
                </History>
            </BrowserRouter>
            <Slideshow />
        </StrictMode>
    );
}

createRoot(document.getElementById("root")).render(<Index />);