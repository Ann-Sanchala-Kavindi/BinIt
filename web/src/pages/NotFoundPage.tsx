import React from 'react';
import { Link } from 'react-router-dom';

export const NotFoundPage: React.FC = () => {
  return (
    <div style={notFoundStyles.container}>
      <div style={notFoundStyles.card}>
        <h1 style={notFoundStyles.code}>404</h1>
        <h2 style={notFoundStyles.title}>Page Not Found</h2>
        <p style={notFoundStyles.description}>
          The page you are looking for does not exist or has been moved.
        </p>
        <Link to="/" style={notFoundStyles.homeBtn}>
          Back to Home
        </Link>
      </div>
    </div>
  );
};

const notFoundStyles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '60vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: '2rem',
    textAlign: 'center',
    fontFamily: 'system-ui, -apple-system, sans-serif',
  },
  card: {
    maxWidth: '480px',
    backgroundColor: '#ffffff',
    padding: '2.5rem',
    borderRadius: '12px',
    border: '1px solid #e2e8f0',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.05)',
  },
  code: {
    margin: 0,
    fontSize: '3.5rem',
    fontWeight: 800,
    color: '#64748b',
  },
  title: {
    margin: '0.5rem 0 1rem 0',
    fontSize: '1.5rem',
    color: '#1e293b',
  },
  description: {
    color: '#64748b',
    fontSize: '0.95rem',
    lineHeight: 1.5,
    marginBottom: '1.5rem',
  },
  homeBtn: {
    display: 'inline-block',
    padding: '0.625rem 1.25rem',
    backgroundColor: '#16a34a',
    color: '#ffffff',
    textDecoration: 'none',
    borderRadius: '6px',
    fontWeight: 600,
    fontSize: '0.95rem',
  },
};
