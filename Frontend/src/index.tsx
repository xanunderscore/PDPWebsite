import { StrictMode, Suspense, lazy } from "react";
import { createRoot } from "react-dom/client";
import "./index.scss";
import Loader from "./components/loader";
import { AuthProvider } from "./components/auth";
import RequestProvider from "./components/request";
const App = lazy(() => import("./app"));
const Slideshow = lazy(() => import("./components/slideshow"));
const SignalRProvider = lazy(() => import("./components/signalr"));

function Index() {
    return (
        <StrictMode>
            <Suspense fallback={<Loader />}>
                <Slideshow>
                    <SignalRProvider>
                        <RequestProvider>
                            <AuthProvider>
                                <App />
                            </AuthProvider>
                        </RequestProvider>
                    </SignalRProvider>
                </Slideshow>
            </Suspense>
        </StrictMode >
    );
}

createRoot(document.getElementById("root")).render(<Index />);