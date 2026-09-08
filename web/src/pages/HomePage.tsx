import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export const HomePage: React.FC = () => {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div style={homeStyles.container}>
      <div style={homeStyles.card}>
        <h2 style={homeStyles.heading}>Smart Waste Management System</h2>
        <div style={homeStyles.welcomeBox}>
          <p style={homeStyles.welcomeText}>
            Welcome, <strong>{user?.fullName || 'User'}</strong>
          </p>
          <p style={homeStyles.roleText}>
            Role: <span style={homeStyles.roleBadge}>{user?.role}</span>
          </p>
          <p style={homeStyles.emailText}>Email: {user?.email}</p>
        </div>

        <div style={homeStyles.infoBox}>
          <p style={homeStyles.infoNotice}>
            <strong>Authentication Foundation Ready:</strong> You are securely logged in.
            Business modules (Waste Reporting, Bin Monitoring, Collection Fleet, Complaints, AI Services)
            will be mounted in subsequent steps.
          </p>
        </div>

        <div style={homeStyles.actions}>
          <button onClick={handleLogout} style={homeStyles.logoutBtn} type="button">
            Logout
          </button>
        </div>
      </div>
    </div>
  );
};

const homeStyles: Record<string, React.CSSProperties> = {
  container: {
    padding: '1rem 0',
  },
  card: {
    backgroundColor: '#ffffff',
    borderRadius: '12px',
    padding: '2.5rem',
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
    border: '1px solid #e2e8f0',
  },
  heading: {
    margin: '0 0 1.5rem 0',
    fontSize: '1.75rem',
    fontWeight: 700,
    color: '#15803d',
  },
  welcomeBox: {
    backgroundColor: '#f8fafc',
    padding: '1.25rem',
    borderRadius: '8px',
    border: '1px solid #e2e8f0',
    marginBottom: '1.5rem',
  },
  welcomeText: {
    margin: '0 0 0.5rem 0',
    fontSize: '1.125rem',
    color: '#1e293b',
  },
  roleText: {
    margin: '0 0 0.5rem 0',
    fontSize: '1rem',
    color: '#475569',
  },
  roleBadge: {
    padding: '0.2rem 0.6rem',
    backgroundColor: '#dcfce7',
    color: '#15803d',
    borderRadius: '9999px',
    fontWeight: 600,
    fontSize: '0.875rem',
  },
  emailText: {
    margin: 0,
    fontSize: '0.875rem',
    color: '#64748b',
  },
  infoBox: {
    backgroundColor: '#eff6ff',
    border: '1px solid #bfdbfe',
    borderRadius: '8px',
    padding: '1rem 1.25rem',
    marginBottom: '2rem',
  },
  infoNotice: {
    margin: 0,
    fontSize: '0.9rem',
    color: '#1e40af',
    lineHeight: 1.5,
  },
  actions: {
    display: 'flex',
    gap: '1rem',
  },
  logoutBtn: {
    padding: '0.625rem 1.25rem',
    fontSize: '0.95rem',
    fontWeight: 600,
    color: '#dc2626',
    backgroundColor: '#fee2e2',
    border: '1px solid #fca5a5',
    borderRadius: '6px',
    cursor: 'pointer',
  },
};
