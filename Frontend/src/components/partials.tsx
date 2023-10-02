import React from 'react';
import { Link } from 'react-router-dom';

export function Header() {
    return (
        <nav className='navbar navbar-expand-lg bg-body-tertiary'>
            <div className='container-fluid'>
                <Link to="/" className='navbar-brand'>PDP</Link>
                <h1>Header</h1>
            </div>
        </nav>
    );
}