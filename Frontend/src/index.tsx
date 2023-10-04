import { Suspense, lazy } from "react";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { createRoot } from "react-dom/client";
import "./index.scss";
import { Header } from "./components/partials";
import Loader from "./components/loader";
import { AuthProvider } from "./components/auth";
import { Login, Logout } from "./pages/loginout";
var Home = lazy(() => import("./pages/home"));

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");

function Index() {
    return (
        <BrowserRouter basename={baseUrl}>
            <AuthProvider>
                <Header />
                <Suspense fallback={<Loader />}>
                    <Routes>
                        <Route path="/" element={<Home />} />
                        <Route path="/login" element={<Login />} />
                        <Route path="/logout" element={<Logout />} />
                    </Routes>
                </Suspense>
            </AuthProvider>
        </BrowserRouter>
    );
}

createRoot(document.getElementById("root")).render(<Index />);