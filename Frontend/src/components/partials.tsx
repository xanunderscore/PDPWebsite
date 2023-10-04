import { Link } from 'react-router-dom';
import { useAuth } from './auth';

export function Header() {
    const auth = useAuth();

    return (
        <nav className='navbar navbar-expand-lg bg-body-tertiary'>
            <div className='container-fluid'>
                <Link to="/" className='navbar-brand'>PDP</Link>
                <div className='collapse navbar-collapse'>
                    <ul className='navbar-nav me-auto'>
                        <li className='nav-item'>
                            <Link to="/" className='nav-link'>Home</Link>
                        </li>
                        <li className='nav-item'>
                            <Link to="/about" className='nav-link'>About</Link>
                        </li>
                    </ul>
                    <ul className='navbar-nav ms-auto'>
                        <li className='nav-item'>
                            {auth?.user ? <Link to="/logout" className='nav-link'>Logout</Link> : <Link to="/login" className='nav-link'>Login</Link>}
                        </li>
                    </ul>
                </div>
            </div>
        </nav>
    );
}