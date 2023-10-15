import { Link, useLocation } from 'react-router-dom';
import { useAuth } from './auth';
import "./partials.scss";
import { Collapse } from 'bootstrap';
import { createRef, useEffect, useState } from 'react';
import { useSlideshow } from './slideshow';

export function Header() {
    const auth = useAuth();
    const location = useLocation();
    const { setBlured, navContent } = useSlideshow();
    const collapseRef = createRef<HTMLDivElement>();
    const [collpase, setCollapse] = useState<Collapse>(null);
    const [toggled, setToggled] = useState<boolean>(false);

    useEffect(() => {
        setCollapse(new Collapse(collapseRef.current!, { toggle: false }));
    }, [setCollapse]);

    function toggleBlur() {
        localStorage.removeItem("blured");
        setBlured(blured => blured === "unblured" ? "unblured" : blured === "blured" ? "default" : "blured");
        setToggled(toggled => !toggled);
    }

    return (
        <nav className='navbar navbar-expand-lg bg-body-tertiary navbar-shadow-bottom'>
            <div className='container-fluid'>
                <Link to="/" className='navbar-brand'>PDP</Link>
                <div className="me-2 d-flex align-items-center">
                    <button className="navbar-toggler" type="button" aria-controls="navbarCollapse" aria-expanded="false" aria-label="Toggle navigation" onClick={(e) => {
                        e.preventDefault();
                        collpase?.toggle();
                    }}>
                        <span className="navbar-toggler-icon"></span>
                    </button>
                </div>
                <div className='collapse navbar-collapse justify-content-between' ref={collapseRef}>
                    <ul className='navbar-nav'>
                        <li className='nav-item'>
                            <Link to="/" className='nav-link'>Home</Link>
                        </li>
                        <li className='nav-item'>
                            <Link to="/about" className='nav-link'>About</Link>
                        </li>
                        <li className='nav-item'>
                            <Link to="/schedule" className='nav-link'>Schedule</Link>
                        </li>
                        <li className='nav-item'>
                            <Link to="/slideshow" className='nav-link'>Slideshow</Link>
                        </li>
                        {auth.user && <li className='nav-item'>
                            <Link to="https://pdp.wildwolf.dev/files" className='nav-link'>Files</Link>
                        </li>}
                    </ul>
                    {navContent}
                    <ul className='navbar-nav'>
                        {location.pathname !== "/slideshow" && <div className='form-check form-switch rounded-5' style={{ paddingLeft: "3rem", paddingRight: ".5rem", backgroundColor: "rgba(0,0,0,0.25)" }}>
                            <input className='form-check-input' type='checkbox' id='darkSwitch' onChange={toggleBlur} checked={toggled} />
                            <label className='form-check-label' htmlFor='darkSwitch'>Blur background</label>
                        </div>}
                        <li className='nav-item'>
                            {auth?.user ? <Link to="/logout" className='nav-link'>Logout</Link> : <Link to="/login" className='nav-link'>Login</Link>}
                        </li>
                    </ul>
                </div>
            </div>
        </nav>
    );
}