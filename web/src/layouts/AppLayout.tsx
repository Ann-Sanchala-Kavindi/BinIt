import React from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export const AppLayout: React.FC = () => {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div style={layoutStyles.container}>
      <header style={layoutStyles.header}>
        <div style={layoutStyles.brand}>
          <h1 style={layoutStyles.title}>Smart Waste Management System</h1>
        </div>
        <div style={layoutStyles.userSection}>
          {user && (
            <div style={layoutStyles.userInfo}>
              <span style={layoutStyles.userName}>{user.fullName}</span>
              <span style={layoutStyles.roleBadge}>{user.role}</span>
            </div>
          )}
          <button
            onClick={handleLogout}
            style={layoutStyles.logoutBtn}
            type="button"
          >
            Logout
          </button>
        </div>
      </header>

      <main style={layoutStyles.main}>
        <Outlet />
      </main>
    </div>
  );
};

const layoutStyles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    flexDirection: 'column',
    fontFamily: 'system-ui, -apple-system, sans-serif',
    backgroundColor: '#f8fafc',
    color: '#0f172a',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '1rem 2rem',
    backgroundColor: '#ffffff',
    borderBottom: '1px solid #e2e8f0',
    boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
  },
  brand: {
    display: 'flex',
    alignItems: 'center',
  },
  title: {
    margin: 0,
    fontSize: '1.25rem',
    fontWeight: 700,
    color: '#16a34a', // Waste/Eco green
  },
  userSection: {
    display: 'flex',
    alignItems: 'center',
    gap: '1rem',
  },
  userInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
  },
  userName: {
    fontWeight: 600,
    fontSize: '0.95rem',
  },
  roleBadge: {
    fontSize: '0.75rem',
    padding: '0.2rem 0.6rem',
    backgroundColor: '#e0f2fe',
    color: '#0369a1',
    borderRadius: '9999px',
    fontWeight: 600,
    textTransform: 'uppercase',
  },
  logoutBtn: {
    padding: '0.4rem 0.9rem',
    fontSize: '0.875rem',
    color: '#ef4444',
    backgroundColor: '#fee2e2',
    border: '1px solid #fca5a5',
    borderRadius: '6px',
    cursor: 'pointer',
    fontWeight: 500,
  },
  main: {
    flex: 1,
    padding: '2rem',
    maxWidth: '1200px',
    width: '100%',
    margin: '0 auto',
    boxSizing: 'border-box',
  },
};
